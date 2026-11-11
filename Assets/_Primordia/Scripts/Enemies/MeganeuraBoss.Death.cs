using System.Collections;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MeganeuraBoss
    {
        // Valores provisionales de la puesta en escena de muerte.
        internal const float DeathDuration = 2.2f;
        internal const float RiseBeforeFall = 0.6f;
        internal const float SpiralRadius = 1.5f;
        const float CalmSeconds = 3f;

        [System.Serializable]
        public class DeathEffects
        {
            public Sprite[] splash = System.Array.Empty<Sprite>();
            public Sprite[] ripple = System.Array.Empty<Sprite>();
            public Sprite[] drizzle = System.Array.Empty<Sprite>();
            public Sprite[] steam = System.Array.Empty<Sprite>();
        }

        [SerializeField] DeathEffects deathEffects = new();

        internal void ConfigureDeath(DeathEffects value) => deathEffects = value;

        void CreateDeathPresentation()
        {
            Vector2 start = transform.position;
            Vector2 target = ArenaCenter;
            float nearest = float.PositiveInfinity;
            // La lista incluye tanto las charcas iniciales como las que crecen en la fase 3.
            foreach (var puddle in puddles)
            {
                float distance = (puddle.position - start).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    target = puddle.position;
                }
            }

            ClearDeathHazards();
            foreach (var puddle in puddles)
                puddle.live = false;
            storm?.Calm(CalmSeconds, deathEffects.drizzle, deathEffects.steam);

            var visual = new GameObject("MeganeuraCorpse");
            visual.transform.SetParent(arenaCenter != null ? arenaCenter : transform.parent, true);
            visual.transform.position = start;
            float height = Altitude;
            // Enemy sigue emitiendo Died y destruyéndose como antes. Las piezas y su corrutina pertenecen a la sala.
            if (bodyRenderer != null)
                bodyRenderer.transform.SetParent(visual.transform, true);
            if (shadowRenderer != null)
                shadowRenderer.transform.SetParent(visual.transform, true);
            visual.AddComponent<MeganeuraCorpse>().Configure(bodyRenderer, bodySprite, shadowRenderer, shadowSprite,
                art, deathEffects, height, HighAltitude, target, Burnt);
        }

        void ClearDeathHazards()
        {
            foreach (var player in FindObjectsByType<SpriteFramePlayer>())
            {
                if (!player.TryGetComponent<SpriteRenderer>(out var renderer))
                    continue;
                Vector2 offset = (Vector2)player.transform.position - ArenaCenter;
                if (Mathf.Abs(offset.x) > arenaSize.x || Mathf.Abs(offset.y) > arenaSize.y)
                    continue;
                bool hazard = Contains(effects.boltBigMark, renderer.sprite) || Contains(effects.boltSmallMark, renderer.sprite)
                    || Contains(effects.boltBig, renderer.sprite) || Contains(effects.boltSmall, renderer.sprite)
                    || Contains(effects.boltHuge, renderer.sprite) || Contains(effects.boltRing, renderer.sprite)
                    || Contains(effects.boltBigImpact, renderer.sprite) || Contains(effects.boltSmallImpact, renderer.sprite);
                foreach (var set in effects.puddleElectric)
                    hazard |= Contains(set.frames, renderer.sprite);
                if (hazard)
                    Destroy(player.gameObject);
            }
        }

        static bool Contains(Sprite[] sprites, Sprite sprite) => sprite != null && System.Array.IndexOf(sprites, sprite) >= 0;
    }

    /// <summary>Presentación independiente: sobrevive a Enemy y se descarta junto a la sala.</summary>
    internal sealed class MeganeuraCorpse : MonoBehaviour
    {
        // Ritmos del arte y valores provisionales de colocación.
        const float WingFrameMs = 70f, FallFrameMs = 90f, RippleFrameSeconds = 0.2f, SplashFrameSeconds = 0.09f;
        const float SideAngle = 0f, SpiralTurns = 2f;
        const int FloatingOrder = 4, RippleOrder = 3, SplashOrder = 7;

        SpriteRenderer body, shadow;
        DirectionalSprite bodyAnimation, shadowAnimation;
        DirectionalArt art;
        MeganeuraBoss.DeathEffects effects;
        float altitude, flightHeight;
        Vector2 impact;
        bool burnt;

        internal bool Landed { get; private set; }
        internal float Altitude => altitude;
        internal Vector2 Impact => impact;

        internal void Configure(SpriteRenderer newBody, DirectionalSprite newBodyAnimation, SpriteRenderer newShadow,
            DirectionalSprite newShadowAnimation, DirectionalArt newArt, MeganeuraBoss.DeathEffects newEffects,
            float height, float high, Vector2 target, bool isBurnt)
        {
            body = newBody;
            bodyAnimation = newBodyAnimation;
            shadow = newShadow;
            shadowAnimation = newShadowAnimation;
            art = newArt;
            effects = newEffects;
            altitude = height;
            flightHeight = high;
            impact = target;
            burnt = isBurnt;
            StartCoroutine(DeathRoutine());
        }

        string Sheet(string name) => "Meganeura_" + name + (burnt ? "_Burnt" : "");

        void SetHeight(float height)
        {
            altitude = height;
            if (body != null)
                body.transform.localPosition = Vector3.up * (height * MeganeuraBoss.AltitudeToY);
            if (shadow != null)
                shadow.transform.localPosition = Vector3.zero;
        }

        IEnumerator DeathRoutine()
        {
            if (body == null || bodyAnimation == null)
                yield break;
            body.color = Color.white;
            body.enabled = true;
            if (altitude < flightHeight)
            {
                float from = altitude;
                for (float t = 0f; t < MeganeuraBoss.RiseBeforeFall; t += Time.deltaTime)
                {
                    SetHeight(Mathf.Lerp(from, flightHeight, t / MeganeuraBoss.RiseBeforeFall));
                    yield return null;
                }
                SetHeight(flightHeight);
            }

            bodyAnimation.Play(Sheet("DeathFall"), FallFrameMs, false, restart: true);
            bodyAnimation.Angle = SideAngle;
            var wing = new GameObject("WingDetached");
            wing.transform.SetParent(transform, true);
            wing.transform.position = body.transform.position;
            var wingRenderer = wing.AddComponent<SpriteRenderer>();
            wingRenderer.sortingLayerID = body.sortingLayerID;
            wingRenderer.sortingOrder = body.sortingOrder;
            var wingAnimation = wing.AddComponent<DirectionalSprite>();
            wingAnimation.Configure(wingRenderer, art);
            wingAnimation.Play(Sheet("WingDetached"), WingFrameMs, false, restart: true);
            Vector2 wingStart = wing.transform.position;
            Vector2 start = transform.position;
            Vector2 wingEnd = start + Vector2.left * MeganeuraBoss.SpiralRadius;
            float wingSeconds = wingAnimation.Length;
            float fromHeight = altitude;
            if (shadow != null)
            {
                shadow.enabled = true;
                shadowAnimation?.Play("Meganeura_Shadow_Fly", 50f, true);
            }
            for (float t = 0f; t < MeganeuraBoss.DeathDuration; t += Time.deltaTime)
            {
                float k = t / MeganeuraBoss.DeathDuration;
                float angle = k * SpiralTurns * Mathf.PI * 2f;
                // Empieza exactamente donde murió y termina exactamente en el agua, sin saltos al entrar en la espiral.
                Vector2 orbit = new(Mathf.Cos(angle) - 1f, Mathf.Sin(angle));
                transform.position = Vector2.Lerp(start, impact, Mathf.SmoothStep(0f, 1f, k))
                    + orbit * (MeganeuraBoss.SpiralRadius * (1f - k));
                SetHeight(Mathf.Lerp(fromHeight, 0f, k));
                wing.transform.position = Vector2.Lerp(wingStart, wingEnd, Mathf.Clamp01(t / Mathf.Max(0.01f, wingSeconds)));
                if (t >= wingSeconds)
                    wingRenderer.sortingOrder = FloatingOrder;
                yield return null;
            }

            transform.position = impact;
            SetHeight(0f);
            wing.transform.position = wingEnd;
            wingRenderer.sortingOrder = FloatingOrder;
            if (shadow != null)
                shadow.enabled = false;
            body.sortingOrder = FloatingOrder;
            bodyAnimation.Play(Sheet("Dead"), FallFrameMs, false, restart: true);
            bodyAnimation.Angle = SideAngle;
            var splash = SpriteFramePlayer.PlayOnce(effects.splash, SplashFrameSeconds, impact, SplashOrder);
            if (splash != null)
                splash.transform.SetParent(transform, true);
            if (effects.ripple.Length > 0)
            {
                var ripple = new GameObject("DeadRipple");
                ripple.transform.SetParent(transform, false);
                var renderer = ripple.AddComponent<SpriteRenderer>();
                renderer.sortingLayerID = body.sortingLayerID;
                renderer.sortingOrder = RippleOrder;
                ripple.AddComponent<SpriteFramePlayer>().Play(renderer, effects.ripple, RippleFrameSeconds, true);
            }
            Landed = true;
        }
    }
}
