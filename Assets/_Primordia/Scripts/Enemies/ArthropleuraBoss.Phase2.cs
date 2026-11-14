using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso C1 (F2, 70 % → 35 %): la espesura revienta en cadena con el jugador invulnerable, la sala queda abierta
    /// (60×33 u, cámara ×1) y la cabeza persigue al jugador con el cuerpo como pared. Alterna pisotones con 3 ondas en arco y
    /// embestidas largas (aviso, recta, sin espesura que romper).
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria).
        public const int WaveDamage = 1;
        const float TransitionSeconds = 3.2f, CollapseStart = 0.8f, CollapseSeconds = 1.4f, CollapseFxEvery = 28f;
        const float ChaseSpeed = 7f, ChaseTurn = 100f, ChaseSeconds = 5f;
        const float StompWarnSeconds = 0.9f, WaveGap = 0.3f, WaveSpeed = 11f, WaveMaxRadius = 20f;
        const float WaveArcDegrees = 140f, WaveWidth = 1.4f, WaveSegment = 0.9f, WaveFrameSeconds = 0.08f;
        const int WaveCount = 3;
        // La embestida de la F2 acaba tras recorrer esto (no hay espesura que la pare).
        const float Phase2ChargeDistance = 38f;

        sealed class Wave
        {
            public Vector2 origin;
            public float baseAngle, radius;
            public bool hit;
            public Transform root;
            public readonly List<SpriteRenderer> marks = new();
        }

        bool transitionStarted;
        List<Vector2Int> collapseCells;
        int collapseIndex, wavesFired, phase2Cycle;
        float chaseTimer, stompAngle, chargeTravel;
        readonly List<Wave> waves = new();

        public bool TransitionStarted => transitionStarted;
        public int ActiveWaves => waves.Count;
        public int WavesFired => wavesFired;

        /// <summary>Región por la que se mueve y apunta ahora: el claro en la F1 y toda la sala después.</summary>
        Vector2 RegionHalf => PhaseIndex >= 1 ? new Vector2(ArenaWidth / 2f, ArenaHeight / 2f) : ClearingHalf;

        void BeginTransition()
        {
            transitionStarted = true;
            HideAim();
            SetMode(Mode.Phase2Pending);
            ClearBlockers();
            collapseCells = null;
            collapseIndex = 0;
            playerHealth?.MakeInvulnerable(TransitionSeconds + 0.5f);
            ScreenShake.Pulse(5f, 1.2f);
            SpawnEffect(effects.tremor, GroundToWorld(headPos), 0f, 0.09f);
        }

        void StepTransition(float dt)
        {
            if (collapseCells == null && modeTimer >= CollapseStart)
            {
                // La pared cae en cadena desde donde está la cabeza hacia fuera.
                Vector2 from = new(headPos.x, headPos.y * GroundSquash);
                collapseCells = new List<Vector2Int>(thicket);
                collapseCells.Sort((a, b) => Vector2.Distance(a + Vector2.one * 0.5f, from).CompareTo(Vector2.Distance(b + Vector2.one * 0.5f, from)));
                collapseIndex = 0;
                ScreenShake.SetRumble(this, 1.5f);
            }
            if (collapseCells != null && collapseIndex < collapseCells.Count)
            {
                float progress = Mathf.Clamp01((modeTimer - CollapseStart) / CollapseSeconds);
                int target = Mathf.Min(collapseCells.Count, Mathf.CeilToInt(progress * collapseCells.Count));
                if (target > collapseIndex)
                {
                    var batch = new HashSet<Vector2Int>(collapseCells.GetRange(collapseIndex, target - collapseIndex));
                    ClearThicket(rel => batch.Contains(rel), block: false);
                    for (int i = collapseIndex; i < target; i += Mathf.RoundToInt(CollapseFxEvery))
                        SpawnEffect(effects.wallBreak, center + (Vector2)collapseCells[i] + Vector2.one * 0.5f, 0f, 0.08f);
                    collapseIndex = target;
                }
                if (collapseIndex >= collapseCells.Count)
                    ScreenShake.ClearRumble(this);
            }
            if (modeTimer >= TransitionSeconds)
            {
                ClearThicket(rel => true, block: false); // Por si quedó alguna celda.
                ScreenShake.ClearRumble(this);
                StartChase();
            }
        }

        void StartChase()
        {
            BuildPhase2Scenery();
            chaseTimer = 0f;
            SetMode(Mode.Chase);
        }

        void StepChase(float dt)
        {
            chaseTimer += dt;
            if (TryStartDouse())
                return;
            Vector2 target = player != null ? WorldToGround(player.position) : Vector2.zero;
            if (TryEatAt(ref target) && CurrentMode == Mode.Eat)
                return;
            Advance(AvoidTrees(target), ChaseSpeed, ChaseTurn, dt);
            if (chaseTimer < ChaseSeconds)
                return;
            if (PhaseIndex >= 2)
            {
                phase3Cycle++;
                if (phase3Cycle % 3 == 0) StartRing();
                else if (phase3Cycle % 3 == 1) StartStomp();
                else StartWarn();
                return;
            }
            // Alterna pisotón y embestida.
            phase2Cycle++;
            if (phase2Cycle % 2 == 0)
                StartWarn();
            else
                StartStomp();
        }

        void StartStomp()
        {
            SetMode(Mode.StompWarn);
            wavesFired = 0;
            Vector2 target = player != null ? WorldToGround(player.position) : headPos + Vector2.right;
            stompAngle = Mathf.Atan2(target.y - headPos.y, target.x - headPos.x);
            for (int k = 0; k <= 4; k += 2)
                SpawnEffect(effects.stomp, GroundToWorld(NodeGround(k)), 0f, 0.07f);
            ScreenShake.Pulse(2f, 0.4f);
        }

        void StepStomp()
        {
            if (CurrentMode == Mode.StompWarn)
            {
                FaceHead(new Vector2(Mathf.Cos(stompAngle), Mathf.Sin(stompAngle)));
                if (modeTimer >= StompWarnSeconds)
                {
                    SetMode(Mode.Stomp);
                    FireWave();
                }
                return;
            }
            // Mode.Stomp: 3 ondas con 0,3 s entre ellas; cuando se apaga la última vuelve a perseguir.
            if (wavesFired < WaveCount && modeTimer >= wavesFired * WaveGap)
                FireWave();
            if (wavesFired >= WaveCount && waves.Count == 0)
                StartChase();
        }

        void FireWave()
        {
            wavesFired++;
            var wave = new Wave { origin = headPos, baseAngle = stompAngle, radius = 1f, root = new GameObject("Wave").transform };
            wave.root.SetParent(transform, false);
            waves.Add(wave);
            SpawnEffect(effects.stomp, GroundToWorld(headPos), 0f, 0.07f);
            ScreenShake.Pulse(2.5f, 0.25f);
        }

        void StepWaves(float dt)
        {
            for (int w = waves.Count - 1; w >= 0; w--)
            {
                var wave = waves[w];
                wave.radius += WaveSpeed * dt;
                if (wave.radius > WaveMaxRadius)
                {
                    Destroy(wave.root.gameObject);
                    waves.RemoveAt(w);
                    continue;
                }
                DrawWave(wave);
                HitWithWave(wave);
            }
        }

        void DrawWave(Wave wave)
        {
            var frames = effects.shockwave;
            if (frames == null || frames.Length == 0)
                return;
            float half = WaveArcDegrees * 0.5f * Mathf.Deg2Rad;
            int count = Mathf.Max(3, Mathf.CeilToInt(2f * half * wave.radius / WaveSegment));
            while (wave.marks.Count < count)
            {
                var mark = new GameObject("Mark").AddComponent<SpriteRenderer>();
                mark.transform.SetParent(wave.root, false);
                mark.sortingOrder = 40;
                wave.marks.Add(mark);
            }
            int frame = Mathf.FloorToInt(Time.time / WaveFrameSeconds);
            float alpha = Mathf.Clamp01((WaveMaxRadius - wave.radius) / 6f);
            for (int i = 0; i < wave.marks.Count; i++)
            {
                var mark = wave.marks[i];
                bool on = i < count;
                mark.gameObject.SetActive(on);
                if (!on)
                    continue;
                float a = wave.baseAngle - half + 2f * half * i / (count - 1);
                Vector2 ground = wave.origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * wave.radius;
                // Tangente del arco vista en pantalla (y × 0,82).
                float tangent = Mathf.Atan2(Mathf.Cos(a) * GroundSquash, -Mathf.Sin(a)) * Mathf.Rad2Deg;
                mark.sprite = frames[(frame + i) % frames.Length];
                mark.color = new Color(1f, 1f, 1f, alpha);
                mark.transform.SetPositionAndRotation(GroundToWorld(ground), Quaternion.Euler(0f, 0f, tangent));
            }
        }

        void HitWithWave(Wave wave)
        {
            if (wave.hit || player == null || playerHealth == null || playerHealth.IsDead)
                return;
            if (LogBlocks(GroundToWorld(wave.origin), player.position))
                return;
            Vector2 offset = WorldToGround(player.position) - wave.origin;
            if (Mathf.Abs(offset.magnitude - wave.radius) > WaveWidth * 0.5f)
                return;
            float angle = Mathf.Atan2(offset.y, offset.x);
            if (Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, wave.baseAngle * Mathf.Rad2Deg)) > WaveArcDegrees * 0.5f)
                return;
            wave.hit = true;
            playerHealth.TakeDamage(WaveDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
        }

        void ClearWaves()
        {
            foreach (var wave in waves)
                if (wave.root != null)
                    Destroy(wave.root.gameObject);
            waves.Clear();
        }

        /// <summary>Para pruebas y teclas: salta a la F2 en el punto en que estaría tras la transición.</summary>
        internal void TestSkipTransition()
        {
            if (!transitionStarted)
                return;
            collapseCells ??= new List<Vector2Int>(thicket);
            ClearThicket(rel => true, block: false);
            ScreenShake.ClearRumble(this);
            StartChase();
        }

        internal void TestStartStomp() => StartStomp();

        /// <summary>Teclas de prueba de la F2: 0 = vida al 69 %, 1 = pisotón, 2 = saltar la transición.</summary>
        internal void DebugPhase2(int action)
        {
            if (action == 0)
                Health.Drain(Mathf.Max(0, Health.Current - Mathf.RoundToInt(Health.Max * 0.69f)));
            else if (action == 1 && CurrentMode == Mode.Chase)
                TestStartStomp();
            else if (action == 2)
                TestSkipTransition();
            else if (action == 3)
                Health.Drain(Mathf.Max(0, Health.Current - Mathf.RoundToInt(Health.Max * 0.34f)));
            else if (action == 4 && CurrentMode == Mode.Chase)
                TestStartRing();
            else if (action == 5)
                TestSkipTransition3();
        }
    }
}
