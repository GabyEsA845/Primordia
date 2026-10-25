using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="LeanchoiliaFlagellaMutation"/>: cuando el arma del jugador alcanza a un enemigo y el
    /// latigazo está listo, aparecen los flagelos junto a él (<see cref="FlagellumStrike"/>) y a los 0,4 s le hacen el daño de
    /// 1 s de la Boca con sus efectos (veneno, sangrado). Con la cadena, salta después a los enemigos más cercanos.
    /// </summary>
    public class FlagellaLash : MonoBehaviour
    {
        internal struct Settings
        {
            public float cooldown;
            public float damageMultiplier;
            public int chainTargets;
            public float strikeDelay;
            public float retargetRadius;
            public float chainRadius;
            public Sprite[] lashFrames;
            public float lashFrameDuration;
            public Sprite[] readyFrames;
            public float readyFrameDuration;
        }

        /// <summary>Encima de los enemigos y de los impactos.</summary>
        const int SortingOrder = 22;

        Weapon weapon;
        Settings settings;
        float readyAt;
        bool readyShown = true;
        // Mientras azota, sus propios golpes no vuelven a lanzar otro latigazo.
        bool striking;
        int enemyMask;

        public bool IsReady => Time.time >= readyAt;
        public float Cooldown => settings.cooldown;
        public int ChainTargets => settings.chainTargets;

        /// <summary>Latigazos lanzados (para las pruebas).</summary>
        public int Lashes { get; private set; }

        internal void Configure(Weapon newWeapon, Settings newSettings)
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;

            weapon = newWeapon;
            settings = newSettings;
            if (weapon != null)
                weapon.HitTarget += OnHitTarget;
        }

        void Awake() => enemyMask = LayerMask.GetMask(CombatLayers.Enemy);

        void OnDestroy()
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;
        }

        void Update()
        {
            if (readyShown || !IsReady)
                return;

            readyShown = true;
            FrameEffect.Play("FlagellaReady", settings.readyFrames, settings.readyFrameDuration, SortingOrder, transform);
        }

        /// <summary>Daño del latigazo con la Boca de ahora: el daño de cada golpe por los golpes por segundo.</summary>
        public int LashDamage()
        {
            var attack = weapon != null ? weapon.Attack : null;
            if (attack == null)
                return 0;
            // Con la Corona de brazos, lo que hace el remolino en 1 s (1 cada 0,4 s: 2,5).
            float perSecond = attack.IsArea ? weapon.DamageOf(attack) / attack.AreaTick : weapon.DamageOf(attack) * attack.FireRate;
            return Mathf.Max(1, Mathf.CeilToInt(perSecond * settings.damageMultiplier - 0.001f));
        }

        void OnHitTarget(Health target)
        {
            if (striking || !IsReady || target == null)
                return;

            // Si el golpe ya lo mató, va al más cercano; si no hay nadie, no gasta el latigazo.
            if (target.IsDead)
            {
                target = Nearest(target.transform.position, settings.retargetRadius, null);
                if (target == null)
                    return;
            }

            readyAt = Time.time + settings.cooldown;
            readyShown = false;
            Lashes++;
            int damage = LashDamage();
            Launch(target, damage, weapon.Attack, chain: settings.chainTargets);

            // Con la Corona de brazos, azota también a todos los que hay dentro del remolino.
            var whirlpool = weapon.Attack != null && weapon.Attack.IsArea ? Whirlpool.Containing(target, weapon) : null;
            if (whirlpool != null)
            {
                foreach (var other in whirlpool.Inside(whirlpool.Radius))
                {
                    if (other != target)
                        Launch(other, damage, weapon.Attack, chain: 0);
                }
            }
        }

        void Launch(Health target, int damage, AttackData attack, int chain)
        {
            FlagellumStrike.Spawn(target, settings.lashFrames, settings.lashFrameDuration, settings.strikeDelay, SortingOrder,
                (current, lastPosition) => Strike(current, lastPosition, damage, attack, chain));
        }

        /// <summary>El momento del latigazo: si el enemigo ya murió, va al más cercano. Devuelve a quién azotó.</summary>
        Health Strike(Health target, Vector2 lastPosition, int damage, AttackData attack, int chain)
        {
            if (this == null)
                return null;

            if (target == null || target.IsDead)
            {
                target = Nearest(lastPosition, settings.retargetRadius, null);
                if (target == null)
                    return null;
            }

            Hit(target, damage, attack);

            if (chain > 0)
            {
                var exclude = new HashSet<Health> { target };
                Vector2 from = target.transform.position;
                for (int i = 0; i < chain; i++)
                {
                    var next = Nearest(from, settings.chainRadius, exclude);
                    if (next == null)
                        break;
                    exclude.Add(next);
                    Launch(next, damage, attack, chain: 0);
                }
            }
            return target;
        }

        void Hit(Health target, int damage, AttackData attack)
        {
            striking = true;
            try
            {
                if (!target.TakeDamage(damage))
                    return;
                Sfx.Play(Sounds.Thorns);
                if (attack != null)
                    attack.ApplyStatus(target, weapon != null ? weapon.PoisonMultiplier : 1f);
                // Cuenta como golpe del jugador (curarse al matar, el pánico del Camuflaje...).
                if (weapon != null)
                    weapon.ReportHit(target);
            }
            finally
            {
                striking = false;
            }
        }

        /// <summary>El enemigo vivo más cercano a ese punto dentro del radio, sin contar los de la lista.</summary>
        Health Nearest(Vector2 from, float radius, HashSet<Health> exclude)
        {
            Health best = null;
            float bestDistance = float.MaxValue;
            foreach (var hit in Physics2D.OverlapCircleAll(from, radius, enemyMask))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || (exclude != null && exclude.Contains(health)))
                    continue;
                float distance = ((Vector2)hit.ClosestPoint(from) - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = health;
                }
            }
            return best;
        }
    }
}
