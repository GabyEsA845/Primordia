using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    public class MutationDropTests
    {
        readonly List<Object> created = new();
        PlayerMutations player;

        [SetUp]
        public void SetUp()
        {
            var playerObject = Track(new GameObject("Player") { tag = "Player" });
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            player = playerObject.AddComponent<PlayerMutations>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var pickup in Object.FindObjectsByType<MutationPickup>(FindObjectsInactive.Include))
                Object.DestroyImmediate(pickup.gameObject);

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

        MutationData Mutation(string name, MutationSlot slot, Lineage lineage = Lineage.Predator)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "", slot, lineage, Color.white);
            return mutation;
        }

        /// <summary>Un enemigo con MutationDrop y una reserva con esas mutaciones.</summary>
        (Enemy enemy, MutationDrop drop) CreateBoss(params MutationData[] mutations)
        {
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(mutations);

            var template = Track(new GameObject("PickupTemplate"));
            template.AddComponent<CircleCollider2D>();
            var pickupPrefab = template.AddComponent<MutationPickup>();

            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, 5, moveSpeed: 0f, detectionRange: 1f, contactDamage: 1,
                preferredDistance: 0f, attack: null, splitPrefab: null, splitCount: 0);

            var bossObject = Track(new GameObject("Boss"));
            var enemy = bossObject.AddComponent<Enemy>();
            enemy.SetData(data);
            var drop = bossObject.AddComponent<MutationDrop>();
            drop.Configure(pool, pickupPrefab);
            return (enemy, drop);
        }

        [Test]
        public void Death_DropsThreeMutations_WithoutTheEquippedOnes()
        {
            var equipped = Mutation("Equipada", MutationSlot.Mouth);
            player.Equip(equipped);
            var (enemy, drop) = CreateBoss(equipped, Mutation("A", MutationSlot.Limbs), Mutation("B", MutationSlot.Skin),
                Mutation("C", MutationSlot.Organ), Mutation("D", MutationSlot.Mouth));

            Assert.AreEqual(0, drop.Dropped.Count);
            enemy.Health.TakeDamage(enemy.Health.Max);

            Assert.AreEqual(3, drop.Dropped.Count);
            foreach (var pickup in drop.Dropped)
                Assert.AreNotEqual(equipped, pickup.Mutation);
        }

        [UnityTest]
        public IEnumerator CollectingOne_BurstsTheOthers()
        {
            var (enemy, drop) = CreateBoss(Mutation("A", MutationSlot.Limbs), Mutation("B", MutationSlot.Skin),
                Mutation("C", MutationSlot.Organ));
            enemy.Health.TakeDamage(enemy.Health.Max);
            var pickups = new List<MutationPickup>(drop.Dropped);

            pickups[1].Collect(player);
            yield return null;

            Assert.IsTrue(player.IsEquipped(pickups[1].Mutation));
            Assert.IsTrue(pickups.TrueForAll(p => p == null), "Las tres burbujas deberían haber desaparecido.");
        }

        [Test]
        public void Card_ShowsTheMutationAndLineageIcons()
        {
            var icon = Sprite.Create(Track(new Texture2D(32, 32)), new Rect(0, 0, 32, 32), Vector2.one / 2f);
            var toxic = Sprite.Create(Track(new Texture2D(12, 12)), new Rect(0, 0, 12, 12), Vector2.one / 2f);
            var mutation = Mutation("Esporas", MutationSlot.Mouth, Lineage.Toxic);
            mutation.SetIcons(null, icon);

            var cardObject = Track(new GameObject("Card", typeof(Button)));
            var card = cardObject.AddComponent<MutationCard>();
            Text NewText() => Track(new GameObject("Text")).AddComponent<Text>();
            card.Configure(null, NewText(), NewText(), NewText(), NewText());
            var iconImage = Track(new GameObject("Icon")).AddComponent<Image>();
            var lineageImage = Track(new GameObject("Lineage")).AddComponent<Image>();
            card.ConfigureIcons(iconImage, lineageImage);
            card.SetLineageIcons(new[] { null, null, toxic });

            card.Show(mutation, null);

            Assert.AreEqual(icon, iconImage.sprite);
            Assert.AreEqual(toxic, lineageImage.sprite);
            Assert.IsTrue(iconImage.enabled && lineageImage.enabled);
        }
    }
}
