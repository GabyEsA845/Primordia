using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Clase de un linaje: decide el tipo de beneficio que da al completarse.</summary>
    public enum LineageClass
    {
        Carnivore,
        Herbivore,
        Venomous,
        Fungal
    }

    /// <summary>
    /// Toque propio de cada linaje, que se suma al beneficio de su clase. None = solo el de la clase.
    /// </summary>
    public enum LineageTouch
    {
        None,
        BloodInTheWater, // Tiburón: rematar recarga la esquiva
        BlindPrey,       // Calamar: los cegados cuentan como heridos para Rematar
        Retract,         // Tortuga: usar la Vacuola da invulnerabilidad
        StingingTrail,   // Medusa: la esquiva deja un rastro que envenena
        HiddenSting,     // Escorpión: el ataque desde el camuflaje envenena el triple
        BigColony,       // Moho: más matas y más grandes
        PuffOnHit,       // Cuesco de lobo: al recibir un golpe, nube que ciega
    }

    /// <summary>
    /// Linaje: un animal (Tiburón, Medusa...) que se completa con 4 mutaciones, una por ranura. Se activa cuando
    /// el jugador lleva las 4 a la vez (en cualquier nivel) y se pierde al cambiar una. Una mutación puede estar en
    /// varios linajes; cada mutación guarda los suyos en <see cref="MutationData.Lineages"/> para la interfaz.
    /// El beneficio es el de su clase (<see cref="MutationText.ClassBenefit"/>) más su toque propio; los pone y los
    /// quita <see cref="PlayerLineage"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Lineage", fileName = "Lineage_")]
    public class LineageData : ScriptableObject
    {
        [SerializeField] string displayName;
        [SerializeField] LineageClass lineageClass;
        [Tooltip("Las 4 mutaciones del linaje, en el orden de las ranuras: Boca, Extremidades, Piel y Órgano.")]
        [SerializeField] MutationData[] mutations = new MutationData[4];
        [Tooltip("Toque propio, que se suma al beneficio de la clase.")]
        [SerializeField] LineageTouch touch;
        [Tooltip("Nombre del toque propio (\"Sangre en el agua\").")]
        [SerializeField] string touchName;
        [Tooltip("Lo que hace el toque propio (se muestra en la pausa).")]
        [SerializeField, TextArea(2, 4)] string benefit;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public LineageClass Class => lineageClass;
        public LineageTouch Touch => touch;
        public string TouchName => touchName;
        public string Benefit => benefit;

        /// <summary>Mutación de esa ranura (null si falta).</summary>
        public MutationData MutationFor(MutationSlot slot)
        {
            int index = (int)slot;
            return mutations != null && index < mutations.Length ? mutations[index] : null;
        }

        /// <summary>Cuántas de sus 4 mutaciones lleva el jugador ahora.</summary>
        public int Progress(PlayerMutations player)
        {
            if (player == null || mutations == null)
                return 0;

            int count = 0;
            foreach (var mutation in mutations)
            {
                if (mutation != null && player.IsEquipped(mutation))
                    count++;
            }
            return count;
        }

        public bool IsComplete(PlayerMutations player) => Progress(player) == SlotCount;

        public static int SlotCount => System.Enum.GetValues(typeof(MutationSlot)).Length;

        /// <summary>
        /// Pista para el jugador: true si equipar esa mutación acerca algún linaje que ya ha empezado (lleva al menos
        /// una de sus mutaciones) por encima del más avanzado que tiene ahora. Con un linaje ya completo nunca es
        /// true, para no invitar a romperlo; tampoco si ya la lleva (subir de nivel no añade partes).
        /// </summary>
        public static bool Advances(MutationData mutation, PlayerMutations player)
        {
            if (mutation == null || player == null || player.IsEquipped(mutation))
                return false;

            int best = 0;
            foreach (var equipped in player.Equipped)
            {
                foreach (var lineage in equipped.Lineages)
                {
                    if (lineage != null)
                        best = Mathf.Max(best, lineage.Progress(player));
                }
            }

            foreach (var lineage in mutation.Lineages)
            {
                if (lineage == null)
                    continue;

                // La mutación ocupa la ranura que le toca en ese linaje, así que reemplaza a una que no contaba.
                int progress = lineage.Progress(player);
                if (progress >= 1 && progress + 1 > best)
                    return true;
            }
            return false;
        }

        internal void Configure(string newName, LineageClass newClass, MutationData[] newMutations, string newBenefit)
        {
            displayName = newName;
            lineageClass = newClass;
            mutations = newMutations;
            benefit = newBenefit;
        }

        internal void ConfigureTouch(LineageTouch newTouch, string newTouchName, string newBenefit)
        {
            touch = newTouch;
            touchName = newTouchName;
            benefit = newBenefit;
        }
    }
}
