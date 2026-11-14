#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
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

        [UnityTest]
        public IEnumerator Chain_HasThirtyPiecesWithSprites()
        {
            yield return null;
            Assert.AreEqual(ArthropleuraBoss.SegmentCount + 2, boss.PieceCount);
            var renderers = boss.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(boss.PieceCount, renderers.Length);
            Assert.IsTrue(renderers.All(r => r.sprite != null), "Alguna pieza no tiene sprite.");
            Assert.AreEqual(600, boss.Health.Max);
        }

        [UnityTest]
        public IEnumerator Chain_DrawsTailBehindHead()
        {
            yield return null;
            var renderers = boss.GetComponentsInChildren<SpriteRenderer>();
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
    }
}
#endif
