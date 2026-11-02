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
    /// Familia de arma de una Boca: decide la forma del jugador desde el piso 2 (una criatura por familia en cada piso).
    /// Sin Boca se usa la de proyectil, porque el disparo base es un proyectil.
    /// </summary>
    public enum WeaponFamily
    {
        Projectile,
        Melee,
        Area
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

        [Header("Piso")]
        [Tooltip("Piso al que pertenece: desde el piso 2, cada piso solo ofrece las suyas.")]
        [SerializeField, Min(1)] int floor = 1;
        [Tooltip("Mutación especial: solo sale de las almejas grandes, completa cualquier linaje del que se lleven las otras 3 "
            + "(comodín) y, mientras se lleva, cada golpe quita medio corazón más (Inestable). No está en la reserva.")]
        [SerializeField] bool special;
        [Tooltip("Solo Boca: familia de arma, que decide la criatura del jugador desde el piso 2.")]
        [SerializeField] WeaponFamily family;
        [Tooltip("Solo Boca desde el piso 2: la criatura en que se convierte el jugador, en reposo y atacando (de lado). "
            + "Se ve en la ventana de elección de Boca de la sala de paso.")]
        [SerializeField] Sprite[] creatureIdleFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] creatureAttackFrames = System.Array.Empty<Sprite>();
        [Tooltip("Solo Boca desde el piso 2: animaciones del cuerpo de la criatura, con los estados del jugador más "
            + "Attack_side/down/up. Sustituyen a la célula y a la Piel.")]
        [SerializeField] RuntimeAnimatorController creatureBody;
        [Tooltip("Solo Boca desde el piso 2: segundos desde que empieza la animación de ataque del cuerpo hasta su cuadro "
            + "del golpe, cuando sale el proyectil o muerde. 0 = al instante.")]
        [SerializeField, Min(0f)] float attackHitTime;
        [Tooltip("Solo Boca desde el piso 2: peso del cuerpo al arrancar y frenar. 1 = normal, más resbala, menos es más ágil.")]
        [SerializeField, Min(0f)] float moveInertia = 1f;
        [Tooltip("Solo Piel desde el piso 2: su capa encima de cada cuerpo de criatura (las animaciones del cuerpo y las de "
            + "la capa, con los mismos estados).")]
        [SerializeField] CreatureLayer[] creatureLayers = System.Array.Empty<CreatureLayer>();
        [Tooltip("Solo Boca desde el piso 2: metamorfosis, la silueta blanca que pasa de la célula a la criatura.")]
        [SerializeField] Sprite[] morphFrames = System.Array.Empty<Sprite>();
        [Tooltip("Solo Boca desde el piso 2: de la silueta blanca a los colores de la criatura, al final de la metamorfosis.")]
        [SerializeField] Sprite[] revealFrames = System.Array.Empty<Sprite>();

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
        public int Floor => floor;
        /// <summary>Mutación especial de las almejas: comodín de linaje y precio Inestable.</summary>
        public bool IsSpecial => special;
        public WeaponFamily Family => family;
        public Sprite[] CreatureIdleFrames => creatureIdleFrames ?? System.Array.Empty<Sprite>();
        public Sprite[] CreatureAttackFrames => creatureAttackFrames ?? System.Array.Empty<Sprite>();
        public RuntimeAnimatorController CreatureBody => creatureBody;
        /// <summary>Capa de una Piel o unas Extremidades sobre un cuerpo de criatura: las animaciones del cuerpo y las de la capa.</summary>
        [System.Serializable]
        public struct CreatureLayer
        {
            public RuntimeAnimatorController body;
            public RuntimeAnimatorController layer;
        }

        /// <summary>Solo Boca: segundos hasta el cuadro del golpe de la animación de ataque de su criatura.</summary>
        public float AttackHitTime => attackHitTime;

        /// <summary>Solo Boca: peso del cuerpo de su criatura al arrancar y frenar (ver PlayerController.Inertia).</summary>
        public float MoveInertia => moveInertia;

        /// <summary>Piel y Extremidades: animaciones de su capa sobre ese cuerpo de criatura, o null si no tiene.</summary>
        public RuntimeAnimatorController CreatureLayerFor(RuntimeAnimatorController body)
        {
            if (body == null || creatureLayers == null)
                return null;
            foreach (var layer in creatureLayers)
            {
                if (layer.body == body)
                    return layer.layer;
            }
            return null;
        }

        public Sprite[] MorphFrames => morphFrames ?? System.Array.Empty<Sprite>();
        public Sprite[] RevealFrames => revealFrames ?? System.Array.Empty<Sprite>();

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

        internal void SetFloor(int newFloor, WeaponFamily newFamily = WeaponFamily.Projectile)
        {
            floor = Mathf.Max(1, newFloor);
            family = newFamily;
        }

        internal void SetSpecial(bool newSpecial) => special = newSpecial;

        internal void SetAttackHitTime(float seconds) => attackHitTime = Mathf.Max(0f, seconds);

        internal void SetMoveInertia(float inertia) => moveInertia = Mathf.Max(0f, inertia);

        internal void SetCreatureLayers(params CreatureLayer[] layers) => creatureLayers = layers ?? System.Array.Empty<CreatureLayer>();

        internal void SetCreatureFrames(Sprite[] idle, Sprite[] attack)
        {
            creatureIdleFrames = idle ?? System.Array.Empty<Sprite>();
            creatureAttackFrames = attack ?? System.Array.Empty<Sprite>();
        }

        internal void SetCreature(RuntimeAnimatorController body, Sprite[] morph, Sprite[] reveal)
        {
            creatureBody = body;
            morphFrames = morph ?? System.Array.Empty<Sprite>();
            revealFrames = reveal ?? System.Array.Empty<Sprite>();
        }
    }
}
