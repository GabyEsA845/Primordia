using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Clase de un linaje: decide el tipo de beneficio que da al completarse.</summary>
    public enum LineageClass
    {
        Carnivore,
        Herbivore,
        Venomous,
        Fungal,
        Scavenger, // Carroñero (piso 2)
        Parasite   // Parásito (piso 3)
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
        // Piso 2
        Feast,           // Placodermo: rematar con un mordisco cura medio corazón
        FiveEyes,        // Opabinia: los Ojos compuestos duran más y los remates los alargan
        StingingPolyps,  // Burgessomedusa: las larvas envenenan al morder
        StingingBed,     // Anémona: quien pisa tus púas queda envenenado
        NutrientCurrent, // Lirio de mar: el remolino atrae la biomasa
        Harvest,         // Wiwaxia: una tanda de escleritos que alcanza a 3 da medio corazón verde
        CarrionArmor,    // Trilobite: tras comer restos, el siguiente proyectil se deshace en las placas
        DoubleServing,   // Hallucigenia: las púas que matan dejan restos dobles
        // Piso 3
        MudStalker,      // Eryops: cruza el lodo sin hundirse, +30 % de velocidad en él y 5 s después
        CephalicSpine,   // Xenacanthus: los confundidos cuentan como heridos
        GasSting,        // Pulmonoscorpius: el chorro envenena y vuelve tóxico el metano
        HundredLegs,     // Ciempiés: charco de veneno al aterrizar e inmunidad al veneno
        Tinder,          // Yesquero: la bola arde y deja fuego en vez de veneno
        Zombie,          // Cordyceps: el confundido que muere es tu aliado
        SpinningSucker,  // Lamprea: girando, los parásitos se duplican y el máximo sube a 6
        LeechPool,       // Sanguijuela: los charcos pegan parásitos y la bola salta a los enemigos
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

        [Header("Aspecto")]
        [Tooltip("Animaciones que sustituyen a las de la Boca mientras el linaje está activo (vacío = la de la mutación). " +
                 "Mismos estados que la pieza a la que sustituye.")]
        [SerializeField] RuntimeAnimatorController mouthLook;
        [Tooltip("Distancia de la unión a la punta de esa Boca, de donde salen los proyectiles. 0 = la de la mutación.")]
        [SerializeField, Min(0f)] float mouthMuzzleDistance;
        [Tooltip("Animaciones que sustituyen a las de las Extremidades mientras el linaje está activo (vacío = las de la mutación).")]
        [SerializeField] RuntimeAnimatorController limbsLook;
        [Tooltip("Animación de la evolución al completarlo: la célula se transforma en el animal y vuelve.")]
        [SerializeField] GameObject evolutionEffect;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public LineageClass Class => lineageClass;

        /// <summary>
        /// Piso del linaje, el de sus mutaciones (el mayor). Los beneficios de las clases se remodelan en cada piso
        /// (<see cref="PlayerLineage"/>, <see cref="MutationText.ClassBenefit"/>).
        /// </summary>
        public int Floor
        {
            get
            {
                int floor = 1;
                if (mutations != null)
                {
                    foreach (var mutation in mutations)
                    {
                        if (mutation != null)
                            floor = Mathf.Max(floor, mutation.Floor);
                    }
                }
                return floor;
            }
        }
        public LineageTouch Touch => touch;
        public string TouchName => touchName;
        public string Benefit => benefit;
        public float MouthMuzzleDistance => mouthMuzzleDistance;
        public GameObject EvolutionEffect => evolutionEffect;

        /// <summary>Aspecto con el que el linaje sustituye a la pieza de esa ranura, o null si no la cambia (la Piel nunca).</summary>
        public RuntimeAnimatorController LookFor(MutationSlot slot) => slot switch
        {
            MutationSlot.Mouth => mouthLook,
            MutationSlot.Limbs => limbsLook,
            _ => null
        };

        /// <summary>Mutación de esa ranura (null si falta).</summary>
        public MutationData MutationFor(MutationSlot slot)
        {
            int index = (int)slot;
            return mutations != null && index < mutations.Length ? mutations[index] : null;
        }

        /// <summary>
        /// Cuántas de sus 4 mutaciones lleva el jugador ahora. Una mutación especial en una ranura que le falta cuenta como
        /// la suya (comodín), pero solo una: el comodín completa el linaje si se llevan las otras 3.
        /// </summary>
        public int Progress(PlayerMutations player)
        {
            if (player == null || mutations == null)
                return 0;

            int count = 0;
            bool wildcard = false;
            for (int slot = 0; slot < mutations.Length; slot++)
            {
                var mutation = mutations[slot];
                if (mutation != null && player.IsEquipped(mutation))
                {
                    count++;
                    continue;
                }
                var worn = player.Get((MutationSlot)slot);
                if (worn != null && worn.IsSpecial)
                    wildcard = true;
            }
            return count + (wildcard ? 1 : 0);
        }

        /// <summary>True si lo completa una mutación especial (comodín) en lugar de la suya.</summary>
        public bool CompletedByWildcard(PlayerMutations player)
        {
            if (!IsComplete(player))
                return false;
            foreach (var mutation in mutations)
            {
                if (mutation != null && !player.IsEquipped(mutation))
                    return true;
            }
            return false;
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

        internal void ConfigureLook(RuntimeAnimatorController newMouthLook, float newMouthMuzzleDistance,
            RuntimeAnimatorController newLimbsLook, GameObject newEvolutionEffect)
        {
            mouthLook = newMouthLook;
            mouthMuzzleDistance = Mathf.Max(0f, newMouthMuzzleDistance);
            limbsLook = newLimbsLook;
            evolutionEffect = newEvolutionEffect;
        }

        internal void ConfigureTouch(LineageTouch newTouch, string newTouchName, string newBenefit)
        {
            touch = newTouch;
            touchName = newTouchName;
            benefit = newBenefit;
        }
    }
}
