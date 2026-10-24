using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    public class MouthChoiceTests
    {
        readonly List<Object> created = new();
        PlayerMutations mutations;

        [SetUp]
        public void SetUp()
        {
            var playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            mutations = playerObject.AddComponent<PlayerMutations>();
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

        StatMutation Mutation(MutationSlot slot, string name)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "Base.", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            return mutation;
        }

        void Equip(MutationData mutation, int level)
        {
            for (int i = 0; i < level; i++)
                mutations.Equip(mutation);
        }

        [Test]
        public void Plan_GivesTheExtraLevels_ToMutationsThatCanStillGrow()
        {
            Equip(Mutation(MutationSlot.Mouth, "Mandíbulas"), 3);
            var fins = Mutation(MutationSlot.Limbs, "Aletas");
            Equip(fins, 1);
            Equip(Mutation(MutationSlot.Skin, "Caparazón"), 3);

            var plan = MouthChange.Plan(mutations, new System.Random(1));

            Assert.AreEqual(2, plan.Count, "Mandíbulas III tiene dos niveles por encima del I.");
            Assert.IsTrue(plan.All(transfer => transfer.Slot == MutationSlot.Limbs), "El Caparazón ya está al máximo.");

            var newMouth = Mutation(MutationSlot.Mouth, "Placas mandibulares");
            MouthChange.Apply(mutations, newMouth, plan);
            Assert.AreEqual(newMouth, mutations.Get(MutationSlot.Mouth));
            Assert.AreEqual(1, mutations.LevelOf(MutationSlot.Mouth), "La Boca nueva empieza en el I.");
            Assert.AreEqual(3, mutations.LevelOf(fins));
        }

        [Test]
        public void Plan_LosesTheLevels_ThatFitNowhere()
        {
            Equip(Mutation(MutationSlot.Mouth, "Mandíbulas"), 3);
            Equip(Mutation(MutationSlot.Limbs, "Aletas"), 2);

            var plan = MouthChange.Plan(mutations, new System.Random(3));

            Assert.AreEqual(2, plan.Count);
            Assert.AreEqual(1, plan.Count(transfer => transfer.Slot == MutationSlot.Limbs));
            Assert.AreEqual(1, plan.Count(transfer => transfer.Lost), "El que no cabe se pierde, no vuelve a la Boca.");
            StringAssert.Contains("los que no quepan se perderán", MutationText.MouthChangeNote(mutations));
        }

        [Test]
        public void Plan_SpreadsAtRandom_WithoutGoingOverTheMaximum()
        {
            Equip(Mutation(MutationSlot.Mouth, "Mandíbulas"), 3);
            Equip(Mutation(MutationSlot.Limbs, "Aletas"), 2);
            Equip(Mutation(MutationSlot.Organ, "Vejiga"), 2);

            var targets = new HashSet<MutationSlot?>();
            for (int seed = 1; seed <= 50; seed++)
            {
                var plan = MouthChange.Plan(mutations, new System.Random(seed));
                Assert.IsFalse(plan.Any(transfer => transfer.Lost), $"Caben los dos (semilla {seed}).");
                Assert.IsTrue(plan.Select(transfer => transfer.Slot).Distinct().Count() == 2, $"Uno a cada una (semilla {seed}).");
                foreach (var transfer in plan)
                    targets.Add(transfer.Slot);
            }
            CollectionAssert.AreEquivalent(new MutationSlot?[] { MutationSlot.Limbs, MutationSlot.Organ }, targets);
        }

        [Test]
        public void Plan_IsEmpty_WithoutMouthOrWithItAtLevelOne()
        {
            Assert.IsEmpty(MouthChange.Plan(mutations, new System.Random(1)));
            Assert.AreEqual("", MutationText.MouthChangeNote(mutations), "Sin Boca no hay aviso.");

            Equip(Mutation(MutationSlot.Mouth, "Aguijón"), 1);
            Equip(Mutation(MutationSlot.Limbs, "Aletas"), 1);
            Assert.IsEmpty(MouthChange.Plan(mutations, new System.Random(1)));
            StringAssert.Contains("se queda atrás", MutationText.MouthChangeNote(mutations));
        }

        [UnityTest]
        public IEnumerator Window_Choosing_EquipsTheMouth_AndCallsBackWhenTheSpreadEnds()
        {
            Equip(Mutation(MutationSlot.Mouth, "Mandíbulas"), 2);
            var fins = Mutation(MutationSlot.Limbs, "Aletas");
            Equip(fins, 1);

            var canvas = Track(new GameObject("Canvas", typeof(RectTransform)));
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var cards = new MouthCard[2];
            for (int i = 0; i < cards.Length; i++)
            {
                var cardObject = new GameObject($"Card_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObject.transform.SetParent(panel.transform, false);
                cards[i] = cardObject.AddComponent<MouthCard>();
            }
            var window = canvas.AddComponent<MouthChoiceUI>();
            window.Configure(panel, null, null, cards, null, new Image[4]);
            window.SetTiming(0f, 0f, 0.05f, 0f, 0.1f);

            var offered = new MutationData[] { Mutation(MutationSlot.Mouth, "Cnidocitos"), Mutation(MutationSlot.Mouth, "Placas") };
            bool finished = false;
            window.Show(offered, mutations, "océano cámbrico", () => finished = true);

            Assert.IsTrue(window.IsOpen);
            window.Choose(1);
            Assert.IsTrue(window.IsDistributing);
            window.Choose(0);
            Assert.AreEqual(offered[1], window.Chosen, "Solo cuenta la primera elección.");

            for (float t = 0f; t < 2f && !finished; t += Time.unscaledDeltaTime)
                yield return null;

            Assert.IsTrue(finished, "Avisa al acabar el reparto.");
            Assert.IsFalse(window.IsOpen);
            Assert.AreEqual(offered[1], mutations.Get(MutationSlot.Mouth));
            Assert.AreEqual(1, mutations.LevelOf(MutationSlot.Mouth));
            Assert.AreEqual(2, mutations.LevelOf(fins), "El nivel extra de Mandíbulas II ha ido a las Aletas.");
        }
    }
}
