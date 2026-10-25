using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Piso 2, paso 4: almejas, corazón morado y oleadas por las entradas.</summary>
    public class Floor2PiecesTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
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

        Sprite[] Frames(string name, int count) => Enumerable.Range(0, count).Select(i =>
        {
            var sprite = Track(Sprite.Create(Track(new Texture2D(16, 16)), new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f));
            sprite.name = $"{name}_{i}";
            return sprite;
        }).ToArray();

        Health CreateHealth(int max = 6)
        {
            var health = Track(new GameObject("Player")).AddComponent<Health>();
            health.SetMax(max);
            health.ResetHealth();
            health.InvulnerabilityAfterHit = 0f;
            return health;
        }

        [Test]
        public void PurpleHeart_StopsAWholeHit_ThenIsSpent_ButIsRemembered()
        {
            var health = CreateHealth();
            health.AddPurpleHeart();
            Assert.AreEqual(1, health.PurpleHearts);

            Assert.IsTrue(health.TakeDamage(3));
            Assert.AreEqual(6, health.Current, "El corazón morado para el golpe entero.");
            Assert.AreEqual(0, health.PurpleHearts, "Y se gasta con él.");
            Assert.IsTrue(health.PurpleHeartFound, "Se recuerda que se encontró (sala secreta del piso 3).");

            health.TakeDamage(1);
            Assert.AreEqual(5, health.Current);
        }

        [Test]
        public void PurpleHeart_GoesBeforeTheGreenBarrier()
        {
            var health = CreateHealth();
            health.Barrier = 1;
            health.AddPurpleHeart();
            health.TakeDamage(2);
            Assert.AreEqual(0, health.PurpleHearts);
            Assert.AreEqual(1, health.Barrier, "El verde sigue ahí para el siguiente golpe.");
        }

        [UnityTest]
        public IEnumerator PurpleHeart_InlaysEveryHeartOfTheHud_UntilItIsSpent()
        {
            var health = CreateHealth();
            var hud = Track(new GameObject("Hearts", typeof(RectTransform)));
            var display = hud.AddComponent<UI.HeartsDisplay>();
            var full = Frames("Full", 1)[0];
            display.Configure(health, full, Frames("Half", 1)[0], Frames("Empty", 1)[0]);
            var inlays = Frames("Inlay", 3);
            display.SetPurpleInlays(inlays);
            yield return null;

            Assert.AreEqual(0, display.InlaidShown);
            health.AddPurpleHeart();
            Assert.AreEqual(3, display.InlaidShown, "Los tres corazones llevan el contorno morado y las grietas.");
            Assert.AreEqual(0, display.PurpleShown, "Con las grietas no hace falta el icono suelto.");

            health.TakeDamage(1);
            Assert.AreEqual(0, display.InlaidShown, "Gastado el corazón morado, vuelven a ser rojos.");
        }

        [Test]
        public void HeartCoral_GivesThePurpleHeart_Once()
        {
            var coralObject = Track(new GameObject("HeartCoral"));
            var body = coralObject.AddComponent<SpriteRenderer>();
            var coral = coralObject.AddComponent<HeartCoral>();
            var taken = Frames("Taken", 1)[0];
            coral.Configure(body, Frames("Idle", 6), taken);

            var health = CreateHealth();
            Assert.IsTrue(coral.IsAvailable);
            coral.Interact(health.gameObject);
            Assert.AreEqual(1, health.PurpleHearts);
            Assert.IsTrue(coral.Taken);
            Assert.IsFalse(coral.IsAvailable);
            Assert.AreSame(taken, body.sprite, "Queda el coral sin el corazón.");

            coral.Interact(health.gameObject);
            Assert.AreEqual(1, health.PurpleHearts, "Solo hay uno.");
        }

        [UnityTest]
        public IEnumerator Clam_OpensWithE_AndStaysOpen()
        {
            var clamObject = Track(new GameObject("Clam"));
            var body = clamObject.AddComponent<SpriteRenderer>();
            var clam = clamObject.AddComponent<Clam>();
            var opened = Frames("Opened", 1)[0];
            clam.Configure(body, Frames("Idle", 4), Frames("Open", 6), opened, false, new Vector2Int(5, 15), null, null, null, null);

            Assert.IsTrue(clam.IsAvailable);
            clam.Interact(Track(new GameObject("Player")));
            Assert.IsTrue(clam.IsOpen);
            Assert.IsFalse(clam.IsAvailable, "Una vez abierta ya no se ofrece.");
            yield return new WaitForSeconds(0.7f);
            Assert.AreSame(opened, body.sprite);
        }

        [Test]
        public void BigClam_PicksASpecialMutation_ThePlayerDoesNotHave()
        {
            var a = Track(ScriptableObject.CreateInstance<StatMutation>());
            a.ConfigureBase("A", "", MutationSlot.Limbs, Color.white);
            var b = Track(ScriptableObject.CreateInstance<StatMutation>());
            b.ConfigureBase("B", "", MutationSlot.Skin, Color.white);

            var playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Combat.Weapon>();
            playerObject.AddComponent<Player.PlayerController>().readInput = false;
            var player = playerObject.AddComponent<PlayerMutations>();
            player.Equip(a);

            for (int seed = 0; seed < 20; seed++)
                Assert.AreSame(b, Clam.PickMutation(new MutationData[] { a, b }, player, new System.Random(seed)));
            Assert.IsNull(Clam.PickMutation(new MutationData[] { a }, player, new System.Random(1)), "Sin especiales libres, solo biomasa.");
            Assert.IsNull(Clam.PickMutation(System.Array.Empty<MutationData>(), player, new System.Random(1)));
        }

        Enemy CreateEnemy(Transform parent)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, 3, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent, false);
            var enemy = go.AddComponent<Enemy>();
            enemy.SetData(data);
            return enemy;
        }

        [UnityTest]
        public IEnumerator Room_WithWaves_SendsTheNextOne_AndClearsAfterTheLast()
        {
            var roomObject = Track(new GameObject("Room"));
            roomObject.AddComponent<BoxCollider2D>();
            var first = new GameObject("Enemies").transform;
            first.SetParent(roomObject.transform, false);
            var second = new GameObject("Wave_2").transform;
            second.SetParent(roomObject.transform, false);
            var third = new GameObject("Wave_3").transform;
            third.SetParent(roomObject.transform, false);
            var enemies = new[] { CreateEnemy(first), CreateEnemy(second), CreateEnemy(third) };

            var ventObject = Track(new GameObject("Vent"));
            var vent = ventObject.AddComponent<WaveVent>();
            vent.Configure(ventObject.AddComponent<SpriteRenderer>(), Frames("Idle", 4), Frames("Active", 6));

            var room = roomObject.AddComponent<Room>();
            room.Configure(first, System.Array.Empty<Door>());
            room.ConfigureWaves(new[] { second, third }, new[] { vent });
            Assert.IsFalse(second.gameObject.activeSelf, "Las oleadas siguientes esperan.");

            int cleared = 0;
            room.Cleared += _ => cleared++;
            room.Begin();
            Assert.AreEqual(Room.RoomState.Combat, room.State);
            Assert.AreEqual(2, room.PendingWaves);

            for (int wave = 0; wave < 3; wave++)
            {
                enemies[wave].Health.TakeDamage(100);
                yield return null;
                if (wave < 2)
                {
                    Assert.AreEqual(Room.RoomState.Combat, room.State, $"Tras la oleada {wave + 1} sigue el combate.");
                    yield return new WaitForSeconds(1.5f);
                    Assert.AreEqual(1, room.AliveEnemies, $"Llega la oleada {wave + 2}.");
                }
            }

            Assert.AreEqual(Room.RoomState.Cleared, room.State, "Limpia tras la última oleada.");
            Assert.AreEqual(1, cleared);
        }
    }
}
