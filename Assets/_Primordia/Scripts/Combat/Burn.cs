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

        protected override DamageSource Source => DamageSource.Burn;

        // Cada cuánto prende lo inflamable que toca, y a qué distancia (casi pegado: un helecho al que se acerca).
        const float IgniteEvery = 0.25f;
        const float IgniteRadius = 1.1f;

        // Los enemigos de cuerpo seco (las plantas y la libélula) dejan un rastro de fuego mientras caminan en llamas.
        const float TrailEvery = 0.4f;
        const float TrailMinStep = 0.3f;
        const float TrailSeconds = 2f;
        const float TrailFeet = 0.3f;

        float igniteTimer;
        float trailTimer;
        Vector2 lastTrail;
        bool leavesTrail;
        bool trailChecked;

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

            if (!trailChecked)
            {
                trailChecked = true;
                leavesTrail = LeavesFireTrail(gameObject);
                lastTrail = transform.position;
            }
            if (leavesTrail)
            {
                trailTimer += Time.deltaTime;
                if (trailTimer >= TrailEvery && Vector2.Distance(lastTrail, transform.position) >= TrailMinStep)
                {
                    trailTimer = 0f;
                    lastTrail = transform.position;
                    // El fuego queda a los pies, no en el centro del cuerpo.
                    Primordia.Rooms.TinderScorch.Attach(Primordia.Rooms.GroundFire.Spawn((Vector2)transform.position + Vector2.down * TrailFeet, TrailSeconds));
                }
            }
        }

        /// <summary>True si el enemigo tiene el cuerpo seco o vegetal (Calamites y su brote, Medullosa, libélula): arde dejando fuego.</summary>
        public static bool LeavesFireTrail(GameObject enemy) =>
            enemy.GetComponent<Primordia.Enemies.CalamitesBrain>() != null || enemy.GetComponent<Primordia.Enemies.CalamitesSproutBrain>() != null
            || enemy.GetComponent<Primordia.Enemies.MedullosaBrain>() != null || enemy.GetComponent<Primordia.Enemies.DragonflyBrain>() != null;

        /// <summary>Null si el objetivo es inmune al fuego.</summary>
        public static Burn Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null) =>
            FireImmune.Has(target) ? null : Apply<Burn>(target, damagePerSecond, duration, effectPrefab);
    }
}
