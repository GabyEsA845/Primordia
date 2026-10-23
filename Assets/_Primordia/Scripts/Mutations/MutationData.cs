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

    /// <summary>
    /// Base de todas las mutaciones: nombre, ranura, linajes, aspecto y niveles. Cada tipo de mutación
    /// (estadísticas, ataque, habilidad...) hereda de aquí y aplica su efecto en <see cref="Apply"/> según el nivel
    /// (1 a <see cref="MaxLevel"/>): el II mejora valores y el III desbloquea una evolución. Los cambios que hace
    /// <see cref="Apply"/> los deshace <see cref="Remove"/> con el mismo nivel; los componentes añadidos con
    /// <see cref="PlayerMutations.AddBehaviour{T}"/> se quitan solos.
    /// </summary>
    public abstract class MutationData : ScriptableObject
    {
        public const int MaxLevel = 3;

        [SerializeField] string displayName;
        [SerializeField, TextArea(2, 4)] string description;
        [SerializeField] MutationSlot slot;
        [Tooltip("Linajes a los que pertenece (los rellena Primordia > Mutations > Crear linajes a partir de cada LineageData).")]
        [SerializeField] LineageData[] lineages = System.Array.Empty<LineageData>();

        [Tooltip("Animaciones de la pieza (se dibuja con MutationLook). En la Piel sustituye a las del cuerpo, " +
                 "con los mismos estados que el jugador.")]
        [SerializeField] RuntimeAnimatorController look;
        [Tooltip("Solo Piel: radio de colisión del cuerpo. 0 = el de la célula.")]
        [SerializeField, Min(0f)] float bodyRadius;
        [Tooltip("Solo Piel: distancia del centro al borde del cuerpo, donde se coloca la Boca. 0 = la de la célula.")]
        [SerializeField, Min(0f)] float mouthDistance;
        [Tooltip("Solo Piel: cuánto más alta (o más baja, negativo) está la cabeza que la de la célula, en unidades. " +
                 "Ahí se pone la Boca que va en la cabeza (el gorrito de las Esporas).")]
        [SerializeField] float headHeight;
        [Tooltip("Solo Boca: va sobre la cabeza y sigue al cuerpo (gorrito de las Esporas) en lugar de girar con el " +
                 "apuntado. Sus animaciones tienen los estados de las Extremidades (Idle_side, Move_down...).")]
        [SerializeField] bool wornOnHead;
        [Tooltip("Solo Boca: distancia de su unión a la punta, de donde salen los proyectiles. 0 = el punto de " +
                 "disparo del jugador.")]
        [SerializeField, Min(0f)] float muzzleDistance;
        [Tooltip("Solo Boca: cuánto se adelanta el cuerpo hacia el cursor al atacar, en unidades (solo se ve).")]
        [SerializeField, Min(0f)] float attackLean;
        [Tooltip("Icono de 16×16: lista de la pausa y burbuja del suelo.")]
        [SerializeField] Sprite icon;
        [Tooltip("Icono de 32×32: tarjetas de elección. Si falta, se usa el pequeño.")]
        [SerializeField] Sprite largeIcon;
        [Tooltip("Color de la mutación en la interfaz (el de su ranura).")]
        [SerializeField] Color color = Color.white;
        [Tooltip("Lo que añade cada nivel: el II y el III (evolución).")]
        [SerializeField, TextArea(2, 4)] string[] levelDescriptions = new string[MaxLevel - 1];
        [Tooltip("Extremidades: sonido de la esquiva con esta mutación (SoundLibrary). Vacío = el normal.")]
        [SerializeField] string dodgeSound;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>Extremidades: sonido de la esquiva con esta mutación (las Aletas). Vacío = el normal.</summary>
        public string DodgeSound => dodgeSound;
        public string Description => description;
        public MutationSlot Slot => slot;
        public System.Collections.Generic.IReadOnlyList<LineageData> Lineages =>
            lineages ?? (System.Collections.Generic.IReadOnlyList<LineageData>)System.Array.Empty<LineageData>();
        public RuntimeAnimatorController Look => look;
        public float BodyRadius => bodyRadius;
        public float MouthDistance => mouthDistance;
        public float HeadHeight => headHeight;
        public bool WornOnHead => wornOnHead;
        public float MuzzleDistance => muzzleDistance;
        public float AttackLean => attackLean;
        public Sprite Icon => icon;
        public Sprite LargeIcon => largeIcon != null ? largeIcon : icon;
        public Color Color => color;

        /// <summary>Lo que añade ese nivel (2 o 3); vacío en el nivel 1 o si no está escrito.</summary>
        public string LevelDescription(int level)
        {
            int index = level - 2;
            return levelDescriptions != null && index >= 0 && index < levelDescriptions.Length ? levelDescriptions[index] ?? "" : "";
        }

        /// <param name="level">Nivel de la mutación, de 1 a <see cref="MaxLevel"/>.</param>
        public abstract void Apply(PlayerMutations player, int level);

        /// <summary>Deshace lo que hizo <see cref="Apply"/> con ese mismo nivel.</summary>
        public virtual void Remove(PlayerMutations player, int level) { }

        /// <summary>Índice (desde 0) del elemento de una lista de mejoras por nivel: el nivel 2 usa el 0. -1 en el nivel 1.</summary>
        protected static int UpgradeIndex<T>(T[] upgrades, int level) =>
            upgrades == null || upgrades.Length == 0 || level < 2 ? -1 : Mathf.Min(level - 2, upgrades.Length - 1);

        internal void ConfigureBase(string newName, string newDescription, MutationSlot newSlot, Color newColor)
        {
            displayName = newName;
            description = newDescription;
            slot = newSlot;
            color = newColor;
        }

        internal void SetLineages(LineageData[] newLineages) => lineages = newLineages ?? System.Array.Empty<LineageData>();

        internal void SetColor(Color newColor) => color = newColor;

        internal void SetDodgeSound(string sound) => dodgeSound = sound;

        internal void SetLevelDescriptions(params string[] descriptions) => levelDescriptions = descriptions;

        internal void SetIcons(Sprite newIcon, Sprite newLargeIcon)
        {
            icon = newIcon;
            largeIcon = newLargeIcon;
        }

        internal void SetLook(RuntimeAnimatorController newLook, float newBodyRadius = 0f, float newMouthDistance = 0f)
        {
            look = newLook;
            bodyRadius = Mathf.Max(0f, newBodyRadius);
            mouthDistance = Mathf.Max(0f, newMouthDistance);
        }

        /// <summary>Cómo se lleva una Boca: en la cabeza o girando con el apuntado, su punta y el adelanto al atacar.</summary>
        internal void SetMouthLook(bool onHead, float newMuzzleDistance, float newAttackLean)
        {
            wornOnHead = onHead;
            muzzleDistance = Mathf.Max(0f, newMuzzleDistance);
            attackLean = Mathf.Max(0f, newAttackLean);
        }

        internal void SetHeadHeight(float height) => headHeight = height;
    }
}
