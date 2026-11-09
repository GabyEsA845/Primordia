using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Capas de la tormenta de la Meganeura (fase 3): oscuridad (#0C1428, alfa ~0,5), destello del relámpago (#E8F4FF, baja en 0,25 s),
    /// lluvia y viento con ceniza en mosaico. Todo en el mundo, cubriendo la sala: la oscuridad (20) y el destello (21) quedan
    /// por debajo de lo que brilla (rayos, señales, charcas electrificadas, fuego, el jefe: orden ≥ 26); la lluvia (22) encima.
    /// </summary>
    public class MeganeuraStorm : MonoBehaviour
    {
        const int DarkOrder = 20, FlashOrder = 21, RainOrder = 22;
        const float FlashFallSeconds = 0.25f;
        const float RainFrameSeconds = 0.06f, AshFrameSeconds = 0.08f;
        static readonly Color DarkColor = new(0x0C / 255f, 0x14 / 255f, 0x28 / 255f, 1f);
        static readonly Color FlashColor = new(0xE8 / 255f, 0xF4 / 255f, 0xFF / 255f, 1f);

        static Sprite white;

        SpriteRenderer dark;
        SpriteRenderer flash;
        float darkFrom, darkTarget, darkTime, darkDuration;
        float flashAlpha;
        readonly List<SpriteRenderer> rain = new();
        readonly List<SpriteRenderer> ash = new();
        Sprite[] rainFrames = System.Array.Empty<Sprite>();
        Sprite[] ashFrames = System.Array.Empty<Sprite>();

        public float DarkAlpha => dark != null ? dark.color.a : 0f;
        public float FlashAlpha => flash != null ? flash.color.a : 0f;
        public bool Raining { get; private set; }
        public bool AshOn { get; private set; }

        public static MeganeuraStorm Create(Transform parent, Vector2 center, Vector2 size, Sprite[] rainFrames, Sprite[] ashFrames)
        {
            var go = new GameObject("MeganeuraStorm");
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            var storm = go.AddComponent<MeganeuraStorm>();
            storm.rainFrames = rainFrames ?? System.Array.Empty<Sprite>();
            storm.ashFrames = ashFrames ?? System.Array.Empty<Sprite>();
            // Un poco más grande que la sala: la cámara ve algo más que el suelo.
            var cover = size + new Vector2(24f, 16f);
            storm.dark = storm.Quad("Dark", DarkColor, DarkOrder, cover);
            storm.flash = storm.Quad("Flash", FlashColor, FlashOrder, cover);
            storm.Tile(storm.rain, storm.rainFrames, cover, RainOrder);
            storm.Tile(storm.ash, storm.ashFrames, cover, RainOrder);
            SetTiles(storm.rain, false);
            SetTiles(storm.ash, false);
            return storm;
        }

        SpriteRenderer Quad(string name, Color color, int order, Vector2 size)
        {
            if (white == null)
                white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = white;
            renderer.sortingOrder = order;
            color.a = 0f;
            renderer.color = color;
            return renderer;
        }

        void Tile(List<SpriteRenderer> into, Sprite[] frames, Vector2 cover, int order)
        {
            if (frames.Length == 0)
                return;
            var tile = frames[0].bounds.size;
            int columns = Mathf.CeilToInt(cover.x / tile.x), rows = Mathf.CeilToInt(cover.y / tile.y);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var go = new GameObject("Tile");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3((x - (columns - 1) * 0.5f) * tile.x, (y - (rows - 1) * 0.5f) * tile.y, 0f);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = frames[0];
                    renderer.sortingOrder = order;
                    into.Add(renderer);
                }
            }
        }

        static void SetTiles(List<SpriteRenderer> tiles, bool on)
        {
            foreach (var tile in tiles)
                tile.enabled = on;
        }

        public void SetRain(bool on)
        {
            Raining = on;
            SetTiles(rain, on);
        }

        public void SetAsh(bool on)
        {
            AshOn = on;
            SetTiles(ash, on);
        }

        /// <summary>Lleva la oscuridad a <paramref name="alpha"/> en <paramref name="seconds"/>.</summary>
        public void Darken(float alpha, float seconds)
        {
            darkFrom = DarkAlpha;
            darkTarget = alpha;
            darkTime = 0f;
            darkDuration = Mathf.Max(0.01f, seconds);
        }

        /// <summary>Destello de pantalla: sube de golpe y baja en 0,25 s.</summary>
        public void Flash(float alpha) => flashAlpha = Mathf.Max(flashAlpha, alpha);

        /// <summary>La tormenta amaina: se va la oscuridad y la lluvia.</summary>
        public void Calm(float seconds)
        {
            SetAsh(false);
            SetRain(false);
            Darken(0f, seconds);
        }

        void Update()
        {
            if (darkDuration > 0f && dark != null)
            {
                darkTime += Time.deltaTime;
                var color = dark.color;
                color.a = Mathf.Lerp(darkFrom, darkTarget, Mathf.Clamp01(darkTime / darkDuration));
                dark.color = color;
            }
            if (flash != null)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime / FlashFallSeconds);
                var color = flash.color;
                color.a = flashAlpha;
                flash.color = color;
            }
            Animate(rain, rainFrames, RainFrameSeconds);
            Animate(ash, ashFrames, AshFrameSeconds);
        }

        static void Animate(List<SpriteRenderer> tiles, Sprite[] frames, float frameSeconds)
        {
            if (frames.Length == 0 || tiles.Count == 0 || !tiles[0].enabled)
                return;
            var frame = frames[Mathf.FloorToInt(Time.time / frameSeconds) % frames.Length];
            foreach (var tile in tiles)
                tile.sprite = frame;
        }
    }
}
