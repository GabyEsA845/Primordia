using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Hilo de las Patas hilanderas: se tiende entre dos puntos (la salida y la llegada de la esquiva) y dura unos segundos. Frena a
    /// los enemigos que lo tocan y daña una vez a cada uno. Ver <see cref="SilkThreads"/>.
    /// </summary>
    public class SilkThread : MonoBehaviour
    {
        // Largo de un tramo del dibujo (8 px a 16 por unidad) y grosor de la zona que atrapa.
        const float SegmentLength = 0.5f;
        const float Thickness = 0.35f;
        const float CheckEvery = 0.1f;
        const float SlowFactor = 0.5f;
        const int Order = -2;

        static readonly List<SilkThread> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Los hilos que hay ahora.</summary>
        public static IReadOnlyList<SilkThread> All => all;

        readonly HashSet<Health> harmed = new();
        Vector2 from;
        Vector2 to;
        float until;
        float nextCheck;
        int damage;
        Sprite[] hitFrames;

        public Vector2 From => from;
        public Vector2 To => to;
        public int Damage => damage;
        public float Length => (to - from).magnitude;

        /// <summary>Tiende un hilo entre esos dos puntos. Los cuadros pueden faltar (en las pruebas): el hilo funciona igual.</summary>
        public static SilkThread Spawn(Vector2 from, Vector2 to, float seconds, int damage, Sprite[] threadFrames,
            Sprite[] anchorFrames, Sprite[] hitFrames)
        {
            var obj = new GameObject("SilkThread");
            var thread = obj.AddComponent<SilkThread>();
            thread.from = from;
            thread.to = to;
            thread.damage = damage;
            thread.hitFrames = hitFrames;
            thread.until = Time.time + seconds;
            thread.BuildVisual(threadFrames, anchorFrames);
            return thread;
        }

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        void BuildVisual(Sprite[] threadFrames, Sprite[] anchorFrames)
        {
            Vector2 axis = to - from;
            float length = axis.magnitude;
            if (length < 0.01f)
                return;
            transform.position = from;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            if (threadFrames != null && threadFrames.Length > 0)
            {
                int segments = Mathf.Max(1, Mathf.CeilToInt(length / SegmentLength));
                for (int i = 0; i < segments; i++)
                {
                    float x = Mathf.Min((i + 0.5f) * SegmentLength, length - SegmentLength / 2f);
                    Piece("Segment", new Vector2(Mathf.Max(x, SegmentLength / 2f), 0f), threadFrames, 0.15f);
                }
            }
            if (anchorFrames != null && anchorFrames.Length > 0)
            {
                Piece("AnchorStart", Vector2.zero, anchorFrames, 0.2f);
                Piece("AnchorEnd", new Vector2(length, 0f), anchorFrames, 0.2f);
            }
        }

        void Piece(string pieceName, Vector2 localPosition, Sprite[] frames, float frameSeconds)
        {
            var piece = new GameObject(pieceName);
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = localPosition;
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = Order;
            piece.AddComponent<SpriteFramePlayer>().Play(renderer, frames, frameSeconds, newLoop: true);
        }

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
            Catch();
        }

        static readonly Collider2D[] hits = new Collider2D[24];

        void Catch()
        {
            Vector2 axis = to - from;
            float length = axis.magnitude;
            if (length < 0.01f)
                return;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            int count = Physics2D.OverlapBox((from + to) / 2f, new Vector2(length, Thickness), angle, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                if (health.TryGetComponent<EnemyAI>(out var ai))
                    ai.Slow(SlowFactor, CheckEvery * 3f);
                if (damage > 0 && harmed.Add(health))
                {
                    health.TakeDamage(damage);
                    SpriteFramePlayer.PlayOnce(hitFrames, 0.06f, health.transform.position, Order + 10);
                }
            }
        }

        /// <summary>Punto en que este hilo cruza a otro, si se cruzan.</summary>
        public bool Crosses(SilkThread other, out Vector2 point)
        {
            point = default;
            Vector2 r = to - from;
            Vector2 s = other.to - other.from;
            float denominator = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(denominator) < 0.0001f)
                return false;
            Vector2 delta = other.from - from;
            float t = (delta.x * s.y - delta.y * s.x) / denominator;
            float u = (delta.x * r.y - delta.y * r.x) / denominator;
            if (t < 0f || t > 1f || u < 0f || u > 1f)
                return false;
            point = from + r * t;
            return true;
        }
    }
}
