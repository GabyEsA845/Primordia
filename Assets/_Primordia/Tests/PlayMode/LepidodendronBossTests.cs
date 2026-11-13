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
            RunInventory.Reset();
            room = new GameObject("SalaLepidodendron");
            room.transform.position = new Vector2(200f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Ejecuta Primordia > Boss > Crear Lepidodendron y escena de prueba.");
            boss = Object.Instantiate(prefab, room.transform).GetComponent<LepidodendronBoss>();
            boss.SetArena(room.transform, new Vector2(158f, 30f));
            // La mayorÃ­a de comprobaciones no necesitan enemigos activos. Las pruebas de oleadas reponen su reserva.
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("waveEnemies").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            RunInventory.Reset();
            Time.timeScale = 1f;
            if (walker != null) Object.DestroyImmediate(walker);
            if (room != null) Object.DestroyImmediate(room);
            if (cameraRig != null) Object.DestroyImmediate(cameraRig);
        }

        Transform Scenery => boss.transform.Find("Scenery");

        [Test]
        public void L5SkinUsaSaviaCabezaYLosTresNudosReales()
        {
            var skin = boss.GetComponent<Primordia.UI.BossBarAppearance>().Skin;
            Assert.IsTrue(skin.BottomAnchored);
            Assert.AreEqual(new Vector2(320, 44), skin.Frame.rect.size);
            Assert.AreEqual(new RectInt(46, 23, 260, 8), skin.Window);
            Assert.AreEqual(3, boss.PhaseThresholds.Length);
            Assert.That(skin.Phase(boss.PhaseIndex).fill[0].name, Does.Contain("Fill_Sap_"));
            Assert.That(skin.Phase(0).eyes[0].name, Does.Contain("Head_Calm"));
            boss.Walls[0].Health.TakeDamage(10);
            Assert.AreEqual(0, boss.PhaseIndex);
            boss.Walls[0].Health.Kill();
            Assert.AreEqual(1, boss.PhaseIndex);
            boss.TestFinalPhase();
            Assert.That(skin.Phase(boss.PhaseIndex).fill[0].name, Does.Contain("SapGold"));
            Assert.That(skin.Phase(boss.PhaseIndex).eyes[0].name, Does.Contain("Head_Final"));
        }

        [Test]
        public void L5ConosDeBarraReflejanArmaduraCarbonizadoExpuestoYRoto()
        {
            var cone = GroundCone();
            Assert.AreEqual(0, boss.BarConeState(0));
            cone.Ignite();
            Assert.AreEqual(1, boss.BarConeState(0));
            cone.Health.TakeDamage(100);
            Assert.AreEqual(2, boss.BarConeState(0));
            cone.Health.Kill();
            Assert.AreEqual(3, boss.BarConeState(0));
            Assert.AreEqual(0, boss.BarConeState(1));
        }

        [UnityTest]
        public IEnumerator L5PresentacionBloqueaJugadorYCombateHastaVolver()
        {
            CreateWalker(boss.TestStop(0));
            var controller = walker.AddComponent<PlayerController>();
            controller.readInput = false;
            var target = new GameObject("CameraTarget").AddComponent<CameraLookAhead>();
            target.transform.SetParent(room.transform);
            target.Configure(controller, 0f, 0f);
            var health = walker.GetComponent<Health>();
            var body = walker.GetComponent<Rigidbody2D>();
            var constraints = body.constraints;
            boss.ReplayPresentation();
            Assert.IsTrue(boss.IntroPlaying);
            Assert.IsFalse(boss.FightReady);
            Assert.IsFalse(health.TakeDamage(1));
            Assert.AreEqual(RigidbodyConstraints2D.FreezeAll, body.constraints);
            yield return new WaitForSeconds(4.9f);
            Assert.IsTrue(boss.IntroPlayed);
            Assert.IsFalse(boss.IntroPlaying);
            Assert.IsTrue(boss.FightReady);
            Assert.AreEqual(0f, target.FocusWeight);
            Assert.AreEqual(constraints, body.constraints);
            boss.enabled = false;
            boss.enabled = true;
            yield return null;
            Assert.IsFalse(boss.IntroPlaying);
        }

        [UnityTest]
        public IEnumerator L5DesplomeDuraConProteccionYDejaTroncoColisionYAnclaSinRecompensa()
        {
            CreateWalker(boss.TestStop(3));
            walker.AddComponent<PlayerController>().readInput = false;
            var health = walker.GetComponent<Health>();
            int completed = 0, died = 0;
            boss.Collapsed += () => completed++;
            boss.Health.Died += () => died++;
            boss.TestEndingAction(0);
            Assert.IsTrue(boss.Collapsing);
            Assert.IsFalse(health.TakeDamage(1));
            yield return new WaitForSeconds(6f);
            Assert.IsTrue(boss.Collapsing);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(boss.HasCollapsed);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(0, died);
            Assert.AreEqual(0, boss.Health.Current);
            Assert.IsNotNull(Scenery.Find("FallenTrunk").GetComponent<PolygonCollider2D>());
            Assert.AreEqual(4, Scenery.Find("FallenTrunk").GetComponent<PolygonCollider2D>().points.Length);
            Assert.IsNotNull(boss.StumpCrack);
            Assert.IsNotNull(Scenery.Find("GoldColumn"));
            Assert.IsNull(ActiveBoss.Current);
            Assert.IsEmpty(room.GetComponentsInChildren<Primordia.Mutations.MutationPickup>());
            Assert.IsFalse(health.IsInvulnerable);
        }

        [UnityTest]
        public IEnumerator L5DestruirDuranteDesplomeLiberaJugadorYCamara()
        {
            CreateWalker(boss.TestStop(3));
            var controller = walker.AddComponent<PlayerController>(); controller.readInput = false;
            var target = new GameObject("Target").AddComponent<CameraLookAhead>();
            cameraRig = target.gameObject;
            target.Configure(controller);
            var body = walker.GetComponent<Rigidbody2D>();
            var constraints = body.constraints;
            boss.TestEndingAction(0);
            yield return new WaitForSeconds(0.1f);
            Object.DestroyImmediate(room);
            Assert.IsFalse(walker.GetComponent<Health>().IsInvulnerable);
            Assert.AreEqual(constraints, body.constraints);
            Assert.AreEqual(0, target.FocusWeight);
            Assert.IsNull(ActiveBoss.Current);
        }

        [Test]
        public void L5AtajosDeVidaMantienenConosYBarraSinDesplomePrematuro()
        {
            boss.TestEndingAction(2);
            Assert.AreEqual(150, boss.Health.Current);
            boss.TestEndingAction(3);
            Assert.AreEqual(6, boss.Health.Current);
            Assert.IsFalse(boss.Collapsing);
            Assert.AreEqual(6, boss.Cones.Sum(c => c.Health.Current));
        }

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
                Assert.AreEqual(i + 2, wall.Cast<Transform>().Count(t => t.name.StartsWith("Knot")));
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
            // El ensamblado de pruebas no referencia Cinemachine: inspecciÃ³n serializada sin cambiar su asmdef.
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
        public IEnumerator EsInvulnerableYNoTieneIAArmasContactoYUsaBarraEstandar()
        {
            int health = boss.Health.Current;
            Assert.AreEqual(600, boss.Health.Max);
            boss.Health.TakeDamage(100);
            boss.BeginFight();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(boss.Health.IsInvulnerable);
            Assert.AreEqual(health, boss.Health.Current);
            Assert.IsNull(boss.GetComponent<EnemyAI>());
            Assert.IsNull(boss.GetComponent<Weapon>());
            Assert.IsNull(boss.GetComponent<ContactDamage>());
            Assert.AreSame(boss, ActiveBoss.Current);
            Assert.AreEqual(BossBarStyle.Normal, boss.BarStyle);
        }

        [Test]
        public void LasParedesTienenUnaSolaVidaYRestanSoloLoAbsorbidoAlJefe()
        {
            Assert.AreEqual(600, boss.Health.Current);
            foreach (var wall in boss.Walls)
            {
                Assert.AreEqual(150, wall.Health.Current);
                Assert.AreEqual(1, wall.GetComponentsInChildren<Health>().Length);
                foreach (var collider in wall.GetComponentsInChildren<Collider2D>())
                    Assert.AreSame(wall.Health, collider.GetComponentInParent<Health>());
            }
            var first = boss.Walls[0];
            first.Health.TakeDamage(25);
            Assert.AreEqual(125, first.Health.Current);
            Assert.AreEqual(575, boss.Health.Current);
            first.Health.TakeDamage(1000);
            Assert.AreEqual(450, boss.Health.Current, "Nunca resta el sobre-daÃ±o.");
            Assert.IsFalse(boss.Health.TakeDamage(100));
            Assert.AreEqual(450, boss.Health.Current);
        }

        [Test]
        public void D0D1D2D3YHurtSiguenLaVidaRestante()
        {
            var wall = boss.Walls[0];
            Assert.AreEqual(0, wall.DamageState);
            wall.Health.TakeDamage(38);
            Assert.AreEqual(1, wall.DamageState);
            wall.Health.TakeDamage(37);
            Assert.AreEqual(2, wall.DamageState);
            wall.Health.TakeDamage(38);
            Assert.AreEqual(3, wall.DamageState);
            Assert.IsTrue(wall.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Lepido_Wall1_D3"));
            Assert.IsTrue(wall.transform.Find("Hurt").GetComponent<SpriteRenderer>().sprite.name.StartsWith("Lepido_Wall1_Hurt_D3"));
        }

        [UnityTest]
        public IEnumerator ElNudoDuplicaRevientaA22SinMultiplicarYDejaDeDuplicar()
        {
            var wall = boss.Walls[0];
            var point = wall.KnotPosition(0);
            wall.Health.TakeDamage(11, DamageKind.Hit, DamageSource.Unknown, point);
            Assert.AreEqual(128, wall.Health.Current);
            Assert.AreEqual(11, wall.KnotRemaining(0));
            wall.Health.TakeDamage(11, DamageKind.Hit, DamageSource.Unknown, point);
            Assert.AreEqual(106, wall.Health.Current);
            Assert.AreEqual(0, wall.KnotRemaining(0));
            Assert.IsNotNull(wall.transform.Find("KnotBurst"));
            yield return new WaitForSeconds(0.5f);
            Assert.IsNull(wall.transform.Find("KnotBurst"));
            Assert.IsTrue(wall.transform.Find("Knot1").GetComponent<SpriteRenderer>().sprite.name.Contains("Wound"));
            wall.Health.TakeDamage(5, DamageKind.Hit, DamageSource.Unknown, point);
            Assert.AreEqual(101, wall.Health.Current);
        }

        [Test]
        public void SinPuntoNoUsaUnImpactoAnteriorNiInventaUnNudo()
        {
            var wall = boss.Walls[0];
            var point = wall.KnotPosition(0);
            wall.Health.TakeDamage(2, DamageKind.Hit, DamageSource.Unknown, point);
            Assert.AreEqual(point, wall.Health.LastImpactPoint.Value);
            int before = wall.Health.Current;
            wall.Health.TakeDamage(3);
            Assert.IsNull(wall.Health.LastImpactPoint);
            Assert.AreEqual(before - 3, wall.Health.Current);
            Assert.AreEqual(20, wall.KnotRemaining(0));
        }

        [Test]
        public void UnMordiscoRealAlNudoCuentaSoloUnaVezPorPared()
        {
            var wall = boss.Walls[0];
            var point = wall.KnotPosition(0);
            CreateWalker(new Vector2(wall.transform.position.x - wall.BodyCollider.size.x / 2f - 1f, point.y));
            var weapon = walker.AddComponent<Weapon>();
            var attack = ScriptableObject.CreateInstance<AttackData>();
            try
            {
                attack.Configure(null, 1f, 5, 1f, 1f);
                attack.ConfigureMelee(3f, 180f, null);
                Physics2D.SyncTransforms();
                weapon.Fire(Vector2.right, attack);
                Assert.AreEqual(140, wall.Health.Current);
                Assert.AreEqual(590, boss.Health.Current);
                Assert.AreEqual(17f, wall.KnotRemaining(0));
                Assert.IsNotNull(wall.Health.LastImpactPoint);
            }
            finally { Object.DestroyImmediate(attack); }
        }

        [UnityTest]
        public IEnumerator ElEstallidoRealCuentaUnaVezYTienePuntoDeImpacto()
        {
            var wall = boss.Walls[0];
            Scenery.Find("Wall1Supplies").gameObject.SetActive(false); // AÃ­sla el golpe del estallido, sin cadenas de metano.
            var position = new Vector2(wall.transform.position.x - wall.BodyCollider.size.x / 2f - 0.4f, wall.KnotPosition(0).y);
            PoisonSpitBurst.Spawn(position, 2, 2f, 0f, null, Team.Enemy);
            yield return new WaitForSeconds(0.2f); // DespuÃ©s del impacto y antes del primer tick de veneno (0,5 s).
            Assert.AreEqual(146, wall.Health.Current);
            Assert.AreEqual(596, boss.Health.Current);
            Assert.IsNotNull(wall.Health.LastImpactPoint);
        }

        [UnityTest]
        public IEnumerator LasParedesCompartenLosTopesDelJefe()
        {
            boss.Walls[0].Health.TakeDamage(50, DamageKind.Hit, DamageSource.Jet);
            boss.Walls[1].Health.TakeDamage(50, DamageKind.Hit, DamageSource.Jet);
            Assert.AreEqual(145, boss.Walls[0].Health.Current);
            Assert.AreEqual(150, boss.Walls[1].Health.Current);
            yield return new WaitForSeconds(1.1f);
            boss.Walls[1].Health.TakeDamage(50, DamageKind.Hit, DamageSource.Jet);
            Assert.AreEqual(145, boss.Walls[1].Health.Current);
        }

        [UnityTest]
        public IEnumerator ElFuegoCubreEnTresSegundosYRespetaSuDanoPorSegundoSinTopeBurn()
        {
            var wall = boss.Walls[0];
            boss.GetComponent<BossDamageLimiter>().Configure(new[] { new BossDamageLimiter.Cap { source = DamageSource.Burn, perSecond = 0f } });
            float start = Time.time;
            wall.IgniteAt(3f);
            yield return new WaitForSeconds(1.05f);
            Assert.AreEqual(150 - Mathf.FloorToInt((Time.time - start) * LepidodendronWall.FireDamagePerSecond), wall.Health.Current, 2f);
            Assert.IsFalse(wall.FullyCharred);
            var band = wall.transform.Find("CharredBand").GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(4, band.colors.Length);
            Assert.IsTrue(band.colors.All(c => c == Color.white));
            var north = wall.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "FireNorth");
            var south = wall.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "FireSouth");
            Assert.Greater(north.transform.localPosition.y, 3f);
            Assert.Less(south.transform.localPosition.y, 3f);
            yield return new WaitForSeconds(2.15f);
            Assert.IsTrue(wall.FullyCharred);
            Assert.AreEqual(150 - Mathf.FloorToInt((Time.time - start) * LepidodendronWall.FireDamagePerSecond), wall.Health.Current, 2f);
            int before = wall.Health.Current;
            wall.Health.TakeDamage(5); // Cuerpo, sin nudo.
            Assert.AreEqual(before - 10, wall.Health.Current);
        }

        [UnityTest]
        public IEnumerator CarbonizadoYNudoSeMultiplicanYLaHeridaCambiaAQuemada()
        {
            var wall = boss.Walls[0];
            wall.IgniteAt(9f);
            yield return new WaitForSeconds(3.15f);
            int before = wall.Health.Current;
            wall.Health.TakeDamage(5, DamageKind.Hit, DamageSource.Unknown, wall.KnotPosition(0));
            Assert.AreEqual(before - 20, wall.Health.Current);
            wall.Health.TakeDamage(17, DamageKind.Hit, DamageSource.Unknown, wall.KnotPosition(0));
            Assert.AreEqual(0, wall.KnotRemaining(0));
            yield return null;
            Assert.IsTrue(wall.transform.Find("Knot1").GetComponent<SpriteRenderer>().sprite.name.Contains("WoundCharred"));
        }

        [UnityTest]
        public IEnumerator CaerLiberaElPasoYUsaRetractScarOCrumbleAshScar()
        {
            var first = boss.Walls[0];
            first.Health.Kill();
            Assert.IsTrue(first.Fallen);
            Assert.IsTrue(first.GetComponentsInChildren<Collider2D>().All(c => !c.enabled));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(first.GetComponent<SpriteRenderer>().sprite.name.Contains("Retract"));
            yield return new WaitForSeconds(0.75f);
            Assert.IsTrue(first.GetComponent<SpriteRenderer>().sprite.name.Contains("Scar"));
            Assert.IsFalse(first.UsesAshScar);
            var second = boss.Walls[1];
            second.IgniteAt(9f);
            yield return new WaitForSeconds(3.15f);
            second.Health.Kill();
            Assert.IsTrue(second.UsesAshScar);
            Assert.IsTrue(second.GetComponent<SpriteRenderer>().sprite.name.Contains("Crumble"));
            Assert.IsTrue(second.GetComponentsInChildren<Collider2D>().All(c => !c.enabled));
            yield return new WaitForSeconds(0.85f);
            Assert.IsTrue(second.GetComponent<SpriteRenderer>().sprite.name.Contains("AshScar"));
        }

        [Test]
        public void TrasLasTresParedesQuedan150YElAvisoFinalSoloSeLanzaUnaVez()
        {
            int ready = 0;
            boss.FinalPhaseBecameReady += () => ready++;
            foreach (var wall in boss.Walls) wall.Health.Kill();
            Assert.AreEqual(150, boss.Health.Current);
            Assert.IsTrue(boss.FinalPhaseReady);
            Assert.AreEqual(1, ready);
            boss.Walls[2].Health.Kill();
            Assert.AreEqual(1, ready);
        }

        LepidodendronCone GroundCone(int index = 0)
        {
            boss.TestFinalPhase();
            var cone = boss.Cones[index];
            Assert.IsTrue(cone.BeginDrop(LepidodendronCone.WarningSeconds, Vector2.zero, Time.time));
            cone.Tick(Time.time + LepidodendronCone.WarningSeconds + LepidodendronCone.ShadowSeconds + LepidodendronCone.DescentSeconds + 0.001f);
            Assert.AreEqual(LepidodendronCone.Phase.Grounded, cone.State);
            return cone;
        }

        [Test]
        public void L3EmpiezaConTresConosDe30Y50YColgadosNoRecibenNiAtacan()
        {
            boss.TestFinalPhase();
            Assert.AreEqual(150, boss.Health.Current);
            Assert.AreEqual(3, boss.Cones.Count);
            foreach (var cone in boss.Cones)
            {
                Assert.AreEqual(30, cone.ArmorRemaining);
                Assert.AreEqual(50, cone.Health.Current);
                Assert.AreEqual(1, cone.GetComponentsInChildren<Health>().Length);
                Assert.IsFalse(cone.Health.TakeDamage(100));
                cone.Ignite();
                Assert.IsFalse(cone.Charred);
                Assert.IsTrue(cone.GetComponentsInChildren<Collider2D>().All(c => !c.enabled));
                Assert.AreEqual(30, cone.ArmorRemaining);
                Assert.AreEqual(50, cone.Health.Current);
            }
        }

        [Test]
        public void L3AvisoDe1500MsFijaPuntoAlEmpezarLos720MsDeSombra()
        {
            boss.TestFinalPhase();
            CreateWalker(boss.ArenaCenter);
            boss.ConePlayer = walker.transform;
            var cone = boss.Cones[0];
            float start = Time.time;
            cone.BeginDrop(1.5f, Vector2.zero, start);
            walker.transform.position += Vector3.left;
            cone.Tick(start + 1.49f);
            Assert.AreEqual((Vector2)walker.transform.position, cone.LandingPoint);
            Assert.AreEqual(LepidodendronCone.Phase.Warning, cone.State);
            cone.Tick(start + 1.501f);
            Assert.AreEqual(LepidodendronCone.Phase.Shadow, cone.State);
            var fixedPoint = cone.LandingPoint;
            walker.transform.position += Vector3.right * 10f;
            cone.Tick(start + 2.219f);
            Assert.AreEqual(fixedPoint, cone.LandingPoint);
            Assert.AreEqual(LepidodendronCone.Phase.Shadow, cone.State);
            cone.Tick(start + 2.221f);
            Assert.AreEqual(LepidodendronCone.Phase.Descending, cone.State);
            Assert.AreEqual(fixedPoint, cone.LandingPoint);
        }

        [TestCase(0f, 2)]
        [TestCase(2.2f, 0)]
        public void L3GolpeSoloHaceDosUnaVezEnCuadroCeroYDentroDelRadio(float distance, int damage)
        {
            boss.TestFinalPhase();
            CreateWalker(boss.ArenaCenter + Vector2.right * distance);
            var health = walker.AddComponent<Health>();
            walker.AddComponent<BoxCollider2D>(); // Dos colisionadores no duplican el golpe.
            int before = health.Current;
            Physics2D.SyncTransforms();
            var cone = boss.Cones[0];
            float start = Time.time;
            cone.BeginDrop(1.5f, Vector2.zero, start);
            cone.Tick(start + 2.56f);
            Assert.AreEqual(before, health.Current);
            cone.Tick(start + 2.571f);
            Assert.AreEqual(before - damage, health.Current);
            Assert.IsNotNull(cone.transform.Find("Fx_ConeSlam"));
            cone.Tick(start + 2.7f);
            cone.Tick(start + 3f);
            Assert.AreEqual(before - damage, health.Current);
        }

        [Test]
        public void L3ArmaduraAcumuladaVentanaDeOchoSegundosYSubidaSinCuracion()
        {
            float start = Time.time;
            var cone = GroundCone();
            cone.Health.TakeDamage(10);
            Assert.AreEqual(1, cone.ArmorState);
            cone.Health.TakeDamage(10);
            Assert.AreEqual(2, cone.ArmorState);
            Assert.AreEqual(150, boss.Health.Current);
            cone.Tick(start + 10.56f);
            Assert.AreEqual(LepidodendronCone.Phase.Grounded, cone.State);
            cone.Tick(start + 10.571f);
            Assert.AreEqual(LepidodendronCone.Phase.Rising, cone.State);
            Assert.IsFalse(cone.Health.TakeDamage(100));
            cone.Tick(start + 11.571f);
            Assert.AreEqual(LepidodendronCone.Phase.Hanging, cone.State);
            cone.BeginDrop(1.5f, Vector2.zero, start + 15f);
            cone.Tick(start + 17.571f);
            Assert.AreEqual(10, cone.ArmorRemaining);
            cone.Health.TakeDamage(10);
            Assert.AreEqual(0, cone.ArmorRemaining);
            Assert.AreEqual(50, cone.Health.Current);
            cone.Health.TakeDamage(10); // Ya vulnerable desde el cuadro 0 de Shatter.
            Assert.AreEqual(40, cone.Health.Current);
            Assert.AreEqual(140, boss.Health.Current);
        }

        [Test]
        public void L3FuegoCarbonizaSinDanoDuplicaResistenciaYNoRebrota()
        {
            float start = Time.time;
            var cone = GroundCone();
            Assert.AreEqual(1, Flammables.IgniteNear(cone.Position + Vector2.up * 2.5f, 0.01f));
            Assert.IsTrue(cone.Charred);
            Assert.AreEqual(30, cone.ArmorRemaining);
            cone.Health.TakeDamage(9, DamageKind.Tick, DamageSource.Burn);
            Assert.AreEqual(30, cone.ArmorRemaining);
            Assert.AreEqual(150, boss.Health.Current);
            cone.Health.TakeDamage(10);
            Assert.AreEqual(10, cone.ArmorRemaining);
            cone.Tick(start + 11.571f);
            cone.BeginDrop(1.5f, Vector2.zero, start + 15f);
            cone.Tick(start + 17.571f);
            Assert.IsTrue(cone.Charred);
            Assert.AreEqual(10, cone.ArmorRemaining);
            Assert.IsTrue(cone.transform.Find("Body").GetComponent<SpriteRenderer>().sprite.name.StartsWith("Lepido_Cone_ArmorCharred2"));
            cone.Health.TakeDamage(5);
            cone.Health.TakeDamage(20, DamageKind.Tick, DamageSource.Burn);
            Assert.AreEqual(50, cone.Health.Current);
            Assert.IsFalse(cone.CanIgnite);
        }

        [Test]
        public void L3LimiteDeFuenteCompartidoEntreConosYTronco()
        {
            var first = GroundCone(0);
            var second = GroundCone(1);
            first.Health.TakeDamage(30);
            second.Health.TakeDamage(30);
            first.Health.TakeDamage(4, DamageKind.Hit, DamageSource.Jet);
            second.Health.TakeDamage(4, DamageKind.Hit, DamageSource.Jet);
            boss.Health.TakeDamage(100, DamageKind.Hit, DamageSource.Jet);
            Assert.AreEqual(145, boss.Health.Current);
            Assert.AreEqual(46, first.Health.Current);
            Assert.AreEqual(49, second.Health.Current);
        }

        [Test]
        public void L3TroncoAplicaVeintePorCientoAlMasSanoIgnorandoArmaduraYAltura()
        {
            boss.TestFinalPhase();
            boss.Health.TakeDamage(50);
            Assert.AreEqual(140, boss.Health.Current);
            Assert.AreEqual(40, boss.Cones[0].Health.Current);
            boss.Health.TakeDamage(100);
            Assert.AreEqual(30, boss.Cones[1].Health.Current);
            Assert.AreEqual(120, boss.Health.Current);
            Assert.IsTrue(boss.Cones.All(c => c.ArmorRemaining == 30 && c.State == LepidodendronCone.Phase.Hanging));
        }

        [Test]
        public void L3TresRoturasDejanCeroEmitenUnaVezSinDiedNiDestruirSala()
        {
            int completed = 0, deaths = 0;
            boss.ConesDestroyed += () => completed++;
            boss.Health.Died += () => deaths++;
            for (int i = 0; i < 3; i++)
            {
                var cone = GroundCone(i);
                cone.Health.TakeDamage(30);
                cone.Health.TakeDamage(1000);
                Assert.AreEqual(LepidodendronCone.Phase.Husk, cone.State);
                Assert.IsFalse(cone.BeginDrop(1.5f, Vector2.zero, Time.time));
            }
            Assert.AreEqual(0, boss.Health.Current);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(0, deaths);
            Assert.IsFalse(boss.Health.IsDead);
            Assert.IsTrue(room.activeSelf);
            Assert.AreSame(boss, ActiveBoss.Current);
            boss.CheckConesDestroyed();
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void L3PausaCuentaDesdeFinDeSubidaYTrasRoturaPermiteDos()
        {
            float start = Time.time;
            var cone = GroundCone();
            Assert.AreEqual(4f, boss.CurrentConePause);
            Assert.AreEqual(1, boss.ConeConcurrency);
            cone.Tick(start + 11.571f);
            boss.UpdateCones(start + 15.56f);
            Assert.IsTrue(boss.Cones.All(c => c.Available));
            boss.UpdateCones(start + 15.571f);
            Assert.AreEqual(1, boss.Cones.Count(c => !c.Available));
            cone.Health.Kill();
            Assert.AreEqual(3f, boss.CurrentConePause);
            Assert.AreEqual(2, boss.ConeConcurrency);
            boss.UpdateCones(Time.time + 3.01f);
            Assert.AreEqual(2, boss.Cones.Count(c => !c.Broken && !c.Available));
        }

        [TestCase(-100f)]
        [TestCase(100f)]
        public void L3MetanoLejanoNoActuaYCercanoBajaTresSeparadosDentroDeArena(float edge)
        {
            boss.TestFinalPhase();
            CreateWalker(boss.ArenaCenter + Vector2.right * edge);
            boss.ConePlayer = walker.transform;
            boss.TriggerConeMethane(boss.Foot + Vector2.right * 5.01f, Time.time);
            Assert.IsTrue(boss.Cones.All(c => c.Available));
            boss.TriggerConeMethane(boss.Foot + Vector2.right * 5f, Time.time);
            Assert.IsTrue(boss.IsCanopyShaking);
            foreach (var cone in boss.Cones)
            {
                Assert.AreEqual(1f, cone.CurrentWarningSeconds);
                cone.Tick(Time.time + 1.001f);
                Assert.AreEqual(LepidodendronCone.Phase.Shadow, cone.State);
                Assert.AreEqual(cone.LandingPoint, boss.ClampConePoint(cone.LandingPoint));
            }
            for (int i = 1; i < 3; i++)
                Assert.GreaterOrEqual(Vector2.Distance(boss.Cones[i - 1].LandingPoint, boss.Cones[i].LandingPoint), 3f);
        }

        [Test]
        public void L3OdresRealesAvisanUnaVezYNoSeReponen()
        {
            boss.TestFinalPhase();
            var sacs = Scenery.GetComponentsInChildren<MethaneSac>().Where(s => s.transform.parent == Scenery).ToArray();
            Assert.AreEqual(3, sacs.Length);
            foreach (var sac in sacs) Assert.That(Vector2.Distance(sac.transform.position, boss.Foot), Is.InRange(7f, 8f));
            int notifications = 0;
            void Count(MethaneSac sac) => notifications++;
            MethaneSac.BurstStarted += Count;
            try
            {
                sacs[0].transform.position = boss.Foot + Vector2.down * 3f;
                sacs[0].Ignite();
                sacs[0].Ignite();
                Assert.AreEqual(1, notifications);
                Assert.IsTrue(boss.Cones.All(c => !c.Available));
            }
            finally { MethaneSac.BurstStarted -= Count; }
        }

        [Test]
        public void L3BaseSolidaTranslucidezYHuskPersistente()
        {
            var cone = GroundCone();
            CreateWalker(cone.LandingPoint + Vector2.up);
            boss.ConePlayer = walker.transform;
            cone.Tick(Time.time + 3f);
            var renderer = cone.transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.AreEqual(0.5f, renderer.color.a);
            var solid = cone.GetComponent<BoxCollider2D>();
            Assert.LessOrEqual(solid.size.y, 1f);
            Assert.IsTrue(solid.enabled);
            cone.Health.Kill();
            cone.Tick(Time.time + 3f);
            Assert.IsTrue(renderer.sprite.name.StartsWith("Lepido_Cone_Husk"));
            Assert.IsTrue(solid.enabled);
            Assert.IsFalse(cone.transform.Find("ConeTarget").GetComponent<Collider2D>().enabled);
            walker.transform.position = cone.LandingPoint + Vector2.down;
            cone.Tick(Time.time + 3f);
            Assert.AreEqual(1f, renderer.color.a);
        }

        [UnityTest]
        public IEnumerator L3DestruirSalaLimpiaConosEfectosYAmbosPuntosInflamables()
        {
            var cone = GroundCone();
            var basePoint = cone.Position;
            cone.Health.TakeDamage(10);
            var owned = cone.GetComponentsInChildren<Transform>().ToArray();
            Assert.AreEqual(1, Flammables.Near(basePoint, 0.01f).Count);
            Assert.AreEqual(1, Flammables.Near(basePoint + Vector2.up * 2.5f, 0.01f).Count);
            Object.Destroy(room);
            yield return null;
            yield return null;
            Assert.IsTrue(owned.All(o => o == null));
            Assert.AreEqual(0, Flammables.Near(basePoint, 3f).Count);
            Assert.IsFalse(Resources.FindObjectsOfTypeAll<Mesh>().Any(m => m.name.StartsWith("Lepidodendron") && m.hideFlags == HideFlags.HideAndDontSave));
        }

        Health PrepareL4(bool final = true)
        {
            CreateWalker(boss.ArenaCenter);
            var health = walker.AddComponent<Health>();
            health.SetMax(100);
            health.ResetHealth();
            boss.ConePlayer = walker.transform;
            if (final) boss.TestFinalPhase();
            boss.TestAttackAction(4);
            return health;
        }

        void StartL4(LepidodendronBoss.AttackKind kind)
        {
            Assert.IsTrue(boss.QueueAttack(kind));
            boss.TickAttacks(0.001f);
        }

        [Test]
        public void L4LatigazoAvisaRespetaPuntaYHaceDosUnaVez()
        {
            var health = PrepareL4();
            walker.transform.position = boss.ArenaCenter + Vector2.left * 10f;
            Physics2D.SyncTransforms();
            StartL4(LepidodendronBoss.AttackKind.Whip);
            boss.TickAttacks(0.799f);
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(0.12f); // Cuadro 2: la punta todavÃ­a no llega.
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(0.23f); // Cuadro 6: llega hasta 214 px.
            Assert.AreEqual(98, health.Current);
            boss.TickAttacks(0.3f);
            boss.TickAttacks(0.25f);
            Assert.AreEqual(98, health.Current);
            Assert.IsTrue(boss.IsCanopyShaking);
        }

        [Test]
        public void L4LatigazoAlternaLadoYNoGolpeaFueraDeFranja()
        {
            var health = PrepareL4();
            StartL4(LepidodendronBoss.AttackKind.Whip);
            var first = Scenery.Find("L4_Whip").GetComponent<SpriteRenderer>();
            Assert.IsFalse(first.flipX);
            walker.transform.position += Vector3.up * 3f;
            Physics2D.SyncTransforms();
            boss.TickAttacks(1.5f);
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(3f);
            StartL4(LepidodendronBoss.AttackKind.Whip);
            Assert.IsTrue(Scenery.GetComponentsInChildren<SpriteRenderer>().Single(r => r.gameObject.activeSelf && r.name == "L4_Whip").flipX);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void L4CorroFijaElCentroYPermiteEsquivar(bool dodge)
        {
            var health = PrepareL4();
            Physics2D.SyncTransforms();
            StartL4(LepidodendronBoss.AttackKind.Spikes);
            if (dodge) walker.transform.position += Vector3.right * 4f;
            Physics2D.SyncTransforms();
            boss.TickAttacks(0.779f);
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(0.002f);
            Assert.AreEqual(dodge ? 100 : 98, health.Current);
            boss.TickAttacks(0.2f);
            Assert.AreEqual(dodge ? 100 : 98, health.Current);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void L4EsporaSoloGolpeaEnCuadroCeroYCerca(bool dodge)
        {
            var health = PrepareL4();
            boss.AddSpore(boss.ArenaCenter, 0.5f);
            boss.TickAttacks(0.1f);
            Assert.IsFalse(Scenery.Find("L4_Rain/Espora").GetComponent<SpriteRenderer>().enabled);
            boss.TickAttacks(0.4f);
            Assert.IsTrue(Scenery.Find("L4_Rain/Espora").GetComponent<SpriteRenderer>().enabled);
            if (dodge) walker.transform.position += Vector3.right * 4f;
            Physics2D.SyncTransforms();
            boss.TickAttacks(0.99f);
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(0.02f);
            Assert.AreEqual(dodge ? 100 : 99, health.Current);
            walker.transform.position = boss.ArenaCenter;
            Physics2D.SyncTransforms();
            boss.TickAttacks(0.07f);
            Assert.AreEqual(dodge ? 100 : 99, health.Current);
        }

        [Test]
        public void L4ColaSeparaAvisosIncluidaTodaLaLluvia()
        {
            PrepareL4();
            foreach (var kind in new[] { LepidodendronBoss.AttackKind.Rain, LepidodendronBoss.AttackKind.Whip,
                LepidodendronBoss.AttackKind.Spikes, LepidodendronBoss.AttackKind.Corners }) boss.QueueAttack(kind);
            boss.TickAttacks(0.001f);
            float previous = boss.LastAttackStart, warning = boss.AttackWarningUntil;
            int starts = 1;
            for (int i = 0; i < 600; i++)
            {
                boss.TickAttacks(0.01f);
                if (boss.LastAttackStart == previous) continue;
                Assert.GreaterOrEqual(boss.LastAttackStart - previous, LepidodendronBoss.AttackGap);
                Assert.GreaterOrEqual(boss.LastAttackStart, warning);
                previous = boss.LastAttackStart;
                warning = boss.AttackWarningUntil;
                starts++;
            }
            Assert.AreEqual(4, starts);
        }

        [TestCase(0, 1f)]
        [TestCase(1, 0.85f)]
        [TestCase(2, 0.70f)]
        [TestCase(3, 0.55f)]
        [TestCase(4, 0.50f)]
        public void L4CadenciaRestaQuincePuntosConSuelo(int broken, float expected)
            => Assert.AreEqual(expected, LepidodendronBoss.AttackCadenceScale(broken), 0.0001f);

        [Test]
        public void L4CadenciaEmpiezaADosSegundosYSePuedePausar()
        {
            PrepareL4();
            boss.TestAttackAction(4);
            boss.TickAttacks(1.99f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(0f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(0.02f);
            Assert.AreEqual(1, boss.ActiveAttackCount);
        }

        [Test]
        public void L4ConoRotoAcortaIntervalosSinAdelantarElArranque()
        {
            PrepareL4();
            boss.TestAttackAction(4);
            boss.Cones[0].Health.Kill();
            Assert.AreEqual(7f * 0.85f, boss.AttackInterval(LepidodendronBoss.AttackKind.Whip), 0.0001f);
            Assert.AreEqual(9f * 0.85f, boss.AttackInterval(LepidodendronBoss.AttackKind.Spikes), 0.0001f);
            Assert.AreEqual(12f * 0.85f, boss.AttackInterval(LepidodendronBoss.AttackKind.Rain), 0.0001f);
            Assert.AreEqual(20f * 0.85f, boss.AttackInterval(LepidodendronBoss.AttackKind.Corners), 0.0001f);
            boss.TickAttacks(1.9f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(0.11f);
            Assert.AreEqual(1, boss.ActiveAttackCount);
        }

        [Test]
        public void L4LluviaDeParedSoloActivaVivaYFueraDelCuerpo()
        {
            PrepareL4(false);
            boss.TestAttackAction(4);
            boss.TickAttacks(11f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            var wall = boss.Walls[0];
            boss.StartWallWaves(wall);
            boss.TickAttacks(0.01f);
            boss.TickAttacks(10f);
            Assert.That(boss.ActiveAttackCount, Is.InRange(3, 4));
            foreach (var r in Scenery.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "L4_Rain"))
            {
                float distance = Mathf.Abs(r.transform.position.x - wall.transform.position.x);
                Assert.That(distance, Is.InRange(2.5f, 4f));
                Assert.LessOrEqual(Mathf.Abs(r.transform.position.y - boss.ArenaCenter.y), 7f);
            }
            wall.Health.Kill();
            boss.TickAttacks(0.01f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(20f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
        }

        [Test]
        public void L4LluviaEsperaTresSegundosDesdeOleadaReciente()
        {
            PrepareL4(false);
            boss.TestAttackAction(4);
            var wall = boss.Walls[0];
            boss.StartWallWaves(wall);
            boss.TickAttacks(0.01f);
            boss.TickAttacks(9f);
            boss.NoteWallWave(wall);
            boss.TickAttacks(1.1f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(1.8f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            boss.TickAttacks(0.11f);
            Assert.That(boss.ActiveAttackCount, Is.InRange(3, 4));
        }

        [Test]
        public void L4EsquinasDosEspeciesYTopeSeis()
        {
            PrepareL4();
            EnableWaves();
            for (int i = 0; i < 4; i++)
            {
                StartL4(LepidodendronBoss.AttackKind.Corners);
                boss.TickAttacks(1.01f);
                boss.TickAttacks(0.21f);
                Assert.LessOrEqual(boss.LivingWaveEnemies, 6);
                if (i == 0)
                {
                    var enemies = room.GetComponentsInChildren<Enemy>().Where(e => e != boss.GetComponent<Enemy>()).ToArray();
                    Assert.AreEqual(2, enemies.Length);
                    Assert.AreNotEqual(enemies[0].name, enemies[1].name);
                }
            }
            Assert.AreEqual(6, boss.LivingWaveEnemies);
        }

        [UnityTest]
        public IEnumerator L4MuerteJugadorLimpiaAtaquesEnemigosYTextura()
        {
            var health = PrepareL4();
            EnableWaves();
            StartL4(LepidodendronBoss.AttackKind.Corners);
            var sprite = Scenery.Find("L4_Corners").GetComponent<SpriteRenderer>().sprite;
            var texture = sprite.texture;
            boss.TickAttacks(1.01f);
            boss.QueueAttack(LepidodendronBoss.AttackKind.Rain);
            health.Kill();
            boss.TickAttacks(0f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            Assert.AreEqual(0, boss.PendingAttackCount);
            Assert.AreEqual(0, boss.LivingWaveEnemies);
            yield return null;
            Assert.IsTrue(sprite == null);
            Assert.IsTrue(texture == null);
        }

        [UnityTest]
        public IEnumerator L4DestruirSalaLimpiaAvisosYTextura()
        {
            PrepareL4();
            EnableWaves();
            StartL4(LepidodendronBoss.AttackKind.Corners);
            var sprite = Scenery.Find("L4_Corners").GetComponent<SpriteRenderer>().sprite;
            var texture = sprite.texture;
            Object.Destroy(room);
            yield return null;
            yield return null;
            Assert.IsTrue(sprite == null);
            Assert.IsTrue(texture == null);
        }

        [Test]
        public void L4CambiarSalaCancelaColaYActivos()
        {
            PrepareL4();
            StartL4(LepidodendronBoss.AttackKind.Spikes);
            boss.QueueAttack(LepidodendronBoss.AttackKind.Rain);
            var other = new GameObject("OtraSala");
            other.AddComponent<BoxCollider2D>();
            try
            {
                boss.AttackRoomEntered(other.AddComponent<Room>());
                Assert.AreEqual(0, boss.ActiveAttackCount);
                Assert.AreEqual(0, boss.PendingAttackCount);
                Assert.IsFalse(boss.QueueAttack(LepidodendronBoss.AttackKind.Whip));
            }
            finally { Object.DestroyImmediate(other); }
        }

        [Test]
        public void L4FinDeConosLimpiaAtaquesAlComenzarDesplome()
        {
            var health = PrepareL4();
            StartL4(LepidodendronBoss.AttackKind.Spikes);
            boss.QueueAttack(LepidodendronBoss.AttackKind.Rain);
            boss.TestAttackAction(4); // Cadencia activa al terminar la fase.
            foreach (var cone in boss.Cones) cone.Health.Kill();
            Assert.IsTrue(boss.AllConesDestroyed);
            Assert.AreEqual(0, boss.PendingAttackCount);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            Physics2D.SyncTransforms();
            boss.TickAttacks(0.79f);
            Assert.AreEqual(100, health.Current);
            boss.TickAttacks(30f);
            Assert.AreEqual(0, boss.ActiveAttackCount);
            Assert.AreEqual(0, boss.PendingAttackCount);
            Assert.IsFalse(boss.QueueAttack(LepidodendronBoss.AttackKind.Whip));
        }

        [Test]
        public void L4EsporasNoDananParedesConosNiArmadura()
        {
            PrepareL4(false);
            foreach (var wall in boss.Walls) boss.AddSpore(wall.transform.position, 0f);
            boss.TickAttacks(1.01f);
            Assert.IsTrue(boss.Walls.All(w => w.Health.Current == 150));
            boss.TestFinalPhase();
            foreach (var cone in boss.Cones) boss.AddSpore(cone.Position, 0f);
            boss.TickAttacks(1.01f);
            Assert.IsTrue(boss.Cones.All(c => c.Health.Current == 50 && c.ArmorRemaining == 30));
        }

        void EnableWaves()
        {
            var enemies = new[] { "Calamites", "Toad", "Spider", "Beetle", "Dragonfly" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}Prefabs/Enemies/Enemy_{n}.prefab").GetComponent<Enemy>()).ToArray();
            boss.ConfigureWallCombat(boss.EncounterConfig, enemies, null, null);
        }

        [Test]
        public void LasAparicionesUsanElTramoOesteLibreYA5UnidadesDelJugador()
        {
            EnableWaves();
            Vector2 entrance = (Vector2)room.transform.position - Vector2.right * 78f;
            CreateWalker(entrance + new Vector2(2f, -7f));
            walker.AddComponent<PlayerController>().readInput = false;
            boss.Walls[0].Health.TakeDamage(1);
            var spawned = room.GetComponentsInChildren<Enemy>().Where(e => e != boss.GetComponent<Enemy>()).ToArray();
            Assert.AreEqual(3, spawned.Length);
            foreach (var enemy in spawned)
            {
                var p = (Vector2)enemy.transform.position;
                Assert.GreaterOrEqual(Vector2.Distance(p, walker.transform.position), 5f);
                Assert.That(p.x - entrance.x, Is.InRange(0f, 32.5f));
                Assert.That(p.y - entrance.y, Is.InRange(-9f, 9f));
                Assert.IsNotNull(enemy.GetComponent<EnemyBrain>());
                Assert.AreEqual(0, Physics2D.OverlapCircleAll(p, 0.6f).Count(c => !c.isTrigger && c.GetComponentInParent<Enemy>() != enemy));
            }
        }

        [Test]
        public void LosHijosDeLasOleadasTambienRespetanElTopeDeSeis()
        {
            EnableWaves();
            foreach (int i in Enumerable.Range(0, 5)) DummyEnemy(i);
            boss.Walls[0].Health.TakeDamage(1);
            var parent = room.GetComponentsInChildren<Enemy>().First(e => e.GetComponent<CalamitesBrain>() != null);
            var extra = DummyEnemy(6);
            parent.RegisterChild(extra);
            Assert.IsTrue(extra.Health.IsDead);
            Assert.IsFalse(extra.gameObject.activeSelf);
            Assert.AreEqual(6, boss.LivingWaveEnemies);
        }

        [Test]
        public void LaTerceraParedMezclaEnemigosConLibelulaDesdeLaPrimeraOleada()
        {
            EnableWaves();
            boss.Walls[2].Health.TakeDamage(1);
            var spawned = room.GetComponentsInChildren<Enemy>().Where(e => e != boss.GetComponent<Enemy>()).ToArray();
            Assert.AreEqual(4, spawned.Length);
            Assert.IsTrue(spawned.Any(e => e.GetComponent<DragonflyBrain>() != null));
            Assert.Greater(spawned.Select(e => e.Data).Distinct().Count(), 1);
        }

        Enemy DummyEnemy(int index)
        {
            var obj = new GameObject("Relleno");
            obj.transform.SetParent(room.transform);
            obj.transform.position = room.transform.position + new Vector3(0f, -30f - index, 0f);
            return obj.AddComponent<Enemy>();
        }

        [UnityTest]
        public IEnumerator ConCincoVivosSaleUnoYLosTresPendientesEntranAlMorirEnemigos()
        {
            EnableWaves();
            var fillers = Enumerable.Range(0, 5).Select(DummyEnemy).ToArray();
            var wall = boss.Walls[2]; // Primera oleada de 4.
            wall.Health.TakeDamage(1);
            Assert.AreEqual(6, boss.LivingWaveEnemies);
            Assert.AreEqual(3, boss.PendingFor(wall));
            for (int i = 0; i < 3; i++)
            {
                fillers[i].Health.Kill();
                yield return null;
                Assert.AreEqual(6, boss.LivingWaveEnemies);
                Assert.AreEqual(2 - i, boss.PendingFor(wall));
            }
            Assert.AreEqual(1, boss.WavesCompletedFor(wall));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, boss.WavesCompletedFor(wall));
            wall.Health.Kill();
            Assert.AreEqual(0, boss.PendingFor(wall));
            Assert.AreEqual(6, boss.LivingWaveEnemies, "Los ya aparecidos sobreviven a la pared.");
        }

        [UnityTest]
        public IEnumerator LasOleadasEsperan12DesdeElUltimoEnSalirYSeCancelanAlCaer()
        {
            EnableWaves();
            var fillers = Enumerable.Range(0, 5).Select(DummyEnemy).ToArray();
            var wall = boss.Walls[2];
            wall.Health.TakeDamage(1);
            yield return new WaitForSeconds(1f);
            foreach (var filler in fillers) filler.Health.Kill();
            yield return null;
            float completedAt = Time.time;
            Assert.AreEqual(1, boss.WavesCompletedFor(wall));
            foreach (var e in room.GetComponentsInChildren<Enemy>().Where(e => e != boss.GetComponent<Enemy>()).ToArray()) e.Health.Kill();
            yield return new WaitForSeconds(10.8f);
            Assert.AreEqual(0, boss.LivingWaveEnemies);
            yield return new WaitForSeconds(1.4f);
            Assert.GreaterOrEqual(Time.time - completedAt, 12f);
            Assert.AreEqual(4, boss.LivingWaveEnemies);
            wall.Health.Kill();
            Assert.AreEqual(0, boss.PendingFor(wall));
            foreach (var e in room.GetComponentsInChildren<Enemy>().Where(e => e != boss.GetComponent<Enemy>()).ToArray()) e.Health.Kill();
            yield return new WaitForSeconds(12.2f);
            Assert.AreEqual(0, boss.LivingWaveEnemies);
        }

        [Test]
        public void HayDosOdresYTresHelechosPorParedYLaSuperficieSePuedePrender()
        {
            foreach (var wall in boss.Walls)
            {
                var supplies = Scenery.Find($"Wall{wall.Index + 1}Supplies");
                Assert.AreEqual(2, supplies.GetComponentsInChildren<MethaneSac>().Length);
                Assert.AreEqual(3, supplies.GetComponentsInChildren<Fern>().Length);
                Assert.IsTrue(supplies.GetComponentsInChildren<Fern>().All(f => f.CanIgnite));
                var surface = wall.GetComponentInChildren<LepidodendronBossWallIgnition>();
                Assert.Greater(Flammables.IgniteNear(surface.Position, 0.01f), 0);
                Assert.IsTrue(wall.IsBurning);
            }
        }

        [UnityTest]
        public IEnumerator DestruirLaSalaLiberaMallasMaterialesYElRegistroInflamable()
        {
            foreach (var wall in boss.Walls) wall.IgniteAt(9f);
            var ownedMeshes = Resources.FindObjectsOfTypeAll<Mesh>().Where(m => m.name.StartsWith("Lepidodendron")).ToArray();
            var ownedMaterials = Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name.StartsWith("Lepidodendron")).ToArray();
            Assert.GreaterOrEqual(ownedMeshes.Length, 3);
            var point = boss.Walls[0].GetComponentInChildren<LepidodendronBossWallIgnition>().Position;
            Object.Destroy(room);
            yield return null;
            yield return null;
            Assert.IsTrue(ownedMeshes.All(m => m == null));
            Assert.IsTrue(ownedMaterials.All(m => m == null));
            Assert.AreEqual(0, Flammables.Near(point, 1f).Count);
            Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t => t.name.StartsWith("Lepidodendron") && t.hideFlags == HideFlags.HideAndDontSave));
            Assert.IsFalse(Resources.FindObjectsOfTypeAll<Sprite>().Any(s => s.name.StartsWith("Lepidodendron") && s.hideFlags == HideFlags.HideAndDontSave));
        }

        [UnityTest]
        public IEnumerator UnHelechoDeSuministroPrendeLaParedConElFuegoComun()
        {
            var previousKit = ReactionKit.Current;
            var existingFires = GroundFire.All.ToArray();
            ReactionKit.Current = boss.EncounterConfig.Reactions;
            try
            {
                Scenery.Find("Wall1Supplies").GetComponentInChildren<Fern>().Ignite();
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(boss.Walls[0].IsBurning);
            }
            finally
            {
                foreach (var fire in GroundFire.All.ToArray().Except(existingFires))
                    if (fire != null) Object.DestroyImmediate(fire.gameObject);
                ReactionKit.Current = previousKit;
            }
        }

        [UnityTest]
        public IEnumerator LaQuemaduraDeUnArmaPrendeLaBaseSinInventarImpactos()
        {
            var wall = boss.Walls[2]; // La mÃ¡s gruesa: el registro de la base debe alcanzar tambiÃ©n Burn.
            Burn.Apply(wall.Health, 1, 2f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(wall.IsBurning);
            Assert.IsNull(wall.Health.LastImpactPoint);
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

        // ---- L5b: recompensa, sala del tocón y reliquia ----

        IEnumerator CollapseNow()
        {
            CreateWalker(boss.TestStop(3));
            walker.AddComponent<PlayerController>().readInput = false;
            boss.TestEndingAction(0);
            yield return new WaitForSeconds(7.2f);
            Assert.IsTrue(boss.HasCollapsed);
        }

        LepidodendronPrompt PromptNamed(string title) =>
            boss.GetComponentsInChildren<LepidodendronPrompt>(true).FirstOrDefault(p => p.Title == title);

        [UnityTest]
        public IEnumerator L5bLaRecompensaBajaTrasElDesplomeYElCorazonDaUnCorazonUnaVez()
        {
            yield return CollapseNow();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(boss.HeartDropped);
            Assert.IsTrue(boss.SporeDropped);
            var health = walker.GetComponent<Health>();
            int max = health.Max;
            var heart = boss.GetComponentsInChildren<LepidodendronDrop>().First(d => d.name == "Drop_Heart");
            Assert.IsFalse(heart.Landed, "Todavía baja por la luz.");
            yield return new WaitForSeconds(2.3f);
            Assert.IsTrue(heart.Landed);
            walker.transform.position = heart.transform.position;
            yield return null; yield return null;
            Assert.AreEqual(max + 2, health.Max);
            Assert.IsTrue(RunInventory.HasFlag(RunInventory.LepidoHeartTaken));
            Assert.IsNull(boss.GetComponentsInChildren<LepidodendronDrop>().FirstOrDefault(d => d.name == "Drop_Heart"));
            boss.TestRewardAction(0); // Lo ya cogido no vuelve a bajar.
            yield return new WaitForSeconds(0.6f);
            Assert.IsNull(boss.GetComponentsInChildren<LepidodendronDrop>().FirstOrDefault(d => d.name == "Drop_Heart"));
            Assert.AreEqual(max + 2, health.Max);
        }

        [UnityTest]
        public IEnumerator L5bLaEsporaMoradaEntraEnObjetosClaveYLanzaSuPresentacion()
        {
            yield return CollapseNow();
            yield return new WaitForSeconds(3f);
            var spore = boss.GetComponentsInChildren<LepidodendronDrop>().First(d => d.name == "Drop_PurpleSpore");
            walker.transform.position = spore.transform.position;
            yield return null; yield return null;
            Assert.IsTrue(RunInventory.Has(RunInventory.PurpleSpore));
            Assert.AreEqual(1, RunInventory.KeyItems.Count);
            Assert.IsTrue(Primordia.UI.RewardPresenter.Showing);
            Assert.AreEqual(0f, Time.timeScale, "El juego se para durante la presentación.");
            yield return new WaitForSecondsRealtime(4.3f);
            Assert.IsFalse(Primordia.UI.RewardPresenter.Showing);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsEmpty(RunInventory.Relics);
        }

        [UnityTest]
        public IEnumerator L5bLaGrietaLlevaALaSalaYLaSalidaVuelve()
        {
            yield return CollapseNow();
            var crack = PromptNamed("Grieta del tocón");
            Assert.IsNotNull(crack);
            Assert.IsFalse(boss.InStumpRoom);
            crack.Interact(walker);
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.IsTrue(boss.InStumpRoom);
            Assert.IsNotNull(boss.StumpRoomRoot);
            Assert.Less(Vector2.Distance(walker.transform.position, boss.StumpRoomPlayerStart), 0.5f);
            Assert.IsTrue(boss.RelicOnPedestal);
            Assert.IsNotNull(boss.StumpRoomRoot.Find("Wall").GetComponent<EdgeCollider2D>());
            var exit = PromptNamed("Salir");
            Assert.IsNotNull(exit);
            exit.Interact(walker);
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.IsFalse(boss.InStumpRoom);
            Assert.Less(Vector2.Distance(walker.transform.position, boss.StumpCrack.position), 3f);
        }

        [UnityTest]
        public IEnumerator L5bLaReliquiaSeCogeUnaVezDejaElPedestalVacioYCuraAlLlegarAUnPisoNuevo()
        {
            yield return CollapseNow();
            PromptNamed("Grieta del tocón").Interact(walker);
            yield return new WaitForSecondsRealtime(1.8f);
            var relic = PromptNamed("Raíz ancestral");
            Assert.IsTrue(relic.IsAvailable);
            relic.Interact(walker);
            yield return null;
            Assert.IsTrue(RunInventory.Has(RunInventory.RootAncestral));
            Assert.IsFalse(boss.RelicOnPedestal);
            Assert.IsFalse(relic.IsAvailable);
            yield return new WaitForSecondsRealtime(4.6f);
            Assert.IsFalse(Primordia.UI.RewardPresenter.Showing);
            var pedestal = boss.StumpRoomRoot.Find("Pedestal").GetComponent<SpriteRenderer>();
            Assert.That(pedestal.sprite.name, Does.Contain("Pedestal_Empty"));
            relic.Interact(walker); // Ya no hace nada: una sola reliquia.
            Assert.AreEqual(1, RunInventory.Relics.Count);
            var health = walker.GetComponent<Health>();
            health.TakeDamage(health.Current - 1);
            RunInventory.OnFloorEntered(walker);
            Assert.AreEqual(health.Max, health.Current);
        }

        [Test]
        public void L5bSinLaReliquiaUnPisoNuevoNoCura()
        {
            CreateWalker(boss.TestStop(3));
            var health = walker.AddComponent<Health>();
            health.TakeDamage(1);
            int before = health.Current;
            RunInventory.OnFloorEntered(walker);
            Assert.AreEqual(before, health.Current);
        }

        [Test]
        public void L5bLaPausaMuestraLasFilasSoloSiHayObjetos()
        {
            var panel = new GameObject("Pausa");
            try
            {
                var row = panel.AddComponent<Primordia.UI.PauseRelicsRow>();
                row.Rebuild();
                Assert.AreEqual(0, row.RowCount);
                Assert.IsFalse(row.ShowsRelics);
                Assert.IsFalse(row.ShowsKeyItems);
                RunInventory.AddKeyItem(new RunInventory.Entry { Id = RunInventory.PurpleSpore, Name = "Espora morada", Effect = "x" });
                row.Rebuild();
                Assert.IsTrue(row.ShowsKeyItems);
                Assert.IsFalse(row.ShowsRelics);
                Assert.AreEqual(2, row.RowCount); // encabezado + una fila
                RunInventory.AddRelic(new RunInventory.Entry { Id = RunInventory.RootAncestral, Name = "Raíz ancestral", Effect = "y" });
                row.Rebuild();
                Assert.IsTrue(row.ShowsRelics);
                Assert.AreEqual(4, row.RowCount);
            }
            finally { Object.DestroyImmediate(panel); }
        }

        [Test]
        public void L5bLaSalaDelToconNoSeCreaAntesDelDesplome()
        {
            Assert.IsNull(boss.StumpRoomRoot);
            Assert.IsFalse(boss.InStumpRoom);
            Assert.IsFalse(boss.HeartDropped);
            Assert.AreEqual("Primero fuerza el desplome (Alt+F1)", boss.TestRewardAction(0));
        }
    }
}
#endif
