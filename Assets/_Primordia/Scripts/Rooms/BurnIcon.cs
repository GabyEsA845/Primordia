using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Icono provisional de la barra de quemado (hecho por código, sin sprites): una llama sobre la cabeza del jugador que se
    /// rellena de abajo arriba y late mientras sigue ardiendo. No es hijo del jugador: lo siguen sus coordenadas, así su
    /// máscara no se mezcla con la del lodo. Se sustituirá por arte en la tanda de arte.
    /// </summary>
    public class BurnIcon : MonoBehaviour
    {
        // Silueta de la llama, de arriba abajo (8×11).
        static readonly string[] Flame =
        {
            "...#....",
            "...##...",
            "..###...",
            "..####.#",
            ".#######",
            ".#######",
            "########",
            "########",
            "########",
            ".######.",
            "..####..",
        };

        const int Width = 8, Height = 11;
        const float Scale = 1.4f;
        const int FillOrder = 31;
        static readonly Vector3 Offset = new(0f, 1.05f, 0f);

        static Sprite silhouette;
        static Sprite white;

        Transform target;
        SpriteRenderer outline;
        SpriteRenderer fill;
        SpriteMask mask;
        float bar;
        bool burning;

        public static BurnIcon Create(Transform target)
        {
            EnsureSprites();
            var root = new GameObject("BurnIcon");
            var icon = root.AddComponent<BurnIcon>();
            icon.target = target;
            root.transform.localScale = Vector3.one * Scale;

            icon.outline = AddRenderer(root.transform, "Outline", silhouette, 30, new Color(0.22f, 0.07f, 0.03f, 0.9f));
            icon.fill = AddRenderer(root.transform, "Fill", silhouette, FillOrder, new Color(1f, 0.55f, 0.1f, 1f));
            icon.fill.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            var maskObject = new GameObject("FillMask");
            maskObject.transform.SetParent(root.transform, false);
            icon.mask = maskObject.AddComponent<SpriteMask>();
            icon.mask.sprite = white;
            // Solo afecta a la llama de dentro: su orden de dibujado y ningún otro.
            icon.mask.isCustomRangeActive = true;
            icon.mask.frontSortingOrder = FillOrder + 1;
            icon.mask.backSortingOrder = FillOrder - 1;
            icon.Place();
            root.SetActive(false);
            return icon;
        }

        static SpriteRenderer AddRenderer(Transform parent, string name, Sprite sprite, int order, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color;
            return renderer;
        }

        public void Set(float newBar, bool visible, bool newBurning)
        {
            bar = Mathf.Clamp01(newBar);
            burning = newBurning;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            Place();
        }

        void Place()
        {
            if (mask == null)
                return;
            // La máscara cubre de los pies de la llama hasta la altura de la barra (en tiles de la llama).
            float height = Height / 16f;
            mask.transform.localScale = new Vector3(Width / 16f + 0.2f, height * bar + 0.0001f, 1f);
            mask.transform.localPosition = new Vector3(0f, -height / 2f + height * bar / 2f, 0f);
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = target.position + Offset;
            // Mientras sigue ardiendo, la llama late.
            float pulse = burning ? 1f + 0.12f * Mathf.Sin(Time.time * 14f) : 1f;
            transform.localScale = Vector3.one * Scale * pulse;
        }

        static void EnsureSprites()
        {
            if (silhouette == null)
            {
                var texture = new Texture2D(Width, Height) { filterMode = FilterMode.Point };
                for (int row = 0; row < Height; row++)
                    for (int x = 0; x < Width; x++)
                        texture.SetPixel(x, Height - 1 - row, Flame[row][x] == '#' ? Color.white : Color.clear);
                texture.Apply();
                silhouette = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 16f);
            }
            if (white == null)
            {
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
        }
    }
}
