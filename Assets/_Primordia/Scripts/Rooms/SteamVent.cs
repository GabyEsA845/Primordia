using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Fumarola del piso 3: un agujero del suelo que en reposo suelta un hilo de vapor, avisa (brilla y burbujea) y lanza un
    /// chorro de vapor en columna. El chorro hace 1 de daño al jugador y 3 a los enemigos que están en la columna (una vez por
    /// chorro; la esquiva pasa por encima) y no prende nada. Además empuja a quien está en la columna hacia fuera, al lado en el
    /// que está (unos 2 tiles; los jefes no se mueven), con una ráfaga de vapor detrás: así se puede mandar a un enemigo al lodo o
    /// al fuego. Cada fumarola empieza en un punto distinto del ciclo.
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
        // El empujón: unos 2 tiles. Al jugador, a velocidad constante 0,2 s; al enemigo, con la frenada de sus empujones (30 u/s²).
        const float PlayerPushSpeed = 10f;
        const float PlayerPushSeconds = 0.2f;
        const float EnemyPushSpeed = 11f;
        const float EnemyStunSeconds = 0.25f;
        const float GustFrameSeconds = 0.07f;
        // La columna: ancho y alto sobre la boca, en tiles.
        static readonly Vector2 Column = new(0.9f, 3.5f);

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] warn = System.Array.Empty<Sprite>();
        Sprite[] blast = System.Array.Empty<Sprite>();

        /// <summary>True durante el aviso y el chorro (para que el audio y las pruebas lo sepan).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Segundos que espera antes de empezar su ciclo (negativo: uno al azar). La sala Fumarolas lo fija por filas.</summary>
        float startOffset = -1f;

        /// <summary>Lo que dura un ciclo completo sin el chorro: reposo y aviso.</summary>
        internal const float CycleSeconds = IdleSeconds + WarnSeconds;

        internal static SteamVent Create(Transform parent, Vector2 position, Sprite[] idle, Sprite[] warn, Sprite[] blast, float startOffset = -1f)
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
            vent.startOffset = startOffset;
            return vent;
        }

        IEnumerator Start()
        {
            // Cada fumarola empieza en un punto distinto del ciclo.
            yield return new WaitForSeconds(startOffset >= 0f ? startOffset : Random.value * CycleSeconds);
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
                    float side = SideOf(hit);
                    player.Shove(Vector2.right * side * PlayerPushSpeed, PlayerPushSeconds);
                    Gust(hit.bounds.center, side);
                }
                else if (health.GetComponentInParent<Enemy>() != null)
                {
                    hurt.Add(health);
                    health.TakeDamage(EnemyDamage);
                    if (!health.IsDead && health.GetComponentInParent<IBoss>() == null
                        && health.GetComponentInParent<IPushable>() is { } pushable)
                    {
                        float side = SideOf(hit);
                        pushable.Knockback(Vector2.right * side * EnemyPushSpeed, EnemyStunSeconds);
                        Gust(hit.bounds.center, side);
                    }
                }
            }
        }

        /// <summary>A qué lado de la columna está (1 derecha, −1 izquierda); justo en medio, a la derecha.</summary>
        float SideOf(Collider2D hit) => hit.bounds.center.x < transform.position.x ? -1f : 1f;

        /// <summary>La ráfaga de vapor detrás de lo que sale empujado, mirando hacia donde va.</summary>
        static void Gust(Vector2 position, float side)
        {
            var frames = PolishArt.Current != null ? PolishArt.Current.ventGust : null;
            if (frames == null || frames.Length == 0)
                return;
            var go = new GameObject("VentGust");
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.flipX = side < 0f;
            renderer.sortingOrder = 3;
            go.AddComponent<SpriteFramePlayer>().Play(renderer, frames, GustFrameSeconds, newLoop: false);
        }
    }
}
