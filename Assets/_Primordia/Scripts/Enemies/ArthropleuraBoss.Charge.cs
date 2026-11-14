using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, F1: el ciclo rodear → aviso bajo la espesura → embestida en línea recta que revienta la espesura al entrar al
    /// claro, lo cruza entero y la revienta al salir. Los dos agujeros quedan para siempre.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (ciclo cerrado con la usuaria el 2026-11-13; velocidades a ojo).
        const float CircleSeconds = 8f, WarnSeconds = 1.4f, LockBefore = 0.35f;
        // Con piezas aún en el claro rodea más deprisa para despejarlo antes de la siguiente embestida.
        const float CircleSpeed = 5f, CatchUpSpeed = 16f, ChargeSpeed = 20f, ChargeBeyond = 5f; // Hasta dónde sigue tras reventar la salida.
        const float CircleTurn = 140f, AimMargin = 2f;

        Vector2 chargeDirection; // Suelo, unitario.
        Vector2 chargeImpact;
        bool chargeBroke, chargeEntered;
        float nextTremor;
        Transform aimRoot;
        readonly List<SpriteRenderer> aimMarks = new();

        void SetMode(Mode mode)
        {
            CurrentMode = mode;
            modeTimer = 0f;
        }

        bool AnyPieceInClearing()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                Vector2 g = NodeGround(i);
                if (InClearing(new Vector2(g.x, g.y * GroundSquash)))
                    return true;
            }
            return false;
        }

        void StepHunt(float dt)
        {
            modeTimer += dt;
            if (!entriesSealed && entryCells.Count > 0 && player != null)
            {
                Vector2 inside = (Vector2)player.position - center;
                if (Mathf.Abs(inside.x) < ClearingHalf.x - 1.5f && Mathf.Abs(inside.y) < ClearingHalf.y - 1.5f)
                    SealEntries();
            }
            Vector2 head = headPos;
            switch (CurrentMode)
            {
                case Mode.Circling:
                    bool clearing = AnyPieceInClearing();
                    Advance(RingPoint(RingAngle(head) - 0.2f), clearing ? CatchUpSpeed : CircleSpeed, CircleTurn, dt);
                    if (modeTimer >= CircleSeconds && PhaseIndex == 0 && !clearing)
                        StartWarn();
                    break;
                case Mode.Warn:
                    // Se queda quieto bajo la espesura: tiembla el suelo y una línea marca la trayectoria (se fija un poco antes).
                    if (modeTimer < WarnSeconds - LockBefore)
                        chargeDirection = AimDirection(head);
                    FaceHead(chargeDirection);
                    UpdateAim(head);
                    if (Time.time >= nextTremor)
                    {
                        nextTremor = Time.time + 0.35f;
                        SpawnEffect(effects.tremor, GroundToWorld(head), 0f, 0.09f);
                    }
                    if (modeTimer >= WarnSeconds)
                        StartCharge(head);
                    break;
                case Mode.Charge:
                    StepCharge(head, dt);
                    break;
            }
            DamagePlayer();
        }

        void StepCharge(Vector2 head, float dt)
        {
            Vector2 next = head + chargeDirection * (ChargeSpeed * dt);
            Vector2 nextWorld = new(next.x, next.y * GroundSquash);
            if (InClearing(nextWorld))
                chargeEntered = true;
            if (chargeEntered && !chargeBroke && thicket.Contains(CellOf(nextWorld)))
            {
                chargeBroke = true;
                chargeImpact = head;
                BreakLine(head, chargeDirection, -1f, ChargeBeyond + 4f);
                SpawnEffect(effects.edgeBreak, GroundToWorld(head), WorldAngle(chargeDirection) - 90f, 0.08f);
            }
            bool atLimit = Mathf.Abs(next.x) > ArenaWidth / 2f - 2f || Mathf.Abs(nextWorld.y) > ArenaHeight / 2f - 2f;
            if (atLimit || chargeBroke && Vector2.Distance(head, chargeImpact) >= ChargeBeyond)
            {
                SetMode(Mode.Circling);
                return;
            }
            heading = WorldAngleToHeading();
            Advance(next, ChargeSpeed, 720f, dt);
        }

        float WorldAngleToHeading() => Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg;

        float WorldAngle(Vector2 groundDirection) => Mathf.Atan2(groundDirection.y * GroundSquash, groundDirection.x) * Mathf.Rad2Deg;

        void StartWarn()
        {
            chargeDirection = AimDirection(headPos);
            SetMode(Mode.Warn);
        }

        /// <summary>Hacia el jugador (o el centro si no está en el claro): siempre apunta a un punto del claro.</summary>
        Vector2 AimDirection(Vector2 head)
        {
            Vector2 world = player != null ? (Vector2)player.position - center : Vector2.zero;
            world.x = Mathf.Clamp(world.x, -ClearingHalf.x + AimMargin, ClearingHalf.x - AimMargin);
            world.y = Mathf.Clamp(world.y, -ClearingHalf.y + AimMargin, ClearingHalf.y - AimMargin);
            Vector2 direction = new Vector2(world.x, world.y / GroundSquash) - head;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
        }

        /// <summary>Tramo de la línea (en mundo, desde el origen) que cruza el claro: distancia de entrada y de salida.</summary>
        bool RayThroughClearing(Vector2 originWorld, Vector2 directionWorld, out float enter, out float exit)
        {
            enter = 0f;
            exit = float.MaxValue;
            for (int axis = 0; axis < 2; axis++)
            {
                float o = originWorld[axis], d = directionWorld[axis], half = ClearingHalf[axis];
                if (Mathf.Abs(d) < 1e-5f)
                {
                    if (Mathf.Abs(o) >= half)
                        return false;
                    continue;
                }
                float t1 = (-half - o) / d, t2 = (half - o) / d;
                enter = Mathf.Max(enter, Mathf.Min(t1, t2));
                exit = Mathf.Min(exit, Mathf.Max(t1, t2));
            }
            return exit > enter;
        }

        void StartCharge(Vector2 head)
        {
            HideAim();
            chargeHit = false;
            chargeBroke = false;
            chargeEntered = false;
            Vector2 origin = new(head.x, head.y * GroundSquash);
            Vector2 worldDirection = new Vector2(chargeDirection.x, chargeDirection.y * GroundSquash).normalized;
            float angle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
            // Abre la espesura desde donde está hasta el claro: agujero de entrada, para siempre.
            if (RayThroughClearing(origin, worldDirection, out float enter, out _))
            {
                BreakLine(head, chargeDirection, -1f, enter + 1f);
                SpawnEffect(effects.edgeBreak, center + origin + worldDirection * enter, angle + 90f, 0.08f);
            }
            SpawnEffect(effects.chargeDust, center + origin, angle + 180f, 0.07f);
            SetMode(Mode.Charge);
        }

        /// <summary>Quita la espesura en una franja de <see cref="BreachWidth"/> a lo largo de la dirección (distancias de mundo desde el origen).</summary>
        void BreakLine(Vector2 originGround, Vector2 directionGround, float from, float to)
        {
            Vector2 origin = new(originGround.x, originGround.y * GroundSquash);
            Vector2 d = new Vector2(directionGround.x, directionGround.y * GroundSquash).normalized;
            ClearThicket(rel =>
            {
                Vector2 q = new Vector2(rel.x + 0.5f, rel.y + 0.5f) - origin;
                float along = Vector2.Dot(q, d);
                return along >= from && along <= to && Mathf.Abs(q.x * d.y - q.y * d.x) <= BreachWidth / 2f;
            });
        }

        /// <summary>Chevrones por la línea de embestida dentro del claro (arte Fx_Arthropleura_ChargeAim).</summary>
        void UpdateAim(Vector2 head)
        {
            if (effects.chargeAim == null || effects.chargeAim.Length == 0)
                return;
            Vector2 origin = new(head.x, head.y * GroundSquash);
            Vector2 d = new Vector2(chargeDirection.x, chargeDirection.y * GroundSquash).normalized;
            if (!RayThroughClearing(origin, d, out float enter, out float exit))
            {
                HideAim();
                return;
            }
            if (aimRoot == null)
                aimRoot = new GameObject("Aim").transform;
            int count = Mathf.CeilToInt(exit - enter);
            while (aimMarks.Count < count)
            {
                var mark = new GameObject("Mark").AddComponent<SpriteRenderer>();
                mark.transform.SetParent(aimRoot, false);
                mark.sortingOrder = 50;
                aimMarks.Add(mark);
            }
            int frame = Mathf.FloorToInt(Time.time / 0.08f);
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (int i = 0; i < aimMarks.Count; i++)
            {
                bool on = i < count;
                aimMarks[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                aimMarks[i].sprite = effects.chargeAim[(frame + i) % effects.chargeAim.Length];
                aimMarks[i].transform.SetPositionAndRotation(center + origin + d * (enter + i + 0.5f), Quaternion.Euler(0f, 0f, angle));
            }
        }

        void HideAim()
        {
            foreach (var mark in aimMarks)
                mark.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (aimRoot != null)
                Destroy(aimRoot.gameObject);
        }

        /// <summary>Para pruebas: saltarse la espera de rodear.</summary>
        internal void TestStartCharge()
        {
            if (CurrentMode == Mode.Circling)
                StartWarn();
        }
    }
}
