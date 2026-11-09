using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El veneno del piso 3 (charcos, nube tóxica, bulbos): envenenar a un enemigo con la marca de gotas del arte y el tinte lima,
    /// y frenarlo. Al jugador lo envenena <see cref="PlayerToxin"/>, con sus propias reglas.
    /// </summary>
    public static class Toxins
    {
        /// <summary>Un charco frena un 30 %.</summary>
        public const float PuddleSlow = 0.7f;

        /// <summary>
        /// Envenena a un enemigo (daño por segundo durante unos segundos, renovado si ya lo estaba), con la marca de gotas y el
        /// tinte lima. Los jefes lo reciben con el tope de veneno de siempre.
        /// </summary>
        public static void PoisonEnemy(Health health, int damagePerSecond, float seconds)
        {
            if (health == null || health.IsDead)
                return;
            var kit = ReactionKit.Current;
            Poison.Apply(health, damagePerSecond, seconds, kit != null ? kit.PoisonedMarker : null);
            if (!health.TryGetComponent<PoisonTint>(out _))
                health.gameObject.AddComponent<PoisonTint>();
        }

        /// <summary>Veneno virulento del Escupitajo: golpes de <see cref="VenomDamage"/> cada <see cref="VenomTick"/> s durante <see cref="VenomSeconds"/> s.</summary>
        public const int VenomDamage = 2;
        public const float VenomTick = 0.5f;
        public const float VenomSeconds = 3f;

        public static void VenomEnemy(Health health)
        {
            if (health == null || health.IsDead)
                return;
            var kit = ReactionKit.Current;
            Poison.ApplyVenom(health, VenomDamage, VenomTick, VenomSeconds, kit != null ? kit.PoisonedMarker : null);
            if (!health.TryGetComponent<PoisonTint>(out _))
                health.gameObject.AddComponent<PoisonTint>();
        }

        /// <summary>Frena a un enemigo que anda por el charco.</summary>
        public static void SlowEnemy(Health health, float seconds)
        {
            if (health != null && health.TryGetComponent<EnemyAI>(out var ai))
                ai.Slow(PuddleSlow, seconds);
        }
    }
}
