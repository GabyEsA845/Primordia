using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Primordia.Audio
{
    /// <summary>
    /// Reproduce los efectos y la música del juego. Se crea solo al empezar si hay una <see cref="SoundLibrary"/> en
    /// Resources (sin ella el juego sigue, mudo) y sobrevive a los cambios de escena. Los efectos son 2D (la cámara
    /// enseña toda la acción) y usan un grupo fijo de voces; los que se piden muy seguidos se juntan
    /// (<see cref="SoundLibrary.Sound.minInterval"/>). Todo va con tiempo real: suena igual con el juego en pausa.
    /// El código lo usa a través de <see cref="Sfx"/> y <see cref="Music"/>.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class AudioManager : MonoBehaviour
    {
        const int VoiceCount = 24;
        const float LoopFadeTime = 0.25f;

        public static AudioManager Instance { get; private set; }

        [SerializeField] SoundLibrary library;

        public SoundLibrary Library => library;
        public MusicPlayer Music { get; private set; }

        /// <summary>Volumen general de cada grupo (para unas futuras opciones de audio).</summary>
        public float MusicVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;
        public float UIVolume { get; set; } = 1f;

        /// <summary>Último efecto que sonó (para las pruebas).</summary>
        internal string LastSound { get; private set; }

        readonly List<AudioSource> voices = new();
        readonly Dictionary<string, float> lastPlayed = new();
        readonly Dictionary<string, int> lastClip = new();
        readonly Dictionary<string, LoopVoice> loops = new();
        AudioListener ownListener;
        int nextVoice;
        // Sin director (pruebas) el gestor no toca la escena: ni botones ni jugador.
        bool bindScenes;

        class LoopVoice
        {
            public AudioSource Source;
            public float Target;
            public float Level;
            public float Volume;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null)
                return;
            var loaded = Resources.Load<SoundLibrary>(SoundLibrary.ResourcePath);
            if (loaded != null)
                Create(loaded);
        }

        /// <summary>Crea el gestor con esa biblioteca (también lo usan las pruebas).</summary>
        public static AudioManager Create(SoundLibrary soundLibrary, bool withDirector = true)
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            var manager = go.AddComponent<AudioManager>();
            manager.Init(soundLibrary, withDirector);
            return manager;
        }

        void Init(SoundLibrary soundLibrary, bool withDirector)
        {
            library = soundLibrary;
            for (int i = 0; i < VoiceCount; i++)
                voices.Add(NewSource($"Voice {i}"));

            Music = new MusicPlayer(this);
            ownListener = gameObject.AddComponent<AudioListener>();
            ownListener.enabled = false;
            bindScenes = withDirector;
            if (withDirector)
            {
                // Los volúmenes que eligió el jugador en la pausa (las pruebas usan los de fábrica).
                AudioVolumes.Apply(this);
                gameObject.AddComponent<MusicDirector>();
                // La primera escena ya estaba cargada al crear el gestor: no llega su sceneLoaded.
                ButtonSounds.BindAll();
                PlayerAudio.Attach();
            }
            UpdateListener();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateListener();
            if (bindScenes)
            {
                ButtonSounds.BindAll();
                PlayerAudio.Attach();
            }
            // Los bucles de la escena anterior (latido, jadeo de la ameba) no siguen en la nueva.
            foreach (var id in new List<string>(loops.Keys))
            {
                if (id != Sounds.AmbFloor1 && id != Sounds.AmbBossRoom)
                    StopLoop(id, 0f);
            }
        }

        /// <summary>Si la escena no tiene oyente (la cámara suele traerlo), lo pone el gestor.</summary>
        void UpdateListener()
        {
            if (ownListener == null)
                return;
            bool other = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != ownListener && listener.isActiveAndEnabled)
                    other = true;
            }
            ownListener.enabled = !other;
        }

        AudioSource NewSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        internal AudioSource CreateSource(string sourceName) => NewSource(sourceName);

        public float GroupVolume(SoundGroup group) => group switch
        {
            SoundGroup.Music => MusicVolume,
            SoundGroup.UI => UIVolume,
            _ => SfxVolume
        };

        /// <summary>Hace sonar ese efecto. Devuelve false si no existe o si sonó hace muy poco.</summary>
        public bool PlaySound(string id, float volumeScale = 1f, float pitch = 1f)
        {
            var sound = library != null ? library.GetSound(id) : null;
            if (sound == null || sound.clips.Length == 0)
                return false;

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < sound.minInterval)
                return false;
            lastPlayed[id] = now;

            var clip = PickClip(sound);
            if (clip == null)
                return false;

            var voice = NextVoice();
            voice.clip = clip;
            voice.loop = false;
            voice.volume = Mathf.Clamp01(sound.volume * volumeScale * GroupVolume(sound.group));
            voice.pitch = pitch * (1f + Random.Range(-sound.pitchJitter, sound.pitchJitter));
            voice.Play();
            LastSound = id;
            return true;
        }

        /// <summary>Variante al azar, sin repetir la última.</summary>
        AudioClip PickClip(SoundLibrary.Sound sound)
        {
            int count = sound.clips.Length;
            int index = count == 1 ? 0 : Random.Range(0, count);
            if (count > 1 && lastClip.TryGetValue(sound.id, out int previous) && index == previous)
                index = (index + 1 + Random.Range(0, count - 1)) % count;
            lastClip[sound.id] = index;
            return sound.clips[index];
        }

        /// <summary>Una voz libre; si todas suenan, la que empezó hace más tiempo.</summary>
        AudioSource NextVoice()
        {
            for (int i = 0; i < voices.Count; i++)
            {
                var candidate = voices[(nextVoice + i) % voices.Count];
                if (!candidate.isPlaying)
                {
                    nextVoice = (nextVoice + i + 1) % voices.Count;
                    return candidate;
                }
            }
            var stolen = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;
            stolen.Stop();
            return stolen;
        }

        /// <summary>Enciende un bucle (con un fundido corto). Si ya suena, no hace nada.</summary>
        public void StartLoop(string id, float volumeScale = 1f)
        {
            var sound = library != null ? library.GetSound(id) : null;
            if (sound == null || sound.clips.Length == 0)
                return;

            if (!loops.TryGetValue(id, out var loop))
            {
                loop = new LoopVoice { Source = NewSource($"Loop {id}") };
                loop.Source.loop = true;
                loop.Source.clip = sound.clips[0];
                loop.Source.volume = 0f;
                loops[id] = loop;
            }
            loop.Volume = sound.volume * volumeScale;
            loop.Target = 1f;
            if (!loop.Source.isPlaying)
                loop.Source.Play();
        }

        /// <summary>Apaga un bucle con un fundido (0 = en el acto).</summary>
        public void StopLoop(string id, float fade = LoopFadeTime)
        {
            if (!loops.TryGetValue(id, out var loop))
                return;
            loop.Target = 0f;
            if (fade <= 0f)
            {
                loop.Level = 0f;
                loop.Source.Stop();
                loop.Source.volume = 0f;
            }
        }

        public bool IsLoopPlaying(string id) => loops.TryGetValue(id, out var loop) && loop.Target > 0f && loop.Source.isPlaying;

        void Update()
        {
            float step = Time.unscaledDeltaTime / LoopFadeTime;
            foreach (var (id, loop) in loops)
            {
                var sound = library.GetSound(id);
                float group = sound != null ? GroupVolume(sound.group) : 1f;
                loop.Level = Mathf.MoveTowards(loop.Level, loop.Target, step);
                loop.Source.volume = loop.Level * loop.Volume * group;
                if (loop.Level <= 0f && loop.Target <= 0f && loop.Source.isPlaying)
                    loop.Source.Stop();
            }

            Music.Update(Time.unscaledDeltaTime);
        }
    }

    /// <summary>Efectos de sonido para el código del juego. Sin <see cref="AudioManager"/> no hacen nada.</summary>
    public static class Sfx
    {
        public static void Play(string id, float volume = 1f, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(id))
                return;
            var manager = AudioManager.Instance;
            if (manager != null)
                manager.PlaySound(id, volume, pitch);
        }

        public static void StartLoop(string id)
        {
            var manager = AudioManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(id))
                manager.StartLoop(id);
        }

        public static void StopLoop(string id)
        {
            var manager = AudioManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(id))
                manager.StopLoop(id);
        }
    }
}
