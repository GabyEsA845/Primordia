using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Charco del escenario del piso 3 (no el de veneno): <b>lodo</b>, que frena un 40 % a quien lo pisa (jugador y enemigos que
    /// andan; la esquiva pasa por encima), o <b>agua de charca</b>, que solo se ve (con reflejos; la Piel anfibia y el sapo la
    /// usarán más adelante). Pegado al suelo, por debajo de todo.
    /// </summary>
    public class GroundPuddle : MonoBehaviour
    {
        /// <summary>El lodo deja al 60 % de la velocidad (frena un 40 %).</summary>
        public const float SludgeSpeed = 0.6f;

        const float CheckEvery = 0.1f;
        const float SlowHold = 0.25f;

        bool sludge;
        Vector2 area;
        float nextCheck;

        public bool IsSludge => sludge;

        internal static GroundPuddle Create(Transform parent, Vector2 position, Sprite[] frames, float frameSeconds, bool sludge)
        {
            var obj = new GameObject(sludge ? "Puddle_Sludge" : "Puddle_Water");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -6;
            var puddle = obj.AddComponent<GroundPuddle>();
            puddle.sludge = sludge;
            if (frames.Length > 0)
            {
                renderer.sprite = frames[0];
                // La zona que frena es el interior del dibujo (sin los bordes irregulares).
                var size = frames[0].bounds.size;
                puddle.area = new Vector2(size.x * 0.8f, size.y * 0.7f);
                obj.AddComponent<SpriteFramePlayer>().Play(renderer, frames, frameSeconds, newLoop: true);
            }
            return puddle;
        }

        void Update()
        {
            if (!sludge || Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;

            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapBoxAll(transform.position, area, 0f))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerController>() is { } player)
                {
                    if (!player.IsDodging && !player.IsLunging)
                        player.Slow(SludgeSpeed, SlowHold);
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health != null && !health.IsDead && seen.Add(health) && health.TryGetComponent<EnemyAI>(out var ai))
                    ai.Slow(SludgeSpeed, SlowHold);
            }
        }
    }
}
