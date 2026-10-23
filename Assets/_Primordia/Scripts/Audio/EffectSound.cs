using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Hace sonar un efecto al aparecer el objeto. Lo llevan los prefabs de efectos visuales que ya marcan un momento
    /// del juego (curación, pisotón, compra, marcas de estado...), así el sonido sale con su dibujo sin tocar el código
    /// que los crea. Lo pone el menú Primordia > Audio > Importar audio.
    /// </summary>
    public class EffectSound : MonoBehaviour
    {
        [SerializeField] string sound;
        [SerializeField, Range(0f, 1f)] float volume = 1f;

        public string Sound => sound;

        void Start() => Sfx.Play(sound, volume);

        internal void Configure(string newSound, float newVolume = 1f)
        {
            sound = newSound;
            volume = Mathf.Clamp01(newVolume);
        }
    }
}
