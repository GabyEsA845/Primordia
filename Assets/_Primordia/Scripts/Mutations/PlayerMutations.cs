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
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health), typeof(Weapon))]
    public class PlayerMutations : MonoBehaviour
    {
        [Tooltip("Donde se cuelgan los aspectos de Boca. Gira con el apuntado.")]
        [SerializeField] Transform mouthAnchor;
        [Tooltip("Donde se cuelgan los aspectos del resto de ranuras.")]
        [SerializeField] Transform bodyAnchor;
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

        /// <summary>Se lanza al cambiar una ranura: (ranura, mutación nueva o null si quedó vacía).</summary>
        public event Action<MutationSlot, MutationData> Changed;

        readonly Dictionary<MutationSlot, MutationData> equipped = new();
        readonly Dictionary<MutationSlot, GameObject> visuals = new();
        readonly Dictionary<MutationSlot, List<Component>> behaviours = new();

        public IEnumerable<MutationData> Equipped => equipped.Values;

        void Awake()
        {
            Controller = GetComponent<PlayerController>();
            Health = GetComponent<Health>();
            Weapon = GetComponent<Weapon>();
            DefaultAttack = Weapon.Attack;
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
            AddVisual(mutation);

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

            if (visuals.TryGetValue(slot, out var visual))
            {
                if (visual != null)
                    Destroy(visual);
                visuals.Remove(slot);
            }

            return true;
        }

        void AddVisual(MutationData mutation)
        {
            if (mutation.VisualPrefab == null)
                return;

            var anchor = mutation.Slot == MutationSlot.Mouth ? mouthAnchor : bodyAnchor;
            if (anchor == null)
                anchor = transform;

            visuals[mutation.Slot] = Instantiate(mutation.VisualPrefab, anchor, false);
        }

        internal void SetAnchors(Transform mouth, Transform body)
        {
            mouthAnchor = mouth;
            bodyAnchor = body;
        }
    }
}
