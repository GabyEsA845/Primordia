using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>Grupo del mezclador: cada uno tiene su volumen general (para unas futuras opciones de audio).</summary>
    public enum SoundGroup
    {
        Music,
        Sfx,
        UI
    }

    /// <summary>
    /// Todos los efectos y pistas del juego, por nombre. La crea el menú Primordia > Audio > Importar audio en una
    /// carpeta Resources, y el <see cref="AudioManager"/> la carga sola al empezar.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        /// <summary>Ruta dentro de Resources.</summary>
        public const string ResourcePath = "SoundLibrary";

        [Serializable]
        public class Sound
        {
            [Tooltip("Nombre del sonido, sin el prefijo SFX_ ni el número de variante (Player_Shot).")]
            public string id;
            [Tooltip("Variantes: se alternan al azar sin repetir la anterior.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            public SoundGroup group = SoundGroup.Sfx;
            [Tooltip("Segundos mínimos entre dos veces seguidas: los que se piden a la vez (doce proyectiles de una " +
                     "ráfaga, cinco enemigos que se ciegan) suenan una sola.")]
            [Min(0f)] public float minInterval = 0.03f;
            [Tooltip("Variación de tono al azar en cada vez (0,04 = ±4 %).")]
            [Range(0f, 0.3f)] public float pitchJitter;
            [Tooltip("Bucle que se enciende y se apaga (latido con poca vida, ambiente), no un golpe suelto.")]
            public bool loop;
        }

        [Serializable]
        public class Track
        {
            [Tooltip("Nombre de la pista (Music_Floor1_Explore).")]
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
            public bool loop = true;
        }

        [SerializeField] Sound[] sounds = Array.Empty<Sound>();
        [SerializeField] Track[] tracks = Array.Empty<Track>();

        Dictionary<string, Sound> soundIndex;
        Dictionary<string, Track> trackIndex;

        public IReadOnlyList<Sound> Sounds => sounds;
        public IReadOnlyList<Track> Tracks => tracks;

        void OnEnable() => ClearIndex();

        void OnValidate() => ClearIndex();

        void ClearIndex()
        {
            soundIndex = null;
            trackIndex = null;
        }

        /// <summary>El sonido con ese nombre, o null.</summary>
        public Sound GetSound(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            soundIndex ??= Index(sounds, s => s.id);
            return soundIndex.TryGetValue(id, out var sound) ? sound : null;
        }

        /// <summary>La pista con ese nombre, o null.</summary>
        public Track GetTrack(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            trackIndex ??= Index(tracks, t => t.id);
            return trackIndex.TryGetValue(id, out var track) ? track : null;
        }

        static Dictionary<string, T> Index<T>(T[] items, Func<T, string> key)
        {
            var index = new Dictionary<string, T>();
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(key(item)))
                    index[key(item)] = item;
            }
            return index;
        }

        internal void Configure(Sound[] newSounds, Track[] newTracks)
        {
            sounds = newSounds ?? Array.Empty<Sound>();
            tracks = newTracks ?? Array.Empty<Track>();
            ClearIndex();
        }
    }
}
