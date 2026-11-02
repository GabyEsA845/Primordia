using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Icono de una barra de estado sobre la cabeza del jugador (el quemado, el veneno de la nube): el dibujo vacío debajo y el
    /// lleno encima, recortado de abajo arriba hasta el nivel de la barra; late mientras el estado sigue activo. Con el arte del
    /// pulido usa los iconos de <see cref="PolishArt"/> (llama y nube con calavera); sin importar, una silueta provisional hecha
    /// por código (llama o gota). No es hijo del jugador: lo siguen sus coordenadas, así su máscara no se mezcla con la del lodo.
    /// </summary>
    public class BurnIcon : MonoBehaviour
    {
        public enum Shape
        {
            Flame,
            Cloud
        }

        // Siluetas provisionales, de arriba abajo (8×11).
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
        // El icono provisional se agranda; los dibujos del arte ya vienen del tamaño que se ve.
        const float ProvisionalScale = 1.4f;
        const int FillOrder = 31;

        static Sprite flameSprite;
        static Sprite dropSprite;
        static Sprite white;

        Transform target;
        Vector3 offset;
        SpriteMask mask;
        float baseScale = 1f;
        // Tamaño del dibujo en unidades (la máscara lo recorta).
        Vector2 size;
        float bar;
        bool pulsing;

        public static BurnIcon Create(Transform target, Shape shape, Color fill, Vector3 offset)
        {
            EnsureSprites();
            var art = PolishArt.Current;
            var empty = art == null ? null : shape == Shape.Cloud ? art.toxicEmpty : art.burnEmpty;
            var full = art == null ? null : shape == Shape.Cloud ? art.toxicFull : art.burnFull;
            bool drawn = empty != null && full != null;

            var silhouette = drawn ? full : shape == Shape.Cloud ? dropSprite : flameSprite;
            var root = new GameObject(shape == Shape.Cloud ? "PoisonIcon" : "BurnIcon");
            var icon = root.AddComponent<BurnIcon>();
            icon.target = target;
            icon.offset = offset;
            icon.baseScale = drawn ? 1f : ProvisionalScale;
            icon.size = silhouette.bounds.size;
            root.transform.localScale = Vector3.one * icon.baseScale;

            // Cada icono tiene su propio orden de dibujado para que sus máscaras no se mezclen con las de otro.
            int order = shape == Shape.Cloud ? FillOrder + 10 : FillOrder;
            if (drawn)
                AddRenderer(root.transform, "Empty", empty, order - 1, Color.white);
            else
                AddRenderer(root.transform, "Outline", silhouette, order - 1, new Color(0.15f, 0.1f, 0.05f, 0.9f));
            var fillRenderer = AddRenderer(root.transform, "Fill", silhouette, order, drawn ? Color.white : fill);
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
            // La máscara cubre de los pies de la silueta hasta la altura de la barra (en unidades).
            mask.transform.localScale = new Vector3(size.x + 0.2f, size.y * bar + 0.0001f, 1f);
            mask.transform.localPosition = new Vector3(0f, -size.y / 2f + size.y * bar / 2f, 0f);
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
            transform.localScale = Vector3.one * baseScale * pulse;
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
