using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Audio;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Música y ambientes del piso 2: una música por franja de profundidad y el ambiente del Abismo.</summary>
    public class Floor2AudioTests
    {
        readonly List<Object> created = new();

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        static AudioClip Clip(string name, float seconds = 1f) =>
            AudioClip.Create(name, Mathf.CeilToInt(44100 * seconds), 1, 44100, false);

        static SoundLibrary.Track Music(string id) => new() { id = id, clip = Clip(id, 2f), volume = 1f, loop = true };

        static SoundLibrary.Sound Loop(string id) => new() { id = id, clips = new[] { Clip(id) }, volume = 1f, loop = true };

        (AudioManager audio, MusicDirector director) CreateAudio()
        {
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);
            var library = Track(ScriptableObject.CreateInstance<SoundLibrary>());
            library.Configure(new[]
            {
                Loop(Sounds.AmbFloor1), Loop(Sounds.AmbFloor2), Loop(Sounds.AmbFloor2Abyss), Loop(Sounds.AmbFloor2BossRoom),
            }, new[]
            {
                Music(Sounds.MusicFloor1Explore), Music(Sounds.MusicFloor1Combat),
                Music(Sounds.MusicFloor2ShallowsExplore), Music(Sounds.MusicFloor2ShallowsCombat),
                Music(Sounds.MusicFloor2ReefExplore), Music(Sounds.MusicFloor2ReefCombat),
                Music(Sounds.MusicFloor2AbyssExplore), Music(Sounds.MusicFloor2AbyssCombat),
            });
            var audio = AudioManager.Create(library);
            Track(audio.gameObject);
            // El director aplica los volúmenes guardados en la pausa: las pruebas usan los de fábrica.
            audio.MusicVolume = 1f;
            audio.SfxVolume = 1f;
            return (audio, audio.GetComponent<MusicDirector>());
        }

        Room CreateRoom(int band)
        {
            var go = Track(new GameObject($"Room band {band}"));
            go.AddComponent<BoxCollider2D>();
            var room = go.AddComponent<Room>();
            room.SetDepthBand(band);
            return room;
        }

        [UnityTest]
        public IEnumerator Floor2_EachDepthBand_HasItsOwnMusic()
        {
            var (audio, director) = CreateAudio();
            yield return null;

            director.OnFloorChanged(2);
            Assert.AreEqual(Sounds.MusicFloor2ShallowsExplore, audio.Music.CurrentId, "El piso 2 empieza en las Someras.");
            Assert.IsTrue(audio.IsLoopPlaying(Sounds.AmbFloor2));

            director.OnPlayerEntered(CreateRoom(1));
            Assert.AreEqual(Sounds.MusicFloor2ReefExplore, audio.Music.CurrentId);
            Assert.AreEqual(1, director.CurrentBand);

            director.OnPlayerEntered(CreateRoom(2));
            Assert.AreEqual(Sounds.MusicFloor2AbyssExplore, audio.Music.CurrentId);
            Assert.IsTrue(audio.IsLoopPlaying(Sounds.AmbFloor2Abyss), "El Abismo tiene su propio ambiente.");
            Assert.IsFalse(audio.IsLoopPlaying(Sounds.AmbFloor2));
        }

        [UnityTest]
        public IEnumerator Floor2_ChangingBand_CrossfadesSlowly()
        {
            var (audio, director) = CreateAudio();
            yield return null;
            director.OnFloorChanged(2);
            yield return new WaitForSecondsRealtime(2.2f);

            director.OnPlayerEntered(CreateRoom(1));
            yield return new WaitForSecondsRealtime(MusicDirector.BandFade / 3f);
            float partway = audio.Music.CurrentMainVolume;
            yield return new WaitForSecondsRealtime(MusicDirector.BandFade);
            float full = audio.Music.CurrentMainVolume;

            Assert.Greater(full, 0f);
            Assert.That(partway / full, Is.InRange(0.1f, 0.75f), "A un tercio del fundido, la nueva franja aún está entrando.");
        }

        [UnityTest]
        public IEnumerator Floor2_RoomWithoutBand_KeepsTheMusic()
        {
            var (audio, director) = CreateAudio();
            yield return null;
            director.OnFloorChanged(2);
            director.OnPlayerEntered(CreateRoom(1));

            director.OnPlayerEntered(CreateRoom(-1));
            Assert.AreEqual(Sounds.MusicFloor2ReefExplore, audio.Music.CurrentId, "Una sala sin franja no cambia la música.");
        }
    }
}
