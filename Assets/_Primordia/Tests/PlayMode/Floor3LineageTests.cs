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

        Enemy CreateEnemy(int maxHealth, Vector2 position = default)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: maxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
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
