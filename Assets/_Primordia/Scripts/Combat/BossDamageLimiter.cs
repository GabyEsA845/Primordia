using System;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Límite de daño por segundo que un jefe admite de cada fuente (chorro, peonza, escupitajo, veneno, fuego, gas). Sirve para que
    /// la pelea dure parecido con cualquier Boca: lo que pasa del tope en una ventana de un segundo se descarta. Las fuentes sin
    /// etiquetar (golpes, explosiones...) no se limitan. Se engancha al <see cref="Health.DamageGate"/> del jefe.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class BossDamageLimiter : MonoBehaviour
    {
        [Serializable]
        public struct Cap
        {
            public DamageSource source;
            [Min(0f)] public float perSecond;
        }

        const float WindowSeconds = 1f;

        [Tooltip("Daño máximo por segundo de cada fuente. Las que no estén aquí no se limitan.")]
        [SerializeField] Cap[] caps = DefaultCaps();

        readonly Dictionary<DamageSource, List<(float time, int amount)>> log = new();
        Health health;

        /// <summary>Topes por defecto (provisionales: se ajustan con la prueba de banco de las tres Bocas).</summary>
        public static Cap[] DefaultCaps() => new[]
        {
            new Cap { source = DamageSource.Jet, perSecond = 5f },
            new Cap { source = DamageSource.Spin, perSecond = 8f },
            new Cap { source = DamageSource.Spit, perSecond = 8f },
            new Cap { source = DamageSource.Poison, perSecond = 2f },
            new Cap { source = DamageSource.Venom, perSecond = 4f },
            new Cap { source = DamageSource.Burn, perSecond = 2f },
            new Cap { source = DamageSource.Gas, perSecond = 3f },
        };

        public void Configure(Cap[] newCaps)
        {
            caps = newCaps ?? Array.Empty<Cap>();
            log.Clear();
        }

        void Awake()
        {
            health = GetComponent<Health>();
            health.DamageGate = Filter;
        }

        void OnDestroy()
        {
            if (health != null && health.DamageGate == (Func<int, DamageKind, DamageSource, int>)Filter)
                health.DamageGate = null;
        }

        /// <summary>El tope de esa fuente (0 o más), o null si no se limita.</summary>
        public float? CapOf(DamageSource source)
        {
            foreach (var cap in caps)
            {
                if (cap.source == source)
                    return cap.perSecond;
            }
            return null;
        }

        int Filter(int amount, DamageKind kind, DamageSource source) => Apply(amount, source, Time.time);

        /// <summary>Admite como mucho el tope de esa fuente en el último segundo (el tiempo se pasa fuera para poder simular).</summary>
        internal int Apply(int amount, DamageSource source, float now)
        {
            var cap = CapOf(source);
            if (cap == null)
                return amount;

            if (!log.TryGetValue(source, out var entries))
                log[source] = entries = new List<(float, int)>();
            int used = 0;
            entries.RemoveAll(entry => now - entry.time >= WindowSeconds);
            foreach (var entry in entries)
                used += entry.amount;

            int allowed = Mathf.Min(amount, Mathf.Max(0, Mathf.FloorToInt(cap.Value) - used));
            if (allowed > 0)
                entries.Add((now, allowed));
            return allowed;
        }
    }
}
