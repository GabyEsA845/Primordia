using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class MetamorphosisTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerMutations mutations;
        PlayerAnimator playerAnimator;
        MutationLook look;
        Animator bodyAnimator;
        SpriteRenderer limbsRenderer;
        int seenBefore;

        [SetUp]
        public void SetUp()
        {
            seenBefore = PlayerPrefs.GetInt(Metamorphosis.SeenKey, 0);
            PlayerPrefs.DeleteKey(Metamorphosis.SeenKey);

            playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            playerObject.AddComponent<PlayerController>().readInput = false;
            mutations = playerObject.AddComponent<PlayerMutations>();

            var bodyObject = Child(playerObject.transform, "Body").gameObject;
            var body = bodyObject.AddComponent<SpriteRenderer>();
            bodyAnimator = bodyObject.AddComponent<Animator>();
            bodyAnimator.runtimeAnimatorController = Controller("Idle_side", "Idle_down", "Idle_up");
            playerAnimator = playerObject.AddComponent<PlayerAnimator>();
            playerAnimator.Configure(bodyAnimator, body);

            var aimPivot = Child(playerObject.transform, "AimPivot");
            var limbs = Piece(playerObject.transform, "Limbs");
            limbsRenderer = limbs.GetComponent<SpriteRenderer>();
            look = playerObject.AddComponent<MutationLook>();
            look.Configure(limbs, Piece(playerObject.transform, "Organ"), Piece(aimPivot, "MouthPiece"), Piece(playerObject.transform, "Cap"));
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
            PlayerPrefs.SetInt(Metamorphosis.SeenKey, seenBefore);
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

        Animator Piece(Transform parent, string name)
        {
            var piece = Child(parent, name).gameObject;
            piece.AddComponent<SpriteRenderer>().enabled = false;
            var animator = piece.AddComponent<Animator>();
            return animator;
        }

        RuntimeAnimatorController Controller(params string[] states)
        {
            var controller = Track(new UnityEditor.Animations.AnimatorController());
            controller.AddLayer("Base");
            foreach (var state in states)
                controller.layers[0].stateMachine.AddState(state);
            return controller;
        }

        MutationData Mutation(MutationSlot slot, RuntimeAnimatorController piece = null)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(slot.ToString(), "", slot, Color.white);
            mutation.Configure(1f, 1f, 0);
            if (piece != null)
                mutation.SetLook(piece);
            return mutation;
        }

        MutationData CreatureMouth(out RuntimeAnimatorController creature)
        {
            creature = Controller("Idle_side", "Idle_down", "Idle_up", "Attack_side", "Attack_down", "Attack_up");
            var mouth = Mutation(MutationSlot.Mouth);
            mouth.SetFloor(2, WeaponFamily.Melee);
            mouth.SetCreature(creature, null, null);
            return mouth;
        }

        [UnityTest]
        public IEnumerator CreatureMouth_ReplacesCellAndSkin_AndHidesThePieces()
        {
            var cell = bodyAnimator.runtimeAnimatorController;
            var skinLook = Controller("Idle_side", "Idle_down", "Idle_up");
            mutations.Equip(Mutation(MutationSlot.Skin, skinLook));
            mutations.Equip(Mutation(MutationSlot.Limbs, Controller("Idle")));
            yield return null;
            Assert.AreEqual(skinLook, bodyAnimator.runtimeAnimatorController);
            Assert.IsTrue(limbsRenderer.enabled, "Con la célula se ven las piezas.");

            var mouth = CreatureMouth(out var creature);
            mutations.Equip(mouth);
            yield return null;
            Assert.AreEqual(creature, bodyAnimator.runtimeAnimatorController, "La criatura manda sobre la Piel.");
            Assert.IsTrue(look.CreatureForm);
            Assert.IsFalse(limbsRenderer.enabled, "Las piezas no se dibujan en el cuerpo de la criatura.");

            mutations.Unequip(MutationSlot.Mouth);
            yield return null;
            Assert.AreEqual(skinLook, bodyAnimator.runtimeAnimatorController, "Sin la Boca vuelve la Piel.");
            mutations.Unequip(MutationSlot.Skin);
            Assert.AreEqual(cell, bodyAnimator.runtimeAnimatorController);
        }

        [UnityTest]
        public IEnumerator HeldForm_KeepsTheOldBody_UntilReleased()
        {
            var cell = bodyAnimator.runtimeAnimatorController;
            look.HoldForm();
            mutations.Equip(CreatureMouth(out var creature));
            yield return null;
            Assert.AreEqual(cell, bodyAnimator.runtimeAnimatorController, "Retenida: sigue siendo la célula.");

            look.ReleaseForm();
            Assert.AreEqual(creature, bodyAnimator.runtimeAnimatorController);
            Assert.IsFalse(look.FormHeld);
        }

        [UnityTest]
        public IEnumerator Attack_PlaysOnlyIfTheBodyHasIt()
        {
            yield return null;
            Assert.IsFalse(playerAnimator.PlayAttack(PlayerAnimator.Facing.Side), "La célula no tiene animación de ataque.");

            mutations.Equip(CreatureMouth(out _));
            yield return null;
            Assert.IsTrue(playerAnimator.PlayAttack(PlayerAnimator.Facing.Side));
            Assert.IsTrue(playerAnimator.IsAttacking);
        }

        [UnityTest]
        public IEnumerator Metamorphosis_RevealsTheNewForm_AndCanBeSkippedOnceSeen()
        {
            var host = Track(new GameObject("Pit"));
            var metamorphosis = host.AddComponent<Metamorphosis>();
            metamorphosis.SetTiming(0.02f, 0.02f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.02f);
            metamorphosis.readInput = false;
            var mouth = CreatureMouth(out var creature);

            look.HoldForm();
            mutations.Equip(mouth);
            Assert.IsFalse(Metamorphosis.Seen);
            metamorphosis.StartCoroutine(metamorphosis.Play(playerObject, mouth));
            yield return null;
            metamorphosis.skipPressed = true;   // la primera vez no se puede saltar
            for (float t = 0f; t < 3f && metamorphosis.IsPlaying; t += Time.deltaTime)
                yield return null;

            Assert.IsFalse(metamorphosis.IsPlaying);
            Assert.IsFalse(metamorphosis.Skipped, "La primera vez se ve entera.");
            Assert.IsTrue(Metamorphosis.Seen);
            Assert.AreEqual(creature, bodyAnimator.runtimeAnimatorController, "Al acabar ya tiene la forma nueva.");
            Assert.AreEqual(Color.white, playerAnimator.Body.color, "El cuerpo vuelve a verse con su color.");

            // La segunda vez, Interactuar la salta.
            metamorphosis.SetTiming(0.02f, 0.02f, 5f, 5f, 5f, 5f, 5f, 0.02f);
            metamorphosis.StartCoroutine(metamorphosis.Play(playerObject, mouth));
            yield return null;
            yield return null;
            metamorphosis.skipPressed = true;
            for (float t = 0f; t < 3f && metamorphosis.IsPlaying; t += Time.deltaTime)
                yield return null;

            Assert.IsFalse(metamorphosis.IsPlaying, "Saltada, acaba enseguida.");
            Assert.IsTrue(metamorphosis.Skipped);
            Assert.IsTrue(look.CreatureForm);
        }
    }
}
