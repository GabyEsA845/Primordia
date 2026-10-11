using System.Collections;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Facing = Primordia.Player.PlayerAnimator.Facing;
using Action = Primordia.Player.PlayerAnimator.Action;

namespace Primordia.Tests
{
    public class PlayerAnimatorTests
    {
        GameObject gameObject;
        PlayerController player;
        PlayerAnimator animator;
        Health health;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Player");
            player = gameObject.AddComponent<PlayerController>();
            player.readInput = false;
            health = gameObject.GetComponent<Health>();
            animator = gameObject.AddComponent<PlayerAnimator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [TestCase(1f, 0f, Facing.Side)]
        [TestCase(-1f, 0.5f, Facing.Side)]
        [TestCase(0.3f, 1f, Facing.Up)]
        [TestCase(-0.3f, -1f, Facing.Down)]
        public void ChooseFacing_UsesTheDominantAxis(float x, float y, Facing expected)
        {
            Assert.AreEqual(expected, PlayerAnimator.ChooseFacing(new Vector2(x, y)));
        }

        [Test]
        public void StateName_MatchesClipNames()
        {
            Assert.AreEqual("Idle_side", PlayerAnimator.StateName(Action.Idle, Facing.Side));
            Assert.AreEqual("Move_down", PlayerAnimator.StateName(Action.Move, Facing.Down));
            Assert.AreEqual("Dodge_up", PlayerAnimator.StateName(Action.Dodge, Facing.Up));
        }

        [UnityTest]
        public IEnumerator Moving_FacesTheAim()
        {
            player.SetInput(Vector2.right, Vector2.up);
            yield return null;

            Assert.AreEqual(Action.Move, animator.CurrentAction);
            Assert.AreEqual(Facing.Up, animator.CurrentFacing);
        }

        [UnityTest]
        public IEnumerator Dodge_FacesTheDodgeDirection()
        {
            player.SetInput(Vector2.left, Vector2.up);
            Assert.IsTrue(player.TryDodge());
            yield return null;

            Assert.AreEqual(Action.Dodge, animator.CurrentAction);
            Assert.AreEqual(Facing.Side, animator.CurrentFacing);
        }

        [UnityTest]
        public IEnumerator Damage_ShowsHurtForAMoment()
        {
            health.TakeDamage(1);
            Assert.IsTrue(animator.IsHurt);

            yield return new WaitForSeconds(0.3f);

            Assert.IsFalse(animator.IsHurt);
        }

        [Test]
        public void Death_HidesEverySpriteButTheBody()
        {
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(gameObject.transform);
            var mouth = new GameObject("Mouth").AddComponent<SpriteRenderer>();
            mouth.transform.SetParent(gameObject.transform);
            animator.Configure(null, body);

            health.TakeDamage(health.Max);

            Assert.IsTrue(body.enabled);
            Assert.IsFalse(mouth.enabled);
        }
    }
}
