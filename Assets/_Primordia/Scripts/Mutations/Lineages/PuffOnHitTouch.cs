using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Bocanada al golpe (Cuesco de lobo): al recibir un golpe suelta una nube de tinta alrededor que ciega a los
    /// enemigos, como mucho cada cierto tiempo. La nube es la de la Vejiga de tinta que lleva equipada.
    /// </summary>
    public class PuffOnHitTouch : MonoBehaviour
    {
        PlayerMutations player;
        float radius = 2f;
        float cooldown = 6f;
        float readyTime;

        /// <summary>Nubes soltadas (para las pruebas).</summary>
        public int Puffs { get; private set; }

        public void Configure(PlayerMutations newPlayer, float newRadius, float newCooldown)
        {
            Unsubscribe();
            player = newPlayer;
            radius = newRadius;
            cooldown = newCooldown;
            readyTime = 0f;
            player.Health.Damaged += OnDamaged;
        }

        void OnDamaged(int _)
        {
            if (Time.time < readyTime || player.Health.IsDead)
                return;

            if (player.Get(MutationSlot.Organ) is not InkCloudMutation ink)
                return;

            readyTime = Time.time + cooldown;
            Puffs++;
            ink.SpawnCloud(player.transform.position, radius);
        }

        void Unsubscribe()
        {
            if (player != null)
                player.Health.Damaged -= OnDamaged;
        }

        void OnDestroy() => Unsubscribe();
    }
}
