using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Red del nivel III de las Patas hilanderas: al tender el tercer hilo, los extremos de los tres se unen formando un polígono
    /// (el contorno convexo de los seis extremos) y una telaraña cubre todo lo de dentro. Cada enemigo atrapado recibe daño una
    /// vez y queda inmovilizado. Ver <see cref="SilkThreads"/>.
    /// </summary>
    public class SilkNet : MonoBehaviour
    {
        const float CheckEvery = 0.1f;

        readonly HashSet<Health> caught = new();
        List<Vector2> polygon;
        SilkNetArt.Frames frames;
        int damage;
        float holdSeconds;
        float until;
        float nextCheck;

        /// <summary>Enemigos atrapados hasta ahora (para las pruebas).</summary>
        public int CaughtCount => caught.Count;

        public IReadOnlyList<Vector2> Polygon => polygon;

        /// <summary>Teje la red sobre ese contorno. Con menos de 3 puntos no hace nada. Los cuadros pueden faltar.</summary>
        public static SilkNet Spawn(IReadOnlyList<Vector2> hull, float seconds, int damage, float holdSeconds,
            Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            if (hull.Count < 3)
                return null;
            var obj = new GameObject("SilkNet");
            var net = obj.AddComponent<SilkNet>();
            net.polygon = new List<Vector2>(hull);
            net.damage = damage;
            net.holdSeconds = holdSeconds;
            net.until = Time.time + seconds;
            net.FillVisual(seconds, appear, idle, fade);
            return net;
        }

        /// <summary>
        /// La telaraña se teje (los radios crecen del centro, 0,4 s), brilla mientras dura y se deshilacha al final (0,6 s). Se dibuja por
        /// código con la forma exacta del polígono (<see cref="SilkNetArt"/>); los cuadros de las celdas sueltas ya no se usan.
        /// </summary>
        void FillVisual(float seconds, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            frames = SilkNetArt.Build(polygon, out var center);
            if (frames == null)
                return;
            var obj = new GameObject("NetWeb");
            obj.transform.SetParent(transform, false);
            obj.transform.position = center;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -1;
            renderer.sprite = frames[0];
            StartCoroutine(PlayWeb(renderer));
        }

        System.Collections.IEnumerator PlayWeb(SpriteRenderer renderer)
        {
            for (int i = 0; i < SilkNetArt.GrowFrames; i++)
            {
                renderer.sprite = frames[i];
                yield return new WaitForSeconds(SilkNetArt.GrowStep);
            }
            float fraySeconds = SilkNetArt.FrayFrames * SilkNetArt.FrayStep;
            int twinkle = 0;
            while (Time.time < until - fraySeconds)
            {
                renderer.sprite = frames[SilkNetArt.GrowFrames + twinkle % SilkNetArt.TwinkleFrames];
                twinkle++;
                yield return new WaitForSeconds(Mathf.Min(SilkNetArt.TwinkleStep, until - fraySeconds - Time.time));
            }
            for (int i = 0; i < SilkNetArt.FrayFrames; i++)
            {
                renderer.sprite = frames[SilkNetArt.GrowFrames + SilkNetArt.TwinkleFrames + i];
                yield return new WaitForSeconds(SilkNetArt.FrayStep);
            }
            renderer.sprite = null;
            ReleaseFrames();
        }

        void OnDestroy() => ReleaseFrames();

        void ReleaseFrames()
        {
            frames?.Dispose();
            frames = null;
        }

        /// <summary>True si el punto cae dentro del polígono (contorno convexo, en cualquier orden de giro).</summary>
        public bool Contains(Vector2 point)
        {
            bool positive = false, negative = false;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                float cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
                if (cross > 0f)
                    positive = true;
                else if (cross < 0f)
                    negative = true;
                if (positive && negative)
                    return false;
            }
            return true;
        }

        static readonly Collider2D[] hits = new Collider2D[32];

        void Update()
        {
            if (Time.time >= until)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;

            var center = Vector2.zero;
            foreach (var point in polygon)
                center += point;
            center /= polygon.Count;
            float reach = 0f;
            foreach (var point in polygon)
                reach = Mathf.Max(reach, Vector2.Distance(center, point));

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.OverlapCircle(center, reach + 0.5f, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || caught.Contains(health) || !Contains(health.transform.position))
                    continue;
                caught.Add(health);
                if (damage > 0)
                    health.TakeDamage(damage);
                if (!health.IsDead && health.TryGetComponent<EnemyAI>(out var ai))
                    ai.Knockback(Vector2.zero, holdSeconds);
            }
        }

        /// <summary>Contorno convexo de unos puntos (cadena monótona), en sentido antihorario.</summary>
        public static List<Vector2> ConvexHull(IEnumerable<Vector2> points)
        {
            var sorted = new List<Vector2>(points);
            sorted.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            if (sorted.Count < 3)
                return sorted;

            var hull = new List<Vector2>();
            for (int pass = 0; pass < 2; pass++)
            {
                int start = hull.Count;
                foreach (var point in sorted)
                {
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(point);
                }
                hull.RemoveAt(hull.Count - 1);
                sorted.Reverse();
            }
            return hull;
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
