using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte compartido de las reacciones del piso 3 (fuego, metano, veneno): lo que el código necesita cargar en tiempo de
    /// juego. Lo crea Primordia > Floor 3 > Crear piso 3 y lo lleva el <see cref="FloorConfig"/>; <see cref="FloorGenerator"/>
    /// lo deja en <see cref="Current"/> al montar el piso.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Rooms/Reaction Kit", fileName = "ReactionKit_")]
    public class ReactionKit : ScriptableObject
    {
        [Header("Fuego")]
        [Tooltip("Fuego en el suelo (32×24, en bucle). Pivote: abajo.")]
        [SerializeField] Sprite[] groundFire = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float groundFireFrameSeconds = 0.1f;
        [Tooltip("El fuego se apaga (32×32). Pivote: abajo.")]
        [SerializeField] Sprite[] fireOut = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float fireOutFrameSeconds = 0.09f;
        [Tooltip("Marca sobre un enemigo que arde (efecto en bucle).")]
        [SerializeField] GameObject burningMarker;

        [Header("Metano")]
        [Tooltip("Explosión del odre (64×64, una pasada). Pivote: centro.")]
        [SerializeField] Sprite[] explosion = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float explosionFrameSeconds = 0.07f;
        [Tooltip("Casilla de niebla que se prende (32×24, una pasada). Pivote: abajo.")]
        [SerializeField] Sprite[] mistIgnite = System.Array.Empty<Sprite>();
        [Tooltip("Llamarada suelta al final del fogonazo (24×40). Pivote: abajo.")]
        [SerializeField] Sprite[] mistFlare = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float mistIgniteFrameSeconds = 0.07f;

        public static ReactionKit Current { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        public Sprite[] GroundFire => groundFire ?? System.Array.Empty<Sprite>();
        public float GroundFireFrameSeconds => groundFireFrameSeconds;
        public Sprite[] FireOut => fireOut ?? System.Array.Empty<Sprite>();
        public float FireOutFrameSeconds => fireOutFrameSeconds;
        public GameObject BurningMarker => burningMarker;
        public Sprite[] Explosion => explosion ?? System.Array.Empty<Sprite>();
        public float ExplosionFrameSeconds => explosionFrameSeconds;
        public Sprite[] MistIgnite => mistIgnite ?? System.Array.Empty<Sprite>();
        public Sprite[] MistFlare => mistFlare ?? System.Array.Empty<Sprite>();
        public float MistIgniteFrameSeconds => mistIgniteFrameSeconds;

        internal void ConfigureMethane(Sprite[] newExplosion, Sprite[] newMistIgnite, Sprite[] newMistFlare)
        {
            explosion = newExplosion;
            mistIgnite = newMistIgnite;
            mistFlare = newMistFlare;
        }

        internal void ConfigureFire(Sprite[] newGroundFire, Sprite[] newFireOut, GameObject newBurningMarker)
        {
            groundFire = newGroundFire;
            fireOut = newFireOut;
            burningMarker = newBurningMarker;
        }
    }
}
