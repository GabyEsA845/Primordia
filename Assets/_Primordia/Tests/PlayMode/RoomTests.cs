using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class RoomTests
    {
        readonly List<Object> created = new();
        Room room;
        Door door;
        Transform enemiesRoot;

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(enemy.gameObject);

            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
            Time.timeScale = 1f;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        EnemyData CreateData(Enemy splitPrefab = null, int splitCount = 0)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 1, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0,
                splitPrefab: splitPrefab, splitCount: splitCount);
            return data;
        }

        Enemy CreateEnemy(EnemyData data, Transform parent, Vector2 position)
        {
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            enemy.SetData(data);
            return enemy;
        }

        /// <summary>Sala de 10×10 en el origen con una puerta y los enemigos indicados.</summary>
        void CreateRoom(int enemyCount, EnemyData data = null)
        {
            var roomObject = Track(new GameObject("Room"));
            roomObject.AddComponent<BoxCollider2D>().size = new Vector2(10f, 10f);
            room = roomObject.AddComponent<Room>();

            enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);

            data ??= CreateData();
            for (int i = 0; i < enemyCount; i++)
                CreateEnemy(data, enemiesRoot, new Vector2(i * 2f - 2f, 2f));

            var doorObject = new GameObject("Door");
            doorObject.transform.SetParent(roomObject.transform, false);
            doorObject.transform.position = new Vector3(0f, 6f, 0f);
            doorObject.AddComponent<BoxCollider2D>();
            door = doorObject.AddComponent<Door>();

            room.Configure(enemiesRoot, new[] { door });
        }

        GameObject CreatePlayer(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = position;
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            return player;
        }

        static void KillAll(IEnumerable<Enemy> enemies)
        {
            foreach (var enemy in enemies)
                enemy.Health.TakeDamage(enemy.Health.Max);
        }

        [Test]
        public void Enemies_AreHidden_UntilTheRoomBegins()
        {
            CreateRoom(enemyCount: 2);

            Assert.IsFalse(enemiesRoot.gameObject.activeSelf);
            Assert.AreEqual(Room.RoomState.Waiting, room.State);
            Assert.IsTrue(door.IsOpen);
        }

        [Test]
        public void EmptyRoom_IsClearedOnBegin()
        {
            CreateRoom(enemyCount: 0);

            room.Begin();

            Assert.AreEqual(Room.RoomState.Cleared, room.State);
            Assert.IsTrue(door.IsOpen);
        }

        [Test]
        public void Begin_ClosesDoors_AndWakesTheEnemies()
        {
            CreateRoom(enemyCount: 2);

            room.Begin();

            Assert.AreEqual(Room.RoomState.Combat, room.State);
            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(2, room.AliveEnemies);
            Assert.IsTrue(enemiesRoot.gameObject.activeSelf);
            foreach (var ai in enemiesRoot.GetComponentsInChildren<EnemyAI>())
                Assert.IsTrue(ai.IsAlert);
        }

        [Test]
        public void KillingEveryEnemy_OpensTheDoors()
        {
            CreateRoom(enemyCount: 2);
            int cleared = 0;
            room.Cleared += _ => cleared++;

            room.Begin();
            KillAll(enemiesRoot.GetComponentsInChildren<Enemy>());

            Assert.AreEqual(Room.RoomState.Cleared, room.State);
            Assert.IsTrue(door.IsOpen);
            Assert.AreEqual(1, cleared);
        }

        [Test]
        public void SplitChildren_KeepTheRoomInCombat()
        {
            var splitTemplate = CreateEnemy(CreateData(), null, new Vector2(100f, 0f));
            CreateRoom(enemyCount: 1, CreateData(splitTemplate, splitCount: 2));
            room.Begin();

            var parent = enemiesRoot.GetComponentInChildren<Enemy>();
            var children = new List<Enemy>();
            parent.ChildSpawned += children.Add;
            parent.Health.TakeDamage(parent.Health.Max);

            Assert.AreEqual(2, children.Count);
            Assert.AreEqual(Room.RoomState.Combat, room.State);
            Assert.AreEqual(2, room.AliveEnemies);

            KillAll(children);

            Assert.AreEqual(Room.RoomState.Cleared, room.State);
        }

        [UnityTest]
        public IEnumerator PlayerEntering_BeginsTheRoom()
        {
            CreateRoom(enemyCount: 1);

            CreatePlayer(Vector2.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(Room.RoomState.Combat, room.State);
        }

        [UnityTest]
        public IEnumerator Exit_AppearsWhenCleared_AndTouchingItWins()
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            CreateRoom(enemyCount: 1);

            var exitObject = Track(new GameObject("Exit"));
            exitObject.transform.position = new Vector3(20f, 0f, 0f);
            exitObject.AddComponent<BoxCollider2D>();
            var exit = exitObject.AddComponent<RoomExit>();
            exit.SetRoom(room);

            Assert.IsFalse(exit.IsOpen);

            room.Begin();
            KillAll(enemiesRoot.GetComponentsInChildren<Enemy>());
            Assert.IsTrue(exit.IsOpen);

            CreatePlayer(new Vector2(20f, 0f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(GameState.Victory, manager.State);
        }
    }
}
