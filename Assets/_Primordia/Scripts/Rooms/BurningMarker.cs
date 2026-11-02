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
            if (!hasBounds)
                return;

            Place();
            Tint();
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
            float shape = height * 1000f + bounds.size.x * PixelsPerUnit;
            if (!Mathf.Approximately(shape, sizePixels))
            {
                sizePixels = shape;
                Rebuild(height, bounds.size.x * PixelsPerUnit);
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

            var art = PolishArt.Current;
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

        void Place()
        {
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
