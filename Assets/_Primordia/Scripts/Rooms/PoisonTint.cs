using System.Collections.Generic;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Un enemigo envenenado se tiñe de lima (≈ 35 %) mientras dura el veneno. Se añade solo con <see cref="Toxins.PoisonEnemy"/>
    /// y se quita solo, devolviendo el color de cada sprite, cuando el veneno acaba.
    /// </summary>
    public class PoisonTint : MonoBehaviour
    {
        const float Amount = 0.35f;
        static readonly Color Lime = new(0.659f, 0.878f, 0.251f, 1f);

        readonly List<(SpriteRenderer renderer, Color color)> originals = new();
        float nextRefresh;

        void OnEnable() => Tint();

        void Update()
        {
            if (!TryGetComponent<Poison>(out _))
            {
                Destroy(this);
                return;
            }
            // Los sprites del cuerpo pueden cambiar (formas nuevas): se vuelve a mirar de vez en cuando.
            if (Time.time >= nextRefresh)
                Tint();
        }

        void Tint()
        {
            nextRefresh = Time.time + 0.5f;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer == null || originals.Exists(pair => pair.renderer == renderer))
                    continue;
                originals.Add((renderer, renderer.color));
                var color = Color.Lerp(renderer.color, Lime, Amount);
                color.a = renderer.color.a;
                renderer.color = color;
            }
        }

        void OnDestroy()
        {
            foreach (var (renderer, color) in originals)
            {
                if (renderer != null)
                    renderer.color = color;
            }
            originals.Clear();
        }
    }
}
