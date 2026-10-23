using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class LineageEvolutionTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerMutations mutations;
        PlayerLineage lineage;
        MutationLook look;
        LineageEvolution evolution;
        Animator limbs;
        SpriteRenderer body;

        [SetUp]
        public void SetUp()
        {
            playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            mutations = playerObject.AddComponent<PlayerMutations>();

            body = Child(playerObject.transform, "Body").gameObject.AddComponent<SpriteRenderer>();
            playerObject.AddComponent<PlayerAnimator>().Configure(null, body);

            var aimPivot = Child(playerObject.transform, "AimPivot");
            limbs = Piece(playerObject.transform, "Limbs");
            look = playerObject.AddComponent<MutationLook>();
            look.Configure(limbs, Piece(playerObject.transform, "Organ"), Piece(aimPivot, "MouthPiece"), Piece(playerObject.transform, "Cap"));

            lineage = playerObject.AddComponent<PlayerLineage>();
            evolution = playerObject.AddComponent<LineageEvolution>();
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
            Time.timeScale = 1f;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static Animator Piece(Transform parent, string name)
        {
            var piece = Child(parent, name).gameObject;
            piece.AddComponent<SpriteRenderer>().enabled = false;
            return piece.AddComponent<Animator>();
        }

        RuntimeAnimatorController Controller()
        {
            var controller = Track(new UnityEditor.Animations.AnimatorController());
            controller.AddLayer("Base");
            controller.layers[0].stateMachine.AddState("Idle");
            return controller;
        }

        StatMutation Mutation(MutationSlot slot)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(slot.ToString(), "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            mutation.SetLook(Controller());
            return mutation;
        }

        /// <summary>Un linaje que sustituye las Extremidades, sin animación de evolución (dura lo de respaldo).</summary>
        (LineageData data, MutationData[] parts, RuntimeAnimatorController limbsLook) Lineage()
        {
            var parts = new MutationData[] { Mutation(MutationSlot.Mouth), Mutation(MutationSlot.Limbs), Mutation(MutationSlot.Skin), Mutation(MutationSlot.Organ) };
            var data = Track(ScriptableObject.CreateInstance<LineageData>());
            data.Configure("Tiburón", LineageClass.Carnivore, parts, "");
            var limbsLook = Controller();
            data.ConfigureLook(null, 0f, limbsLook, null);
            foreach (var part in parts)
                part.SetLineages(new[] { data });
            return (data, parts, limbsLook);
        }

        [Test]
        public void LineageLook_ReplacesTheLimbs_OnlyWhileItsMutationIsThere()
        {
            var (data, parts, limbsLook) = Lineage();
            mutations.Equip(parts[1]);

            look.SetLineageLook(data);
            Assert.AreSame(limbsLook, limbs.runtimeAnimatorController);

            var other = Mutation(MutationSlot.Limbs);
            mutations.Equip(other);
            Assert.AreSame(other.Look, limbs.runtimeAnimatorController, "Otra mutación de la ranura se ve como siempre.");

            look.SetLineageLook(null);
            mutations.Equip(parts[1]);
            Assert.AreSame(parts[1].Look, limbs.runtimeAnimatorController);
        }

        [UnityTest]
        public IEnumerator CompletingALineage_PlaysTheEvolution_ThenPutsItsPieces()
        {
            var (data, parts, limbsLook) = Lineage();
            foreach (var part in parts)
                mutations.Equip(part);
            Assert.AreSame(data, lineage.Active);

            LineageData started = null;
            evolution.Evolving += l => started = l;
            yield return null;

            Assert.IsTrue(evolution.IsEvolving);
            Assert.AreSame(data, started);
            Assert.IsFalse(body.enabled, "Durante la animación el jugador se oculta.");
            Assert.AreNotSame(limbsLook, limbs.runtimeAnimatorController, "Las piezas cambian al acabar.");

            yield return new WaitForSecondsRealtime(1.3f);

            Assert.IsFalse(evolution.IsEvolving);
            Assert.IsTrue(body.enabled);
            Assert.AreSame(data, look.LineageLook);
            Assert.AreSame(limbsLook, limbs.runtimeAnimatorController);
        }

        [UnityTest]
        public IEnumerator LosingTheLineage_BringsBackTheMutationPieces()
        {
            var (data, parts, _) = Lineage();
            foreach (var part in parts)
                mutations.Equip(part);
            yield return null;
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreSame(data, look.LineageLook);

            var other = Mutation(MutationSlot.Organ);
            mutations.Equip(other);

            Assert.IsNull(lineage.Active);
            Assert.IsNull(look.LineageLook);
            Assert.AreSame(parts[1].Look, limbs.runtimeAnimatorController);
        }

        [UnityTest]
        public IEnumerator DisablingThePlayer_RemovesTheLineage_WithoutThePuff()
        {
            var puff = Track(new GameObject("LostPuff"));
            puff.SetActive(false);
            evolution.Configure(puff);
            var (data, parts, _) = Lineage();
            foreach (var part in parts)
                mutations.Equip(part);
            yield return null;
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreSame(data, look.LineageLook);

            // Al cerrar la escena el jugador se desactiva: crear el pof entonces deja restos en la escena siguiente.
            playerObject.SetActive(false);

            Assert.IsNull(lineage.Active);
            Assert.IsFalse(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Any(t => t.name.StartsWith("LostPuff(Clone)")), "No se crea el pof al desactivarse.");
        }

        [UnityTest]
        public IEnumerator Evolution_WaitsWhileTheGameIsInAMenu()
        {
            var manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            manager.SetState(GameState.MutationChoice);

            var (_, parts, _) = Lineage();
            foreach (var part in parts)
                mutations.Equip(part);
            yield return null;
            Assert.IsFalse(evolution.IsEvolving);
            Assert.IsNotNull(evolution.Pending);

            manager.SetState(GameState.Playing);
            yield return null;
            Assert.IsTrue(evolution.IsEvolving);
            Assert.AreEqual(GameState.Evolution, manager.State);
            Assert.AreEqual(0f, Time.timeScale, "El juego se congela.");

            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual(GameState.Playing, manager.State);
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
