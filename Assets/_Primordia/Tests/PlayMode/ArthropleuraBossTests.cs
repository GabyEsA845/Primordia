#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Primordia.Tests
{
    public class ArthropleuraBossTests
    {
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Arthropleura.prefab";
        GameObject room;
        ArthropleuraBoss boss;

        [SetUp]
        public void SetUp()
        {
            room = new GameObject("SalaArthropleura");
            room.transform.position = new Vector2(300f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Ejecuta Primordia > Boss > Crear Arthropleura y escena de prueba.");
            boss = Object.Instantiate(prefab, room.transform).GetComponent<ArthropleuraBoss>();
            boss.SetArena(room.transform, new Vector2(ArthropleuraBoss.ArenaWidth, ArthropleuraBoss.ArenaHeight));
        }

        [TearDown]
        public void TearDown() => Object.Destroy(room);

        void CallBoss(string name, params object[] args) => typeof(ArthropleuraBoss)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boss, args);

        IEnumerator PrepareEntry(int side = -1)
        {
            Object.DestroyImmediate(room);
            room = null;
            boss = BuildInLevel();
            var door = new GameObject("Door", typeof(Door));
            door.transform.SetParent(room.transform);
            door.transform.position = boss.ArenaCenter + Vector2.right * (side * 30f);
            var player = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            player.transform.SetParent(room.transform);
            player.transform.position = boss.ArenaCenter + Vector2.right * (side * 10f);
            yield return null;
        }

        void WakeForTest()
        {
            CallBoss("WakeUp");
            boss.Moving = false;
        }

        void ReachStage(ArthropleuraBoss.EntryStage stage)
        {
            for (int i = 0; i < 2000 && boss.IntroStage != stage; i++) CallBoss("StepIntro", 0.02f);
            CallBoss("Place");
            Assert.AreEqual(stage, boss.IntroStage);
        }

        [UnityTest]
        public IEnumerator Intro_SleepsInvisibleWithoutBarUntilCenter()
        {
            yield return PrepareEntry();
            var initial = boss.NodeGround(0);
            Assert.AreEqual(ArthropleuraBoss.EntryStage.Sleeping, boss.IntroStage);
            Assert.AreNotSame(boss, ActiveBoss.Current);
            Assert.IsTrue(BodyRenderers().All(r => r.sprite == null));
            yield return null;
            Assert.AreEqual(initial, boss.NodeGround(0));
            room.GetComponentInChildren<Primordia.Player.PlayerController>().transform.position = boss.ArenaCenter;
            yield return null;
            Assert.AreEqual(ArthropleuraBoss.EntryStage.Arriving, boss.IntroStage);
            Assert.IsFalse(boss.GetComponent<BossIntro>().enabled);
        }

        [UnityTest]
        public IEnumerator Intro_EntersFromBothSidesOnlyAsRidges()
        {
            foreach (int side in new[] { -1, 1 })
            {
                yield return PrepareEntry(side);
                WakeForTest();
                for (int i = 0; i < boss.PieceCount; i++)
                    Assert.GreaterOrEqual(boss.NodeGround(i).x * -side, 24f);
                float elapsed = 0f;
                while (boss.IntroStage == ArthropleuraBoss.EntryStage.Arriving && elapsed < 20f)
                {
                    CallBoss("StepIntro", 0.02f);
                    CallBoss("Place");
                    elapsed += 0.02f;
                    if (boss.IntroStage == ArthropleuraBoss.EntryStage.Arriving) foreach (var r in BodyRenderers())
                        if (r.sprite != null) StringAssert.Contains("CanopyRidge", r.sprite.name, $"side {side} t {elapsed} node {r.name} at {r.transform.position} stage {boss.IntroStage}");
                }
                Assert.AreEqual(ArthropleuraBoss.EntryStage.Peeking, boss.IntroStage);
                Assert.That(elapsed, Is.InRange(5f, 15f));
            }
        }

        [UnityTest]
        public IEnumerator Intro_PeekKeepsTheBodyVisibleInTheBreach()
        {
            yield return PrepareEntry();
            WakeForTest();
            ReachStage(ArthropleuraBoss.EntryStage.Roaring);
            // Las piezas que quedan en el agujero (sin espesura encima) se dibujan enteras: la cabeza no puede parecer suelta.
            Assert.IsNotNull(BodyRenderers().First().sprite);
            Assert.IsTrue(BodyRenderers().Skip(1).Take(2).All(r => r.sprite != null));
        }

        [UnityTest]
        public IEnumerator Intro_OpensTopBreachWithInvisibleBlockers()
        {
            yield return PrepareEntry();
            WakeForTest();
            ReachStage(ArthropleuraBoss.EntryStage.Peeking);
            var walls = room.transform.Find("Walls").GetComponent<Tilemap>();
            var blocks = room.transform.Find("ThicketBlock").GetComponent<Tilemap>();
            for (int x = -3; x < 3; x++)
            {
                var cell = walls.WorldToCell(boss.ArenaCenter + new Vector2(x + 0.5f, 8.5f));
                Assert.IsFalse(walls.HasTile(cell));
                Assert.IsTrue(blocks.HasTile(cell));
            }
            Assert.IsFalse(boss.GetComponent<BossIntro>().enabled);
        }

        [UnityTest]
        public IEnumerator Intro_RoarStartsBannerAndSkipStartsFirstCharge()
        {
            yield return PrepareEntry();
            var manager = new GameObject("GameManager", typeof(GameManager));
            manager.transform.SetParent(room.transform);
            var intro = boss.GetComponent<BossIntro>();
            Assert.IsNotNull(intro.Data);
            WakeForTest();
            ReachStage(ArthropleuraBoss.EntryStage.Roaring);
            Assert.IsTrue(intro.enabled);
            Assert.AreSame(boss, ActiveBoss.Current);
            Assert.That(boss.GroundToWorld(boss.NodeGround(0)).y - boss.ArenaCenter.y, Is.EqualTo(6.5f).Within(0.11f));
            yield return null;
            yield return null;
            Assert.IsTrue(intro.Playing);
            Assert.AreEqual(GameState.Cinematic, manager.GetComponent<GameManager>().State);
            yield return new WaitForSeconds(0.3f);
            intro.skipPressed = true;
            float until = Time.time + 3f;
            while (intro.Playing && Time.time < until) yield return null;
            Assert.IsTrue(intro.Skipped);
            Assert.AreEqual(ArthropleuraBoss.Mode.Warn, boss.CurrentMode);
            Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(r => r.name == "Mark" && r.sprite != null));
            boss.Moving = true;
            until = Time.time + 8f;
            bool charged = false;
            while (Time.time < until)
            {
                yield return null;
                charged |= boss.CurrentMode == ArthropleuraBoss.Mode.Charge;
                if (charged && boss.CurrentMode == ArthropleuraBoss.Mode.Circling) break;
            }
            Assert.IsTrue(charged);
            Assert.Less(boss.GroundToWorld(boss.NodeGround(0)).y - boss.ArenaCenter.y, -8f);
        }

        [UnityTest]
        public IEnumerator Intro_CannotIgniteHeadOrSacsBeforeFight()
        {
            yield return PrepareEntry();
            Assert.IsFalse(boss.IgnitePiece(0));
            WakeForTest();
            ReachStage(ArthropleuraBoss.EntryStage.Roaring);
            Assert.IsFalse(boss.IgnitePiece(0));
            Flammables.IgniteNear(boss.GroundToWorld(boss.NodeGround(0)), 100f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Normal, boss.PieceFire(0));
            Assert.IsFalse(boss.SacAt(0).enabled);
            Assert.IsFalse(boss.SacAt(0).GetComponent<Health>().TakeDamage(10));
            boss.BeginFight();
            Assert.IsTrue(boss.IgnitePiece(0));
            Assert.IsTrue(boss.SacAt(0).enabled);
        }

        [UnityTest]
        public IEnumerator Intro_WithoutDoorsStartsCirclingWithoutPresentation()
        {
            yield return null;
            Assert.AreEqual(ArthropleuraBoss.EntryStage.Complete, boss.IntroStage);
            Assert.AreEqual(ArthropleuraBoss.Mode.Circling, boss.CurrentMode);
            Assert.IsFalse(boss.GetComponent<BossIntro>().enabled);
            Assert.AreSame(boss, ActiveBoss.Current);
        }

        [UnityTest]
        public IEnumerator Chain_HasThirtyPiecesWithSprites()
        {
            yield return null;
            Assert.AreEqual(ArthropleuraBoss.SegmentCount + 2, boss.PieceCount);
            var renderers = BodyRenderers();
            Assert.AreEqual(boss.PieceCount, renderers.Length);
            Assert.IsTrue(renderers.All(r => r.sprite != null), "Alguna pieza no tiene sprite.");
            Assert.AreEqual(600, boss.Health.Max);
        }

        [UnityTest]
        public IEnumerator Chain_DrawsTailBehindHead()
        {
            yield return null;
            var renderers = BodyRenderers();
            Assert.Greater(renderers[0].sortingOrder, renderers[renderers.Length - 1].sortingOrder);
        }

        [UnityTest]
        public IEnumerator Chain_KeepsItsPitchWhileMoving()
        {
            for (int i = 0; i < 60; i++)
                yield return null;
            for (int i = 0; i < boss.PieceCount - 1; i++)
            {
                float gap = (boss.NodeGround(i) - boss.NodeGround(i + 1)).magnitude;
                Assert.That(gap, Is.InRange(1.2f, ArthropleuraBoss.Pitch + 0.25f), $"Nudo {i}");
            }
        }

        // Un piso mínimo: rejilla con Floor y Walls (con un tile de pared en una esquina) y el jefe en su sala.
        ArthropleuraBoss BuildInLevel()
        {
            Object.Destroy(room);
            var level = new GameObject("Level", typeof(Grid));
            var floor = new GameObject("Ground", typeof(Tilemap)); floor.transform.SetParent(level.transform, false);
            var walls = new GameObject("Walls", typeof(Tilemap), typeof(TilemapCollider2D)); walls.transform.SetParent(level.transform, false);
            var tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -32; x < 32; x++)
                for (int y = -19; y < 19; y++)
                    floor.GetComponent<Tilemap>().SetTile(new Vector3Int(300 + x, 100 + y, 0), tile);
            walls.GetComponent<Tilemap>().SetTile(new Vector3Int(300, 100 + 18, 0), tile);
            room = new GameObject("SalaArthropleura");
            room.transform.SetParent(level.transform, false);
            room.transform.position = new Vector2(300f, 100f);
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), room.transform).GetComponent<ArthropleuraBoss>();
            instance.SetArena(room.transform, new Vector2(ArthropleuraBoss.ArenaWidth, ArthropleuraBoss.ArenaHeight));
            room = level;
            return instance;
        }

        [UnityTest]
        public IEnumerator Thicket_FillsEverythingButTheClearing()
        {
            var b = BuildInLevel();
            yield return null;
            Assert.Greater(b.ThicketCells, 1000, "La espesura debería cubrir el anillo de la sala.");
            Assert.Less(b.ThicketCells, 60 * 34 - 28 * 16 + 1);
        }

        [UnityTest]
        public IEnumerator Thicket_LeavesAPathFromTheDoor()
        {
            var doorObject = new GameObject("Door", typeof(Primordia.Rooms.Door));
            doorObject.transform.position = new Vector2(300f - 30f, 100f);
            var b = BuildInLevel();
            yield return null;
            var walls = b.transform.parent.parent.Find("Walls").GetComponent<Tilemap>();
            for (float x = -29.5f; x < -14f; x += 1f)
                Assert.IsFalse(walls.HasTile(walls.WorldToCell(new Vector3(300f + x, 100f + 0.5f))), $"Pared tapando la entrada en x={x}");
            // Con el jugador dentro del claro, el pasillo se cierra.
            var playerObject = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            playerObject.transform.position = new Vector2(300f, 100f);
            yield return null;
            yield return null;
            Assert.IsTrue(walls.HasTile(walls.WorldToCell(new Vector3(300f - 20f, 100f + 0.5f))), "El pasillo de la puerta debería cerrarse con el jugador dentro.");
            Object.Destroy(playerObject);
            Object.Destroy(doorObject);
        }

        [UnityTest]
        public IEnumerator Charge_WarnsHiddenThenCrossesTheWholeClearing()
        {
            var b = BuildInLevel();
            var playerObject = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            playerObject.transform.position = new Vector2(300f - 8f, 100f + 2f);
            yield return null;
            int before = b.ThicketCells;
            b.TestStartCharge();
            Assert.AreEqual(ArthropleuraBoss.Mode.Warn, b.CurrentMode);
            Vector2 warnHead = b.NodeGround(0);
            Assert.IsFalse(Mathf.Abs(warnHead.x) < 14f && Mathf.Abs(warnHead.y * ArthropleuraBoss.GroundSquash) < 8f, "Avisa dentro del claro: debería estar bajo la espesura.");
            bool charged = false, crossed = false, back = false;
            float until = Time.time + 25f;
            while (Time.time < until && !back)
            {
                yield return null;
                charged |= b.CurrentMode == ArthropleuraBoss.Mode.Charge;
                Vector2 g = b.NodeGround(0);
                bool inside = Mathf.Abs(g.x) < 14f && Mathf.Abs(g.y * ArthropleuraBoss.GroundSquash) < 8f;
                if (charged && inside) crossed = true;
                back = charged && crossed && b.CurrentMode == ArthropleuraBoss.Mode.Circling;
            }
            Assert.IsTrue(charged, "No llegó a embestir.");
            Assert.IsTrue(crossed, "No entró en el claro.");
            Assert.IsTrue(back, "No terminó la embestida.");
            Assert.Less(b.ThicketCells, before - 40, "La embestida debe abrir dos agujeros (entrada y salida).");
            Assert.AreEqual(before - b.ThicketCells, b.BlockedCells, "Cada celda abierta debe quedar con pared invisible.");
            var block = b.transform.parent.parent.Find("ThicketBlock");
            Assert.IsNotNull(block, "Falta el mapa de la pared invisible.");
            Assert.IsNull(block.GetComponent<TilemapRenderer>(), "La pared invisible no debe dibujarse.");
            var blockMap = block.GetComponent<Tilemap>();
            int blocked = 0;
            foreach (var cell in blockMap.cellBounds.allPositionsWithin)
                if (blockMap.HasTile(cell))
                    blocked++;
            Assert.AreEqual(b.BlockedCells, blocked);
            // Al acabar queda al otro lado: fuera del claro y lejos de donde empezó.
            Vector2 end = b.NodeGround(0);
            Assert.Greater((end - warnHead).magnitude, 15f, "La embestida no cruzó la sala.");
            Object.Destroy(playerObject);
        }

        [Test]
        public void Phases_FollowTheThresholds()
        {
            Assert.AreEqual(0, boss.PhaseIndex);
            Assert.IsTrue(boss.Health.IsInvulnerable, "El caparazón es invulnerable.");
            CollectionAssert.AreEqual(new[] { 0.7f, 0.35f, 0.1f }, boss.PhaseThresholds);
        }

        SpriteRenderer[] BodyRenderers() => boss.GetComponentsInChildren<SpriteRenderer>()
            .Where(r => r.transform.parent == boss.transform &&
                (r.name == "Head" || r.name == "Tail" || r.name.StartsWith("Segment_"))).ToArray();

        [Test]
        public void Art_IgnitionHasFourFramesAndDecorHasNoMissingSprites()
        {
            var serialized = new SerializedObject(boss);
            var ignition = serialized.FindProperty("clearingArt.ignite");
            Assert.AreEqual(4, ignition.arraySize);
            for (int i = 0; i < ignition.arraySize; i++)
            {
                var sprite = ignition.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                Assert.IsNotNull(sprite);
                Assert.AreEqual(new Vector2(16, 16), sprite.rect.size);
            }
            var decor = serialized.FindProperty("clearingArt.decor");
            Assert.AreEqual(8, decor.arraySize);
            for (int i = 0; i < decor.arraySize; i++)
                Assert.IsNotNull(decor.GetArrayElementAtIndex(i).objectReferenceValue);
            Assert.AreEqual(4, boss.GetComponentsInChildren<MethaneSac>().Length);
            Assert.AreEqual(0, boss.GetComponentsInChildren<Fern>().Length, "El decorado de F1 no debe poder arder.");
        }

        void PutPieceInClearing(int index)
        {
            boss.Moving = false;
            var field = typeof(ArthropleuraBoss).GetField("trail", BindingFlags.Instance | BindingFlags.NonPublic);
            var trail = (List<Vector2>)field.GetValue(boss);
            Vector2 delta = -boss.NodeGround(index);
            for (int i = 0; i < trail.Count; i++) trail[i] += delta;
        }

        void Simulate(float seconds)
        {
            // Misma ruta que Update, con pasos pequeños y reloj propio de la limitadora.
            int steps = Mathf.CeilToInt(seconds / 0.02f);
            for (int i = 0; i < steps; i++) boss.TickFire(seconds / steps);
        }

        [Test]
        public void Fire_OutsideCannotIgniteEvenThroughRegistry()
        {
            boss.Moving = false;
            Assert.IsFalse(boss.IgnitePiece(0));
            Vector2 position = boss.GroundToWorld(boss.NodeGround(0));
            Flammables.IgniteNear(position, 0.01f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Normal, boss.PieceFire(0));
        }

        [Test]
        public void Fire_InsideIgnitesAndSpreadsBothWaysAfterDelay()
        {
            PutPieceInClearing(14);
            Flammables.IgniteNear(boss.ArenaCenter, 0.01f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(14));
            Simulate(0.38f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Normal, boss.PieceFire(13));
            Assert.AreEqual(ArthropleuraBoss.FireState.Normal, boss.PieceFire(15));
            Simulate(0.04f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(13));
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(15));
            Simulate(6f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(0), "La mecha debe cruzar la espesura.");
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(29));
        }

        [Test]
        public void Fire_OpensForSixSecondsThenClosesAndCools()
        {
            PutPieceInClearing(0);
            Assert.IsTrue(boss.IgnitePiece(0));
            Assert.IsFalse(boss.PieceOpen(0));
            Simulate(0.38f);
            Assert.IsTrue(boss.PieceOpen(0));
            Simulate(5.9f);
            Assert.IsTrue(boss.PieceOpen(0));
            Simulate(0.5f);
            Assert.AreEqual(ArthropleuraBoss.FireState.Charred, boss.PieceFire(0));
            Assert.IsFalse(boss.PieceOpen(0));
            Assert.IsFalse(boss.IgnitePiece(0));
            Simulate(24.8f);
            Assert.IsFalse(boss.IgnitePiece(0));
            Simulate(0.3f);
            Assert.IsTrue(boss.IgnitePiece(0));
        }

        [Test]
        public void Damage_OnlyOpenBurningPiecesAndSixPerRollingSecond()
        {
            PutPieceInClearing(0);
            int before = boss.Health.Current;
            Simulate(1f);
            Assert.AreEqual(before, boss.Health.Current);
            boss.IgnitePiece(0);
            Simulate(0.34f);
            Assert.AreEqual(before, boss.Health.Current, "Abrir las placas aún no hace daño.");
            var samples = new Queue<int>();
            for (int i = 0; i < 1100; i++)
            {
                boss.TickFire(0.02f);
                samples.Enqueue(boss.Health.Current);
                if (samples.Count > 50)
                    Assert.LessOrEqual(samples.Dequeue() - boss.Health.Current, 6, "Tope global en una ventana de un segundo.");
            }
            Assert.Less(boss.Health.Current, before);
            int charredHealth = boss.Health.Current;
            Simulate(2f);
            Assert.AreEqual(charredHealth, boss.Health.Current, "Las piezas carbonizadas no hacen daño.");
        }

        [Test]
        public void Damage_DirectHitsAndExplosionsDoNotHurtShell()
        {
            PutPieceInClearing(0);
            int before = boss.Health.Current;
            Assert.IsFalse(boss.Health.TakeDamage(100));
            Explosion.Detonate(boss.ArenaCenter, ArthropleuraBoss.SacIgnitionRadius, 100, 0, false);
            Assert.AreEqual(before, boss.Health.Current);
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(0));
            Assert.IsFalse(boss.Health.TakeDamage(100, DamageKind.Tick, DamageSource.Burn));
        }

        [Test]
        public void Sacs_RegrowTwentySecondsAfterBurst()
        {
            var old = boss.SacAt(0);
            old.Ignite();
            Simulate(19.9f);
            Assert.AreSame(old, boss.SacAt(0));
            Simulate(0.12f);
            Assert.AreNotSame(old, boss.SacAt(0));
            Assert.IsTrue(boss.SacAt(0).CanIgnite);
        }

        [Test]
        public void Phase2_EventOnlyOnceAndStopsAtThreshold()
        {
            PutPieceInClearing(0);
            int events = 0;
            boss.ReachedPhase2 += () => events++;
            boss.DebugFireAction(1, boss.ArenaCenter);
            Assert.AreEqual(426, boss.Health.Current);
            boss.IgnitePiece(0);
            Simulate(4f);
            Assert.AreEqual(420, boss.Health.Current);
            Assert.AreEqual(1, events);
            Assert.AreEqual(ArthropleuraBoss.Mode.Phase2Pending, boss.CurrentMode);
            Simulate(20f);
            Assert.AreEqual(1, events);
            Assert.AreEqual(420, boss.Health.Current);
        }

        [Test]
        public void Fire_TwoFullBurnsReachPhase2WithoutDebugDamage()
        {
            PutPieceInClearing(0);
            boss.IgnitePiece(0);
            Simulate(20f);
            int firstBurn = 600 - boss.Health.Current;
            Assert.That(firstBurn, Is.InRange(90, 110));
            Simulate(25f);
            Assert.IsTrue(boss.IgnitePiece(0));
            Simulate(20f);
            Assert.AreEqual(420, boss.Health.Current);
            Assert.IsTrue(boss.Phase2Reached);
        }

        [UnityTest]
        public IEnumerator Phase2_FinishesCurrentChargeAndClearsBlockers()
        {
            boss = BuildInLevel();
            yield return null;
            boss.TestStartCharge();
            float until = Time.time + 5f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Charge && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Charge, boss.CurrentMode);
            boss.Health.Drain(180);
            Assert.AreEqual(ArthropleuraBoss.Mode.Charge, boss.CurrentMode);
            until = Time.time + 10f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Phase2Pending && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Phase2Pending, boss.CurrentMode);
            Assert.AreEqual(0, boss.BlockedCells);
        }

        // F2 (C1): transición del 70 %, persecución, pisotón con ondas y embestida larga.
        IEnumerator ReachChase()
        {
            boss = BuildInLevel();
            yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.69f));
            float until = Time.time + 12f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
        }

        [UnityTest]
        public IEnumerator Phase2_TransitionCollapsesTheThicketAndEndsInChase()
        {
            boss = BuildInLevel();
            yield return null;
            Assert.Greater(boss.ThicketCells, 1000);
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.69f));
            yield return null;
            yield return null;
            Assert.IsTrue(boss.TransitionStarted);
            Assert.AreEqual(ArthropleuraBoss.Mode.Phase2Pending, boss.CurrentMode);
            float until = Time.time + 12f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            Assert.AreEqual(0, boss.ThicketCells, "La espesura entera debe haber caído.");
        }

        [UnityTest]
        public IEnumerator Phase2_StompFiresThreeWavesAndReturnsToChase()
        {
            yield return ReachChase();
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            boss.TestStartStomp();
            float until = Time.time + 12f;
            while (!(boss.WavesFired >= 3 && boss.ActiveWaves == 0 && boss.CurrentMode == ArthropleuraBoss.Mode.Chase) && Time.time < until) yield return null;
            Assert.AreEqual(3, boss.WavesFired);
            Assert.AreEqual(0, boss.ActiveWaves);
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase2_WaveHurtsThePlayerInItsArc()
        {
            boss = BuildInLevel();
            var player = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            player.transform.SetParent(room.transform);
            player.transform.position = boss.ArenaCenter + Vector2.right * 20f;
            var health = player.GetComponent<Health>();
            yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.69f));
            float until = Time.time + 12f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            boss.Moving = true;
            boss.TestStartStomp();
            player.transform.position = boss.GroundToWorld(boss.NodeGround(0) + Vector2.right * 8f);
            int before = health.Current;
            until = Time.time + 5f;
            while (health.Current == before && Time.time < until) yield return null;
            Assert.Less(health.Current, before, "Una onda en arco debe dañar al jugador que está en su camino.");
        }

        [UnityTest]
        public IEnumerator Phase2_ChargeEndsByDistanceAndReturnsToChase()
        {
            yield return ReachChase();
            CallBoss("StartWarn");
            Assert.AreEqual(ArthropleuraBoss.Mode.Warn, boss.CurrentMode);
            float until = Time.time + 6f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Charge && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Charge, boss.CurrentMode);
            until = Time.time + 10f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase2_FireStillOpensPlatesAndHurtsDownTo35Percent()
        {
            yield return ReachChase();
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            Assert.IsTrue(boss.IgnitePiece(10), "En la F2 se puede prender una pieza en cualquier punto de la sala.");
            int before = boss.Health.Current;
            Simulate(30f);
            Assert.Less(boss.Health.Current, before);
            Assert.GreaterOrEqual(boss.Health.Current, Mathf.RoundToInt(boss.Health.Max * 0.35f));
        }

        [UnityTest]
        public IEnumerator Phase2_BuildsFernsAndRegrowsThem()
        {
            yield return ReachChase();
            Assert.AreEqual(6, boss.FernCount);
            Object.Destroy(boss.FernAt(0).gameObject);
            yield return null;
            Assert.AreEqual(5, boss.FernCount);
            Simulate(26f);
            Assert.AreEqual(6, boss.FernCount);
        }

        [UnityTest]
        public IEnumerator Phase2_EatingABurningFernLightsTheHead()
        {
            yield return ReachChase();
            boss.FernAt(1).Ignite();
            Assert.IsTrue(boss.FernAt(1).IsBurning);
            boss.TestEat(1);
            float until = Time.time + 5f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            Assert.AreEqual(ArthropleuraBoss.FireState.Burning, boss.PieceFire(0));
            Assert.AreEqual(5, boss.FernCount);
        }

        [UnityTest]
        public IEnumerator Phase2_ChargeFromNextToAWallStillTravels()
        {
            yield return ReachChase();
            // La cabeza llega pegada a la pared este y embiste hacia dentro: antes la embestida acababa en el primer cuadro.
            typeof(ArthropleuraBoss).GetField("headPos", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, new Vector2(27.5f, 0f));
            typeof(ArthropleuraBoss).GetField("chargeDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, Vector2.left);
            CallBoss("StartCharge", new Vector2(27.5f, 0f));
            Assert.AreEqual(ArthropleuraBoss.Mode.Charge, boss.CurrentMode);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(ArthropleuraBoss.Mode.Charge, boss.CurrentMode, "La embestida no debe acabar al instante.");
            Assert.Less(boss.NodeGround(0).x, 20f, "La cabeza debe haber avanzado hacia dentro.");
        }

        [UnityTest]
        public IEnumerator Phase2_BurningBodyRunsToTheMudAndPutsItselfOut()
        {
            yield return ReachChase();
            Vector2 lake = boss.ArenaCenter + new Vector2(-18f, 6f);
            boss.MudProbe = p => Vector2.Distance(p, lake) < 5f;
            for (int i = 0; i < 8; i++) boss.IgnitePiece(i);
            Simulate(3f);
            Assert.GreaterOrEqual(boss.BurningPieces, 5);
            float until = Time.time + 9f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Douse && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Douse, boss.CurrentMode, "Con 5 o más piezas ardiendo debe ir al lago.");
            until = Time.time + 14f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.Douse && Time.time < until) yield return null;
            Assert.AreNotEqual(ArthropleuraBoss.Mode.Douse, boss.CurrentMode);
            int charred = 0;
            for (int i = 0; i < boss.PieceCount; i++)
                if (boss.PieceFire(i) == ArthropleuraBoss.FireState.Charred) charred++;
            Assert.GreaterOrEqual(charred, 3, "El lodo debe haber apagado piezas (quedan chamuscadas).");
        }

        [UnityTest]
        public IEnumerator Phase2_NoMudNoDouse()
        {
            yield return ReachChase();
            boss.MudProbe = p => false;
            for (int i = 0; i < 8; i++) boss.IgnitePiece(i);
            Simulate(3f);
            yield return new WaitForSeconds(1f);
            Assert.AreNotEqual(ArthropleuraBoss.Mode.Douse, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase2_MudPutsOutEveryBurningPieceItTouches()
        {
            yield return ReachChase();
            boss.Moving = false;
            for (int i = 0; i < 6; i++) boss.IgnitePiece(i);
            Assert.AreEqual(6, boss.BurningPieces);
            boss.MudProbe = p => true;
            boss.TickFire(0.02f);
            Assert.AreEqual(0, boss.BurningPieces);
            Assert.AreEqual(ArthropleuraBoss.FireState.Charred, boss.PieceFire(0));
        }

        [UnityTest]
        public IEnumerator Phase2_ChargeThroughASigillariaFellsItAndStunsTheHead()
        {
            yield return ReachChase();
            Assert.AreEqual(4, boss.StandingTrees);
            int before = boss.Health.Current;
            var trunk = GameObject.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).First(c => c.name == "SigillariaF2");
            Assert.Greater(trunk.bounds.size.y, 3f, "El tronco entero debe ser sólido, no solo la base.");
            Vector2 foot = boss.TreeFoot(0);
            // La cabeza arranca a la izquierda del árbol y embiste hacia él.
            Vector2 start = boss.WorldToGround(foot + Vector2.left * 5f);
            typeof(ArthropleuraBoss).GetField("headPos", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, start);
            typeof(ArthropleuraBoss).GetField("chargeDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, Vector2.right);
            CallBoss("StartCharge", start);
            float until = Time.time + 3f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Stunned && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Stunned, boss.CurrentMode);
            Assert.AreEqual(3, boss.StandingTrees);
            Assert.AreEqual(before - ArthropleuraBoss.TreeImpactDamage, boss.Health.Current, "Embestir un árbol le cuesta vida al jefe.");
            until = Time.time + 6f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.Stunned && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode, "Tras el aturdimiento vuelve a perseguir.");
        }

        [UnityTest]
        public IEnumerator Phase2_FallenLogCoversFromWaves()
        {
            yield return ReachChase();
            boss.Moving = false;
            Vector2 foot = boss.TreeFoot(0);
            typeof(ArthropleuraBoss).GetField("headPos", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, boss.WorldToGround(foot + Vector2.left * 5f));
            Assert.IsFalse(boss.LogBlocks(foot + Vector2.left * 3f, foot + Vector2.right * 6f), "Con el árbol en pie no hay cobertura.");
            boss.Moving = true;
            CallBoss("StartCharge", boss.WorldToGround(foot + Vector2.left * 5f));
            typeof(ArthropleuraBoss).GetField("chargeDirection", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, Vector2.right);
            float until = Time.time + 3f;
            while (boss.StandingTrees == 4 && Time.time < until) yield return null;
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(boss.LogBlocks(foot + Vector2.left * 1f, foot + Vector2.right * 5f), "El tronco tumbado tapa de las ondas.");
            Assert.IsFalse(boss.LogBlocks(foot + Vector2.up * 4f, foot + Vector2.up * 8f), "Lejos del tronco no hay cobertura.");
        }

        [UnityTest]
        public IEnumerator Phase2_ChaseGoesAroundAStandingTree()
        {
            boss = BuildInLevel();
            var player = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            player.transform.SetParent(room.transform);
            yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.69f));
            float until = Time.time + 12f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            typeof(ArthropleuraBoss).GetField("eatCooldownAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, 1e9f);
            Vector2 foot = boss.TreeFoot(3);
            // Cabeza a la izquierda del árbol y jugador a la derecha, justo detrás: en línea recta pasaría por el tronco.
            Vector2 start = boss.WorldToGround(foot + Vector2.left * 7f);
            typeof(ArthropleuraBoss).GetField("headPos", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, start);
            typeof(ArthropleuraBoss).GetField("heading", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, 0f);
            player.transform.position = foot + Vector2.right * 5f;
            float closest = float.MaxValue;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                Vector2 head = boss.GroundToWorld(boss.NodeGround(0));
                closest = Mathf.Min(closest, Vector2.Distance(head, foot + Vector2.up * 1.3f));
                yield return null;
            }
            Assert.Greater(closest, 1.2f, "La cabeza no debe atravesar el árbol al perseguir.");
            Assert.AreEqual(4, boss.StandingTrees);
        }

        // F3 (D1): transición del 35 %, cerco con hueco y apretón.
        IEnumerator ReachPhase3()
        {
            yield return ReachChase();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            float until = Time.time + 8f;
            while ((!boss.Transition3Started || boss.CurrentMode != ArthropleuraBoss.Mode.Chase) && Time.time < until) yield return null;
        }

        [UnityTest]
        public IEnumerator Phase3_TransitionInvulnerabilityThenChase()
        {
            yield return ReachChase();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            yield return null;
            yield return null;
            Assert.IsTrue(boss.Transition3Started);
            Assert.AreEqual(ArthropleuraBoss.Mode.Phase3Pending, boss.CurrentMode);
            float until = Time.time + 8f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.Phase3Pending && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase3_RingFormsHoldsContractsAndReleases()
        {
            yield return ReachPhase3();
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            boss.TestStartRing();
            Assert.AreEqual(ArthropleuraBoss.Mode.RingForm, boss.CurrentMode);
            float until = Time.time + 6f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.RingForm && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.RingHold, boss.CurrentMode);
            // Con el anillo formado, cada pieza queda cerca del radio del anillo (el cuerpo de verdad lo forma).
            Vector2 c = boss.RingCenter;
            int onRing = 0;
            for (int i = 1; i < boss.PieceCount - 1; i++)
                if (Mathf.Abs(Vector2.Distance(boss.NodeGround(i), c) - boss.RingRadius) < 2.5f) onRing++;
            Assert.GreaterOrEqual(onRing, 15, "El cuerpo debe formar el anillo (la cola aún sale de la espiral).");
            until = Time.time + 6f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.RingHold && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.RingContract, boss.CurrentMode);
            until = Time.time + 6f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.RingContract && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase3_SqueezeHurtsAndThrowsOutAPlayerInsideTheRing()
        {
            boss = BuildInLevel();
            var player = new GameObject("Player", typeof(Primordia.Player.PlayerController));
            player.transform.SetParent(room.transform);
            player.transform.position = boss.ArenaCenter;
            var health = player.GetComponent<Health>();
            yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.69f));
            float until = Time.time + 12f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Chase && Time.time < until) yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            until = Time.time + 8f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.Phase3Pending && Time.time < until) yield return null;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.Phase3Pending && Time.time < until + 4f) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            yield return new WaitForSeconds(1.5f); // acaba la invulnerabilidad de la transición
            health.Heal(health.Max);
            player.transform.position = boss.ArenaCenter + new Vector2(2f, 0f);
            boss.TestStartRing();
            int before = health.Current;
            until = Time.time + 14f;
            while (!boss.Squeezed && Time.time < until) yield return null;
            Assert.IsTrue(boss.Squeezed, "El jugador que sigue dentro al cerrarse recibe el apretón.");
            Assert.AreEqual(before - ArthropleuraBoss.SqueezeDamage, health.Current);
            Vector2 offset = boss.WorldToGround(player.transform.position) - boss.RingCenter;
            Assert.Greater(offset.magnitude, 13f, "Sale despedido fuera del anillo.");
        }

        // F3 (D2): tramos que se abren solos y daño hasta el 10 %.
        [UnityTest]
        public IEnumerator Phase3_SectionsOpenAndHurtDownTo10Percent()
        {
            yield return ReachPhase3();
            Assert.AreEqual(ArthropleuraBoss.Mode.Chase, boss.CurrentMode);
            boss.Moving = false;
            // Simula el reloj de fuego: cada 4 s se abre un tramo de 2-3 piezas y las abiertas dañan.
            int before = boss.Health.Current;
            int maxOpen = 0;
            for (int i = 0; i < 100 * 50; i++)
            {
                boss.TickFire(0.02f);
                int open = 0;
                for (int p = 0; p < boss.PieceCount; p++) if (boss.PieceOpen(p)) open++;
                maxOpen = Mathf.Max(maxOpen, open);
            }
            Assert.GreaterOrEqual(boss.SectionsOpened, 20);
            Assert.That(boss.LastSectionLength, Is.InRange(2, 3));
            Assert.That(maxOpen, Is.InRange(2, 6));
            Assert.Less(boss.Health.Current, before);
            Assert.GreaterOrEqual(boss.Health.Current, Mathf.RoundToInt(boss.Health.Max * 0.1f), "La F3 acaba en el 10 %: ahí empieza la rueda.");
        }

        [UnityTest]
        public IEnumerator Phase3_FireOpensPlatesButDoesNotHurt()
        {
            yield return ReachPhase3();
            boss.Moving = false;
            // Sin tramos que se abran solos: así solo queda el efecto del fuego.
            typeof(ArthropleuraBoss).GetField("nextSectionAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boss, 1e9f);
            Assert.IsTrue(boss.IgnitePiece(12));
            int before = boss.Health.Current;
            bool opened = false;
            for (int i = 0; i < 20 * 50; i++)
            {
                boss.TickFire(0.02f);
                opened |= boss.PieceOpen(12);
            }
            Assert.IsTrue(opened, "El fuego abre las placas.");
            Assert.AreEqual(before, boss.Health.Current, "En la F3 el fuego abre placas pero no daña.");
        }

        // Rueda del último 10 % (E1) y muerte (E2).
        IEnumerator ReachWheel()
        {
            yield return ReachPhase3();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.1f));
            float until = Time.time + 8f;
            while ((!boss.Transition4Started || boss.CurrentMode == ArthropleuraBoss.Mode.Phase4Pending) && Time.time < until) yield return null;
        }

        [UnityTest]
        public IEnumerator Phase4_BodyBecomesAWheelThatWarnsThenRolls()
        {
            yield return ReachChase();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            float until = Time.time + 8f;
            while ((!boss.Transition3Started || boss.CurrentMode != ArthropleuraBoss.Mode.Chase) && Time.time < until) yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.1f));
            yield return null;
            yield return null;
            Assert.IsTrue(boss.Transition4Started);
            Assert.IsTrue(boss.WheelActive);
            Assert.AreEqual(ArthropleuraBoss.Mode.Phase4Pending, boss.CurrentMode);
            Assert.IsTrue(BodyRenderers().All(r => !r.gameObject.activeSelf), "Las piezas desaparecen: el cuerpo es una rueda.");
            until = Time.time + 6f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.WheelWarn && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.WheelWarn, boss.CurrentMode);
            until = Time.time + 4f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.WheelRoll && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.WheelRoll, boss.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Phase4_WallCrashCostsHealthAndStuns()
        {
            yield return ReachChase();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            float until = Time.time + 8f;
            while ((!boss.Transition3Started || boss.CurrentMode != ArthropleuraBoss.Mode.Chase) && Time.time < until) yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.1f));
            until = Time.time + 8f;
            while (boss.CurrentMode != ArthropleuraBoss.Mode.WheelRoll && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.WheelRoll, boss.CurrentMode);
            int before = boss.Health.Current;
            until = Time.time + 8f;
            while (boss.CurrentMode == ArthropleuraBoss.Mode.WheelRoll && Time.time < until) yield return null;
            Assert.AreEqual(ArthropleuraBoss.Mode.WheelStun, boss.CurrentMode, "Choca contra una pared y queda aturdida.");
            Assert.AreEqual(before - ArthropleuraBoss.WheelHitDamage, boss.Health.Current);
        }

        [UnityTest]
        public IEnumerator Phase4_SixCrashesKillItAndLeaveASolidCorpse()
        {
            yield return ReachChase();
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.34f));
            float until = Time.time + 8f;
            while ((!boss.Transition3Started || boss.CurrentMode != ArthropleuraBoss.Mode.Chase) && Time.time < until) yield return null;
            boss.Health.Drain(boss.Health.Current - Mathf.RoundToInt(boss.Health.Max * 0.1f));
            Assert.AreEqual(60, boss.Health.Max / 10);
            var enemy = boss.GetComponent<Enemy>();
            bool died = false;
            enemy.Died += _ => died = true;
            Transform parent = boss.transform.parent;
            for (int i = 0; i < 6; i++) boss.TestCrashWheel();
            Assert.IsTrue(died);
            yield return null;
            yield return new WaitForSeconds(0.2f);
            var corpse = Object.FindAnyObjectByType<ArthropleuraCorpse>();
            Assert.IsNotNull(corpse, "Queda un cadáver aunque el jefe se destruya.");
            yield return new WaitForSeconds(3.4f);
            Assert.AreEqual(corpse.PieceCount, corpse.DeadPieces, "Todas las piezas acaban muertas.");
            Assert.IsTrue(corpse.HeadDead);
            Assert.GreaterOrEqual(corpse.ColliderCount, corpse.PieceCount + 1, "Cada pieza del cuerpo y la cabeza son sólidas.");
        }
    }
}
#endif
