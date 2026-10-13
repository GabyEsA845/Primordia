using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>Sangrado (Mandíbulas nivel III): daño en el tiempo con su marca de gotas rojas.</summary>
    public class Bleed : DamageOverTime
    {
        public const string Key = "Bleed";

        protected override string MarkerKey => Key;

        public static Bleed Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null) =>
            Apply<Bleed>(target, damagePerSecond, duration, effectPrefab);
    }
}
