using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Equipo de partida para las escenas de prueba: al empezar, el jugador lleva estas mutaciones con este nivel, como si
    /// viniera de pisos anteriores (Test_Floor2 empieza con una Boca del piso 2 y varias del piso 1 a nivel II).
    /// </summary>
    public class TestLoadout : MonoBehaviour
    {
        [SerializeField] MutationData[] mutations = System.Array.Empty<MutationData>();
        [SerializeField, Range(1, MutationData.MaxLevel)] int level = 2;

        void Start()
        {
            if (!TryGetComponent<PlayerMutations>(out var player))
                return;

            foreach (var mutation in mutations)
            {
                if (mutation == null)
                    continue;
                player.Equip(mutation);
                while (player.LevelOf(mutation.Slot) < level && player.LevelUp(mutation.Slot))
                {
                }
            }
        }

        internal void Configure(MutationData[] newMutations, int newLevel)
        {
            mutations = newMutations ?? System.Array.Empty<MutationData>();
            level = Mathf.Clamp(newLevel, 1, MutationData.MaxLevel);
        }
    }
}
