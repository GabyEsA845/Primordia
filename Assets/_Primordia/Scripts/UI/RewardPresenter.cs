using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Pantalla de revelación de reliquias y objetos clave (formato común, ver README del Lepidodendron «Recompensa»), sobre un
    /// escenario de 480×270, y fundidos de color a pantalla completa. Todo lo que crea (lienzo, imágenes) se destruye con el objeto.
    /// </summary>
    public sealed class RewardPresenter : MonoBehaviour
    {
        // Medidas y tiempos del README (provisionales los tiempos).
        public const float EnterSeconds = 0.5f, TextSeconds = 0.3f, ExitSeconds = 0.4f, AutoCloseSeconds = 2.5f;
        static readonly Color Dim = new(6f / 255f, 4f / 255f, 10f / 255f, 175f / 255f);
        static readonly Color Gold = new(1f, 0.83f, 0.41f), Cream = new(246f / 255f, 232f / 255f, 200f / 255f);
        const float StageWidth = 480f, StageHeight = 270f, ItemX = 240f, ItemY = 98f, HaloFrame = 0.12f;

        public static bool Showing { get; private set; }

        public struct Content
        {
            public Sprite[] halo, item;
            public Sprite title, filletTop, filletBottom;
            public string label, effect;
            /// <summary>Punto de partida del objeto en el escenario de 480×270 (y hacia abajo).</summary>
            public Vector2 from;
        }

        RectTransform stage;
        Image fade;
        Font font;
        float previousScale;
        bool holdingTime;

        public static RewardPresenter Create()
        {
            var go = new GameObject("RewardPresenter");
            var presenter = go.AddComponent<RewardPresenter>();
            presenter.Build();
            return presenter;
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(StageWidth, StageHeight);
            scaler.matchWidthOrHeight = 0.5f;
            var sample = FindAnyObjectByType<Text>(FindObjectsInactive.Include);
            font = sample != null && sample.font != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var stageObject = new GameObject("Stage", typeof(RectTransform));
            stageObject.transform.SetParent(transform, false);
            stage = stageObject.GetComponent<RectTransform>();
            stage.sizeDelta = new Vector2(StageWidth, StageHeight);
            var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fadeObject.transform.SetParent(transform, false);
            var fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero; fadeRect.anchorMax = Vector2.one; fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            fade = fadeObject.GetComponent<Image>();
            fade.raycastTarget = false;
            fade.color = Color.clear;
            fadeObject.transform.SetAsLastSibling();
        }

        void OnDestroy()
        {
            ReleaseTime();
        }

        void ReleaseTime()
        {
            if (!holdingTime) return;
            holdingTime = false;
            Showing = false;
            if (Time.timeScale == 0f) Time.timeScale = previousScale;
        }

        /// <summary>Fundido de color a pantalla completa (tiempo real). De alfa <paramref name="from"/> a <paramref name="to"/>.</summary>
        public IEnumerator Fade(Color color, float seconds, float from, float to)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(color.r, color.g, color.b, Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            fade.color = new Color(color.r, color.g, color.b, to);
        }

        public IEnumerator Present(Content content)
        {
            Showing = true;
            holdingTime = true;
            previousScale = Time.timeScale;
            Time.timeScale = 0f;
            var dim = Add("Dim", null, Vector2.zero, Vector2.zero);
            var dimRect = dim.rectTransform;
            dimRect.SetParent(transform, false);
            dimRect.SetSiblingIndex(0);
            dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one; dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            dim.color = Color.clear;
            var halo = Add("Halo", content.halo != null && content.halo.Length > 0 ? content.halo[0] : null, new Vector2(ItemX, ItemY));
            var item = Add("Item", content.item != null && content.item.Length > 0 ? content.item[0] : null, content.from);
            var label = AddText("Label", content.label, 8, Gold, new Vector2(ItemX, 28f));
            Image title = content.title != null ? Add("Title", content.title, new Vector2(ItemX, 166f + content.title.rect.height / 2f)) : null;
            float nameBottom = 166f + (content.title != null ? content.title.rect.height : 0f);
            var top = content.filletTop != null ? Add("FilletTop", content.filletTop, new Vector2(ItemX, 156f + content.filletTop.rect.height / 2f)) : null;
            var bottom = content.filletBottom != null ? Add("FilletBottom", content.filletBottom, new Vector2(ItemX, nameBottom + 3f + content.filletBottom.rect.height / 2f)) : null;
            var effect = AddText("Effect", content.effect, 8, Cream, new Vector2(ItemX, nameBottom + 14f));
            SetAlpha(title, 0f); SetAlpha(top, 0f); SetAlpha(bottom, 0f); SetAlpha(label, 0f); SetAlpha(effect, 0f);
            SetAlpha(halo, 0f); SetAlpha(item, 0f);

            float clock = 0f;
            for (float t = 0f; t < EnterSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / EnterSeconds);
                clock += Time.unscaledDeltaTime;
                dim.color = new Color(Dim.r, Dim.g, Dim.b, Dim.a * k);
                SetAlpha(halo, k); SetAlpha(item, k);
                Place(item, Vector2.Lerp(content.from, new Vector2(ItemX, ItemY), k));
                Animate(halo, content.halo, clock); Animate(item, content.item, clock);
                yield return null;
            }
            for (float t = 0f; t < TextSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / TextSeconds;
                clock += Time.unscaledDeltaTime;
                SetAlpha(title, k); SetAlpha(top, k); SetAlpha(bottom, k); SetAlpha(label, k); SetAlpha(effect, k);
                Animate(halo, content.halo, clock); Animate(item, content.item, clock);
                yield return null;
            }
            SetAlpha(title, 1f); SetAlpha(top, 1f); SetAlpha(bottom, 1f); SetAlpha(label, 1f); SetAlpha(effect, 1f);
            for (float held = 0f; held < AutoCloseSeconds; held += Time.unscaledDeltaTime)
            {
                clock += Time.unscaledDeltaTime;
                Animate(halo, content.halo, clock); Animate(item, content.item, clock);
                var keyboard = Keyboard.current;
                if (held > 0.2f && keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                    break;
                yield return null;
            }
            for (float t = 0f; t < ExitSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / ExitSeconds;
                clock += Time.unscaledDeltaTime;
                dim.color = new Color(Dim.r, Dim.g, Dim.b, Dim.a * k);
                SetAlpha(halo, k); SetAlpha(item, k); SetAlpha(title, k); SetAlpha(top, k); SetAlpha(bottom, k); SetAlpha(label, k); SetAlpha(effect, k);
                Animate(halo, content.halo, clock); Animate(item, content.item, clock);
                yield return null;
            }
            ReleaseTime();
            Destroy(gameObject);
        }

        static void Animate(Image image, Sprite[] frames, float clock)
        {
            if (image == null || frames == null || frames.Length == 0) return;
            image.sprite = frames[Mathf.FloorToInt(clock / HaloFrame) % frames.Length];
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        void Place(Graphic graphic, Vector2 stagePoint) => graphic.rectTransform.anchoredPosition = new Vector2(stagePoint.x - StageWidth / 2f, StageHeight / 2f - stagePoint.y);

        Image Add(string name, Sprite sprite, Vector2 stagePoint, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(stage, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.color = Color.white;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? (sprite != null ? sprite.rect.size : Vector2.zero);
            Place(image, stagePoint);
            return image;
        }

        Text AddText(string name, string text, int size, Color color, Vector2 stagePoint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(stage, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.text = text ?? "";
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(440f, 14f);
            Place(label, stagePoint);
            return label;
        }
    }
}
