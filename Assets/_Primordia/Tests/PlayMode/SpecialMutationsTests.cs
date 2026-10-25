using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Mutaciones especiales de las almejas, paso 1 (lo común): el precio Inestable (+½ corazón por golpe y por especial,
    /// solo en golpes), el comodín de linaje (completa uno si se llevan las otras 3; si completa dos, se elige), las mejoras
    /// en las ofertas aunque no estén en la reserva, los textos y que dos almejas no guarden la misma.
    /// </summary>
    public class SpecialMutationsTests
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

        StatMutation Mutation(string name, MutationSlot slot, bool special = false)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(name, "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            mutation.SetSpecial(special);
            return mutation;
        }

        LineageData Lineage(string name, LineageClass lineageClass, params MutationData[] parts)
        {
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure(name, lineageClass, parts, "");
            foreach (var part in parts)
            {
                var list = new List<LineageData>(part.Lineages) { data };
                part.SetLineages(list.ToArray());
            }
            return data;
        }

        [Test]
        public void Unstable_AddsHalfAHeartToHits_ButNotToTicksNorFalls()
        {
            var health = Track(new GameObject("Health")).AddComponent<Health>();
            health.ExtraHitDamage = 1;
            int extraSeen = 0;
            health.ExtraDamageTaken += extra => extraSeen += extra;

            Assert.IsTrue(health.TakeDamage(1));
            Assert.AreEqual(4, health.Current, "Un golpe de medio corazón quita uno entero.");
            Assert.AreEqual(1, extraSeen);

            health.TakeDamage(1, DamageKind.Tick);
            Assert.AreEqual(3, health.Current, "El veneno no lleva el extra.");
            health.TakeDamage(1, DamageKind.Fall);
            Assert.AreEqual(2, health.Current, "Caer a una fosa tampoco.");
            Assert.AreEqual(1, extraSeen);
        }

        [Test]
        public void Unstable_WholeHitBlockers_StopTheExtraToo()
        {
            var health = Track(new GameObject("Health")).AddComponent<Health>();
            health.ExtraHitDamage = 2;
            health.AddPurpleHeart();
            health.Barrier = 1;

            health.TakeDamage(1);
            health.TakeDamage(1);
            Assert.AreEqual(6, health.Current, "El corazón morado y el verde paran el golpe con su extra.");
            health.TakeDamage(1);
            Assert.AreEqual(3, health.Current, "Con dos especiales, medio corazón más uno.");
        }

        [Test]
        public void Unstable_StacksWithEachSpecial_AndGoesAwayWithThem()
        {
            mutations.Equip(Mutation("Flagelos", MutationSlot.Limbs, special: true));
            Assert.AreEqual(1, mutations.Health.ExtraHitDamage);
            mutations.Equip(Mutation("Cutícula", MutationSlot.Skin, special: true));
            Assert.AreEqual(2, mutations.Health.ExtraHitDamage, "Se acumula: medio corazón por especial.");
            mutations.Equip(Mutation("Placas", MutationSlot.Skin));
            Assert.AreEqual(1, mutations.Health.ExtraHitDamage, "Reemplazar la especial quita su parte.");
            mutations.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(0, mutations.Health.ExtraHitDamage);
        }

        [Test]
        public void Wildcard_CompletesALineage_OnlyWithTheOtherThree()
        {
            var mouth = Mutation("Placas mandibulares", MutationSlot.Mouth);
            var tail = Mutation("Cola", MutationSlot.Limbs);
            var plates = Mutation("Placas de trilobite", MutationSlot.Skin);
            var eyes = Mutation("Ojos", MutationSlot.Organ);
            var placoderm = Lineage("Placodermo", LineageClass.Carnivore, mouth, tail, plates, eyes);

            mutations.Equip(mouth);
            mutations.Equip(eyes);
            mutations.Equip(Mutation("Flagelos", MutationSlot.Limbs, special: true));
            mutations.Equip(Mutation("Cutícula", MutationSlot.Skin, special: true));
            Assert.AreEqual(3, placoderm.Progress(mutations), "Solo cuenta un comodín.");
            Assert.IsNull(lineage.Active, "Con dos de las suyas y dos especiales no se completa.");

            mutations.Equip(plates);
            Assert.AreSame(placoderm, lineage.Active, "Con las otras 3, la especial de Extremidades lo completa.");
            Assert.IsTrue(placoderm.CompletedByWildcard(mutations));

            mutations.Equip(tail);
            Assert.AreSame(placoderm, lineage.Active);
            Assert.IsFalse(placoderm.CompletedByWildcard(mutations));
        }

        [Test]
        public void Wildcard_CompletingTwoLineages_AsksToChoose_AndRemembersTheChoice()
        {
            var mouth = Mutation("Placas mandibulares", MutationSlot.Mouth);
            var plates = Mutation("Placas de trilobite", MutationSlot.Skin);
            var eyes = Mutation("Ojos", MutationSlot.Organ);
            var placoderm = Lineage("Placodermo", LineageClass.Carnivore, mouth, Mutation("Cola", MutationSlot.Limbs), plates, eyes);
            var trilobite = Lineage("Trilobite", LineageClass.Herbivore, mouth, Mutation("Espinas", MutationSlot.Limbs), plates, eyes);

            var asked = new List<IReadOnlyList<LineageData>>();
            lineage.ChoiceNeeded += options => asked.Add(options.ToList());

            mutations.Equip(mouth);
            mutations.Equip(plates);
            mutations.Equip(eyes);
            mutations.Equip(Mutation("Flagelos", MutationSlot.Limbs, special: true));

            Assert.AreEqual(1, asked.Count, "Pregunta al completar dos a la vez.");
            CollectionAssert.AreEquivalent(new[] { placoderm, trilobite }, asked[0]);
            Assert.IsTrue(lineage.AwaitingChoice);
            Assert.IsNull(lineage.Active, "Ninguno hasta que elija.");

            lineage.Choose(trilobite);
            Assert.AreSame(trilobite, lineage.Active);
            Assert.IsFalse(lineage.AwaitingChoice);

            // Subir de nivel no vuelve a preguntar: sigue el elegido.
            mutations.Equip(eyes);
            Assert.AreSame(trilobite, lineage.Active);
            Assert.AreEqual(1, asked.Count);

            // Desde la pausa se puede cambiar al otro.
            lineage.Choose(placoderm);
            Assert.AreSame(placoderm, lineage.Active);
        }

        [Test]
        public void Wildcard_WithoutAnyoneToAsk_TakesTheFirst()
        {
            var mouth = Mutation("Placas mandibulares", MutationSlot.Mouth);
            var plates = Mutation("Placas de trilobite", MutationSlot.Skin);
            var eyes = Mutation("Ojos", MutationSlot.Organ);
            Lineage("Placodermo", LineageClass.Carnivore, mouth, Mutation("Cola", MutationSlot.Limbs), plates, eyes);
            Lineage("Trilobite", LineageClass.Herbivore, mouth, Mutation("Espinas", MutationSlot.Limbs), plates, eyes);

            foreach (var m in new MutationData[] { mouth, plates, eyes, Mutation("Flagelos", MutationSlot.Limbs, special: true) })
                mutations.Equip(m);

            Assert.IsNotNull(lineage.Active);
            Assert.AreEqual(2, lineage.Candidates.Count);
        }

        [Test]
        public void Offers_IncludeUpgradesOfEquippedSpecials_ThoughTheyAreNotInThePool()
        {
            var special = Mutation("Yema clonal", MutationSlot.Organ, special: true);
            var pool = Track(ScriptableObject.CreateInstance<MutationPool>());
            pool.Configure(new MutationData[] { Mutation("Ojos", MutationSlot.Organ), Mutation("Cola", MutationSlot.Limbs) });
            mutations.Equip(special);

            for (int i = 0; i < 10; i++)
                CollectionAssert.Contains(pool.PickOffer(3, mutations), special, "Su mejora sale siempre (es la única posible).");

            mutations.LevelUp(MutationSlot.Organ);
            mutations.LevelUp(MutationSlot.Organ);
            CollectionAssert.DoesNotContain(pool.PickOffer(3, mutations), special, "En nivel III ya no se ofrece.");
        }

        [Test]
        public void Texts_SaySpecial_AndHalfHearts()
        {
            var special = Mutation("Flagelos", MutationSlot.Limbs, special: true);
            Assert.AreEqual(MutationText.SpecialLine, MutationText.LineageList(special, mutations));
            Assert.AreEqual(MutationText.SpecialLine, MutationText.LineageProgress(special, mutations));
            Assert.AreEqual("+½", MutationText.HalfHearts(1));
            Assert.AreEqual("+1", MutationText.HalfHearts(2));
            Assert.AreEqual("+1½", MutationText.HalfHearts(3));
        }

        [Test]
        public void TwoBigClams_DoNotKeepTheSameSpecial_UnlessTheyRunOut()
        {
            var a = Mutation("Flagelos", MutationSlot.Limbs, special: true);
            var b = Mutation("Cutícula", MutationSlot.Skin, special: true);
            var options = new MutationData[] { a, b };
            var random = new System.Random(3);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(b, Clam.PickMutation(options, mutations, random, new HashSet<MutationData> { a }));
                Assert.IsNotNull(Clam.PickMutation(options, mutations, random, new HashSet<MutationData> { a, b }), "Si ya están todas, repite.");
            }
            mutations.Equip(b);
            Assert.AreSame(a, Clam.PickMutation(options, mutations, random, new HashSet<MutationData> { a }),
                "Nunca la que ya lleva, aunque otra almeja guarde la otra.");
        }

        [UnityTest]
        public IEnumerator BigClam_DropsTheSpecial_TowardsThePlayer_ReadyToPickUp()
        {
            var special = Mutation("Flagelos", MutationSlot.Limbs, special: true);
            var template = Track(new GameObject("PickupTemplate"));
            template.AddComponent<CircleCollider2D>();
            template.AddComponent<MutationPickup>();
            // Activa (las copias salen como el original), lejos de todo.
            template.transform.position = new Vector3(100f, 100f);

            var clamObject = Track(new GameObject("ClamBig"));
            clamObject.transform.position = new Vector3(10f, 10f);
            var clam = clamObject.AddComponent<Clam>();
            clam.Configure(clamObject.AddComponent<SpriteRenderer>(), null, null, null, true, Vector2Int.zero, special,
                template.GetComponent<MutationPickup>(), null, null);

            // El jugador abre la almeja desde abajo.
            mutations.transform.position = new Vector3(10f, 8.5f);
            clam.Interact(mutations.gameObject);
            yield return new WaitForSeconds(0.1f);

            var bubble = Object.FindObjectsByType<MutationPickup>(FindObjectsSortMode.None).FirstOrDefault(p => p.Mutation == special);
            Assert.IsNotNull(bubble, "La almeja grande suelta su especial.");
            Track(bubble.gameObject);
            Assert.IsFalse(bubble.GetComponent<Collider2D>().enabled, "No se coge en el aire.");

            yield return new WaitForSeconds(0.6f);
            if (bubble != null)
            {
                Assert.Less(bubble.transform.position.y, 9.2f, "Cae delante de la almeja, hacia el jugador (abajo).");
                Assert.LessOrEqual(Vector2.Distance(bubble.transform.position, clam.transform.position), 1.25f);
                Assert.IsTrue(bubble.GetComponent<Collider2D>().enabled, "Al caer ya se puede coger.");
            }
            else
            {
                Assert.IsTrue(mutations.IsEquipped(special), "Si ya no está, es que la ha cogido el jugador al caerle encima.");
            }
        }

        [UnityTest]
        public IEnumerator Hearts_GetTheGoldenCrackedBorder_WhileUnstable()
        {
            var health = mutations.Health;
            var canvas = Track(new GameObject("Canvas", typeof(Canvas)));
            var display = new GameObject("Hearts", typeof(RectTransform)).AddComponent<HeartsDisplay>();
            display.transform.SetParent(canvas.transform, false);
            var texture = Track(new Texture2D(16, 16));
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f));
            display.Configure(health, sprite, sprite, sprite);
            display.SetUnstableArt(new[] { sprite }, new[] { sprite }, new[] { sprite });
            yield return null;

            Assert.AreEqual(0, display.UnstableShown);
            mutations.Equip(Mutation("Flagelos", MutationSlot.Limbs, special: true));
            Assert.AreEqual(3, display.UnstableShown, "Los 3 corazones llevan el borde dorado.");

            health.TakeDamage(1);
            Assert.IsNotNull(canvas.GetComponentsInChildren<UISpriteAnimation>(true).FirstOrDefault(a => a.name == "UnstableBreak"),
                "El medio corazón extra se rompe con su animación.");

            mutations.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(0, display.UnstableShown);
        }
    }
}
