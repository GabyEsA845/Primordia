using Primordia.Audio;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso E1 (último 10 %, 60 PV): el cuerpo se enrolla en una rueda de ~8 u. Tras un aviso sale rodando hacia el
    /// jugador (2 de daño si lo toca); al chocar contra una pared pierde 15 PV y queda aturdida; un árbol en pie cae y cuenta como choque
    /// y los troncos tumbados la hacen rebotar. Una llama del suelo bajo la rueda también le quita vida. Con 0 muere (<c>Health.Kill</c>).
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria).
        public const int WheelHitDamage = 10, WheelContactDamage = 2;
        const float Transition4Seconds = 1.8f, WheelWarnSeconds = 1f, WheelRollMaxSeconds = 6f, WheelStunSeconds = 2.5f;
        const float WheelSpeed = 16f, WheelRadius = 3.5f, WheelFireRadius = 2.2f, WheelFireDps = 1.5f;
        // Cuánto sube el centro del dibujo sobre el punto de suelo (26 px de 16 por unidad).
        const float WheelLift = 26f / 16f;
        const float WheelFrameSeconds = 0.06f, WheelStunFrameSeconds = 0.2f;

        bool reachedPhase4, transition4Started, wheelActive, wheelContactHit;
        Vector2 wheelPos, wheelVelocity; // Mundo (posición del punto de suelo bajo la rueda).
        float wheelFire;
        SpriteRenderer wheelRenderer;
        public event System.Action ReachedPhase4;

        public bool Phase4Reached => reachedPhase4;
        public bool Transition4Started => transition4Started;
        public bool WheelActive => wheelActive;
        public Vector2 WheelPosition => wheelPos;

        void CheckPhase4(int current, int max)
        {
            if (reachedPhase4 || current > Mathf.RoundToInt(max * Thresholds[2])) return;
            reachedPhase4 = true;
            ReachedPhase4?.Invoke();
        }

        void BeginTransition4()
        {
            transition4Started = true;
            HideAim();
            ClearWaves();
            HideGap();
            SetMode(Mode.Phase4Pending);
            playerHealth?.MakeInvulnerable(Transition4Seconds + 0.5f);
            ScreenShake.Pulse(5f, 1f);
            // La cabeza se queda donde estaba y el cuerpo entero se vuelve rueda.
            Vector2 start = GroundToWorld(headPos);
            wheelPos = ClampWheel(start);
            SetWheelVisible(true);
            SpawnEffect(effects.chargeDust, wheelPos, 0f, 0.07f);
        }

        void SetWheelVisible(bool visible)
        {
            wheelActive = visible;
            foreach (var piece in pieces)
                piece.renderer.gameObject.SetActive(!visible);
            if (!visible)
            {
                if (wheelRenderer != null) Destroy(wheelRenderer.gameObject);
                return;
            }
            if (wheelRenderer == null)
            {
                var obj = new GameObject("Wheel");
                obj.transform.SetParent(transform, false);
                wheelRenderer = obj.AddComponent<SpriteRenderer>();
                wheelRenderer.sortingOrder = FirstOrder * 4 + 20;
            }
            headPos = WorldToGround(wheelPos);
        }

        Vector2 ClampWheel(Vector2 world)
        {
            Vector2 rel = world - center;
            rel.x = Mathf.Clamp(rel.x, -ArenaWidth / 2f + WheelRadius, ArenaWidth / 2f - WheelRadius);
            rel.y = Mathf.Clamp(rel.y, -ArenaHeight / 2f + WheelRadius, ArenaHeight / 2f - WheelRadius);
            return center + rel;
        }

        void StepWheel(float dt)
        {
            headPos = WorldToGround(wheelPos);
            switch (CurrentMode)
            {
                case Mode.Phase4Pending:
                    if (modeTimer >= Transition4Seconds)
                        StartWheelWarn();
                    break;
                case Mode.WheelWarn:
                    chargeDirection = AimDirection(headPos);
                    UpdateAim(headPos);
                    if (modeTimer >= WheelWarnSeconds)
                        StartRoll();
                    break;
                case Mode.WheelRoll:
                    StepRoll(dt);
                    break;
                case Mode.WheelStun:
                    if (modeTimer >= WheelStunSeconds)
                        StartWheelWarn();
                    break;
            }
            StepWheelFire(dt);
            PaintWheel();
        }

        void StartWheelWarn()
        {
            chargeDirection = AimDirection(headPos);
            SetMode(Mode.WheelWarn);
        }

        void StartRoll()
        {
            HideAim();
            Vector2 world = new Vector2(chargeDirection.x, chargeDirection.y * GroundSquash).normalized;
            wheelVelocity = world * WheelSpeed;
            wheelContactHit = false;
            SpawnEffect(effects.chargeDust, wheelPos, Mathf.Atan2(world.y, world.x) * Mathf.Rad2Deg + 180f, 0.07f);
            SetMode(Mode.WheelRoll);
        }

        void StepRoll(float dt)
        {
            wheelPos += wheelVelocity * dt;
            bool hitWall = false;
            Vector2 rel = wheelPos - center;
            float limitX = ArenaWidth / 2f - WheelRadius, limitY = ArenaHeight / 2f - WheelRadius;
            if (Mathf.Abs(rel.x) > limitX || Mathf.Abs(rel.y) > limitY)
            {
                hitWall = true;
                wheelPos = ClampWheel(wheelPos);
            }
            BounceOffLogs();
            bool hitTree = TryFellTree(true);
            HitPlayerWithWheel();
            if (hitWall || hitTree)
                WheelCrash();
            else if (modeTimer >= WheelRollMaxSeconds)
                StartWheelWarn(); // Se cansa: apunta de nuevo sin chocar.
        }

        /// <summary>Un tronco tumbado la desvía: refleja la velocidad por el eje en que más se ha metido.</summary>
        void BounceOffLogs()
        {
            foreach (var tree in trees)
            {
                if (tree.state == TreeState.Standing || !tree.dealt)
                    continue;
                var rect = tree.Log;
                Vector2 nearest = new(Mathf.Clamp(wheelPos.x, rect.xMin, rect.xMax), Mathf.Clamp(wheelPos.y, rect.yMin, rect.yMax));
                Vector2 away = wheelPos - nearest;
                if (away.magnitude >= WheelRadius * 0.6f)
                    continue;
                if (Mathf.Abs(away.x) * rect.height > Mathf.Abs(away.y) * rect.width) wheelVelocity.x = Mathf.Abs(wheelVelocity.x) * Mathf.Sign(away.x == 0f ? 1f : away.x);
                else wheelVelocity.y = Mathf.Abs(wheelVelocity.y) * Mathf.Sign(away.y == 0f ? 1f : away.y);
                wheelPos = nearest + (away.sqrMagnitude < 0.0001f ? Vector2.up : away.normalized) * (WheelRadius * 0.6f + 0.05f);
            }
        }

        void HitPlayerWithWheel()
        {
            if (wheelContactHit || player == null || playerHealth == null || playerHealth.IsDead)
                return;
            if (Vector2.Distance(player.position, wheelPos + Vector2.up * 0.5f) > WheelRadius + 0.4f)
                return;
            wheelContactHit = true;
            playerHealth.TakeDamage(WheelContactDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
        }

        /// <summary>Choque contra una pared o un árbol: 10 PV y aturdida. Con 0 PV, muere.</summary>
        void WheelCrash()
        {
            ScreenShake.Pulse(5f, 0.4f);
            Sfx.Play(Sounds.BossArthropleuraRollCrash);
            SpawnEffect(effects.stomp, wheelPos, 0f, 0.07f);
            DrainWheel(WheelHitDamage);
            if (!Health.IsDead)
                SetMode(Mode.WheelStun);
        }

        /// <summary>Baja la vida de la rueda hasta 0; a 0 muere (la muerte la monta <see cref="BuildCorpse"/>).</summary>
        void DrainWheel(int amount)
        {
            Health.Drain(amount, allowZero: true);
            if (Health.Current <= 0 && !Health.IsDead)
            {
                Health.IsInvulnerable = false;
                Health.Kill();
            }
        }

        void StepWheelFire(float dt)
        {
            if (Health.IsDead) return;
            bool burning = false;
            foreach (var fire in GroundFire.All)
                if (fire != null && fire.IsBurning && Vector2.Distance(fire.transform.position, wheelPos) <= WheelFireRadius + 0.5f)
                {
                    burning = true;
                    break;
                }
            if (!burning)
                return;
            wheelFire += WheelFireDps * dt;
            int whole = Mathf.FloorToInt(wheelFire);
            if (whole <= 0) return;
            wheelFire -= whole;
            DrainWheel(whole);
        }

        void PaintWheel()
        {
            if (wheelRenderer == null || art == null)
                return;
            var sheet = art.Find("Arthropleura_Roll");
            if (sheet == null) return;
            Vector2 look = CurrentMode == Mode.WheelRoll ? wheelVelocity : new Vector2(chargeDirection.x, chargeDirection.y * GroundSquash);
            int row = DirectionalArt.RowFor(look.sqrMagnitude > 0.01f ? Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg : -90f, out bool flip);
            bool rolling = CurrentMode == Mode.WheelRoll;
            float frameTime = rolling ? WheelFrameSeconds : CurrentMode == Mode.WheelStun ? WheelStunFrameSeconds : WheelFrameSeconds * 2f;
            int frame = Mathf.FloorToInt(Time.time / frameTime) % sheet.frames;
            // Aturdida: se bambolea sin avanzar. Avisando: gira en el sitio, achicándose un poco.
            wheelRenderer.sprite = sheet.Get(row, frame);
            wheelRenderer.flipX = flip;
            float wobble = CurrentMode == Mode.WheelStun ? Mathf.Sin(Time.time * 30f) * 0.08f : 0f;
            float squash = CurrentMode == Mode.WheelWarn ? 1f - 0.12f * Mathf.Clamp01(modeTimer / WheelWarnSeconds) : 1f;
            wheelRenderer.transform.position = wheelPos + new Vector2(wobble, WheelLift);
            wheelRenderer.transform.localScale = new Vector3(squash, squash, 1f);
        }

        /// <summary>Contacto con la rueda ya se resuelve en <see cref="HitPlayerWithWheel"/>; el cuerpo en piezas no existe.</summary>
        internal void TestStartWheel()
        {
            if (!transition4Started) return;
            StartWheelWarn();
        }

        internal void TestCrashWheel() => WheelCrash();
    }
}
