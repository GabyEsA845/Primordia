using System;
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

        public EnemyData Data => data;
        public Health Health { get; private set; }

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

        void OnDied()
        {
            // Primero los hijos: quien cuente enemigos vivos (la sala) los registra antes de recibir Died.
            SpawnSplits();
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        void SpawnSplits()
        {
            if (data == null || data.SplitCount == 0)
                return;

            var target = TryGetComponent<EnemyAI>(out var ai) ? ai.Target : null;
            float step = 360f / data.SplitCount;

            for (int i = 0; i < data.SplitCount; i++)
            {
                // Repartidos en círculo para que no aparezcan uno encima de otro.
                Vector2 offset = Quaternion.Euler(0f, 0f, step * i + 45f) * Vector2.right * 0.5f;
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
