using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Veneno: daño en el tiempo con su marca de burbujas verdes. Con contagio (Aguijón nivel III), si el objetivo
    /// muere envenenado estalla y envenena a los de su equipo que estén cerca, que a su vez pueden contagiar.
    /// </summary>
    public class Poison : DamageOverTime
    {
        public const string Key = "Poison";

        float burstRadius;
        GameObject burstEffect;
        GameObject markerPrefab;
        float duration;

        protected override string MarkerKey => Key;

        bool venom;

        protected override DamageSource Source => venom ? DamageSource.Venom : DamageSource.Poison;

        /// <summary>True si lleva veneno virulento (Escupitajo): sus golpes cuentan como Venom para el límite del jefe.</summary>
        public bool IsVenom => venom;

        /// <summary>Veneno virulento del Escupitajo: <paramref name="damagePerTick"/> de daño cada <paramref name="tickSeconds"/> durante <paramref name="duration"/> s.</summary>
        public static Poison ApplyVenom(Health target, int damagePerTick, float tickSeconds, float duration, GameObject effectPrefab = null)
        {
            if (Immune(target))
                return null;
            var poison = Apply<Poison>(target, damagePerTick, duration, effectPrefab, tickSeconds);
            poison.venom = true;
            return poison;
        }

        /// <summary>Contra un jefe el veneno no pasa de 2 de daño por segundo (ajustable aquí).</summary>
        public const int BossDamagePerSecondCap = 2;

        protected override int BossDamageCap => BossDamagePerSecondCap;

        /// <summary>Radio del estallido al morir envenenado (0 = sin contagio).</summary>
        public float BurstRadius => burstRadius;

        public static Poison Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null) =>
            Immune(target) ? null : Apply<Poison>(target, damagePerSecond, duration, effectPrefab);

        /// <summary>True si el objetivo no sufre ningún veneno (el linaje Ciempiés).</summary>
        static bool Immune(Health target) => target != null && target.TryGetComponent<IFullPoisonImmune>(out _);

        /// <summary>Veneno que, si el objetivo muere con él, estalla y contagia a los cercanos.</summary>
        public static Poison ApplyContagious(Health target, int damagePerSecond, float duration, GameObject effectPrefab,
            float burstRadius, GameObject burstEffect)
        {
            if (Immune(target))
                return null;
            var poison = Apply<Poison>(target, damagePerSecond, duration, effectPrefab);
            if (burstRadius > poison.burstRadius)
            {
                poison.burstRadius = burstRadius;
                poison.burstEffect = burstEffect;
            }
            poison.markerPrefab = effectPrefab;
            poison.duration = Mathf.Max(poison.duration, duration);
            return poison;
        }

        void OnEnable()
        {
            if (TryGetComponent(out Health target))
                target.Died += OnDied;
        }

        void OnDisable()
        {
            if (TryGetComponent(out Health target))
                target.Died -= OnDied;
        }

        void OnDied()
        {
            if (burstRadius <= 0f)
                return;

            Vector2 center = transform.position;
            OneShotEffect.Spawn(burstEffect, center);
            Sfx.Play(Sounds.PoisonBurst);

            // Contagia a los de la misma capa (enemigos con enemigos), sin contar al que estalla.
            int mask = 1 << gameObject.layer;
            foreach (var hit in Physics2D.OverlapCircleAll(center, burstRadius, mask))
            {
                var other = hit.GetComponentInParent<Health>();
                if (other == null || other == health || other.IsDead)
                    continue;

                ApplyContagious(other, DamagePerTick, duration, markerPrefab, burstRadius, burstEffect);
            }
        }
    }
}
