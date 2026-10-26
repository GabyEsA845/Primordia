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
    /// <summary>Anomalocaris, fase de acecho: nada junto a las paredes con su estela de burbujas y el zarpazo con aviso.</summary>
    public class AnomalocarisBossTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            AnomalocarisBubble.PopAll();
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

        static void MovePlayer(Health player, Vector2 position)
        {
            player.transform.position = position;
            player.GetComponent<Rigidbody2D>().position = position;
        }

        AnomalocarisBubble BubbleTemplate()
        {
            // Lejos y con mucha vida: hace de prefab.
            var bubble = Track(new GameObject("BubbleTemplate") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) });
            bubble.transform.position = new Vector2(500f, 500f);
            bubble.AddComponent<Rigidbody2D>();
            bubble.AddComponent<CircleCollider2D>().radius = 0.28f;
            bubble.AddComponent<Health>();
            var component = bubble.AddComponent<AnomalocarisBubble>();
            component.Configure(1, 100f, null);
            return component;
        }

        AnomalocarisBoss Boss(Vector2 position, Vector2 strikeInterval, AnomalocarisBubble bubble = null)
        {
            var center = Track(new GameObject("ArenaCenter")).transform;
            var root = new GameObject("Boss_Anomalocaris") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.SetActive(false);
            Track(root);
            root.transform.position = position;
            root.AddComponent<Rigidbody2D>();
            var health = root.AddComponent<Health>();
            health.SetMax(300, healDifference: false);
            health.ResetHealth();
            root.AddComponent<Enemy>();
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            var boss = root.AddComponent<AnomalocarisBoss>();
            boss.Configure(body, null, null, bubble, null);
            boss.SetArena(center, new Vector2(40f, 31f));
            root.SetActive(true);
            boss.SetTimings(strikeInterval, 3.5f, 0f);
            return boss;
        }

        [Test]
        public void Claw_HitsAConeInFrontOfTheHead()
        {
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f));
            Assert.IsTrue(boss.InClaw(new Vector2(4f, 0f)), "Delante de la cabeza.");
            Assert.IsTrue(boss.InClaw(new Vector2(3.4f, 1f)), "Algo de lado, dentro del cono.");
            Assert.IsFalse(boss.InClaw(new Vector2(1.9f, 4f)), "Muy de lado, fuera del cono.");
            Assert.IsFalse(boss.InClaw(new Vector2(-3f, 0f)), "Detrás.");
            Assert.IsFalse(boss.InClaw(new Vector2(6f, 0f)), "Demasiado lejos.");
        }

        [UnityTest]
        public IEnumerator Stalk_SwimsTowardsTheWalls_LeavingABubbleTrail()
        {
            Player(new Vector2(0f, -40f));
            var boss = Boss(Vector2.zero, new Vector2(100f, 100f), BubbleTemplate());

            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Swim, boss.CurrentMode);
            Assert.Greater(Vector2.Distance(boss.transform.position, Vector2.zero), 5f, "Sale hacia el óvalo junto a las paredes.");
            Assert.Greater(AnomalocarisBubble.All.Count, 3, "Deja una estela de burbujas.");
        }

        [UnityTest]
        public IEnumerator Strike_WarnsFirst_ThenDashesAndBitesForAHeart_AndRestsVulnerable()
        {
            var player = Player(new Vector2(4.5f, 0f));
            var boss = Boss(Vector2.zero, Vector2.zero);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Warn, boss.CurrentMode, "Avisa antes de lanzarse.");
            Assert.AreEqual(player.Max, player.Current, "El aviso no hace daño.");

            yield return new WaitForSeconds(0.6f);
            Assert.LessOrEqual(player.Current, player.Max - 2, "Se lanza y el mordisco quita un corazón.");

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Vulnerable, boss.CurrentMode, "Tras el zarpazo se queda quieto.");
            Vector2 resting = boss.transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Vulnerable, boss.CurrentMode);
            Assert.Less(Vector2.Distance(resting, boss.transform.position), 0.01f, "Y sin moverse.");
        }

        [UnityTest]
        public IEnumerator Strike_Misses_IfThePlayerGetsOutOfTheDashLine_AfterItAims()
        {
            var player = Player(new Vector2(4.5f, 0f));
            var boss = Boss(Vector2.zero, Vector2.zero);

            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Warn, boss.CurrentMode);
            // Pasada la puntería, la dirección queda fija: salir de su línea (esquivando) basta.
            MovePlayer(player, new Vector2(0f, 6f));

            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Vulnerable, boss.CurrentMode);
            Assert.AreEqual(player.Max, player.Current, "Esquivado: sin daño.");
        }

        [UnityTest]
        public IEnumerator Bubble_HurtsThePlayerHalfAHeart_AndPopsWhenShot()
        {
            var player = Player(Vector2.zero);
            var template = BubbleTemplate();

            var touching = Object.Instantiate(template, Vector2.zero, Quaternion.identity);
            touching.Launch(Vector2.zero, 3f);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.AreEqual(player.Max - 1, player.Current, "Medio corazón al tocarla.");
            Assert.IsTrue(touching == null, "Y revienta.");

            var shot = Object.Instantiate(template, new Vector2(5f, 0f), Quaternion.identity);
            shot.Launch(Vector2.zero, 3f);
            shot.GetComponent<Health>().TakeDamage(1);
            yield return null;
            Assert.IsTrue(shot == null, "Un disparo la revienta.");
        }
    }
}
