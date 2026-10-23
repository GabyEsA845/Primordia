using System.Collections;
using NUnit.Framework;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    public class AudioTests
    {
        SoundLibrary library;
        AudioManager manager;
        readonly System.Collections.Generic.List<Object> created = new();

        static AudioClip Clip(string name, float seconds = 0.2f) =>
            AudioClip.Create(name, Mathf.CeilToInt(44100 * seconds), 1, 44100, false);

        static SoundLibrary.Sound Sound(string id, float minInterval = 0f, params AudioClip[] clips) =>
            new() { id = id, clips = clips, volume = 1f, minInterval = minInterval };

        [SetUp]
        public void SetUp()
        {
            // Si el juego creó su propio gestor (con la biblioteca de Resources), estas pruebas usan el suyo.
            if (AudioManager.Instance != null)
                Object.DestroyImmediate(AudioManager.Instance.gameObject);

            library = ScriptableObject.CreateInstance<SoundLibrary>();
            library.Configure(new[]
            {
                Sound("Shot", 0f, Clip("Shot_01"), Clip("Shot_02")),
                Sound("Slow", 10f, Clip("Slow")),
                new SoundLibrary.Sound { id = "Loop", clips = new[] { Clip("Loop", 1f) }, volume = 1f, loop = true },
                Sound(Sounds.PlayerHeal, 0f, Clip("Heal")),
                Sound(Sounds.PlayerHurt, 0f, Clip("Hurt")),
            }, new[]
            {
                new SoundLibrary.Track { id = "Explore", clip = Clip("Explore", 2f), volume = 1f },
                new SoundLibrary.Track { id = "Combat", clip = Clip("Combat", 2f), volume = 1f },
                new SoundLibrary.Track { id = "Sting", clip = Clip("Sting", 1f), volume = 1f, loop = false },
            });
            manager = AudioManager.Create(library, withDirector: false);
        }

        [TearDown]
        public void TearDown()
        {
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            Object.DestroyImmediate(library);
        }

        GameObject New(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        [Test]
        public void Library_FindsSoundsAndTracksById()
        {
            Assert.AreEqual(2, library.GetSound("Shot").clips.Length);
            Assert.IsNotNull(library.GetTrack("Explore"));
            Assert.IsNull(library.GetSound("Nada"));
            Assert.IsNull(library.GetSound(null));
        }

        [Test]
        public void PlaySound_UnknownSound_DoesNothing()
        {
            Assert.IsFalse(manager.PlaySound("Nada"));
            Assert.DoesNotThrow(() => Sfx.Play(null));
        }

        [Test]
        public void Sfx_WithoutManager_DoesNothing()
        {
            Object.DestroyImmediate(manager.gameObject);
            manager = null;
            Assert.DoesNotThrow(() => Sfx.Play("Shot"));
            Assert.DoesNotThrow(() => Music.Play("Explore"));
        }

        [Test]
        public void PlaySound_RequestedTooSoon_SoundsOnce()
        {
            Assert.IsTrue(manager.PlaySound("Slow"));
            Assert.IsFalse(manager.PlaySound("Slow"));
        }

        [Test]
        public void PlaySound_AlternatesVariants()
        {
            Assert.IsTrue(manager.PlaySound("Shot"));
            Assert.IsTrue(manager.PlaySound("Shot"));

            var clips = new System.Collections.Generic.HashSet<AudioClip>();
            foreach (var source in manager.GetComponentsInChildren<AudioSource>())
            {
                if (source.isPlaying && source.clip != null && source.clip.name.StartsWith("Shot"))
                    clips.Add(source.clip);
            }
            Assert.AreEqual(2, clips.Count, "Dos disparos seguidos usan variantes distintas.");
        }

        [Test]
        public void Loops_StartAndStop()
        {
            manager.StartLoop("Loop");
            Assert.IsTrue(manager.IsLoopPlaying("Loop"));

            manager.StopLoop("Loop", 0f);
            Assert.IsFalse(manager.IsLoopPlaying("Loop"));
        }

        [UnityTest]
        public IEnumerator Music_LayeredTrack_FadesToTheLayer()
        {
            manager.Music.Play("Explore", "Combat", 0f);
            Assert.AreEqual("Explore", manager.Music.CurrentId);
            Assert.AreEqual(0f, manager.Music.LayerWeight);

            manager.Music.SetLayer(1f, 0f);
            yield return null;

            Assert.AreEqual(1f, manager.Music.LayerWeight);
            Assert.Less(manager.Music.CurrentMainVolume, 0.01f, "Con la capa entera, la pista principal calla.");
        }

        [UnityTest]
        public IEnumerator Music_PlayingTheSameTrack_KeepsIt()
        {
            manager.Music.Play("Explore", "Combat", 0f);
            manager.Music.SetLayer(1f, 0f);
            yield return null;

            manager.Music.Play("Explore", "Combat", 0f);
            yield return null;

            Assert.AreEqual(1f, manager.Music.LayerWeight, "Volver a pedirla no reinicia la capa.");
        }

        [UnityTest]
        public IEnumerator Music_Stinger_SilencesTheMusicWhileItPlays()
        {
            manager.Music.Play("Explore", 0f);
            yield return null;
            float before = manager.Music.CurrentMainVolume;

            manager.Music.Stinger("Sting", resume: true);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.Greater(before, 0.5f);
            Assert.Less(manager.Music.CurrentMainVolume, 0.05f);
            Assert.AreEqual("Explore", manager.Music.CurrentId, "Con resume, la pista sigue debajo.");
        }

        [Test]
        public void Music_StingerWithoutResume_StopsTheMusic()
        {
            manager.Music.Play("Explore", 0f);
            manager.Music.Stinger("Sting", resume: false);
            Assert.IsNull(manager.Music.CurrentId);
        }

        [Test]
        public void Health_Deflected_WhenHitWhileInvulnerable()
        {
            var health = New("Health").AddComponent<Health>();
            int deflected = 0;
            health.Deflected += () => deflected++;

            health.IsInvulnerable = true;
            Assert.IsFalse(health.TakeDamage(1));
            health.IsInvulnerable = false;
            health.TakeDamage(1);

            Assert.AreEqual(1, deflected);
        }

        [Test]
        public void Room_WithoutEnemies_AnnouncesItIsCleared()
        {
            var go = New("Room");
            go.AddComponent<BoxCollider2D>();
            var room = go.AddComponent<Room>();
            Room cleared = null;
            void OnCleared(Room r) => cleared = r;
            Room.ClearedAny += OnCleared;
            try
            {
                room.Begin();
            }
            finally
            {
                Room.ClearedAny -= OnCleared;
            }
            Assert.AreEqual(room, cleared);
        }

        [Test]
        public void ButtonSounds_BindAll_GivesSoundsToSceneButtons()
        {
            var button = New("Button").AddComponent<Button>();

            ButtonSounds.BindAll();

            Assert.IsTrue(button.TryGetComponent<ButtonSounds>(out var sounds));
            Assert.AreEqual(Sounds.UIHover, sounds.Hover);
            Assert.AreEqual(Sounds.UIClick, sounds.Click);
        }

        [Test]
        public void EffectSound_PlaysWhenTheEffectAppears()
        {
            var effect = New("Effect").AddComponent<EffectSound>();
            effect.Configure("Shot");
            effect.SendMessage("Start");
            Assert.AreEqual("Shot", manager.LastSound);
        }

        [Test]
        public void Volumes_AreSavedAndApplied()
        {
            float music = AudioVolumes.Music;
            float effects = AudioVolumes.Effects;
            try
            {
                AudioVolumes.Music = 0.5f;
                AudioVolumes.Effects = 1f;

                Assert.AreEqual(0.5f, AudioVolumes.Music, 1e-4f);
                Assert.AreEqual(0.25f, manager.MusicVolume, 1e-4f, "La barra a la mitad suena a la cuarta parte de ganancia.");
                Assert.AreEqual(1f, manager.SfxVolume, 1e-4f);
                Assert.AreEqual(1f, manager.UIVolume, 1e-4f, "La interfaz va con los efectos.");
            }
            finally
            {
                AudioVolumes.Music = music;
                AudioVolumes.Effects = effects;
            }
        }

        [Test]
        public void VolumeSlider_ChangesTheVolume()
        {
            float effects = AudioVolumes.Effects;
            try
            {
                var go = New("Slider");
                var slider = go.AddComponent<Slider>();
                go.AddComponent<VolumeSlider>().Configure(SoundGroup.Sfx, null);

                slider.value = 0.3f;

                Assert.AreEqual(0.3f, AudioVolumes.Effects, 1e-4f);
            }
            finally
            {
                AudioVolumes.Effects = effects;
            }
        }

        [UnityTest]
        public IEnumerator PlayerAudio_HurtAndHeal()
        {
            var player = New("Player");
            var health = player.AddComponent<Health>();
            player.AddComponent<PlayerAudio>();
            // Al aparecer no suena nada (el jugador se equipa y se cura al cargar la escena).
            yield return new WaitForSecondsRealtime(0.6f);

            health.TakeDamage(2);
            Assert.AreEqual(Sounds.PlayerHurt, manager.LastSound);

            health.Heal(1);
            Assert.AreEqual(Sounds.PlayerHeal, manager.LastSound);
        }
    }
}
