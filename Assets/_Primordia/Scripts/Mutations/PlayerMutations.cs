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

        /// <summary>
        /// Se lanza al cambiar una ranura: (ranura, mutación nueva o null si quedó vacía). También al subir
        /// de nivel la mutación de la ranura.
        /// </summary>
        public event Action<MutationSlot, MutationData> Changed;

        /// <summary>Se lanza al subir de nivel una mutación: (ranura, nivel nuevo).</summary>
        public event Action<MutationSlot, int> LevelChanged;

        readonly Dictionary<MutationSlot, MutationData> equipped = new();
        readonly Dictionary<MutationSlot, int> levels = new();
        readonly Dictionary<MutationSlot, List<Component>> behaviours = new();

        public IEnumerable<MutationData> Equipped => equipped.Values;

        /// <summary>Mutaciones especiales que lleva: cada una suma medio corazón a cada golpe (precio Inestable).</summary>
        public int SpecialCount
        {
            get
            {
                int count = 0;
                foreach (var mutation in equipped.Values)
                {
                    if (mutation != null && mutation.IsSpecial)
                        count++;
                }
                return count;
            }
        }

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

        /// <summary>Nivel de la mutación de esa ranura (0 si está vacía).</summary>
        public int LevelOf(MutationSlot slot) => levels.TryGetValue(slot, out int level) ? level : 0;

        /// <summary>Nivel de esa mutación si la lleva equipada; 0 si no.</summary>
        public int LevelOf(MutationData mutation) => IsEquipped(mutation) ? LevelOf(mutation.Slot) : 0;

        /// <summary>True si la lleva equipada y aún puede subir de nivel.</summary>
        public bool CanLevelUp(MutationData mutation) => IsEquipped(mutation) && LevelOf(mutation.Slot) < MutationData.MaxLevel;

        /// <summary>
        /// Equipa la mutación en nivel 1, reemplazando a la que hubiera en su ranura (que pierde su nivel).
        /// Si ya la lleva, la sube de nivel.
        /// </summary>
        public void Equip(MutationData mutation)
        {
            if (mutation == null)
                return;

            if (IsEquipped(mutation))
            {
                LevelUp(mutation.Slot);
                return;
            }

            RemoveFromSlot(mutation.Slot);

            equipped[mutation.Slot] = mutation;
            levels[mutation.Slot] = 1;
            mutation.Apply(this, 1);
            UpdateBodyRadius();
            UpdateUnstable();

            Changed?.Invoke(mutation.Slot, mutation);
        }

        /// <summary>
        /// Sube un nivel la mutación de esa ranura (hasta <see cref="MutationData.MaxLevel"/>): deshace el nivel
        /// actual y aplica el siguiente. También la usará la tienda. Devuelve false si no pudo subir.
        /// </summary>
        public bool LevelUp(MutationSlot slot)
        {
            var mutation = Get(slot);
            int level = LevelOf(slot);
            if (mutation == null || level >= MutationData.MaxLevel)
                return false;

            RemoveEffects(slot, mutation, level);
            levels[slot] = level + 1;
            mutation.Apply(this, level + 1);
            UpdateBodyRadius();

            LevelChanged?.Invoke(slot, level + 1);
            Changed?.Invoke(slot, mutation);
            return true;
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

            RemoveEffects(slot, old, LevelOf(slot));
            equipped.Remove(slot);
            levels.Remove(slot);

            UpdateBodyRadius();
            UpdateUnstable();
            return true;
        }

        /// <summary>Precio Inestable: medio corazón más por golpe por cada especial equipada (se acumula).</summary>
        void UpdateUnstable()
        {
            if (Health != null)
                Health.ExtraHitDamage = SpecialCount;
        }

        /// <summary>Deshace el efecto de la mutación en ese nivel y quita sus componentes (al momento, para que no se dupliquen al subir de nivel).</summary>
        void RemoveEffects(MutationSlot slot, MutationData mutation, int level)
        {
            mutation.Remove(this, level);

            if (behaviours.TryGetValue(slot, out var list))
            {
                foreach (var component in list)
                {
                    if (component != null)
                        DestroyImmediate(component);
                }
                behaviours.Remove(slot);
            }
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
