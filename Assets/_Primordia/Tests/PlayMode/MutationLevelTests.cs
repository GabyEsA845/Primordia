using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    public class MutationLevelTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerController controller;
        PlayerMutations mutations;

        [SetUp]
        public void SetUp()
        {
            playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            controller = playerObject.AddComponent<PlayerController>();
            controller.readInput = false;
            mutations = playerObject.AddComponent<PlayerMutations>();
            playerObject.AddComponent<PlayerAbility>().readInput = false;
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

        T CreateMutation<T>(MutationSlot slot, string name = null) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase(name ?? typeof(T).Name, "Base.", slot, Color.white);
            mutation.SetLevelDescriptions("Mejora.", "Evolución.");
            return mutation;
        }

        StatMutation CreateFins()
        {
            var fins = CreateMutation<StatMutation>(MutationSlot.Limbs, "Aletas");
            fins.Configure(1.3f, 1.5f, 0);
            var upgrade = new StatMutation.StatLevel { moveSpeedMultiplier = 1.45f, dodgeDistanceMultiplier = 1.5f, dodgeCooldownMultiplier = 0.75f };
            fins.ConfigureUpgrades(upgrade, upgrade);
            return fins;
        }

        [Test]
        public void EquippingItAgain_LevelsUp_UpToThree()
        {
            var fins = CreateFins();
            float baseSpeed = controller.MoveSpeed;
            float baseCooldown = controller.DodgeCooldown;
            var levelUps = new List<int>();
            mutations.LevelChanged += (_, level) => levelUps.Add(level);

            mutations.Equip(fins);
            Assert.AreEqual(1, mutations.LevelOf(fins));
            Assert.AreEqual(baseSpeed * 1.3f, controller.MoveSpeed, 0.001f);

            mutations.Equip(fins);
            Assert.AreEqual(2, mutations.LevelOf(fins));
            Assert.AreEqual(baseSpeed * 1.45f, controller.MoveSpeed, 0.001f, "Los valores del nivel II sustituyen a los del I.");
            Assert.AreEqual(baseCooldown * 0.75f, controller.DodgeCooldown, 0.001f);

            mutations.Equip(fins);
            Assert.AreEqual(3, mutations.LevelOf(fins));
            Assert.IsFalse(mutations.CanLevelUp(fins));

            Assert.IsFalse(mutations.LevelUp(MutationSlot.Limbs), "No pasa del nivel III.");
            Assert.AreEqual(3, mutations.LevelOf(fins));
            CollectionAssert.AreEqual(new[] { 2, 3 }, levelUps);
        }

        [Test]
        public void Replacing_LosesTheLevel_AndLeavesNoLeftovers()
        {
            var fins = CreateFins();
            var other = CreateMutation<StatMutation>(MutationSlot.Limbs);
            other.Configure(1f, 1f, 0);
            float baseSpeed = controller.MoveSpeed;
            float baseCooldown = controller.DodgeCooldown;

            mutations.Equip(fins);
            mutations.Equip(fins);
            mutations.Equip(other);

            Assert.AreEqual(1, mutations.LevelOf(MutationSlot.Limbs));
            Assert.AreEqual(baseSpeed, controller.MoveSpeed, 0.001f);
            Assert.AreEqual(baseCooldown, controller.DodgeCooldown, 0.001f);

            mutations.Equip(fins);
            Assert.AreEqual(1, mutations.LevelOf(fins), "Vuelve a empezar en el nivel I.");
            Assert.AreEqual(baseSpeed * 1.3f, controller.MoveSpeed, 0.001f);
        }

        [Test]
        public void Offer_AlwaysHasAnUpgrade_WhileOneIsPossible()
        {
            var fins = CreateFins();
            var all = new List<MutationData> { fins };
            for (int i = 0; i < 6; i++)
                all.Add(CreateMutation<StatMutation>((MutationSlot)(i % 4), $"M{i}"));
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(all);
            mutations.Equip(fins);

            for (int i = 0; i < 20; i++)
            {
                var offer = pool.PickOffer(3, mutations);
                Assert.AreEqual(3, offer.Count);
                Assert.AreEqual(3, offer.Distinct().Count());
                Assert.Contains(fins, offer);
            }

            mutations.Equip(fins);
            mutations.Equip(fins);
            for (int i = 0; i < 20; i++)
                Assert.IsFalse(pool.PickOffer(3, mutations).Contains(fins), "En el nivel III ya no se ofrece.");
        }

        [UnityTest]
        public IEnumerator Ambush_RaisesDamageAfterADodge_ForAWhile()
        {
            var fins = CreateFins();
            fins.ConfigureAmbush(0.5f, 0.3f, null);
            var weapon = playerObject.GetComponent<Weapon>();
            for (int i = 0; i < 3; i++)
                mutations.Equip(fins);
            yield return null;

            controller.SetInput(Vector2.zero, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            Assert.AreEqual(1f, weapon.DamageMultiplier, "Aún no: empieza al terminar la esquiva.");

            yield return new WaitForSeconds(0.3f); // la esquiva dura 0,25 s
            Assert.AreEqual(1.5f, weapon.DamageMultiplier);

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1f, weapon.DamageMultiplier);
        }

        [Test]
        public void DamageMultiplier_RoundsUp()
        {
            var weapon = playerObject.GetComponent<Weapon>();
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(null, 1f, 3, 10f, 1f);

            Assert.AreEqual(3, weapon.DamageOf(attack));
            weapon.DamageMultiplier = 1.5f;
            Assert.AreEqual(5, weapon.DamageOf(attack));
        }

        [Test]
        public void Plates_BlockTheFirstHit_AndRechargeOnRequest()
        {
            var carapace = CreateMutation<StatMutation>(MutationSlot.Skin, "Caparazón");
            carapace.Configure(0.9f, 1f, 2);
            carapace.ConfigurePlates(true, null, null);
            var health = playerObject.GetComponent<Health>();
            for (int i = 0; i < 3; i++)
                mutations.Equip(carapace);

            int before = health.Current;
            Assert.IsTrue(health.TakeDamage(1), "El golpe cuenta, pero no quita vida.");
            Assert.AreEqual(before, health.Current);

            health.TakeDamage(1);
            Assert.AreEqual(before - 1, health.Current);

            playerObject.GetComponent<Plates>().Recharge();
            health.TakeDamage(1);
            Assert.AreEqual(before - 1, health.Current);

            mutations.Unequip(MutationSlot.Skin);
            Assert.AreEqual(0, health.Shields, "Sin Caparazón no queda escudo.");
        }

        [Test]
        public void Bleed_AndPoison_ShowTheirMarkers_SideBySide()
        {
            var target = Track(new GameObject("Target")).AddComponent<Health>();
            var marker = Track(new GameObject("Marker"));
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(null, 1f, 1, 10f, 1f, poisonDamagePerSecond: 1, poisonDuration: 3f);
            attack.SetPoisonEffect(marker);
            attack.ConfigureBleed(1, 3f, marker);

            attack.ApplyStatus(target);

            Assert.IsTrue(target.TryGetComponent<Poison>(out var poison));
            Assert.IsTrue(target.TryGetComponent<Bleed>(out var bleed));
            var markers = target.GetComponent<StatusMarkers>();
            Assert.AreEqual(2, markers.Count);
            Assert.AreNotEqual(poison.Effect.transform.localPosition.x, bleed.Effect.transform.localPosition.x, "Una al lado de la otra.");
        }

        [UnityTest]
        public IEnumerator Contagion_PoisonsNearbyEnemies_WhenAPoisonedOneDies()
        {
            int enemyLayer = LayerMask.NameToLayer(CombatLayers.Enemy);
            Health Enemy(Vector2 position)
            {
                var enemy = Track(new GameObject("Enemy"));
                enemy.layer = enemyLayer;
                enemy.transform.position = position;
                enemy.AddComponent<CircleCollider2D>().radius = 0.4f;
                return enemy.AddComponent<Health>();
            }

            var dying = Enemy(Vector2.zero);
            var near = Enemy(new Vector2(1.5f, 0f));
            var far = Enemy(new Vector2(6f, 0f));
            yield return new WaitForFixedUpdate();

            Poison.ApplyContagious(dying, 1, 3f, null, burstRadius: 2f, burstEffect: null);
            dying.TakeDamage(dying.Max);

            Assert.IsTrue(near.TryGetComponent<Poison>(out var spread));
            Assert.AreEqual(2f, spread.BurstRadius, "El contagio también se contagia.");
            Assert.IsFalse(far.TryGetComponent<Poison>(out _));
        }

        [Test]
        public void Abilities_GetTheirLevelValues()
        {
            var shock = CreateMutation<ShockwaveMutation>(MutationSlot.Organ);
            shock.ConfigureAbility(6f, null);
            shock.ConfigureUpgrades(new ShockwaveMutation.ShockLevel { radius = 6f, cooldown = 4.5f });

            Assert.AreEqual(6f, shock.CooldownAt(1));
            Assert.AreEqual(4.5f, shock.CooldownAt(2));
            Assert.AreEqual(4.5f, shock.CooldownAt(3), "Sin datos del III, se queda con los del II.");
            Assert.AreEqual(6f, shock.RadiusAt(3));
        }

        [Test]
        public void Card_ShowsAnUpgrade_WithTheNextLevel()
        {
            var fins = CreateFins();
            var cardObject = Track(new GameObject("Card", typeof(Button)));
            var card = cardObject.AddComponent<MutationCard>();
            Text NewText() => Track(new GameObject("Text")).AddComponent<Text>();
            var description = NewText();
            var replaces = NewText();
            card.Configure(null, NewText(), NewText(), description, replaces);

            card.Show(fins, null);
            Assert.IsFalse(card.IsUpgrade);
            Assert.AreEqual("Base.", description.text);
            Assert.AreEqual("Ranura libre", replaces.text);

            card.Show(fins, fins, currentLevel: 2);
            Assert.IsTrue(card.IsUpgrade);
            Assert.AreEqual("Evolución.", description.text);
            Assert.AreEqual("Nivel II > III", replaces.text);
        }

        [Test]
        public void FullDescription_ListsTheReachedLevels()
        {
            var fins = CreateFins();
            Assert.AreEqual("Base.", MutationText.FullDescription(fins, 1));
            Assert.AreEqual("Base.\nNivel II: Mejora.", MutationText.CurrentDescription(fins, 2));
            Assert.AreEqual("Base.\nNivel III: Evolución.", MutationText.CurrentDescription(fins, 3));
            Assert.AreEqual("Base.\nNivel II: Mejora.\nNivel III: Evolución.", MutationText.FullDescription(fins, 3));
        }
    }
}
