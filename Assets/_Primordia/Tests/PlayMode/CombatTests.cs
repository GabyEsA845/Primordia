using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class CombatTests
    {
        GameObject projectileTemplate;
        GameObject shooter;
        GameObject target;
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            // Plantilla inactiva: las copias que crea el pool se activan al dispararse.
            projectileTemplate = new GameObject("ProjectileTemplate");
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = projectileTemplate.AddComponent<Projectile>();

            attack = ScriptableObject.CreateInstance<AttackData>();
            attack.Configure(projectile, fireRate: 2f, damage: 1, speed: 20f, lifetime: 2f);

            shooter = new GameObject("Shooter");
            weapon = shooter.AddComponent<Weapon>();
            weapon.Attack = attack;
        }

        [TearDown]
        public void TearDown()
        {
            // Incluye la plantilla, que también tiene un Projectile.
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);

            ProjectilePool.Clear();
            Object.DestroyImmediate(shooter);
            Object.DestroyImmediate(attack);
            if (target != null)
                Object.DestroyImmediate(target);
        }

        static Projectile[] ActiveProjectiles() =>
            Object.FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        GameObject CreateTarget(bool withHealth)
        {
            target = new GameObject("Target");
            target.transform.position = new Vector3(3f, 0f, 0f);
            target.AddComponent<BoxCollider2D>();
            if (withHealth)
                target.AddComponent<Health>();
            return target;
        }

        [Test]
        public void Fire_SpawnsEveryProjectileOfTheFan()
        {
            attack.Configure(attack.ProjectilePrefab, fireRate: 2f, damage: 1, speed: 20f, lifetime: 2f, count: 3, spread: 30f);

            weapon.Fire(Vector2.right);

            var angles = ActiveProjectiles()
                .Select(p => Vector2.SignedAngle(Vector2.right, p.GetComponent<Rigidbody2D>().linearVelocity))
                .OrderBy(a => a)
                .ToArray();

            Assert.AreEqual(3, angles.Length);
            Assert.AreEqual(-15f, angles[0], 0.1f);
            Assert.AreEqual(0f, angles[1], 0.1f);
            Assert.AreEqual(15f, angles[2], 0.1f);
        }

        [Test]
        public void TryFire_RespectsFireRate()
        {
            Assert.IsTrue(weapon.TryFire(Vector2.right));
            Assert.IsFalse(weapon.TryFire(Vector2.right));
            Assert.AreEqual(1, ActiveProjectiles().Length);
        }

        [UnityTest]
        public IEnumerator Projectile_DamagesTargetAndDisappears()
        {
            var health = CreateTarget(withHealth: true).GetComponent<Health>();

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(health.Max - 1, health.Current);
            Assert.AreEqual(0, ActiveProjectiles().Length);
        }

        [UnityTest]
        public IEnumerator Projectile_PassesThroughInvulnerableTarget()
        {
            var health = CreateTarget(withHealth: true).GetComponent<Health>();
            health.IsInvulnerable = true;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(health.Max, health.Current);
            Assert.AreEqual(1, ActiveProjectiles().Length);
        }

        [UnityTest]
        public IEnumerator Projectile_StopsAtWall()
        {
            CreateTarget(withHealth: false);

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(0, ActiveProjectiles().Length);
        }

        [UnityTest]
        public IEnumerator Projectile_ExpiresAfterLifetime()
        {
            attack.Configure(attack.ProjectilePrefab, fireRate: 2f, damage: 1, speed: 20f, lifetime: 0.2f);

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(0, ActiveProjectiles().Length);
        }

        PlayerAttack AddPlayerAttack(bool automatic)
        {
            attack.Configure(attack.ProjectilePrefab, fireRate: 100f, damage: 1, speed: 20f, lifetime: 2f, automatic: automatic);

            // PlayerAttack añade PlayerController (y su Rigidbody2D y Health) por RequireComponent.
            var playerAttack = shooter.AddComponent<PlayerAttack>();
            playerAttack.readInput = false;
            shooter.GetComponent<PlayerController>().readInput = false;
            return playerAttack;
        }

        [UnityTest]
        public IEnumerator SemiAutomatic_FiresOncePerClick_EvenIfHeld()
        {
            var playerAttack = AddPlayerAttack(automatic: false);

            playerAttack.attackHeld = true;
            playerAttack.attackPressed = true;
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(1, ActiveProjectiles().Length);
        }

        [UnityTest]
        public IEnumerator Automatic_KeepsFiringWhileHeld()
        {
            var playerAttack = AddPlayerAttack(automatic: true);

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.2f);

            Assert.Greater(ActiveProjectiles().Length, 1);
        }

        [UnityTest]
        public IEnumerator Projectile_IsReusedByThePool()
        {
            attack.Configure(attack.ProjectilePrefab, fireRate: 2f, damage: 1, speed: 20f, lifetime: 0.1f);

            weapon.Fire(Vector2.right);
            var first = ActiveProjectiles().Single();
            yield return new WaitForSeconds(0.3f);

            weapon.Fire(Vector2.right);

            Assert.AreSame(first, ActiveProjectiles().Single());
        }

        [UnityTest]
        public IEnumerator Melee_HitsEveryEnemyInTheArc_AndNothingBehind()
        {
            attack.ConfigureMelee(range: 1.5f, arc: 180f, effect: null);
            var front = CreateEnemyDummy(new Vector2(1f, 0.5f));
            var side = CreateEnemyDummy(new Vector2(0.2f, -1.2f));
            var behind = CreateEnemyDummy(new Vector2(-1f, 0f));
            var far = CreateEnemyDummy(new Vector2(4f, 0f));
            int reported = 0;
            weapon.HitTarget += _ => reported++;
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(weapon.TryFire(Vector2.right));

            Assert.AreEqual(front.Max - 1, front.Current);
            Assert.AreEqual(side.Max - 1, side.Current);
            Assert.AreEqual(behind.Max, behind.Current);
            Assert.AreEqual(far.Max, far.Current);
            Assert.AreEqual(2, reported, "Cada golpe avisa al arma, para la curación de Mandíbulas.");
            Assert.IsEmpty(ActiveProjectiles(), "El golpe cuerpo a cuerpo no dispara proyectiles.");
        }

        Health CreateEnemyDummy(Vector2 position)
        {
            var dummy = new GameObject("EnemyDummy") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            dummy.transform.position = position;
            dummy.AddComponent<CircleCollider2D>().radius = 0.3f;
            dummy.transform.SetParent(shooter.transform, true);
            return dummy.AddComponent<Health>();
        }
    }
}
