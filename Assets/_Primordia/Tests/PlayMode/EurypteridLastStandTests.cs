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
    /// <summary>Último aliento del Euriptérido: no muere al llegar a 0, cae rendido en el centro, se levanta y suelta el bullet hell.</summary>
    public class EurypteridLastStandTests
    {
        readonly List<Object> created = new();
        const float MoltTime = 0.3f;

        [TearDown]
        public void TearDown()
        {
            EurypteridSpine.BreakAll();
            foreach (var shell in Object.FindObjectsByType<EurypteridExuvia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(shell.gameObject);
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
            health.SetMax(200, healDifference: false);
            health.ResetHealth();
            return health;
        }

        EurypteridSpine DropTemplate()
        {
            var drop = Track(new GameObject("DropTemplate"));
            drop.SetActive(false);
            drop.transform.position = new Vector2(500f, 500f);
            drop.AddComponent<Rigidbody2D>();
            var component = drop.AddComponent<EurypteridSpine>();
            component.Configure(1, 3f, 0.7f, 1.5f);
            return component;
        }

        static readonly EurypteridBoss.BarrageSegment[] ShortBarrage =
        {
            new() { duration = 0.6f, ringInterval = 0.25f, ringCount = 12, ringGap = 3, ringGapStep = 30f, spiralInterval = 0.2f,
                    fanCount = 3, speed = 4f },
            new() { duration = 0.6f, spiralInterval = 0.2f, ringCount = 1, fanInterval = 0.25f, fanCount = 3, fanSpread = 30f,
                    speed = 4f },
        };

        EurypteridBoss Boss(Vector2 position, Transform arena)
        {
            var root = new GameObject("Boss_Eurypterid") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
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
            var boss = root.AddComponent<EurypteridBoss>();
            boss.Configure(body, null, null, null);
            boss.SetMoltTime(MoltTime);
            boss.ConfigureLastStand(DropTemplate());
            boss.SetLastStandTimings(0.4f, 0.2f, 0.2f, ShortBarrage);
            boss.SetArena(arena, new Vector2(30f, 22f));
            root.SetActive(true);
            boss.SetTimings(new Vector2(100f, 100f), 0f);
            return boss;
        }

        IEnumerator MakeSoft(EurypteridBoss boss)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            boss.Health.TakeDamage(150);
            yield return new WaitForSeconds(MoltTime + 0.15f);
            Assert.AreEqual(EurypteridBoss.Stage.Soft, boss.CurrentStage);
        }

        [UnityTest]
        public IEnumerator AtZero_DoesNotDie_ButStartsItsLastStand_Immune()
        {
            Player(new Vector2(-12f, 8f));
            var arena = Track(new GameObject("Arena")).transform;
            var boss = Boss(Vector2.zero, arena);
            yield return MakeSoft(boss);

            boss.Health.TakeDamage(1000);
            Assert.AreEqual(1, boss.Health.Current, "En la fase blanda no baja de 1.");
            Assert.IsFalse(boss.Health.IsDead);
            yield return null;
            Assert.AreEqual(EurypteridBoss.Stage.LastStand, boss.CurrentStage);
            Assert.AreEqual(2, boss.PhaseIndex, "La barra pasa a su aspecto del último aliento.");
            boss.Health.TakeDamage(50);
            Assert.AreEqual(1, boss.Health.Current, "Durante el último aliento es inmune.");
        }

        [UnityTest]
        public IEnumerator LastStand_GoesToTheCenter_Refills_ThenDrainsWhileShooting_AndDies()
        {
            Player(new Vector2(-12f, 8f));
            var arena = Track(new GameObject("Arena")).transform;
            arena.position = new Vector2(0.6f, 0f);
            var boss = Boss(Vector2.zero, arena);
            yield return MakeSoft(boss);

            boss.Health.TakeDamage(1000);
            yield return new WaitForSeconds(0.45f);
            Assert.Less(Vector2.Distance(boss.transform.position, arena.position), 0.35f, "Va tambaleándose al centro de la sala.");

            // Tambaleo (0,4) + caer (~0,96) + rendido (0,2) + levantarse (~0,88).
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(EurypteridBoss.Mode.Fury, boss.CurrentMode, "Se levanta y empieza el bullet hell.");
            Assert.Greater(boss.Health.Current, 150, "Con la barra rellenada (estaba en 1; ya ha empezado a bajar).");

            yield return new WaitForSeconds(0.5f);
            Assert.Greater(EurypteridSpine.All.Count, 0, "Suelta gotas venenosas.");
            Assert.Less(boss.Health.Current, 250, "Y la barra baja sola.");
            Assert.IsFalse(boss.Health.IsDead);

            yield return new WaitForSeconds(1f);
            Assert.IsTrue(boss.Health.IsDead, "Cuando la barra llega a 0, muere de verdad.");
        }
    }
}
