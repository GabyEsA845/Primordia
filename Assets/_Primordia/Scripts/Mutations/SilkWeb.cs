using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Telaraña del nivel III de las Patas hilanderas: se forma donde se cruzan dos hilos. Inmoviliza durante un segundo a cada
    /// enemigo que la toca (una vez por telaraña) y dura unos segundos.
    /// </summary>
    public class SilkWeb : MonoBehaviour
    {
        const float CheckEvery = 0.1f;
        const float AppearFrameSeconds = 0.08f;
        const int Order = -1;

        readonly HashSet<Health> held = new();
        float radius;
        float holdSeconds;
        float until;
        float nextCheck;

        /// <summary>Los enemigos que ha inmovilizado (para las pruebas).</summary>
        public int HeldCount => held.Count;

        public static SilkWeb Spawn(Vector2 position, float seconds, float holdSeconds, float radius, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            var obj = new GameObject("SilkWeb");
            obj.transform.position = position;
            var web = obj.AddComponent<SilkWeb>();
            web.radius = radius;
            web.holdSeconds = holdSeconds;
            web.until = Time.time + seconds;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = Order;
            web.StartCoroutine(web.Run(renderer, appear, idle, fade));
            return web;
        }

        IEnumerator Run(SpriteRenderer renderer, Sprite[] appear, Sprite[] idle, Sprite[] fade)
        {
            yield return Play(renderer, appear);
            if (idle != null && idle.Length > 0)
                renderer.sprite = idle[0];
            while (Time.time < until)
                yield return null;
            yield return Play(renderer, fade);
            Destroy(gameObject);
        }

        static IEnumerator Play(SpriteRenderer renderer, Sprite[] frames)
        {
            if (frames == null)
                yield break;
            foreach (var frame in frames)
            {
                renderer.sprite = frame;
                yield return new WaitForSeconds(AppearFrameSeconds);
            }
        }

        static readonly Collider2D[] hits = new Collider2D[24];

        void Update()
        {
            // Con radio 0 es solo dibujo (las celdas de la red de las Patas hilanderas).
            if (radius <= 0f || Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || !health.TryGetComponent<EnemyAI>(out var ai) || !held.Add(health))
                    continue;
                ai.Knockback(Vector2.zero, holdSeconds);
            }
        }
    }
}
