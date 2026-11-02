using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Icono provisional de una barra de estado sobre la cabeza del jugador (el quemado, el veneno de la nube), hecho por código,
    /// sin sprites: una silueta (llama o gota) que se rellena de abajo arriba y late mientras el estado sigue activo. No es hijo
    /// del jugador: lo siguen sus coordenadas, así su máscara no se mezcla con la del lodo. Se sustituirá por arte en la tanda
    /// de arte.
    /// </summary>
    public class BurnIcon : MonoBehaviour
    {
        public enum Shape
        {
            Flame,
            Drop
        }

        // Siluetas, de arriba abajo (8×11).
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

        static readonly string[] Drop =
        {
            "...##...",
            "...##...",
            "..####..",
            "..####..",
            ".######.",
            ".######.",
            "########",
            "########",
            "########",
            ".######.",
            "..####..",
        };

        const int Width = 8, Height = 11;
        const float Scale = 1.4f;
        const int FillOrder = 31;

        static Sprite flameSprite;
        static Sprite dropSprite;
        static Sprite white;

        Transform target;
        Vector3 offset;
        SpriteMask mask;
        float bar;
        bool pulsing;

        public static BurnIcon Create(Transform target, Shape shape, Color fill, Vector3 offset)
        {
            EnsureSprites();
            var silhouette = shape == Shape.Drop ? dropSprite : flameSprite;
            var root = new GameObject(shape == Shape.Drop ? "PoisonIcon" : "BurnIcon");
            var icon = root.AddComponent<BurnIcon>();
            icon.target = target;
            icon.offset = offset;
            root.transform.localScale = Vector3.one * Scale;

            // Cada icono tiene su propio orden de dibujado para que sus máscaras no se mezclen con las de otro.
            int order = shape == Shape.Drop ? FillOrder + 10 : FillOrder;
            AddRenderer(root.transform, "Outline", silhouette, order - 1, new Color(0.15f, 0.1f, 0.05f, 0.9f));
            var fillRenderer = AddRenderer(root.transform, "Fill", silhouette, order, fill);
            fillRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            var maskObject = new GameObject("FillMask");
            maskObject.transform.SetParent(root.transform, false);
            icon.mask = maskObject.AddComponent<SpriteMask>();
            icon.mask.sprite = white;
            // Solo afecta a la silueta de dentro: su orden de dibujado y ningún otro.
            icon.mask.isCustomRangeActive = true;
            icon.mask.frontSortingOrder = order + 1;
            icon.mask.backSortingOrder = order - 1;
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

        public void Set(float newBar, bool visible, bool newPulsing)
        {
            bar = Mathf.Clamp01(newBar);
            pulsing = newPulsing;
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            Place();
        }

        void Place()
        {
            if (mask == null)
                return;
            // La máscara cubre de los pies de la silueta hasta la altura de la barra (en tiles).
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
            transform.position = target.position + offset;
            // Mientras el estado sigue activo, el icono late.
            float pulse = pulsing ? 1f + 0.12f * Mathf.Sin(Time.time * 14f) : 1f;
            transform.localScale = Vector3.one * Scale * pulse;
        }

        static void EnsureSprites()
        {
            if (flameSprite == null)
                flameSprite = Build(Flame);
            if (dropSprite == null)
                dropSprite = Build(Drop);
            if (white == null)
            {
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
        }

        static Sprite Build(string[] rows)
        {
            var texture = new Texture2D(Width, Height) { filterMode = FilterMode.Point };
            for (int row = 0; row < Height; row++)
                for (int x = 0; x < Width; x++)
                    texture.SetPixel(x, Height - 1 - row, rows[row][x] == '#' ? Color.white : Color.clear);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
