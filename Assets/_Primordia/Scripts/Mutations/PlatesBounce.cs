using System;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="TrilobitePlatesMutation"/>: cuenta los proyectiles enemigos que alcanzan al jugador y
    /// hace rebotar uno de cada N, que desaparece sin hacer daño (no se devuelve: eso es de la Cutícula de Marrella). Con
    /// Enrollarse, mientras esquiva (y un instante después) la bola de placas destruye todo proyectil enemigo contra el que
    /// esquiva; sin él, la esquiva solo los deja pasar a través.
    /// </summary>
    public class PlatesBounce : MonoBehaviour, IHitReflector
    {
        /// <summary>Encima del jugador y de los enemigos.</summary>
        const int SortingOrder = 22;
        /// <summary>Radio de la bola de Enrollarse (la dibujada mide 22 px).</summary>
        const float RollRadius = 0.75f;
        /// <summary>Segundos que sigue enrollado después de la esquiva.</summary>
        const float RollGrace = 0.12f;

        PlayerController controller;
        int every;
        bool roll;
        Sprite[] bounceFrames = Array.Empty<Sprite>();
        Sprite[] rollFrames = Array.Empty<Sprite>();
        float frameDuration;
        int counted;
        bool nextBounces;
        float rollUntil;
        static readonly Collider2D[] hits = new Collider2D[16];

        /// <summary>Proyectiles rebotados (para las pruebas y los toques de los linajes).</summary>
        public int Bounced { get; private set; }

        /// <summary>Se lanza con cada rebote.</summary>
        public event Action Bounce;

        internal void Configure(PlayerController newController, int newEvery, bool newRoll, Sprite[] newBounceFrames,
            Sprite[] newRollFrames, float newFrameDuration)
        {
            Unsubscribe();
            controller = newController;
            every = Mathf.Max(1, newEvery);
            roll = newRoll;
            bounceFrames = newBounceFrames ?? Array.Empty<Sprite>();
            rollFrames = newRollFrames ?? Array.Empty<Sprite>();
            frameDuration = newFrameDuration;
            if (controller != null)
            {
                controller.DodgeStarted += OnDodgeStarted;
                controller.DodgeEnded += OnDodgeEnded;
            }
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (controller == null)
                return;
            controller.DodgeStarted -= OnDodgeStarted;
            controller.DodgeEnded -= OnDodgeEnded;
        }

        /// <summary>True mientras la bola de Enrollarse destruye lo que toca.</summary>
        public bool IsRolling => roll && controller != null && (controller.IsDodging || Time.time < rollUntil);

        void OnDodgeEnded() => rollUntil = Time.time + RollGrace;

        /// <summary>
        /// Enrollarse: la bola destruye los proyectiles enemigos que tiene dentro (la esquiva es tan rápida que, si solo
        /// contaran los que entran en el cuerpo justo mientras esquiva, casi ninguno coincidiría).
        /// </summary>
        void Update()
        {
            if (!IsRolling)
                return;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(1 << CombatLayers.ProjectileLayer(Team.Enemy));
            int count = Physics2D.OverlapCircle(transform.position, RollRadius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var projectile = hits[i].GetComponentInParent<Projectile>();
                if (projectile == null || !projectile.isActiveAndEnabled)
                    continue;
                BounceOff(projectile);
                projectile.Despawn();
            }
        }

        /// <summary>Enrollarse: la bola de placas mientras dura la esquiva.</summary>
        void OnDodgeStarted()
        {
            if (roll && rollFrames.Length > 0)
                FrameEffect.Play("PlatesRoll", rollFrames, frameDuration, SortingOrder, transform);
        }

        /// <summary>El siguiente proyectil rebota seguro (la Coraza de carroña del Trilobite).</summary>
        public void BounceNext() => nextBounces = true;

        public bool TryReflect(Projectile projectile)
        {
            if (projectile == null)
                return false;
            // Enrollado destruye todo; si no, esquivando lo atraviesa como siempre y, quieto, rebota uno de cada N.
            if (!IsRolling)
            {
                if (controller != null && controller.IsDodging)
                    return false;
                if (nextBounces)
                {
                    nextBounces = false;
                }
                else
                {
                    counted++;
                    if (counted < every)
                        return false;
                    counted = 0;
                }
            }

            BounceOff(projectile);
            return true;
        }

        void BounceOff(Projectile projectile)
        {
            Bounced++;
            Sfx.Play(Sounds.ShockwaveProjectileBreak);
            FrameEffect.Play("PlatesBounce", bounceFrames, frameDuration, SortingOrder, null, projectile.transform.position);
            Bounce?.Invoke();
        }

        /// <summary>Los golpes cuerpo a cuerpo y de contacto no rebotan.</summary>
        public bool TryReflect(Health attacker) => false;
    }
}
