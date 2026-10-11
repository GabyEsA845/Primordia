using System.Collections;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class PlayerControllerTests
    {
        GameObject gameObject;
        PlayerController player;
        Health health;
        GameManager manager;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Player");
            player = gameObject.AddComponent<PlayerController>();
            player.readInput = false;
            health = gameObject.GetComponent<Health>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            Time.timeScale = 1f;
        }

        [Test]
        public void ChooseDodgeDirection_WhileMoving_UsesMovement()
        {
            var direction = PlayerController.ChooseDodgeDirection(new Vector2(0f, 1f), Vector2.right);

            Assert.AreEqual(Vector2.up, direction);
        }

        [Test]
        public void ChooseDodgeDirection_WhileStill_UsesAim()
        {
            var direction = PlayerController.ChooseDodgeDirection(Vector2.zero, new Vector2(-2f, 0f));

            Assert.AreEqual(Vector2.left, direction);
        }

        [Test]
        public void ChooseDodgeDirection_DiagonalIsNormalized()
        {
            var direction = PlayerController.ChooseDodgeDirection(new Vector2(1f, 1f), Vector2.right);

            Assert.AreEqual(1f, direction.magnitude, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Moves_InTheInputDirection()
        {
            player.SetInput(Vector2.right, Vector2.right);

            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.Greater(gameObject.transform.position.x, 0.1f);
            Assert.AreEqual(0f, gameObject.transform.position.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator Dodge_IsInvulnerable_ThenEnds()
        {
            player.SetInput(Vector2.zero, Vector2.up);

            Assert.IsTrue(player.TryDodge());
            Assert.IsTrue(player.IsDodging);
            Assert.IsTrue(health.IsInvulnerable);
            Assert.AreEqual(Vector2.up, player.DodgeDirection);

            yield return new WaitForSeconds(0.4f);

            Assert.IsFalse(player.IsDodging);
            Assert.IsFalse(health.IsInvulnerable);
            Assert.Greater(gameObject.transform.position.y, 1f);
        }

        [Test]
        public void Dodge_BlockedWhileAlreadyDodging()
        {
            Assert.IsTrue(player.TryDodge());
            Assert.IsFalse(player.TryDodge());
        }

        [Test]
        public void Dodge_BlockedWhenPaused()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
            manager.SetState(GameState.Paused);

            Assert.IsFalse(player.TryDodge());
        }

        [Test]
        public void Death_SetsDefeat()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();

            health.TakeDamage(health.Max);

            Assert.AreEqual(GameState.Defeat, manager.State);
            Assert.IsFalse(player.TryDodge());
        }
    }
}
