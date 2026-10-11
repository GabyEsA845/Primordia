using NUnit.Framework;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Tests
{
    public class GameManagerTests
    {
        GameManager manager;

        [SetUp]
        public void SetUp()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            // Inmediato: si no, el GameManager de la siguiente prueba vería todavía esta instancia.
            Object.DestroyImmediate(manager.gameObject);
            Time.timeScale = 1f;
        }

        [Test]
        public void IsTheSingleton()
        {
            Assert.AreSame(manager, GameManager.Instance);
            Assert.AreEqual(GameState.Playing, manager.State);
        }

        [Test]
        public void SetState_RaisesEventWithPreviousAndNew()
        {
            GameState? previous = null, current = null;
            manager.StateChanged += (from, to) => { previous = from; current = to; };

            manager.SetState(GameState.Victory);

            Assert.AreEqual(GameState.Playing, previous);
            Assert.AreEqual(GameState.Victory, current);
        }

        [Test]
        public void SetState_SameStateDoesNothing()
        {
            int calls = 0;
            manager.StateChanged += (_, _) => calls++;

            manager.SetState(GameState.Playing);

            Assert.AreEqual(0, calls);
        }

        [TestCase(GameState.Paused, 0f)]
        [TestCase(GameState.MutationChoice, 0f)]
        [TestCase(GameState.Defeat, 1f)]
        public void SetState_ControlsTimeScale(GameState state, float expected)
        {
            manager.SetState(state);

            Assert.AreEqual(expected, Time.timeScale);
        }

        [Test]
        public void TogglePause_SwitchesBetweenPlayingAndPaused()
        {
            manager.TogglePause();
            Assert.AreEqual(GameState.Paused, manager.State);

            manager.TogglePause();
            Assert.AreEqual(GameState.Playing, manager.State);
        }

        [Test]
        public void TogglePause_IgnoredOutsideGameplay()
        {
            manager.SetState(GameState.MutationChoice);

            manager.TogglePause();

            Assert.AreEqual(GameState.MutationChoice, manager.State);
        }
    }
}
