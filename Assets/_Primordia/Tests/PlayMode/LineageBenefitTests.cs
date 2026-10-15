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
    public class LineageBenefitTests
    {
        readonly List<Object> created = new();
        GameObject player;
        PlayerMutations mutations;
        PlayerLineage lineage;
        PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player"));
            player.AddComponent<Weapon>().Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            player.AddComponent<PlayerBiomass>();
            mutations = player.AddComponent<PlayerMutations>();
            lineage = player.AddComponent<PlayerLineage>();
        }

        [TearDown]
        public void TearDown()
        {
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

        StatMutation Mutation(MutationSlot slot)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(slot.ToString(), "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            return mutation;
        }

        /// <summary>Completa un linaje de esa clase y toque con 4 mutaciones sin efecto.</summary>
        LineageData Complete(LineageClass lineageClass, LineageTouch touch = LineageTouch.None)
        {
            var parts = new MutationData[] { Mutation(MutationSlot.Mouth), Mutation(MutationSlot.Limbs), Mutation(MutationSlot.Skin), Mutation(MutationSlot.Organ) };
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

        Enemy CreateEnemy(int maxHealth)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: maxHealth, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            var go = Track(new GameObject("Enemy") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) });
            go.transform.position = new Vector3(10f, 0f, 0f);
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            enemy.SetData(data);
            return enemy;
        }

        [Test]
        public void Carnivore_DoublesDamage_OnlyToWoundedEnemies()
        {
            Complete(LineageClass.Carnivore);
            var enemy = CreateEnemy(10);

            enemy.Health.TakeDamage(2);
            Assert.AreEqual(8, enemy.Health.Current, "Con más de la mitad de vida, daño normal.");

            enemy.Health.TakeDamage(3);
            Assert.AreEqual(5, enemy.Health.Current);

            enemy.Health.TakeDamage(1);
            Assert.AreEqual(3, enemy.Health.Current, "Con la mitad de vida o menos, el doble.");
        }

        [Test]
        public void Carnivore_DoesNotChangeTheDamageThePlayerTakes()
        {
            Complete(LineageClass.Carnivore);
            mutations.Health.SetMax(10);
            mutations.Health.TakeDamage(6);
            Assert.AreEqual(4, mutations.Health.Current);
        }

        [Test]
        public void BlindPrey_CountsBlindedEnemiesAsWounded()
        {
            Complete(LineageClass.Carnivore, LineageTouch.BlindPrey);
            var enemy = CreateEnemy(10);
            enemy.GetComponent<EnemyAI>().Blind(3f);

            enemy.Health.TakeDamage(2);
            Assert.AreEqual(6, enemy.Health.Current);
        }

        [Test]
        public void BloodInTheWater_FinishingAnEnemy_RefillsTheDodge()
        {
            Complete(LineageClass.Carnivore, LineageTouch.BloodInTheWater);
            controller.SetInput(Vector2.right, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            Assert.IsFalse(controller.CanDodge);

            var enemy = CreateEnemy(4);
            enemy.Health.TakeDamage(2);
            enemy.Health.TakeDamage(1);

            Assert.IsTrue(enemy.Health.IsDead);
            Assert.AreEqual(1, lineage.BenefitOf<FinisherBenefit>().DodgeRefills);
        }

        [Test]
        public void Venomous_PoisonedEnemiesTakeHalfAgainAsMuch()
        {
            Complete(LineageClass.Venomous);
            var enemy = CreateEnemy(20);

            enemy.Health.TakeDamage(2);
            Assert.AreEqual(18, enemy.Health.Current);

            Poison.Apply(enemy.Health, 1, 3f);
            enemy.Health.TakeDamage(2);
            Assert.AreEqual(15, enemy.Health.Current);
        }

        [Test]
        public void ScaleDamage_RoundsAtRandom_AndAveragesTheExactValue()
        {
            int total = 0;
            for (int i = 0; i < 2000; i++)
            {
                int damage = Health.ScaleDamage(1, 1.5f);
                Assert.That(damage, Is.InRange(1, 2));
                total += damage;
            }
            Assert.AreEqual(1.5f, total / 2000f, 0.08f);
        }

        [Test]
        public void Herbivore_BiomassHeals_AndAtFullHealthGivesABarrier()
        {
            Complete(LineageClass.Herbivore);
            var health = mutations.Health;
            var biomass = player.GetComponent<PlayerBiomass>();
            health.TakeDamage(1);

            biomass.Add(5);
            biomass.Add(5);
            Assert.AreEqual(health.Max, health.Current, "10 de biomasa curan medio corazón.");

            biomass.Add(10);
            Assert.AreEqual(1, health.Barrier, "Con la vida llena, medio corazón verde.");
            biomass.Add(10);
            Assert.AreEqual(1, health.Barrier, "Como mucho uno a la vez.");

            biomass.TrySpend(20);
            Assert.AreEqual(0, lineage.BenefitOf<GrazingBenefit>().Grazed, "Gastar no cuenta.");
        }

        [Test]
        public void Barrier_StopsAWholeHit_BeforeTheShields()
        {
            var health = mutations.Health;
            health.Barrier = 1;
            health.Shields = 1;

            Assert.IsTrue(health.TakeDamage(2));
            Assert.AreEqual(health.Max, health.Current);
            Assert.AreEqual(0, health.Barrier);
            Assert.AreEqual(1, health.Shields);
        }

        [UnityTest]
        public IEnumerator LosingTheLineage_RemovesItsBenefits()
        {
            Complete(LineageClass.Herbivore);
            player.GetComponent<PlayerBiomass>().Add(10);
            Assert.AreEqual(1, mutations.Health.Barrier);

            mutations.Equip(Mutation(MutationSlot.Mouth));
            Assert.IsNull(lineage.Active);
            // Los componentes del beneficio se destruyen al final del cuadro.
            yield return null;
            Assert.IsNull(lineage.BenefitOf<GrazingBenefit>());
            Assert.AreEqual(0, mutations.Health.Barrier, "El corazón verde se va con el linaje.");
        }

        [UnityTest]
        public IEnumerator Retract_UsingTheAbility_MakesThePlayerInvulnerable()
        {
            var ability = player.AddComponent<PlayerAbility>();
            Complete(LineageClass.Herbivore, LineageTouch.Retract);
            var vacuole = Track(ScriptableObject.CreateInstance<DigestiveVacuoleMutation>());
            vacuole.ConfigureBase("Vacuola", "", MutationSlot.Organ, Color.white);
            // La vacuola nueva rompe el linaje de prueba: se rehace con ella.
            var parts = new MutationData[] { mutations.Get(MutationSlot.Mouth), mutations.Get(MutationSlot.Limbs), mutations.Get(MutationSlot.Skin), vacuole };
            var turtle = Track(ScriptableObject.CreateInstance<LineageData>());
            turtle.Configure("Tortuga", LineageClass.Herbivore, parts, "");
            turtle.ConfigureTouch(LineageTouch.Retract, "Retraerse", "");
            foreach (var part in parts)
                part.SetLineages(new[] { turtle });
            mutations.Equip(vacuole);
            Assert.AreSame(turtle, lineage.Active);
            yield return null;

            mutations.Health.TakeDamage(1);
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(ability.TryUse());
            Assert.IsFalse(mutations.Health.TakeDamage(1), "Retraída, no recibe daño.");
        }

        [UnityTest]
        public IEnumerator StingingTrail_DodgingLeavesPoisonousPatches()
        {
            Complete(LineageClass.Venomous, LineageTouch.StingingTrail);
            yield return null;

            controller.SetInput(Vector2.right, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(lineage.BenefitOf<StingingTrailTouch>().Spawned, 1);
        }
    }
}
