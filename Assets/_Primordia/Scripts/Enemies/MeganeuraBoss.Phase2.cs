using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Fase 2 de la Meganeura (65–30 %; paso 7c): pone 3-4 racimos de huevos en la orilla de las charcas (aterriza, abdomen al agua) y
    /// sigue picando mientras queden. Cada racimo eclosiona a los 10 s y suelta una ninfa que persigue y lanza el labio; el jugador
    /// puede romperlo antes. Cuando no queda ninguno, baja furiosa: chillido con ondas y posada furiosa junto a la charca, que es su
    /// ventana de daño. Luego vuelve a subir y repite.
    /// </summary>
    public partial class MeganeuraBoss
    {
        const float EggHatchSeconds = 10f;
        const float RageSeconds = 6f;
        const float ScreechRadius = 3.5f;
        const int ScreechDamage = 1;
        const float FlySpeed = 12f;
        static readonly Vector2[] PuddleOffsets = { new(-10f, 2f), new(3f, -6f), new(-2f, 7f) };

        readonly List<Vector2> puddleSpots = new();

        /// <summary>0 = fase 1; 1 = fase 2 (≤ 65 %); 2 = fase 3 (≤ 30 %).</summary>
        public int PhaseIndex
        {
            get
            {
                float fraction = Health != null && Health.Max > 0 ? (float)Health.Current / Health.Max : 1f;
                return fraction <= torn2Threshold ? 2 : fraction <= torn1Threshold ? 1 : 0;
            }
        }

        /// <summary>Racimos de huevos que aún no han eclosionado ni se han roto.</summary>
        public int EggsAlive => MeganeuraEgg.Alive.Count;

        // Para las pruebas: las imágenes y la ninfa con las que sale un racimo.
        internal Effects EffectsInUse => effects;
        internal Enemy NymphInUse => nymphPrefab;

        Transform SummonParent => transform.parent != null ? transform.parent : arenaCenter;

        IEnumerator PhaseTwoCycle()
        {
            EnsurePuddles();
            int clusters = Random.Range(3, 5);
            for (int i = 0; i < clusters; i++)
            {
                if (!IsHigh)
                    yield return TakeOff();
                var spot = puddleSpots.Count > 0 ? puddleSpots[i % puddleSpots.Count] : (Vector2)transform.position;
                yield return FlyTo(spot);
                yield return LayEggs(spot);
            }

            // Mientras queden huevos sigue picando (un picado por vuelta).
            while (EggsAlive > 0)
            {
                if (!IsHigh)
                    yield return TakeOff();
                yield return Chase(0.8f);
                yield return Dive();
                yield return LowWindow(0.9f);
            }
            yield return Rage();
        }

        void EnsurePuddles()
        {
            if (puddleSpots.Count > 0)
                return;
            foreach (var offset in PuddleOffsets)
                puddleSpots.Add(ArenaCenter + offset);
            for (int i = 0; i < puddleSpots.Count && effects.puddles.Length > 0; i++)
            {
                var puddle = new GameObject("MeganeuraPuddle");
                puddle.transform.SetParent(SummonParent, true);
                puddle.transform.position = puddleSpots[i];
                var renderer = puddle.AddComponent<SpriteRenderer>();
                renderer.sprite = effects.puddles[i % effects.puddles.Length];
                renderer.sortingOrder = 1;
            }
        }

        IEnumerator FlyTo(Vector2 spot)
        {
            CurrentMode = Mode.High;
            if (shadowSprite != null)
                shadowSprite.Play("Meganeura_Shadow_Fly", FlyMs, true);
            for (float t = 0f; t < 3f && Vector2.Distance(transform.position, spot) > 0.3f; t += Playing ? Time.deltaTime : 0f)
            {
                if (Playing)
                    MoveTowards(spot, FlySpeed);
                yield return null;
            }
        }

        /// <summary>Aterriza a la orilla, baja el abdomen al agua (Lay) y deja un racimo en el cuadro 3; vulnerable mientras tanto.</summary>
        IEnumerator LayEggs(Vector2 spot)
        {
            CurrentMode = Mode.Hover;
            MoveTo(spot);
            ShowBody(true);
            SetShadowAlpha(ShadowAlphaGround);
            SetAngle(-90f);
            SetVulnerable(true);
            PlayBody("Land", LandMs, loop: false, restart: true);
            for (float t = 0f; t < 0.27f; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(3f, 0.8f, t / 0.27f);
                yield return null;
            }
            PlayBody("Lay", 120f, loop: false, restart: true);
            yield return Wait(0.36f);
            var at = (Vector2)transform.position + new Vector2(Random.Range(-1.4f, 1.4f), -1.2f);
            MeganeuraEgg.Spawn(SummonParent, at, effects, nymphPrefab, EggHatchSeconds);
            yield return Wait(0.36f);
        }

        /// <summary>Baja furiosa: chillido con ondas (empuja y hiere si estás cerca) y posada furiosa, su ventana de daño.</summary>
        IEnumerator Rage()
        {
            CurrentMode = Mode.Rage;
            FindPlayer();
            MoveTo(puddleSpots.Count > 0 ? puddleSpots[0] + Vector2.up * 2f : (Vector2)transform.position);
            ShowBody(true);
            SetShadowAlpha(ShadowAlphaGround);
            SetAngle(-90f);
            SetVulnerable(true);
            PlayBody("Screech", 90f, loop: false, restart: true);
            for (float t = 0f; t < 0.4f; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(6f, 1.2f, t / 0.4f);
                yield return null;
            }
            // Las ondas salen en el cuadro 2 de la hoja.
            SpriteFramePlayer.PlayOnce(effects.screech, 0.16f, bodyRenderer.transform.position, 26);
            if (playerTarget != null && Vector2.Distance(playerTarget.position, transform.position) <= ScreechRadius
                && playerTarget.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(ScreechDamage);
                if (playerTarget.TryGetComponent<Player.PlayerController>(out var controller))
                    controller.Shove(((Vector2)playerTarget.position - (Vector2)transform.position).normalized * 8f, 0.3f);
            }
            yield return Wait(0.2f);
            PlayBody("PerchRage", 120f, loop: true, restart: true);
            yield return Wait(RageSeconds);
        }

        /// <summary>Al morir la madre, sus huevos y ninfas se van con ella.</summary>
        void ClearSummons()
        {
            foreach (var egg in new List<MeganeuraEgg>(MeganeuraEgg.Alive))
                egg.Vanish();
            foreach (var nymph in FindObjectsByType<MeganeuraNymph>())
            {
                if (nymph.TryGetComponent<Health>(out var health) && !health.IsDead)
                    health.TakeDamage(9999);
            }
        }
    }
}
