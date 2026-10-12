using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class EnemyTests
    {
        readonly List<Object> created = new();
        GameObject target;

        [SetUp]
        public void SetUp()
        {
            target = Track(new GameObject("Target"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
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

        EnemyData CreateData(EnemyBehaviour behaviour, int maxHealth = 3, float detectionRange = 9f,
            float preferredDistance = 6f, AttackData attack = null, Enemy splitPrefab = null, int splitCount = 0)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(behaviour, maxHealth, moveSpeed: 4f, detectionRange, contactDamage: 1,
                preferredDistance, attack, splitPrefab, splitCount);
            return data;
        }

        Enemy CreateEnemy(EnemyData data, Vector2 position, bool withWeapon = false, bool withContactDamage = false)
        {
            var go = new GameObject("Enemy");
            go.transform.position = position;

            if (withWeapon)
                go.AddComponent<Weapon>();
            if (withContactDamage)
            {
                go.AddComponent<CircleCollider2D>().radius = 0.45f;
                go.AddComponent<ContactDamage>();
            }

            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>().Target = target.transform;
            enemy.SetData(data);
            return enemy;
        }

        [Test]
        public void SetData_AppliesMaxHealth()
        {
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser, maxHealth: 7), Vector2.zero);

            Assert.AreEqual(7, enemy.Health.Max);
            Assert.AreEqual(7, enemy.Health.Current);
        }

        [UnityTest]
        public IEnumerator Chaser_StaysIdle_WhenPlayerIsFar()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser, detectionRange: 5f), Vector2.zero);

            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(EnemyAI.State.Idle, enemy.GetComponent<EnemyAI>().Current);
            Assert.AreEqual(0f, enemy.transform.position.x, 0.05f);
        }

        [UnityTest]
        public IEnumerator Chaser_ChasesThePlayer_InsideDetectionRange()
        {
            target.transform.position = new Vector3(4f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser), Vector2.zero);

            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(EnemyAI.State.Chase, enemy.GetComponent<EnemyAI>().Current);
            Assert.Greater(enemy.transform.position.x, 0.3f);
        }

        [UnityTest]
        public IEnumerator Damage_AlertsTheEnemy_EvenFromFar()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser, detectionRange: 5f), Vector2.zero);

            enemy.Health.TakeDamage(1);
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(enemy.transform.position.x, 0.3f);
        }

        [UnityTest]
        public IEnumerator Shooter_Retreats_WhenPlayerIsTooClose()
        {
            target.transform.position = new Vector3(2f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Shooter, preferredDistance: 6f), Vector2.zero);

            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(EnemyAI.State.Retreat, enemy.GetComponent<EnemyAI>().Current);
            Assert.Less(enemy.transform.position.x, -0.3f);
        }

        [UnityTest]
        public IEnumerator Shooter_Fires_WhenPlayerIsInRange()
        {
            var projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectileTemplate.AddComponent<Projectile>(), fireRate: 5f, damage: 1, speed: 10f, lifetime: 2f);

            target.transform.position = new Vector3(6f, 0f, 0f);
            CreateEnemy(CreateData(EnemyBehaviour.Shooter, preferredDistance: 6f, attack: attack), Vector2.zero, withWeapon: true);

            yield return new WaitForSeconds(0.2f);

            Assert.Greater(Object.FindObjectsByType<Projectile>().Length, 0);
        }

        [UnityTest]
        public IEnumerator Death_RaisesDiedAndDestroysTheEnemy()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser), Vector2.zero);
            int deaths = 0;
            enemy.Died += _ => deaths++;

            enemy.Health.TakeDamage(enemy.Health.Max);
            yield return null;

            Assert.AreEqual(1, deaths);
            Assert.IsTrue(enemy == null);
        }

        [UnityTest]
        public IEnumerator Death_SplitsIntoChildren_ThatAreAlert()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var splitTemplate = CreateEnemy(CreateData(EnemyBehaviour.Chaser, maxHealth: 1, detectionRange: 0f), new Vector2(100f, 0f));
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Shooter, splitPrefab: splitTemplate, splitCount: 2), Vector2.zero);

            var children = new List<Enemy>();
            enemy.ChildSpawned += children.Add;

            enemy.Health.TakeDamage(enemy.Health.Max);
            yield return null;

            Assert.AreEqual(2, children.Count);
            foreach (var child in children)
            {
                Assert.IsTrue(child.GetComponent<EnemyAI>().IsAlert);
                Assert.AreSame(target.transform, child.GetComponent<EnemyAI>().Target);
                Assert.Less(child.transform.position.magnitude, 1f);
            }
        }

        [UnityTest]
        public IEnumerator Death_WithDeathDuration_RaisesDiedAtOnce_AndStaysForTheAnimation()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser), Vector2.zero);
            enemy.SetAnimationTimings(newDeathDuration: 0.3f, newSplitDuration: 0f);
            int deaths = 0;
            enemy.Died += _ => deaths++;

            enemy.Health.TakeDamage(enemy.Health.Max);
            yield return null;

            Assert.AreEqual(1, deaths, "La sala debe contarlo como muerto en el acto.");
            Assert.IsTrue(enemy != null);
            Assert.IsFalse(enemy.GetComponent<Rigidbody2D>().simulated, "Mientras se ve la muerte no choca con nada.");

            yield return new WaitForSeconds(0.4f);

            Assert.IsTrue(enemy == null);
        }

        [UnityTest]
        public IEnumerator Death_WithSplitDuration_SpawnsChildrenAtTheEnd()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var splitTemplate = CreateEnemy(CreateData(EnemyBehaviour.Chaser, maxHealth: 1, detectionRange: 0f), new Vector2(100f, 0f));
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Shooter, splitPrefab: splitTemplate, splitCount: 2), Vector2.zero);
            enemy.SetAnimationTimings(newDeathDuration: 0f, newSplitDuration: 0.3f);

            var children = new List<Enemy>();
            int deaths = 0;
            enemy.ChildSpawned += children.Add;
            enemy.Died += _ => deaths++;

            enemy.Health.TakeDamage(enemy.Health.Max);
            yield return null;

            Assert.AreEqual(0, children.Count);
            Assert.AreEqual(0, deaths, "Died llega después de las crías, para que la sala no se abra antes.");

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(2, children.Count);
            Assert.AreEqual(1, deaths);
            Assert.IsTrue(enemy == null);
        }

        [UnityTest]
        public IEnumerator Shooter_WithWindup_StopsToAim_ThenFires()
        {
            var attack = CreateAttack();
            target.transform.position = new Vector3(6f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Shooter, preferredDistance: 6f, attack: attack), Vector2.zero, withWeapon: true);
            var ai = enemy.GetComponent<EnemyAI>();
            ai.FireWindup = 0.15f;
            int aims = 0;
            ai.AimStarted += () => aims++;

            yield return null;

            Assert.AreEqual(1, aims);
            Assert.AreEqual(EnemyAI.State.Aim, ai.Current);
            Assert.AreEqual(0, Object.FindObjectsByType<Projectile>().Length);

            yield return new WaitForSeconds(0.25f);

            Assert.Greater(Object.FindObjectsByType<Projectile>().Length, 0);
        }

        [Test]
        public void Animator_ShowsHurt_WhenDamaged()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Chaser), Vector2.zero);
            var animator = enemy.gameObject.AddComponent<EnemyAnimator>();

            enemy.Health.TakeDamage(1);

            Assert.AreEqual(EnemyAnimator.HurtState, animator.CurrentState);
        }

        [Test]
        public void Animator_ShowsDeath_OrSplit_WhenItDies()
        {
            target.transform.position = new Vector3(20f, 0f, 0f);
            var splitTemplate = CreateEnemy(CreateData(EnemyBehaviour.Chaser, maxHealth: 1, detectionRange: 0f), new Vector2(100f, 0f));
            var splitter = CreateEnemy(CreateData(EnemyBehaviour.Shooter, splitPrefab: splitTemplate, splitCount: 2), Vector2.zero);
            var chaser = CreateEnemy(CreateData(EnemyBehaviour.Chaser), new Vector2(0f, 5f));
            var splitterAnimator = splitter.gameObject.AddComponent<EnemyAnimator>();
            var chaserAnimator = chaser.gameObject.AddComponent<EnemyAnimator>();

            splitter.Health.TakeDamage(splitter.Health.Max);
            chaser.Health.TakeDamage(chaser.Health.Max);

            Assert.AreEqual(EnemyAnimator.SplitState, splitterAnimator.CurrentState);
            Assert.AreEqual(EnemyAnimator.DeathState, chaserAnimator.CurrentState);
        }

        [UnityTest]
        public IEnumerator Animator_ShowsShoot_WhenAiming()
        {
            target.transform.position = new Vector3(6f, 0f, 0f);
            var enemy = CreateEnemy(CreateData(EnemyBehaviour.Shooter, preferredDistance: 6f, attack: CreateAttack()), Vector2.zero, withWeapon: true);
            enemy.GetComponent<EnemyAI>().FireWindup = 0.15f;
            var animator = enemy.gameObject.AddComponent<EnemyAnimator>();

            yield return null;

            Assert.AreEqual(EnemyAnimator.ShootState, animator.CurrentState);
        }

        AttackData CreateAttack()
        {
            var projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectileTemplate.AddComponent<Projectile>(), fireRate: 5f, damage: 1, speed: 10f, lifetime: 2f);
            return attack;
        }

        [UnityTest]
        public IEnumerator ContactDamage_HurtsThePlayer()
        {
            target.layer = LayerMask.NameToLayer(CombatLayers.Player);
            target.transform.position = new Vector3(1.5f, 0f, 0f);
            target.AddComponent<Rigidbody2D>().gravityScale = 0f;
            target.AddComponent<CircleCollider2D>().radius = 0.4f;
            var playerHealth = target.AddComponent<Health>();

            CreateEnemy(CreateData(EnemyBehaviour.Chaser), Vector2.zero, withContactDamage: true);

            yield return new WaitForSeconds(0.5f);

            Assert.Less(playerHealth.Current, playerHealth.Max);
        }
    }
}
