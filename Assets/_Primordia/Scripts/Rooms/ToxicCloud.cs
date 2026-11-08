using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Nube tóxica (fuego + charco de veneno, piso 3): una columna mostaza que nace, se mantiene y se deshace en unos 5 s. A los
    /// enemigos que están dentro los envenena (2 por segundo durante 4 s, renovado); al jugador le llena la barra de veneno
    /// (<see cref="PlayerToxin"/>). La esquiva atraviesa la nube sin llenarla.
    /// </summary>
    public class ToxicCloud : MonoBehaviour, IWindPushable
    {
        public const float Seconds = 5f;

        // Lo que cubre la columna en el suelo y a qué altura está su centro, y cada cuánto se comprueba quién está dentro.
        const float Radius = 1.2f;
        const float CenterHeight = 0.8f;
        const float CheckEvery = 0.1f;
        const int EnemyDamagePerSecond = 2;
        const float EnemyPoisonSeconds = 4f;

        float nextCheck;
        float endTime;

        /// <summary>Levanta una nube en ese punto (la base de la columna). Null si no hay arte del veneno.</summary>
        public static ToxicCloud Spawn(Vector2 position)
        {
            var kit = ReactionKit.Current;
            if (kit == null || kit.CloudIdle.Length == 0)
                return null;

            var obj = new GameObject("ToxicCloud");
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 16;
            var cloud = obj.AddComponent<ToxicCloud>();
            cloud.endTime = Time.time + Seconds;
            cloud.StartCoroutine(cloud.Run(renderer, kit));
            return cloud;
        }

        IEnumerator Run(SpriteRenderer renderer, ReactionKit kit)
        {
            foreach (var frame in kit.CloudForm)
            {
                renderer.sprite = frame;
                yield return new WaitForSeconds(kit.CloudFrameSeconds);
            }

            // Se mantiene hasta que quede lo justo para deshacerse.
            float fadeSeconds = kit.CloudFade.Length * kit.CloudFrameSeconds;
            float start = Time.time;
            while (Time.time < endTime - fadeSeconds)
            {
                renderer.sprite = kit.CloudIdle[Mathf.FloorToInt((Time.time - start) / 0.12f) % kit.CloudIdle.Length];
                yield return null;
            }
            foreach (var frame in kit.CloudFade)
            {
                renderer.sprite = frame;
                yield return new WaitForSeconds(kit.CloudFrameSeconds);
            }
            Destroy(gameObject);
        }

        Vector2 IWindPushable.Position => transform.position;

        public void Push(Vector2 delta) => transform.position += (Vector3)delta;

        void OnEnable() => Wind.Register(this);

        void OnDisable() => Wind.Unregister(this);

        void Update()
        {
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;

            var center = (Vector2)transform.position + Vector2.up * CenterHeight;
            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(center, Radius))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerToxin>() is { } player)
                {
                    if (!player.IsDodging)
                        player.Feed();
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;
                Toxins.PoisonEnemy(health, EnemyDamagePerSecond, EnemyPoisonSeconds);
            }
        }
    }
}
