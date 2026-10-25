using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Piso 2, paso 3: las fosas. El jugador que anda encima cae (−½ corazón y vuelta al último suelo firme), esquivando las
    /// cruza; los enemigos que andan las rodean y, empujados, caen y mueren; los que nadan pasan por encima.
    /// </summary>
    public class PitTests
    {
        readonly List<Object> created = new();
        FloorMap map;

        [SetUp]
        public void SetUp()
        {
            // Suelo de 20×5 con una fosa de 2 tiles de ancho en x = 6 y 7.
            map = new FloorMap(new RectInt(-1, -1, 22, 7));
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 5; y++)
                    map.Set(new Vector2Int(x, y), x is 6 or 7 ? FloorMap.Kind.Pit : FloorMap.Kind.Floor);
            Pits.Map = map;
        }

        [TearDown]
        public void TearDown()
        {
            Pits.Map = null;
            NavigationGrid.Current = null;
            NavigationGrid.Swimming = null;
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

        (PlayerController controller, PlayerPitFall fall, Health health, Rigidbody2D body) CreatePlayer(Vector2 position)
        {
            var player = Track(new GameObject("Player"));
            player.transform.position = position;
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var health = player.AddComponent<Health>();
            health.SetMax(6);
            health.ResetHealth();
            var controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            var fall = player.AddComponent<PlayerPitFall>();
            return (controller, fall, health, body);
        }

        Enemy CreateEnemy(Vector2 position, bool swims, int splitCount = 0, Enemy splitPrefab = null)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, 5, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0, 6f, null, splitPrefab, splitCount);
            data.ConfigureSwimming(swims);
            var go = new GameObject("Enemy");
            go.transform.position = position;
            new GameObject("Body").transform.SetParent(go.transform, false);
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            enemy.SetData(data);
            return enemy;
        }

        [Test]
        public void Pits_KnowWhereTheyAre()
        {
            Assert.IsTrue(Pits.IsPit(new Vector2(6.5f, 2.5f)));
            Assert.IsFalse(Pits.IsPit(new Vector2(5.5f, 2.5f)));
            Assert.IsFalse(Pits.IsSafe(new Vector2(5.7f, 2.5f), 0.45f), "Pegado al borde no cuenta como suelo firme.");
            Assert.IsTrue(Pits.IsSafe(new Vector2(4.5f, 2.5f), 0.45f));
        }

        [UnityTest]
        public IEnumerator Player_WalkingIntoAPit_FallsBackToSolidGround_AndLosesHalfAHeart()
        {
            var (_, fall, health, body) = CreatePlayer(new Vector2(3.5f, 2.5f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            body.position = new Vector2(6.5f, 2.5f);
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(fall.IsFalling, "Cae al pisar la fosa (tras la tolerancia de 0,08 s).");

            yield return new WaitForSeconds(PitFallEffect.Duration + 0.2f);
            Assert.IsFalse(fall.IsFalling);
            Assert.AreEqual(3.5f, body.position.x, 0.05f, "Vuelve al último suelo firme.");
            Assert.AreEqual(6 - PlayerPitFall.FallDamage, health.Current, "Medio corazón menos.");
            Assert.AreEqual(Vector3.one, fall.transform.localScale, "Vuelve a su tamaño.");
        }

        [UnityTest]
        public IEnumerator Player_LeaningOverTheEdge_DoesNotFall()
        {
            // Fosa horizontal: filas 3 y 4 en las columnas 10–15, con suelo debajo. El centro del cuerpo ya está sobre la
            // fosa, pero los pies (0,3 por debajo) apenas la rozan.
            for (int x = 10; x < 16; x++)
                for (int y = 3; y < 5; y++)
                    map.Set(new Vector2Int(x, y), FloorMap.Kind.Pit);
            var (_, fall, health, body) = CreatePlayer(new Vector2(12.5f, 1.5f));
            yield return new WaitForFixedUpdate();

            body.position = new Vector2(12.5f, 3.35f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(fall.IsFalling, "Asomarse por abajo no hace caer: los pies apenas pisan el borde.");

            body.position = new Vector2(10.1f, 4f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(fall.IsFalling, "Ni rozar el borde lateral.");
            Assert.AreEqual(6, health.Current);
        }

        [UnityTest]
        public IEnumerator Player_Dodging_CrossesAPitOfTwoTiles()
        {
            var (controller, fall, health, body) = CreatePlayer(new Vector2(5.2f, 2.5f));
            yield return new WaitForFixedUpdate();

            // Sin entrada, la esquiva va hacia donde apunta (la derecha): 3 u, de 5,2 a 8,2, por encima de la fosa.
            Assert.IsTrue(controller.TryDodge());
            while (controller.IsDodging)
            {
                Assert.IsFalse(fall.IsFalling, "Esquivando no se cae.");
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(fall.IsFalling);
            Assert.Greater(body.position.x, 8f, "Llega al otro lado.");
            Assert.AreEqual(6, health.Current);
        }

        [UnityTest]
        public IEnumerator WalkingEnemy_PushedIntoAPit_FallsAndDies_WithoutSplitting()
        {
            var child = CreateEnemy(new Vector2(15.5f, 2.5f), swims: false);
            child.gameObject.SetActive(false);
            var enemy = CreateEnemy(new Vector2(6.5f, 2.5f), swims: false, splitCount: 2, splitPrefab: child);
            bool died = false;
            enemy.Died += _ => died = true;

            yield return new WaitForSeconds(PitFallEffect.Duration + 0.3f);
            Assert.IsTrue(died, "Cae y muere.");
            Assert.IsTrue(enemy == null || enemy.FellIntoPit);
            Assert.AreEqual(1, Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include).Length, "No suelta crías en el fondo.");
        }

        [UnityTest]
        public IEnumerator SwimmingEnemy_FloatsOverPits_WithItsShadowOnTheGround()
        {
            var enemy = CreateEnemy(new Vector2(6.5f, 2.5f), swims: true);
            yield return new WaitForSeconds(PitFallEffect.Duration + 0.3f);

            Assert.IsFalse(enemy.Health.IsDead, "Los que nadan no caen.");
            var look = enemy.GetComponent<SwimmerLook>();
            Assert.IsNotNull(look);
            Assert.IsNotNull(look.Shadow, "Sombra separada.");
            Assert.Greater(enemy.transform.Find("Body").localPosition.y, 0.2f, "El cuerpo flota por encima de su sombra.");
        }

        [UnityTest]
        public IEnumerator WalkingEnemy_WithNoWayToThePlayer_WaitsAtTheEdge()
        {
            // La fosa (x = 6 y 7) corta el suelo de arriba abajo: no hay camino andando hasta el jugador, al otro lado.
            NavigationGrid.Current = new NavigationGrid(map);
            NavigationGrid.Swimming = NavigationGrid.ForSwimmers(map);
            var target = Track(new GameObject("Target"));
            target.transform.position = new Vector2(10.5f, 2.5f);

            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, 5, moveSpeed: 4f, detectionRange: 20f, contactDamage: 1);
            var go = new GameObject("Enemy");
            go.transform.position = new Vector2(2.5f, 2.5f);
            new GameObject("Body").transform.SetParent(go.transform, false);
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>().Target = target.transform;
            enemy.SetData(data);

            yield return new WaitForSeconds(2f);
            Assert.IsFalse(enemy.Health.IsDead, "No se tira a la fosa.");
            Assert.Less(enemy.transform.position.x, 6f, "Espera en la orilla.");
            Assert.Greater(enemy.transform.position.x, 4.5f, "Pero se acerca hasta ella.");
        }

        [Test]
        public void Navigation_WalkersGoAround_SwimmersGoAcross()
        {
            var cell = new Vector2Int(6, 2);
            Assert.IsFalse(new NavigationGrid(map).IsWalkable(cell));
            Assert.IsTrue(NavigationGrid.ForSwimmers(map).IsWalkable(cell));
        }
    }
}
