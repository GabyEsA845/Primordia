using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Daña a lo que toca, mientras siga en contacto. La invulnerabilidad tras un golpe
    /// del jugador evita que reciba daño en cada frame.
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField, Min(0)] int damage = 1;
        [Tooltip("Capas a las que daña. Si está vacío, usa la capa Player.")]
        [SerializeField] LayerMask targets;

        public int Damage
        {
            get => damage;
            set => damage = Mathf.Max(0, value);
        }

        void Awake()
        {
            if (targets.value == 0)
                targets = LayerMask.GetMask(CombatLayers.Player);
        }

        void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);

        void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

        void TryDamage(Collider2D other)
        {
            if (damage == 0 || (targets.value & (1 << other.gameObject.layer)) == 0)
                return;

            var health = other.GetComponentInParent<Health>();
            if (health == null)
                return;

            // La esquiva justa de la Cutícula devuelve el golpe al que toca.
            var reflector = other.GetComponentInParent<IHitReflector>();
            if (reflector != null && reflector.TryReflect(GetComponentInParent<Health>()))
                return;

            health.TakeDamage(damage);
        }
    }
}
