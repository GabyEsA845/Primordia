using System;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="HallucigeniaSpinesMutation"/>: al empezar cada esquiva deja una hilera de púas donde
    /// estaba el jugador, atravesada a la dirección de la esquiva (una trampa a la espalda).
    /// </summary>
    public class SpineTraps : MonoBehaviour
    {
        PlayerController controller;
        Weapon weapon;
        GroundSpine prefab;
        int count;
        float duration;
        int damage;
        float spacing;
        AttackData shot;
        float shotRange;

        /// <summary>Se lanza con cada púa que se pone (los toques de los linajes se cuelgan de aquí).</summary>
        public event Action<GroundSpine> Placed;

        public void Configure(PlayerController newController, Weapon newWeapon, GroundSpine newPrefab, int newCount, float newDuration,
            int newDamage, float newSpacing, AttackData newShot, float newShotRange)
        {
            Unsubscribe();
            controller = newController;
            weapon = newWeapon;
            prefab = newPrefab;
            count = newCount;
            duration = newDuration;
            damage = newDamage;
            spacing = newSpacing;
            shot = newShot;
            shotRange = newShotRange;
            if (controller != null)
                controller.DodgeStarted += OnDodgeStarted;
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (controller != null)
                controller.DodgeStarted -= OnDodgeStarted;
        }

        void OnDodgeStarted()
        {
            Vector2 center = transform.position;
            var direction = controller.DodgeDirection.sqrMagnitude > 0.0001f ? controller.DodgeDirection : Vector2.right;
            var across = Vector2.Perpendicular(direction);
            for (int i = 0; i < count; i++)
            {
                var position = center + across * ((i - (count - 1) / 2f) * spacing);
                var spine = GroundSpine.Spawn(prefab, position, damage, duration, shot, shotRange, weapon);
                Placed?.Invoke(spine);
            }
            Sfx.Play(Sounds.HallucigeniaPlace);
        }
    }
}
