using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Rooms;
using Primordia.Shop;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class Floor2StructureTests
    {
        readonly List<Object> created = new();
        FloorConfig floor1;
        FloorConfig floor2;
        TransitionPit pitTemplate;

        [SetUp]
        public void SetUp()
        {
            var pitObject = Track(new GameObject("TransitionPit"));
            pitObject.SetActive(false);
            pitObject.AddComponent<CapsuleCollider2D>();
            pitTemplate = pitObject.AddComponent<TransitionPit>();

            floor1 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor1.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal) }, new[] { Template(RoomLayouts.Small) }, new Vector2Int(2, 2), Vector2Int.zero);
            floor1.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null);
            floor1.ConfigureRewards(null, System.Array.Empty<int>());
            floor1.ConfigureTransition(Template(RoomLayouts.TransitionCave), pitTemplate, Theme("Mid"), Theme("Next"));
            floor1.ConfigureArt(Theme("Soup"), Theme("Boss"), null);

            floor2 = Track(ScriptableObject.CreateInstance<FloorConfig>());
            floor2.ConfigureRooms(Template(RoomLayouts.Small), Template(RoomLayouts.Normal),
                new[] { Template(RoomLayouts.Normal), Template(RoomLayouts.Pillars) }, new[] { Template(RoomLayouts.Small) },
                new Vector2Int(10, 12), new Vector2Int(2, 3));
            floor2.ConfigureEnemies(System.Array.Empty<FloorConfig.EnemyEntry>(), null, newBaseBudget: 4f, newBudgetPerRoom: 1.4f);
            floor2.ConfigureRewards(null, new[] { 2, 4, 6, 8, 10 });
            floor2.ConfigureArt(Theme("Reef"), Theme("Boss2"), null);
            floor2.ConfigureBands(new[] { Theme("Shallows"), Theme("Reef"), Theme("Abyss") });
            floor2.ConfigureFloor(2, null);

            floor1.ConfigureFloor(1, floor2);
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
            // La victoria (y otros estados) cambian la escala de tiempo: que no pase a las demás pruebas.
            Time.timeScale = 1f;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        RoomTemplate Template(string name)
        {
            var template = Track(ScriptableObject.CreateInstance<RoomTemplate>());
            template.name = name;
            template.SetLayout(RoomLayouts.All[name]);
            return template;
        }

        RoomTheme Theme(string name)
        {
            var theme = Track(ScriptableObject.CreateInstance<RoomTheme>());
            theme.name = name;
            return theme;
        }

        void AddShop(FloorConfig config)
        {
            var shopObject = Track(new GameObject("Shop"));
            shopObject.SetActive(false);
            config.ConfigureShop(Template(RoomLayouts.Small), shopObject.AddComponent<ShopRoom>());
        }

        [Test]
        public void Bands_SplitThePath_InThirds()
        {
            var bands = Enumerable.Range(1, 12).Select(depth => FloorBuilder.BandOf(depth, 12, 3)).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 }, bands, "Con 12 salas: 1–4 Someras, 5–8 Arrecife, 9–12 Abismo.");
            Assert.AreEqual(0, FloorBuilder.BandOf(0, 12, 3), "El inicio va en la primera franja.");
            Assert.AreEqual(2, FloorBuilder.BandOf(10, 10, 3), "La última sala siempre en la última franja.");
        }

        [Test]
        public void Layout_OfFloor2_HasTenToTwelveCombatRooms_AndRewardsUpToTheTenth()
        {
            for (int seed = 1; seed <= 100; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                string context = $"semilla {seed}";
                int combat = layout.Rooms.Count(r => r.Role == RoomRole.Combat);
                Assert.That(combat, Is.InRange(10, 12), context);
                Assert.IsNull(layout.Transition, $"El piso 2 todavía no tiene sala de paso ({context}).");
                foreach (var room in layout.Rooms.Where(r => r.Role == RoomRole.Combat))
                    Assert.AreEqual(room.Depth is 2 or 4 or 6 or 8 or 10, room.Reward, $"Recompensa en la sala {room.Depth} ({context}).");
            }
        }

        [Test]
        public void Layout_PutsTheShop_InTheMiddleThirdOfThePath()
        {
            AddShop(floor2);
            floor2.ConfigureShopDepth(new Vector2(1f / 3f, 2f / 3f));
            for (int seed = 1; seed <= 100; seed++)
            {
                var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(seed));
                int combat = layout.Rooms.Count(r => r.Role == RoomRole.Combat);
                var shop = layout.Rooms.Single(r => r.Role == RoomRole.Shop);
                float fraction = (shop.Depth - 1f) / (combat - 1f);
                Assert.That(fraction, Is.InRange(1f / 3f - 0.001f, 2f / 3f + 0.001f), $"semilla {seed}: la tienda cuelga de la sala {shop.Depth} de {combat}.");
            }
        }

        [Test]
        public void Build_GivesEachRoom_TheTilesetOfItsBand()
        {
            var parent = Track(new GameObject("Floor")).transform;
            var layout = FloorLayoutGenerator.Generate(floor2, new System.Random(11));
            var built = FloorBuilder.Build(layout, floor2, parent, new System.Random(11));
            int deepest = layout.Rooms.Where(r => r.Role == RoomRole.Combat).Max(r => r.Depth);

            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                var floorCell = FirstFloorCell(built, built.MapRooms[i].Rect);
                int expected = room.Role == RoomRole.Boss ? 1 : 4 + FloorBuilder.BandOf(room.Depth, deepest, 3);
                Assert.AreEqual(expected, built.Map.GetTheme(floorCell), $"{room.Name}: tileset de su franja.");
            }
        }

        static Vector2Int FirstFloorCell(FloorBuilder.BuiltFloor built, RectInt rect)
        {
            foreach (var cell in rect.allPositionsWithin)
            {
                if (built.Map.Get(cell) == FloorMap.Kind.Floor)
                    return cell;
            }
            throw new AssertionException("La sala no tiene suelo.");
        }

        [Test]
        public void Pool_WithoutNewMouths_NeverOffersAMouth()
        {
            var mouthA = Mutation(MutationSlot.Mouth);
            var mouthB = Mutation(MutationSlot.Mouth);
            var limbs = Mutation(MutationSlot.Limbs);
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(new[] { mouthA, mouthB, limbs }, newOffersNewMouths: false);

            var playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Combat.Weapon>();
            playerObject.AddComponent<Player.PlayerController>().readInput = false;
            var player = playerObject.AddComponent<PlayerMutations>();

            for (int i = 0; i < 30; i++)
            {
                Assert.IsFalse(pool.PickOffer(3, player).Any(m => m.Slot == MutationSlot.Mouth), "Las recompensas no ofrecen Bocas nuevas.");
                Assert.IsFalse(pool.PickOffer(3, System.Array.Empty<MutationData>()).Any(m => m.Slot == MutationSlot.Mouth), "La tienda tampoco.");
            }
            CollectionAssert.Contains(pool.PickOffer(3, player), limbs);
        }

        [Test]
        public void Pool_OfFloor1_StillOffersMouths()
        {
            var mouth = Mutation(MutationSlot.Mouth);
            var limbs = Mutation(MutationSlot.Limbs);
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(new[] { mouth, limbs });

            Assert.IsTrue(pool.OffersNewMouths, "Por defecto (piso 1) las Bocas se ofrecen como siempre.");
            CollectionAssert.Contains(pool.PickOffer(2, System.Array.Empty<MutationData>()), mouth);
        }

        [Test]
        public void Pool_OfFloor2_OffersUpgradesOfFloor1Mutations_ButNotNewOnes()
        {
            var oldSkin = Mutation(MutationSlot.Skin);
            var otherOld = Mutation(MutationSlot.Organ);
            var newLimbs = Mutation(MutationSlot.Limbs);
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(new[] { newLimbs }, newOffersNewMouths: false);

            var playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Combat.Weapon>();
            playerObject.AddComponent<Player.PlayerController>().readInput = false;
            var player = playerObject.AddComponent<PlayerMutations>();
            player.Equip(oldSkin);

            for (int i = 0; i < 10; i++)
            {
                var offer = pool.PickOffer(3, player);
                CollectionAssert.Contains(offer, oldSkin, "La mejora de lo que se trae del piso 1 se ofrece aunque no esté en la reserva.");
                CollectionAssert.Contains(offer, newLimbs);
                CollectionAssert.DoesNotContain(offer, otherOld, "Mutaciones nuevas, solo las de la reserva del piso.");
            }
        }

        MutationData Mutation(MutationSlot slot)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase($"{slot}", "", slot, Color.white);
            return mutation;
        }

        (FloorGenerator generator, Transform player, GameManager manager) CreateGame(FloorConfig config)
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            var player = Track(new GameObject("Player")).transform;
            var body = player.gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            var generatorObject = Track(new GameObject("FloorGenerator"));
            generatorObject.SetActive(false);
            var generator = generatorObject.AddComponent<FloorGenerator>();
            generator.Configure(config, player, newFixedSeed: 5);
            generator.SetArrivalFade(0.05f);
            generatorObject.SetActive(true);
            return (generator, player, manager);
        }

        [UnityTest]
        public IEnumerator NextFloor_IsBuiltInTheSameScene_KeepingThePlayer()
        {
            var (generator, player, manager) = CreateGame(floor1);
            Assert.AreEqual(1, FloorGenerator.CurrentFloor);
            Assert.IsTrue(generator.HasNextFloor);
            var oldRooms = generator.Built.Rooms.ToList();
            int changedTo = 0;
            System.Action<int> onChanged = floor => changedTo = floor;
            FloorGenerator.FloorChanged += onChanged;

            // Como tras el salto a la fosa: sin física y en otro sitio.
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.position = new Vector3(999f, 999f, 0f);
            manager.SetState(GameState.Cinematic);

            generator.GoToNextFloor();
            while (generator.ChangingFloor)
                yield return null;
            FloorGenerator.FloorChanged -= onChanged;

            Assert.AreEqual(2, FloorGenerator.CurrentFloor);
            Assert.AreEqual(2, changedTo, "Avisa del piso nuevo (mapa y música).");
            Assert.AreSame(floor2, generator.Config);
            Assert.IsTrue(oldRooms.All(room => room == null), "Las salas del piso 1 ya no existen.");
            Assert.That(generator.Built.Rooms.Count(r => r.name.StartsWith("Room_") && char.IsDigit(r.name[5])), Is.GreaterThanOrEqualTo(10));
            // Con margen: al volver la física, el choque con lo que tiene cerca lo mueve una fracción de píxel.
            Assert.Less(Vector2.Distance(generator.Built.StartPosition, player.position), 0.1f, "El jugador empieza en la sala de inicio del piso 2.");
            Assert.IsTrue(player.GetComponent<Rigidbody2D>().simulated, "Vuelve a tener física.");
            Assert.AreEqual(GameState.Playing, manager.State);
            Assert.IsFalse(generator.HasNextFloor);
        }

        [UnityTest]
        public IEnumerator LastFloor_GivesTheVictory()
        {
            var (generator, _, manager) = CreateGame(floor2);
            yield return null;

            generator.GoToNextFloor();
            Assert.AreEqual(GameState.Victory, manager.State);
            Assert.IsFalse(generator.ChangingFloor);
        }
    }
}
