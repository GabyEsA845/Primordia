using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Hojas de docs/multimedia-temp/ui-extras (corazón de sala, corazón verde entero, iconos del mapa y corazón morado).
    /// Lo crea Primordia > Art > Importar extras de interfaz en Resources/UiExtrasArt. Quien no lo encuentra usa su dibujo
    /// provisional por código, así que las pruebas y las escenas sin importar siguen funcionando.
    /// </summary>
    public class UiExtrasArt : ScriptableObject
    {
        public const string ResourceName = "UiExtrasArt";

        [Header("Medio corazón del suelo")]
        public Sprite[] roomHeartIdle = System.Array.Empty<Sprite>();
        public Sprite roomHeartShadow;
        public Sprite[] roomHeartCollect = System.Array.Empty<Sprite>();
        public Sprite[] roomHeartFly = System.Array.Empty<Sprite>();

        [Header("HUD")]
        public Sprite barrierFull;

        [Header("Mapa")]
        public Sprite mapHeart;
        public Sprite[] mapTeleport = System.Array.Empty<Sprite>();
        public Sprite[] mapTeleportHover = System.Array.Empty<Sprite>();
        public Sprite mapTeleportDim;
        public Sprite mapReturn;
        public Sprite mapReturnHover;
        public Sprite mapReturnDim;

        [Header("Corazón morado")]
        public Sprite[] purpleAura = System.Array.Empty<Sprite>();
        public Sprite[] purpleMote = System.Array.Empty<Sprite>();
        public Sprite[] purpleBlock = System.Array.Empty<Sprite>();

        static UiExtrasArt current;
        static bool loaded;

        /// <summary>El arte importado, o null si aún no se ha importado.</summary>
        public static UiExtrasArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<UiExtrasArt>(ResourceName);
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }
    }
}
