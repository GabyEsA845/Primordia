using System;
using System.Collections.Generic;
using Primordia.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Base de los menús de membrana (pausa y bestiario). La página se dibuja por código en un lienzo de 480×270
    /// centrado, con las coordenadas de las maquetas (x a la derecha, y hacia abajo), y se rehace entera al cambiar
    /// de pantalla. Teclado: flechas o WASD para moverse, Intro para pulsar, Q y E para las pestañas.
    /// </summary>
    public abstract class MemScreen : MonoBehaviour
    {
        public static readonly Color Cream = new Color32(0xf6, 0xe8, 0xc8, 0xff);
        public static readonly Color Bright = new Color32(0xff, 0xfb, 0xe8, 0xff);
        public static readonly Color Dim = new Color32(0x9e, 0xc8, 0xb4, 0xff);
        public static readonly Color Faint = new Color32(0x7f, 0xa8, 0x98, 0xff);
        public static readonly Color Heading = new Color32(0xff, 0xd2, 0x7a, 0xff);
        public static readonly Color Amber = new Color32(0xf2, 0xc2, 0x4e, 0xff);
        static readonly Color ShadowColor = new Color32(0x18, 0x14, 0x25, 0xff);

        readonly List<MemItem> items = new();
        MemItem focused;
        RectTransform info;

        internal PauseMenuArt Art { get; set; }
        internal RectTransform Page { get; private set; }
        internal IReadOnlyList<MemItem> Items => items;
        internal MemItem Focused => focused;
        internal bool InfoShown => info != null;

        /// <summary>Borra la página y crea una vacía.</summary>
        internal void ResetPage(Transform parent, int siblingIndex = 0)
        {
            ClearPage();
            Page = NewRect("Page", parent);
            Page.anchorMin = Page.anchorMax = Page.pivot = new Vector2(0.5f, 0.5f);
            Page.sizeDelta = new Vector2(480f, 270f);
            Page.SetSiblingIndex(siblingIndex);
        }

        internal void ClearPage()
        {
            foreach (var item in items)
            {
                if (item == null)
                    continue;
                if (item == focused)
                    item.Look?.Invoke(false);
                item.Bind(null, null, null, null, true);
            }
            items.Clear();
            focused = null;
            info = null;

            if (Page == null)
                return;
            Remove(Page.gameObject);
            Page = null;
        }

        internal static void Remove(GameObject target)
        {
            target.SetActive(false);
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        // ---------- Foco ----------

        /// <summary>Hace señalable un elemento (el ratón lo detecta por su gráfico).</summary>
        internal MemItem AddItem(RectTransform rect, Action<bool> look, Action submit = null, Action<int> adjust = null,
            bool keyboard = true)
        {
            if (!rect.TryGetComponent<Graphic>(out var graphic))
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                graphic = image;
            }
            graphic.raycastTarget = true;
            if (!rect.TryGetComponent<MemItem>(out var item))
                item = rect.gameObject.AddComponent<MemItem>();
            item.Bind(this, look, submit, adjust, keyboard);
            items.Add(item);
            look?.Invoke(false);
            return item;
        }

        internal void Focus(MemItem item, bool sound = false)
        {
            if (item == null || item == focused)
                return;
            var previous = focused;
            focused = item;
            if (previous != null)
                previous.Look?.Invoke(false);
            item.Look?.Invoke(true);
            if (sound)
                Sfx.Play(Sounds.UIHover);
        }

        internal void Unfocus(MemItem item)
        {
            if (item != focused)
                return;
            focused = null;
            item.Look?.Invoke(false);
        }

        internal void Press(MemItem item)
        {
            if (item == null || item.Submit == null)
                return;
            Sfx.Play(Sounds.UIClick);
            item.Submit();
        }

        internal void FocusIndex(int index)
        {
            if (index >= 0 && index < items.Count)
                Focus(items[index]);
        }

        /// <summary>Pasa al elemento más cercano en esa dirección (o, sin foco, al primero).</summary>
        internal void Move(Vector2 direction)
        {
            if (focused == null || !focused.Keyboard)
            {
                Focus(items.Find(i => i.Keyboard), sound: true);
                return;
            }

            if (focused.Adjust != null && direction.x != 0f)
            {
                focused.Adjust(direction.x > 0f ? 1 : -1);
                return;
            }

            Vector2 from = Center(focused.Rect);
            MemItem best = null;
            float bestScore = float.MaxValue;
            foreach (var item in items)
            {
                if (item == focused || !item.Keyboard)
                    continue;
                Vector2 delta = Center(item.Rect) - from;
                float along = Vector2.Dot(delta, direction);
                if (along <= 1f)
                    continue;
                float across = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float score = along + across * 2f;
                if (score < bestScore)
                {
                    best = item;
                    bestScore = score;
                }
            }
            Focus(best, sound: true);
        }

        Vector2 Center(RectTransform rect) => transform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));

        /// <summary>Q (−1) y E (+1).</summary>
        protected virtual void ChangeTab(int step) { }

        protected virtual void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || Page == null)
                return;

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                Move(Vector2.up);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                Move(Vector2.down);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                Move(Vector2.left);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                Move(Vector2.right);
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                Press(focused);
            else if (keyboard.qKey.wasPressedThisFrame)
                ChangeTab(-1);
            else if (keyboard.eKey.wasPressedThisFrame)
                ChangeTab(1);
        }

        // ---------- Piezas ----------

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>Rectángulo con su esquina de arriba a la izquierda en (x, y) de la página.</summary>
        internal static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>Imagen a su tamaño.</summary>
        internal Image Picture(Sprite sprite, float x, float y, Transform parent = null)
        {
            var size = sprite != null ? sprite.rect.size : Vector2.zero;
            return Box(sprite, x, y, size.x, size.y, Image.Type.Simple, parent);
        }

        internal Image Box(Sprite sprite, float x, float y, float width, float height, Image.Type type, Transform parent = null)
        {
            var rect = NewRect(sprite != null ? sprite.name : "Image", parent != null ? parent : Page);
            Place(rect, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        internal static void Animate(Image image, Sprite[] frames, float frameDuration)
        {
            if (frames == null || frames.Length == 0)
                return;
            image.sprite = frames[0];
            image.enabled = true;
            if (!image.TryGetComponent<UISpriteAnimation>(out var animation))
                animation = image.gameObject.AddComponent<UISpriteAnimation>();
            animation.Configure(frames, frameDuration, newLoop: true);
        }

        /// <summary>
        /// Texto de una línea centrado en vertical en y; según la alineación, x es su centro, su borde izquierdo o su
        /// borde derecho. Con <paramref name="wrapWidth"/> es un párrafo y y es el centro de su primera línea.
        /// </summary>
        internal Text Label(string content, float x, float y, Color color, TextAnchor alignment = TextAnchor.MiddleCenter,
            bool bold = false, float wrapWidth = 0f, Transform parent = null)
        {
            var rect = NewRect("Text", parent != null ? parent : Page);
            bool wrap = wrapWidth > 0f;
            bool left = alignment is TextAnchor.MiddleLeft or TextAnchor.UpperLeft;
            bool right = alignment is TextAnchor.MiddleRight or TextAnchor.UpperRight;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(left ? 0f : right ? 1f : 0.5f, wrap ? 1f : 0.5f);
            rect.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(wrap ? y - 5f : y));
            rect.sizeDelta = new Vector2(wrap ? wrapWidth : 300f, 10f);

            var text = rect.gameObject.AddComponent<Text>();
            var font = bold && Art.boldFont != null ? Art.boldFont : Art.font;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 8;
            text.color = color;
            text.alignment = left ? (wrap ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft)
                : right ? (wrap ? TextAnchor.UpperRight : TextAnchor.MiddleRight)
                : wrap ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.text = content;
            // Centrado y de ancho impar, empezaría en medio píxel y las letras saldrían deformadas.
            if (!wrap && !left && !right)
                rect.sizeDelta = new Vector2(300f + Mathf.CeilToInt(text.preferredWidth) % 2, 10f);
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(0f, -1f);
            return text;
        }

        internal static float WidthOf(Text text) => Mathf.Ceil(text.preferredWidth);

        /// <summary>Panel de membrana.</summary>
        internal Image Panel(float x, float y, float width, float height) =>
            Box(Art.panel, x, y, width, height, Image.Type.Tiled);

        /// <summary>Cinta del título, centrada, con su texto a la altura <paramref name="centerY"/>.</summary>
        internal Text TitleBanner(string content, float centerY)
        {
            var banner = Box(Art.title, 0f, centerY - 11f, 64f, 22f, Image.Type.Sliced);
            var label = Label(content, 240f, centerY, Bright, bold: true);
            float width = WidthOf(label) + 50f;
            width += width % 2f;
            Place(banner.rectTransform, 240f - width / 2f, centerY - 11f, width, 22f);
            return label;
        }

        /// <summary>Botón célula centrado en x, con el núcleo que late solo cuando tiene el foco.</summary>
        internal MemItem CellButton(string content, float centerX, float y, float width, Action submit, bool danger = false)
        {
            float x = centerX - width / 2f - 1f;
            var image = Box(danger ? Art.dangerNormal : Art.buttonNormal, x, y, width + 2f, 17f, Image.Type.Tiled);
            image.name = "Button_" + content;
            var nucleus = Box(null, 3f, 2f, 13f, 13f, Image.Type.Simple, image.transform);
            Animate(nucleus, danger ? Art.nucleusDanger : Art.nucleus, 0.15f);
            var label = Label(content, centerX, y + 8f, Cream);
            return AddItem(image.rectTransform, focus =>
            {
                image.sprite = focus
                    ? danger ? Art.dangerHighlighted : Art.buttonHighlighted
                    : danger ? Art.dangerNormal : Art.buttonNormal;
                nucleus.gameObject.SetActive(focus);
                label.color = focus ? Bright : Cream;
            }, submit);
        }

        /// <summary>Fila de datos: franja suave, icono, nombre y valor a la derecha.</summary>
        internal Image Row(float x, float y, float width, Sprite icon, string name, string value, Color? color = null)
        {
            var strip = Box(Art.row, x, y, width, 14f, Image.Type.Sliced);
            if (icon != null)
                Box(icon, x + 4f, y + 1f, 12f, 12f, Image.Type.Simple).preserveAspect = true;
            Label(name, x + (icon != null ? 20f : 6f), y + 7f, color ?? Cream, TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(value))
                Label(value, x + width - 5f, y + 7f, color ?? Bright, TextAnchor.MiddleRight);
            return strip;
        }

        /// <summary>
        /// Fila de pestañas en yema sobre el borde de arriba de un panel, con las teclas Q y E a los lados. Las
        /// pestañas solo se pulsan con el ratón (el teclado usa Q y E).
        /// </summary>
        internal void Tabs(string[] labels, Sprite[] icons, int active, float panelX, float panelY, float panelWidth,
            Action<int> select)
        {
            var texts = new Text[labels.Length];
            var widths = new float[labels.Length];
            float total = 0f;
            for (int i = 0; i < labels.Length; i++)
            {
                texts[i] = Label(labels[i], 0f, 0f, i == active ? Bright : Dim);
                widths[i] = WidthOf(texts[i]) + 35f;
                widths[i] += widths[i] % 2f;
                total += widths[i];
            }

            float x = panelX + Mathf.Floor((panelWidth - total + 2f * (labels.Length - 1)) / 2f);
            // x par respecto al panel, para que el punteado de la membrana case.
            x += Mathf.Abs(x - panelX) % 2f;
            float first = x;
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                bool selected = i == active;
                var tab = Box(selected ? Art.tabActive : Art.tabIdle, x, panelY - 18f, widths[i], 24f, Image.Type.Tiled);
                tab.name = "Tab_" + labels[i];
                float centerY = panelY - (selected ? 8f : 6f);
                if (icons != null && i < icons.Length)
                {
                    var icon = Picture(icons[i], x + 9f, centerY - 6f);
                    if (!selected)
                        icon.color = new Color(0.68f, 0.74f, 0.7f);
                }
                texts[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(x + 24f + (widths[i] - 33f) / 2f), -centerY);
                texts[i].transform.SetAsLastSibling();
                if (!selected)
                {
                    AddItem(tab.rectTransform, focus => tab.sprite = focus ? Art.tabHover : Art.tabIdle,
                        () => select(index), keyboard: false);
                }
                x += widths[i] - 2f;
            }

            Key(Art.keyQ, first - 15f, panelY - 15f, () => ChangeTab(-1));
            Key(Art.keyE, x + 4f, panelY - 15f, () => ChangeTab(1));
        }

        void Key(Sprite[] frames, float x, float y, Action submit)
        {
            if (frames == null || frames.Length == 0)
                return;
            var key = Picture(frames[0], x, y);
            AddItem(key.rectTransform, focus => key.sprite = frames[focus && frames.Length > 1 ? 1 : 0], submit, keyboard: false);
        }

        /// <summary>"ESC VOLVER" abajo a la derecha, que también se puede pulsar.</summary>
        internal void BackHint(float panelX, float panelY, float panelWidth, float panelHeight, Action back)
        {
            float x = panelX + panelWidth - 44f, y = panelY + panelHeight - 10f;
            var area = NewRect("Back", Page);
            Place(area, x - 32f, y - 6f, 64f, 12f);
            var label = Label("ESC VOLVER", x, y, Faint);
            AddItem(area, focus => label.color = focus ? Bright : Faint, back, keyboard: false);
        }

        // ---------- Cuadro de información ----------

        /// <summary>Cuadro de información a la derecha de lo señalado, con el pseudópodo apuntando a la izquierda.</summary>
        internal void ShowInfo(float x, float centerY, string title, string subtitle, Color color, string body)
        {
            HideInfo();
            if (Page == null)
                return;
            const float width = 124f;
            info = NewRect("Info", Page);
            Place(info, 0f, 0f, 480f, 270f);
            var background = Box(Art.tooltip, x, 0f, width, 40f, Image.Type.Tiled, info);
            var backing = Box(null, x + 5f, 0f, width - 10f, 30f, Image.Type.Simple, info);
            Opaque(backing);
            backing.transform.SetAsFirstSibling();
            var stripe = Box(Art.tooltipStripe, x + 6f, 0f, width - 12f, 4f, Image.Type.Sliced, info);
            stripe.color = color;
            var name = Label(title, x + 8f, 0f, Bright, TextAnchor.UpperLeft, wrapWidth: width - 14f, parent: info);
            var kind = Label(subtitle, x + 8f, 0f, color, TextAnchor.UpperLeft, wrapWidth: width - 14f, parent: info);
            var text = Label(body, x + 8f, 0f, Dim, TextAnchor.UpperLeft, wrapWidth: width - 14f, parent: info);

            float nameHeight = Mathf.Ceil(name.preferredHeight);
            float kindHeight = Mathf.Ceil(kind.preferredHeight);
            float bodyHeight = string.IsNullOrEmpty(body) ? 0f : Mathf.Ceil(text.preferredHeight);
            float height = 14f + nameHeight + kindHeight + bodyHeight + 10f;
            height += height % 2f;
            float top = Mathf.Round(Mathf.Clamp(centerY - height / 2f, 4f, 266f - height));

            Place(background.rectTransform, x, top, width, height);
            Place(backing.rectTransform, x + 5f, top + 5f, width - 10f, height - 10f);
            stripe.rectTransform.anchoredPosition = new Vector2(x + 6f, -(top + 6f));
            name.rectTransform.anchoredPosition = new Vector2(x + 8f, -(top + 12f));
            kind.rectTransform.anchoredPosition = new Vector2(x + 8f, -(top + 14f + nameHeight));
            text.rectTransform.anchoredPosition = new Vector2(x + 8f, -(top + 18f + nameHeight + kindHeight));
            Picture(Art.tooltipPod, x - 8f, Mathf.Clamp(centerY - 7f, top, top + height - 14f), info);
        }

        /// <summary>
        /// Fondo opaco para el interior de un cuadro de información: con la mezcla en espacio lineal, el interior
        /// translúcido deja leer lo de debajo.
        /// </summary>
        internal static void Opaque(Image backing)
        {
            backing.enabled = true;
            backing.color = new Color32(0x0f, 0x20, 0x1d, 0xff);
        }

        internal void HideInfo()
        {
            if (info == null)
                return;
            Remove(info.gameObject);
            info = null;
        }
    }
}
