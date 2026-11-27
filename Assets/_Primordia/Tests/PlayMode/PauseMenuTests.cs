#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    /// <summary>Menú de pausa de membrana y bestiario, sobre los prefabs reales.</summary>
    public class PauseMenuTests
    {
        readonly List<Object> created = new();
        readonly Dictionary<string, int> savedKills = new();
        GameManager manager;
        PauseMenu menu;
        GameObject ui;

        [SetUp]
        public void SetUp()
        {
            RunInventory.Reset();
            RunStats.Reset();
            // El bestiario es el de verdad (PlayerPrefs): se guarda y se deja como estaba.
            foreach (var entry in PauseMenuArt.Current.entries)
            {
                savedKills[entry.id] = BestiaryProgress.Kills(entry.id);
                BestiaryProgress.Set(entry.id, 0);
            }
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
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            foreach (var pair in savedKills)
                BestiaryProgress.Set(pair.Key, pair.Value);
            savedKills.Clear();
            RunInventory.Reset();
            RunStats.Reset();
            Time.timeScale = 1f;
            Cursor.visible = true;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        GameObject Prefab(string path) =>
            Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Primordia/Prefabs/{path}.prefab")));

        IEnumerator OpenPause()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
            ui = Prefab("UI/GameUI");
            yield return null;
            manager.SetState(GameState.Paused);
            menu = ui.GetComponentInChildren<PauseMenu>(true);
            yield return null;
        }

        string[] Texts() => menu.Page.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();

        int Count(string name) => menu.Page.GetComponentsInChildren<Transform>().Count(t => t.name == name);

        [UnityTest]
        public IEnumerator Root_OpensSubmenus_AndEscGoesBackOneStep()
        {
            yield return OpenPause();

            Assert.AreEqual(PauseMenu.Submenu.Root, menu.Current);
            Assert.AreEqual(5, menu.Items.Count);
            Assert.IsFalse(menu.Back(), "En la raíz, Esc reanuda.");

            menu.Press(menu.Items[1]);
            Assert.AreEqual(PauseMenu.Submenu.Settings, menu.Current);
            Assert.IsTrue(menu.Back());
            Assert.AreEqual(PauseMenu.Submenu.Root, menu.Current);
            Assert.AreSame(menu.Items[1], menu.Focused, "El foco vuelve a la opción de la que se venía.");

            menu.Open(PauseMenu.Submenu.Exit);
            menu.Press(menu.Items[0]);
            Assert.AreEqual(PauseMenu.Submenu.Confirm, menu.Current);
            Assert.AreSame(menu.Items[1], menu.Focused, "La confirmación empieza en No.");
            menu.Back();
            Assert.AreEqual(PauseMenu.Submenu.Exit, menu.Current);

            manager.SetState(GameState.Playing);
            Assert.IsNull(manager.PauseBack);
        }

        [UnityTest]
        public IEnumerator OrganHud_IsHiddenWhilePaused()
        {
            yield return OpenPause();
            var group = ui.GetComponentInChildren<OrganHud>(true).GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, group.alpha);

            manager.SetState(GameState.Playing);
            yield return null;
            Assert.AreEqual(1f, group.alpha);
        }

        [UnityTest]
        public IEnumerator Settings_SliderAndShakeAnswerTheKeyboard()
        {
            var shake = ScreenShake.Setting;
            float music = Primordia.Audio.AudioVolumes.Get(Primordia.Audio.SoundGroup.Music);
            yield return OpenPause();
            menu.Open(PauseMenu.Submenu.Settings);
            yield return null;

            var slider = menu.Page.GetComponentInChildren<Slider>();
            slider.value = 0.5f;
            menu.Move(Vector2.right);
            Assert.AreEqual(0.6f, slider.value, 0.001f);

            menu.Move(Vector2.down);
            menu.Move(Vector2.down);
            ScreenShake.Setting = ScreenShake.Level.Normal;
            menu.Press(menu.Focused);
            Assert.AreEqual(ScreenShake.Level.Off, ScreenShake.Setting);

            ScreenShake.Setting = shake;
            Primordia.Audio.AudioVolumes.Set(Primordia.Audio.SoundGroup.Music, music);
        }

        [UnityTest]
        public IEnumerator Character_ShowsEachTab_WithExtraHeartsAndItems()
        {
            var player = Prefab("Player/Player");
            yield return OpenPause();

            menu.ShowTab(0);
            yield return null;
            var zone = menu.GetComponentInChildren<CreaturePanel>(true);
            Assert.IsTrue(zone.gameObject.activeSelf);

            var health = player.GetComponent<Health>();
            health.SetMax(health.Max + 4);
            menu.ShowTab(1);
            yield return null;
            Assert.IsFalse(zone.gameObject.activeSelf);
            Assert.IsTrue(ui.transform.Find("ExchangePanel/Creature") == null
                || ui.transform.Find("ExchangePanel/Creature").gameObject.activeSelf, "La sala de cambio enseña la criatura.");
            Assert.AreEqual(2, Count("HeartExtra"));
            CollectionAssert.Contains(Texts(), "+2 EXTRA");

            RunInventory.AddRelic(new RunInventory.Entry { Id = RunInventory.RootAncestral, Name = "Raíz ancestral", Effect = "y" });
            menu.ShowTab(2);
            yield return null;
            Assert.AreEqual(1, Count("Item_" + RunInventory.RootAncestral));
            menu.FocusIndex(menu.Items.Count - 1);
            Assert.IsTrue(menu.InfoShown);

            RunStats.AddDefeat(false);
            RunStats.AddTime(75f);
            menu.ShowTab(3);
            yield return null;
            CollectionAssert.Contains(Texts(), "01:15");
        }

        [UnityTest]
        public IEnumerator Bestiary_HidesTheUnknown_AndTheSecretBoss()
        {
            yield return OpenPause();
            menu.Open(PauseMenu.Submenu.Bestiary);
            yield return null;
            CollectionAssert.Contains(Texts(), "BESTIARIO  0/25");
            CollectionAssert.Contains(Texts(), "???");

            BestiaryProgress.Set("Chaser", 3);
            BestiaryProgress.Set("LepidodendronBoss", 1);
            menu.Open(PauseMenu.Submenu.Bestiary);
            yield return null;
            CollectionAssert.Contains(Texts(), "BESTIARIO  2/26");
            CollectionAssert.Contains(Texts(), "BACTERIA");
            CollectionAssert.Contains(Texts(), "3");
        }

        [Test]
        public void Bestiary_DataIsComplete()
        {
            var art = PauseMenuArt.Current;
            Assert.AreEqual(26, art.entries.Length);
            foreach (var entry in art.entries)
            {
                Assert.NotNull(entry.portrait, entry.id);
                Assert.NotNull(entry.lockedPortrait, entry.id);
                Assert.AreEqual(entry.trap, entry.data == null, entry.id);
                if (entry.tab != BestiaryTab.Bosses)
                    Assert.IsNotEmpty(entry.frames, entry.id);
            }
        }

        [UnityTest]
        public IEnumerator Defeats_AreCountedAndRemembered()
        {
            Track(new GameObject("Tracker").AddComponent<BestiaryTracker>().gameObject);
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.name = "Enemy_Chaser";
            var enemy = Track(new GameObject("Enemy")).AddComponent<Enemy>();
            enemy.SetData(data);
            yield return null;

            enemy.Health.TakeDamage(999);

            Assert.AreEqual(1, BestiaryProgress.Kills("Chaser"));
            Assert.AreEqual(1, RunStats.EnemiesDefeated);
            Assert.AreEqual(0, RunStats.BossesDefeated);
        }

        [TestCase(0f, "00:00")]
        [TestCase(1453.9f, "24:13")]
        [TestCase(3729f, "1:02:09")]
        public void RunTime_IsFormatted(float seconds, string expected) =>
            Assert.AreEqual(expected, RunStats.FormatTime(seconds));
    }
}
#endif
