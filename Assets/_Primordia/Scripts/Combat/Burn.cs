using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Enemigo que arde (piso 3): daño en el tiempo con su marca de llama. El fuego del suelo lo renueva mientras pisa. El
    /// jugador no usa esto: se quema con la barra de <see cref="Primordia.Rooms.PlayerBurn"/>.
    /// </summary>
    public class Burn : DamageOverTime
    {
        public const string Key = "Burn";

        protected override string MarkerKey => Key;

        /// <summary>Null si el objetivo es inmune al fuego.</summary>
        public static Burn Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null) =>
            FireImmune.Has(target) ? null : Apply<Burn>(target, damagePerSecond, duration, effectPrefab);
    }
}
