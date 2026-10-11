using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Parte del cuerpo que ocupa una mutación. Solo cabe una por ranura.</summary>
    public enum MutationSlot
    {
        Mouth,
        Limbs,
        Skin,
        Organ
    }

    public enum Lineage
    {
        Predator,
        Armored,
        Toxic
    }

    /// <summary>
    /// Base de todas las mutaciones: nombre, ranura, linaje y aspecto. Cada tipo de mutación
    /// (estadísticas, ataque, habilidad...) hereda de aquí y aplica su efecto en <see cref="Apply"/>.
    /// Los cambios que hace <see cref="Apply"/> los deshace <see cref="Remove"/>; los componentes
    /// añadidos con <see cref="PlayerMutations.AddBehaviour{T}"/> se quitan solos.
    /// </summary>
    public abstract class MutationData : ScriptableObject
    {
        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 4)] string description;
        [SerializeField] MutationSlot slot;
        [SerializeField] Lineage lineage;

        [Tooltip("Aspecto que se añade al cuerpo del jugador. En la boca gira con el apuntado.")]
        [SerializeField] GameObject visualPrefab;
        [Tooltip("Color de la mutación en la interfaz y en los objetos del suelo.")]
        [SerializeField] Color color = Color.white;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public MutationSlot Slot => slot;
        public Lineage Lineage => lineage;
        public GameObject VisualPrefab => visualPrefab;
        public Color Color => color;

        public abstract void Apply(PlayerMutations player);

        public virtual void Remove(PlayerMutations player) { }

        internal void ConfigureBase(string newName, string newDescription, MutationSlot newSlot, Lineage newLineage,
            Color newColor, GameObject newVisual = null)
        {
            displayName = newName;
            description = newDescription;
            slot = newSlot;
            lineage = newLineage;
            color = newColor;
            visualPrefab = newVisual;
        }

        internal void SetVisual(GameObject newVisual) => visualPrefab = newVisual;
    }
}
