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
            // La mayoría de comprobaciones no necesitan enemigos activos. Las pruebas de oleadas reponen su reserva.
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("waveEnemies").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            Assert.AreEqual(450, boss.Health.Current, "Nunca resta el sobre-daño.");
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
            Scenery.Find("Wall1Supplies").gameObject.SetActive(false); // Aísla el golpe del estallido, sin cadenas de metano.
            var position = new Vector2(wall.transform.position.x - wall.BodyCollider.size.x / 2f - 0.4f, wall.KnotPosition(0).y);
            PoisonSpitBurst.Spawn(position, 2, 2f, 0f, null, Team.Enemy);
            yield return new WaitForSeconds(0.2f); // Después del impacto y antes del primer tick de veneno (0,5 s).
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
        public IEnumerator ElFuegoCubreEnTresSegundosYHace12PorSegundoSinTopeBurn()
        {
            var wall = boss.Walls[0];
            boss.GetComponent<BossDamageLimiter>().Configure(new[] { new BossDamageLimiter.Cap { source = DamageSource.Burn, perSecond = 0f } });
            float start = Time.time;
            wall.IgniteAt(3f);
            yield return new WaitForSeconds(1.05f);
            Assert.AreEqual(150 - Mathf.FloorToInt((Time.time - start) * 12f), wall.Health.Current, 2f);
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
            Assert.AreEqual(150 - Mathf.FloorToInt((Time.time - start) * 12f), wall.Health.Current, 2f);
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
            var wall = boss.Walls[2]; // La más gruesa: el registro de la base debe alcanzar también Burn.
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
    }
}
#endif
