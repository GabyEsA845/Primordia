using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Euriptérido, fase acorazada: giro lento, coraza frontal, pinzas con aviso, púas venenosas y piedras del techo.</summary>
    public class EurypteridBossTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            EurypteridSpine.BreakAll();
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

        Health Player(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer(CombatLayers.Player) });
            player.transform.position = position;
            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.position = position;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = player.AddComponent<Health>();
            health.SetMax(20, healDifference: false);
            health.ResetHealth();
            return health;
        }

        EurypteridSpine SpineTemplate()
        {
            // Lejos y desactivada: hace de prefab.
            var spine = Track(new GameObject("SpineTemplate"));
            spine.SetActive(false);
            spine.transform.position = new Vector2(500f, 500f);
            spine.AddComponent<Rigidbody2D>();
            var component = spine.AddComponent<EurypteridSpine>();
            component.Configure(1, 5f, 0.7f, 1.5f);
            return component;
        }

        EurypteridBoss Boss(Vector2 position, Vector2 spineInterval, EurypteridSpine spine = null, Transform parent = null)
        {
            var root = new GameObject("Boss_Eurypterid") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.SetActive(false);
            Track(root);
            if (parent != null)
                root.transform.SetParent(parent);
            root.transform.position = position;
            root.AddComponent<Rigidbody2D>();
            var health = root.AddComponent<Health>();
            health.SetMax(300, healDifference: false);
            health.ResetHealth();
            root.AddComponent<Enemy>();
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            var boss = root.AddComponent<EurypteridBoss>();
            boss.Configure(body, null, null, spine);
            root.SetActive(true);
            boss.SetTimings(spineInterval, 0f);
            return boss;
        }

        [Test]
        public void Armor_HalvesTheDamageFromTheFront_NotFromBehind()
        {
            var player = Player(new Vector2(5f, 0f));
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            boss.Face(Vector2.right);

            Assert.IsTrue(boss.IsArmoredAgainst(player.transform.position));
            boss.Health.TakeDamage(10);
            Assert.AreEqual(295, boss.Health.Current, "De frente, la mitad.");

            player.transform.position = new Vector2(-5f, 0f);
            Assert.IsFalse(boss.IsArmoredAgainst(player.transform.position));
            boss.Health.TakeDamage(10);
            Assert.AreEqual(285, boss.Health.Current, "Por detrás, entero.");
        }

        [UnityTest]
        public IEnumerator Walk_TurnsSlowly_TowardsThePlayer()
        {
            Player(new Vector2(-8f, 0f));
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            boss.Face(Vector2.right);
            yield return new WaitForSeconds(1f);
            float turned = Vector2.Angle(Vector2.right, boss.Heading);
            Assert.That(turned, Is.InRange(60f, 115f), "Gira unos 90° por segundo, no de golpe.");
        }

        [Test]
        public void Pincers_HitAConeInFrontOfTheHead()
        {
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            boss.Face(Vector2.right);
            Assert.IsTrue(boss.InPincers(new Vector2(4.3f, 0f)), "Delante de la cabeza.");
            Assert.IsTrue(boss.InPincers(new Vector2(3.6f, 1.2f)), "Algo de lado, dentro del cono.");
            Assert.IsFalse(boss.InPincers(new Vector2(2.3f, 3.5f)), "Muy de lado.");
            Assert.IsFalse(boss.InPincers(new Vector2(-3f, 0f)), "Detrás.");
            Assert.IsFalse(boss.InPincers(new Vector2(6f, 0f)), "Demasiado lejos.");
        }

        [UnityTest]
        public IEnumerator Pincers_WarnFirst_ThenHitForOneHeart()
        {
            var player = Player(new Vector2(4f, 0f));
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            boss.Face(Vector2.right);

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(EurypteridBoss.Mode.PincerWarn, boss.CurrentMode, "Con el jugador junto a la cabeza, avisa.");
            Assert.AreEqual(20, player.Current, "El aviso no hace daño.");

            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(18, player.Current, "Las pinzas quitan un corazón.");
        }

        [UnityTest]
        public IEnumerator Spines_FanOfThree_HurtAndSlowThePlayer()
        {
            var player = Player(new Vector2(-9f, 0f));
            var controller = player.gameObject.AddComponent<PlayerController>();
            controller.readInput = false;
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var boss = Boss(Vector2.zero, new Vector2(0.1f, 0.1f), SpineTemplate());
            boss.Face(Vector2.left);

            yield return new WaitForSeconds(0.85f);
            Assert.AreEqual(3, EurypteridSpine.All.Count, "Suelta un abanico de 3 púas.");
            // Una sola tanda: otra alargaría el veneno.
            boss.SetTimings(new Vector2(100f, 100f), 0f);

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(19, player.Current, "La púa que le da quita medio corazón.");
            Assert.IsTrue(controller.IsSlowed, "Y lo ralentiza.");
            Assert.AreEqual(0.7f, controller.SlowFactor, 0.001f);
            yield return new WaitForSeconds(1.6f);
            Assert.IsFalse(controller.IsSlowed, "El veneno se pasa.");
        }

        [UnityTest]
        public IEnumerator Spines_StopAgainstARock()
        {
            Player(new Vector2(-9f, 0f));
            var rock = Track(new GameObject("Rock"));
            rock.transform.position = new Vector2(-4f, 0f);
            rock.AddComponent<BoxCollider2D>().size = new Vector2(2f, 6f);
            var boss = Boss(Vector2.zero, new Vector2(0.1f, 0.1f), SpineTemplate());
            boss.Face(Vector2.left);

            yield return new WaitForSeconds(0.85f);
            Assert.AreEqual(EurypteridBoss.Mode.Walk, boss.CurrentMode, "Sin verlo no dispara.");
            Assert.AreEqual(0, EurypteridSpine.All.Count);
        }

        [UnityTest]
        public IEnumerator FallingRock_HurtsThroughTheArmor_AndStuns()
        {
            var player = Player(new Vector2(12f, 0f));
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            boss.Face(Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            boss.HitByFallingRock(8, 1f);
            Assert.AreEqual(292, boss.Health.Current, "La coraza no para las piedras, aunque el jugador esté delante.");
            Assert.AreEqual(EurypteridBoss.Mode.Stunned, boss.CurrentMode);
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(EurypteridBoss.Mode.Walk, boss.CurrentMode, "Tras el aturdido vuelve a andar.");
            Assert.IsNotNull(player);
        }

        [UnityTest]
        public IEnumerator Fight_MakesTheCaveCreak()
        {
            Player(new Vector2(12f, 0f));
            var room = Track(new GameObject("Room"));
            var cave = room.AddComponent<CaveCollapse>();
            Boss(Vector2.zero, new Vector2(100f, 100f), parent: room.transform);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(CaveCollapse.Mode.Creaking, cave.Current, "En la fase acorazada la cueva cruje.");
            cave.SetMode(CaveCollapse.Mode.Calm);
        }
    }
}
