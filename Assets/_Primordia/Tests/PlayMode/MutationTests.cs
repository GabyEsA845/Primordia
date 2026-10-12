using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public class MutationTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerMutations mutations;
        PlayerController controller;
        Health health;
        Weapon weapon;
        AttackData defaultAttack;

        [SetUp]
        public void SetUp()
        {
            defaultAttack = Track(ScriptableObject.CreateInstance<AttackData>());

            playerObject = Track(new GameObject("Player"));
            weapon = playerObject.AddComponent<Weapon>();
            weapon.Attack = defaultAttack;
            controller = playerObject.AddComponent<PlayerController>();
            controller.readInput = false;
            health = playerObject.GetComponent<Health>();

            // Weapon.Attack ya está puesto cuando PlayerMutations guarda el ataque por defecto.
            mutations = playerObject.AddComponent<PlayerMutations>();
            playerObject.AddComponent<PlayerAbility>().readInput = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);

            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            created.Clear();
            Time.timeScale = 1f;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        T CreateMutation<T>(MutationSlot slot, Lineage lineage = Lineage.Predator) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase(typeof(T).Name, "", slot, lineage, Color.white);
            return mutation;
        }

        StatMutation CreateStat(MutationSlot slot, float speed, float dodge, int healthBonus)
        {
            var mutation = CreateMutation<StatMutation>(slot);
            mutation.Configure(speed, dodge, healthBonus);
            return mutation;
        }

        [Test]
        public void Skin_GrowsTheCollider_AndRestoresItOnUnequip()
        {
            // Se añade otra vez el jugador con colisión: PlayerMutations guarda el radio de la célula en Awake.
            var cellObject = Track(new GameObject("PlayerWithCollider"));
            var collider = cellObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            cellObject.AddComponent<Weapon>();
            cellObject.AddComponent<PlayerController>().readInput = false;
            var cell = cellObject.AddComponent<PlayerMutations>();

            var carapace = CreateStat(MutationSlot.Skin, 1f, 1f, 0);
            carapace.SetLook(null, newBodyRadius: 0.55f);
            var plainSkin = CreateStat(MutationSlot.Skin, 1f, 1f, 0);

            cell.Equip(carapace);
            Assert.AreEqual(0.55f, collider.radius, 0.0001f);

            // Una Piel sin radio propio deja el de la célula.
            cell.Equip(plainSkin);
            Assert.AreEqual(0.4f, collider.radius, 0.0001f);

            cell.Equip(carapace);
            cell.Unequip(MutationSlot.Skin);
            Assert.AreEqual(0.4f, collider.radius, 0.0001f);
        }

        [Test]
        public void StatMutation_ChangesSpeedAndDodge_AndUndoesOnUnequip()
        {
            float speed = controller.MoveSpeed;
            float dodge = controller.DodgeDistance;

            mutations.Equip(CreateStat(MutationSlot.Limbs, 1.3f, 1.5f, 0));
            Assert.AreEqual(speed * 1.3f, controller.MoveSpeed, 0.001f);
            Assert.AreEqual(dodge * 1.5f, controller.DodgeDistance, 0.001f);

            mutations.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(speed, controller.MoveSpeed, 0.001f);
            Assert.AreEqual(dodge, controller.DodgeDistance, 0.001f);
        }

        [Test]
        public void EquippingTheSameSlot_ReplacesThePreviousMutation()
        {
            float speed = controller.MoveSpeed;
            var fins = CreateStat(MutationSlot.Limbs, 1.3f, 1f, 0);
            var slow = CreateStat(MutationSlot.Limbs, 0.5f, 1f, 0);

            mutations.Equip(fins);
            mutations.Equip(slow);

            Assert.AreSame(slow, mutations.Get(MutationSlot.Limbs));
            Assert.IsFalse(mutations.IsEquipped(fins));
            Assert.AreEqual(speed * 0.5f, controller.MoveSpeed, 0.001f);
        }

        [Test]
        public void Carapace_AddsAndHealsAHeart_AndTakesItBack()
        {
            health.TakeDamage(1);
            int max = health.Max;
            int current = health.Current;

            mutations.Equip(CreateStat(MutationSlot.Skin, 0.9f, 1f, 2));
            Assert.AreEqual(max + 2, health.Max);
            Assert.AreEqual(current + 2, health.Current);

            mutations.Unequip(MutationSlot.Skin);
            Assert.AreEqual(max, health.Max);
        }

        [Test]
        public void AttackMutation_ChangesTheAttack_AndRestoresTheDefault()
        {
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            var mouth = CreateMutation<AttackMutation>(MutationSlot.Mouth);
            mouth.Configure(attack, newHealEveryKills: 0);

            mutations.Equip(mouth);
            Assert.AreSame(attack, weapon.Attack);

            mutations.Unequip(MutationSlot.Mouth);
            Assert.AreSame(defaultAttack, weapon.Attack);
        }

        [Test]
        public void KillHealer_HealsEveryNKills()
        {
            var healer = playerObject.AddComponent<KillHealer>();
            healer.Configure(weapon, health, everyKills: 3);
            health.TakeDamage(2);
            int before = health.Current;

            for (int i = 0; i < 3; i++)
            {
                var victim = Track(new GameObject("Victim")).AddComponent<Health>();
                victim.TakeDamage(victim.Max);
                weapon.ReportHit(victim);
            }

            Assert.AreEqual(3, healer.Kills);
            Assert.AreEqual(before + 1, health.Current);
        }

        [UnityTest]
        public IEnumerator Poison_DealsDamageEverySecond_ThenWearsOff()
        {
            var target = Track(new GameObject("Target")).AddComponent<Health>();

            Poison.Apply(target, damagePerSecond: 1, duration: 2f);
            yield return new WaitForSeconds(2.3f);

            Assert.AreEqual(target.Max - 2, target.Current);
            Assert.IsFalse(target.TryGetComponent<Poison>(out _));
        }

        [Test]
        public void Ability_GoesOnCooldownAfterUse()
        {
            var ink = CreateMutation<InkCloudMutation>(MutationSlot.Organ);
            ink.ConfigureAbility(newCooldown: 5f, newEffect: null);
            var ability = playerObject.GetComponent<PlayerAbility>();

            Assert.IsFalse(ability.TryUse(), "Sin órgano no hay habilidad.");

            mutations.Equip(ink);
            Assert.IsTrue(ability.IsReady);
            Assert.IsTrue(ability.TryUse());
            Assert.IsTrue(ability.IsCharging);
            Assert.Less(ability.CooldownProgress, 0.1f);
            Assert.IsFalse(ability.TryUse());

            mutations.Unequip(MutationSlot.Organ);
            Assert.IsNull(ability.Ability);
        }

        [UnityTest]
        public IEnumerator InkCloud_BlindsNearbyEnemies()
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: 3, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);

            Enemy CreateEnemy(Vector2 position)
            {
                var go = new GameObject("Enemy") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
                go.transform.position = position;
                go.AddComponent<CircleCollider2D>();
                var enemy = go.AddComponent<Enemy>();
                go.AddComponent<EnemyAI>();
                enemy.SetData(data);
                return enemy;
            }

            var near = CreateEnemy(new Vector2(2f, 0f));
            var far = CreateEnemy(new Vector2(10f, 0f));
            yield return new WaitForFixedUpdate();

            var ink = CreateMutation<InkCloudMutation>(MutationSlot.Organ);
            ink.ConfigureAbility(newCooldown: 5f, newEffect: null);
            ink.Configure(newRadius: 3f, newBlindDuration: 3f);
            ink.Activate(mutations);

            Assert.IsTrue(near.GetComponent<EnemyAI>().IsBlind);
            Assert.IsFalse(far.GetComponent<EnemyAI>().IsBlind);
        }

        [Test]
        public void Pool_OffersDistinctMutations_WithoutTheEquippedOnes()
        {
            var all = Enumerable.Range(0, 5).Select(_ => (MutationData)CreateStat(MutationSlot.Limbs, 1f, 1f, 0)).ToList();
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(all);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var offer = pool.PickOffer(3, new[] { all[0], all[1] });

                Assert.AreEqual(3, offer.Count);
                Assert.AreEqual(3, offer.Distinct().Count());
                Assert.IsFalse(offer.Contains(all[0]) || offer.Contains(all[1]));
            }
        }

        [Test]
        public void MutationList_ShowsEachSlot()
        {
            var fins = CreateStat(MutationSlot.Limbs, 1.3f, 1f, 0);
            fins.ConfigureBase("Aletas", "Más velocidad.", MutationSlot.Limbs, Lineage.Predator, Color.cyan);
            mutations.Equip(fins);

            var listObject = Track(new GameObject("List"));
            var names = new UnityEngine.UI.Text[4];
            var descriptions = new UnityEngine.UI.Text[4];
            for (int i = 0; i < 4; i++)
            {
                names[i] = new GameObject($"Name{i}").AddComponent<UnityEngine.UI.Text>();
                descriptions[i] = new GameObject($"Description{i}").AddComponent<UnityEngine.UI.Text>();
                names[i].transform.SetParent(listObject.transform);
                descriptions[i].transform.SetParent(listObject.transform);
            }

            var list = listObject.AddComponent<UI.MutationListUI>();
            list.Configure(names, descriptions);
            list.Refresh(mutations);

            Assert.AreEqual("Aletas", names[(int)MutationSlot.Limbs].text);
            Assert.AreEqual("Más velocidad.", descriptions[(int)MutationSlot.Limbs].text);
            Assert.AreEqual(Color.cyan, names[(int)MutationSlot.Limbs].color);
            Assert.AreEqual("— vacía —", names[(int)MutationSlot.Mouth].text);
            Assert.AreEqual("", descriptions[(int)MutationSlot.Organ].text);
        }

        [Test]
        public void Pool_OffersFewer_WhenNotEnoughAreLeft()
        {
            var all = Enumerable.Range(0, 2).Select(_ => (MutationData)CreateStat(MutationSlot.Limbs, 1f, 1f, 0)).ToList();
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(all);

            Assert.AreEqual(1, pool.PickOffer(3, new[] { all[0] }).Count);
        }
    }
}
