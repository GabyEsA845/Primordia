using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Hifas, Camuflaje y Vacuola digestiva.</summary>
    public class Floor1MutationTests
    {
        readonly List<Object> created = new();
        PlayerMutations mutations;
        PlayerAbility ability;
        Weapon weapon;
        Health health;

        [SetUp]
        public void SetUp()
        {
            var player = Track(new GameObject("Player"));
            weapon = player.AddComponent<Weapon>();
            weapon.Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            player.AddComponent<PlayerController>().readInput = false;
            health = player.GetComponent<Health>();
            mutations = player.AddComponent<PlayerMutations>();
            ability = player.AddComponent<PlayerAbility>();
            ability.readInput = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var patch in Object.FindObjectsByType<MyceliumPatch>(FindObjectsInactive.Include))
                Object.DestroyImmediate(patch.gameObject);
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

        T Mutation<T>(MutationSlot slot) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase(typeof(T).Name, "", slot, Color.white);
            return mutation;
        }

        Enemy CreateEnemy(Vector2 position, float speed = 0f)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 10, moveSpeed: speed, detectionRange: 0f, contactDamage: 0);
            var go = Track(new GameObject("Enemy") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) });
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var enemy = go.AddComponent<Enemy>();
            var ai = go.AddComponent<EnemyAI>();
            enemy.SetData(data);
            ai.Target = mutations.transform;
            ai.Alert();
            return enemy;
        }

        Camouflage EquipCamouflage(float noticeTime = 0.2f, float delay = 0.1f, int level = 1)
        {
            var camo = Mutation<CamouflageMutation>(MutationSlot.Skin);
            camo.ConfigureTiming(new[] { delay, delay, delay }, new[] { 2f, 3f, 3f }, 2.5f, noticeTime);
            for (int i = 0; i < level; i++)
                mutations.Equip(camo);
            return mutations.GetComponent<Camouflage>();
        }

        [Test]
        public void Weapon_CombinesItsMultiplierWithEachBonus()
        {
            weapon.DamageMultiplier = 1.5f;
            var source = new object();
            weapon.SetDamageBonus(source, 2f);
            Assert.AreEqual(3f, weapon.DamageMultiplier, 0.001f);

            weapon.DamageMultiplier = 1f;
            Assert.AreEqual(2f, weapon.DamageMultiplier, 0.001f, "Asignarlo cambia solo la base.");

            weapon.ClearDamageBonus(source);
            Assert.AreEqual(1f, weapon.DamageMultiplier, 0.001f);
        }

        [UnityTest]
        public IEnumerator Camouflage_HidesAfterTheDelay_BoostsDamage_AndAHitRevealsIt()
        {
            var camouflage = EquipCamouflage();
            Assert.IsFalse(camouflage.IsConcealed);

            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(camouflage.IsConcealed);
            Assert.AreEqual(2f, weapon.DamageMultiplier, 0.001f);

            health.TakeDamage(1);
            Assert.IsFalse(camouflage.IsConcealed);
            Assert.AreEqual(1f, weapon.DamageMultiplier, 0.001f);

            mutations.Unequip(MutationSlot.Skin);
            yield return null;
            Assert.IsNull(mutations.GetComponent<Camouflage>());
        }

        [UnityTest]
        public IEnumerator Camouflage_EnemiesSearchFromAfar_AndDiscoverThePlayerUpClose()
        {
            // Tarda 0,5 s en volver a ocultarse: da tiempo a comprobar que lo descubrió.
            var camouflage = EquipCamouflage(noticeTime: 0.3f, delay: 0.5f);
            var far = CreateEnemy(new Vector2(8f, 0f)).GetComponent<EnemyAI>();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(camouflage.IsConcealed);
            Assert.AreEqual(EnemyAI.State.Search, far.Current);
            Assert.AreEqual(0f, far.Suspicion);

            var near = CreateEnemy(new Vector2(1.5f, 0f)).GetComponent<EnemyAI>();
            yield return null;
            yield return null;
            Assert.Greater(near.Suspicion, 0f, "Cerca, sospecha.");
            Assert.IsTrue(camouflage.IsConcealed, "Todavía no lo ha descubierto.");

            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(camouflage.IsConcealed, "Al llenarse la sospecha, lo descubre.");
        }

        [UnityTest]
        public IEnumerator Scare_StopsAttacking_AndBacksAwaySlowly()
        {
            var enemy = CreateEnemy(new Vector2(2f, 0f), speed: 4f).GetComponent<EnemyAI>();
            yield return null;
            enemy.Scare(1f, 0.35f);
            yield return null;
            Assert.AreEqual(EnemyAI.State.Scared, enemy.Current);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(enemy.transform.position.x, 2.05f, "Se aleja del jugador.");
        }

        [UnityTest]
        public IEnumerator Mycelium_SlowsAndDamagesTheEnemyOnTop()
        {
            var enemy = CreateEnemy(new Vector2(20f, 0f));
            yield return new WaitForFixedUpdate();

            var patch = new GameObject("Patch").AddComponent<MyceliumPatch>();
            patch.transform.position = enemy.transform.position;
            patch.Configure(new MyceliumPatch.Settings { duration = 3f, damagePerSecond = 2, slowFactor = 0.6f, radius = 0.5f });

            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(enemy.GetComponent<EnemyAI>().IsSlowed);
            Assert.AreEqual(10, enemy.Health.Current, "Rozarlo no daña al momento.");

            yield return new WaitForSeconds(0.6f);
            Assert.Less(enemy.Health.Current, 10);
        }

        [Test]
        public void HyphaeTrail_LeavesAPatchEveryFewSteps()
        {
            var hyphae = Mutation<HyphaeMutation>(MutationSlot.Limbs);
            mutations.Equip(hyphae);
            var trail = mutations.GetComponent<HyphaeTrail>();
            Assert.IsNotNull(trail);
            Assert.AreEqual(0, trail.Spawned);
        }

        [Test]
        public void Vacuole_ChargesWithKills_AndHealsOnlyWhenHurt()
        {
            var vacuole = Mutation<DigestiveVacuoleMutation>(MutationSlot.Organ);
            mutations.Equip(vacuole);
            Assert.IsTrue(ability.IsReady, "Empieza lista.");
            Assert.IsFalse(ability.TryUse(), "Con la vida llena no se gasta.");

            health.TakeDamage(3);
            int hurt = health.Current;
            Assert.IsTrue(ability.TryUse());
            Assert.AreEqual(hurt + 1, health.Current, "Nivel I: medio corazón.");
            Assert.IsFalse(ability.IsReady);
            Assert.IsTrue(ability.UsesKills);

            for (int i = 0; i < 3; i++)
                ability.CountKill();
            Assert.IsFalse(ability.IsReady);
            Assert.AreEqual(0.75f, ability.CooldownProgress, 0.001f);
            ability.CountKill();
            Assert.IsTrue(ability.IsReady, "Nivel I: cada 4 muertes.");

            mutations.Equip(vacuole);
            mutations.Equip(vacuole);
            Assert.AreEqual(3, vacuole.KillsToChargeAt(3));
            hurt = health.Current;
            Assert.IsTrue(ability.TryUse());
            Assert.AreEqual(hurt + 2, health.Current, "Nivel III: un corazón entero.");
            Assert.AreEqual(3, ability.KillsLeft);
        }
    }
}
