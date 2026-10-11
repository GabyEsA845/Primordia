using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class BossTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);

            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
            ProjectilePool.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        EnemyData CreateData(int maxHealth)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            return data;
        }

        AttackData CreateAttack()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), fireRate: 1f, damage: 1, speed: 5f, lifetime: 5f);
            return attack;
        }

        Enemy CreateMinionTemplate()
        {
            var go = new GameObject("MinionTemplate");
            go.transform.position = new Vector3(100f, 0f, 0f);
            var minion = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            minion.SetData(CreateData(maxHealth: 1));
            return minion;
        }

        BossAI CreateBoss(AttackData spiral = null, AttackData burst = null, Enemy minion = null, float summonInterval = 8f)
        {
            var go = new GameObject("Boss");
            go.AddComponent<Weapon>();
            var enemy = go.AddComponent<Enemy>();
            enemy.SetData(CreateData(maxHealth: 10));
            var boss = go.AddComponent<BossAI>();
            boss.Configure(spiral, burst, minion, delay: 0f, newSummonInterval: summonInterval);
            return boss;
        }

        [Test]
        public void Boss_IsTheCurrentBoss_WhileActive()
        {
            var boss = CreateBoss();

            Assert.AreSame(boss, BossAI.Current);

            Object.DestroyImmediate(boss.gameObject);
            Assert.IsNull(BossAI.Current);
        }

        [UnityTest]
        public IEnumerator Boss_EntersPhase2_BelowHalfHealth()
        {
            var boss = CreateBoss();
            int phaseEvents = 0;
            boss.PhaseChanged += _ => phaseEvents++;

            boss.Health.TakeDamage(4);
            yield return null;
            Assert.AreEqual(1, boss.Phase);

            boss.Health.TakeDamage(1);
            yield return null;
            Assert.AreEqual(2, boss.Phase);
            Assert.AreEqual(1, phaseEvents);
        }

        [UnityTest]
        public IEnumerator Boss_FiresSpiralAndBurst()
        {
            CreateBoss(CreateAttack(), CreateAttack());

            yield return new WaitForSeconds(0.5f);

            // La ráfaga de fase 1 son 12 proyectiles; la espiral añade varios más.
            Assert.Greater(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, 12);
        }

        [UnityTest]
        public IEnumerator Phase2_SummonsMinions_UpToTheLimit_AndReportsThem()
        {
            var boss = CreateBoss(minion: CreateMinionTemplate(), summonInterval: 0.1f);
            var reported = new List<Enemy>();
            boss.GetComponent<Enemy>().ChildSpawned += reported.Add;

            boss.Health.TakeDamage(5);
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(4, boss.Minions.Count, "Nunca más de 4 invocaciones vivas.");
            Assert.AreEqual(4, reported.Count);
            foreach (var minion in reported)
                Assert.IsTrue(minion.GetComponent<EnemyAI>().IsAlert);
        }

        [UnityTest]
        public IEnumerator Minions_DieWithTheBoss()
        {
            var boss = CreateBoss(minion: CreateMinionTemplate(), summonInterval: 0.1f);
            boss.Health.TakeDamage(5);
            yield return new WaitForSeconds(1.5f);

            var minions = new List<Enemy>(boss.Minions);
            Assert.IsNotEmpty(minions);

            boss.Health.TakeDamage(boss.Health.Max);

            foreach (var minion in minions)
                Assert.IsTrue(minion.Health.IsDead);
        }

        [UnityTest]
        public IEnumerator Burst_IsAnnounced_BeforeItFires()
        {
            var burst = CreateAttack();
            var boss = CreateBoss(burst: burst);
            boss.SetAnimationTimings(newBurstWindup: 0.3f, newSummonWindup: 0f, newTransitionDuration: 0f);
            boss.Configure(null, burst, null);
            int announcements = 0;
            boss.BurstStarting += () => announcements++;

            yield return null;

            Assert.AreEqual(1, announcements);
            Assert.AreEqual(0, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);

            yield return new WaitForSeconds(0.4f);

            Assert.GreaterOrEqual(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, 12);
            Assert.AreEqual(1, announcements);
        }

        [UnityTest]
        public IEnumerator Phase2Transition_PausesTheAttacks()
        {
            var spiral = CreateAttack();
            var boss = CreateBoss(spiral: spiral);
            boss.SetAnimationTimings(newBurstWindup: 0f, newSummonWindup: 0f, newTransitionDuration: 0.5f);

            boss.Health.TakeDamage(5);
            yield return null;
            Assert.IsTrue(boss.IsTransitioning);
            int projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(projectiles, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);

            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(boss.IsTransitioning);
            Assert.Greater(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, projectiles);
        }
    }
}
