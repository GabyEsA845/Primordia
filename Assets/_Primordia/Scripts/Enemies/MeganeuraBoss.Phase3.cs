using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Fase 3 de la Meganeura (≤ 30 %; paso 7d): la tormenta. Entra con un chillido, oscurece, viento con ceniza y el primer trueno
    /// junto al jugador; luego llueve y salen charcos nuevos. Cada ciclo: sube mientras caen rayos sueltos marcados en el suelo
    /// (nunca cerca de una Sigillaria en pie), pica, y a ras de suelo carga el rayo grande que sigue al jugador. Si ese rayo cae
    /// cerca de ella queda electrocutada (cae, boca arriba, vulnerable). Una Sigillaria en pie atrae el rayo grande (un solo uso) y
    /// una charca golpeada se electrifica unos segundos (mata ninfas, daña, y aturde a la Meganeura si está en ella).
    /// </summary>
    public partial class MeganeuraBoss
    {
        // Rayo grande (sigue al jugador).
        const float BigChargeSeconds = 1.5f, BigFixedSeconds = 0.5f, BigMarkSpeed = 8f;
        const float BigDirectRadius = 1.3f;
        const int BigDamage = 2;
        // Corriente circular que sale del impacto.
        const float RingSeconds = 4f, RingFromRadius = 1.6f, RingToRadius = 2.6f, RingBand = 0.5f, RingShockRadius = 3.4f;
        // Rayos sueltos.
        const int LooseBoltCount = 4;
        const float LooseChargeSeconds = 1f, LooseRadius = 1.3f, LooseMinGap = 2.5f;
        const int LooseDamage = 1;
        // Charcas electrificadas.
        const float PuddleLiveSeconds = 5.5f, PuddlePadding = 0.6f, PuddleDamageEvery = 1f;
        const int PuddleDamage = 1;
        // Tormenta.
        const float StormDarkAlpha = 0.5f, ShockSeconds = 0.8f, ShockGroundedSeconds = 6f;
        /// <summary>Daño máximo (fracción de la vida total) que admite en un aturdimiento: llegado a él se levanta y vuelve a ser invulnerable (≥ 3 aturdimientos).</summary>
        const float StunBudgetFraction = 0.10f;
        static readonly Vector2[] GrowOffsets = { new(-9f, -5f), new(9f, 8f), new(11f, -1f) };
        // Conjuntos de charca electrificada (Round, Kidney, Lobed, Splash) y radio de cada una.
        static readonly float[] PuddleRadii = { 1.3f, 1.6f, 1.6f, 1.5f };
        static readonly int[] GrowElectricSet = { 0, 1, 3 };

        sealed class PuddleInfo
        {
            public Vector2 position;
            public int electricSet;
            public bool live;
        }

        readonly List<PuddleInfo> puddles = new();
        readonly List<Coroutine> stormRoutines = new();
        bool stormStarted;
        MeganeuraStorm storm;
        bool stunWindow;
        bool stunCut;
        int stunStartHealth;

        /// <summary>Daño que admite un aturdimiento.</summary>
        int StunBudget => Mathf.Max(1, Mathf.CeilToInt(Health.Max * StunBudgetFraction));

        /// <summary>Fase 3: ventana abierta (único momento en que recibe daño).</summary>
        public bool StunWindowOpen => stunWindow;

        public bool StormStarted => stormStarted;
        internal MeganeuraStorm Storm => storm;

        /// <summary>En el aire o en una fase en que no se le puede aturdir con un rayo.</summary>
        bool CanBeShocked => CurrentMode is Mode.Hover or Mode.Stuck or Mode.Gust;

        void RegisterPuddle(Vector2 position, int electricSet) => puddles.Add(new PuddleInfo { position = position, electricSet = electricSet });

        void StartStorm(IEnumerator routine) => stormRoutines.Add(StartCoroutine(routine));

        void StopStormRoutines()
        {
            foreach (var routine in stormRoutines)
            {
                if (routine != null)
                    StopCoroutine(routine);
            }
            stormRoutines.Clear();
        }

        // ---------- Ciclo ----------

        IEnumerator PhaseThreeCycle()
        {
            if (!stormStarted)
                yield return StormEntry();
            yield return TakeOff();
            // Mientras sube y persigue caen rayos sueltos; tras el picado carga el rayo grande (la ventana en que hay que llevarlo hasta ella).
            StartStorm(LooseBoltVolley());
            yield return Chase(2.2f);
            yield return Dive();
            StartStorm(BigBoltRoutine());
            yield return LowWindow(BigChargeSeconds + BigFixedSeconds + 1.2f);
        }

        // ---------- Entrada ----------

        IEnumerator StormEntry()
        {
            stormStarted = true;
            // Los huevos que queden se van; las ninfas siguen.
            foreach (var egg in new List<MeganeuraEgg>(MeganeuraEgg.Alive))
                egg.Vanish();
            EnsureStorm();

            CurrentMode = Mode.Climbing;
            SetVulnerable(false);
            ShowBody(true);
            SetShadowAlpha(ShadowAlphaGround);
            SetAngle(-90f);
            PlayBody("Screech", 90f, loop: false, restart: true);
            storm.Darken(StormDarkAlpha, 1.6f);
            storm.SetAsh(true);
            GrowPuddles();
            float from = Altitude;
            for (float t = 0f; t < 0.4f; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(from, 5f, t / 0.4f);
                yield return null;
            }
            SpriteFramePlayer.PlayOnce(effects.screech, 0.16f, bodyRenderer.transform.position, 26);
            yield return Wait(2f);

            // El viento amaina, empieza a llover y cae el primer trueno, junto al jugador.
            storm.SetAsh(false);
            storm.SetRain(true);
            yield return Wait(0.4f);
            FindPlayer();
            var thunder = ThunderSpot();
            SpriteFramePlayer.PlayOnce(effects.boltHuge, 0.05f, thunder, 28);
            SpriteFramePlayer.PlayOnce(effects.boltBigImpact, 0.06f, thunder, 28);
            SpriteFramePlayer.PlayOnce(effects.boltScorch, 0.5f, thunder, 2);
            storm.Flash(0.85f);
            ScreenShake.Pulse(6f, 0.35f);
            yield return Wait(0.9f);
        }

        void EnsureStorm()
        {
            if (storm == null)
                storm = MeganeuraStorm.Create(arenaCenter != null ? arenaCenter : transform.parent, ArenaCenter, arenaSize, effects.rain, effects.ash);
        }

        /// <summary>Un punto a ≈ 4 u del jugador, dentro del suelo y lejos de las Sigillarias en pie (el trueno no las rompe).</summary>
        Vector2 ThunderSpot()
        {
            Vector2 origin = playerTarget != null ? (Vector2)playerTarget.position : ArenaCenter;
            for (int i = 0; i < 16; i++)
            {
                var candidate = ClampToFloor(origin + Random.insideUnitCircle.normalized * 4f, 1f);
                if (BossSigillaria.IsSafeSpot(candidate))
                    return candidate;
            }
            return ClampToFloor(origin, 1f);
        }

        /// <summary>Charcos nuevos que crecen con la lluvia (el último cuadro = la charca entera).</summary>
        void GrowPuddles()
        {
            for (int i = 0; i < GrowOffsets.Length && i < effects.puddleGrow.Length; i++)
            {
                var frames = effects.puddleGrow[i].frames;
                if (frames.Length == 0)
                    continue;
                var position = ArenaCenter + GrowOffsets[i];
                var puddle = new GameObject("MeganeuraPuddleGrow");
                puddle.transform.SetParent(SummonParent, true);
                puddle.transform.position = position;
                var renderer = puddle.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = 1;
                RegisterPuddle(position, GrowElectricSet[i]);
                StartCoroutine(GrowRoutine(renderer, frames));
            }
        }

        static IEnumerator GrowRoutine(SpriteRenderer renderer, Sprite[] frames)
        {
            for (int i = 0; i < frames.Length && renderer != null; i++)
            {
                renderer.sprite = frames[i];
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ---------- Rayos sueltos ----------

        /// <summary>Puntos de impacto de los rayos sueltos: alrededor del jugador, dentro del suelo, separados y a salvo de las Sigillarias.</summary>
        internal List<Vector2> PickLooseSpots(Vector2 around, int count)
        {
            var spots = new List<Vector2>();
            for (int attempt = 0; attempt < count * 12 && spots.Count < count; attempt++)
            {
                var candidate = ClampToFloor(around + Random.insideUnitCircle.normalized * Random.Range(2.5f, 7f), 1f);
                if (!BossSigillaria.IsSafeSpot(candidate))
                    continue;
                bool apart = true;
                foreach (var other in spots)
                    apart &= Vector2.Distance(other, candidate) >= LooseMinGap;
                if (apart)
                    spots.Add(candidate);
            }
            return spots;
        }

        IEnumerator LooseBoltVolley()
        {
            FindPlayer();
            var spots = PickLooseSpots(playerTarget != null ? (Vector2)playerTarget.position : ArenaCenter, LooseBoltCount);
            var marks = new List<GameObject>();
            foreach (var spot in spots)
            {
                var mark = new GameObject("LooseBoltMark");
                mark.transform.position = spot;
                var renderer = mark.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 26;
                int frames = Mathf.Max(1, effects.boltSmallMark.Length);
                mark.AddComponent<SpriteFramePlayer>().Play(renderer, effects.boltSmallMark, LooseChargeSeconds / frames, newLoop: false);
                temporary.Add(mark);
                marks.Add(mark);
            }
            yield return Wait(LooseChargeSeconds);
            foreach (var mark in marks)
                Destroy(mark);
            foreach (var spot in spots)
                StrikeSmall(spot);
        }

        void StrikeSmall(Vector2 spot)
        {
            SpriteFramePlayer.PlayOnce(effects.boltSmall, 0.05f, spot, 28);
            SpriteFramePlayer.PlayOnce(effects.boltSmallImpact, 0.06f, spot, 28);
            SpriteFramePlayer.PlayOnce(effects.boltScorch, 0.5f, spot, 2);
            storm?.Flash(0.3f);
            ScreenShake.Pulse(2f, 0.12f);
            DamagePlayerNear(spot, LooseRadius, LooseDamage);
            TouchPuddles(spot);
        }

        // ---------- Rayo grande ----------

        IEnumerator BigBoltRoutine()
        {
            FindPlayer();
            if (playerTarget == null)
                yield break;
            Vector2 position = playerTarget.position;
            var mark = new GameObject("BigBoltMark");
            mark.transform.position = position;
            var renderer = mark.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 26;
            temporary.Add(mark);
            var frames = effects.boltBigMark;
            int charge = Mathf.Max(1, frames.Length - 2);
            // Cuadros 0-7: la carga, sigue al jugador. Cuadros 8-9: fijo un instante (blanco que late) antes de caer.
            for (float t = 0f; t < BigChargeSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                if (Playing)
                    position = Vector2.MoveTowards(position, playerTarget.position, BigMarkSpeed * Time.deltaTime);
                mark.transform.position = position;
                if (frames.Length > 0)
                    renderer.sprite = frames[Mathf.Min(charge - 1, Mathf.FloorToInt(t / BigChargeSeconds * charge))];
                yield return null;
            }
            for (float t = 0f; t < BigFixedSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                if (frames.Length > charge)
                    renderer.sprite = frames[charge + Mathf.FloorToInt(t / 0.08f) % (frames.Length - charge)];
                yield return null;
            }
            temporary.Remove(mark);
            Destroy(mark);
            StrikeBig(position);
        }

        /// <summary>Cae el rayo grande: una Sigillaria en pie cerca lo atrae hacia sí; la corriente circular sale del impacto.</summary>
        internal void StrikeBig(Vector2 position)
        {
            var rod = BossSigillaria.Nearest(position, BossSigillaria.AttractRadius);
            Vector2 target = position;
            if (rod != null)
            {
                target = rod.Position;
                rod.Strike(effects);
            }
            SpriteFramePlayer.PlayOnce(effects.boltBig, 0.05f, target, 28);
            SpriteFramePlayer.PlayOnce(effects.boltBigImpact, 0.06f, target, 28);
            SpriteFramePlayer.PlayOnce(effects.boltScorch, 0.5f, target, 2);
            storm?.Flash(0.6f);
            ScreenShake.Pulse(4f, 0.2f);
            DamagePlayerNear(target, BigDirectRadius, BigDamage);
            TouchPuddles(target);
            StartCoroutine(RingRoutine(target));
            if (CanBeShocked && Vector2.Distance(transform.position, target) <= RingShockRadius)
                TriggerShock();
        }

        /// <summary>La corriente circular (≈ 4 s): se expande de 1,6 a 2,6 u y daña una vez al jugador que la cruza.</summary>
        IEnumerator RingRoutine(Vector2 center)
        {
            SpriteFramePlayer.PlayOnce(effects.boltRing, RingSeconds / Mathf.Max(1, effects.boltRing.Length), center, 27);
            bool hit = false;
            for (float t = 0f; t < RingSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                float radius = Mathf.Lerp(RingFromRadius, RingToRadius, t / RingSeconds);
                FindPlayer();
                if (!hit && playerTarget != null && Mathf.Abs(Vector2.Distance(playerTarget.position, center) - radius) <= RingBand
                    && playerTarget.TryGetComponent<Health>(out var health))
                {
                    hit = true;
                    health.TakeDamage(1);
                }
                yield return null;
            }
        }

        // ---------- Charcas electrificadas ----------

        void TouchPuddles(Vector2 point)
        {
            foreach (var puddle in puddles)
            {
                if (!puddle.live && Vector2.Distance(puddle.position, point) <= PuddleRadii[puddle.electricSet] + PuddlePadding)
                    StartCoroutine(PuddleLive(puddle));
            }
        }

        IEnumerator PuddleLive(PuddleInfo puddle)
        {
            puddle.live = true;
            float radius = PuddleRadii[puddle.electricSet];
            GameObject overlay = null;
            if (puddle.electricSet < effects.puddleElectric.Length && effects.puddleElectric[puddle.electricSet].frames.Length > 0)
            {
                overlay = new GameObject("MeganeuraPuddleElectric");
                overlay.transform.position = puddle.position;
                var renderer = overlay.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 27;
                overlay.AddComponent<SpriteFramePlayer>().Play(renderer, effects.puddleElectric[puddle.electricSet].frames, 0.07f, newLoop: true);
            }
            float nextHurt = 0f;
            for (float t = 0f; t < PuddleLiveSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                foreach (var nymph in FindObjectsByType<MeganeuraNymph>())
                {
                    if (Vector2.Distance(nymph.transform.position, puddle.position) <= radius && nymph.TryGetComponent<Health>(out var nymphHealth)
                        && !nymphHealth.IsDead)
                        nymphHealth.TakeDamage(9999);
                }
                FindPlayer();
                if (t >= nextHurt && playerTarget != null && Vector2.Distance(playerTarget.position, puddle.position) <= radius
                    && playerTarget.TryGetComponent<Health>(out var health))
                {
                    health.TakeDamage(PuddleDamage);
                    nextHurt = t + PuddleDamageEvery;
                }
                if (CanBeShocked && Vector2.Distance(transform.position, puddle.position) <= radius + 0.8f)
                    TriggerShock();
                yield return null;
            }
            if (overlay != null)
                Destroy(overlay);
            puddle.live = false;
        }

        // ---------- Electrocutada ----------

        /// <summary>Alcanzada por la corriente: destellos, cae y queda boca arriba (vulnerable); luego se endereza y vuelve a subir.</summary>
        internal void TriggerShock()
        {
            StopFightRoutine();
            fight = StartCoroutine(ShockRoutine());
        }

        IEnumerator ShockRoutine()
        {
            CurrentMode = Mode.Shocked;
            stunWindow = true;
            stunCut = false;
            stunStartHealth = Health.Current;
            SetVulnerable(true);
            ShowBody(true);
            SetShadowAlpha(ShadowAlphaGround);
            PlayBody("Shocked", 60f, loop: true, restart: true);
            storm?.Flash(0.4f);
            yield return WaitStun(ShockSeconds);
            yield return FallRoutine(burning: false, ShockGroundedSeconds);
        }

        /// <summary>Espera el aturdimiento, que se acaba antes si llegó a su tope de daño.</summary>
        IEnumerator WaitStun(float seconds)
        {
            for (float t = 0f; t < seconds && !stunCut; t += Playing ? Time.deltaTime : 0f)
                yield return null;
        }

        // ---------- Utilidades ----------

        Vector2 ClampToFloor(Vector2 position, float margin)
        {
            Vector2 half = new(arenaSize.x * 0.5f - 2.5f - margin, arenaSize.y * 0.5f - 3.5f - margin);
            Vector2 center = ArenaCenter;
            position.x = Mathf.Clamp(position.x, center.x - half.x, center.x + half.x);
            position.y = Mathf.Clamp(position.y, center.y - half.y, center.y + half.y);
            return position;
        }

        void DamagePlayerNear(Vector2 center, float radius, int damage)
        {
            FindPlayer();
            if (playerTarget != null && Vector2.Distance(playerTarget.position, center) <= radius && playerTarget.TryGetComponent<Health>(out var health))
                health.TakeDamage(damage);
        }
    }
}
