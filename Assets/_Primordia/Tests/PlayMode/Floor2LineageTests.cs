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
            foreach (var spine in Object.FindObjectsByType<GroundSpine>(FindObjectsSortMode.None))
                Object.DestroyImmediate(spine.gameObject);
            foreach (var larva in Object.FindObjectsByType<Larva>(FindObjectsSortMode.None))
                Object.DestroyImmediate(larva.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();
            if (player != null && player.TryGetComponent<CompoundEyesEffect>(out var eyes))
                eyes.End();
            TimeWarp.Set(1f);
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
        LineageData Complete(LineageClass lineageClass, int floor = 2, LineageTouch touch = LineageTouch.None)
        {
            var parts = new MutationData[]
            {
                Mutation(MutationSlot.Mouth, floor), Mutation(MutationSlot.Limbs, floor),
                Mutation(MutationSlot.Skin, floor), Mutation(MutationSlot.Organ, floor),
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

        // ---------- Toques del piso 2 ----------

        [UnityTest]
        public IEnumerator Feast_FinishingWithABite_HealsHalfAHeart()
        {
            var health = mutations.Health;
            health.SetMax(6);
            health.TakeDamage(2);
            Complete(LineageClass.Carnivore, touch: LineageTouch.Feast);
            var weapon = player.GetComponent<Weapon>();
            weapon.Attack.ConfigureMelee(range: 1.5f, arc: 180f, effect: null);

            var first = CreateEnemy(4, new Vector2(1f, 0f));
            first.Health.TakeDamage(2);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(weapon.TryFire(Vector2.right));
            Assert.IsTrue(first.Health.IsDead, "El mordisco al herido lo remata.");
            Assert.AreEqual(5, health.Current, "Rematar con un mordisco cura medio corazón.");
            Assert.AreEqual(1, player.GetComponent<FeastTouch>().Feasts);

            var second = CreateEnemy(4, new Vector2(-20f, 0f));
            second.Health.TakeDamage(2);
            second.Health.TakeDamage(2);
            Assert.IsTrue(second.Health.IsDead);
            Assert.AreEqual(1, player.GetComponent<FeastTouch>().Feasts, "Un remate que no es mordisco no cura.");
        }

        [UnityTest]
        public IEnumerator FiveEyes_EyesLastOneMoreSecond_AndFinishesExtendThem()
        {
            Complete(LineageClass.Carnivore, touch: LineageTouch.FiveEyes);
            var eyes = player.AddComponent<CompoundEyesEffect>();
            eyes.Begin(mutations, 3f, 1f, 1f, System.Array.Empty<Sprite>(), Color.white);
            Assert.AreEqual(4f, eyes.Remaining, 0.01f, "Un segundo más.");

            var enemy = CreateEnemy(4);
            enemy.Health.TakeDamage(2);
            enemy.Health.TakeDamage(1);
            Assert.IsTrue(enemy.Health.IsDead);
            Assert.AreEqual(4.5f, eyes.Remaining, 0.05f, "Cada remate los alarga medio segundo.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StingingBed_SpinesPoisonWhoStepsOnThem()
        {
            Complete(LineageClass.Venomous, touch: LineageTouch.StingingBed);
            var enemy = CreateEnemy(30, new Vector2(20f, 0f));
            yield return new WaitForFixedUpdate();
            GroundSpine.Spawn(null, new Vector2(20f, 0f), 1, 1f, null, 0f, player.GetComponent<Weapon>());
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(enemy.TryGetComponent<Poison>(out _), "Quien pisa la púa queda envenenado.");
        }

        [UnityTest]
        public IEnumerator StingingPolyps_LarvaBitesPoison()
        {
            Complete(LineageClass.Venomous, touch: LineageTouch.StingingPolyps);
            player.transform.position = new Vector3(40f, 0f, 0f);
            var enemy = CreateEnemy(30, new Vector2(42f, 0f));
            Larva.Spawn(null, new Vector2(40f, 0f), new Larva.Settings
            {
                lifetime = 8f, speed = 5.5f, range = 9f, biteDamage = 1, biteInterval = 0.7f, biteReach = 0.55f,
            }, mutations);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(enemy.Health.Current, 30, "La larva muerde.");
            Assert.IsTrue(enemy.TryGetComponent<Poison>(out _), "Y envenena.");
        }

        [UnityTest]
        public IEnumerator DoubleServing_SpineKillsLeaveDoubleRemains()
        {
            var settings = new LineageBenefitSettings { remainsPrefab = RemainsPrefab(), doubleRemainsPrefab = RemainsPrefab(2) };
            lineage.ConfigureBenefits(settings);
            Complete(LineageClass.Scavenger, touch: LineageTouch.DoubleServing);
            var carrion = lineage.BenefitOf<CarrionBenefit>();

            CreateEnemy(1, new Vector2(60f, 0f)).Health.TakeDamage(1);
            yield return null;
            Assert.AreEqual(1, carrion.Spawned[0].Value, "Sin púa, restos normales.");

            CreateEnemy(1, new Vector2(64f, 0f));
            yield return new WaitForFixedUpdate();
            GroundSpine.Spawn(null, new Vector2(64f, 0f), 1, 1f, null, 0f, player.GetComponent<Weapon>());
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(2, carrion.Spawned.Count);
            Assert.AreEqual(2, carrion.Spawned[1].Value, "La púa que mata deja restos dobles.");
        }

        [UnityTest]
        public IEnumerator CarrionArmor_EatingRemainsReadiesThePlates()
        {
            ConfigureCarrion(RemainsPrefab());
            Complete(LineageClass.Scavenger, touch: LineageTouch.CarrionArmor);
            player.AddComponent<PlatesBounce>();
            var carrion = lineage.BenefitOf<CarrionBenefit>();

            CreateEnemy(1, new Vector2(3f, 0f)).Health.TakeDamage(1);
            yield return null;
            player.transform.position = new Vector3(3f, 0f, 0f);
            yield return null;
            Assert.AreEqual(1, carrion.Stacks);
            Assert.AreEqual(1, player.GetComponent<CarrionTouch>().Armored, "Comer restos prepara las placas.");
        }

        [UnityTest]
        public IEnumerator Harvest_AVolleyHittingThreeEnemies_GivesAGreenHeart()
        {
            Complete(LineageClass.Herbivore, touch: LineageTouch.Harvest);
            var template = Track(new GameObject("ScaleTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), 1f, 1, 14f, 1f);

            player.transform.position = new Vector3(80f, 0f, 0f);
            var volley = player.AddComponent<ScleriteVolley>();
            volley.Configure(player.GetComponent<Weapon>(), new ScleriteVolley.Settings { attack = attack, count = 3, interval = 99f, range = 7f });
            CreateEnemy(30, new Vector2(82f, 1.5f));
            CreateEnemy(30, new Vector2(82.5f, 0f));
            CreateEnemy(30, new Vector2(82f, -1.5f));
            yield return new WaitForFixedUpdate();
            volley.Ready();
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(1, player.GetComponent<HarvestTouch>().Harvests, "3 enemigos alcanzados: medio corazón verde.");
            Assert.AreEqual(1, mutations.Health.Barrier);
        }
    }
}
