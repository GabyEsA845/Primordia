using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Arte del Chorro de vinagre (<see cref="AttackData.IsJet"/>), de docs/multimedia-temp/floor3-jet: el chorro se monta con tramos de
    /// 1 u (16 px) hacia la derecha, todos en el mismo cuadro a la vez. El primero es el inicio, los siguientes alternan tres tramos
    /// (A, B, C) y, si el chorro llega a su alcance sin chocar, el último es la punta. Cada hoja tiene <see cref="Frames"/> cuadros
    /// con pivote en el borde izquierdo, a media altura.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Combat/Jet Art", fileName = "JetArt_")]
    public class JetArt : ScriptableObject
    {
        /// <summary>Cuadros de cada hoja y segundos por cuadro (el mismo reloj para todos los tramos).</summary>
        public const int Frames = 8;
        public const float FrameSeconds = 0.06f;
        /// <summary>Letras de los tramos que se repiten (A, B, C).</summary>
        public const int Variants = 3;

        [SerializeField] Sprite[] start = System.Array.Empty<Sprite>();
        [Tooltip("Tramos A, B y C, uno tras otro (3 × 8 cuadros).")]
        [SerializeField] Sprite[] streams = System.Array.Empty<Sprite>();
        [Tooltip("Puntas A, B y C, una tras otra (3 × 8 cuadros).")]
        [SerializeField] Sprite[] tips = System.Array.Empty<Sprite>();

        /// <summary>Hay todas las hojas.</summary>
        public bool IsComplete => start != null && start.Length >= Frames && streams != null && streams.Length >= Frames * Variants
            && tips != null && tips.Length >= Frames * Variants;

        /// <summary>Cuadro que toca ahora, igual para todos los tramos.</summary>
        public static int CurrentFrame => Mathf.FloorToInt(Time.time / FrameSeconds) % Frames;

        public Sprite StartFrame(int frame) => start[frame];
        public Sprite Stream(int variant, int frame) => streams[variant * Frames + frame];
        public Sprite Tip(int variant, int frame) => tips[variant * Frames + frame];

        internal void Configure(Sprite[] newStart, Sprite[] newStreams, Sprite[] newTips)
        {
            start = newStart;
            streams = newStreams;
            tips = newTips;
        }
    }
}
