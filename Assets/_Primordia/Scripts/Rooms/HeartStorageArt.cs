using System;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Arte del almacén de corazones y de la cucaracha ladrona del piso 3 (docs/multimedia-temp/floor3-environment/pieces y floor3-roach):
    /// la vaina de la tienda y las hojas de la cucaracha. Lo crea Primordia > Floor 3 > Importar almacén de corazones y cucaracha en
    /// Resources/HeartStorageArt. Sin él, el almacén y el robo no funcionan (las pruebas y escenas viejas siguen igual).
    /// </summary>
    public class HeartStorageArt : ScriptableObject
    {
        public const string ResourceName = "HeartStorageArt";

        [Header("Vaina de la tienda (32×48)")]
        [Tooltip("5 cuadros: el cuadro n = n medios corazones guardados.")]
        public Sprite[] podStored = Array.Empty<Sprite>();
        [Tooltip("Late al recibir un corazón.")]
        public Sprite[] podReceive = Array.Empty<Sprite>();

        [Header("Cucaracha (40×28, mira a la derecha)")]
        public Sprite[] idle = Array.Empty<Sprite>();
        public Sprite[] run = Array.Empty<Sprite>();
        [Tooltip("Coge el botín; el cuadro que cuenta es el 3.º.")]
        public Sprite[] grab = Array.Empty<Sprite>();
        public Sprite[] hurt = Array.Empty<Sprite>();
        public Sprite[] death = Array.Empty<Sprite>();
        [Tooltip("50×28, sin girar; la grieta está en (10, 14).")]
        public Sprite[] emerge = Array.Empty<Sprite>();
        [Tooltip("50×28, sin girar; la grieta está en (39, 14). El cuadro que cuenta es el 3.º.")]
        public Sprite[] hide = Array.Empty<Sprite>();
        [Tooltip("La grieta del suelo sola (20×12).")]
        public Sprite crack;

        static HeartStorageArt current;
        static bool loaded;

        public static HeartStorageArt Current
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    current = Resources.Load<HeartStorageArt>(ResourceName);
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

        public bool HasRoach => run != null && run.Length > 0 && emerge != null && emerge.Length > 0 && hide != null && hide.Length > 0
                                && grab != null && grab.Length > 0;
    }
}
