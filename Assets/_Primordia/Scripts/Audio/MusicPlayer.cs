using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Música del <see cref="AudioManager"/>. Una pista suena en un "plato" y la siguiente entra en el otro mientras
    /// la anterior se funde. Una pista puede llevar una capa (el combate del piso 1 sobre la exploración): las dos
    /// arrancan a la vez, en sincronía, y <see cref="SetLayer"/> funde de una a otra. Los fragmentos
    /// (<see cref="Stinger"/>) tapan la música mientras suenan; la pausa la deja más baja (<see cref="SetDucked"/>).
    /// </summary>
    public class MusicPlayer
    {
        const float DuckedLevel = 0.35f;
        const float DuckTime = 0.3f;
        const float StingerFadeOut = 0.15f;
        const float StingerFadeIn = 1.2f;
        // Margen para que las dos capas arranquen en la misma muestra.
        const double ScheduleDelay = 0.05;

        class Deck
        {
            public AudioSource Main;
            public AudioSource Layer;
            public string Id;
            public string LayerId;
            public float MainVolume;
            public float LayerVolume;
            public float Level;
            public float Target;
            public float Speed;

            public bool IsActive => Target > 0f || Level > 0f;
        }

        readonly AudioManager manager;
        readonly Deck[] decks = new Deck[2];
        readonly AudioSource stinger;
        int currentDeck;
        float layer;
        float layerTarget;
        float layerSpeed = 1f;
        float duck = 1f;
        float duckTarget = 1f;
        float stingerGain = 1f;
        float stingerUntil = -1f;
        float stingerVolume;
        bool resumeAfterStinger;

        internal MusicPlayer(AudioManager owner)
        {
            manager = owner;
            for (int i = 0; i < decks.Length; i++)
            {
                decks[i] = new Deck
                {
                    Main = owner.CreateSource($"Music {i}"),
                    Layer = owner.CreateSource($"Music {i} layer")
                };
            }
            stinger = owner.CreateSource("Music stinger");
        }

        Deck Current => decks[currentDeck];

        /// <summary>Pista que suena (o entra) ahora; null si no hay ninguna.</summary>
        public string CurrentId => Current.Target > 0f ? Current.Id : null;

        /// <summary>Peso de la capa, de 0 (solo la pista) a 1 (solo la capa).</summary>
        public float LayerWeight => layer;

        public bool IsStingerPlaying => stinger.isPlaying;

        /// <summary>Volumen con que suena ahora la pista principal (para las pruebas).</summary>
        public float CurrentMainVolume => Current.Main.volume;

        public void Play(string id, float fade = 1f) => Play(id, null, fade);

        /// <summary>
        /// Pone esa pista con una capa encima que empieza en silencio. Si ya suena la misma, no hace nada (la
        /// capa sigue con su peso).
        /// </summary>
        public void Play(string id, string layerId, float fade = 1f)
        {
            if (Current.Target > 0f && Current.Id == id && Current.LayerId == layerId && Current.Main.isPlaying)
                return;

            var library = manager.Library;
            var track = library != null ? library.GetTrack(id) : null;
            if (track == null || track.clip == null)
            {
                Stop(fade);
                return;
            }
            var layerTrack = layerId != null && library != null ? library.GetTrack(layerId) : null;

            // La que sonaba se funde; la nueva entra en el otro plato.
            Current.Target = 0f;
            Current.Speed = SpeedFor(fade);
            currentDeck = 1 - currentDeck;
            var deck = Current;
            deck.Main.Stop();
            deck.Layer.Stop();

            deck.Id = id;
            deck.LayerId = layerTrack != null && layerTrack.clip != null ? layerId : null;
            deck.MainVolume = track.volume;
            deck.Main.clip = track.clip;
            deck.Main.loop = track.loop;
            double start = AudioSettings.dspTime + ScheduleDelay;
            deck.Main.PlayScheduled(start);
            if (deck.LayerId != null)
            {
                deck.LayerVolume = layerTrack.volume;
                deck.Layer.clip = layerTrack.clip;
                deck.Layer.loop = layerTrack.loop;
                deck.Layer.PlayScheduled(start);
            }

            layer = layerTarget = 0f;
            deck.Level = fade > 0f ? 0f : 1f;
            deck.Target = 1f;
            deck.Speed = SpeedFor(fade);
            Apply();
        }

        /// <summary>Funde hacia la capa (1) o hacia la pista (0).</summary>
        public void SetLayer(float weight, float fade = 1.5f)
        {
            layerTarget = Mathf.Clamp01(weight);
            layerSpeed = SpeedFor(fade);
            if (fade <= 0f)
                layer = layerTarget;
        }

        public void Stop(float fade = 1f)
        {
            Current.Target = 0f;
            Current.Speed = SpeedFor(fade);
            if (fade <= 0f)
                Current.Level = 0f;
        }

        /// <summary>
        /// Fragmento corto encima de todo. Con <paramref name="resume"/>, la música se calla mientras suena y luego
        /// vuelve (la evolución); sin él, la música se para (victoria, derrota).
        /// </summary>
        public void Stinger(string id, bool resume)
        {
            var library = manager.Library;
            var track = library != null ? library.GetTrack(id) : null;
            if (track == null || track.clip == null)
                return;

            stinger.Stop();
            stinger.clip = track.clip;
            stinger.loop = false;
            stingerVolume = track.volume;
            stinger.volume = stingerVolume * manager.MusicVolume;
            stinger.Play();
            stingerUntil = Time.unscaledTime + track.clip.length;
            resumeAfterStinger = resume;
            if (!resume)
                Stop(StingerFadeOut);
        }

        /// <summary>Música más baja (pausa, mapa, elección de mutación).</summary>
        public void SetDucked(bool ducked) => duckTarget = ducked ? DuckedLevel : 1f;

        static float SpeedFor(float fade) => fade > 0f ? 1f / fade : float.PositiveInfinity;

        internal void Update(float dt)
        {
            foreach (var deck in decks)
            {
                if (!deck.IsActive)
                    continue;
                deck.Level = float.IsPositiveInfinity(deck.Speed) ? deck.Target : Mathf.MoveTowards(deck.Level, deck.Target, deck.Speed * dt);
                if (deck.Level <= 0f && deck.Target <= 0f)
                {
                    deck.Main.Stop();
                    deck.Layer.Stop();
                }
            }

            layer = float.IsPositiveInfinity(layerSpeed) ? layerTarget : Mathf.MoveTowards(layer, layerTarget, layerSpeed * dt);
            duck = Mathf.MoveTowards(duck, duckTarget, dt / DuckTime);

            bool stingerOn = stingerUntil >= 0f && Time.unscaledTime < stingerUntil;
            if (stingerUntil >= 0f && !stingerOn)
                stingerUntil = -1f;
            float stingerTarget = stingerOn && resumeAfterStinger ? 0f : 1f;
            stingerGain = Mathf.MoveTowards(stingerGain, stingerTarget, dt / (stingerTarget < stingerGain ? StingerFadeOut : StingerFadeIn));

            Apply();
        }

        void Apply()
        {
            float common = duck * stingerGain * manager.MusicVolume;
            stinger.volume = stingerVolume * manager.MusicVolume;
            // Fundido de igual potencia entre la pista y su capa: a mitad no baja el volumen.
            float angle = layer * Mathf.PI / 2f;
            foreach (var deck in decks)
            {
                float level = deck.Level * common;
                bool layered = deck.LayerId != null;
                deck.Main.volume = level * deck.MainVolume * (layered ? Mathf.Cos(angle) : 1f);
                deck.Layer.volume = layered ? level * deck.LayerVolume * Mathf.Sin(angle) : 0f;
            }
        }
    }

    /// <summary>Música para el código del juego. Sin <see cref="AudioManager"/> no hace nada.</summary>
    public static class Music
    {
        static MusicPlayer Player => AudioManager.Instance != null ? AudioManager.Instance.Music : null;

        public static void Play(string id, float fade = 1f) => Player?.Play(id, fade);

        public static void Stop(float fade = 1f) => Player?.Stop(fade);

        public static void Stinger(string id, bool resume) => Player?.Stinger(id, resume);
    }
}
