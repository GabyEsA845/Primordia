using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Biomass;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.Shop;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class ShopTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerMutations mutations;
        PlayerBiomass wallet;
        Health health;
        PlayerInteractor interactor;
        MutationPool pool;
        ShopRoom shop;

        [SetUp]
        public void SetUp()
        {
            playerObject = Track(new GameObject("Player") { tag = "Player" });
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            mutations = playerObject.AddComponent<PlayerMutations>();
            wallet = playerObject.AddComponent<PlayerBiomass>();
            interactor = playerObject.AddComponent<PlayerInteractor>();
            interactor.readInput = false;
            health = playerObject.GetComponent<Health>();
            // Para poder hacerle daño varias veces seguidas.
            health.InvulnerabilityAfterHit = 0f;

            pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(new[]
            {
                Mutation("Aletas", MutationSlot.Limbs), Mutation("Caparazón", MutationSlot.Skin),
                Mutation("Tinta", MutationSlot.Organ), Mutation("Esporas", MutationSlot.Mouth),
            });
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

        StatMutation Mutation(string name, MutationSlot slot)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "Base.", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            mutation.SetLevelDescriptions("Mejora.", "Evolución.");
            return mutation;
        }

        /// <summary>Tienda sin arte: 4 pedestales en fila, a 3 unidades unos de otros, por encima del jugador.</summary>
        ShopRoom CreateShop()
        {
            var root = Track(new GameObject("Shop"));
            var pedestals = new ShopPedestal[4];
            for (int i = 0; i < pedestals.Length; i++)
            {
                var go = new GameObject($"Pedestal_{i}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(i * 3f, 10f, 0f);
                pedestals[i] = go.AddComponent<ShopPedestal>();
            }

            shop = root.AddComponent<ShopRoom>();
            shop.Configure(pool, pedestals, null, null, 0f);
            return shop;
        }

        ShopItemKind[] Kinds() => shop.Pedestals.Select(p => p.Kind).ToArray();

        // ---------- Mercancía ----------

        [Test]
        public void Stock_WithoutUpgrades_SellsThreeMutationsAndHealing()
        {
            CreateShop();
            health.TakeDamage(1);

            shop.Stock(mutations);

            CollectionAssert.AreEqual(new[] { ShopItemKind.Mutation, ShopItemKind.Mutation, ShopItemKind.Mutation, ShopItemKind.Heal }, Kinds());
            Assert.AreEqual(3, shop.Pedestals.Take(3).Select(p => p.Mutation).Distinct().Count());
        }

        [Test]
        public void Stock_WithAnUpgradableMutation_AndFullHealth_SellsUpgradeAndHeart()
        {
            var fins = pool.Mutations[0];
            mutations.Equip(fins);
            CreateShop();

            shop.Stock(mutations);

            CollectionAssert.AreEqual(new[] { ShopItemKind.Mutation, ShopItemKind.Mutation, ShopItemKind.Upgrade, ShopItemKind.HeartContainer }, Kinds());
            Assert.AreEqual(fins, shop.Pedestals[2].Mutation);
            CollectionAssert.DoesNotContain(shop.Pedestals.Take(2).Select(p => p.Mutation).ToList(), fins, "No vende lo que ya lleva.");
        }

        [Test]
        public void Stock_HappensOnlyOnce()
        {
            CreateShop();
            shop.Stock(mutations);
            var first = shop.Pedestals.Select(p => p.Mutation).ToArray();

            health.TakeDamage(1);
            shop.Stock(mutations);

            CollectionAssert.AreEqual(first, shop.Pedestals.Select(p => p.Mutation).ToArray());
            Assert.AreEqual(ShopItemKind.HeartContainer, shop.Pedestals[3].Kind, "Entró con la vida llena: sigue siendo el corazón.");
        }

        [Test]
        public void UpgradePrice_IsHigherForTheEvolution()
        {
            var fins = pool.Mutations[0];
            mutations.Equip(fins);
            CreateShop();
            shop.Stock(mutations);
            var upgrade = shop.Pedestals[2];

            Assert.AreEqual(20, shop.PriceOf(upgrade, mutations));
            mutations.LevelUp(fins.Slot);
            Assert.AreEqual(30, shop.PriceOf(upgrade, mutations));
        }

        // ---------- Compras ----------

        [Test]
        public void Buying_WithoutEnoughBiomass_Fails()
        {
            CreateShop();
            shop.Stock(mutations);
            wallet.Add(10);

            Assert.IsFalse(shop.TryBuy(shop.Pedestals[0], playerObject));

            Assert.AreEqual(10, wallet.Amount);
            Assert.IsTrue(shop.Pedestals[0].HasItem);
            Assert.AreEqual(0, mutations.Equipped.Count());
        }

        [Test]
        public void Buying_AMutation_EquipsIt_AndSpendsBiomass()
        {
            CreateShop();
            shop.Stock(mutations);
            wallet.Add(20);
            var pedestal = shop.Pedestals[0];

            Assert.IsTrue(shop.TryBuy(pedestal, playerObject));

            Assert.AreEqual(5, wallet.Amount);
            Assert.IsTrue(mutations.IsEquipped(pedestal.Mutation));
            Assert.IsTrue(pedestal.Sold);
            Assert.IsFalse(pedestal.IsAvailable);
            Assert.IsFalse(shop.TryBuy(pedestal, playerObject), "Un pedestal vendido no vuelve a vender.");
        }

        [Test]
        public void Buying_TheUpgrade_LevelsUpTheMutation()
        {
            var fins = pool.Mutations[0];
            mutations.Equip(fins);
            CreateShop();
            shop.Stock(mutations);
            wallet.Add(20);

            Assert.IsTrue(shop.TryBuy(shop.Pedestals[2], playerObject));

            Assert.AreEqual(2, mutations.LevelOf(fins));
            Assert.AreEqual(0, wallet.Amount);
            Assert.IsTrue(shop.Pedestals[2].Sold);
            wallet.Add(30);
            Assert.IsFalse(shop.TryBuy(shop.Pedestals[2], playerObject), "La mejora se compra una sola vez.");
        }

        // ---------- Pedestales que cambian ----------

        [UnityTest]
        public IEnumerator MutationObtainedElsewhere_IsSoldAsUpgrade()
        {
            CreateShop();
            shop.Stock(mutations);
            var pedestal = shop.Pedestals[0];

            mutations.Equip(pedestal.Mutation);
            yield return null;

            Assert.AreEqual(ShopItemKind.Upgrade, pedestal.Offer);
            Assert.AreEqual(20, shop.PriceOf(pedestal, mutations));
            wallet.Add(20);
            Assert.IsTrue(shop.TryBuy(pedestal, playerObject));
            Assert.AreEqual(2, mutations.LevelOf(pedestal.Mutation));
            Assert.IsTrue(pedestal.Sold);
            Assert.IsFalse(pedestal.Closed);
        }

        [UnityTest]
        public IEnumerator MutationAtMaxLevel_ClosesThePedestalForever()
        {
            CreateShop();
            shop.Stock(mutations);
            var pedestal = shop.Pedestals[0];
            var mutation = pedestal.Mutation;

            // Cogerla otra vez la sube de nivel: llega al III.
            mutations.Equip(mutation);
            mutations.Equip(mutation);
            mutations.Equip(mutation);
            Assert.AreEqual(MutationData.MaxLevel, mutations.LevelOf(mutation));
            yield return null;

            Assert.IsTrue(pedestal.Closed);
            Assert.IsFalse(pedestal.IsAvailable);
            wallet.Add(50);
            Assert.IsFalse(shop.TryBuy(pedestal, playerObject));

            // Aunque la cambie por otra, no vuelve a venderse.
            mutations.Equip(Mutation("Otra", mutation.Slot));
            yield return null;
            Assert.IsTrue(pedestal.Closed);
            Assert.IsFalse(shop.TryBuy(pedestal, playerObject));
            Assert.AreEqual(50, wallet.Amount);
        }

        [UnityTest]
        public IEnumerator UpgradeLeveledElsewhere_OffersTheNextLevel()
        {
            var fins = pool.Mutations[0];
            mutations.Equip(fins);
            CreateShop();
            shop.Stock(mutations);
            var upgrade = shop.Pedestals[2];

            mutations.LevelUp(fins.Slot);
            yield return null;

            Assert.AreEqual(ShopItemKind.Upgrade, upgrade.Offer);
            Assert.AreEqual(30, shop.PriceOf(upgrade, mutations));
            Assert.IsFalse(upgrade.Closed);
        }

        [UnityTest]
        public IEnumerator UpgradeOfAReplacedMutation_IsSoldAsANewMutation()
        {
            var fins = pool.Mutations[0];
            mutations.Equip(fins);
            CreateShop();
            shop.Stock(mutations);
            var upgrade = shop.Pedestals[2];

            mutations.Equip(Mutation("Tentáculo", MutationSlot.Limbs));
            yield return null;

            Assert.AreEqual(ShopItemKind.Mutation, upgrade.Offer);
            Assert.AreEqual(15, shop.PriceOf(upgrade, mutations));
            wallet.Add(15);
            Assert.IsTrue(shop.TryBuy(upgrade, playerObject));
            Assert.IsTrue(mutations.IsEquipped(fins));
            Assert.AreEqual(1, mutations.LevelOf(fins));
        }

        [Test]
        public void Healing_CannotBeBought_WithFullHealth()
        {
            CreateShop();
            health.TakeDamage(1);
            shop.Stock(mutations);
            wallet.Add(50);
            health.Heal(10);

            Assert.IsFalse(shop.TryBuy(shop.Pedestals[3], playerObject));

            health.TakeDamage(2);
            int before = health.Current;
            Assert.IsTrue(shop.TryBuy(shop.Pedestals[3], playerObject));
            Assert.AreEqual(before + 1, health.Current, "Medio corazón = 1 punto de vida.");
            Assert.AreEqual(45, wallet.Amount);
        }

        [Test]
        public void HeartContainer_AddsAFullHeart()
        {
            CreateShop();
            shop.Stock(mutations);
            wallet.Add(25);
            int max = health.Max;

            Assert.IsTrue(shop.TryBuy(shop.Pedestals[3], playerObject));

            Assert.AreEqual(max + 2, health.Max);
            Assert.AreEqual(health.Max, health.Current);
        }

        [Test]
        public void Describe_SaysWhichMutationIsReplaced()
        {
            var tentacle = Mutation("Tentáculo", MutationSlot.Limbs);
            mutations.Equip(tentacle);
            CreateShop();
            shop.Stock(mutations);
            var limbs = shop.Pedestals.Take(2).FirstOrDefault(p => p.Mutation.Slot == MutationSlot.Limbs);
            var other = shop.Pedestals.Take(2).FirstOrDefault(p => p.Mutation.Slot != MutationSlot.Limbs);

            if (limbs != null)
                StringAssert.Contains("Reemplaza: Tentáculo", shop.Describe(limbs, mutations).note);
            Assert.IsNull(shop.Describe(other, mutations).note);
        }

        // ---------- Interactuar ----------

        [UnityTest]
        public IEnumerator Interactor_FocusesTheNearestPedestal_AndBuysWithInteract()
        {
            CreateShop();
            shop.Stock(mutations);
            wallet.Add(15);
            playerObject.transform.position = new Vector3(3.3f, 9f, 0f);

            yield return null;
            Assert.AreEqual(shop.Pedestals[1], interactor.Focused);
            Assert.IsTrue(shop.Pedestals[1].IsFocused);

            interactor.interactPressed = true;
            yield return null;

            Assert.IsTrue(shop.Pedestals[1].Sold);
            Assert.AreEqual(0, wallet.Amount);

            yield return null;
            Assert.IsNull(interactor.Focused, "Un pedestal vacío ya no se puede elegir.");
        }

        [UnityTest]
        public IEnumerator Interactor_IgnoresPedestalsOutOfReach()
        {
            CreateShop();
            shop.Stock(mutations);
            playerObject.transform.position = new Vector3(0f, 5f, 0f);

            yield return null;

            Assert.IsNull(interactor.Focused);
        }

        // ---------- Mapa y plano ----------

        [Test]
        public void Layout_AddsOneShop_HangingFromTheSecondHalf()
        {
            var config = Track(ScriptableObject.CreateInstance<FloorConfig>());
            var small = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            small.SetLayout(RoomLayouts.All[RoomLayouts.Small]);
            config.ConfigureRooms(small, small, new[] { small }, new[] { small }, new Vector2Int(4, 5), new Vector2Int(1, 2));
            config.ConfigureShop(small, CreateShop());

            for (int seed = 1; seed <= 200; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var shops = layout.Rooms.Where(r => r.Role == RoomRole.Shop).ToList();
                Assert.AreEqual(1, shops.Count, $"semilla {seed}");

                var shopRoom = shops[0];
                Assert.AreEqual(1, shopRoom.Doors.Count, $"Sin salida (semilla {seed}).");
                var parent = layout.Rooms.Single(r => r.Cell == shopRoom.Cell + shopRoom.Doors[0]);
                int combatCount = layout.Rooms.Count(r => r.Role == RoomRole.Combat);
                Assert.AreEqual(RoomRole.Combat, parent.Role, $"semilla {seed}");
                Assert.Greater(parent.Depth, combatCount / 2, $"Cuelga de la segunda mitad (semilla {seed}).");
                Assert.IsFalse(shopRoom.Reward);
            }
        }

        [Test]
        public void ShopIcon_AppearsWhenTheNeighbourIsCleared()
        {
            Room NewRoom()
            {
                var go = Track(new GameObject("Room"));
                go.AddComponent<BoxCollider2D>();
                var room = go.AddComponent<Room>();
                room.Configure(null, null);
                return room;
            }

            var parent = new MapRoom { Room = NewRoom(), Role = RoomRole.Combat };
            var shopRoom = new MapRoom { Room = NewRoom(), Role = RoomRole.Shop };
            parent.Neighbors.Add(shopRoom);
            shopRoom.Neighbors.Add(parent);

            Assert.IsFalse(MapUI.ShopDiscovered(shopRoom));
            parent.Room.Begin();
            Assert.IsTrue(MapUI.ShopDiscovered(shopRoom));
        }
    }
}
