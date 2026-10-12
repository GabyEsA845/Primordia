using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class EffectTests
    {
        const string EffectName = "EffectTemplate";

        readonly List<Object> created = new();
        GameObject effectTemplate;

        [SetUp]
        public void SetUp()
        {
            // Plantilla lejos de todo; sus copias se cuentan por nombre.
            effectTemplate = Track(new GameObject(EffectName));
            effectTemplate.transform.position = new Vector3(500f, 500f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var copy in Copies())
                Object.DestroyImmediate(copy);
            // Incluye las crías de la división, que no están en la lista.
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();

            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        static GameObject[] Copies() =>
            Object.FindObjectsByType<Transform>()
                .Where(t => t.name == EffectName + "(Clone)")
                .Select(t => t.gameObject)
                .ToArray();

        [UnityTest]
        public IEnumerator OneShotEffect_DestroysItself_WhenItsAnimationEnds()
        {
            var effect = Track(new GameObject("OneShot")).AddComponent<OneShotEffect>();
            effect.SetDuration(0.1f);

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(effect == null);
        }

        [UnityTest]
        public IEnumerator Projectile_LeavesAnImpact_OnWalls_ButNotWhenItExpires()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = template.AddComponent<Projectile>();
            projectile.SetImpactEffect(effectTemplate);

            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, fireRate: 2f, damage: 1, speed: 20f, lifetime: 0.3f);
            var weapon = Track(new GameObject("Shooter")).AddComponent<Weapon>();
            weapon.Attack = attack;

            var wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector3(3f, 0f, 0f);
            wall.AddComponent<BoxCollider2D>();

            weapon.Fire(Vector2.right);
            weapon.Fire(Vector2.left);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(1, Copies().Length, "Solo el disparo que chocó con la pared deja impacto.");
        }

        [UnityTest]
        public IEnumerator Poison_ShowsItsEffect_WhileItLasts()
        {
            var target = Track(new GameObject("Target"));
            var health = target.AddComponent<Health>();

            var poison = Poison.Apply(health, damagePerSecond: 1, duration: 1f, effectTemplate);
            Poison.Apply(health, damagePerSecond: 1, duration: 1f, effectTemplate);

            Assert.AreEqual(1, Copies().Length, "Un veneno renovado no duplica el efecto.");
            Assert.AreSame(target.transform, poison.Effect.transform.parent);

            yield return new WaitForSeconds(1.2f);

            Assert.IsEmpty(Copies());
        }

        [UnityTest]
        public IEnumerator DodgeTrail_LeavesSilhouettes_OnlyWhileDodging()
        {
            var player = Track(new GameObject("Player"));
            var controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            player.AddComponent<DodgeTrail>().Configure(effectTemplate, newInterval: 0.05f);

            yield return new WaitForSeconds(0.1f);
            Assert.IsEmpty(Copies());

            controller.SetInput(Vector2.zero, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            yield return new WaitForSeconds(0.4f);

            // La esquiva dura 0,25 s: unas 5 siluetas.
            int trails = Copies().Length;
            Assert.GreaterOrEqual(trails, 3);
            Assert.LessOrEqual(trails, 7);
        }

        [Test]
        public void EnemyDeath_ShowsBubbles_UnlessItSplits()
        {
            var splitTemplate = CreateEnemy(new Vector2(100f, 0f), null);
            var chaser = CreateEnemy(Vector2.zero, null);
            var splitter = CreateEnemy(new Vector2(0f, 5f), splitTemplate);

            chaser.Health.TakeDamage(chaser.Health.Max);
            Assert.AreEqual(1, Copies().Length);

            splitter.Health.TakeDamage(splitter.Health.Max);
            Assert.AreEqual(1, Copies().Length, "La división no lleva burbujas.");
        }

        Enemy CreateEnemy(Vector2 position, Enemy splitPrefab)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 2, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0,
                splitPrefab: splitPrefab, splitCount: splitPrefab != null ? 2 : 0);

            var go = Track(new GameObject("Enemy"));
            go.transform.position = position;
            var enemy = go.AddComponent<Enemy>();
            enemy.SetData(data);
            go.AddComponent<EnemyAnimator>().Configure(null, null, effectTemplate);
            return enemy;
        }
    }
}
