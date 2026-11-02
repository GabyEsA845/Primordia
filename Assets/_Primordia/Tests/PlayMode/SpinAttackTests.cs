using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Peonza del Diplocaulus: mantener gira y golpea cada 0,25 s a los enemigos del radio (y solo a esos), soltar la detiene con una
    /// breve espera antes de volver a girar, el nivel II devuelve los proyectiles enemigos del radio y el III lanza el bumerán si se giró
    /// el tiempo mínimo. Sin Animator no hay preparación ni frenado, así que se prueba la lógica del giro.
    /// </summary>
    public class SpinAttackTests
    {
        readonly List<Object> created = new();
        GameObject player;
        PlayerAttack playerAttack;
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(200f, 200f);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CircleCollider2D>().radius = 0.3f;
            player.AddComponent<Health>().SetMax(20);
            weapon = player.AddComponent<Weapon>();
            var controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            player.AddComponent<PlayerAnimator>();
            playerAttack = player.AddComponent<PlayerAttack>();
            playerAttack.readInput = false;

            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(null, fireRate: 4f, damage: 2, speed: 1f, lifetime: 1f);
            attack.ConfigureSpin(1.3f, 2, 0.25f, 0f, 0.65f, 3f, 0.5f, 0.5f, false, null, null);
            weapon.Attack = attack;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);
            foreach (var boomerang in Object.FindObjectsByType<SpinBoomerang>(FindObjectsSortMode.None))
                Object.DestroyImmediate(boomerang.gameObject);
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

        Health Enemy(Vector2 offset)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            enemy.transform.position = (Vector2)player.transform.position + offset;
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            return health;
        }

        [UnityTest]
        public IEnumerator HoldingTheButton_HitsEnemiesInRadius_AndNotThoseOutside()
        {
            var near = Enemy(new Vector2(1f, 0f));
            var far = Enemy(new Vector2(3f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.6f);

            // Un golpe nada más empezar y otro cada 0,25 s: al menos tres en 0,6 s.
            Assert.LessOrEqual(near.Current, 94, "Recibe varios golpes de 2.");
            Assert.AreEqual(100, far.Current, "Fuera del radio no le pasa nada.");
            Assert.IsTrue(player.GetComponent<SpinAttack>().IsActive);
            Assert.Less(player.GetComponent<PlayerController>().ChannelSpeedFactor, 1f, "Gira más lento.");
        }

        [UnityTest]
        public IEnumerator Releasing_StopsTheSpin_AndRestoresSpeed()
        {
            var near = Enemy(new Vector2(1f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.3f);
            playerAttack.attackHeld = false;
            yield return null;
            int afterRelease = near.Current;
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(afterRelease, near.Current, "Al soltar deja de golpear.");
            Assert.AreEqual(1f, player.GetComponent<PlayerController>().ChannelSpeedFactor, 0.001f);
            Assert.IsFalse(player.GetComponent<SpinAttack>().IsActive);
        }

        [UnityTest]
        public IEnumerator AfterReleasing_ThereIsAShortWaitBeforeSpinningAgain()
        {
            yield return null;
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.2f);
            playerAttack.attackHeld = false;
            yield return null;
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(player.GetComponent<SpinAttack>().IsActive, "Dentro de la espera no vuelve a girar.");

            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(player.GetComponent<SpinAttack>().IsActive, "Pasada la espera, sí.");
        }

        [UnityTest]
        public IEnumerator Dodging_CancelsTheSpin()
        {
            yield return null;
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.2f);
            var spin = player.GetComponent<SpinAttack>();
            Assert.IsTrue(spin.IsActive);

            playerAttack.attackHeld = false;
            Assert.IsTrue(player.GetComponent<PlayerController>().TryDodge());
            yield return null;
            yield return null;
            Assert.IsFalse(spin.IsActive, "Esquivar corta el giro.");
        }

        [UnityTest]
        public IEnumerator LevelTwo_ReturnsEnemyProjectilesInsideTheRadius()
        {
            attack.ConfigureSpin(1.5f, 3, 0.25f, 0f, 0.65f, 4f, 0.5f, 0.5f, true, null, null);
            var template = Track(new GameObject("ShotTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = template.AddComponent<Projectile>();
            var shotAttack = Track(ScriptableObject.CreateInstance<AttackData>());
            shotAttack.Configure(projectile, fireRate: 1f, damage: 1, speed: 1f, lifetime: 5f);
            yield return null;

            var shot = ProjectilePool.Get(projectile);
            shot.Launch((Vector2)player.transform.position + new Vector2(1f, 0f), Vector2.left, shotAttack,
                CombatLayers.ProjectileLayer(Team.Enemy), null);
            yield return new WaitForFixedUpdate();

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.3f);

            int playerLayer = CombatLayers.ProjectileLayer(Team.Player);
            bool anyReturned = false;
            foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                anyReturned |= p.gameObject.layer == playerLayer && p.Attack == shotAttack;
            Assert.IsTrue(anyReturned, "El proyectil enemigo vuelve como disparo del jugador.");
        }

        [UnityTest]
        public IEnumerator Dizzy_LocksMovementAndDodge_ForItsDuration()
        {
            attack.ConfigureSpin(1.3f, 2, 0.25f, 0f, 0.65f, 0.3f, 0.6f, 0.2f, false, null, null);
            var controller = player.GetComponent<PlayerController>();
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(controller.MovementLocked, "Al agotar el giro se marea y no se puede mover.");
            Assert.IsFalse(controller.CanDodge, "Tampoco esquivar.");

            playerAttack.attackHeld = false;
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(controller.MovementLocked, "Pasado el mareo vuelve a moverse.");
            Assert.IsTrue(controller.CanDodge);
        }

        [UnityTest]
        public IEnumerator WhileTheBoomerangIsOut_CannotSpinAgain()
        {
            var prefab = Track(new GameObject("BoomerangPrefab"));
            prefab.SetActive(false);
            prefab.AddComponent<SpinBoomerang>();
            attack.ConfigureSpin(1.3f, 2, 0.25f, 0f, 0.65f, 3f, 1f, 0.1f, false, null, null);
            attack.ConfigureBoomerang(0.2f, 9f, 8, 0f, prefab);
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.4f);
            playerAttack.attackHeld = false;
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<SpinBoomerang>(FindObjectsSortMode.None).Length);

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(player.GetComponent<SpinAttack>().IsActive, "Con el bumerán fuera no gira.");

            yield return new WaitForSeconds(2f);
            Assert.IsTrue(player.GetComponent<SpinAttack>().IsActive, "Cuando vuelve, puede girar otra vez.");
        }

        [UnityTest]
        public IEnumerator LevelThree_ThrowsTheBoomerang_OnlyIfSpunLongEnough()
        {
            var prefab = Track(new GameObject("BoomerangPrefab"));
            prefab.SetActive(false);
            prefab.AddComponent<SpinBoomerang>();
            attack.ConfigureBoomerang(0.3f, 3f, 8, 0f, prefab);
            var target = Enemy(new Vector2(2f, 0f));
            yield return null;

            // Soltar pronto: no sale.
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.1f);
            playerAttack.attackHeld = false;
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<SpinBoomerang>(FindObjectsSortMode.None).Length, "Giro corto: sin bumerán.");

            yield return new WaitForSeconds(0.7f);
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.5f);
            playerAttack.attackHeld = false;
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<SpinBoomerang>(FindObjectsSortMode.None).Length, "Giro largo: sale el bumerán.");

            yield return new WaitForSeconds(0.5f);
            Assert.Less(target.Current, 100, "El bumerán golpea al enemigo del camino.");
        }
    }
}
