using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Volumen de la música y de los efectos que elige el jugador (en la pausa). Se guarda en PlayerPrefs, así que se
    /// conserva entre partidas (en la web, en el navegador), y se aplica al <see cref="AudioManager"/> al cambiarlo y
    /// al crearse. La interfaz va con los efectos: un solo control para todo lo que no es música.
    /// </summary>
    public static class AudioVolumes
    {
        public const float Default = 0.8f;
        const string MusicKey = "Primordia.Audio.Music";
        const string SfxKey = "Primordia.Audio.Sfx";

        public static float Music
        {
            get => Get(SoundGroup.Music);
            set => Set(SoundGroup.Music, value);
        }

        public static float Effects
        {
            get => Get(SoundGroup.Sfx);
            set => Set(SoundGroup.Sfx, value);
        }

        static string KeyOf(SoundGroup group) => group == SoundGroup.Music ? MusicKey : SfxKey;

        public static float Get(SoundGroup group) => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyOf(group), Default));

        public static void Set(SoundGroup group, float value)
        {
            PlayerPrefs.SetFloat(KeyOf(group), Mathf.Clamp01(value));
            Apply(AudioManager.Instance);
        }

        /// <summary>Guarda en disco lo cambiado (al cerrar la pausa; PlayerPrefs también lo hace al salir).</summary>
        public static void Save() => PlayerPrefs.Save();

        /// <summary>Pone al gestor los volúmenes guardados.</summary>
        public static void Apply(AudioManager manager)
        {
            if (manager == null)
                return;
            // La curva hace que la mitad de la barra suene a "la mitad" (el oído es logarítmico).
            manager.MusicVolume = Perceived(Music);
            manager.SfxVolume = manager.UIVolume = Perceived(Effects);
        }

        /// <summary>De la posición de la barra (0 a 1) a la ganancia.</summary>
        public static float Perceived(float slider) => slider * slider;
    }
}
