using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Biomass;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Clases de linaje remodeladas del piso 2 y la Carroña del Carroñero.</summary>
    public class Floor2LineageTests
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
            foreach (var remains in Object.FindObjectsByType<CarrionRemains>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(remains.gameObject);
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

        /// <summary>Completa un linaje de esa clase con 4 mutaciones sin efecto de ese piso.</summary>
        LineageData Complete(LineageClass lineageClass, int floor = 2)
        {
            var parts = new MutationData[]
            {
                Mutation(MutationSlot.Mouth, floor), Mutation(MutationSlot.Limbs, floor),
                Mutation(MutationSlot.Skin, floor), Mutation(MutationSlot.Organ, floor),
            };
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure(lineageClass.ToString(), lineageClass, parts, "");
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

        CarrionRemains RemainsPrefab(int value = 1)
        {
            var go = Track(new GameObject("RemainsPrefab"));
            go.SetActive(false);
            var remains = go.AddComponent<CarrionRemains>();
            remains.Configure(value, 0.55f);
            return remains;
        }

        void ConfigureCarrion(CarrionRemains prefab)
        {
            var settings = new LineageBenefitSettings { remainsPrefab = prefab };
            lineage.ConfigureBenefits(settings);
        }

        // ---------- piso de cada linaje ----------

        [Test]
        public void Lineage_TakesTheFloorOfItsMutations()
        {
            Assert.AreEqual(2, Complete(LineageClass.Herbivore).Floor);
            Assert.AreEqual("Carroña", MutationText.ClassBenefit(LineageClass.Scavenger, 2).name);
            StringAssert.Contains("12", MutationText.ClassBenefit(LineageClass.Herbivore, 2).description);
            StringAssert.Contains("10", MutationText.ClassBenefit(LineageClass.Herbivore).description);
        }

        // ---------- Carnívoro ----------

        [Test]
        public void Carnivore_Floor2_StunnedAndShatteredCountAsWounded()
        {
            Complete(LineageClass.Carnivore);
            var stunned = CreateEnemy(10);
            stunned.GetComponent<EnemyAI>().Knockback(Vector2.zero, 2f);
            stunned.Health.TakeDamage(2);
            Assert.AreEqual(6, stunned.Health.Current, "Aturdido con toda la vida: cuenta como herido (el doble).");

            var shattered = CreateEnemy(20, new Vector2(-10f, 0f));
            Shatter.Apply(shattered.Health, 3f, 1f);
            shattered.Health.TakeDamage(2);
            Assert.AreEqual(16, shattered.Health.Current, "Con Quebrar cuenta como herido.");
        }

        [Test]
        public void Carnivore_Floor1_StunnedDoesNotCount()
        {
            Complete(LineageClass.Carnivore, floor: 1);
            var enemy = CreateEnemy(10);
            enemy.GetComponent<EnemyAI>().Knockback(Vector2.zero, 2f);
            enemy.Health.TakeDamage(2);
            Assert.AreEqual(8, enemy.Health.Current);
        }

        // ---------- Herbívoro ----------

        [UnityTest]
        public IEnumerator Herbivore_Floor2_HealsEvery12_AndKeepsUpTo2GreenHearts()
        {
            var health = mutations.Health;
            health.SetMax(6);
            Complete(LineageClass.Herbivore);
            var biomass = player.GetComponent<PlayerBiomass>();

            health.TakeDamage(1);
            biomass.Add(11);
            Assert.AreEqual(5, health.Current, "Con 11 aún no cura.");
            biomass.Add(1);
            Assert.AreEqual(6, health.Current, "Con 12, medio corazón.");

            biomass.Add(12);
            biomass.Add(12);
            biomass.Add(12);
            Assert.AreEqual(2, health.Barrier, "Con la vida llena guarda hasta 2 corazones verdes.");

            // El beneficio se quita con Destroy, al final del cuadro.
            mutations.Unequip(MutationSlot.Organ);
            yield return null;
            Assert.AreEqual(0, health.Barrier, "Al perder el linaje se pierden los que dio.");
        }

        // ---------- Venenoso ----------

        [UnityTest]
        public IEnumerator Venomous_Floor2_SlowsPoisonedEnemies()
        {
            Complete(LineageClass.Venomous);
            var poisoned = CreateEnemy(30);
            var clean = CreateEnemy(30, new Vector2(-10f, 0f));
            Poison.Apply(poisoned.Health, 1, 3f);
            yield return null;
            yield return null;
            Assert.IsTrue(poisoned.GetComponent<EnemyAI>().IsSlowed, "El envenenado va más lento.");
            Assert.IsFalse(clean.GetComponent<EnemyAI>().IsSlowed);
        }

        [UnityTest]
        public IEnumerator Venomous_Floor1_DoesNotSlow()
        {
            Complete(LineageClass.Venomous, floor: 1);
            var poisoned = CreateEnemy(30);
            Poison.Apply(poisoned.Health, 1, 3f);
            yield return null;
            yield return null;
            Assert.IsFalse(poisoned.GetComponent<EnemyAI>().IsSlowed);
        }

        // ---------- Carroñero ----------

        [UnityTest]
        public IEnumerator Scavenger_DyingEnemyLeavesRemains_ThatGiveDamageWhenEaten()
        {
            ConfigureCarrion(RemainsPrefab());
            Complete(LineageClass.Scavenger);
            var carrion = lineage.BenefitOf<CarrionBenefit>();
            Assert.IsNotNull(carrion);

            var victim = CreateEnemy(1, new Vector2(3f, 0f));
            victim.Health.TakeDamage(1);
            yield return null;
            Assert.AreEqual(1, carrion.Spawned.Count, "El enemigo muerto deja restos.");
            Assert.AreEqual(0, carrion.Stacks);

            player.transform.position = new Vector3(3f, 0f, 0f);
            yield return null;
            Assert.AreEqual(1, carrion.Stacks, "Al pasar por encima se los come.");
            Assert.AreEqual(0, carrion.Spawned.Count);

            var target = CreateEnemy(100, new Vector2(-10f, 0f));
            target.Health.TakeDamage(10);
            Assert.AreEqual(88, target.Health.Current, "+20 % de daño.");
        }

        [Test]
        public void Scavenger_StacksUpToThree()
        {
            ConfigureCarrion(RemainsPrefab());
            Complete(LineageClass.Scavenger);
            var carrion = lineage.BenefitOf<CarrionBenefit>();
            carrion.AddStacks(2);
            carrion.AddStacks(2);
            Assert.AreEqual(3, carrion.Stacks);
            Assert.AreEqual(1.6f, carrion.Multiplier, 0.001f);
        }

        [UnityTest]
        public IEnumerator Scavenger_RemainsDisappearAfterTheirTime_AndWhenTheLineageIsLost()
        {
            var settings = new LineageBenefitSettings { remainsPrefab = RemainsPrefab(), remainsDuration = 0.1f };
            lineage.ConfigureBenefits(settings);
            Complete(LineageClass.Scavenger);
            var carrion = lineage.BenefitOf<CarrionBenefit>();

            CreateEnemy(1, new Vector2(5f, 0f)).Health.TakeDamage(1);
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<CarrionRemains>(FindObjectsSortMode.None).Length);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(0, Object.FindObjectsByType<CarrionRemains>(FindObjectsSortMode.None).Length, "Se van al acabar su tiempo.");

            CreateEnemy(1, new Vector2(5f, 0f)).Health.TakeDamage(1);
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<CarrionRemains>(FindObjectsSortMode.None).Length);
            mutations.Unequip(MutationSlot.Mouth);
            yield return null;
            Assert.IsNull(lineage.BenefitOf<CarrionBenefit>());
            Assert.AreEqual(0, Object.FindObjectsByType<CarrionRemains>(FindObjectsSortMode.None).Length, "Al perder el linaje se van los restos.");
        }
    }
}
