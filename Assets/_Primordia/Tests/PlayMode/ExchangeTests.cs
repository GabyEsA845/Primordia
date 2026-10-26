using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>
    /// Sala de cambio (el laboratorio de Darwin): entregas la mutación de una ranura y recibes otra del piso de esa ranura,
    /// al mismo nivel (uno menos si la entregada es de un piso anterior); las especiales ni se dan ni se reciben, y la
    /// cápsula solo se usa una vez.
    /// </summary>
    public class ExchangeTests
    {
        readonly List<Object> created = new();
        PlayerMutations player;

        [SetUp]
        public void SetUp()
        {
            var playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            player = playerObject.AddComponent<PlayerMutations>();
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

        MutationData Mutation(string name, MutationSlot slot, int floor, WeaponFamily family = WeaponFamily.Projectile,
            bool special = false)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "", slot, Color.white);
            mutation.SetFloor(floor, family);
            mutation.SetSpecial(special);
            return mutation;
        }

        MutationPool Pool(params MutationData[] mutations)
        {
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(mutations);
            return pool;
        }

        void EquipAt(MutationData mutation, int level)
        {
            player.Equip(mutation);
            for (int i = 1; i < level; i++)
                player.LevelUp(mutation.Slot);
        }

        [Test]
        public void Options_AreTheFloorsOthersOfThatSlot_NotSpecialsNorOtherFloors()
        {
            var jelly = Mutation("Cnidocitos", MutationSlot.Mouth, 2, WeaponFamily.Projectile);
            var fish = Mutation("Placas", MutationSlot.Mouth, 2, WeaponFamily.Melee);
            var crown = Mutation("Corona", MutationSlot.Mouth, 2, WeaponFamily.Area);
            var special = Mutation("Especial", MutationSlot.Mouth, 2, special: true);
            var old = Mutation("Aguijón", MutationSlot.Mouth, 1);
            var tail = Mutation("Cola", MutationSlot.Limbs, 2);
            EquipAt(jelly, 1);

            var options = MutationExchange.Options(Pool(jelly, fish, crown, special, old, tail), player, MutationSlot.Mouth, 2);

            CollectionAssert.AreEquivalent(new[] { fish, crown }, options);
        }

        [Test]
        public void Exchange_KeepsTheLevel_WithinTheSameFloor()
        {
            var jelly = Mutation("Cnidocitos", MutationSlot.Mouth, 2);
            var fish = Mutation("Placas", MutationSlot.Mouth, 2, WeaponFamily.Melee);
            EquipAt(jelly, 3);

            Assert.AreEqual(3, MutationExchange.Apply(player, fish, 2));
            Assert.AreSame(fish, player.Get(MutationSlot.Mouth));
            Assert.AreEqual(3, player.LevelOf(MutationSlot.Mouth));
            Assert.IsFalse(player.IsEquipped(jelly));
        }

        [Test]
        public void Exchange_FromAnEarlierFloor_EntersOneLevelLower_AtLeastI()
        {
            var legs = Mutation("Patas", MutationSlot.Limbs, 1);
            var tail = Mutation("Cola", MutationSlot.Limbs, 2);
            EquipAt(legs, 2);
            Assert.AreEqual(1, MutationExchange.LevelFor(player, MutationSlot.Limbs, 2));
            Assert.AreEqual(1, MutationExchange.Apply(player, tail, 2));

            var plates = Mutation("Placas", MutationSlot.Skin, 1);
            var wiwaxia = Mutation("Wiwaxia", MutationSlot.Skin, 2);
            EquipAt(plates, 3);
            Assert.AreEqual(2, MutationExchange.Apply(player, wiwaxia, 2), "Un nivel III del piso 1 da un nivel II.");

            var spikes = Mutation("Púas", MutationSlot.Organ, 1);
            var eyes = Mutation("Ojos", MutationSlot.Organ, 2);
            EquipAt(spikes, 1);
            Assert.AreEqual(1, MutationExchange.Apply(player, eyes, 2), "Como mínimo, el nivel I.");
        }

        [Test]
        public void Specials_CannotBeGiven()
        {
            var special = Mutation("Cutícula", MutationSlot.Skin, 2, special: true);
            var wiwaxia = Mutation("Wiwaxia", MutationSlot.Skin, 2);
            EquipAt(special, 1);

            Assert.IsFalse(MutationExchange.CanGive(player, MutationSlot.Skin));
            Assert.IsEmpty(MutationExchange.Options(Pool(wiwaxia), player, MutationSlot.Skin, 2));
            Assert.AreEqual(0, MutationExchange.Apply(player, wiwaxia, 2));
            Assert.AreSame(special, player.Get(MutationSlot.Skin));
            Assert.IsFalse(MutationExchange.CanGive(player, MutationSlot.Organ), "Ni una ranura vacía.");
        }

        [Test]
        public void ChangesBody_OnlyWhenTheMouthFamilyChanges()
        {
            var jelly = Mutation("Cnidocitos", MutationSlot.Mouth, 2, WeaponFamily.Projectile);
            var fish = Mutation("Placas", MutationSlot.Mouth, 2, WeaponFamily.Melee);
            var spit = Mutation("Escupitajo", MutationSlot.Mouth, 2, WeaponFamily.Projectile);
            EquipAt(jelly, 1);

            Assert.IsTrue(MutationExchange.ChangesBody(player, fish));
            Assert.IsFalse(MutationExchange.ChangesBody(player, spit));
        }

        [Test]
        public void Capsule_IsUsedOnlyOncePerFloor()
        {
            var jelly = Mutation("Cnidocitos", MutationSlot.Mouth, 2);
            var fish = Mutation("Placas", MutationSlot.Mouth, 2, WeaponFamily.Melee);
            var crown = Mutation("Corona", MutationSlot.Mouth, 2, WeaponFamily.Area);
            EquipAt(jelly, 2);
            var capsule = Track(new GameObject("Capsule")).AddComponent<ExchangeCapsule>();
            capsule.SetFloor(Pool(jelly, fish, crown), 2);
            MutationData received = null;
            capsule.Exchanged += mutation => received = mutation;

            capsule.Exchange(player, fish);
            Assert.IsTrue(capsule.Used);
            Assert.IsFalse(capsule.IsAvailable, "Gastada, ya no se puede usar.");
            Assert.AreSame(fish, received);
            Assert.AreEqual(2, player.LevelOf(MutationSlot.Mouth));

            capsule.Exchange(player, crown);
            Assert.AreSame(fish, player.Get(MutationSlot.Mouth), "Solo un cambio por piso.");
        }
    }
}
