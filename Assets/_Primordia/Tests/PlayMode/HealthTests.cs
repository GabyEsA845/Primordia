using NUnit.Framework;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Tests
{
    public class HealthTests
    {
        GameObject gameObject;
        Health health;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Health");
            health = gameObject.AddComponent<Health>();
        }

        [TearDown]
        public void TearDown() => Object.Destroy(gameObject);

        [Test]
        public void StartsAtMax()
        {
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndRaisesEvents()
        {
            int damaged = 0;
            int changedTo = -1;
            health.Damaged += amount => damaged = amount;
            health.Changed += (current, _) => changedTo = current;

            Assert.IsTrue(health.TakeDamage(2));

            Assert.AreEqual(health.Max - 2, health.Current);
            Assert.AreEqual(2, damaged);
            Assert.AreEqual(health.Current, changedTo);
        }

        [Test]
        public void TakeDamage_IgnoresZeroOrNegative()
        {
            Assert.IsFalse(health.TakeDamage(0));
            Assert.IsFalse(health.TakeDamage(-3));
            Assert.AreEqual(health.Max, health.Current);
        }

        [Test]
        public void Dies_OnceAtZero_AndIgnoresFurtherDamage()
        {
            int deaths = 0;
            health.Died += () => deaths++;

            health.TakeDamage(health.Max + 10);
            Assert.IsFalse(health.TakeDamage(1));

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void IsInvulnerable_BlocksDamage()
        {
            health.IsInvulnerable = true;

            Assert.IsFalse(health.TakeDamage(1));
            Assert.AreEqual(health.Max, health.Current);
        }

        [Test]
        public void InvulnerabilityAfterHit_BlocksSecondHit()
        {
            health.InvulnerabilityAfterHit = 10f;

            Assert.IsTrue(health.TakeDamage(1));
            Assert.IsFalse(health.TakeDamage(1));
            Assert.AreEqual(health.Max - 1, health.Current);
        }

        [Test]
        public void Heal_ClampsToMax_AndDoesNotReviveTheDead()
        {
            health.TakeDamage(1);
            health.Heal(100);
            Assert.AreEqual(health.Max, health.Current);

            health.TakeDamage(health.Max);
            health.Heal(1);
            Assert.AreEqual(0, health.Current);
        }

        [Test]
        public void SetMax_RaisingHealsTheDifference()
        {
            health.TakeDamage(1);
            int before = health.Current;

            health.SetMax(health.Max + 2);

            Assert.AreEqual(before + 2, health.Current);
        }

        [Test]
        public void SetMax_LoweringClampsCurrent()
        {
            health.SetMax(2);

            Assert.AreEqual(2, health.Max);
            Assert.AreEqual(2, health.Current);
        }

        [Test]
        public void ResetHealth_Revives()
        {
            health.TakeDamage(health.Max);
            health.ResetHealth();

            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(health.Max, health.Current);
        }
    }
}
