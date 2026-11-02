using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El lodo en el cuerpo de quien se hunde en el lodo profundo del piso 3 (el arte del pulido): cada sprite del cuerpo se
    /// repinta con <see cref="MudBodyPaint"/> (el cuerpo baja lo hundido y el lodo le sube por encima con borde grumoso); un
    /// anillo de lodo rodea la superficie por delante y por detrás, salen burbujas y, al salir, al jugador le quedan manchas que
    /// gotean y se limpian al andar. Los enemigos usan la versión simple. Lo crea <see cref="MudSinkVisual.SetLevel"/>; sin el
    /// arte importado no se usa y queda la máscara de siempre.
    /// </summary>
    public class MudBodyVisual : MonoBehaviour
    {
        // Cuánto del alto del cuerpo tapa el lodo con el hundimiento al máximo, y qué parte de eso es hundirse de verdad.
        const float MaxCover = 0.85f;
        const float SinkShare = 0.5f;
        const float PixelsPerUnit = 16f;
        // Las manchas empiezan algo limpias y se limpian del todo tras andar esta distancia.
        const float StartClean = 0.25f;
        const float CleanDistance = 2.5f;
        const float FrameSeconds = 0.11f;
        const float RescanSeconds = 0.3f;
        const float BubbleEvery = 0.3f;
        const float DripEvery = 0.35f;
        const int EffectOrder = 3;

        /// <summary>Un sprite del cuerpo y su copia pintada.</summary>
        class Layer
        {
            public SpriteRenderer renderer;
            public Sprite source;
            public Sprite painted;
            public Texture2D texture;
            public Color32[] buffer;
            public int width, height;
            public Vector2 pivot;
            public float ppu;
            public MudBodyPaint.Data data;
        }

        readonly Dictionary<SpriteRenderer, Layer> layers = new();
        readonly List<SpriteRenderer> gone = new();
        MudBodyPaint.Tables tables;
        StatusMarkers markers;
        bool simple;
        float level;
        float peak;                       // lo más alto que llegó el lodo (en unidades), para las manchas
        bool drying;
        float clean;
        float walked;
        Vector2 lastPosition;
        float nextScan;
        float nextBubble;
        float nextDrip;

        SpriteRenderer ringFront, ringBack;
        Sprite[] ringFrames, ringBackFrames;
        Bounds bounds;
        float feetY, topY;
        bool hasBody;
        int sortingLayer, minOrder, topOrder;

        /// <summary>True si hay arte para pintar el lodo en el cuerpo.</summary>
        public static bool Ready
        {
            get
            {
                var art = PolishArt.Current;
                return art != null && art.mudTexture != null && art.mudNoise != null && art.mudRingFront.m.Length > 0
                       && art.mudRingBack.m.Length > 0;
            }
        }

        /// <summary>Hundimiento visible, de 0 (nada) a 1 (tapado del todo). Con las manchas secándose, 0.</summary>
        public float Level => level;

        public bool IsDrying => drying;

        /// <summary>Lo llama <see cref="MudSinkVisual.SetLevel"/>. Con 0, el jugador queda manchado y limpiándose.</summary>
        public static void SetLevel(Transform root, float level)
        {
            var visual = root.GetComponent<MudBodyVisual>();
            if (visual == null)
            {
                if (level <= 0f)
                    return;
                visual = root.gameObject.AddComponent<MudBodyVisual>();
            }
            visual.Apply(Mathf.Clamp01(level));
        }

        void Awake()
        {
            markers = GetComponent<StatusMarkers>();
            // El jugador deja manchas y lleva la textura entera; los enemigos, la versión simple.
            simple = GetComponent<PlayerController>() == null;
            var art = PolishArt.Current;
            tables = MudBodyPaint.TablesFrom(art.mudNoise, art.mudTexture);
        }

        void Apply(float newLevel)
        {
            if (newLevel > 0f)
            {
                level = newLevel;
                if (drying)
                    drying = false;
                return;
            }

            level = 0f;
            // Sin manchas que dejar (enemigos o casi nada de lodo), se acabó.
            if (simple || peak <= 0.05f || tables == null)
            {
                Destroy(this);
                return;
            }
            if (!drying)
            {
                drying = true;
                clean = StartClean;
                walked = 0f;
                lastPosition = transform.position;
            }
        }

        void LateUpdate()
        {
            if (tables == null)
            {
                Destroy(this);
                return;
            }
            if (TryGetComponent<Health>(out var health) && health.IsDead && drying)
            {
                Destroy(this);
                return;
            }

            if (Time.time >= nextScan)
                Scan();
            if (!Measure())
                return;

            if (drying)
            {
                walked += Vector2.Distance(lastPosition, transform.position);
                lastPosition = transform.position;
                clean = Mathf.Clamp01(StartClean + (1f - StartClean) * walked / CleanDistance);
                if (clean >= 1f)
                {
                    Destroy(this);
                    return;
                }
            }
            else
                peak = Mathf.Max(peak, level * MaxCover * (topY - feetY));

            PaintLayers();
            PlaceEffects();
        }

        void OnDestroy() => Release();

        void OnDisable() => Release();

        // ---------------------------------------------------------------------------------------------
        // El cuerpo
        // ---------------------------------------------------------------------------------------------
        /// <summary>Recoge los sprites del cuerpo (pueden cambiar: mutaciones, efectos).</summary>
        void Scan()
        {
            nextScan = Time.time + RescanSeconds;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (!layers.ContainsKey(renderer) && IsBody(renderer))
                    layers[renderer] = new Layer { renderer = renderer };
            }

            gone.Clear();
            foreach (var renderer in layers.Keys)
            {
                if (renderer == null)
                    gone.Add(renderer);
            }
            foreach (var renderer in gone)
                layers.Remove(renderer);
        }

        bool IsBody(SpriteRenderer renderer)
        {
            string name = renderer.gameObject.name;
            if (name == "BurnTint" || name.Contains("Shadow"))
                return false;
            return markers == null || !markers.IsMarker(renderer.transform);
        }

        /// <summary>Dónde están los pies y la cabeza ahora (con los dibujos, no con la celda). False si no hay cuerpo a la vista.</summary>
        bool Measure()
        {
            hasBody = false;
            feetY = float.MaxValue;
            topY = float.MinValue;
            minOrder = int.MaxValue;
            topOrder = int.MinValue;
            foreach (var layer in layers.Values)
            {
                var renderer = layer.renderer;
                if (renderer == null || !renderer.enabled)
                    continue;
                var current = renderer.sprite == layer.painted && layer.painted != null ? layer.source : renderer.sprite;
                layer.source = current;
                layer.data = MudBodyPaint.DataOf(current);
                if (layer.data == null)
                    continue;

                var box = renderer.bounds;
                float perPixel = box.size.y / layer.data.height;
                feetY = Mathf.Min(feetY, box.min.y + layer.data.minRow * perPixel);
                topY = Mathf.Max(topY, box.min.y + (layer.data.maxRow + 1) * perPixel);
                if (hasBody)
                    bounds.Encapsulate(box);
                else
                {
                    bounds = box;
                    hasBody = true;
                }
                if (renderer.sortingOrder > topOrder)
                {
                    topOrder = renderer.sortingOrder;
                    sortingLayer = renderer.sortingLayerID;
                }
                minOrder = Mathf.Min(minOrder, renderer.sortingOrder);
            }
            return hasBody && topY > feetY;
        }

        // ---------------------------------------------------------------------------------------------
        // Pintar
        // ---------------------------------------------------------------------------------------------
        float ReferenceHeight => topY - feetY;

        void PaintLayers()
        {
            int t = Mathf.FloorToInt(Time.time / FrameSeconds);
            float levelWorld = level * MaxCover * ReferenceHeight;
            foreach (var layer in layers.Values)
            {
                var renderer = layer.renderer;
                if (renderer == null || layer.data == null || layer.source == null || !renderer.enabled)
                    continue;

                float perPixel = renderer.bounds.size.y / layer.data.height;
                var settings = new MudBodyPaint.Settings
                {
                    level = levelWorld / perPixel,
                    stain = simple ? 0f : (drying ? peak / perPixel : 0f),
                    clean = clean,
                    sunk = Mathf.FloorToInt(levelWorld * SinkShare / perPixel),
                    feetRow = Mathf.RoundToInt((feetY - renderer.bounds.min.y) / perPixel),
                    t = t,
                    simple = simple,
                };
                if (!EnsureSprite(layer))
                    continue;
                MudBodyPaint.Paint(layer.data, layer.buffer, settings, tables);
                layer.texture.SetPixels32(layer.buffer);
                layer.texture.Apply(false);
                renderer.sprite = layer.painted;
            }
        }

        /// <summary>La copia pintada del sprite actual (se rehace si cambia el tamaño o el pivote).</summary>
        static bool EnsureSprite(Layer layer)
        {
            var source = layer.source;
            int w = layer.data.width, h = layer.data.height;
            var pivot = new Vector2(source.pivot.x / w, source.pivot.y / h);
            if (layer.painted != null && layer.width == w && layer.height == h && layer.pivot == pivot
                && Mathf.Approximately(layer.ppu, source.pixelsPerUnit))
                return true;

            DestroyPainted(layer);
            layer.texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            layer.buffer = new Color32[w * h];
            layer.painted = Sprite.Create(layer.texture, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit);
            layer.width = w;
            layer.height = h;
            layer.pivot = pivot;
            layer.ppu = source.pixelsPerUnit;
            return true;
        }

        static void DestroyPainted(Layer layer)
        {
            if (layer.painted != null)
                Destroy(layer.painted);
            if (layer.texture != null)
                Destroy(layer.texture);
            layer.painted = null;
            layer.texture = null;
        }

        // ---------------------------------------------------------------------------------------------
        // Anillo, burbujas y goterones
        // ---------------------------------------------------------------------------------------------
        void PlaceEffects()
        {
            var art = PolishArt.Current;
            float widthPixels = bounds.size.x * PixelsPerUnit;
            int sunk = Mathf.FloorToInt(level * MaxCover * ReferenceHeight * SinkShare * PixelsPerUnit);

            // El anillo de la superficie, mientras el cuerpo está hundido.
            if (sunk > 0)
            {
                EnsureRing(art, widthPixels);
                var position = new Vector3(bounds.center.x, feetY, 0f);
                ringBack.transform.position = position;
                ringFront.transform.position = position;
                ringBack.sortingLayerID = ringFront.sortingLayerID = sortingLayer;
                ringBack.sortingOrder = minOrder - 1;
                ringFront.sortingOrder = topOrder + 1;
                ringBack.enabled = ringFront.enabled = true;
            }
            else if (ringFront != null)
                ringBack.enabled = ringFront.enabled = false;

            float coverWorld = level * MaxCover * ReferenceHeight * (1f - SinkShare);
            if (level > 0f && coverWorld > 2f / PixelsPerUnit && Time.time >= nextBubble)
            {
                nextBubble = Time.time + BubbleEvery;
                var at = new Vector2(bounds.center.x + (Random.value - 0.5f) * bounds.size.x * 0.7f, feetY + coverWorld - 0.1f);
                Effect(art.mudBubble, at, 0.07f);
            }

            if (drying && !simple && Time.time >= nextDrip)
            {
                nextDrip = Time.time + DripEvery * Mathf.Lerp(1f, 3f, clean);
                var at = new Vector2(bounds.center.x + (Random.value - 0.5f) * bounds.size.x * 0.7f, feetY + peak * (0.3f + 0.4f * Random.value));
                Effect(art.mudDrip, at, 0.08f);
            }
        }

        void Effect(Sprite[] frames, Vector2 position, float frameSeconds) =>
            SpriteFramePlayer.PlayOnce(frames, frameSeconds, position, topOrder + EffectOrder);

        void EnsureRing(PolishArt art, float widthPixels)
        {
            if (ringFront != null)
                return;
            // Del ancho del cuerpo: pequeño, mediano o grande.
            var front = widthPixels < 24f ? art.mudRingFront.s : widthPixels < 37f ? art.mudRingFront.m : art.mudRingFront.l;
            var back = widthPixels < 24f ? art.mudRingBack.s : widthPixels < 37f ? art.mudRingBack.m : art.mudRingBack.l;
            ringFrames = front.Length > 0 ? front : art.mudRingFront.m;
            ringBackFrames = back.Length > 0 ? back : art.mudRingBack.m;
            ringBack = NewRing("MudRing_Back", ringBackFrames);
            ringFront = NewRing("MudRing_Front", ringFrames);
        }

        static SpriteRenderer NewRing(string name, Sprite[] frames)
        {
            // Fuera del cuerpo: si fuera hijo heredaría su escala y su volteo.
            var go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            go.AddComponent<SpriteFramePlayer>().Play(renderer, frames, 0.12f, newLoop: true);
            return renderer;
        }

        // ---------------------------------------------------------------------------------------------
        // Dejarlo todo como estaba
        // ---------------------------------------------------------------------------------------------
        void Release()
        {
            foreach (var layer in layers.Values)
            {
                if (layer.renderer != null && layer.painted != null && layer.renderer.sprite == layer.painted)
                    layer.renderer.sprite = layer.source;
                DestroyPainted(layer);
            }
            layers.Clear();
            if (ringFront != null)
                Destroy(ringFront.gameObject);
            if (ringBack != null)
                Destroy(ringBack.gameObject);
            ringFront = ringBack = null;
        }
    }
}
