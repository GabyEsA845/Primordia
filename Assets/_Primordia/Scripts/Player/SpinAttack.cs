using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// La Peonza del Diplocaulus (<see cref="AttackData.IsSpin"/>): mientras se mantiene el botón, el cuerpo gira y golpea y empuja a
    /// los enemigos de un radio cada cierto tiempo, yendo más lento. Empieza con una preparación, a los segundos máximos se marea, y
    /// al soltar o marearse hay una breve espera antes de poder girar otra vez. Esquivar la cancela. En el nivel II devuelve los
    /// proyectiles enemigos que entran en el radio; en el III, si se giró un rato, al soltar sale un bumerán hacia el cursor.
    /// <see cref="PlayerAttack"/> la maneja (la añade sola).
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Weapon), typeof(PlayerAnimator))]
    public class SpinAttack : MonoBehaviour
    {
        public enum Phase { Idle, Winding, Spinning, Ending, Dizzy }

        // Cuánto aturde el empujón de cada golpe (lo justo para que se note), y duraciones de reserva si el cuerpo no trae la animación.
        const float PushStun = 0.15f;
        const float DefaultWindingSeconds = 0.21f;
        const float DefaultEndingSeconds = 0.24f;
        const float DefaultDizzySeconds = 1f;
        // Radio al que está dibujado el remolino.
        const float EffectArtRadius = 1.5f;

        PlayerController controller;
        PlayerAnimator animator;
        Weapon weapon;
        Health health;
        AttackData attack;
        GameObject effect;
        float phaseTimer;
        float tickTimer;
        float spinTime;
        float readyTime;
        bool dizzyNext;
        SpinBoomerang thrownBoomerang;
        readonly HashSet<Health> struck = new();
        readonly List<Projectile> toReturn = new();

        public Phase State { get; private set; }

        /// <summary>True mientras gira (con o sin preparación, frenando o mareado).</summary>
        public bool IsActive => State != Phase.Idle;

        /// <summary>Segundos que lleva girando en este giro.</summary>
        public float SpinSeconds => spinTime;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponent<PlayerAnimator>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
        }

        void OnDisable() => Cancel();

        /// <summary>Se llama cada fotograma mientras el ataque equipado es una peonza. <paramref name="held"/>: el botón está pulsado.</summary>
        public void Tick(bool held)
        {
            var equipped = weapon.Attack;
            if (State != Phase.Idle && attack != equipped)
            {
                Cancel();
                return;
            }

            if (health.IsDead || !controller.CanAct || controller.IsDodging)
            {
                if (State != Phase.Idle)
                    Cancel();
                return;
            }

            float dt = Time.deltaTime * TimeWarp.PlayerSpeed;
            switch (State)
            {
                case Phase.Idle:
                    // Con el bumerán fuera no se puede atacar: hay que esperar a que vuelva.
                    if (held && Time.time >= readyTime && thrownBoomerang == null)
                        Begin(equipped);
                    break;

                case Phase.Winding:
                    phaseTimer -= dt;
                    if (!held)
                        Finish(dizzy: false, spun: false);
                    else if (phaseTimer <= 0f)
                        StartSpinning();
                    break;

                case Phase.Spinning:
                    Spin(held, dt);
                    break;

                case Phase.Ending:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                        AfterEnding();
                    break;

                case Phase.Dizzy:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                        Rest();
                    break;
            }
        }

        void Begin(AttackData equipped)
        {
            attack = equipped;
            spinTime = 0f;
            string clip = $"AttackStart_{animator.CurrentFacing.ToString().ToLowerInvariant()}";
            bool hasWinding = animator.Hold(clip);
            phaseTimer = hasWinding ? animator.ClipLength(clip, DefaultWindingSeconds) : 0f;
            State = Phase.Winding;
            if (!hasWinding)
                StartSpinning();
        }

        void StartSpinning()
        {
            controller.ChannelSpeedFactor = attack.SpinSpeedFactor;
            animator.Hold("Spin");
            State = Phase.Spinning;
            tickTimer = 0f;
            if (attack.SpinEffect != null && effect == null)
            {
                effect = Instantiate(attack.SpinEffect, transform);
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localScale = Vector3.one * (attack.SpinRadius / EffectArtRadius);
            }
        }

        void Spin(bool held, float dt)
        {
            spinTime += dt;
            controller.ChannelSpeedFactor = attack.SpinSpeedFactor;

            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer += attack.SpinTick;
                Strike();
            }

            if (attack.SpinReflects)
                ReturnProjectiles();

            if (!held)
                Finish(dizzy: false, spun: true);
            else if (spinTime >= attack.SpinMaxSeconds)
                Finish(dizzy: true, spun: true);
        }

        /// <summary>Daña y empuja hacia fuera a cada enemigo del radio.</summary>
        void Strike()
        {
            int damage = Mathf.Max(0, Mathf.CeilToInt(attack.SpinDamage * weapon.DamageMultiplier - 0.001f));
            Vector2 center = transform.position;
            struck.Clear();
            foreach (var hit in Physics2D.OverlapCircleAll(center, attack.SpinRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !struck.Add(target))
                    continue;

                if (target.TakeDamage(damage))
                    weapon.ReportHit(target);
                if (target.IsDead || attack.SpinPush <= 0f)
                    continue;
                var pushable = hit.GetComponentInParent<IPushable>();
                if (pushable == null)
                    continue;
                Vector2 away = (Vector2)hit.bounds.center - center;
                away = away.sqrMagnitude > 0.0001f ? away.normalized : Random.insideUnitCircle.normalized;
                pushable.Knockback(away * attack.SpinPush, PushStun);
            }
        }

        /// <summary>Nivel II: los proyectiles enemigos del radio vuelven hacia quien los lanzó, ya como disparos del jugador.</summary>
        void ReturnProjectiles()
        {
            Vector2 center = transform.position;
            toReturn.Clear();
            foreach (var hit in Physics2D.OverlapCircleAll(center, attack.SpinRadius, LayerMask.GetMask(CombatLayers.EnemyProjectile)))
            {
                var projectile = hit.GetComponent<Projectile>();
                if (projectile != null && projectile.Attack != null && projectile.Attack.ProjectilePrefab != null && !toReturn.Contains(projectile))
                    toReturn.Add(projectile);
            }

            foreach (var projectile in toReturn)
            {
                var returned = projectile.Attack;
                Vector2 position = projectile.transform.position;
                Vector2 direction = -projectile.Velocity;
                if (direction.sqrMagnitude < 0.0001f)
                    direction = position - center;
                if (direction.sqrMagnitude < 0.0001f)
                    direction = Vector2.right;
                projectile.Despawn();
                ProjectilePool.Get(returned.ProjectilePrefab)
                    .Launch(position, direction.normalized, returned, CombatLayers.ProjectileLayer(Team.Player), weapon);
                OneShotEffect.Spawn(attack.SpinDeflectEffect, position);
            }
        }

        /// <summary>Acaba el giro: bumerán si procede, y la animación de frenado.</summary>
        void Finish(bool dizzy, bool spun)
        {
            RemoveEffect();
            controller.ChannelSpeedFactor = 1f;
            dizzyNext = dizzy;
            if (spun && !dizzy && attack.BoomerangAfter > 0f && spinTime >= attack.BoomerangAfter)
                ThrowBoomerang();

            if (animator.Hold("SpinEnd"))
            {
                phaseTimer = animator.ClipLength("SpinEnd", DefaultEndingSeconds);
                State = Phase.Ending;
            }
            else
            {
                AfterEnding();
            }
        }

        void ThrowBoomerang()
        {
            if (attack.BoomerangPrefab == null)
                return;
            int damage = Mathf.Max(0, Mathf.CeilToInt(attack.BoomerangDamage * weapon.DamageMultiplier - 0.001f));
            var boomerang = Instantiate(attack.BoomerangPrefab, transform.position, Quaternion.identity);
            if (boomerang.TryGetComponent<SpinBoomerang>(out var thrown))
            {
                thrownBoomerang = thrown;
                thrown.Launch(transform, controller.AimDirection, attack.BoomerangRange, damage, attack.BoomerangStun, weapon);
            }
            boomerang.SetActive(true);
        }

        void AfterEnding()
        {
            if (dizzyNext)
            {
                animator.Hold("Dizzy");
                // Mareado no puede andar ni esquivar.
                controller.MovementLocked = true;
                phaseTimer = attack.SpinDizzySeconds > 0f ? attack.SpinDizzySeconds : DefaultDizzySeconds;
                State = Phase.Dizzy;
                return;
            }
            Rest();
        }

        /// <summary>Vuelve a reposo con la espera de después de girar.</summary>
        void Rest()
        {
            controller.ChannelSpeedFactor = 1f;
            controller.MovementLocked = false;
            animator.Release();
            readyTime = Time.time + (attack != null ? attack.SpinRecharge : 0f);
            State = Phase.Idle;
            dizzyNext = false;
        }

        /// <summary>Corta el giro sin mareo (esquiva, muerte, cambio de Boca): la animación vuelve a la normal.</summary>
        public void Cancel()
        {
            RemoveEffect();
            if (State == Phase.Idle)
                return;
            Rest();
        }

        void RemoveEffect()
        {
            if (effect != null)
                Destroy(effect);
            effect = null;
        }
    }
}
