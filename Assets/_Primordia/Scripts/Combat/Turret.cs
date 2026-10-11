using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Torreta fija que dispara al jugador cuando está a su alcance.
    /// Sirve para probar el combate y como trampa sencilla en las salas.
    /// </summary>
    [RequireComponent(typeof(Weapon))]
    public class Turret : MonoBehaviour
    {
        [Tooltip("A quién dispara. Si está vacío, busca el objeto con la etiqueta Player.")]
        [SerializeField] Transform target;
        [Tooltip("Distancia máxima a la que dispara, en unidades.")]
        [SerializeField, Min(0f)] float range = 12f;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        Weapon weapon;
        Health health;

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
        }

        void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
        }

        void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return;
            if (health != null && health.IsDead)
                return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                return;

            Vector2 toTarget = target.position - transform.position;
            if (toTarget.sqrMagnitude <= range * range)
                weapon.TryFire(toTarget);
        }
    }
}
