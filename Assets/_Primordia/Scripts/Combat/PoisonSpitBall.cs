using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// La bola del Escupitajo venenoso (<see cref="AttackData.IsSpit"/>), sobre un <see cref="Projectile"/>: atraviesa a los enemigos,
    /// encoge mientras vuela dejando un charco pequeño cada cierto tiempo y estalla (daño en un radio y charco grande) al acabar su
    /// tiempo, al chocar con una pared o al disparar otra con la misma arma. En la Camada, el estallido suelta bolitas en cruz.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    public class PoisonSpitBall : MonoBehaviour
    {
        // Tamaño que le queda a la bola justo antes de estallar, respecto al del principio.
        const float EndScale = 0.3f;

        // La última bola viva de cada arma: al disparar otra, esta estalla.
        static readonly Dictionary<Weapon, PoisonSpitBall> latest = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => latest.Clear();

        Projectile projectile;
        AttackData attack;
        Weapon weapon;
        Vector3 baseScale;
        float age;
        float nextPuddle;
        bool alive;

        void Awake()
        {
            projectile = GetComponent<Projectile>();
            baseScale = transform.localScale;
            projectile.Launched += OnLaunched;
            projectile.Ending += OnEnding;
        }

        void OnDestroy()
        {
            if (projectile == null)
                return;
            projectile.Launched -= OnLaunched;
            projectile.Ending -= OnEnding;
        }

        void OnLaunched(Projectile launched)
        {
            attack = launched.Attack;
            alive = attack != null && attack.IsSpit;
            if (!alive)
                return;

            age = 0f;
            nextPuddle = attack.SpitTrailEvery;
            transform.localScale = baseScale;
            weapon = launched.Source;
            if (weapon == null || attack.SpitMini)
                return;

            // Disparar otra hace estallar la anterior.
            if (latest.TryGetValue(weapon, out var previous) && previous != null && previous != this && previous.alive)
                previous.projectile.Despawn(Projectile.EndReason.Detonated);
            latest[weapon] = this;
        }

        void FixedUpdate()
        {
            if (!alive)
                return;

            age += Time.fixedDeltaTime * (IsPlayerShot ? Core.TimeWarp.PlayerSpeed : 1f);
            float t = Mathf.Clamp01(age / attack.Lifetime);
            transform.localScale = baseScale * Mathf.Lerp(1f, EndScale, t);

            while (age >= nextPuddle && nextPuddle < attack.Lifetime)
            {
                nextPuddle += attack.SpitTrailEvery;
                PoisonPuddle.Spawn(transform.position, attack.SpitTrailSeconds, small: true, harmsPlayer: !IsPlayerShot);
            }
        }

        bool IsPlayerShot => gameObject.layer == CombatLayers.ProjectileLayer(Team.Player);

        void OnEnding(Projectile ending, Projectile.EndReason reason)
        {
            if (!alive)
                return;
            alive = false;
            if (weapon != null && latest.TryGetValue(weapon, out var current) && current == this)
                latest.Remove(weapon);
            if (reason == Projectile.EndReason.Cleared)
                return;

            Explode(ending.transform.position);
        }

        void Explode(Vector2 position)
        {
            var victims = IsPlayerShot ? Team.Enemy : Team.Player;
            int damage = weapon != null
                ? Mathf.Max(0, Mathf.CeilToInt(attack.SpitBurstDamage * weapon.DamageMultiplier - 0.001f))
                : attack.SpitBurstDamage;
            PoisonSpitBurst.Spawn(position, damage, attack.SpitBurstRadius, attack.SpitBurstPuddleSeconds, weapon, victims);

            // Camada: bolitas en cruz.
            if (attack.SpitSplitCount > 0 && attack.SpitSplitAttack != null && attack.SpitSplitAttack.ProjectilePrefab != null)
            {
                float step = 360f / attack.SpitSplitCount;
                for (int i = 0; i < attack.SpitSplitCount; i++)
                {
                    var direction = Quaternion.Euler(0f, 0f, step * i) * Vector2.right;
                    ProjectilePool.Get(attack.SpitSplitAttack.ProjectilePrefab)
                        .Launch(position, direction, attack.SpitSplitAttack, gameObject.layer, weapon);
                }
            }
        }
    }
}
