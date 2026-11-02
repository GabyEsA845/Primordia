using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fumarola del piso 3: un agujero del suelo que en reposo suelta un hilo de vapor, avisa (brilla y burbujea) y lanza un
    /// chorro de vapor en columna. El chorro hace 1 de daño al jugador y 3 a los enemigos que están en la columna (una vez por
    /// chorro; la esquiva pasa por encima) y no prende nada. Cada fumarola empieza en un punto distinto del ciclo.
    /// </summary>
    public class SteamVent : MonoBehaviour
    {
        const float IdleSeconds = 4.5f;
        const float WarnSeconds = 1f;
        const float IdleFrameSeconds = 0.26f;
        const float WarnFrameSeconds = 0.12f;
        const float BlastFrameSeconds = 0.09f;
        // Cuadros del chorro que dañan (el 1 al 3).
        const int FirstDamageFrame = 1;
        const int LastDamageFrame = 3;
        const int PlayerDamage = 1;
        const int EnemyDamage = 3;
        // La columna: ancho y alto sobre la boca, en tiles.
        static readonly Vector2 Column = new(0.9f, 3.5f);

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] warn = System.Array.Empty<Sprite>();
        Sprite[] blast = System.Array.Empty<Sprite>();

        /// <summary>True durante el aviso y el chorro (para que el audio y las pruebas lo sepan).</summary>
        public bool IsActive { get; private set; }

        internal static SteamVent Create(Transform parent, Vector2 position, Sprite[] idle, Sprite[] warn, Sprite[] blast)
        {
            var obj = new GameObject("SteamVent");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            // Pegada al suelo pero por encima de los charcos y las rocas.
            renderer.sortingOrder = -3;
            var vent = obj.AddComponent<SteamVent>();
            vent.body = renderer;
            vent.idle = idle;
            vent.warn = warn;
            vent.blast = blast;
            return vent;
        }

        IEnumerator Start()
        {
            // Cada fumarola empieza en un punto distinto del ciclo.
            yield return new WaitForSeconds(Random.value * (IdleSeconds + WarnSeconds));
            while (true)
            {
                IsActive = false;
                yield return Loop(idle, IdleFrameSeconds, IdleSeconds);
                IsActive = true;
                yield return Loop(warn, WarnFrameSeconds, WarnSeconds);
                yield return Blast();
            }
        }

        IEnumerator Loop(Sprite[] frames, float frameSeconds, float seconds)
        {
            if (frames.Length == 0)
            {
                yield return new WaitForSeconds(seconds);
                yield break;
            }
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                body.sprite = frames[Mathf.FloorToInt(t / frameSeconds) % frames.Length];
                yield return null;
            }
        }

        IEnumerator Blast()
        {
            var hurt = new HashSet<Health>();
            for (int i = 0; i < blast.Length; i++)
            {
                body.sprite = blast[i];
                if (i >= FirstDamageFrame && i <= LastDamageFrame)
                    Hurt(hurt);
                yield return new WaitForSeconds(BlastFrameSeconds);
            }
        }

        void Hurt(HashSet<Health> hurt)
        {
            var center = (Vector2)transform.position + Vector2.up * (Column.y / 2f);
            foreach (var hit in Physics2D.OverlapBoxAll(center, Column, 0f))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || hurt.Contains(health))
                    continue;

                if (health.GetComponentInParent<PlayerController>() is { } player)
                {
                    // La esquiva pasa por encima del chorro.
                    if (player.IsDodging || player.IsLunging)
                        continue;
                    hurt.Add(health);
                    health.TakeDamage(PlayerDamage);
                }
                else if (health.GetComponentInParent<Enemy>() != null)
                {
                    hurt.Add(health);
                    health.TakeDamage(EnemyDamage);
                }
            }
        }
    }
}
