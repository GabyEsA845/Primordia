using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Filas «Reliquias» y «Objetos clave» en la esquina inferior izquierda de la pausa: icono, nombre y efecto. Cada fila se
    /// oculta si el jugador no tiene nada de ese tipo. Se monta sola al abrir la pausa (<see cref="GameUI"/> la añade).
    /// </summary>
    public sealed class PauseRelicsRow : MonoBehaviour
    {
        const float Margin = 8f, RowHeight = 18f, IconSize = 16f;
        static readonly Color Gold = new(1f, 0.83f, 0.41f), Cream = new(246f / 255f, 232f / 255f, 200f / 255f);

        RectTransform root;

        internal int RowCount { get; private set; }
        internal bool ShowsRelics { get; private set; }
        internal bool ShowsKeyItems { get; private set; }

        void OnEnable() => Rebuild();

        void OnDisable() => Clear();

        void Clear()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            RowCount = 0;
        }

        internal void Rebuild()
        {
            Clear();
            ShowsRelics = RunInventory.Relics.Count > 0;
            ShowsKeyItems = RunInventory.KeyItems.Count > 0;
            if (!ShowsRelics && !ShowsKeyItems) return;
            var go = new GameObject("RelicsRow", typeof(RectTransform));
            root = go.GetComponent<RectTransform>();
            root.SetParent(transform, false);
            root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
            root.anchoredPosition = new Vector2(Margin, Margin);
            var sample = GetComponentInChildren<Text>(true);
            var font = sample != null && sample.font != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 0f;
            if (ShowsKeyItems) y = AddGroup("OBJETOS CLAVE", RunInventory.KeyItems, y, font);
            if (ShowsRelics) AddGroup("RELIQUIAS", RunInventory.Relics, y, font);
        }

        float AddGroup(string title, System.Collections.Generic.IReadOnlyList<RunInventory.Entry> entries, float y, Font font)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(root, false);
                icon.sprite = entry.Icon16;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);
                icon.rectTransform.anchoredPosition = new Vector2(0f, y);
                AddText(font, $"{entry.Name}: {entry.Effect}", Cream, new Vector2(IconSize + 4f, y));
                y += RowHeight;
                RowCount++;
            }
            AddText(font, title, Gold, new Vector2(0f, y));
            RowCount++;
            return y + RowHeight;
        }

        void AddText(Font font, string text, Color color, Vector2 position)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(root, false);
            label.font = font;
            label.fontSize = 8;
            label.text = text;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(300f, IconSize);
            label.rectTransform.anchoredPosition = position;
        }
    }
}
