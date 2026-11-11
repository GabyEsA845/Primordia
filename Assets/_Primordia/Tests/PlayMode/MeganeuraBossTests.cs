#if UNITY_EDITOR
using System.Collections;
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
    /// <summary>Meganeura, paso 7a: hojas con direcciones, sala, presentación (sombra, posarse, abrir las alas) y variantes de alas.</summary>
    public class MeganeuraBossTests
    {
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Meganeura.prefab";
        const string ArtPath = "Assets/_Primordia/Data/Bosses/MeganeuraArt.asset";

        GameObject room;
        MeganeuraBoss boss;

        [SetUp]
        public void SetUp()
        {
            room = new GameObject("Room_Boss_Meganeura");
            room.transform.position = new Vector3(100f, 100f, 0f);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "falta el prefab de la Meganeura (Primordia > Boss > Crear Meganeura y escena de prueba)");
            // Sin presentación: el prefab lleva BossIntro y esperaría al GameManager.
            var instance = Object.Instantiate(prefab, room.transform);
            Object.DestroyImmediate(instance.GetComponent<BossIntro>());
            boss = instance.GetComponent<MeganeuraBoss>();
            boss.SetArena(room.transform, new Vector2(42f, 28f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(room);
        }

        [Test]
        public void LaSalaTiene5SigillariasSolidasBienRepartidasYLasCharcasDesdeElPrincipio()
        {
            var trees = room.GetComponentsInChildren<BossSigillaria>();
            Assert.AreEqual(5, trees.Length);
            var stump = (Vector2)room.transform.position + new Vector2(8f, 3f);
            var center = (Vector2)room.transform.position;
            foreach (var tree in trees)
            {
                Assert.IsNotNull(tree.GetComponent<BoxCollider2D>());
                var local = tree.Position - center;
                // Dentro del suelo (40×24), lejos del centro de combate y del Lepidodendron, y sin pegarse a las puertas.
                Assert.That(Mathf.Abs(local.x), Is.LessThan(18f));
                Assert.That(Mathf.Abs(local.y), Is.LessThan(11.5f));
                Assert.That(local.magnitude, Is.GreaterThan(8f));
                Assert.That(Vector2.Distance(tree.Position, stump), Is.GreaterThan(6f));
                foreach (var other in trees)
                {
                    if (other != tree)
                        Assert.That(Vector2.Distance(tree.Position, other.Position), Is.GreaterThan(9f));
                }
            }
            Assert.AreEqual(3, room.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "MeganeuraPuddle"));
        }

        [Test]
        public void UnRayoSueltoNoPuedeCaerCercaDeUnaSigillariaEnPie()
        {
            var tree = room.GetComponentInChildren<BossSigillaria>();
            Assert.IsFalse(BossSigillaria.IsSafeSpot(tree.Position + Vector2.right * (BossSigillaria.SafeRadius - 0.5f)));
            Assert.IsTrue(BossSigillaria.IsSafeSpot(room.transform.position));
        }
        [Test]
        public void ElArteDeLaTormentaEstaImportadoConSusCuadros()
        {
            var fx = boss.EffectsInUse;
            Assert.AreEqual(10, fx.boltBigMark.Length);
            Assert.AreEqual(5, fx.boltBig.Length);
            Assert.AreEqual(16, fx.boltRing.Length);
            Assert.AreEqual(6, fx.boltHuge.Length);
            Assert.AreEqual(4, fx.rain.Length);
            Assert.AreEqual(8, fx.sigillariaStruck.Length);
            Assert.AreEqual(4, fx.puddleElectric.Length);
            Assert.AreEqual(3, fx.puddleGrow.Length);
            foreach (var set in fx.puddleElectric.Concat(fx.puddleGrow))
                Assert.AreEqual(6, set.frames.Length);
        }

        [Test]
        public void LosRayosSueltosCaenSiempreAlejadosDeLasSigillariasEnPie()
        {
            for (int run = 0; run < 20; run++)
            {
                var spots = boss.PickLooseSpots(room.transform.position + new Vector3(14f, -7f), 4);
                Assert.That(spots.Count, Is.GreaterThan(0));
                foreach (var spot in spots)
                    Assert.IsTrue(BossSigillaria.IsSafeSpot(spot), $"el rayo suelto {spot} cae demasiado cerca de una Sigillaria");
            }
        }

        [Test]
        public void ElRayoGrandeLoAtraeUnaSigillariaCercanaYSoloUnaVez()
        {
            var tree = room.GetComponentInChildren<BossSigillaria>();
            int standing = BossSigillaria.Standing.Count;
            boss.StrikeBig(tree.Position + new Vector2(2f, 0f));
            Assert.IsTrue(tree.Struck);
            Assert.AreEqual(standing - 1, BossSigillaria.Standing.Count);
            // Ya no atrae ni protege: un segundo rayo cae donde cae y el punto deja de estar vetado.
            Assert.IsNull(BossSigillaria.Nearest(tree.Position, BossSigillaria.AttractRadius));
            Assert.IsTrue(BossSigillaria.IsSafeSpot(tree.Position));
        }

        [Test]
        public void UnRayoLaDejaAturdidaYCayendo()
        {
            boss.TriggerShock();
            Assert.AreEqual(MeganeuraBoss.Mode.Shocked, boss.CurrentMode);
        }

        [Test]
        public void LasCapasDeLaTormentaOscurecenYElDestelloBaja()
        {
            var storm = MeganeuraStorm.Create(room.transform, room.transform.position, new Vector2(42f, 28f), boss.EffectsInUse.rain, boss.EffectsInUse.ash);
            Assert.AreEqual(0f, storm.DarkAlpha);
            storm.Darken(0.5f, 0.01f);
            storm.Flash(0.85f);
            storm.SetRain(true);
            Assert.IsTrue(storm.Raining);
            storm.Calm(0.01f);
            Assert.IsFalse(storm.Raining);
        }
        [Test]
        public void LasFilasDeDireccionYElFlipCasanConLosAngulos()
        {
            Assert.AreEqual(0, DirectionalArt.RowFor(90f, out bool flip));
            Assert.IsFalse(flip);
            Assert.AreEqual(4, DirectionalArt.RowFor(0f, out flip));
            Assert.IsFalse(flip);
            Assert.AreEqual(8, DirectionalArt.RowFor(-90f, out flip));
            Assert.AreEqual(4, DirectionalArt.RowFor(180f, out flip));
            Assert.IsTrue(flip);
            // 135° = fila de 45° con Flip X.
            Assert.AreEqual(2, DirectionalArt.RowFor(135f, out flip));
            Assert.IsTrue(flip);
            Assert.AreEqual(6, DirectionalArt.RowFor(-135f, out flip));
            Assert.IsTrue(flip);
        }

        [Test]
        public void LasHojasEstanImportadasConSuTamano()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<DirectionalArt>(ArtPath);
            Assert.IsNotNull(art);
            var perch = art.Find("Meganeura_Perch");
            Assert.AreEqual(4, perch.frames);
            Assert.AreEqual(9, perch.rows);
            Assert.AreEqual(36, perch.sprites.Length);
            Assert.AreEqual(192f, perch.Get(4, 0).rect.width);
            // Variantes de alas y sombras con los mismos cuadros.
            Assert.IsNotNull(art.Find("Meganeura_Fly_Torn2Burnt"));
            Assert.AreEqual(perch.frames, art.Find("Meganeura_Shadow_Perch").frames);
            // DeathFall es una sola fila y la ninfa tiene celda de 64.
            Assert.AreEqual(1, art.Find("Meganeura_DeathFall").rows);
            Assert.AreEqual(64f, art.Find("Meganeura_Nymph_Idle").Get(0, 0).rect.width);
        }

        [Test]
        public void LasAlasSeRasganConLaVidaYSeQuemanConElMetano()
        {
            var health = boss.GetComponent<Health>();
            Assert.AreEqual("", boss.WingVariant);
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.40f));
            Assert.AreEqual("Torn1", boss.WingVariant);
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.30f));
            Assert.AreEqual("Torn2", boss.WingVariant);
            boss.Burnt = true;
            Assert.AreEqual("Torn2Burnt", boss.WingVariant);
        }

        [UnityTest]
        public IEnumerator LaPresentacionLaPosaEnElTroncoYAbreLasAlas()
        {
            var body = boss.transform.Find("Body").GetComponent<SpriteRenderer>();
            var shadow = boss.transform.Find("Shadow").GetComponent<SpriteRenderer>();
            boss.PlayIntroPose(2f);
            yield return null;
            // Mientras cruza la sala solo se ve su sombra.
            Assert.IsFalse(body.enabled);
            Assert.IsTrue(shadow.enabled);

            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(body.enabled, "tras cruzar la sala se posa");
            Assert.IsNotNull(body.sprite);
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Land") || body.sprite.name.StartsWith("Meganeura_Intro"), body.sprite.name);

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Intro"), body.sprite.name);

            boss.BeginFight();
            yield return null;
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Perch"), body.sprite.name);
        }

        // ---------- Paso 7b: fase 1 ----------

        Health Player(Vector2 position)
        {
            var player = new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer(CombatLayers.Player) };
            player.transform.position = position;
            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.position = position;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = player.AddComponent<Health>();
            health.SetMax(20, healDifference: false);
            health.ResetHealth();
            extras.Add(player);
            return health;
        }

        readonly System.Collections.Generic.List<GameObject> extras = new();

        [TearDown]
        public void TearDownExtras()
        {
            foreach (var extra in extras)
            {
                if (extra != null)
                    Object.DestroyImmediate(extra);
            }
            extras.Clear();
        }

        IEnumerator UntilMode(MeganeuraBoss.Mode mode, float timeout, System.Action<MeganeuraBoss.Mode> each = null)
        {
            for (float t = 0f; t < timeout && boss.CurrentMode != mode; t += Time.deltaTime)
            {
                each?.Invoke(boss.CurrentMode);
                yield return null;
            }
            Assert.AreEqual(mode, boss.CurrentMode, $"no llegó a {mode}");
        }

        [UnityTest]
        public IEnumerator ElCicloSubePersigueCaeYQuedaVulnerable()
        {
            Player((Vector2)room.transform.position + new Vector2(-10f, -5f));
            var hit = boss.GetComponent<Collider2D>();
            boss.BeginFight();
            var seen = new System.Collections.Generic.HashSet<MeganeuraBoss.Mode>();
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 14f, mode =>
            {
                seen.Add(mode);
                // Alta o cayendo en picado es invulnerable: sin colisión y con la barra con candado.
                if (boss.IsHigh)
                {
                    Assert.IsFalse(hit.enabled, $"{mode} debería ser invulnerable");
                    Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
                }
            });
            foreach (var mode in new[] { MeganeuraBoss.Mode.Climbing, MeganeuraBoss.Mode.High, MeganeuraBoss.Mode.Marking, MeganeuraBoss.Mode.Diving })
                Assert.IsTrue(seen.Contains(mode), $"no pasó por {mode}");
            Assert.IsTrue(hit.enabled, "a ras de suelo se le puede pegar");
            Assert.AreEqual(BossBarStyle.Normal, boss.BarStyle);
        }

        [UnityTest]
        public IEnumerator ElPicadoDanaAlQueSeQuedaEnLaMarca()
        {
            var health = Player((Vector2)room.transform.position + new Vector2(-10f, -5f));
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 14f);
            Assert.AreEqual(18, health.Current, "el picado quita un corazón entero (2 puntos)");
        }

        [UnityTest]
        public IEnumerator SiCaeSobreElTroncoQuedaClavadaYLoArranca()
        {
            Player((Vector2)boss.transform.position);
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.Stuck, 14f);
            Assert.IsTrue(boss.GetComponent<Collider2D>().enabled, "clavada es vulnerable");
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 4f);
        }

        [UnityTest]
        public IEnumerator SiSePrendeElMetanoBajoElllaCaeArdiendo()
        {
            Player((Vector2)room.transform.position + new Vector2(-10f, -5f));
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 14f);
            Assert.IsFalse(boss.Burnt);
            var mistObject = new GameObject("Mist");
            extras.Add(mistObject);
            mistObject.transform.position = boss.transform.position;
            var mist = mistObject.AddComponent<MethaneMist>();
            mist.Ignite();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(boss.Burnt, "las alas quemadas son permanentes");
            Assert.IsTrue(boss.CurrentMode is MeganeuraBoss.Mode.BurnFall or MeganeuraBoss.Mode.Grounded, boss.CurrentMode.ToString());
            Assert.IsTrue(boss.WingVariant.EndsWith("Burnt"));
            yield return UntilMode(MeganeuraBoss.Mode.Grounded, 2f);
            Assert.IsTrue(boss.GetComponent<Collider2D>().enabled);
            // Después se endereza y vuelve al ciclo.
            yield return UntilMode(MeganeuraBoss.Mode.Climbing, 8f);
        }

        [Test]
        public void ElVientoArrastraLaNieblaDentroDelConoYNoFuera()
        {
            var inside = new GameObject("MistIn");
            var outside = new GameObject("MistOut");
            extras.Add(inside);
            extras.Add(outside);
            inside.transform.position = new Vector3(5f, 0f, 0f);
            outside.transform.position = new Vector3(-5f, 0f, 0f);
            inside.AddComponent<MethaneMist>();
            outside.AddComponent<MethaneMist>();
            // Un frame de viento hacia la derecha: 5 u/s durante 0,2 s = 1 u.
            Wind.Blow(Vector2.zero, Vector2.right, 9f, 35f, 5f, 0f, 0.2f);
            Assert.AreEqual(6f, inside.transform.position.x, 0.01f);
            Assert.AreEqual(-5f, outside.transform.position.x, 0.01f);
        }

        // ---------- Paso 7c: fase 2 ----------

        IEnumerator UntilTrue(System.Func<bool> condition, float timeout, string what)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime)
                yield return null;
            Assert.IsTrue(condition(), what);
        }

        [UnityTest]
        public IEnumerator Fase2_PoneHuevosEnLasCharcasYSeRompenAGolpes()
        {
            Player((Vector2)room.transform.position + new Vector2(-14f, -8f));
            boss.GetComponent<Health>().TakeDamage(Mathf.CeilToInt(boss.GetComponent<Health>().Max * 0.40f));
            Assert.AreEqual(1, boss.PhaseIndex);
            boss.BeginFight();
            yield return UntilTrue(() => boss.EggsAlive > 0, 14f, "no puso ningún racimo");
            Assert.IsNotNull(room.transform.Find("MeganeuraPuddle"), "las charcas están en la sala");
            var egg = MeganeuraEgg.Alive[0];
            Assert.IsTrue(egg.TryGetComponent<Health>(out var eggHealth));
            bool broken = false;
            egg.Resolved += (_, wasBroken) => broken = wasBroken;
            eggHealth.TakeDamage(10);
            // Si aún está apareciendo, se rompe en cuanto acaba de salir (unos 0,4 s).
            yield return UntilTrue(() => broken, 2f, "romper el racimo lo resuelve como roto");
            Assert.IsFalse(MeganeuraEgg.Alive.Contains(egg));
        }

        [UnityTest]
        public IEnumerator UnRacimoSinRomperEclosionaYSueltaUnaNinfa()
        {
            var egg = MeganeuraEgg.Spawn(room.transform, (Vector2)room.transform.position + new Vector2(-12f, 0f), boss.EffectsInUse, boss.NymphInUse, 0.5f);
            bool broken = true;
            egg.Resolved += (_, wasBroken) => broken = wasBroken;
            yield return UntilTrue(() => Object.FindAnyObjectByType<MeganeuraNymph>() != null, 4f, "no salió la ninfa");
            Assert.IsFalse(broken, "eclosionó, no lo rompieron");
        }

        [UnityTest]
        public IEnumerator SinHuevosBajaFuriosaYSePuedeHerir()
        {
            Player((Vector2)room.transform.position + new Vector2(-14f, -8f));
            boss.GetComponent<Health>().TakeDamage(Mathf.CeilToInt(boss.GetComponent<Health>().Max * 0.40f));
            boss.BeginFight();
            yield return UntilTrue(() => boss.EggsAlive > 0, 14f, "no puso huevos");
            // El jugador los rompe todos según salen.
            yield return UntilTrue(() =>
            {
                foreach (var egg in new System.Collections.Generic.List<MeganeuraEgg>(MeganeuraEgg.Alive))
                    egg.GetComponent<Health>().TakeDamage(10);
                return boss.CurrentMode == MeganeuraBoss.Mode.Rage;
            }, 25f, "no bajó furiosa");
            Assert.IsTrue(boss.GetComponent<Collider2D>().enabled, "furiosa es vulnerable");
            Assert.AreEqual(BossBarStyle.Normal, boss.BarStyle);
        }

        [UnityTest]
        public IEnumerator LaNinfaPersigueYLanzaElLabioHaciendoDano()
        {
            var health = Player((Vector2)room.transform.position);
            var prefab = boss.NymphInUse;
            var nymph = Object.Instantiate(prefab, (Vector2)room.transform.position + new Vector2(5f, 0f), Quaternion.identity, room.transform);
            yield return UntilTrue(() => health.Current < 20, 6f, "la ninfa no le pegó");
            Assert.AreEqual(19, health.Current);
            Assert.IsNotNull(nymph);
        }

        [UnityTest]
        public IEnumerator AlMorirLaMadreSeVanHuevosYNinfas()
        {
            var egg = MeganeuraEgg.Spawn(room.transform, (Vector2)room.transform.position + new Vector2(-12f, 0f), boss.EffectsInUse, boss.NymphInUse, 30f);
            var nymph = Object.Instantiate(boss.NymphInUse, (Vector2)room.transform.position + new Vector2(-12f, 3f), Quaternion.identity, room.transform);
            yield return null;
            boss.BeginFight();
            yield return null;
            boss.GetComponent<Health>().TakeDamage(9999);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, MeganeuraEgg.Alive.Count);
            Assert.IsTrue(nymph.GetComponent<Health>().IsDead);
            Assert.IsTrue(egg == null || !MeganeuraEgg.Alive.Contains(egg));
        }

        [Test]
        public void ConLaSalaDesactivadaSeMontaSinFallar()
        {
            // La sala mantiene a sus enemigos desactivados hasta que entra el jugador: SetArena corre antes que el Awake del jefe.
            var other = new GameObject("Room_Hidden");
            other.transform.position = new Vector3(300f, 0f, 0f);
            var holder = new GameObject("Enemies");
            holder.transform.SetParent(other.transform);
            holder.SetActive(false);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var hidden = Object.Instantiate(prefab, holder.transform).GetComponent<MeganeuraBoss>();
            Assert.DoesNotThrow(() => hidden.SetArena(other.transform, new Vector2(42f, 28f)));
            Assert.AreEqual(308f, hidden.transform.position.x, 0.01f);
            holder.SetActive(true);
            Assert.AreEqual(308f, hidden.GetComponent<Rigidbody2D>().position.x, 0.01f);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void LaSalaTieneElTroncoPartidoSolidoYElJefeEmpiezaSobreEl()
        {
            var stump = room.transform.Find("LepidoSplit");
            Assert.IsNotNull(stump);
            Assert.IsFalse(stump.GetComponent<BoxCollider2D>().isTrigger);
            Assert.AreEqual((Vector2)stump.position, (Vector2)boss.transform.position);
        }

        [UnityTest]
        public IEnumerator Fase3_EntraEnLaTormentaCaenRayosYUnRayoCercanoLaAturde()
        {
            Player((Vector2)room.transform.position + new Vector2(-14f, -8f));
            var health = boss.GetComponent<Health>();
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.75f));
            Assert.AreEqual(2, boss.PhaseIndex);
            boss.BeginFight();
            yield return UntilTrue(() => boss.StormStarted && boss.Storm != null && boss.Storm.Raining, 14f, "no empezó la tormenta");
            Assert.That(boss.Storm.DarkAlpha, Is.GreaterThan(0.1f));
            // Con la tormenta en marcha, a ras de suelo, un rayo grande cerca de ella la electrocuta.
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 20f);
            boss.StrikeBig(boss.transform.position);
            Assert.AreEqual(MeganeuraBoss.Mode.Shocked, boss.CurrentMode);
            yield return UntilMode(MeganeuraBoss.Mode.Grounded, 6f);
            yield return UntilMode(MeganeuraBoss.Mode.Climbing, 12f);
        }

        [UnityTest]
        public IEnumerator VolandoAltaEsInvulnerableDeVerdadYElDanoEnElTiempoNoLeLlega()
        {
            Player((Vector2)room.transform.position + new Vector2(-10f, -5f));
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.High, 14f);
            var health = boss.GetComponent<Health>();
            Assert.IsTrue(health.IsInvulnerable);
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
            int before = health.Current;
            Assert.IsFalse(health.TakeDamage(5));
            Assert.IsFalse(health.TakeDamage(5, DamageKind.Tick, DamageSource.Poison));
            Assert.AreEqual(before, health.Current);
        }

        [UnityTest]
        public IEnumerator Fase3_SoloRecibeDanoAturdidaYElAturdimientoTieneTopeDeDano()
        {
            Player((Vector2)room.transform.position + new Vector2(-14f, -8f));
            var health = boss.GetComponent<Health>();
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.75f));
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 20f);
            // A ras de suelo pero sin ventana: no hay daño y la barra lo dice.
            int before = health.Current;
            Assert.IsFalse(health.TakeDamage(3));
            Assert.AreEqual(before, health.Current);
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);

            boss.StrikeBig(boss.transform.position);
            yield return UntilMode(MeganeuraBoss.Mode.Grounded, 6f);
            Assert.IsTrue(boss.StunWindowOpen);
            Assert.IsTrue(health.TakeDamage(3));
            // Al llegar al tope (10 % de la vida) se levanta y vuelve a ser invulnerable, aunque quedara tiempo.
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.10f));
            Assert.IsFalse(boss.StunWindowOpen);
            Assert.IsTrue(health.IsInvulnerable);
            yield return UntilMode(MeganeuraBoss.Mode.Climbing, 8f);
        }

        [UnityTest]
        public IEnumerator ElVenenoVirulentoQuitaDosCadaMedioSegundo()
        {
            var go = new GameObject("Victim");
            extras.Add(go);
            var health = go.AddComponent<Health>();
            health.SetMax(40, healDifference: false);
            health.ResetHealth();
            var poison = Poison.ApplyVenom(health, 2, 0.5f, 3f);
            Assert.IsTrue(poison.IsVenom);
            Assert.AreEqual(0.5f, poison.TickSeconds);
            yield return new WaitForSeconds(1.3f);
            Assert.AreEqual(4, 40 - health.Current, 2, "2 de daño en los golpes de 0,5 s y 1,0 s: unos 4 de daño en 1,3 s");
        }

        [Test]
        public void LasFeromonasConfundenALasNinfasYElLabioDanaAlOtroEnemigo()
        {
            var nymphA = Object.Instantiate(boss.NymphInUse, room.transform);
            var nymphB = Object.Instantiate(boss.NymphInUse, room.transform);
            nymphB.transform.position = nymphA.transform.position + new Vector3(1.5f, 0f, 0f);
            var confusion = Primordia.Mutations.Confusion.Apply(nymphA.Health, 5f, false, null, null, null);
            Assert.IsNotNull(confusion, "las ninfas tienen que poder confundirse");
            Assert.IsTrue(Primordia.Mutations.Confusion.IsConfused(nymphA));
            Assert.IsNotNull(Primordia.Mutations.Confusion.Apply(nymphB.Health, 5f, true, null, null, null));
        }

        MeganeuraCorpse KillForDeath(float altitude, bool burnt = false)
        {
            boss.Altitude = altitude;
            boss.Burnt = burnt;
            boss.Health.IsInvulnerable = false;
            boss.Health.TakeDamage(boss.Health.Max);
            Assert.AreEqual(MeganeuraBoss.Mode.Dead, boss.CurrentMode);
            Assert.IsFalse(boss.GetComponent<Collider2D>().enabled);
            var corpse = room.GetComponentInChildren<MeganeuraCorpse>();
            Assert.IsNotNull(corpse);
            Assert.AreEqual(room.transform, corpse.transform.parent);
            return corpse;
        }

        IEnumerator CheckAirDeath(bool burnt)
        {
            // La charca nueva debe ganar a las tres iniciales cuando es la más cercana.
            boss.SendMessage("GrowPuddles");
            Vector2 expected = (Vector2)room.transform.position + new Vector2(11f, -1f);
            boss.transform.position = expected + Vector2.right;
            boss.GetComponent<Rigidbody2D>().position = boss.transform.position;
            var corpse = KillForDeath(16f, burnt);
            string suffix = burnt ? "_Burnt" : "";
            var wing = corpse.transform.Find("WingDetached");
            Assert.IsNotNull(wing);
            Assert.AreEqual(1, corpse.GetComponentsInChildren<DirectionalSprite>().Count(d => d.name == "WingDetached"));
            Assert.AreEqual("Meganeura_WingDetached" + suffix, wing.GetComponent<DirectionalSprite>().Current);
            Assert.AreEqual("Meganeura_DeathFall" + suffix, corpse.transform.Find("Body").GetComponent<DirectionalSprite>().Current);
            Assert.AreEqual(expected, corpse.Impact);
            yield return new WaitForSeconds(1.35f);
            Assert.IsTrue(boss == null, "Enemy se destruye como antes, pero la presentacion sigue viva.");
            Assert.IsFalse(corpse.Landed);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(corpse.Landed);
            Assert.AreEqual(expected.x, corpse.transform.position.x, 0.01f);
            Assert.AreEqual(expected.y, corpse.transform.position.y, 0.01f);
            var body = corpse.transform.Find("Body").GetComponent<DirectionalSprite>();
            Assert.AreEqual("Meganeura_Dead" + suffix, body.Current);
            Assert.AreEqual(0f, body.Angle);
            var ripple = corpse.transform.Find("DeadRipple").GetComponent<SpriteRenderer>();
            Assert.Less(ripple.sortingOrder, body.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.IsFalse(corpse.transform.Find("Shadow").GetComponent<SpriteRenderer>().enabled);
            Assert.AreEqual(0f, corpse.Altitude);
            var splash = corpse.GetComponentsInChildren<SpriteRenderer>().First(r => r.sprite != null && r.sprite.name.StartsWith("Fx_Meganeura_Splash"));
            Assert.AreEqual(26f, splash.sprite.pivot.y, 0.01f);
            Assert.AreEqual(expected.y, splash.transform.position.y, 0.01f);
            Object.DestroyImmediate(room);
            Assert.IsTrue(corpse == null);
        }

        [UnityTest]
        public IEnumerator MuerteEnElAire_AlaCharcaNuevaYRestosPersistentes() => CheckAirDeath(false);

        [UnityTest]
        public IEnumerator MuerteQuemada_UsaLasTresHojasBurnt() => CheckAirDeath(true);

        [UnityTest]
        public IEnumerator MuerteEnElSuelo_PrimeroSubeYSinTormentaNoCreaAmbiente()
        {
            var corpse = KillForDeath(0f);
            Assert.IsNull(corpse.transform.Find("WingDetached"));
            yield return new WaitForSeconds(0.3f);
            Assert.That(corpse.Altitude, Is.GreaterThan(0f).And.LessThan(16f));
            Assert.IsNull(corpse.transform.Find("WingDetached"));
            yield return new WaitForSeconds(0.45f);
            Assert.IsNotNull(corpse.transform.Find("WingDetached"));
            Assert.IsNull(room.GetComponentInChildren<MeganeuraStorm>());
            yield return new WaitForSeconds(2.3f);
            Assert.IsTrue(corpse.Landed);
        }

        [UnityTest]
        public IEnumerator Muerte_LimpiaInvocacionesMarcasRayosYCorutinas()
        {
            boss.BeginFight();
            var fx = boss.EffectsInUse;
            var egg = MeganeuraEgg.Spawn(room.transform, boss.transform.position, fx, boss.NymphInUse, 10f);
            var nymph = Object.Instantiate(boss.NymphInUse, room.transform);
            boss.SendMessage("GrowPuddles");
            boss.StrikeBig(boss.transform.position);
            var mark = SpriteFramePlayer.PlayOnce(fx.boltSmallMark, 1f, boss.transform.position, 26);
            extras.Add(mark.gameObject);
            var corpse = KillForDeath(16f);
            yield return null;
            Assert.AreEqual(0, MeganeuraEgg.Alive.Count);
            Assert.IsTrue(egg == null || !MeganeuraEgg.Alive.Contains(egg));
            Assert.IsTrue(nymph == null || nymph.Health.IsDead);
            Assert.IsTrue(mark == null);
            Assert.IsNull(typeof(MeganeuraBoss).GetField("fight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(boss));
            Assert.IsFalse(Object.FindObjectsByType<SpriteFramePlayer>().Any(p => p.name == "MeganeuraPuddleElectric"));
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(corpse.Landed);
            Assert.IsFalse(Object.FindObjectsByType<GameObject>().Any(g => g.name == "BigBoltMark" || g.name == "LooseBoltMark"));
        }

        [UnityTest]
        public IEnumerator MuerteConTormenta_AclaraYLloviznaYVaporSeRetiranSolos()
        {
            boss.SendMessage("EnsureStorm");
            var storm = boss.Storm;
            Assert.AreEqual(room.transform, storm.transform.parent);
            storm.SetRain(true);
            storm.SetAsh(true);
            storm.Darken(0.5f, 0.01f);
            yield return new WaitForSeconds(0.05f);
            Assert.Greater(storm.DarkAlpha, 0.1f);
            KillForDeath(16f);
            Assert.IsFalse(storm.Raining);
            Assert.IsFalse(storm.AshOn);
            Assert.IsTrue(storm.Drizzling);
            Assert.AreEqual(10, storm.SteamActive);
            var steam = storm.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Steam");
            Assert.AreEqual(2, steam.sortingOrder);
            yield return new WaitForSeconds(3.2f);
            Assert.AreEqual(0f, storm.DarkAlpha, 0.001f);
            Assert.IsTrue(storm.Drizzling);
            Assert.AreEqual(10, storm.SteamActive);
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(0, storm.SteamActive);
            Assert.IsTrue(storm.Drizzling);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(storm.Drizzling);
            Assert.IsFalse(storm.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.name == "Tile"));
        }

        [UnityTest]
        public IEnumerator Muerte_RecompensaInmediataEnArenaCenterAunqueCaigaEnOtraParte()
        {
            var mutation = ScriptableObject.CreateInstance<Primordia.Mutations.StatMutation>();
            var pool = ScriptableObject.CreateInstance<Primordia.Mutations.MutationPool>();
            try
            {
                mutation.ConfigureBase("Prueba muerte", "", Primordia.Mutations.MutationSlot.Skin, Color.white);
                pool.Configure(new[] { mutation });
                var template = new GameObject("PickupTemplate", typeof(CircleCollider2D));
                extras.Add(template);
                var pickup = template.AddComponent<Primordia.Mutations.MutationPickup>();
                var drop = boss.gameObject.AddComponent<Primordia.Mutations.MutationDrop>();
                drop.Configure(pool, pickup, 1);
                Vector2 center = boss.ArenaCenter;
                var corpse = KillForDeath(16f);
                Assert.AreEqual(1, drop.Dropped.Count);
                var reward = drop.Dropped[0];
                Assert.AreEqual(center + Vector2.up * 3f, (Vector2)reward.transform.position);
                Assert.IsFalse(corpse.Landed);
                yield return new WaitForSeconds(2.5f);
                Assert.IsNotNull(reward);
                Assert.AreEqual(center + Vector2.up * 3f, (Vector2)reward.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(pool);
                Object.DestroyImmediate(mutation);
            }
        }
        [UnityTest]
        public IEnumerator Lluvia_ApagaFuegosATripleVelocidadSoloDentroDeLaSala()
        {
            var previous = ReactionKit.Current;
            ReactionKit.Current = UnityEditor.AssetDatabase.LoadAssetAtPath<ReactionKit>("Assets/_Primordia/Data/Rooms/ReactionKit_Floor3.asset");
            try
            {
                boss.SendMessage("EnsureStorm");
                boss.Storm.SetRain(true);
                Vector2 center = room.transform.position;
                var wet = GroundFire.Spawn(center + new Vector2(-5f, 0f), 4f, false);
                var outside = GroundFire.Spawn(center + new Vector2(100f, 0f), 4f, false);
                Assert.IsNotNull(wet);
                Assert.IsNotNull(outside);
                extras.Add(wet.gameObject);
                extras.Add(outside.gameObject);
                yield return new WaitForSeconds(1.7f);
                Assert.IsTrue(wet == null || !wet.IsBurning, "Con lluvia termina antes de los 2 s, la mitad de los 4 s normales.");
                Assert.IsTrue(outside.IsBurning, "La otra sala conserva su tiempo normal.");
                boss.Storm.SetRain(false);
                var dry = GroundFire.Spawn(center + new Vector2(-8f, 0f), 4f, false);
                extras.Add(dry.gameObject);
                yield return new WaitForSeconds(1.7f);
                Assert.IsTrue(dry.IsBurning, "Sin lluvia no se acelera el apagado.");
            }
            finally
            {
                ReactionKit.Current = previous;
            }
        }

        [UnityTest]
        public IEnumerator Lluvia_ApagaElQuemadoDelJugadorYNoTocaOtrasSalasNiAlasBurnt()
        {
            Vector2 center = room.transform.position;
            var health = Player(center);
            var controller = health.gameObject.AddComponent<Primordia.Player.PlayerController>();
            controller.readInput = false;
            var burn = health.gameObject.AddComponent<PlayerBurn>();
            burn.SetOnFire(0.1f);
            boss.SendMessage("EnsureStorm");
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(burn.Bar, 0.1f, "Sin lluvia, la barra no baja sola.");
            boss.Burnt = true;
            boss.Storm.SetRain(true);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0f, burn.Bar);
            Assert.IsFalse(burn.Burning);
            Assert.IsTrue(boss.Burnt);
            health.transform.position = center + Vector2.right * 100f;
            burn.SetOnFire(0.1f);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(burn.Bar, 0.1f, "La lluvia no apaga al jugador de otra sala.");
            health.transform.position = center;
            boss.Storm.SetRain(false);
            yield return new WaitForSeconds(0.4f);
            Assert.Greater(burn.Bar, 0.1f);
        }

        IEnumerator HighStorm()
        {
            Player((Vector2)room.transform.position + new Vector2(-14f, -8f));
            boss.Health.TakeDamage(Mathf.CeilToInt(boss.Health.Max * 0.75f));
            boss.BeginFight();
            yield return UntilMode(MeganeuraBoss.Mode.High, 14f);
        }

        [UnityTest]
        public IEnumerator Relampago_RevelaSiluetaSoloConDestelloFuerteYEnElAire()
        {
            yield return HighStorm();
            var storm = boss.Storm;
            Assert.IsNull(storm.transform.Find("MeganeuraSilhouette"));
            storm.Flash(0.49f);
            Assert.IsNull(storm.transform.Find("MeganeuraSilhouette"));
            var body = boss.transform.Find("Body").GetComponent<DirectionalSprite>();
            body.Angle = 135f;
            storm.Flash(0.5f);
            var silhouette = storm.transform.Find("MeganeuraSilhouette");
            Assert.IsNotNull(silhouette);
            Assert.AreEqual(body.transform.position, silhouette.position);
            Assert.AreEqual(body.Angle, silhouette.GetComponent<DirectionalSprite>().Angle);
            Assert.AreEqual("Meganeura_Silhouette_Fly", silhouette.GetComponent<DirectionalSprite>().Current);
            Assert.GreaterOrEqual(silhouette.GetComponent<SpriteRenderer>().sortingOrder, 26);
            Assert.IsNull(silhouette.GetComponent<Collider2D>());
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(silhouette == null);
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 12f);
            storm.Flash(0.85f);
            Assert.IsNull(storm.transform.Find("MeganeuraSilhouette"));
        }

        [UnityTest]
        public IEnumerator Tormenta_CuerpoOcultoEnAltoYVisibleAlBajar()
        {
            yield return HighStorm();
            yield return null;
            var body = boss.transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.IsFalse(body.enabled);
            Assert.IsTrue(boss.transform.Find("Shadow").GetComponent<SpriteRenderer>().enabled);
            boss.Storm.Flash(0.85f);
            yield return null;
            Assert.IsFalse(body.enabled, "El destello revela la silueta, no el cuerpo normal.");
            yield return UntilMode(MeganeuraBoss.Mode.Hover, 12f);
            yield return null;
            Assert.IsTrue(body.enabled);
        }

        [UnityTest]
        public IEnumerator Tormenta_NubesSeMuevenBajoPersonajesYSeDesvanecenAlCalmar()
        {
            boss.SendMessage("EnsureStorm");
            var storm = boss.Storm;
            var cloud = storm.transform.Find("CloudShadow");
            Assert.IsNotNull(cloud);
            var renderer = cloud.GetComponent<SpriteRenderer>();
            Assert.AreEqual(new Vector2(256f, 256f), renderer.sprite.rect.size);
            Assert.Less(renderer.sortingOrder, 4);
            Assert.Greater(renderer.sortingOrder, 1);
            Vector3 start = cloud.localPosition;
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(Vector3.Distance(start, cloud.localPosition), 0.1f);
            storm.Calm(3f);
            yield return new WaitForSeconds(1.5f);
            Assert.That(renderer.color.a, Is.GreaterThan(0.2f).And.LessThan(0.8f));
            yield return new WaitForSeconds(1.7f);
            Assert.IsTrue(cloud == null);
            Assert.IsNull(storm.transform.Find("CloudShadow"));
        }

    }
}
#endif
