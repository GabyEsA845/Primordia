using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Bosque en humo (sala especial del piso 3): dos capas de humo que se repiten sin costura y van a la deriva cubren la sala. Cada capa
    /// es una malla con una rejilla de vértices de medio tile y color (alfa) por vértice: el humo se desvanece con suavidad en el borde
    /// de la sala (no se ve desde la sala de al lado), alrededor del jugador (un claro de visión) y alrededor de cada veta de carbón
    /// encendida, que lo dispersa. Solo se ve con el jugador dentro de la sala. Por encima del humo, volutas y ascuas. Es solo visión:
    /// no daña ni frena.
    /// </summary>
    public class SmokeVision : MonoBehaviour
    {
        const float TileWorld = 8f;
        // Un vértice cada medio tile; en salas grandes, cada tile (la malla sigue siendo suave: se interpola).
        const float FineStep = 0.5f;
        const float CoarseStep = 1f;
        const float CoarseAbove = 900f;
        const int OrderA = 40;
        const int OrderB = 41;
        const int OrderWisp = 42;
        const int OrderEmbers = 43;
        const float WispEvery = 4f;
        const float EmberFrameSeconds = 0.14f;
        // Cuánto sobresale la malla por cada lado de la sala (para que el desvanecido del borde caiga fuera del suelo).
        const float Margin = 2f;
        // El borde de la sala: el humo es 0 medio tile fuera de ella y llega a su fuerza completa 1,5 tiles dentro.
        const float EdgeStart = -0.5f;
        const float EdgeEnd = 1.5f;
        // Claro del jugador: del todo despejado hasta ClearIn, con humo completo a partir de ClearOut.
        const float PlayerClearIn = 1.8f;
        const float PlayerClearOut = 5.5f;
        // Claro de una veta encendida.
        const float LightClearIn = 1.2f;
        const float LightClearOut = 4.2f;
        const float FadeSpeed = 1.6f;

        class Layer
        {
            public Mesh mesh;
            public Vector3[] vertices;
            public Vector2[] uvs;
            public Color[] colors;
            public Vector2 drift;
            public Vector2 velocity;
            public float alpha;
        }

        SpecialRoomArt art;
        Transform player;
        RoomPresence presence;
        Bounds area;
        Vector2[] lights = System.Array.Empty<Vector2>();
        readonly List<Layer> layers = new();
        readonly List<(SpriteRenderer renderer, float phase)> embers = new();
        float step = FineStep;
        int columns;
        int rows;
        float shown;
        float nextWisp;

        /// <summary>0 = sin humo, 1 = el humo de siempre (sube con el jugador dentro y baja al salir).</summary>
        public float Shown => shown;

        internal static SmokeVision Create(Transform parent, Bounds roomBounds, SpecialRoomArt art, IEnumerable<Vector2> lights, RoomPresence presence)
        {
            var go = new GameObject("SmokeVision");
            go.transform.SetParent(parent, false);
            var smoke = go.AddComponent<SmokeVision>();
            smoke.art = art;
            smoke.area = roomBounds;
            smoke.presence = presence;
            smoke.lights = new List<Vector2>(lights).ToArray();
            smoke.Build();
            return smoke;
        }

        void Build()
        {
            var playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;

            // Rejilla común: de la sala más el margen, con un vértice cada medio tile.
            var min = new Vector2(area.min.x - Margin, area.min.y - Margin);
            step = area.size.x * area.size.y > CoarseAbove ? CoarseStep : FineStep;
            columns = Mathf.CeilToInt((area.size.x + Margin * 2f) / step) + 1;
            rows = Mathf.CeilToInt((area.size.y + Margin * 2f) / step) + 1;

            layers.Add(MakeLayer("SmokeA", art.smokeA, OrderA, min, 1f, new Vector2(0.35f, 0.1f)));
            if (art.smokeB != null)
                layers.Add(MakeLayer("SmokeB", art.smokeB, OrderB, min, 0.6f, new Vector2(0.55f, -0.12f)));

            BuildEmbers();
            nextWisp = Time.time + Random.Range(1f, WispEvery);
            Refresh();
        }

        Layer MakeLayer(string layerName, Sprite sprite, int order, Vector2 min, float alpha, Vector2 velocity)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            // La malla se dibuja en coordenadas del mundo.
            go.transform.position = Vector3.zero;

            // El mosaico se repite: hace falta que la textura envuelva.
            var texture = sprite.texture;
            texture.wrapMode = TextureWrapMode.Repeat;
            var material = new Material(art.smokeMaterial) { mainTexture = texture };

            var layer = new Layer { alpha = alpha, velocity = velocity };
            int count = columns * rows;
            layer.vertices = new Vector3[count];
            layer.uvs = new Vector2[count];
            layer.colors = new Color[count];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                    layer.vertices[y * columns + x] = new Vector3(min.x + x * step, min.y + y * step, 0f);
            }

            var triangles = new int[(columns - 1) * (rows - 1) * 6];
            int t = 0;
            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < columns - 1; x++)
                {
                    int i = y * columns + x;
                    triangles[t++] = i;
                    triangles[t++] = i + columns;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + columns;
                    triangles[t++] = i + columns + 1;
                }
            }

            layer.mesh = new Mesh { name = layerName };
            layer.mesh.MarkDynamic();
            layer.mesh.vertices = layer.vertices;
            layer.mesh.triangles = triangles;
            layer.mesh.bounds = new Bounds(area.center, area.size + Vector3.one * (Margin * 2f + 1f));
            go.AddComponent<MeshFilter>().sharedMesh = layer.mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return layer;
        }

        void BuildEmbers()
        {
            if (art.floatingEmbers.Length == 0)
                return;
            int count = Mathf.Clamp(Mathf.RoundToInt(area.size.x * area.size.y / 40f), 6, 40);
            var random = new System.Random(Mathf.RoundToInt(area.center.x * 31f + area.center.y * 17f));
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("FloatingEmber");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector2(Mathf.Lerp(area.min.x + 1f, area.max.x - 1f, (float)random.NextDouble()),
                    Mathf.Lerp(area.min.y + 3f, area.max.y - 1f, (float)random.NextDouble()));
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = OrderEmbers;
                embers.Add((renderer, (float)random.NextDouble() * 8f));
            }
        }

        void Update()
        {
            if (player == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            bool inside = presence != null && presence.PlayerInside;
            shown = Mathf.MoveTowards(shown, inside ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
            bool visible = shown > 0.001f;
            SetVisible(visible);
            if (!visible)
                return;

            if (Time.timeScale > 0f)
            {
                foreach (var layer in layers)
                    layer.drift += layer.velocity * Time.deltaTime;
            }
            Refresh();

            foreach (var (renderer, phase) in embers)
                renderer.sprite = art.floatingEmbers[Mathf.FloorToInt((Time.time + phase) / EmberFrameSeconds) % art.floatingEmbers.Length];

            if (shown >= 1f && Time.time >= nextWisp && art.wisp.Length > 0)
            {
                nextWisp = Time.time + Random.Range(WispEvery * 0.6f, WispEvery * 1.4f);
                var spot = new Vector2(Random.Range(area.min.x + 1f, area.max.x - 1f), Random.Range(area.min.y + 3f, area.max.y - 1f));
                FrameEffect.Play("SmokeWisp", art.wisp, 0.14f, OrderWisp, null, spot);
            }
        }

        void SetVisible(bool visible)
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<MeshRenderer>(out var mesh))
                    mesh.enabled = visible;
                else if (child.TryGetComponent<SpriteRenderer>(out var sprite))
                    sprite.enabled = visible;
            }
        }

        // Recalcula el alfa de cada vértice (borde de la sala, claro del jugador y claros de las vetas) y las coordenadas del mosaico.
        void Refresh()
        {
            Vector2 playerAt = player != null ? (Vector2)player.position : new Vector2(float.MaxValue / 4f, float.MaxValue / 4f);
            var min = area.min;
            var max = area.max;
            foreach (var layer in layers)
            {
                for (int i = 0; i < layer.vertices.Length; i++)
                {
                    Vector2 point = layer.vertices[i];
                    layer.uvs[i] = (point + layer.drift) / TileWorld;

                    // Cuánto entra en la sala (negativo fuera).
                    float inside = Mathf.Min(Mathf.Min(point.x - min.x, max.x - point.x), Mathf.Min(point.y - min.y, max.y - point.y));
                    float strength = Smooth(EdgeStart, EdgeEnd, inside);
                    if (strength > 0f)
                    {
                        strength *= Smooth(PlayerClearIn, PlayerClearOut, Vector2.Distance(point, playerAt));
                        foreach (var light in lights)
                        {
                            if (strength <= 0f)
                                break;
                            strength *= Smooth(LightClearIn, LightClearOut, Vector2.Distance(point, light));
                        }
                    }
                    layer.colors[i] = new Color(1f, 1f, 1f, strength * layer.alpha * shown);
                }
                layer.mesh.uv = layer.uvs;
                layer.mesh.colors = layer.colors;
            }
        }

        // Subida suave de 0 (en a) a 1 (en b).
        static float Smooth(float a, float b, float value)
        {
            float t = Mathf.Clamp01((value - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        void OnDestroy()
        {
            foreach (var layer in layers)
            {
                if (layer.mesh != null)
                    Destroy(layer.mesh);
            }
        }
    }
}
