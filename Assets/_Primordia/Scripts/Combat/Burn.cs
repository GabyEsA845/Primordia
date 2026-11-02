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

        // Cada cuánto prende lo inflamable que toca, y a qué distancia (casi pegado: un helecho al que se acerca).
        const float IgniteEvery = 0.25f;
        const float IgniteRadius = 0.9f;

        float igniteTimer;

        protected override void Update()
        {
            base.Update();
            if (health == null || health.IsDead)
                return;

            // Quien arde prende los helechos y la maleza por los que pasa.
            igniteTimer += Time.deltaTime;
            if (igniteTimer >= IgniteEvery)
            {
                igniteTimer = 0f;
                Primordia.Rooms.Flammables.IgniteNear(transform.position, IgniteRadius);
            }
        }

        /// <summary>Null si el objetivo es inmune al fuego.</summary>
        public static Burn Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null) =>
            FireImmune.Has(target) ? null : Apply<Burn>(target, damagePerSecond, duration, effectPrefab);
    }
}
