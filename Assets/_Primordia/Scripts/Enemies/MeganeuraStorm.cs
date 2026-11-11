using System.Collections.Generic;
using UnityEngine;
using Primordia.Rooms;

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

        // Provisionales: duración y reparto del ambiente al terminar la tormenta.
        const float DrizzleSeconds = 8f, SteamSeconds = 6f, SteamFrameSeconds = 0.12f;
        const int SteamCount = 10, SteamOrder = 2;

        // Provisionales: apagado, revelado a contraluz y desplazamiento de las nubes.
        const float GroundQuenchSpeed = 3f, RainDouseSpeed = 1.5f, RainCheckEvery = 0.25f;
        const float SilhouetteSeconds = 0.35f, SilhouetteFrameMs = 90f, RevealFlashAlpha = 0.5f;
        const int SilhouetteOrder = 26;
        const float CloudSpeed = 0.6f, CloudFadeSeconds = 3f;
        const int CloudOrder = 2; // Suelo 1; personajes y troncos desde 4; jugador 10.

        static Sprite white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => white = null;

        MeganeuraBoss boss;
        readonly List<SpriteRenderer> clouds = new();
        Vector2 cloudSpan;
        float wetElapsed;
        GameObject silhouette;

        internal void ConfigureAtmosphere(MeganeuraBoss owner, Sprite cloud)
        {
            boss = owner;
            if (cloud == null || clouds.Count > 0)
                return;
            Vector2 tileSize = cloud.bounds.size;
            Vector2 cover = floorSize + tileSize * 2f;
            cloudSpan = new Vector2(Mathf.Ceil(cover.x / tileSize.x) * tileSize.x, Mathf.Ceil(cover.y / tileSize.y) * tileSize.y);
            Tile(clouds, new[] { cloud }, cover, CloudOrder);
            foreach (var tile in clouds)
                tile.name = "CloudShadow";
        }

        SpriteRenderer dark;
        SpriteRenderer flash;
        float darkFrom, darkTarget, darkTime, darkDuration;
        float flashAlpha;
        readonly List<SpriteRenderer> rain = new();
        readonly List<SpriteRenderer> ash = new();
        Sprite[] rainFrames = System.Array.Empty<Sprite>();
        Sprite[] ashFrames = System.Array.Empty<Sprite>();

        readonly List<SpriteRenderer> steam = new();
        Sprite[] drizzleFrames = System.Array.Empty<Sprite>();
        Sprite[] steamFrames = System.Array.Empty<Sprite>();
        Vector2 floorSize;
        float calmStarted;
        bool calming;
        public bool Drizzling { get; private set; }
        public int SteamActive => steam.Count;

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
            storm.floorSize = size;
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
            if (Raining != on)
                wetElapsed = 0f;
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
        public void Flash(float alpha)
        {
            flashAlpha = Mathf.Max(flashAlpha, alpha);
            if (!calming && alpha >= RevealFlashAlpha && boss != null && boss.IsHigh && !boss.Health.IsDead)
            {
                if (silhouette != null)
                    Destroy(silhouette);
                silhouette = boss.CreateStormSilhouette(transform, SilhouetteSeconds, SilhouetteFrameMs, SilhouetteOrder);
            }
        }

        /// <summary>La tormenta amaina: se va la oscuridad y la lluvia.</summary>
        public void Calm(float seconds, Sprite[] drizzle = null, Sprite[] newSteam = null)
        {
            SetAsh(false);
            SetRain(false);
            Darken(0f, seconds);
            if (calming)
                return;
            calming = true;
            if (silhouette != null)
                Destroy(silhouette);
            calmStarted = Time.time;
            drizzleFrames = drizzle ?? System.Array.Empty<Sprite>();
            steamFrames = newSteam ?? System.Array.Empty<Sprite>();
            Drizzling = drizzleFrames.Length > 0;
            if (Drizzling)
            {
                // Conserva exactamente el mosaico y la capa de la lluvia fuerte.
                if (rain.Count == 0)
                    Tile(rain, drizzleFrames, floorSize + new Vector2(24f, 16f), RainOrder);
                SetTiles(rain, true);
            }
            if (steamFrames.Length == 0)
                return;
            Vector2 half = new(Mathf.Max(0f, floorSize.x * 0.5f - 2.5f), Mathf.Max(0f, floorSize.y * 0.5f - 3.5f));
            for (int i = 0; i < SteamCount; i++)
            {
                var go = new GameObject("Steam");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = steamFrames[i % steamFrames.Length];
                renderer.sortingOrder = SteamOrder;
                steam.Add(renderer);
            }
        }

        bool InRoom(Component target)
        {
            Vector2 offset = target.transform.position - transform.position;
            return target.gameObject.scene == gameObject.scene && Mathf.Abs(offset.x) <= floorSize.x * 0.5f
                && Mathf.Abs(offset.y) <= floorSize.y * 0.5f;
        }

        void QuenchRain()
        {
            if (!Raining)
                return;
            // El reloj natural ya descuenta un segundo: solo se suma el desgaste extra para llegar a x3.
            foreach (var fire in GroundFire.All)
            {
                if (fire != null && fire.IsBurning && InRoom(fire))
                    fire.Quench((GroundQuenchSpeed - 1f) * Time.deltaTime);
            }
            wetElapsed += Time.deltaTime;
            if (wetElapsed < RainCheckEvery)
                return;
            foreach (var burn in FindObjectsByType<PlayerBurn>())
            {
                if (InRoom(burn))
                    burn.Douse(RainDouseSpeed * wetElapsed);
            }
            wetElapsed = 0f;
        }

        void MoveClouds()
        {
            float alpha = calming ? Mathf.Clamp01(1f - (Time.time - calmStarted) / CloudFadeSeconds) : 1f;
            if (alpha <= 0f)
            {
                foreach (var tile in clouds)
                    Destroy(tile.gameObject);
                clouds.Clear();
                return;
            }
            Vector2 step = Vector2.one.normalized * (CloudSpeed * Time.deltaTime);
            foreach (var tile in clouds)
            {
                Vector2 position = (Vector2)tile.transform.localPosition + step;
                position.x = Mathf.Repeat(position.x + cloudSpan.x * 0.5f, cloudSpan.x) - cloudSpan.x * 0.5f;
                position.y = Mathf.Repeat(position.y + cloudSpan.y * 0.5f, cloudSpan.y) - cloudSpan.y * 0.5f;
                tile.transform.localPosition = position;
                tile.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        void Update()
        {
            QuenchRain();
            MoveClouds();
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
            if (calming)
            {
                float elapsed = Time.time - calmStarted;
                if (Drizzling && elapsed >= DrizzleSeconds)
                {
                    Drizzling = false;
                    SetTiles(rain, false);
                }
                if (steam.Count > 0 && elapsed >= SteamSeconds)
                {
                    foreach (var renderer in steam)
                        Destroy(renderer.gameObject);
                    steam.Clear();
                }
            }
            Animate(rain, Drizzling ? drizzleFrames : rainFrames, RainFrameSeconds);
            Animate(steam, steamFrames, SteamFrameSeconds);
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
