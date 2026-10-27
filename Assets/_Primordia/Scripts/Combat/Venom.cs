using System.Collections.Generic;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Veneno de las púas del Euriptérido sobre el jugador: lo ralentiza unos segundos (<see cref="PlayerController.Slow"/>) y
    /// lo tiñe de verde mientras dura. El tinte espera al destello del golpe para no taparlo.
    /// </summary>
    public class Venom : MonoBehaviour
    {
        public static readonly Color Tint = new(0.6f, 1f, 0.55f);
        const float FlashDelay = 0.1f;

        PlayerController controller;
        readonly List<(SpriteRenderer renderer, Color color)> tinted = new();
        float startedAt;
        bool applied;

        /// <summary>Ralentiza al jugador de <paramref name="target"/> y lo tiñe; no hace nada si no es el jugador.</summary>
        public static void Apply(Component target, float factor, float duration)
        {
            var player = target != null ? target.GetComponentInParent<PlayerController>() : null;
            if (player == null)
                return;
            player.Slow(factor, duration);
            if (!player.TryGetComponent<Venom>(out var venom))
                venom = player.gameObject.AddComponent<Venom>();
            venom.controller = player;
            venom.startedAt = Time.time;
        }

        void LateUpdate()
        {
            if (controller == null || !controller.IsSlowed)
            {
                Destroy(this);
                return;
            }
            if (applied || Time.time < startedAt + FlashDelay)
                return;
            applied = true;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                tinted.Add((renderer, renderer.color));
                renderer.color *= Tint;
            }
        }

        void OnDestroy()
        {
            foreach (var (renderer, color) in tinted)
            {
                if (renderer != null)
                    renderer.color = color;
            }
            tinted.Clear();
        }
    }
}
