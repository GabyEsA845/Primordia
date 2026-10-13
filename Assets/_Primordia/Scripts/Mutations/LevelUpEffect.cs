using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Efecto sobre el jugador al subir de nivel una mutación: lima en el nivel II y dorado al evolucionar (III).</summary>
    [RequireComponent(typeof(PlayerMutations))]
    public class LevelUpEffect : MonoBehaviour
    {
        [SerializeField] GameObject levelUpEffect;
        [Tooltip("Efecto al llegar al nivel III. Vacío = el normal.")]
        [SerializeField] GameObject evolveEffect;

        PlayerMutations mutations;

        void Awake() => mutations = GetComponent<PlayerMutations>();

        void OnEnable() => mutations.LevelChanged += OnLevelChanged;

        void OnDisable() => mutations.LevelChanged -= OnLevelChanged;

        internal void Configure(GameObject newLevelUpEffect, GameObject newEvolveEffect)
        {
            levelUpEffect = newLevelUpEffect;
            evolveEffect = newEvolveEffect;
        }

        void OnLevelChanged(MutationSlot slot, int level)
        {
            var prefab = level >= MutationData.MaxLevel && evolveEffect != null ? evolveEffect : levelUpEffect;
            OneShotEffect.Spawn(prefab, transform.position, parent: transform);
        }
    }
}
