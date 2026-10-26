using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cosecha (Wiwaxia): una tanda de escleritos que alcanza a 3 enemigos distintos o más da medio corazón verde
    /// (<see cref="Health.Barrier"/>, con el mismo tope que el Pastar del piso 2). Apunta las escamas de cada tanda y
    /// cuenta a quién dan con <see cref="Projectile.AnyHit"/>. Al perder el linaje se quitan los que dio.
    /// </summary>
    public class HarvestTouch : MonoBehaviour
    {
        // Una tanda que no ha llegado a 3 en este tiempo ya no cuenta (las escamas se reciclan en el pool).
        const float VolleyWindow = 3f;

        class Volley
        {
            public readonly HashSet<Projectile> Scales = new();
            public readonly HashSet<Health> Hit = new();
            public float Until;
            public bool Harvested;
        }

        Health health;
        int targets = 3;
        int maxBarrier = 2;
        GameObject effect;
        int granted;
        readonly List<Volley> volleys = new();

        /// <summary>Medios corazones verdes que ha dado (para las pruebas).</summary>
        public int Harvests { get; private set; }

        public void Configure(Health newHealth, int newTargets, int newMaxBarrier, GameObject newEffect)
        {
            Unsubscribe();
            health = newHealth;
            targets = Mathf.Max(1, newTargets);
            maxBarrier = Mathf.Max(1, newMaxBarrier);
            effect = newEffect;
            ScleriteVolley.AnyReleased += OnReleased;
            Projectile.AnyHit += OnHit;
            if (health != null)
                health.BarrierBlocked += OnBarrierBlocked;
        }

        void OnReleased(ScleriteVolley source, IReadOnlyList<Projectile> scales)
        {
            if (source == null || source.gameObject != gameObject || scales.Count == 0)
                return;
            var volley = new Volley { Until = Time.time + VolleyWindow };
            foreach (var scale in scales)
                volley.Scales.Add(scale);
            volleys.Add(volley);
        }

        void OnHit(Projectile projectile, Health target)
        {
            if (target == null || !target.TryGetComponent<Enemy>(out _))
                return;
            volleys.RemoveAll(v => Time.time > v.Until);
            foreach (var volley in volleys)
            {
                if (volley.Harvested || !volley.Scales.Contains(projectile))
                    continue;
                volley.Hit.Add(target);
                if (volley.Hit.Count >= targets)
                {
                    volley.Harvested = true;
                    Harvest();
                }
                return;
            }
        }

        void Harvest()
        {
            if (health == null || health.IsDead || health.Barrier >= maxBarrier)
                return;
            health.Barrier++;
            granted++;
            Harvests++;
            Sfx.Play(Sounds.LineageBarrier);
            OneShotEffect.Spawn(effect, transform.position, parent: transform);
        }

        void OnBarrierBlocked() => granted = Mathf.Min(granted, health.Barrier);

        void Unsubscribe()
        {
            ScleriteVolley.AnyReleased -= OnReleased;
            Projectile.AnyHit -= OnHit;
            if (health != null)
                health.BarrierBlocked -= OnBarrierBlocked;
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (granted > 0 && health != null)
                health.Barrier = Mathf.Max(0, health.Barrier - granted);
        }
    }
}
