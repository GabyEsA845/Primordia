using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class AmoebaBossTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);

            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
            ProjectilePool.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        AttackData CreateAttack()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), fireRate: 1f, damage: 1, speed: 5f, lifetime: 5f);
            return attack;
        }

        static AmoebaBody CreateBody(Transform parent, string name, float radius, bool active)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            go.transform.SetParent(parent, false);
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>().radius = radius;
            var body = go.AddComponent<AmoebaBody>();
            body.Configure(null, null, LayerMask.GetMask("Default"));
            return body;
        }

        /// <summary>Ameba de 100 de vida con todas las esperas casi a 0, para que las fases pasen en pocos cuadros.</summary>
        AmoebaBoss CreateBoss(Vector2 position = default, float barrageSeconds = 0.2f)
        {
            var root = new GameObject("Amoeba");
            root.transform.position = position;
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<Health>();
            root.AddComponent<Weapon>();
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 100, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            root.AddComponent<Enemy>().SetData(data);

            var attack = CreateAttack();
            root.AddComponent<AmoebaBarrage>().Configure(attack, attack, attack, new[]
            {
                new AmoebaBarrage.Segment { duration = barrageSeconds, ringInterval = 0.05f, ringCount = 8, ringGap = 2 },
            });

            var main = CreateBody(root.transform, "Body", 1f, true);
            var halfA = CreateBody(root.transform, "Half_A", 0.6f, false);
            var halfB = CreateBody(root.transform, "Half_B", 0.6f, false);
            var boss = root.AddComponent<AmoebaBoss>();
            boss.Configure(main, new[] { halfA, halfB });
            boss.SetTimings(0f, 0.01f, 0.01f, 0f, 0f, 0.01f, 0f);
            boss.SetFallbackTransition(0.05f);
            return boss;
        }

        [Test]
        public void Boss_IsTheActiveBoss_WhileEnabled()
        {
            var boss = CreateBoss();

            Assert.AreSame(boss, ActiveBoss.Current);
            Assert.AreEqual(BossBarStyle.Normal, boss.BarStyle);

            Object.DestroyImmediate(boss.gameObject);
            Assert.IsNull(ActiveBoss.Current);
        }

        [UnityTest]
        public IEnumerator Below80Percent_GetsAngry()
        {
            var boss = CreateBoss();

            boss.Health.TakeDamage(20);
            yield return null;
            Assert.AreEqual(AmoebaBoss.Stage.Enraging, boss.CurrentStage);

            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(AmoebaBoss.Stage.Angry, boss.CurrentStage);
            Assert.AreEqual(BossBarStyle.Enraged, boss.BarStyle);
        }

        [UnityTest]
        public IEnumerator Below50Percent_SplitsInTwo_ThatShareTheHealth()
        {
            var boss = CreateBoss();

            boss.Health.TakeDamage(50);
            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual(AmoebaBoss.Stage.Split, boss.CurrentStage);
            Assert.IsFalse(boss.MainBody.gameObject.activeSelf, "La grande desaparece al dividirse.");
            Assert.IsTrue(boss.Halves.All(half => half.gameObject.activeSelf), "Aparecen las dos medianas.");
            Assert.Greater(Vector2.Distance(boss.Halves[0].Position, boss.Halves[1].Position), 0.5f, "Las medianas no salen una encima de otra.");

            // Un golpe a cualquiera de las dos baja la barra común.
            int before = boss.Health.Current;
            boss.Halves[1].GetComponentInParent<Health>().TakeDamage(3);
            Assert.AreEqual(before - 3, boss.Health.Current);
        }

        [UnityTest]
        public IEnumerator AfterSplitting_TheHalvesDashToOppositeSides()
        {
            // Lejos del origen: sin colisionadores que otras pruebas puedan haber dejado cerca.
            var origin = new Vector2(500f, 500f);
            var boss = CreateBoss(origin);
            boss.Health.TakeDamage(50);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(AmoebaBoss.Stage.Split, boss.CurrentStage);
            float start = Vector2.Distance(boss.Halves[0].Position, boss.Halves[1].Position);

            yield return new WaitForSeconds(0.3f);

            // A 10 u/s cada una, sin aviso: se alejan unas 6 unidades en 0,3 s.
            Assert.Greater(Vector2.Distance(boss.Halves[0].Position, boss.Halves[1].Position), start + 4f);
            Assert.Less(boss.Halves[0].Position.x, origin.x, "La primera sale hacia la izquierda.");
            Assert.Greater(boss.Halves[1].Position.x, origin.x, "La segunda, hacia la derecha.");
        }

        [Test]
        public void PredictTarget_AimsWhereTheTargetWillBe()
        {
            // Objetivo a 10 u, corriendo hacia arriba a 5 u/s; quien sale va a 10 u/s.
            var predicted = AmoebaBoss.PredictTarget(Vector2.zero, new Vector2(10f, 0f), new Vector2(0f, 5f), 10f, 2f);
            Assert.Greater(predicted.y, 4f, "Apunta por delante de él.");
            Assert.AreEqual(10f, predicted.x, 0.001f);

            var capped = AmoebaBoss.PredictTarget(Vector2.zero, new Vector2(10f, 0f), new Vector2(0f, 5f), 10f, 0.2f);
            Assert.AreEqual(1f, capped.y, 0.001f, "Como mucho el adelanto máximo.");

            var still = AmoebaBoss.PredictTarget(Vector2.zero, new Vector2(10f, 0f), Vector2.zero, 10f, 2f);
            Assert.AreEqual(new Vector2(10f, 0f), still, "Si está quieto, apunta a donde está.");
        }

        [UnityTest]
        public IEnumerator Health_DoesNotDropBelow10Percent_BeforeTheFinalScene()
        {
            var boss = CreateBoss();

            boss.Health.TakeDamage(99);
            Assert.AreEqual(boss.FinalHealth, boss.Health.Current, "El golpe se queda en el 10 %.");
            Assert.IsFalse(boss.Health.IsDead);

            yield return null;
            Assert.AreEqual(AmoebaBoss.Stage.FinalScene, boss.CurrentStage);
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
            Assert.IsFalse(boss.Health.TakeDamage(5), "Durante la escena y la lluvia es inmune.");
        }

        [UnityTest]
        public IEnumerator FinalScene_MergesIntoOne_FiresAndThenFallsInTwoHits()
        {
            var boss = CreateBoss(barrageSeconds: 1f);
            boss.Health.TakeDamage(50);
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(AmoebaBoss.Stage.Split, boss.CurrentStage);

            boss.Health.TakeDamage(60);
            yield return new WaitForSeconds(0.15f);

            Assert.AreEqual(AmoebaBoss.Stage.Barrage, boss.CurrentStage);
            Assert.IsTrue(boss.MainBody.gameObject.activeSelf, "Las medianas se juntan en la grande.");
            Assert.IsFalse(boss.Halves.Any(half => half.gameObject.activeSelf));

            // Cada tramo deja un respiro de 0,4 s antes del primer anillo.
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(Object.FindObjectsByType<Projectile>().Length, 0, "La lluvia de proyectiles empieza.");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(AmoebaBoss.Stage.Exhausted, boss.CurrentStage);
            Assert.AreEqual(BossBarStyle.Weak, boss.BarStyle);

            boss.Health.TakeDamage(1);
            Assert.IsFalse(boss.Health.IsDead);
            boss.Health.TakeDamage(1);
            Assert.IsTrue(boss.Health.IsDead, "Agotada cae de dos golpes, aunque sean flojos.");
        }

        [UnityTest]
        public IEnumerator Calm_ChargesUntilAWall_AndStopsThere()
        {
            // Pasillo cerrado por todas partes: vaya hacia donde vaya, acaba contra una pared.
            var walls = Track(new GameObject("Walls"));
            foreach (var (center, size) in new[]
            {
                (new Vector2(0f, 4f), new Vector2(10f, 1f)), (new Vector2(0f, -4f), new Vector2(10f, 1f)),
                (new Vector2(4f, 0f), new Vector2(1f, 10f)), (new Vector2(-4f, 0f), new Vector2(1f, 10f)),
            })
            {
                var wall = walls.AddComponent<BoxCollider2D>();
                wall.offset = center;
                wall.size = size;
            }

            var boss = CreateBoss();
            boss.SetRests(10f, 10f, 10f);
            yield return new WaitForSeconds(3f);

            // Con la calma a 1,2 u/s ya ha chocado: quieta, tocando una pared (radio 1 + media pared 0,5).
            Vector2 position = boss.MainBody.Position;
            float edge = Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.y));
            Assert.AreEqual(2.5f, edge, 0.15f);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(position, boss.MainBody.Position, "Se queda quieta tras chocar.");
        }

        [UnityTest]
        public IEnumerator CinematicOverlay_HidesTheHud_AndRestoresEachElementAsItWas()
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            var canvas = Track(new GameObject("GameUI", typeof(RectTransform)));
            var hearts = new GameObject("Hearts", typeof(RectTransform));
            hearts.transform.SetParent(canvas.transform, false);
            // Como el cartel del linaje: lleva su propia transparencia, a 0 mientras no se ve.
            var banner = new GameObject("Banner", typeof(RectTransform)).AddComponent<CanvasGroup>();
            banner.transform.SetParent(canvas.transform, false);
            banner.alpha = 0f;
            var overlay = canvas.AddComponent<UI.CinematicOverlay>();
            yield return null;

            manager.SetState(GameState.Cinematic);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(1f, overlay.Amount);
            Assert.AreEqual(0f, hearts.GetComponent<CanvasGroup>().alpha, "El HUD se oculta.");
            Assert.AreEqual(0f, banner.alpha);

            manager.SetState(GameState.Playing);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(0f, overlay.Amount);
            Assert.AreEqual(1f, hearts.GetComponent<CanvasGroup>().alpha, "El HUD vuelve.");
            Assert.AreEqual(0f, banner.alpha, "El cartel sigue oculto: no aparece al acabar la escena.");
        }

        [UnityTest]
        public IEnumerator Calm_StartsMoving_RightAfterItsWarning()
        {
            // Con la espera de verdad (0,6 s, que es también el aviso): antes salía sin dirección y no se movía.
            var origin = new Vector2(-500f, 500f);
            var boss = CreateBoss(origin);
            boss.SetTimings(0.6f, 0.01f, 0.01f, 0f, 0f, 0.01f, 0f);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(origin, boss.MainBody.Position, "Durante el aviso no se mueve.");

            yield return new WaitForSeconds(0.8f);
            Assert.Greater(Vector2.Distance(origin, boss.MainBody.Position), 0.3f, "En calma ya se desliza por la sala.");
        }

        [UnityTest]
        public IEnumerator Charge_SlidesAlongAWallItGrazes_InsteadOfGettingStuck()
        {
            // Suelo justo debajo y el jugador lejos, un poco más abajo: la embestida roza el suelo.
            var floor = Track(new GameObject("Wall")).AddComponent<BoxCollider2D>();
            floor.offset = new Vector2(0f, -1.6f);
            floor.size = new Vector2(60f, 1f);
            var player = Track(new GameObject("Player") { tag = "Player" });
            player.transform.position = new Vector3(12f, -0.9f, 0f);

            var boss = CreateBoss();
            boss.SetRests(10f, 10f, 10f);
            boss.Health.TakeDamage(20);
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(AmoebaBoss.Stage.Angry, boss.CurrentStage);

            yield return new WaitForSeconds(1.5f);
            Assert.Greater(boss.MainBody.Position.x, 3f, "Resbala por la pared y sigue hacia el jugador.");
            Assert.Greater(boss.MainBody.Position.y, -0.2f, "Sin meterse en la pared.");
        }

        [UnityTest]
        public IEnumerator Touching_HurtsThePlayer_WithoutPushing_AndThornsPrickBack()
        {
            var boss = CreateBoss();
            boss.SetRests(10f, 10f, 10f);
            var player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = player.AddComponent<Health>();
            player.AddComponent<Mutations.Thorns>().Configure(newDamage: 2, newInterval: 1f);
            player.transform.position = new Vector3(0.3f, 0f, 0f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Less(health.Current, health.Max, "Tocar la ameba quita vida.");
            Assert.AreEqual(boss.Health.Max - 2, boss.Health.Current, "Las Espinas la pinchan aunque no haya choque.");
            Assert.Less(body.position.magnitude, 0.5f, "No empuja al jugador: lo atraviesa.");
        }

        [TestCase(1f, 0f, 0)]
        [TestCase(0f, 1f, 2)]
        [TestCase(-1f, 0f, 4)]
        [TestCase(0f, -1f, 6)]
        [TestCase(1f, -1f, 7)]
        [TestCase(-1f, 1f, 3)]
        public void AimSector_GoesCounterclockwiseFromTheRight(float x, float y, int sector)
        {
            Assert.AreEqual(sector, AmoebaBoss.AimSector(new Vector2(x, y)));
        }

        [Test]
        public void DespawnAll_RemovesEveryProjectileInFlight()
        {
            var attack = CreateAttack();
            var shooter = Track(new GameObject("Shooter")).AddComponent<Weapon>();
            for (int i = 0; i < 5; i++)
                shooter.Fire(Vector2.right, attack);
            Assert.AreEqual(5, Projectile.ActiveCount);

            Projectile.DespawnAll();

            Assert.AreEqual(0, Projectile.ActiveCount);
            Assert.AreEqual(0, Object.FindObjectsByType<Projectile>().Length, "Vuelven al pool, desactivados.");
        }

        [Test]
        public void FloorConfig_ChoosesEachBoss_HalfOfTheTime()
        {
            var config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var room = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            room.SetLayout(RoomLayouts.All[RoomLayouts.Normal]);
            var virus = Track(new GameObject("Virus")).AddComponent<Enemy>();
            var amoeba = Track(new GameObject("Amoeba")).AddComponent<Enemy>();
            config.ConfigureBosses(new[]
            {
                new FloorConfig.BossEntry(virus, room, null, Vector2.zero),
                new FloorConfig.BossEntry(amoeba, room, null, Vector2.zero),
            });

            int amoebas = 0;
            const int runs = 2000;
            for (int seed = 1; seed <= runs; seed++)
            {
                if (config.ChooseBoss(new System.Random(seed)).prefab == amoeba)
                    amoebas++;
            }

            Assert.AreEqual(runs / 2f, amoebas, runs * 0.05f);
        }

        [Test]
        public void FloorConfig_WithoutBossList_UsesTheSingleBoss()
        {
            var config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var room = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            var virus = Track(new GameObject("Virus")).AddComponent<Enemy>();
            config.ConfigureRooms(room, room, new[] { room }, new RoomTemplate[0], Vector2Int.one, Vector2Int.zero);
            config.ConfigureEnemies(new FloorConfig.EnemyEntry[0], virus);

            var options = config.BossOptions();

            Assert.AreEqual(1, options.Length);
            Assert.AreSame(virus, options[0].prefab);
            Assert.AreSame(room, options[0].room);
        }

        [Test]
        public void CircleRoom_IsMuchBiggerThanTheNormalRoom()
        {
            var circle = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            circle.SetLayout(RoomLayouts.All[RoomLayouts.BossCircle]);
            var normal = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            normal.SetLayout(RoomLayouts.All[RoomLayouts.Normal]);

            Assert.Greater(FloorArea(circle), FloorArea(normal) * 2);
            Assert.AreEqual(RoomLayouts.BossCircleDiameter, circle.FloorBounds().width);
            Assert.AreEqual(RoomLayouts.BossCircleDiameter, circle.FloorBounds().height);
            // Redonda: las esquinas del cuadrado que la contiene son pared.
            Assert.AreNotEqual(RoomTemplate.Mark.Floor, circle.Get(1, 1));
        }

        static int FloorArea(RoomTemplate template) =>
            Enumerable.Range(0, template.Width * template.Height)
                .Count(i => template.Get(i % template.Width, i / template.Width) == RoomTemplate.Mark.Floor);
    }
}
