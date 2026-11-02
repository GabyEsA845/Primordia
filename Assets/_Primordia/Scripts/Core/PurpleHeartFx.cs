using Primordia.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Core
{
    /// <summary>
    /// Hace notorio el corazón morado: mientras se lleva, un aura morada late alrededor del jugador; al parar un golpe, un
    /// destello morado en pantalla, una onda que se expande, una sacudida corta y el sonido más grave y más fuerte.
    /// PROVISIONAL, todo dibujado por código; el arte definitivo del aura y la onda, en la tanda de arte.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PurpleHeartFx : MonoBehaviour
    {
        static readonly Color Purple = new(0.72f, 0.38f, 1f);
        const float FlashSeconds = 0.35f;
        const float RingSeconds = 0.45f;
        const float RingRadius = 3.2f;

        static Sprite glowSprite;
        static Sprite ringSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            glowSprite = null;
            ringSprite = null;
        }

        Health health;
        SpriteRenderer aura;
        Image flash;
        float flashStart = -10f;
        SpriteRenderer ring;
        float ringStart = -10f;

        void Awake()
        {
            health = GetComponent<Health>();
            aura = CreateSprite("PurpleAura", GlowSprite(), new Color(Purple.r, Purple.g, Purple.b, 0.3f), 3);
            aura.transform.localScale = Vector3.one * 2.2f;
            ring = CreateSprite("PurpleRing", RingSprite(), Purple, 6);
            ring.enabled = false;
        }

        void OnEnable()
        {
            health.PurpleBlocked += OnBlocked;
            health.Changed += OnChanged;
            OnChanged(health.Current, health.Max);
        }

        void OnDisable()
        {
            health.PurpleBlocked -= OnBlocked;
            health.Changed -= OnChanged;
        }

        void OnDestroy()
        {
            if (flash != null)
                Destroy(flash.canvas != null ? flash.canvas.gameObject : flash.gameObject);
        }

        void OnChanged(int current, int max) => aura.enabled = health.PurpleHearts > 0;

        SpriteRenderer CreateSprite(string objectName, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        void OnBlocked()
        {
            flashStart = Time.unscaledTime;
            ringStart = Time.unscaledTime;
            ScreenShake.Pulse(3f, 0.25f);
            Sfx.Play(Sounds.PurpleHeartBlock, pitch: 0.7f);
            EnsureFlash();
        }

        void EnsureFlash()
        {
            if (flash != null)
                return;

            var canvasObject = new GameObject("PurpleFlash");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            var imageObject = new GameObject("Tint");
            imageObject.transform.SetParent(canvasObject.transform, false);
            flash = imageObject.AddComponent<Image>();
            flash.raycastTarget = false;
            var rect = flash.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            flash.color = Color.clear;
        }

        void Update()
        {
            // El aura late despacio.
            if (aura.enabled)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 3f);
                aura.transform.localScale = Vector3.one * 2.2f * pulse;
            }

            float flashT = (Time.unscaledTime - flashStart) / FlashSeconds;
            if (flash != null)
                flash.color = flashT is >= 0f and < 1f ? new Color(Purple.r, Purple.g, Purple.b, 0.45f * (1f - flashT)) : Color.clear;

            float ringT = (Time.unscaledTime - ringStart) / RingSeconds;
            bool ringOn = ringT is >= 0f and < 1f;
            ring.enabled = ringOn;
            if (ringOn)
            {
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, RingRadius, 1f - (1f - ringT) * (1f - ringT));
                ring.color = new Color(Purple.r, Purple.g, Purple.b, 1f - ringT);
            }
        }

        // Círculo suave (aura) y anillo fino (onda), de 64×64 a 32 px por unidad: 2 u de diámetro.
        static Sprite GlowSprite() => glowSprite != null ? glowSprite : glowSprite = Build(false);

        static Sprite RingSprite() => ringSprite != null ? ringSprite : ringSprite = Build(true);

        static Sprite Build(bool ring)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = ring ? (d is > 0.86f and < 1f ? 1f : 0f) : Mathf.Clamp01(1f - d) * (d < 1f ? 1f : 0f);
                    // En escalones, como el resto del pixel art.
                    if (!ring)
                        a = Mathf.Floor(a * 4f) / 4f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
