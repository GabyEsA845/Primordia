#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class LepidodendronBossTests
    {
        const string Root = "Assets/_Primordia/";
        const string PrefabPath = Root + "Prefabs/Enemies/Boss_Lepidodendron.prefab";
        const string RoomPath = Root + "Data/Rooms/Floor3/Room_Boss_Lepidodendron.asset";
        const string ConfigPath = Root + "Data/Rooms/Floor_BossLepidodendronTest.asset";
        const string ScenePath = Root + "Scenes/Test_BossLepidodendron.unity";
        GameObject room, walker, cameraRig;
        LepidodendronBoss boss;

        [SetUp]
        public void SetUp()
        {
            room = new GameObject("SalaLepidodendron");
            room.transform.position = new Vector2(200f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Ejecuta Primordia > Boss > Crear Lepidodendron y escena de prueba.");
            boss = Object.Instantiate(prefab, room.transform).GetComponent<LepidodendronBoss>();
            boss.SetArena(room.transform, new Vector2(158f, 30f));
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (walker != null) Object.DestroyImmediate(walker);
            if (room != null) Object.DestroyImmediate(room);
            if (cameraRig != null) Object.DestroyImmediate(cameraRig);
        }

        Transform Scenery => boss.transform.Find("Scenery");

        [Test]
        public void ElMenuDejaAssetsCompletosSinReferenciasPerdidas()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, "Componente perdido en el prefab.");
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.IsFalse(property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None,
                            $"Referencia perdida: {component.name}/{property.propertyPath}");
            }
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            Assert.IsNotNull(config);
            Assert.AreEqual(1, config.BossOptions().Length);
            Assert.AreSame(prefab.GetComponent<Enemy>(), config.BossOptions()[0].prefab);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Assert.IsTrue(AssetDatabase.GetDependencies(ScenePath).Contains(ConfigPath));
            Assert.IsTrue(AssetDatabase.GetDependencies(PrefabPath).Contains(Root + "Data/Bosses/LepidodendronArt.asset"));
            foreach (var sheet in boss.Art.Sheets)
            {
                Assert.AreEqual(1, sheet.rows);
                Assert.IsTrue(sheet.sprites.All(s => s != null), sheet.name);
                Assert.AreEqual(sheet.frames, sheet.sprites.Length);
            }
        }

        [Test]
        public void LaSalaTienePasillo116Por18Arena40Por26YEntradaOeste()
        {
            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(RoomPath);
            Assert.IsEmpty(template.Validate());
            Assert.AreEqual(158, template.Width);
            Assert.AreEqual(30, template.Height);
            Assert.AreEqual(new RectInt(1, 1, 156, 26), template.FloorBounds());
            for (int x = 1; x <= 156; x++)
            {
                int floors = Enumerable.Range(0, 30).Count(y => template.Get(x, y) == RoomTemplate.Mark.Floor);
                Assert.AreEqual(x <= 116 ? 18 : 26, floors, $"Columna {x}");
            }
            Assert.AreEqual(2, template.DoorCells(Vector2Int.left).Count);
            Assert.IsEmpty(template.DoorCells(Vector2Int.up));
            Assert.IsEmpty(template.DoorCells(Vector2Int.down));
            Assert.IsEmpty(template.DoorCells(Vector2Int.right));
        }

        [Test]
        public void LaEscenaTieneSemillaQueEntraPorElOesteSinOtrosJefes()
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            string scene = File.ReadAllText(ScenePath);
            var match = System.Text.RegularExpressions.Regex.Match(scene, @"fixedSeed: (\d+)");
            Assert.IsTrue(match.Success);
            var layout = FloorLayoutGenerator.Generate(config, new System.Random(int.Parse(match.Groups[1].Value)));
            Assert.AreEqual(Vector2Int.left, layout.Boss.Doors.Single());
            Assert.AreEqual(3, layout.Rooms.Count);
            Assert.IsFalse(config.HasTransition);
            Assert.IsTrue(layout.Rooms.All(r => r.Role != RoomRole.Combat));
            layout.ReleaseGenerated();
        }

        [Test]
        public void SetArenaCreaArbolParedesCopaYDosRayosSinDuplicar()
        {
            Assert.AreEqual(boss.ArenaCenter.y + 13f - 10.5f, boss.Foot.y, 0.001f);
            Assert.AreEqual(boss.Foot, (Vector2)Scenery.Find("Trunk").position);
            for (int i = 0; i < 3; i++)
            {
                var wall = Scenery.Find($"Wall{i + 1}");
                Assert.IsNotNull(wall);
                Assert.AreEqual(new[] { 34f, 64f, 94f }[i], wall.position.x - (room.transform.position.x - 78f), 0.001f);
                var solid = wall.GetComponent<BoxCollider2D>();
                Assert.IsFalse(solid.isTrigger);
                Assert.AreEqual(new[] { 3f, 3.5f, 4f }[i], solid.size.x);
                Assert.AreEqual(18f, solid.size.y);
                Assert.AreEqual(i + 2, wall.childCount);
            }
            Assert.IsNotNull(Scenery.Find("Canopy/ArenaCanopy"));
            Assert.AreEqual(2, Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name.StartsWith("LightShaft")));
            Assert.AreEqual(2, Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name.StartsWith("LightPool")));
            boss.SetArena(room.transform, new Vector2(158f, 30f));
            Assert.AreEqual(1, boss.GetComponentsInChildren<Transform>().Count(t => t.name == "Scenery"));
        }

        [Test]
        public void LasPiezasRespetanCeldasPivotesEscalaYOrdenDeDibujo()
        {
            var trunk = boss.Art.Find("Lepido_Trunk").sprites[0];
            Assert.AreEqual(new Vector2(288f, 384f), trunk.rect.size);
            Assert.AreEqual(144f, trunk.pivot.x, 0.001f);
            Assert.AreEqual(32f, trunk.pivot.y, 0.001f);
            foreach (var sheet in boss.Art.Sheets)
                foreach (var sprite in sheet.sprites)
                {
                    Assert.AreEqual(16f, sprite.pixelsPerUnit);
                    Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
                }
            string[] pieces = { "BackRoots", "MoundShadow", "Mound", "Dapple", "Trunk", "Veins", "LightShaft1" };
            int last = int.MinValue;
            foreach (string name in pieces)
            {
                int order = Scenery.Find(name).GetComponent<SpriteRenderer>().sortingOrder;
                Assert.Greater(order, last);
                last = order;
            }
            Assert.Greater(Scenery.Find("Canopy/ArenaCanopy").GetComponent<SpriteRenderer>().sortingOrder, last);
        }

        Rigidbody2D CreateWalker(Vector2 position)
        {
            walker = new GameObject("JugadorPrueba") { layer = LayerMask.NameToLayer(CombatLayers.Player) };
            walker.transform.position = position;
            var body = walker.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            walker.AddComponent<CircleCollider2D>().radius = 0.3f;
            return body;
        }

        [UnityTest]
        public IEnumerator LasTresParedesDetienenAlJugadorEnTodoElPasillo()
        {
            var body = CreateWalker(boss.TestStop(0));
            for (int i = 0; i < 3; i++)
            {
                var wall = Scenery.Find($"Wall{i + 1}").GetComponent<BoxCollider2D>();
                foreach (float y in new[] { -8.5f, 0f, 8.5f })
                {
                    body.position = new Vector2(wall.bounds.min.x - 1f, room.transform.position.y + y);
                    for (int step = 0; step < 15; step++)
                    {
                        body.linearVelocity = Vector2.right * 8f;
                        yield return new WaitForFixedUpdate();
                    }
                    Assert.Less(body.position.x, wall.bounds.min.x, $"Pared {i + 1}, y={y}");
                }
            }
        }

        [UnityTest]
        public IEnumerator ElPieYLasRaicesTraserasBloqueanElPaso()
        {
            var body = CreateWalker(boss.Foot + Vector2.down * 4f);
            for (int i = 0; i < 30; i++)
            {
                body.linearVelocity = Vector2.up * 8f;
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(body.position.y, boss.Foot.y - 1.5f);
            body.position = boss.Foot + new Vector2(-10f, 6f);
            for (int i = 0; i < 60; i++)
            {
                body.linearVelocity = Vector2.right * 12f;
                yield return new WaitForFixedUpdate();
            }
            Assert.Less(body.position.x, boss.Foot.x - 5f);
            var back = Scenery.Find("BackRoots").GetComponent<PolygonCollider2D>();
            Assert.IsTrue(back.OverlapPoint(boss.Foot + Vector2.up * 10.5f));
            Assert.IsTrue(back.OverlapPoint(boss.Foot));
        }

        [Test]
        public void LaSombraSubeLinealmenteDesdeLaPared1HastaLaArena()
        {
            float entrance = room.transform.position.x - 78f;
            Assert.AreEqual(0f, boss.DappleOpacity(entrance + 34f));
            Assert.AreEqual(0.5f, boss.DappleOpacity(entrance + 75f), 0.0001f);
            Assert.AreEqual(1f, boss.DappleOpacity(entrance + 116f));
            var tiles = Scenery.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "Dapple").OrderBy(r => r.transform.position.x).ToArray();
            Assert.AreEqual(0f, tiles.First().GetComponentInChildren<MeshFilter>().sharedMesh.colors[0].a);
            Assert.AreEqual(1f, tiles.Last().GetComponentInChildren<MeshFilter>().sharedMesh.colors[1].a);
            foreach (var tile in tiles)
            {
                var mesh = tile.GetComponentInChildren<MeshFilter>().sharedMesh;
                for (int i = 0; i < mesh.vertexCount; i++)
                    Assert.AreEqual(boss.DappleOpacity(tile.transform.TransformPoint(mesh.vertices[i]).x), mesh.colors[i].a, 0.0001f);
                Assert.IsFalse(tile.enabled);
                Assert.IsTrue(tile.GetComponentInChildren<MeshRenderer>().enabled);
            }
        }

        [UnityTest]
        public IEnumerator SacudirLaCopaCambiaShakeYVuelveASway()
        {
            var canopy = Scenery.Find("Canopy/ArenaCanopy").GetComponent<SpriteRenderer>();
            int leaves = Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "FallingLeaf");
            int spores = Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "FallingSpore");
            boss.ShakeCanopy(0.15f);
            Assert.IsTrue(boss.IsCanopyShaking);
            Assert.IsTrue(canopy.sprite.name.StartsWith("Canopy_Arena_Shake"));
            Assert.AreEqual(leaves + 70, Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "FallingLeaf"));
            Assert.AreEqual(spores + 30, Scenery.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "FallingSpore"));
            yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(boss.IsCanopyShaking);
            Assert.IsTrue(canopy.sprite.name.StartsWith("Canopy_Arena_Sway"));
        }

        [UnityTest]
        public IEnumerator LaCamaraAlejaSuavementeEnLaArenaYLaCopaSigueSoloLaAlturaDePantalla()
        {
            cameraRig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Core/CameraRig.prefab"));
            cameraRig.GetComponentInChildren<AudioListener>().enabled = false;
            var camera = cameraRig.GetComponentInChildren<Camera>();
            var virtualCamera = cameraRig.transform.Find("CinemachineCamera").GetComponent("CinemachineCamera");
            // El ensamblado de pruebas no referencia Cinemachine: inspección serializada sin cambiar su asmdef.
            float LensSize() => new SerializedObject(virtualCamera).FindProperty("Lens").FindPropertyRelative("OrthographicSize").floatValue;
            float normal = LensSize();
            var body = CreateWalker(boss.TestStop(2));
            var controller = walker.AddComponent<PlayerController>();
            controller.readInput = false;
            var target = new GameObject("CameraTarget").AddComponent<CameraLookAhead>();
            target.transform.SetParent(room.transform);
            target.Configure(controller, 0f, 0f);
            virtualCamera.GetType().GetProperty("Follow").SetValue(virtualCamera, target.transform);
            yield return null;
            float normalView = camera.orthographicSize;
            body.position = boss.TestStop(3);
            walker.transform.position = body.position;
            yield return new WaitForSeconds(0.2f);
            Assert.Greater(LensSize(), normalView);
            Assert.Less(LensSize(), 11.25f);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(11.25f, LensSize(), 0.01f);
            var canopy = Scenery.Find("Canopy/ArenaCanopy");
            Assert.AreEqual(boss.ArenaCenter.x, canopy.position.x, 0.001f);
            Assert.AreEqual(camera.transform.position.y + camera.orthographicSize + 10f / 16f, canopy.position.y, 0.01f);
            var pool = Scenery.Find("LightPool1");
            var poolPosition = pool.position;
            body.position += Vector2.up;
            walker.transform.position = body.position;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(poolPosition, pool.position, "El charco de luz se queda en el suelo.");
            body.position = boss.TestStop(2);
            walker.transform.position = body.position;
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(normal, LensSize(), 0.01f);
            Assert.AreEqual(0f, target.FocusWeight);
        }

        [UnityTest]
        public IEnumerator EsInvulnerableYNoTieneIAArmasContactoNiBarraActiva()
        {
            int health = boss.Health.Current;
            Assert.AreEqual(1000, boss.Health.Max);
            boss.Health.TakeDamage(100);
            boss.BeginFight();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(boss.Health.IsInvulnerable);
            Assert.AreEqual(health, boss.Health.Current);
            Assert.IsNull(boss.GetComponent<EnemyAI>());
            Assert.IsNull(boss.GetComponent<Weapon>());
            Assert.IsNull(boss.GetComponent<ContactDamage>());
            Assert.AreNotSame(boss, ActiveBoss.Current);
            Assert.AreEqual(BossBarStyle.Normal, boss.BarStyle);
        }

        [Test]
        public void SetArenaFuncionaAntesDeAwakeConElJefeDesactivado()
        {
            var inactiveRoom = new GameObject("SalaInactiva");
            inactiveRoom.SetActive(false);
            try
            {
                var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), inactiveRoom.transform);
                var inactive = instance.GetComponent<LepidodendronBoss>();
                inactive.SetArena(inactiveRoom.transform, new Vector2(158f, 30f));
                Assert.IsNotNull(instance.transform.Find("Scenery/Trunk"));
                inactiveRoom.SetActive(true);
                Assert.IsTrue(inactive.Health.IsInvulnerable);
            }
            finally { Object.DestroyImmediate(inactiveRoom); }
        }
    }
}
#endif
