using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cola propulsora (Extremidades, piso 2): dos esquivas seguidas antes de la recarga. Nivel II: la segunda deja 3
    /// burbujas que hacen 2 de daño y empujan. Nivel III (Torbellino): las dos esquivas dejan 5 burbujas de 3 de daño.
    /// Ver <see cref="TailBubbles"/> y <see cref="TailBubble"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Tail Propulsor", fileName = "Mutation_TailPropulsor")]
    public class TailPropulsorMutation : MutationData
    {
        [Tooltip("Esquivas extra que se encadenan antes de la recarga.")]
        [SerializeField, Min(0)] int extraDodges = 1;
        [Tooltip("Burbujas que deja cada esquiva que las suelta, por nivel (I, II, III).")]
        [SerializeField] int[] bubbleCounts = { 0, 3, 5 };
        [Tooltip("Daño de cada burbuja, por nivel.")]
        [SerializeField] int[] bubbleDamage = { 0, 2, 3 };
        [SerializeField] TailBubble bubblePrefab;
        [Tooltip("La burbuja del Torbellino (nivel III).")]
        [SerializeField] TailBubble bigBubblePrefab;
        [SerializeField, Min(0.1f)] float bubbleLifetime = 3f;
        [Tooltip("Velocidad del empujón a quien revienta una burbuja.")]
        [SerializeField, Min(0f)] float bubblePush = 6f;
        [Tooltip("Distancia al jugador del corro de burbujas.")]
        [SerializeField, Min(0f)] float ringRadius = 0.8f;

        public int BubbleCountAt(int level) => bubbleCounts[Mathf.Clamp(level - 1, 0, bubbleCounts.Length - 1)];
        public int BubbleDamageAt(int level) => bubbleDamage[Mathf.Clamp(level - 1, 0, bubbleDamage.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            player.Controller.ExtraDodges = extraDodges;
            int count = BubbleCountAt(level);
            if (count <= 0)
                return;
            // Nivel II: solo la segunda esquiva; nivel III: las dos.
            bool everyDodge = level >= MaxLevel;
            var prefab = everyDodge && bigBubblePrefab != null ? bigBubblePrefab : bubblePrefab;
            player.AddBehaviour<TailBubbles>(Slot).Configure(player.Controller, prefab, count, BubbleDamageAt(level),
                bubbleLifetime, bubblePush, ringRadius, everyDodge);
        }

        public override void Remove(PlayerMutations player, int level) => player.Controller.ExtraDodges = 0;

        internal void ConfigureBubbles(TailBubble newBubble, TailBubble newBigBubble)
        {
            bubblePrefab = newBubble;
            bigBubblePrefab = newBigBubble;
        }
    }
}
