using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Esponja Vauxia (piso 2, apoyo): no se mueve ni ataca. Cuando hay combate, cada 3 s se hincha y emite un pulso
    /// verde que cura 2 a los demás enemigos a 5,2 u (a sí misma no) y les da un escudo leve. A cada uno lo une con ella un
    /// hilo de luz y su cuerpo destella en verde, para que se vea a quién cura. Nada la mueve de su sitio.
    /// Como mucho sale una por sala (lo decide el generador, con el tope de su entrada en el piso).
    /// </summary>
    public class VauxiaBrain : EnemyBrain
    {
        public enum Phase { Idle, Heal }

        [SerializeField, Min(0.5f)] float pulseInterval = 3f;
        [SerializeField, Min(0.5f)] float pulseRadius = 5.2f;
        [SerializeField, Min(0)] int healAmount = 2;
        [Tooltip("Daño que absorbe el escudo de cada pulso.")]
        [SerializeField, Min(0)] int shieldPoints = 2;
        [Tooltip("Segundos que dura el escudo (algo más que entre dos pulsos, para que no parpadee).")]
        [SerializeField, Min(0f)] float shieldDuration = 3.5f;
        [Tooltip("Cuadro de la animación de curar en el que sale el pulso (desde 0), de cuántos.")]
        [SerializeField, Min(0)] int pulseFrame = 2;
        [SerializeField, Min(1)] int healFrames = 5;

        [SerializeField] GameObject pulseEffect;
        [SerializeField] GameObject shieldMarker;
        [SerializeField] GameObject healEffect;
        [Tooltip("Cuadros de cada cuenta del hilo de luz (encendida → apagada).")]
        [SerializeField] Sprite[] threadFrames;

        const float DefaultHeal = 0.55f;

        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>Se lanza con cada pulso, con los enemigos a los que alcanzó.</summary>
        public event System.Action<IReadOnlyList<Health>> Pulsed;

        readonly List<Health> reached = new();
        float phaseStart;
        float phaseUntil;
        float nextPulse;
        bool pulsed;

        public override string AnimationState => Current == Phase.Heal ? "Heal" : "Idle";

        public override bool ShowsHurt => Current == Phase.Idle;

        internal void Configure(GameObject pulse, GameObject shield, GameObject heal, Sprite[] thread)
        {
            pulseEffect = pulse;
            shieldMarker = shield;
            healEffect = heal;
            threadFrames = thread;
        }

        protected override void Awake()
        {
            base.Awake();
            // Ni empujones, ni remolinos, ni el jugador la mueven.
            if (Body != null)
                Body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        void Start() => nextPulse = Time.time + Random.Range(0.8f, 1.5f);

        public override Vector2 Think(Transform target, EnemyData data)
        {
            if (Current == Phase.Heal)
            {
                if (!pulsed && Time.time - phaseStart >= (phaseUntil - phaseStart) * pulseFrame / healFrames)
                {
                    pulsed = true;
                    Pulse();
                }
                if (Time.time >= phaseUntil)
                    Current = Phase.Idle;
                return Vector2.zero;
            }

            // Despierta cuando te ve (o cuando la golpean) y desde ahí cura sin parar.
            if (target != null && Vector2.Distance(target.position, transform.position) <= data.DetectionRange
                && AI.Sees(target.position))
                AI.Alert();

            if (AI.IsAlert && Time.time >= nextPulse)
            {
                float length = Animator != null ? Animator.LengthOf("Heal") : 0f;
                Current = Phase.Heal;
                phaseStart = Time.time;
                phaseUntil = Time.time + (length > 0f ? length : DefaultHeal);
                pulsed = false;
                nextPulse = Time.time + pulseInterval;
            }
            return Vector2.zero;
        }

        /// <summary>Cura y escuda a los demás enemigos vivos dentro del radio.</summary>
        void Pulse()
        {
            OneShotEffect.Spawn(pulseEffect, transform.position);
            reached.Clear();
            var mask = LayerMask.GetMask(CombatLayers.Enemy);
            foreach (var other in Physics2D.OverlapCircleAll(transform.position, pulseRadius, mask))
            {
                var health = other.GetComponentInParent<Health>();
                if (health == null || health == Health || health.IsDead || reached.Contains(health)
                    || health.GetComponentInParent<Enemy>() == null)
                    continue;
                reached.Add(health);
                health.Heal(healAmount);
                VauxiaShield.Give(health, shieldPoints, shieldDuration, shieldMarker);
                OneShotEffect.Spawn(healEffect, health.transform.position, 0f, health.transform);
                VauxiaThread.Spawn(threadFrames, transform, health.transform);
                HealGlow.Play(health);
            }
            Pulsed?.Invoke(reached);
        }
    }
}
