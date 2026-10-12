using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class UITests
    {
        GameObject root;
        GameManager manager;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            Time.timeScale = 1f;
        }

        [TestCase(6, 0, HeartsDisplay.Fill.Full)]
        [TestCase(6, 2, HeartsDisplay.Fill.Full)]
        [TestCase(5, 2, HeartsDisplay.Fill.Half)]
        [TestCase(4, 2, HeartsDisplay.Fill.Empty)]
        [TestCase(0, 0, HeartsDisplay.Fill.Empty)]
        public void HeartFill_MatchesHalfHeartPoints(int health, int index, HeartsDisplay.Fill expected)
        {
            Assert.AreEqual(expected, HeartsDisplay.HeartFill(health, index));
        }

        [UnityTest]
        public IEnumerator Hearts_FollowThePlayerHealth()
        {
            root = new GameObject("UI");
            var health = root.AddComponent<Health>();
            var full = CreateSprite();
            var half = CreateSprite();
            var empty = CreateSprite();

            var display = new GameObject("Hearts").AddComponent<HeartsDisplay>();
            display.transform.SetParent(root.transform);
            display.Configure(health, full, half, empty);
            yield return null; // Start

            Assert.AreEqual(3, display.Hearts.Count);
            Assert.IsTrue(display.Hearts.All(h => h.sprite == full));

            health.TakeDamage(3);

            Assert.AreSame(full, display.Hearts[0].sprite);
            Assert.AreSame(half, display.Hearts[1].sprite);
            Assert.AreSame(empty, display.Hearts[2].sprite);

            health.SetMax(8);

            Assert.AreEqual(4, display.Hearts.Count(h => h.gameObject.activeSelf));
        }

        [UnityTest]
        public IEnumerator GameUI_ShowsThePanelForEachState()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
            root = new GameObject("UI");
            var pause = CreatePanel("Pause");
            var defeat = CreatePanel("Defeat");
            var victory = CreatePanel("Victory");
            var gameUI = root.AddComponent<GameUI>();
            gameUI.Configure(pause, defeat, victory, delay: 0.2f);
            yield return null; // Start

            Assert.IsFalse(pause.activeSelf || defeat.activeSelf || victory.activeSelf);

            manager.SetState(GameState.Paused);
            Assert.IsTrue(pause.activeSelf);

            manager.SetState(GameState.Playing);
            Assert.IsFalse(pause.activeSelf);

            manager.SetState(GameState.Defeat);
            Assert.IsFalse(defeat.activeSelf, "La derrota espera un momento antes de mostrarse.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(defeat.activeSelf);

            manager.SetState(GameState.Victory);
            Assert.IsTrue(victory.activeSelf);
            Assert.IsFalse(defeat.activeSelf);
        }

        [Test]
        public void Resume_OnlyLeavesPause()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();

            manager.SetState(GameState.Paused);
            manager.Resume();
            Assert.AreEqual(GameState.Playing, manager.State);

            manager.SetState(GameState.Defeat);
            manager.Resume();
            Assert.AreEqual(GameState.Defeat, manager.State);
        }

        [UnityTest]
        public IEnumerator MutationChoice_IgnoresClicks_RightAfterOpening()
        {
            root = new GameObject("UI");
            root.SetActive(false);
            var panel = CreatePanel("ChoicePanel");
            var skip = new GameObject("Skip").AddComponent<Button>();
            skip.transform.SetParent(root.transform);
            var choice = root.AddComponent<MutationChoiceUI>();
            choice.Configure(panel, new MutationCard[0], skip, newInputLockDuration: 0.3f);
            root.SetActive(true);

            var mutation = ScriptableObject.CreateInstance<StatMutation>();
            try
            {
                choice.Show(new[] { mutation }, null);

                Assert.IsTrue(choice.IsLocked);
                Assert.IsFalse(skip.interactable);
                choice.Skip();
                Assert.IsTrue(choice.IsOpen, "Un clic durante el bloqueo no cuenta.");

                yield return new WaitForSecondsRealtime(0.4f);

                Assert.IsFalse(choice.IsLocked);
                Assert.IsTrue(skip.interactable);
                choice.Skip();
                Assert.IsFalse(choice.IsOpen);
            }
            finally
            {
                Object.DestroyImmediate(mutation);
            }
        }

        GameObject CreatePanel(string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(root.transform);
            panel.SetActive(false);
            return panel;
        }

        static Sprite CreateSprite() =>
            Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
    }
}
