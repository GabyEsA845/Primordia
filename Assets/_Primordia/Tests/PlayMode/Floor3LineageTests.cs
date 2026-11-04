using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Biomass;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Clases de linaje del piso 3: el Parásito y su Chupar.</summary>
    public class Floor3LineageTests
    {
        readonly List<Object> created = new();
        GameObject player;
        PlayerMutations mutations;
        PlayerLineage lineage;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player"));
            player.AddComponent<Weapon>().Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            player.AddComponent<PlayerController>().readInput = false;
            player.AddComponent<PlayerBiomass>();
            mutations = player.AddComponent<PlayerMutations>();
            lineage = player.AddComponent<PlayerLineage>();
        }

        [TearDown]
        public void TearDown()
        {
            Parasite.RemoveAll();
            Parasite.Limit = Parasite.DefaultLimit;
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

        StatMutation Mutation(MutationSlot slot, int floor)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(slot.ToString(), "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            mutation.SetFloor(floor);
            return mutation;
        }

        /// <summary>Completa un linaje de esa clase con 4 mutaciones sin efecto del piso 3.</summary>
        LineageData Complete(LineageClass lineageClass, LineageTouch touch = LineageTouch.None)
        {
            var parts = new MutationData[]
            {
                Mutation(MutationSlot.Mouth, 3), Mutation(MutationSlot.Limbs, 3),
                Mutation(MutationSlot.Skin, 3), Mutation(MutationSlot.Organ, 3),
            };
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure(lineageClass.ToString(), lineageClass, parts, "");
            data.ConfigureTouch(touch, touch.ToString(), "");
            foreach (var part in parts)
            {
                part.SetLineages(new[] { data });
                mutations.Equip(part);
            }
            Assert.AreSame(data, lineage.Active);
            return data;
        }

        Enemy CreateEnemy(int maxHealth, Vector2 position = default, float moveSpeed = 0f)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: maxHealth, moveSpeed: moveSpeed, detectionRange: 0f, contactDamage: 0);
            var go = Track(new GameObject("Enemy") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) });
            go.transform.position = position == default ? new Vector3(10f, 0f, 0f) : (Vector3)position;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            enemy.SetData(data);
            return enemy;
        }

        // ---------- Clases remodeladas del piso 3 ----------

        [Test]
        public void Carnivore_Floor3_BurningEnemiesCountAsWounded()
        {
            Complete(LineageClass.Carnivore);
            var enemy = CreateEnemy(100);
            var finisher = player.GetComponent<FinisherBenefit>();

            Assert.IsFalse(finisher.IsWounded(enemy.Health));
            Burn.Apply(enemy.Health, 1, 3);
            Assert.IsTrue(finisher.IsWounded(enemy.Health));
        }

        [Test]
        public void Carnivore_Floor2_BurningEnemiesAreNotWounded()
        {
            var parts = new MutationData[]
            {
                Mutation(MutationSlot.Mouth, 2), Mutation(MutationSlot.Limbs, 2),
                Mutation(MutationSlot.Skin, 2), Mutation(MutationSlot.Organ, 2),
            };
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure("Carnivore2", LineageClass.Carnivore, parts, "");
            foreach (var part in parts)
            {
                part.SetLineages(new[] { data });
                mutations.Equip(part);
            }
            var enemy = CreateEnemy(100);
            Burn.Apply(enemy.Health, 1, 3);

            Assert.IsFalse(player.GetComponent<FinisherBenefit>().IsWounded(enemy.Health));
        }

        [Test]
        public void Venomous_Floor3_KeepsToxinAndAddsThePuddleOnDeath()
        {
            Complete(LineageClass.Venomous);
            var toxin = player.GetComponent<ToxinBenefit>();
            Assert.IsNotNull(toxin);
            Assert.AreEqual(0.8f, toxin.SlowFactor, 0.001f);

            // Sin arte del veneno en la prueba no sale charco, pero morir envenenado no debe fallar.
            var enemy = CreateEnemy(10);
            Poison.Apply(enemy.Health, 1, 3);
            Assert.DoesNotThrow(() => enemy.Health.TakeDamage(50));
        }

        [Test]
        public void Fungal_Floor3_AnEnemyDyingOnAMatSproutsAnother_UpToSix()
        {
            Complete(LineageClass.Fungal);
            var colony = player.GetComponent<ColonyBenefit>();
            colony.Colonize(new Bounds(Vector3.zero, new Vector3(30f, 30f, 1f)));
            int before = colony.Mats.Count;
            Assert.Greater(before, 0);

            var mat = colony.Mats[0];
            var enemy = CreateEnemy(5, mat.transform.position);
            enemy.Health.TakeDamage(10);
            Assert.AreEqual(before + 1, colony.Mats.Count);

            // Fuera de las matas no brota nada.
            var far = CreateEnemy(5, new Vector2(200f, 200f));
            far.Health.TakeDamage(10);
            Assert.AreEqual(before + 1, colony.Mats.Count);
        }

        [Test]
        public void Classes_Floor3_TextsMentionTheNewRules()
        {
            StringAssert.Contains("arden", MutationText.ClassBenefit(LineageClass.Carnivore, 3).description);
            StringAssert.Contains("charco", MutationText.ClassBenefit(LineageClass.Venomous, 3).description);
            StringAssert.Contains("otra", MutationText.ClassBenefit(LineageClass.Fungal, 3).description);
            StringAssert.DoesNotContain("arden", MutationText.ClassBenefit(LineageClass.Carnivore, 2).description);
        }

        // ---------- Toques del piso 3 (tanda 1) ----------

        [Test]
        public void Xenacanthus_ConfusedEnemiesCountAsWounded()
        {
            Complete(LineageClass.Carnivore, LineageTouch.CephalicSpine);
            var enemy = CreateEnemy(100);
            var finisher = player.GetComponent<FinisherBenefit>();

            Assert.IsFalse(finisher.IsWounded(enemy.Health));
            enemy.GetComponent<EnemyAI>().IsConfused = true;
            Assert.IsTrue(finisher.IsWounded(enemy.Health));
        }

        [Test]
        public void Carnivore_WithoutTheTouch_ConfusedEnemiesAreNotWounded()
        {
            Complete(LineageClass.Carnivore);
            var enemy = CreateEnemy(100);
            enemy.GetComponent<EnemyAI>().IsConfused = true;

            Assert.IsFalse(player.GetComponent<FinisherBenefit>().IsWounded(enemy.Health));
        }

        [Test]
        public void Centipede_IsImmuneToAllPoisonAndToxicGas()
        {
            var enemy = CreateEnemy(100);
            Assert.IsNotNull(Poison.Apply(enemy.Health, 1, 3));
            Assert.IsFalse(PoisonImmune.Has(player.transform));

            Complete(LineageClass.Venomous, LineageTouch.HundredLegs);

            Assert.IsTrue(PoisonImmune.Has(player.transform));
            Assert.IsNull(Poison.Apply(mutations.Health, 1, 3));
            Assert.IsFalse(Parasite.Has(mutations.Health));
            // Los enemigos siguen envenenándose con normalidad.
            Assert.IsNotNull(Poison.Apply(CreateEnemy(100).Health, 1, 3));
        }

        [Test]
        public void Centipede_LosingTheLineageRestoresPoison()
        {
            Complete(LineageClass.Venomous, LineageTouch.HundredLegs);
            lineage.enabled = false;
            Object.DestroyImmediate(player.GetComponent<CentipedeTouch>());

            Assert.IsNotNull(Poison.Apply(mutations.Health, 1, 3));
        }

        [UnityTest]
        public IEnumerator Lamprey_WhileSpinning_EachParasiteAttachesAnotherToTheNearestEnemy()
        {
            Complete(LineageClass.Parasite, LineageTouch.SpinningSucker);
            var touch = player.GetComponent<LampreyTouch>();
            Assert.IsNotNull(touch);
            touch.forceSpinning = true;
            var first = CreateEnemy(100, new Vector2(10, 0));
            var near = CreateEnemy(100, new Vector2(11, 0));
            var far = CreateEnemy(100, new Vector2(20, 0));

            first.Health.TakeDamage(15);

            Assert.IsTrue(Parasite.Has(first.Health));
            Assert.IsTrue(Parasite.Has(near.Health));
            Assert.IsFalse(Parasite.Has(far.Health));
            Assert.AreEqual(1, touch.Duplicated);
            // El segundo no se duplica a su vez.
            Assert.AreEqual(2, Parasite.Alive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Lamprey_RaisesTheLimitToSixOnlyWhileSpinning()
        {
            Complete(LineageClass.Parasite, LineageTouch.SpinningSucker);
            var touch = player.GetComponent<LampreyTouch>();

            yield return null;
            Assert.AreEqual(Parasite.DefaultLimit, Parasite.Limit);

            touch.forceSpinning = true;
            yield return null;
            Assert.AreEqual(6, Parasite.Limit);

            touch.forceSpinning = false;
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(Parasite.DefaultLimit, Parasite.Limit);
        }

        [UnityTest]
        public IEnumerator Lamprey_NotSpinning_DoesNotDuplicate()
        {
            Complete(LineageClass.Parasite, LineageTouch.SpinningSucker);
            var first = CreateEnemy(100, new Vector2(10, 0));
            var near = CreateEnemy(100, new Vector2(11, 0));

            first.Health.TakeDamage(15);
            yield return null;

            Assert.AreEqual(1, Parasite.Alive);
            Assert.IsFalse(Parasite.Has(near.Health));
        }

        // ---------- Toques del piso 3 (tanda 2): Aguijón de gas ----------

        static MethaneCloud SpawnCloud(Vector2 position)
        {
            var settings = new MethaneCloud.Settings { damage = 6, lifetime = 8f, speed = 0f, explosionRadius = 2f, fireSeconds = 0f };
            return MethaneCloud.Spawn(position, Vector2.right, settings, null, null, null);
        }

        [Test]
        public void GasSting_ToxifyAlong_TurnsTheMethaneCloudIntoGasInsteadOfExploding()
        {
            var cloud = SpawnCloud(new Vector2(5f, 0f));
            created.Add(cloud.gameObject);
            var enemy = CreateEnemy(20, new Vector2(5.5f, 0f));

            int count = Flammables.ToxifyAlong(Vector2.zero, new Vector2(8f, 0f), 0.25f);

            Assert.AreEqual(1, count);
            Assert.IsTrue(cloud.Exploded);
            // No explotó: el enemigo de al lado sigue entero.
            Assert.AreEqual(20, enemy.Health.Current);
        }

        [Test]
        public void GasSting_ToxifyAlong_IgnoresMethaneOffTheBeam()
        {
            var cloud = SpawnCloud(new Vector2(5f, 4f));
            created.Add(cloud.gameObject);

            Assert.AreEqual(0, Flammables.ToxifyAlong(Vector2.zero, new Vector2(8f, 0f), 0.25f));
            Assert.IsFalse(cloud.Exploded);
        }

        [Test]
        public void GasSting_OtherAttacksStillMakeTheMethaneExplode()
        {
            var cloud = SpawnCloud(new Vector2(5f, 0f));
            created.Add(cloud.gameObject);
            var enemy = CreateEnemy(20, new Vector2(5.5f, 0f));

            cloud.Health.TakeDamage(1);

            Assert.IsTrue(cloud.Exploded);
            Assert.AreEqual(14, enemy.Health.Current);
        }

        [Test]
        public void MethaneSac_HitBoxCoversTheWholeDrawing_NotJustTheBase()
        {
            var parent = Track(new GameObject("Props"));
            var sac = MethaneSac.Create(parent.transform, Vector2.zero, new Sprite[0], new Sprite[0], null);
            Physics2D.SyncTransforms();
            int mask = LayerMask.GetMask(CombatLayers.Enemy);

            // A 1 u de la base, por encima del bloqueo (0,6 u), también recibe los golpes.
            Assert.IsNotNull(Physics2D.OverlapPoint(new Vector2(0f, 1.0f), mask), "arriba");
            Assert.IsNotNull(Physics2D.OverlapPoint(new Vector2(0f, 0.3f), mask), "base");
            Assert.IsNull(Physics2D.OverlapPoint(new Vector2(0f, 1.6f), mask), "por encima del dibujo");
            Assert.IsNotNull(sac);
        }

        // ---------- Subpaso 5: reserva del piso 3 ----------

        MutationPool Floor3Pool(out StatMutation[] others, out StatMutation mouth)
        {
            var list = new List<MutationData>();
            var extra = new List<StatMutation>();
            foreach (var slot in new[] { MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ })
            {
                var m = Mutation(slot, 3);
                list.Add(m);
                extra.Add(m);
            }
            mouth = Mutation(MutationSlot.Mouth, 3);
            list.Add(mouth);
            others = extra.ToArray();
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(list, newOffersNewMouths: false);
            return pool;
        }

        [Test]
        public void Floor3Pool_OffersTheFloorMutations_ButNotNewMouths()
        {
            var pool = Floor3Pool(out var others, out var mouth);

            // Sin llevar ninguna Boca del piso 3, no se ofrece (la Boca llega por la sala de paso).
            var offer = pool.PickOffer(10, mutations);

            foreach (var mutation in others)
                CollectionAssert.Contains(offer, mutation);
            CollectionAssert.DoesNotContain(offer, mouth);
        }

        [Test]
        public void Floor3Pool_OffersTheUpgradeOfTheMouthYouCarry_AndNothingFromOtherFloors()
        {
            var pool = Floor3Pool(out _, out var mouth);
            mutations.Equip(mouth);
            // Una mutación del piso 1 que se trae: no está en la reserva, pero su mejora sí se ofrece.
            var carried = Mutation(MutationSlot.Skin, 1);
            mutations.Equip(carried);

            var offer = pool.PickOffer(10, mutations);

            // La del piso 1 no es una opción nueva y, sin subir de nivel, tampoco sale: nada del piso 1 se cuela.
            Assert.IsEmpty(offer.FindAll(m => m.Floor < 3 && !mutations.CanLevelUp(m)));
        }

        // ---------- Toques del piso 3 (tanda 4): Acecho del fango del Eryops ----------

        [UnityTest]
        public IEnumerator Eryops_InTheMud_BoostsSpeedForFiveSecondsAfterLeaving()
        {
            lineage.ConfigureBenefits(new LineageBenefitSettings { eryopsSpeedBonus = 0.3f, eryopsBuffSeconds = 0.6f });
            Complete(LineageClass.Carnivore, LineageTouch.MudStalker);
            var touch = player.GetComponent<EryopsTouch>();
            var controller = player.GetComponent<PlayerController>();
            Assert.IsNotNull(touch);

            yield return null;
            Assert.AreEqual(1f, controller.BoostSpeedFactor);

            touch.forceInMud = true;
            yield return null;
            Assert.IsTrue(touch.Buffed);
            Assert.AreEqual(1.3f, controller.BoostSpeedFactor, 0.001f);

            // Sale del lodo: la mejora sigue un rato y luego se acaba.
            touch.forceInMud = false;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1.3f, controller.BoostSpeedFactor, 0.001f);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1f, controller.BoostSpeedFactor);

            // Volver a entrar la renueva.
            touch.forceInMud = true;
            yield return null;
            Assert.AreEqual(1.3f, controller.BoostSpeedFactor, 0.001f);
        }

        [Test]
        public void Eryops_LosingTheLineageRestoresSpeedAndSinking()
        {
            Complete(LineageClass.Carnivore, LineageTouch.MudStalker);
            var sink = player.AddComponent<PlayerMudSink>();
            var controller = player.GetComponent<PlayerController>();
            var touch = player.GetComponent<EryopsTouch>();
            touch.Configure(0.3f, 0.6f, 5f);
            Assert.IsTrue(sink.SinkImmune);

            Object.DestroyImmediate(touch);

            Assert.IsFalse(sink.SinkImmune);
            Assert.AreEqual(1f, controller.BoostSpeedFactor);
        }

        [Test]
        public void MudBodyPaint_DryMud_UsesTheDryTonesWhereTheNoiseIsBelowTheDryness()
        {
            const int size = 4;
            var source = new MudBodyPaint.Data
            {
                width = size, height = size, pixels = new Color32[size * size], minRow = 0, maxRow = size - 1, lo = 50f, hi = 200f,
            };
            for (int i = 0; i < source.pixels.Length; i++)
                source.pixels[i] = new Color32(120, 80, 60, 255);
            var noise = new Color32[32 * 32];
            for (int i = 0; i < noise.Length; i++)
                noise[i] = new Color32(0, 0, 0, 255);
            var tables = new MudBodyPaint.Tables
            {
                noise = noise, noiseSize = 32, texIndex = new int[16 * 16], texSize = 16, dryIndex = new int[16 * 16],
            };
            for (int i = 0; i < tables.texIndex.Length; i++)
                tables.texIndex[i] = 2;

            var wet = new Color32[size * size];
            var dried = new Color32[size * size];
            MudBodyPaint.Paint(source, wet, new MudBodyPaint.Settings { level = 10f, feetRow = 0 }, tables);
            MudBodyPaint.Paint(source, dried, new MudBodyPaint.Settings { level = 10f, feetRow = 0, dry = 1f }, tables);

            // Sin secar, el tono 2 del lodo (0x4a,0x33,0x24); seco, el tono 0 del barro seco (46,36,28).
            Assert.AreEqual(new Color32(0x4a, 0x33, 0x24, 255), wet[0]);
            Assert.AreEqual(new Color32(46, 36, 28, 255), dried[0]);
        }

        // ---------- Toques del piso 3 (tanda 3): Zombi del Cordyceps ----------

        [Test]
        public void Health_LethalInterceptor_LeavesOneLifeInsteadOfDying()
        {
            var enemy = CreateEnemy(10);
            System.Func<Health, bool> interceptor = h => h == enemy.Health;
            Health.AddLethalInterceptor(interceptor);
            try
            {
                enemy.Health.TakeDamage(50);
                Assert.IsFalse(enemy.Health.IsDead);
                Assert.AreEqual(1, enemy.Health.Current);
            }
            finally
            {
                Health.RemoveLethalInterceptor(interceptor);
            }
            enemy.Health.TakeDamage(5);
            Assert.IsTrue(enemy.Health.IsDead);
        }

        Enemy ConfusedEnemy(int health, Vector2 position)
        {
            var enemy = CreateEnemy(health, position, moveSpeed: 2f);
            Assert.IsNotNull(Confusion.Apply(enemy.Health, 10f, false, null, null, null));
            return enemy;
        }

        [UnityTest]
        public IEnumerator Zombie_ConfusedEnemyThatWouldDie_RisesAndCannotBeHurt()
        {
            Complete(LineageClass.Fungal, LineageTouch.Zombie);
            var touch = player.GetComponent<ZombieTouch>();
            var enemy = ConfusedEnemy(10, new Vector2(10f, 0f));
            CreateEnemy(100, new Vector2(14f, 0f), moveSpeed: 2f);

            enemy.Health.TakeDamage(50);

            Assert.IsFalse(enemy.Health.IsDead);
            Assert.AreEqual(1, enemy.Health.Current);
            Assert.IsNotNull(enemy.GetComponent<ZombieAlly>());
            Assert.AreEqual(1, touch.Raised);

            yield return null;
            Assert.IsFalse(enemy.Health.TakeDamage(5), "intocable mientras es aliado");
            Assert.IsFalse(enemy.Health.IsDead);
        }

        [UnityTest]
        public IEnumerator Zombie_FallsForRealWhenItsTimeIsUp()
        {
            lineage.ConfigureBenefits(new LineageBenefitSettings { zombieSeconds = 0.6f });
            Complete(LineageClass.Fungal, LineageTouch.Zombie);
            var enemy = ConfusedEnemy(10, new Vector2(10f, 0f));
            CreateEnemy(100, new Vector2(14f, 0f), moveSpeed: 2f);
            var health = enemy.Health;
            enemy.Health.TakeDamage(50);
            Assert.IsFalse(health.IsDead);

            yield return new WaitForSeconds(1.3f);

            Assert.IsTrue(health == null || health.IsDead);
        }

        [UnityTest]
        public IEnumerator Zombie_FallsAtOnceWhenThereIsNobodyLeftToFight()
        {
            Complete(LineageClass.Fungal, LineageTouch.Zombie);
            var enemy = ConfusedEnemy(10, new Vector2(10f, 0f));
            var health = enemy.Health;
            health.TakeDamage(50);
            Assert.IsFalse(health.IsDead);

            // Pasada la subida (0,5 s) y sin más enemigos, cae sin esperar los 4 s.
            yield return new WaitForSeconds(0.9f);

            Assert.IsTrue(health == null || health.IsDead);
        }

        [Test]
        public void Zombie_NotConfusedOrNotMobile_DiesNormally()
        {
            Complete(LineageClass.Fungal, LineageTouch.Zombie);
            var plain = CreateEnemy(10, new Vector2(10f, 0f), moveSpeed: 2f);
            plain.Health.TakeDamage(50);
            Assert.IsTrue(plain.Health.IsDead, "sin confundir");

            var still = CreateEnemy(10, new Vector2(12f, 0f), moveSpeed: 0f);
            Confusion.Apply(still.Health, 10f, false, null, null, null);
            still.Health.TakeDamage(50);
            Assert.IsTrue(still.Health.IsDead, "inmóvil (planta o trampa)");
        }

        [UnityTest]
        public IEnumerator Zombie_AlliesAreNotTargetsOfConfusedEnemies()
        {
            Complete(LineageClass.Fungal, LineageTouch.Zombie);
            var ally = ConfusedEnemy(10, new Vector2(10f, 0f));
            ally.Health.TakeDamage(50);
            var other = ConfusedEnemy(100, new Vector2(11f, 0f));

            // La confusión busca objetivo en su primer Update.
            yield return null;
            var confusion = other.GetComponent<Confusion>();
            // El único otro enemigo es un aliado: no hay a quién atacar.
            Assert.IsNull(confusion.Target);
        }

        // ---------- Toques del piso 3 (tanda 2c): matas de hongo ----------

        MyceliumPatch MatPrefab(string prefabName)
        {
            var go = Track(new GameObject(prefabName));
            go.AddComponent<SpriteRenderer>();
            return go.AddComponent<MyceliumPatch>();
        }

        [Test]
        public void Fungal_Floor3_MatsUseTheVariants_AndSproutsAreLarge()
        {
            Complete(LineageClass.Fungal);
            var colony = player.GetComponent<ColonyBenefit>();
            colony.SetVariants(new[] { MatPrefab("MatM") }, new[] { MatPrefab("MatL") }, 1.5f);
            colony.Colonize(new Bounds(Vector3.zero, new Vector3(30f, 30f, 1f)));
            int before = colony.Mats.Count;
            Assert.Greater(before, 0);
            foreach (var mat in colony.Mats)
            {
                StringAssert.StartsWith("MatM", mat.name);
                Assert.AreEqual(1.2f, mat.Current.radius, 0.001f);
            }

            var enemy = CreateEnemy(5, colony.Mats[0].transform.position);
            enemy.Health.TakeDamage(10);

            Assert.AreEqual(before + 1, colony.Mats.Count);
            var sprout = colony.Mats[colony.Mats.Count - 1];
            StringAssert.StartsWith("MatL", sprout.name);
            Assert.AreEqual(1.2f * 1.5f, sprout.Current.radius, 0.001f);
        }

        [Test]
        public void Fungal_Floor3_GeneralMatScale_GrowsTheDrawingAndTheRadius()
        {
            Complete(LineageClass.Fungal);
            var colony = player.GetComponent<ColonyBenefit>();
            colony.SetVariants(new[] { MatPrefab("MatM") }, new[] { MatPrefab("MatL") }, 1.5f, 1.5f);
            colony.Colonize(new Bounds(Vector3.zero, new Vector3(40f, 40f, 1f)));
            Assert.Greater(colony.Mats.Count, 0);
            foreach (var mat in colony.Mats)
            {
                Assert.AreEqual(1.2f * 1.5f, mat.Current.radius, 0.001f, "El radio crece con el tamaño general.");
                Assert.AreEqual(1.5f, mat.transform.localScale.x, 0.001f, "Y el dibujo, igual.");
            }

            var enemy = CreateEnemy(5, colony.Mats[0].transform.position);
            enemy.Health.TakeDamage(10);
            var sprout = colony.Mats[colony.Mats.Count - 1];
            Assert.AreEqual(1.2f * 1.5f * 1.5f, sprout.Current.radius, 0.001f, "La grande, por su escala además.");
            Assert.AreEqual(1.5f, sprout.transform.localScale.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator FungusMat_SnaresTheEnemiesOnTopOfIt_AndLetsThemGo()
        {
            var snare = Track(new GameObject("Snare"));
            snare.AddComponent<SpriteRenderer>();
            var go = Track(new GameObject("Mat"));
            go.transform.position = new Vector3(10f, 0f, 0f);
            go.AddComponent<SpriteRenderer>();
            var patch = go.AddComponent<MyceliumPatch>();
            var mat = go.AddComponent<FungusMat>();
            mat.Configure(new Sprite[0], new Sprite[0], new Sprite[0], snare);
            patch.Configure(new MyceliumPatch.Settings { duration = 30f, damagePerSecond = 1, slowFactor = 1f, radius = 1.2f });
            var enemy = CreateEnemy(100, new Vector2(10f, 0f));
            Physics2D.SyncTransforms();

            yield return null;
            yield return null;
            Assert.AreEqual(1, mat.SnareCount);

            enemy.transform.position = new Vector3(40f, 0f, 0f);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.AreEqual(0, mat.SnareCount);
        }

        // ---------- Toques del piso 3 (tanda 2b): Yesquero ----------

        static Sprite[] FakeFrames(int count)
        {
            var texture = new Texture2D(1, 1);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return frames;
        }

        [Test]
        public void Tinder_EachFireGetsOneScorchMark_ThatFollowsItsFire()
        {
            var kit = Track(ScriptableObject.CreateInstance<ReactionKit>());
            kit.ConfigureFire(FakeFrames(2), FakeFrames(2), null);
            kit.ConfigureTinderEffects(FakeFrames(7), FakeFrames(ReactionKit.ScorchFrames * 4));
            var previous = ReactionKit.Current;
            ReactionKit.Current = kit;
            try
            {
                var fire = GroundFire.Spawn(new Vector2(30f, 0f), 3f, harmsPlayer: false);
                Assert.IsNotNull(fire);
                Assert.IsTrue(fire.IsBurning);

                var mark = TinderScorch.Attach(fire);
                Assert.IsNotNull(mark);
                Assert.AreSame(mark, TinderScorch.Attach(fire), "una sola marca por fuego");
                Assert.AreEqual(fire.transform.position, mark.transform.position);
                Assert.IsFalse(mark.IsFading);
                Object.DestroyImmediate(mark.gameObject);
                Object.DestroyImmediate(fire.gameObject);
            }
            finally
            {
                ReactionKit.Current = previous;
            }
        }

        [Test]
        public void Tinder_WithoutScorchArt_NoMarkIsMade()
        {
            var kit = Track(ScriptableObject.CreateInstance<ReactionKit>());
            kit.ConfigureFire(FakeFrames(2), FakeFrames(2), null);
            var previous = ReactionKit.Current;
            ReactionKit.Current = kit;
            try
            {
                var fire = GroundFire.Spawn(new Vector2(30f, 0f), 3f, harmsPlayer: false);
                Assert.IsNull(TinderScorch.Attach(fire));
                Assert.IsNull(TinderScorch.Attach(null));
                Object.DestroyImmediate(fire.gameObject);
            }
            finally
            {
                ReactionKit.Current = previous;
            }
        }

        [Test]
        public void Tinder_BurstOfTheBall_BurnsWhatItReaches()
        {
            var enemy = CreateEnemy(20, new Vector2(10f, 0f));
            Physics2D.SyncTransforms();

            PoisonSpitBurst.Spawn(new Vector2(10f, 0.5f), 6, 1.5f, 3f, player.GetComponent<Weapon>(), Team.Enemy, PoisonSpitBurst.Look.Fire);

            Assert.AreEqual(14, enemy.Health.Current);
            Assert.IsNotNull(enemy.GetComponent<Burn>());
        }

        [Test]
        public void Tinder_NormalBurst_DoesNotBurn()
        {
            var enemy = CreateEnemy(20, new Vector2(10f, 0f));
            Physics2D.SyncTransforms();

            PoisonSpitBurst.Spawn(new Vector2(10f, 0.5f), 6, 1.5f, 3f, player.GetComponent<Weapon>(), Team.Enemy);

            Assert.AreEqual(14, enemy.Health.Current);
            Assert.IsNull(enemy.GetComponent<Burn>());
        }

        // ---------- Toques del piso 3 (tanda 2b): Sanguijuela ----------

        [Test]
        public void Leech_PlayerPuddles_AttachAParasiteAndHurtMore()
        {
            Complete(LineageClass.Parasite, LineageTouch.LeechPool);
            Assert.AreEqual(1, PoisonPuddle.PlayerBonus);
            var enemy = CreateEnemy(100);

            PoisonPuddle.Affect(enemy.GetComponent<CircleCollider2D>(), false, new HashSet<Health>());

            Assert.IsTrue(Parasite.Has(enemy.Health));
            Assert.AreEqual(2, enemy.GetComponent<Poison>().DamagePerTick);
        }

        [Test]
        public void Leech_ScenerySpuddles_DoNotAttachParasites()
        {
            Complete(LineageClass.Parasite, LineageTouch.LeechPool);
            var enemy = CreateEnemy(100);

            // Un charco del escenario (hace daño al jugador): no es del jugador.
            PoisonPuddle.Affect(enemy.GetComponent<CircleCollider2D>(), true, new HashSet<Health>());

            Assert.IsFalse(Parasite.Has(enemy.Health));
            Assert.AreEqual(1, enemy.GetComponent<Poison>().DamagePerTick);
        }

        [Test]
        public void Leech_BurstOfTheBall_ParasitesJumpToTheNearestEnemiesUpToTheMaximum()
        {
            Complete(LineageClass.Parasite, LineageTouch.LeechPool);
            var enemies = new List<Enemy>();
            for (int i = 0; i < 5; i++)
                enemies.Add(CreateEnemy(100, new Vector2(10 + i * 2, 0)));

            PoisonSpitBurst.Spawn(new Vector2(10f, 1f), 0, 1.5f, 0f, player.GetComponent<Weapon>(), Team.Enemy);

            Assert.AreEqual(3, Parasite.Alive);
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(Parasite.Has(enemies[i].Health), $"enemigo {i}");
            Assert.IsFalse(Parasite.Has(enemies[3].Health));
            Assert.IsFalse(Parasite.Has(enemies[4].Health));
        }

        [Test]
        public void Leech_SmallPelletsOfTheLitterDoNotMakeParasitesJump()
        {
            Complete(LineageClass.Parasite, LineageTouch.LeechPool);
            var enemy = CreateEnemy(100);

            // Radio menor de 1: es una bolita de la Camada.
            PoisonSpitBurst.Spawn(Vector2.zero, 0, 0.8f, 0f, player.GetComponent<Weapon>(), Team.Enemy);

            Assert.IsFalse(Parasite.Has(enemy.Health));
        }

        [Test]
        public void Leech_LosingTheTouchRestoresThePuddleDamage()
        {
            Complete(LineageClass.Parasite, LineageTouch.LeechPool);
            Object.DestroyImmediate(player.GetComponent<LeechTouch>());

            Assert.AreEqual(0, PoisonPuddle.PlayerBonus);
        }

        // ---------- Parásito: textos ----------

        [Test]
        public void Parasite_HasItsOwnNameColorAndBenefit()
        {
            Assert.AreEqual("Parásito", MutationText.ClassName(LineageClass.Parasite));
            Assert.AreEqual("Chupar", MutationText.ClassBenefit(LineageClass.Parasite, 3).name);
            Assert.AreNotEqual(Color.white, MutationText.ClassColor(LineageClass.Parasite));
            // Las demás clases siguen con lo del piso 2 hasta que se remodelen.
            Assert.AreEqual("Carroña", MutationText.ClassBenefit(LineageClass.Scavenger, 3).name);
        }

        // ---------- Parásito: Chupar ----------

        [Test]
        public void Parasite_AttachesEveryFifteenDamageDealt()
        {
            Complete(LineageClass.Parasite);
            var enemy = CreateEnemy(100);

            enemy.Health.TakeDamage(10);
            Assert.IsFalse(Parasite.Has(enemy.Health));
            enemy.Health.TakeDamage(5);
            Assert.IsTrue(Parasite.Has(enemy.Health));
            Assert.AreEqual(1, Parasite.Alive);
        }

        [Test]
        public void Parasite_TheBlowThatKillsDoesNotAttachOne()
        {
            Complete(LineageClass.Parasite);
            var enemy = CreateEnemy(20);

            enemy.Health.TakeDamage(30);
            Assert.AreEqual(0, Parasite.Alive);
        }

        [Test]
        public void Parasite_MaximumOfThreeAliveInTotal()
        {
            Complete(LineageClass.Parasite);
            var enemies = new List<Enemy>();
            for (int i = 0; i < 4; i++)
                enemies.Add(CreateEnemy(100, new Vector2(10 + i, 0)));

            foreach (var enemy in enemies)
                enemy.Health.TakeDamage(15);

            Assert.AreEqual(3, Parasite.Alive);
            Assert.IsFalse(Parasite.Has(enemies[3].Health));
        }

        [UnityTest]
        public IEnumerator Parasite_TakesOnePerSecond()
        {
            Complete(LineageClass.Parasite);
            var enemy = CreateEnemy(100);
            enemy.Health.TakeDamage(15);
            Assert.AreEqual(85, enemy.Health.Current);

            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(84, enemy.Health.Current);
            // El daño del propio parásito no cuenta para pegar otro ni para repetirse.
            Assert.AreEqual(1, Parasite.Alive);
        }

        [UnityTest]
        public IEnumerator Parasite_DrainedLifeHealsHalfAHeart()
        {
            // 1 de vida chupada por medio corazón para no esperar 15 s.
            lineage.ConfigureBenefits(new LineageBenefitSettings { parasiteDrainPerHeal = 1, parasiteBossDrainPerHeal = 2 });
            Complete(LineageClass.Parasite);
            mutations.Health.TakeDamage(3);
            int before = mutations.Health.Current;
            var enemy = CreateEnemy(100);
            enemy.Health.TakeDamage(15);

            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(before + 1, mutations.Health.Current);
            Assert.AreEqual(1, player.GetComponent<ParasiteBenefit>().Healed);
        }

        [UnityTest]
        public IEnumerator Parasite_JumpsToAnotherEnemyWhenItsHostDies()
        {
            Complete(LineageClass.Parasite);
            var first = CreateEnemy(40, new Vector2(10, 0));
            var second = CreateEnemy(100, new Vector2(12, 0));
            first.Health.TakeDamage(15);
            Assert.IsTrue(Parasite.Has(first.Health));

            first.Health.TakeDamage(100);
            yield return null;

            Assert.AreEqual(1, Parasite.Alive);
            Assert.IsTrue(Parasite.Has(second.Health));
        }

        [UnityTest]
        public IEnumerator Parasite_IsLostIfThereIsNobodyToJumpTo()
        {
            Complete(LineageClass.Parasite);
            var only = CreateEnemy(40);
            only.Health.TakeDamage(15);

            only.Health.TakeDamage(100);
            yield return null;

            Assert.AreEqual(0, Parasite.Alive);
        }

        [UnityTest]
        public IEnumerator Parasite_AllRemovedWhenTheLineageIsLost()
        {
            Complete(LineageClass.Parasite);
            var enemy = CreateEnemy(100);
            enemy.Health.TakeDamage(15);
            Assert.AreEqual(1, Parasite.Alive);

            mutations.Unequip(MutationSlot.Mouth);
            yield return null;

            Assert.IsNull(lineage.Active);
            Assert.AreEqual(0, Parasite.Alive);
            Assert.IsFalse(Parasite.Has(enemy.Health));
        }
    }
}
