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
    /// Chorro de vinagre del vinagrillo: mantener echa un chorro que daña a cada golpe al primer enemigo del camino (el nivel II los
    /// atraviesa, las paredes lo cortan), gasta una barra que se rellena al soltar y se bloquea si se vacía, y la salpicadura del nivel III
    /// daña alrededor del final del chorro. Sin Animator no hay preparación ni bajada de cola, así que se prueba la lógica.
    /// </summary>
    public class JetAttackTests
    {
        readonly List<Object> created = new();
        GameObject player;
        PlayerAttack playerAttack;
        PlayerController controller;
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(300f, 300f);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CircleCollider2D>().radius = 0.3f;
            player.AddComponent<Health>().SetMax(20);
            weapon = player.AddComponent<Weapon>();
            controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            player.AddComponent<PlayerAnimator>();
            playerAttack = player.AddComponent<PlayerAttack>();
            playerAttack.readInput = false;

            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(null, fireRate: 6f, damage: 1, speed: 1f, lifetime: 1f);
            attack.ConfigureJet(5f, 0.15f, 180f, 1f, 1f, 2f, false, null, null, null);
            weapon.Attack = attack;
            // Apunta a la derecha.
            controller.SetInput(Vector2.zero, Vector2.right);
        }

        [TearDown]
        public void TearDown()
        {
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
            // El chorro sale de la punta de la cola, algo por encima del centro (0,56 u).
            enemy.transform.position = (Vector2)player.transform.position + offset + Vector2.up * 0.56f;
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            return health;
        }

        [UnityTest]
        public IEnumerator Jet_HitsOnlyTheFirstEnemy_AndNotOnesBeyondTheRange()
        {
            var first = Enemy(new Vector2(2f, 0f));
            var behind = Enemy(new Vector2(3.5f, 0f));
            var tooFar = Enemy(new Vector2(8f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.5f);

            Assert.Less(first.Current, 100, "El primer enemigo recibe golpes de 1.");
            Assert.AreEqual(100, behind.Current, "Sin atravesar, el segundo queda detrás.");
            Assert.AreEqual(100, tooFar.Current, "Más allá del alcance no llega.");
        }

        [UnityTest]
        public IEnumerator Jet_GrowsFromTheTail_InsteadOfAppearingAtOnce()
        {
            attack.ConfigureJet(8f, 0.15f, 180f, 2f, 1f, 2f, false, null, null, null);
            var far = Enemy(new Vector2(6f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.06f);
            var jet = player.GetComponent<JetAttack>();
            Assert.Greater(jet.Reach, 0f);
            Assert.Less(jet.Reach, 6f, "Al empezar aún no ha llegado lejos.");
            Assert.AreEqual(100, far.Current, "No daña lo que el chorro todavía no ha alcanzado.");

            yield return new WaitForSeconds(0.6f);
            Assert.Less(far.Current, 100, "Cuando llega, sí.");
        }

        [UnityTest]
        public IEnumerator Pierce_HitsEveryEnemyInTheLine()
        {
            attack.ConfigureJet(5f, 0.15f, 180f, 1f, 1f, 2f, true, null, null, null);
            var first = Enemy(new Vector2(2f, 0f));
            var behind = Enemy(new Vector2(3.5f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.4f);

            Assert.Less(first.Current, 100);
            Assert.Less(behind.Current, 100, "El nivel II atraviesa.");
        }

        [UnityTest]
        public IEnumerator Wall_CutsTheJet_EvenWithPierce()
        {
            attack.ConfigureJet(5f, 0.15f, 180f, 1f, 1f, 2f, true, null, null, null);
            var wall = Track(new GameObject("Wall"));
            wall.transform.position = (Vector2)player.transform.position + new Vector2(1.5f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 3f);
            var behind = Enemy(new Vector2(3f, 0f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(100, behind.Current, "La pared corta el chorro.");
        }

        [UnityTest]
        public IEnumerator Bar_DrainsWhileFiring_LocksWhenEmpty_AndRefillsAfterwards()
        {
            yield return null;
            var jet = player.GetComponent<JetAttack>();
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.3f);
            jet = player.GetComponent<JetAttack>();
            Assert.Less(jet.Charge01, 1f, "Se gasta mientras sale.");

            // 1 s de carga: se vacía y queda bloqueado aunque se siga manteniendo el botón.
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(jet.IsLocked, "Vacía, la barra se bloquea.");
            Assert.IsFalse(jet.IsActive);

            playerAttack.attackHeld = false;
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(jet.IsLocked, "Sigue bloqueada hasta llenarse entera (2 s para llenar 1 s de carga).");

            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(jet.IsLocked, "Llena, se puede usar otra vez.");
            Assert.AreEqual(1f, jet.Charge01, 0.01f);
        }

        [UnityTest]
        public IEnumerator Splash_HurtsNearbyEnemiesAtTheEndOfTheJet()
        {
            attack.ConfigureJetSplash(1f, 0.5f, 1);
            var atEnd = Enemy(new Vector2(5f, 0.3f));
            yield return null;

            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.5f);

            Assert.Less(atEnd.Current, 100, "La salpicadura alcanza a lo que hay cerca del final del chorro.");
        }

        [UnityTest]
        public IEnumerator Dodging_CutsTheJet()
        {
            yield return null;
            playerAttack.attackHeld = true;
            yield return new WaitForSeconds(0.2f);
            var jet = player.GetComponent<JetAttack>();
            Assert.IsTrue(jet.IsActive);

            playerAttack.attackHeld = false;
            Assert.IsTrue(controller.TryDodge());
            yield return null;
            yield return null;
            Assert.IsFalse(jet.IsActive);
        }
    }
}
