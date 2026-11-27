using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// La marca de «arde» del piso 3, para el jugador y para los enemigos (va en el prefab Effect_Burning, que lleva
    /// <see cref="StatusMarkers"/>): llamas del tamaño del cuerpo pegadas a sus pies (una por cada tantos píxeles de ancho),
    /// unas lenguas sueltas por la parte de arriba y un velo naranja que parpadea sobre cada sprite del cuerpo (una copia
    /// translúcida de su sprite, así no choca con el destello de daño). Con el arte del pulido sin importar, deja el dibujo de
    /// llama de siempre.
    /// </summary>
    public class BurningMarker : MonoBehaviour
    {
        const float PixelsPerUnit = 16f;
        const float FrameSeconds = 0.08f;
        const float LickFrameSeconds = 0.07f;
        const float RescanSeconds = 0.3f;
        // Cuánto naranja lleva el velo (parpadea entre los dos) y de qué color (el amarillo del fuego, sumado a poca opacidad).
        const float TintMin = 0.10f;
        const float TintMax = 0.22f;
        static readonly Color TintColor = new(0.97f, 0.55f, 0.16f, 1f);
        // La base de las llamas, respecto a los pies del cuerpo (la hoja deja ~1,5 px de margen abajo).
        const float FeetDrop = 1.5f / PixelsPerUnit;
        // Lenguas: posición relativa dentro del cuerpo (x de −½ a ½ del ancho, y de 0 a 1 del alto) y desfase de su animación.
        static readonly (Vector2 at, float phase)[] Licks =
        {
            (new Vector2(-0.32f, 0.62f), 0f), (new Vector2(0.05f, 0.85f), 0.21f), (new Vector2(0.34f, 0.5f), 0.4f),
        };

        class Flame
        {
            public SpriteRenderer renderer;
            public Sprite[] frames;
            public float seconds;
            public float phase;
            public float offsetX;
            public Vector2 lick;
            public bool isLick;
        }

        // Cuerpos grandes (jefes): en lugar de una fila de la misma llama en los pies, llamas sueltas repartidas por la superficie.
        const float BigHeightPixels = 36f;
        const float BigWidthPixels = 56f;
        const int BigPieces = 4;
        const float AreaPerFlamePixels = 900f;
        const int MinFlames = 5;
        const int MaxFlames = 14;
        const float FadeIn = 0.25f;
        const float FadeOut = 0.35f;
        static readonly Vector2 PatchLife = new(1.4f, 2.8f);

        class Patch
        {
            public SpriteRenderer renderer;
            public SpriteRenderer source;
            public bool lick;
            public Sprite[] frames;
            public float seconds;
            public float phase;
            // Dónde se agarra, en unidades locales de la pieza (sin voltear): siempre sobre píxeles dibujados.
            public Vector2 anchor;
            public float born, until;
        }

        readonly List<Patch> patches = new();
        readonly List<SpriteRenderer> candidates = new();
        bool big;

        readonly Dictionary<SpriteRenderer, SpriteRenderer> tints = new();
        readonly List<SpriteRenderer> dead = new();
        readonly List<Flame> flames = new();
        Transform owner;
        StatusMarkers markers;
        bool hasArt;
        float nextScan;
        float sizePixels = -1f;
        int sortingLayer;
        int topOrder;
        Bounds bounds;
        bool hasBounds;

        void Start()
        {
            markers = GetComponentInParent<StatusMarkers>();
            owner = markers != null ? markers.transform : transform.parent;
            var art = PolishArt.Current;
            hasArt = owner != null && art != null && art.bodyFlame.m.Length > 0;
            if (!hasArt)
                return;

            // Con el arte nuevo, el dibujo de llama de siempre sobra.
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;
            Scan();
        }

        void LateUpdate()
        {
            if (!hasArt)
                return;
            if (Time.time >= nextScan)
                Scan();
            else
                UpdateBounds();
            // Sin cuerpo a la vista (la Meganeura que vuela alto y solo deja su sombra), tampoco se ven las llamas ni el velo:
            // el velo es un sprite aparte que, si no se apaga, se queda dibujado aunque el cuerpo ya no esté.
            SetVisible(hasBounds);
            if (!hasBounds)
                return;

            Place();
            Tint();
        }

        void SetVisible(bool visible)
        {
            foreach (var flame in flames)
            {
                if (flame.renderer != null)
                    flame.renderer.enabled = visible;
            }
            if (!visible)
            {
                foreach (var patch in patches)
                {
                    if (patch.renderer != null)
                        patch.renderer.enabled = false;
                }
            }
            if (visible)
                return;
            foreach (var tint in tints.Values)
            {
                if (tint != null)
                    tint.enabled = false;
            }
        }

        void OnDestroy()
        {
            foreach (var tint in tints.Values)
            {
                if (tint != null)
                    Destroy(tint.gameObject);
            }
            tints.Clear();
        }

        /// <summary>Busca los sprites del cuerpo (para el velo y el tamaño) y rehace las llamas si el cuerpo cambió de tamaño.</summary>
        void Scan()
        {
            nextScan = Time.time + RescanSeconds;
            if (owner == null)
                return;

            dead.Clear();
            foreach (var pair in tints)
            {
                if (pair.Key == null)
                    dead.Add(pair.Key);
            }
            foreach (var key in dead)
            {
                if (tints[key] != null)
                    Destroy(tints[key].gameObject);
                tints.Remove(key);
            }

            foreach (var renderer in owner.GetComponentsInChildren<SpriteRenderer>())
            {
                if (IsBody(renderer) && !tints.ContainsKey(renderer))
                    tints[renderer] = NewTint(renderer);
            }

            UpdateBounds();
            if (!hasBounds)
                return;
            // Las llamas se rehacen solo si el cuerpo cambia de talla (S, M o L) o de cuántas caben a lo ancho.
            float height = bounds.size.y * PixelsPerUnit;
            float width = bounds.size.x * PixelsPerUnit;
            bool wantBig = height > BigHeightPixels || width > BigWidthPixels || tints.Count >= BigPieces;
            float shape = wantBig ? 1e9f + Mathf.Round(BodyAreaPixels() / 300f) : height * 1000f + width;
            if (!Mathf.Approximately(shape, sizePixels) || wantBig != big)
            {
                sizePixels = shape;
                big = wantBig;
                Rebuild(height, width);
            }
        }

        /// <summary>Dónde está el cuerpo ahora mismo (cada cuadro: el cuerpo se mueve y las llamas tienen que ir con él).</summary>
        void UpdateBounds()
        {
            hasBounds = false;
            topOrder = int.MinValue;
            foreach (var renderer in tints.Keys)
            {
                if (renderer == null || renderer.sprite == null || !renderer.enabled)
                    continue;
                if (hasBounds)
                    bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                if (renderer.sortingOrder > topOrder)
                {
                    topOrder = renderer.sortingOrder;
                    sortingLayer = renderer.sortingLayerID;
                }
            }
        }

        /// <summary>Un sprite del cuerpo: ni de este marcador, ni de otra marca, ni la sombra, ni un velo.</summary>
        bool IsBody(SpriteRenderer renderer)
        {
            if (renderer.transform.IsChildOf(transform) || renderer.gameObject.name == TintName)
                return false;
            if (renderer.gameObject.name.Contains("Shadow"))
                return false;
            return markers == null || !markers.IsMarker(renderer.transform);
        }

        const string TintName = "BurnTint";

        static SpriteRenderer NewTint(SpriteRenderer source)
        {
            var go = new GameObject(TintName);
            go.transform.SetParent(source.transform, false);
            return go.AddComponent<SpriteRenderer>();
        }

        void Rebuild(float heightPixels, float widthPixels)
        {
            foreach (var flame in flames)
            {
                if (flame.renderer != null)
                    Destroy(flame.renderer.gameObject);
            }
            flames.Clear();
            foreach (var patch in patches)
            {
                if (patch.renderer != null)
                    Destroy(patch.renderer.gameObject);
            }
            patches.Clear();

            var art = PolishArt.Current;
            if (big)
            {
                BuildPatches(art);
                return;
            }
            var frames = art.bodyFlame.ForHeight(heightPixels);
            if (frames.Length == 0)
                return;

            // Una llama por cada tantos píxeles de ancho (un jefe grande lleva varias seguidas).
            float flameWidth = frames[0].rect.width;
            int count = Mathf.Max(1, Mathf.RoundToInt(widthPixels / flameWidth));
            for (int i = 0; i < count; i++)
            {
                flames.Add(NewFlame("BodyFlame", frames, FrameSeconds, i * 0.037f,
                    (i - (count - 1) / 2f) * flameWidth / PixelsPerUnit, Vector2.zero, false));
            }

            if (art.flameLick.Length == 0)
                return;
            foreach (var (at, phase) in Licks)
                flames.Add(NewFlame("FlameLick", art.flameLick, LickFrameSeconds, phase, 0f, at, true));
        }

        Flame NewFlame(string name, Sprite[] frames, float seconds, float phase, float offsetX, Vector2 lick, bool isLick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            return new Flame { renderer = renderer, frames = frames, seconds = seconds, phase = phase, offsetX = offsetX, lick = lick, isLick = isLick };
        }

        /// <summary>Superficie que cubren los sprites del cuerpo (en píxeles²): decide cuántas llamas lleva un cuerpo grande.</summary>
        float BodyAreaPixels()
        {
            float area = 0f;
            foreach (var renderer in tints.Keys)
            {
                if (renderer == null || renderer.sprite == null || !renderer.enabled)
                    continue;
                var size = renderer.bounds.size;
                area += size.x * size.y * PixelsPerUnit * PixelsPerUnit;
            }
            return area;
        }

        void BuildPatches(PolishArt art)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(BodyAreaPixels() / AreaPerFlamePixels), MinFlames, MaxFlames);
            int licks = art.flameLick.Length > 0 ? Mathf.Clamp(count / 2, 2, 6) : 0;
            for (int i = 0; i < count + licks; i++)
            {
                var go = new GameObject(i < count ? "BodyFlame" : "FlameLick");
                go.transform.SetParent(transform, false);
                var patch = new Patch { renderer = go.AddComponent<SpriteRenderer>(), lick = i >= count };
                // Cada una nace en un momento distinto, para que no parpadeen a la vez.
                patch.until = Time.time + Random.Range(0f, PatchLife.y);
                patch.born = patch.until - Random.Range(PatchLife.x, PatchLife.y);
                patches.Add(patch);
            }
        }

        /// <summary>Elige otra pieza del cuerpo, otro punto, otro tamaño y otra velocidad para esa llama.</summary>
        void Reroll(Patch patch)
        {
            patch.born = Time.time;
            patch.until = Time.time + Random.Range(PatchLife.x, PatchLife.y);
            patch.phase = Random.value;
            patch.renderer.flipX = Random.value < 0.5f;

            candidates.Clear();
            float total = 0f;
            foreach (var renderer in tints.Keys)
            {
                // Las piezas casi transparentes (brillos, avisos) o sin dibujo no llevan llamas.
                if (renderer == null || !renderer.enabled || renderer.color.a < 0.5f
                    || SpriteBodyPoints.For(renderer.sprite).Length == 0)
                    continue;
                candidates.Add(renderer);
                total += renderer.bounds.size.x * renderer.bounds.size.y;
            }
            patch.source = null;
            if (candidates.Count == 0)
                return;

            // Más probable en las piezas grandes (cuerpo de una pieza: siempre en ella).
            float pick = Random.value * total;
            patch.source = candidates[candidates.Count - 1];
            foreach (var renderer in candidates)
            {
                pick -= renderer.bounds.size.x * renderer.bounds.size.y;
                if (pick <= 0f)
                {
                    patch.source = renderer;
                    break;
                }
            }

            var art = PolishArt.Current;
            float pieceHeight = patch.source.bounds.size.y * PixelsPerUnit;
            // De dos puntos del dibujo, las llamas se quedan con el más bajo y las lenguas con el más alto.
            var points = SpriteBodyPoints.For(patch.source.sprite);
            Vector2 first = points[Random.Range(0, points.Length)], second = points[Random.Range(0, points.Length)];
            patch.anchor = (first.y > second.y) == patch.lick ? first : second;
            if (patch.lick)
            {
                patch.frames = art.flameLick;
                patch.seconds = LickFrameSeconds * Random.Range(0.85f, 1.2f);
                return;
            }
            // El tamaño sigue a la pieza, con algo de variedad: las grandes llevan sobre todo L, mezcladas con M.
            Sprite[] frames = art.bodyFlame.ForHeight(pieceHeight);
            if (pieceHeight > 26f && Random.value < 0.4f)
                frames = art.bodyFlame.m;
            else if (pieceHeight >= 14f && Random.value < 0.2f)
                frames = art.bodyFlame.s;
            patch.frames = frames.Length > 0 ? frames : art.bodyFlame.m;
            patch.seconds = FrameSeconds * Random.Range(0.85f, 1.2f);
        }

        void PlacePatches()
        {
            foreach (var patch in patches)
            {
                if (patch.renderer == null)
                    continue;
                if (patch.source == null || !patch.source.enabled || SpriteBodyPoints.For(patch.source.sprite).Length == 0
                    || Time.time >= patch.until)
                    Reroll(patch);
                if (patch.source == null || patch.frames == null || patch.frames.Length == 0)
                {
                    patch.renderer.enabled = false;
                    continue;
                }
                patch.renderer.enabled = true;
                var position = OnBody(patch.source, patch.anchor);
                if (!patch.lick)
                    position.y -= FeetDrop;
                patch.renderer.transform.position = position;

                // Nace y se apaga con un fundido, para que al cambiar de sitio no salte.
                float age = Time.time - patch.born, life = patch.until - patch.born;
                float fade = Mathf.Clamp01(Mathf.Min(age / FadeIn, (life - age) / FadeOut));
                patch.renderer.color = new Color(1f, 1f, 1f, fade);
                patch.renderer.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, fade);

                int index = Mathf.FloorToInt((Time.time + patch.phase) / patch.seconds) % patch.frames.Length;
                patch.renderer.sprite = patch.frames[index];
                patch.renderer.sortingLayerID = patch.source.sortingLayerID;
                patch.renderer.sortingOrder = patch.source.sortingOrder + (patch.lick ? 3 : 2);
            }
        }

        /// <summary>
        /// El punto dibujado del fotograma actual más cercano al ancla, ya en el mundo (con volteo, giro y escala): así la
        /// llama sigue pegada al cuerpo aunque la animación mueva las patas o las alas.
        /// </summary>
        static Vector3 OnBody(SpriteRenderer source, Vector2 anchor)
        {
            var nearest = anchor;
            float best = float.MaxValue;
            foreach (var point in SpriteBodyPoints.For(source.sprite))
            {
                float distance = (point - anchor).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = point;
                }
            }
            if (source.flipX)
                nearest.x = -nearest.x;
            if (source.flipY)
                nearest.y = -nearest.y;
            var world = source.transform.TransformPoint(nearest);
            world.z = 0f;
            return world;
        }

        void Place()
        {
            if (big)
            {
                PlacePatches();
                return;
            }
            var feet = new Vector2(bounds.center.x, bounds.min.y - FeetDrop);
            foreach (var flame in flames)
            {
                if (flame.renderer == null)
                    continue;
                if (flame.isLick)
                {
                    // Las lenguas solo nacen en el contorno de arriba: se reparten por el cuerpo.
                    flame.renderer.transform.position = new Vector3(
                        bounds.center.x + flame.lick.x * bounds.size.x, bounds.min.y + flame.lick.y * bounds.size.y, 0f);
                }
                else
                    flame.renderer.transform.position = new Vector3(feet.x + flame.offsetX, feet.y, 0f);

                int index = Mathf.FloorToInt((Time.time + flame.phase) / flame.seconds) % flame.frames.Length;
                flame.renderer.sprite = flame.frames[index];
                flame.renderer.sortingLayerID = sortingLayer;
                flame.renderer.sortingOrder = topOrder + (flame.isLick ? 3 : 2);
            }
        }

        void Tint()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f) + 0.15f * Mathf.Sin(Time.time * 31f);
            float amount = Mathf.Lerp(TintMin, TintMax, Mathf.Clamp01(pulse));
            foreach (var (source, tint) in tints)
            {
                if (source == null || tint == null)
                    continue;
                tint.enabled = source.enabled && source.sprite != null;
                tint.sprite = source.sprite;
                tint.flipX = source.flipX;
                tint.flipY = source.flipY;
                tint.sortingLayerID = source.sortingLayerID;
                tint.sortingOrder = source.sortingOrder + 1;
                tint.maskInteraction = source.maskInteraction;
                tint.color = new Color(TintColor.r, TintColor.g, TintColor.b, amount * source.color.a);
            }
        }
    }
}
