using System;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Biomass
{
    /// <summary>
    /// Biomasa en el suelo. Al aparecer salta un poco; si el jugador pasa cerca, el imán la atrae, y al
    /// tocarlo se suma a su <see cref="PlayerBiomass"/>. <see cref="Attract"/> la hace volar hacia él
    /// desde cualquier distancia (al limpiar la sala). Sin físicas: todo va por distancia al jugador.
    /// </summary>
    public class BiomassPickup : MonoBehaviour
    {
        [Tooltip("Cuánta biomasa vale.")]
        [SerializeField, Min(1)] int value = 1;
        [Tooltip("Efecto al recogerla.")]
        [SerializeField] GameObject collectEffect;

        [Header("Recogida")]
        [Tooltip("Distancia a la que el imán empieza a atraerla.")]
        [SerializeField, Min(0f)] float magnetRadius = 1.5f;
        [Tooltip("Distancia a la que se recoge.")]
        [SerializeField, Min(0.05f)] float collectRadius = 0.4f;
        [Tooltip("Velocidad al empezar a volar hacia el jugador, en unidades por segundo.")]
        [SerializeField, Min(0f)] float startSpeed = 3f;
        [Tooltip("Cuánto acelera mientras vuela hacia el jugador.")]
        [SerializeField, Min(0f)] float acceleration = 40f;
        [SerializeField, Min(0.1f)] float maxSpeed = 22f;

        [Header("Al aparecer")]
        [Tooltip("Distancia que salta al aparecer, en una dirección al azar.")]
        [SerializeField, Min(0f)] float popDistance = 0.6f;
        [Tooltip("Segundos del salto. Mientras dura, el imán no la atrae.")]
        [SerializeField, Min(0.01f)] float popDuration = 0.3f;

        public int Value => value;
        public bool IsAttracted { get; private set; }

        /// <summary>Se lanza al recogerla (antes de destruirse).</summary>
        public event Action<BiomassPickup> Collected;

        Transform player;
        PlayerBiomass wallet;
        Health playerHealth;
        Vector2 popFrom;
        Vector2 popTo;
        float popStart = -1f;
        float speed;
        bool collected;

        void Start()
        {
            if (player == null)
                FindPlayer();
        }

        void FindPlayer()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null || !playerObject.TryGetComponent(out wallet))
                return;

            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
        }

        internal void Configure(int newValue, GameObject newCollectEffect)
        {
            value = Mathf.Max(1, newValue);
            collectEffect = newCollectEffect;
        }

        internal void SetMagnet(float newMagnetRadius, float newCollectRadius)
        {
            magnetRadius = Mathf.Max(0f, newMagnetRadius);
            collectRadius = Mathf.Max(0.05f, newCollectRadius);
        }

        /// <summary>Salta desde donde está en una dirección al azar (al soltarla un enemigo).</summary>
        public void Pop()
        {
            if (popDistance <= 0f)
                return;

            popFrom = transform.position;
            popTo = popFrom + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.5f, 1f) * popDistance;
            popStart = Time.time;
        }

        /// <summary>Vuela hacia el jugador esté donde esté y se recoge al llegar.</summary>
        public void Attract()
        {
            if (IsAttracted)
                return;

            IsAttracted = true;
            speed = startSpeed;
        }

        void Update()
        {
            if (collected)
                return;

            if (player == null)
            {
                FindPlayer();
                if (player == null)
                    return;
            }

            if (playerHealth != null && playerHealth.IsDead)
                return;

            Vector2 position = transform.position;
            Vector2 target = player.position;
            float distance = Vector2.Distance(position, target);

            if (distance <= collectRadius)
            {
                Collect();
                return;
            }

            if (popStart >= 0f && !IsAttracted)
            {
                float t = (Time.time - popStart) / popDuration;
                if (t < 1f)
                {
                    // Frena al final, como si el agua la parase.
                    float eased = 1f - (1f - t) * (1f - t);
                    transform.position = Vector2.Lerp(popFrom, popTo, eased);
                    return;
                }

                popStart = -1f;
            }

            if (!IsAttracted && distance > magnetRadius)
                return;

            if (speed <= 0f)
                speed = startSpeed;
            speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
            transform.position = Vector2.MoveTowards(position, target, speed * Time.deltaTime);

            if (Vector2.Distance(transform.position, target) <= collectRadius)
                Collect();
        }

        /// <summary>Se suma a la biomasa del jugador, deja su efecto y desaparece.</summary>
        internal void Collect()
        {
            if (collected || wallet == null)
                return;

            collected = true;
            wallet.Add(value);
            OneShotEffect.Spawn(collectEffect, transform.position);
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
