using System;
using System.Collections;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Aplica un <see cref="EnemyData"/> al enemigo (vida, ataque, daño por contacto)
    /// y gestiona su muerte: avisa, se divide si corresponde y se destruye.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] EnemyData data;

        [Header("Animación")]
        [Tooltip("Segundos que sigue en escena tras morir, para ver la animación Death. 0 = desaparece en el acto.")]
        [SerializeField, Min(0f)] float deathDuration;
        [Tooltip("Segundos que tarda en dividirse (animación Split). Las crías aparecen al final. 0 = en el acto.")]
        [SerializeField, Min(0f)] float splitDuration;

        public EnemyData Data => data;
        public Health Health { get; private set; }

        /// <summary>True si al morir se divide en otros enemigos.</summary>
        public bool WillSplit => data != null && data.SplitCount > 0 && data.SplitPrefab != null;

        /// <summary>Se lanza al morir, después de <see cref="ChildSpawned"/> y antes de destruirse.</summary>
        public event Action<Enemy> Died;

        /// <summary>Se lanza por cada enemigo nuevo que nace de este: al dividirse o al ser invocado (jefe).</summary>
        public event Action<Enemy> ChildSpawned;

        /// <summary>Avisa de un enemigo nuevo creado por este, para que la sala lo cuente.</summary>
        public void RegisterChild(Enemy child) => ChildSpawned?.Invoke(child);

        void Awake()
        {
            Health = GetComponent<Health>();

            var body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (data != null)
                ApplyData();
        }

        void OnEnable() => Health.Died += OnDied;

        void OnDisable() => Health.Died -= OnDied;

        internal void SetData(EnemyData newData)
        {
            data = newData;
            ApplyData();
        }

        void ApplyData()
        {
            Health.SetMax(data.MaxHealth, healDifference: false);
            Health.ResetHealth();

            if (TryGetComponent<Weapon>(out var weapon) && data.Attack != null)
                weapon.Attack = data.Attack;

            if (TryGetComponent<ContactDamage>(out var contact))
                contact.Damage = data.ContactDamage;
        }

        internal void SetAnimationTimings(float newDeathDuration, float newSplitDuration)
        {
            deathDuration = newDeathDuration;
            splitDuration = newSplitDuration;
        }

        void OnDied()
        {
            if (WillSplit && splitDuration > 0f)
            {
                // Died llega al final: la sala sigue en combate hasta que nacen las crías.
                Freeze();
                StartCoroutine(SplitAfterDelay());
                return;
            }

            // Primero los hijos: quien cuente enemigos vivos (la sala) los registra antes de recibir Died.
            SpawnSplits();
            Died?.Invoke(this);

            if (!WillSplit && deathDuration > 0f)
            {
                Freeze();
                Destroy(gameObject, deathDuration);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        IEnumerator SplitAfterDelay()
        {
            yield return new WaitForSeconds(splitDuration);
            SpawnSplits();
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Lo deja quieto y sin colisiones mientras se ve su animación de muerte.</summary>
        void Freeze()
        {
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        void SpawnSplits()
        {
            if (data == null || data.SplitCount == 0)
                return;

            var target = TryGetComponent<EnemyAI>(out var ai) ? ai.Target : null;
            float step = 360f / data.SplitCount;

            for (int i = 0; i < data.SplitCount; i++)
            {
                // Repartidos en círculo para que no aparezcan uno encima de otro. Empezando por la
                // derecha, 2 crías quedan lado a lado, como en el último cuadro de la animación Split.
                Vector2 offset = Quaternion.Euler(0f, 0f, step * i) * Vector2.right * 0.5f;
                var child = Instantiate(data.SplitPrefab, (Vector2)transform.position + offset, Quaternion.identity, transform.parent);

                // Los hijos nacen ya alerta: el jugador acaba de atacar a su "padre".
                if (child.TryGetComponent<EnemyAI>(out var childAi))
                {
                    if (target != null)
                        childAi.Target = target;
                    childAi.Alert();
                }

                RegisterChild(child);
            }
        }
    }
}
