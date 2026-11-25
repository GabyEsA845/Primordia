using Primordia.Audio;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso D1 (F3, 35 % → 10 %): transición corta (jugador invulnerable, cámara ×1,5) y el cerco. El cuerpo se
    /// enrosca en espiral alrededor de donde estaba el jugador y se cierra dejando un hueco (marcado con flechas) por el que salir;
    /// luego se aprieta hacia dentro: quien siga dentro recibe el apretón (daño) y sale despedido. El anillo se deshace solo.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria).
        public const int SqueezeDamage = 2;
        const float Camera3Zoom = 1.5f;
        const float Transition3Seconds = 2.5f;
        // Radio del anillo en suelo: con 78 u de cuerpo, 13,2 deja un hueco de ~5 u.
        const float RingStartRadius = 17f, RingFinalRadius = 13.2f, RingContractRadius = 2.2f;
        const float RingFormSeconds = 4.5f, RingHoldSeconds = 3.5f, RingContractSeconds = 2.2f;
        const float RingSpeed = 12f, RingHoldSpeed = 7f, RingContractSpeed = 11f, RingTurn = 220f;
        const float SqueezeReach = 2.2f, ExpelDistance = 5f, ExpelInvulnerableSeconds = 1.2f;
        const float GapMarkSpacing = 1.1f;
        static readonly int GapMarkCount = 5;

        bool reachedPhase3, transition3Started;
        public event System.Action ReachedPhase3;
        Vector2 ringCenter;
        float ringAngle, ringRadius;
        bool squeezed, wasInside;
        int phase3Cycle;
        readonly List<SpriteRenderer> gapMarks = new();
        Transform gapRoot;

        public bool Phase3Reached => reachedPhase3;
        public bool Transition3Started => transition3Started;
        public bool Squeezed => squeezed;
        internal Vector2 RingCenter => ringCenter;
        internal float RingRadius => ringRadius;

        void CheckPhase3(int current, int max)
        {
            if (reachedPhase3 || current > Mathf.RoundToInt(max * Thresholds[1])) return;
            reachedPhase3 = true;
            ReachedPhase3?.Invoke();
        }

        void BeginTransition3()
        {
            transition3Started = true;
            HideAim();
            ClearWaves();
            HideGap();
            SetMode(Mode.Phase3Pending);
            playerHealth?.MakeInvulnerable(Transition3Seconds + 0.5f);
            ScreenShake.Pulse(5f, 1.2f);
            ScreenShake.SetRumble(this, 1.5f);
            SpawnEffect(effects.tremor, GroundToWorld(headPos), 0f, 0.09f);
        }

        void StepTransition3()
        {
            if (modeTimer < Transition3Seconds)
                return;
            ScreenShake.ClearRumble(this);
            StartChase();
        }

        void StartRing()
        {
            Vector2 at = player != null ? WorldToGround(player.position) : Vector2.zero;
            float limitX = ArenaWidth / 2f - RingStartRadius * 0.5f - 1f;
            float limitY = (ArenaHeight / 2f - 1f) / GroundSquash - RingFinalRadius * 0.5f;
            // El anillo cabe en la sala (el elipse achatado ×0,82) con el jugador cerca del centro.
            ringCenter = new Vector2(Mathf.Clamp(at.x, -limitX, limitX), Mathf.Clamp(at.y, -limitY * 0.4f, limitY * 0.4f));
            Vector2 from = headPos - ringCenter;
            ringAngle = Mathf.Atan2(from.y, from.x);
            ringRadius = RingStartRadius;
            squeezed = false;
            SetMode(Mode.RingForm);
        }

        Vector2 RingTarget(float angle, float radius) => ringCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        void StepRing(float dt)
        {
            switch (CurrentMode)
            {
                case Mode.RingForm:
                    ringRadius = Mathf.Lerp(RingStartRadius, RingFinalRadius, Mathf.Clamp01(modeTimer / RingFormSeconds));
                    ringAngle += RingSpeed / ringRadius * dt;
                    Advance(RingTarget(ringAngle + 0.25f, ringRadius), RingSpeed, RingTurn, dt);
                    if (modeTimer >= RingFormSeconds)
                        SetMode(Mode.RingHold);
                    break;
                case Mode.RingHold:
                    ringAngle += RingHoldSpeed / ringRadius * dt;
                    Advance(RingTarget(ringAngle + 0.25f, ringRadius), RingHoldSpeed, RingTurn, dt);
                    ShowGap();
                    if (modeTimer >= RingHoldSeconds)
                    {
                        HideGap();
                        wasInside = player != null && Vector2.Distance(WorldToGround(player.position), ringCenter) < ringRadius - 0.5f;
                        SetMode(Mode.RingContract);
                    }
                    break;
                case Mode.RingContract:
                    ringRadius = Mathf.Lerp(RingFinalRadius, RingContractRadius, Mathf.Clamp01(modeTimer / RingContractSeconds));
                    ringAngle += RingContractSpeed / ringRadius * dt;
                    Advance(RingTarget(ringAngle + 0.3f, ringRadius), RingContractSpeed, RingTurn * 1.5f, dt);
                    TrySqueeze();
                    // Si el cuerpo arde mucho el cerco se rompe: la F3 recompensa prender el anillo.
                    if (modeTimer >= RingContractSeconds || BurningPieces >= DouseMinBurning)
                        StartChase();
                    break;
            }
        }

        /// <summary>El anillo llega hasta el jugador que seguía dentro: daño y lo despide fuera del cerco.</summary>
        void TrySqueeze()
        {
            if (squeezed || !wasInside || player == null || playerHealth == null || playerHealth.IsDead)
                return;
            Vector2 offset = WorldToGround(player.position) - ringCenter;
            if (offset.magnitude < ringRadius - SqueezeReach)
                return;
            squeezed = true;
            Vector2 direction = offset.sqrMagnitude > 0.01f ? offset.normalized : Vector2.up;
            playerHealth.TakeDamage(SqueezeDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
            Sfx.Play(Sounds.BossArthropleuraSqueeze);
            playerHealth.MakeInvulnerable(ExpelInvulnerableSeconds);
            Vector2 landing = ringCenter + direction * (RingFinalRadius + ExpelDistance);
            landing.x = Mathf.Clamp(landing.x, -ArenaWidth / 2f + 2f, ArenaWidth / 2f - 2f);
            landing.y = Mathf.Clamp(landing.y, -(ArenaHeight / 2f - 2f) / GroundSquash, (ArenaHeight / 2f - 2f) / GroundSquash);
            Vector2 world = GroundToWorld(landing);
            Sfx.Play(Sounds.BossArthropleuraExpel);
            if (player.TryGetComponent<Rigidbody2D>(out var body)) body.position = world;
            player.position = world;
            ScreenShake.Pulse(4f, 0.3f);
        }

        /// <summary>Flechas en el hueco del anillo, apuntando hacia fuera: por ahí se sale.</summary>
        void ShowGap()
        {
            if (effects.chargeAim == null || effects.chargeAim.Length == 0)
                return;
            // El hueco queda entre la cola y la cabeza: su centro es el punto medio de ambas en el anillo.
            Vector2 head = NodeGround(0), tail = NodeGround(pieces.Count - 1);
            Vector2 mid = (head + tail) * 0.5f - ringCenter;
            if (mid.sqrMagnitude < 0.01f)
                mid = Vector2.up;
            Vector2 radial = mid.normalized;
            Vector2 gap = ringCenter + radial * ringRadius;
            if (gapRoot == null)
                gapRoot = new GameObject("Gap").transform;
            while (gapMarks.Count < GapMarkCount)
            {
                var mark = new GameObject("Mark").AddComponent<SpriteRenderer>();
                mark.transform.SetParent(gapRoot, false);
                mark.sortingOrder = 50;
                gapMarks.Add(mark);
            }
            int frame = Mathf.FloorToInt(Time.time / 0.08f);
            float angle = Mathf.Atan2(radial.y * GroundSquash, radial.x) * Mathf.Rad2Deg;
            for (int i = 0; i < gapMarks.Count; i++)
            {
                gapMarks[i].gameObject.SetActive(true);
                gapMarks[i].sprite = effects.chargeAim[(frame + i) % effects.chargeAim.Length];
                gapMarks[i].transform.SetPositionAndRotation(GroundToWorld(gap + radial * ((i - 1) * GapMarkSpacing)), Quaternion.Euler(0f, 0f, angle));
            }
        }

        void HideGap()
        {
            foreach (var mark in gapMarks)
                mark.gameObject.SetActive(false);
        }

        /// <summary>Para pruebas y teclas: tras la transición del 35 %.</summary>
        internal void TestStartRing() => StartRing();

        internal void TestSkipTransition3()
        {
            if (!transition3Started)
                return;
            ScreenShake.ClearRumble(this);
            StartChase();
        }
    }
}
