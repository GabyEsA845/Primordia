using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Euriptérido: escena de la muda (50 %), la muda vacía como cobertura y la fase blanda (cola y doble pinza).</summary>
    public class EurypteridPhasesTests
    {
        readonly List<Object> created = new();
        const float MoltTime = 0.3f;

        [TearDown]
        public void TearDown()
        {
            EurypteridSpine.BreakAll();
            foreach (var trail in Object.FindObjectsByType<EurypteridVenomTrail>(FindObjectsSortMode.None))
                Object.DestroyImmediate(trail.gameObject);
            foreach (var shell in Object.FindObjectsByType<EurypteridExuvia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(shell.gameObject);
            foreach (var effect in Object.FindObjectsByType<FrameEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(effect.gameObject);
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

        EurypteridExuvia ExuviaTemplate()
        {
            // Desactivada y lejos: hace de prefab.
            var shell = Track(new GameObject("ExuviaTemplate"));
            shell.SetActive(false);
            shell.transform.position = new Vector2(500f, 500f);
            shell.AddComponent<SpriteRenderer>();
            var collider = shell.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(4.4f, 1.8f);
            shell.AddComponent<Health>();
            var component = shell.AddComponent<EurypteridExuvia>();
            component.Configure(shell.GetComponent<SpriteRenderer>(), 15);
            return component;
        }

        Sprite[] Frames(int count)
        {
            var texture = Track(new Texture2D(16, 16));
            return Enumerable.Range(0, count)
                .Select(i => Track(Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f))).ToArray();
        }

        EurypteridBoss Boss(Vector2 position, Transform parent = null)
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
            boss.Configure(body, null, null, null);
            boss.ConfigurePhases(ExuviaTemplate(), Frames(5));
            boss.SetMoltTime(MoltTime);
            root.SetActive(true);
            boss.SetTimings(new Vector2(100f, 100f), 0f);
            boss.Face(Vector2.right);
            return boss;
        }

        /// <summary>Lo lleva al 50 % (desde detrás, sin coraza) y espera a que acabe la muda.</summary>
        IEnumerator MakeSoft(EurypteridBoss boss)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            boss.Health.TakeDamage(150);
            yield return new WaitForSeconds(MoltTime + 0.15f);
            Assert.AreEqual(EurypteridBoss.Stage.Soft, boss.CurrentStage);
        }

        [UnityTest]
        public IEnumerator Molt_At50Percent_IsImmune_LeavesTheShell_AndStartsTheCollapse()
        {
            Player(new Vector2(-12f, 0f));
            var room = Track(new GameObject("Room"));
            var cave = room.AddComponent<CaveCollapse>();
            var boss = Boss(Vector2.zero, room.transform);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // Dónde sale el cuerpo nuevo: al acabar la muda (luego echa a andar enseguida).
            Vector2? afterMolt = null;
            boss.ModeChanged += mode =>
            {
                if (mode == EurypteridBoss.Mode.Walk && boss.CurrentStage == EurypteridBoss.Stage.Soft && afterMolt == null)
                    afterMolt = boss.transform.position;
            };

            boss.Health.TakeDamage(400);
            Vector2 before = boss.transform.position;
            Assert.AreEqual(150, boss.Health.Current, "Antes de la muda no baja del 50 %.");
            yield return null;
            Assert.AreEqual(EurypteridBoss.Stage.Molt, boss.CurrentStage);
            Assert.AreEqual(CaveCollapse.Mode.Quake, cave.Current, "La muda trae el terremoto.");
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
            boss.Health.TakeDamage(50);
            Assert.AreEqual(150, boss.Health.Current, "Durante la muda es inmune.");

            yield return new WaitForSeconds(MoltTime + 0.15f);
            Assert.AreEqual(EurypteridBoss.Stage.Soft, boss.CurrentStage);
            Assert.IsNotNull(boss.Exuvia, "Deja la muda vacía.");
            Assert.AreEqual(0f, Vector2.Distance(boss.Exuvia.transform.position, before), 0.01f, "Donde estaba.");
            Assert.IsTrue(afterMolt.HasValue);
            Assert.AreEqual(1f, Vector2.Distance(afterMolt.Value, before), 0.05f, "Y el cuerpo nuevo sale 1 u hacia delante.");
            Assert.AreEqual(CaveCollapse.Mode.Collapse, cave.Current, "La fase blanda trae el derrumbe.");
            cave.SetMode(CaveCollapse.Mode.Calm);
        }

        [UnityTest]
        public IEnumerator Soft_TakesAQuarterMoreDamage_AndNoLongerShootsSpines()
        {
            Player(new Vector2(-12f, 0f));
            var boss = Boss(Vector2.zero);
            yield return MakeSoft(boss);

            boss.Health.TakeDamage(8);
            Assert.AreEqual(140, boss.Health.Current, "Sin coraza, un 25 % más (8 → 10), también de frente.");
            boss.SetTimings(new Vector2(0.1f, 0.1f), 0f);
            yield return new WaitForSeconds(0.5f);
            Assert.AreNotEqual(EurypteridBoss.Mode.SpineWarn, boss.CurrentMode, "Sin caparazón no hay púas.");
        }

        [UnityTest]
        public IEnumerator Exuvia_BreaksAfterFifteenHits_WhateverTheDamage()
        {
            Player(new Vector2(-12f, 0f));
            var boss = Boss(Vector2.zero);
            yield return MakeSoft(boss);
            var shell = boss.Exuvia;
            var shellHealth = shell.GetComponent<Health>();

            shellHealth.TakeDamage(100);
            Assert.AreEqual(14, shell.HitsLeft, "Un golpe fuerte cuenta como uno.");
            for (int i = 0; i < 13; i++)
                shellHealth.TakeDamage(3);
            Assert.IsFalse(shell.Broken);
            shellHealth.TakeDamage(1);
            Assert.IsTrue(shell.Broken, "Al decimoquinto se rompe.");
            Assert.IsFalse(shell.GetComponent<Collider2D>().enabled, "Y deja de cubrir.");
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(shell == null, "Se deshace y desaparece.");
        }

        [UnityTest]
        public IEnumerator Tail_SweepsWhoIsBehind_AndLeavesAVenomTrail()
        {
            var player = Player(new Vector2(-12f, 0f));
            var controller = player.gameObject.AddComponent<PlayerController>();
            controller.readInput = false;
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var boss = Boss(Vector2.zero);
            yield return MakeSoft(boss);

            // Detrás, a 2,5 u del centro.
            boss.Face(Vector2.right);
            boss.ReadyAttacks();
            MovePlayer(player, (Vector2)boss.transform.position + Vector2.left * 2.5f);
            yield return new WaitForSeconds(1.2f);
            Assert.IsNotEmpty(Object.FindObjectsByType<EurypteridVenomTrail>(FindObjectsSortMode.None), "Barre y deja la estela.");
            Assert.LessOrEqual(player.Current, 18, "El barrido quita un corazón.");
            Assert.IsTrue(controller.IsSlowed, "Y la estela envenena.");
        }

        [Test]
        public void Tail_OnlyReachesTheBackAndSides()
        {
            Player(new Vector2(-12f, 0f));
            var boss = Boss(Vector2.zero);
            Assert.IsTrue(boss.InTailSweep(new Vector2(-2.5f, 0f)), "Detrás.");
            Assert.IsTrue(boss.InTailSweep(new Vector2(0f, 2.5f)), "A un lado.");
            Assert.IsFalse(boss.InTailSweep(new Vector2(2.5f, 0.5f)), "Delante no.");
            Assert.IsFalse(boss.InTailSweep(new Vector2(-4f, 0f)), "Demasiado lejos.");
        }

        [UnityTest]
        public IEnumerator Soft_DoublePincer_HitsTwice()
        {
            var player = Player(new Vector2(-12f, 0f));
            var boss = Boss(Vector2.zero);
            yield return MakeSoft(boss);

            boss.Face(Vector2.right);
            boss.ReadyAttacks();
            MovePlayer(player, boss.Head + Vector2.right * 1.4f);
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(16, player.Current, "Dos pinzas, un corazón cada una.");
        }
    }
}
