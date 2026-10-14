using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Tests
{
    public class LineageTests
    {
        readonly List<Object> created = new();
        PlayerMutations mutations;
        PlayerLineage lineage;

        [SetUp]
        public void SetUp()
        {
            var player = Track(new GameObject("Player"));
            player.AddComponent<Weapon>().Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            player.AddComponent<PlayerController>().readInput = false;
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

        StatMutation Mutation(string name, MutationSlot slot)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            return mutation;
        }

        /// <summary>Crea el linaje y lo apunta en cada una de sus mutaciones, como hace LineageSetup.</summary>
        LineageData Lineage(string name, LineageClass lineageClass, params MutationData[] parts)
        {
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure(name, lineageClass, parts, "");
            foreach (var part in parts)
            {
                if (part == null)
                    continue;
                var list = new List<LineageData>(part.Lineages) { data };
                part.SetLineages(list.ToArray());
            }
            return data;
        }

        [Test]
        public void FourMutationsOfTheLineage_ActivateIt_AndChangingOneLosesIt()
        {
            var mandibles = Mutation("Mandíbulas", MutationSlot.Mouth);
            var fins = Mutation("Aletas", MutationSlot.Limbs);
            var carapace = Mutation("Caparazón", MutationSlot.Skin);
            var gland = Mutation("Glándula", MutationSlot.Organ);
            var shark = Lineage("Tiburón", LineageClass.Carnivore, mandibles, fins, carapace, gland);

            var changes = new List<(LineageData, LineageData)>();
            lineage.ActiveChanged += (old, active) => changes.Add((old, active));

            mutations.Equip(mandibles);
            mutations.Equip(fins);
            mutations.Equip(carapace);
            Assert.IsNull(lineage.Active, "Con 3 de 4 todavía no hay linaje.");
            Assert.AreEqual(3, shark.Progress(mutations));

            mutations.Equip(gland);
            Assert.AreSame(shark, lineage.Active);
            Assert.AreEqual(1, changes.Count);

            // Subir de nivel no cambia nada.
            mutations.Equip(fins);
            Assert.AreSame(shark, lineage.Active);
            Assert.AreEqual(1, changes.Count);

            mutations.Equip(Mutation("Patas", MutationSlot.Limbs));
            Assert.IsNull(lineage.Active);
            Assert.AreEqual((shark, (LineageData)null), changes[1]);
        }

        [Test]
        public void SharedMutation_CountsForEveryLineage_AndTheCompleteOneWins()
        {
            var mandibles = Mutation("Mandíbulas", MutationSlot.Mouth);
            var fins = Mutation("Aletas", MutationSlot.Limbs);
            var legs = Mutation("Patas", MutationSlot.Limbs);
            var carapace = Mutation("Caparazón", MutationSlot.Skin);
            var gland = Mutation("Glándula", MutationSlot.Organ);
            var vacuole = Mutation("Vacuola", MutationSlot.Organ);
            var shark = Lineage("Tiburón", LineageClass.Carnivore, mandibles, fins, carapace, gland);
            var turtle = Lineage("Tortuga", LineageClass.Herbivore, mandibles, legs, carapace, vacuole);

            foreach (var m in new MutationData[] { mandibles, legs, carapace, vacuole })
                mutations.Equip(m);

            Assert.AreSame(turtle, lineage.Active);
            Assert.AreEqual(2, shark.Progress(mutations));
            Assert.AreEqual("Linajes: Tiburón 2/4, Tortuga (completo)", MutationText.LineageList(mandibles, mutations));
            Assert.AreEqual("Linajes: Tiburón, Tortuga", MutationText.LineageList(mandibles));
        }

        [Test]
        public void LineageWithAMissingMutation_NeverCompletes()
        {
            var spores = Mutation("Esporas", MutationSlot.Mouth);
            var skin = Mutation("Piel urticante", MutationSlot.Skin);
            var gland = Mutation("Vacuola", MutationSlot.Organ);
            var mold = Lineage("Moho", LineageClass.Fungal, spores, null, skin, gland);

            mutations.Equip(spores);
            mutations.Equip(Mutation("Aletas", MutationSlot.Limbs));
            mutations.Equip(skin);
            mutations.Equip(gland);

            Assert.IsNull(lineage.Active);
            Assert.AreEqual(3, mold.Progress(mutations));
        }

        [Test]
        public void Hint_LightsUpTheMutationThatAdvancesAStartedLineage()
        {
            // El ejemplo del GDD: Mandíbulas, Piel urticante y Glándula de choque; salen las Aletas.
            var mandibles = Mutation("Mandíbulas", MutationSlot.Mouth);
            var stinger = Mutation("Aguijón", MutationSlot.Mouth);
            var fins = Mutation("Aletas", MutationSlot.Limbs);
            var legs = Mutation("Patas", MutationSlot.Limbs);
            var carapace = Mutation("Caparazón", MutationSlot.Skin);
            var stinging = Mutation("Piel urticante", MutationSlot.Skin);
            var gland = Mutation("Glándula", MutationSlot.Organ);
            var vacuole = Mutation("Vacuola", MutationSlot.Organ);
            Lineage("Tiburón", LineageClass.Carnivore, mandibles, fins, carapace, gland);
            Lineage("Medusa", LineageClass.Venomous, stinger, fins, stinging, gland);
            Lineage("Tortuga", LineageClass.Herbivore, mandibles, legs, carapace, vacuole);

            mutations.Equip(mandibles);
            mutations.Equip(stinging);
            mutations.Equip(gland);

            Assert.IsTrue(LineageData.Advances(fins, mutations), "Las Aletas llevan el Tiburón y la Medusa a 3/4.");
            Assert.IsTrue(LineageData.Advances(carapace, mutations), "El Caparazón lleva el Tiburón a 3/4.");
            Assert.IsFalse(LineageData.Advances(legs, mutations), "Las Patas solo llevan la Tortuga a 2/4, por debajo del 2/4 que ya tiene.");
            Assert.IsFalse(LineageData.Advances(gland, mutations), "Ya la lleva: subir de nivel no añade partes.");

            mutations.Equip(fins);
            mutations.Equip(carapace);
            Assert.IsNotNull(lineage.Active);
            Assert.IsFalse(LineageData.Advances(stinger, mutations), "Con un linaje completo no hay pistas.");
        }

        [Test]
        public void MutationWithoutLineages_ShowsNoLineageLine()
        {
            Assert.AreEqual("", MutationText.LineageList(Mutation("Suelta", MutationSlot.Mouth), mutations));
            Assert.AreEqual("Texto.", MutationText.WithLine("Texto.", ""));
            Assert.AreEqual("Texto.\n\nLinajes: X", MutationText.WithLine("Texto.", "Linajes: X"));
        }
    }
}
