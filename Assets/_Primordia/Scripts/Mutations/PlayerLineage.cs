using System;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Vigila las ranuras del jugador y activa el linaje cuyas 4 mutaciones lleva a la vez. Al cambiar una de ellas
    /// el linaje se pierde (se deshace su beneficio). Los linajes posibles salen de las mutaciones equipadas, así
    /// que no hace falta un catálogo.
    /// </summary>
    [RequireComponent(typeof(PlayerMutations))]
    public class PlayerLineage : MonoBehaviour
    {
        [Tooltip("Efecto sobre el jugador al completar un linaje.")]
        [SerializeField] GameObject activateEffect;

        PlayerMutations mutations;

        /// <summary>Linaje completo ahora mismo, o null.</summary>
        public LineageData Active { get; private set; }

        /// <summary>Se lanza al cambiar el linaje activo: (anterior, nuevo); cualquiera puede ser null.</summary>
        public event Action<LineageData, LineageData> ActiveChanged;

        void Awake() => mutations = GetComponent<PlayerMutations>();

        void OnEnable()
        {
            mutations.Changed += OnChanged;
            Refresh();
        }

        void OnDisable()
        {
            mutations.Changed -= OnChanged;
            SetActive(null);
        }

        internal void Configure(GameObject newActivateEffect) => activateEffect = newActivateEffect;

        void OnChanged(MutationSlot slot, MutationData mutation) => Refresh();

        /// <summary>Busca el primer linaje completo entre los de las mutaciones equipadas.</summary>
        public void Refresh()
        {
            LineageData found = null;
            foreach (var mutation in mutations.Equipped)
            {
                foreach (var lineage in mutation.Lineages)
                {
                    if (lineage != null && lineage.IsComplete(mutations))
                    {
                        found = lineage;
                        break;
                    }
                }
                if (found != null)
                    break;
            }
            SetActive(found);
        }

        void SetActive(LineageData lineage)
        {
            if (lineage == Active)
                return;

            var old = Active;
            if (old != null)
                old.Remove(mutations);

            Active = lineage;
            if (lineage != null)
            {
                lineage.Apply(mutations);
                if (activateEffect != null && isActiveAndEnabled)
                    OneShotEffect.Spawn(activateEffect, transform.position, parent: transform);
            }

            ActiveChanged?.Invoke(old, lineage);
        }
    }
}
