using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mancha de micelio de las Hifas: mientras dura, frena y daña a los enemigos que tiene encima (a través de su
    /// <see cref="MyceliumExposure"/>, para que varias manchas juntas no sumen daño). Se desvanece al final.
    /// También sirve para las matas de Colonizar la sala (duran hasta que se quitan) y para la estela urticante de
    /// la Medusa (envenena al tocarla).
    /// </summary>
    public class MyceliumPatch : MonoBehaviour
    {
        [System.Serializable]
        public struct Settings
        {
            public float duration;
            public int damagePerSecond;
            public float slowFactor;
            public float radius;
            public bool burst;
            public float burstRadius;
            public int burstPoisonDamage;
            public float burstPoisonDuration;
            public GameObject burstEffect;
            public GameObject poisonEffect;
            // Veneno a quien la toca (la estela urticante). 0 = sin veneno.
            public int touchPoisonDamage;
            public float touchPoisonDuration;
        }

        // Segundos del final en los que se desvanece.
        const float FadeTime = 0.5f;

        static readonly List<Collider2D> touching = new();

        Settings settings;
        float endTime;
        SpriteRenderer sprite;
        ContactFilter2D filter;

        public Settings Current => settings;

        readonly List<Enemy> covered = new();

        /// <summary>Enemigos vivos que tiene encima ahora (se actualiza en cada Update de la mata).</summary>
        public IReadOnlyList<Enemy> Covered => covered;

        /// <summary>True si ese enemigo está encima ahora.</summary>
        public bool Covers(Enemy enemy) => covered.Contains(enemy);

        public void Configure(Settings newSettings)
        {
            settings = newSettings;
            endTime = Time.time + settings.duration;
            sprite = GetComponentInChildren<SpriteRenderer>();
            filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = LayerMask.GetMask(CombatLayers.Enemy) };
        }

        void Update()
        {
            float left = endTime - Time.time;
            if (left <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (sprite != null && left < FadeTime)
            {
                var color = sprite.color;
                color.a = left / FadeTime;
                sprite.color = color;
            }

            touching.Clear();
            covered.Clear();
            Physics2D.OverlapCircle(transform.position, settings.radius, filter, touching);
            foreach (var hit in touching)
            {
                var enemy = hit.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.Health.IsDead)
                    continue;
                if (!covered.Contains(enemy))
                    covered.Add(enemy);

                var exposure = enemy.TryGetComponent<MyceliumExposure>(out var existing) ? existing : enemy.gameObject.AddComponent<MyceliumExposure>();
                exposure.Touch(settings);
            }
        }
    }
}
