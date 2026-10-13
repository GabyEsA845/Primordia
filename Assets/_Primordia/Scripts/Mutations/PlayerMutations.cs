using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Las 4 ranuras del cuerpo del jugador. Equipar una mutación en una ranura ocupada
    /// reemplaza la anterior: primero se deshace su efecto y luego se aplica el nuevo.
    /// La Piel cambia también el radio de colisión; el aspecto de las piezas lo dibuja <see cref="MutationLook"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health), typeof(Weapon))]
    public class PlayerMutations : MonoBehaviour
    {
        [Tooltip("Mutaciones con las que empieza la partida. Útil para probar.")]
        [SerializeField] MutationData[] startingMutations = Array.Empty<MutationData>();

        public PlayerController Controller { get; private set; }
        public Health Health { get; private set; }
        public Weapon Weapon { get; private set; }
        /// <summary>Habilidad del Órgano especial. Se busca al usarla, por si se añadió después.</summary>
        public PlayerAbility Ability => ability != null ? ability : (ability = GetComponent<PlayerAbility>());

        PlayerAbility ability;

        /// <summary>Ataque sin mutación de Boca; se recupera al quitarla.</summary>
        public AttackData DefaultAttack { get; private set; }
        /// <summary>Vida máxima de la célula sin mutaciones; las estadísticas de la pausa comparan con ella.</summary>
        public int BaseMaxHealth { get; private set; }
        /// <summary>Velocidad de la célula sin mutaciones.</summary>
        public float BaseMoveSpeed { get; private set; }

        /// <summary>Se lanza al cambiar una ranura: (ranura, mutación nueva o null si quedó vacía).</summary>
        public event Action<MutationSlot, MutationData> Changed;

        readonly Dictionary<MutationSlot, MutationData> equipped = new();
        readonly Dictionary<MutationSlot, List<Component>> behaviours = new();

        public IEnumerable<MutationData> Equipped => equipped.Values;

        CircleCollider2D bodyCollider;
        float cellRadius;

        void Awake()
        {
            Controller = GetComponent<PlayerController>();
            Health = GetComponent<Health>();
            Weapon = GetComponent<Weapon>();
            DefaultAttack = Weapon.Attack;
            BaseMaxHealth = Health.Max;
            BaseMoveSpeed = Controller.MoveSpeed;
            bodyCollider = GetComponent<CircleCollider2D>();
            if (bodyCollider != null)
                cellRadius = bodyCollider.radius;
        }

        void Start()
        {
            foreach (var mutation in startingMutations)
                Equip(mutation);
        }

        public MutationData Get(MutationSlot slot) => equipped.TryGetValue(slot, out var mutation) ? mutation : null;

        public bool IsEquipped(MutationData mutation) =>
            mutation != null && equipped.TryGetValue(mutation.Slot, out var current) && current == mutation;

        public void Equip(MutationData mutation)
        {
            if (mutation == null || IsEquipped(mutation))
                return;

            RemoveFromSlot(mutation.Slot);

            equipped[mutation.Slot] = mutation;
            mutation.Apply(this);
            UpdateBodyRadius();

            Changed?.Invoke(mutation.Slot, mutation);
        }

        public void Unequip(MutationSlot slot)
        {
            if (RemoveFromSlot(slot))
                Changed?.Invoke(slot, null);
        }

        /// <summary>
        /// Añade un componente al jugador que pertenece a la mutación de esa ranura.
        /// Se destruye solo cuando la mutación se quita o se reemplaza.
        /// </summary>
        public T AddBehaviour<T>(MutationSlot slot) where T : Component
        {
            var component = gameObject.AddComponent<T>();
            if (!behaviours.TryGetValue(slot, out var list))
                behaviours[slot] = list = new List<Component>();
            list.Add(component);
            return component;
        }

        bool RemoveFromSlot(MutationSlot slot)
        {
            if (!equipped.TryGetValue(slot, out var old))
                return false;

            old.Remove(this);
            equipped.Remove(slot);

            if (behaviours.TryGetValue(slot, out var list))
            {
                foreach (var component in list)
                {
                    if (component != null)
                        Destroy(component);
                }
                behaviours.Remove(slot);
            }

            UpdateBodyRadius();
            return true;
        }

        /// <summary>La Piel agranda (o encoge) la colisión; sin Piel, vuelve a la de la célula.</summary>
        void UpdateBodyRadius()
        {
            if (bodyCollider == null)
                return;

            var skin = Get(MutationSlot.Skin);
            bodyCollider.radius = skin != null && skin.BodyRadius > 0f ? skin.BodyRadius : cellRadius;
        }
    }
}
