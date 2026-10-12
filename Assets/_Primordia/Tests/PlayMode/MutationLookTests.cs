using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class MutationLookTests
    {
        readonly List<Object> created = new();
        GameObject playerObject;
        PlayerController controller;
        PlayerMutations mutations;
        MutationLook look;
        Health health;

        [SetUp]
        public void SetUp()
        {
            playerObject = Track(new GameObject("Player"));
            playerObject.AddComponent<Weapon>();
            controller = playerObject.AddComponent<PlayerController>();
            controller.readInput = false;
            health = playerObject.GetComponent<Health>();
            mutations = playerObject.AddComponent<PlayerMutations>();

            var body = Child(playerObject.transform, "Body").gameObject.AddComponent<SpriteRenderer>();
            var animator = playerObject.AddComponent<PlayerAnimator>();
            animator.Configure(null, body);

            var aimPivot = Child(playerObject.transform, "AimPivot");
            look = playerObject.AddComponent<MutationLook>();
            look.Configure(Piece(playerObject.transform, "Limbs"), Piece(playerObject.transform, "Organ"), Piece(aimPivot, "MouthPiece"));
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

        /// <summary>Mutación sin efecto, con un controlador de un solo estado (Idle) como aspecto.</summary>
        MutationData Mutation(MutationSlot slot, float mouthDistance = 0f)
        {
            var mutation = Track(ScriptableObject.CreateInstance<StatMutation>());
            mutation.ConfigureBase(slot.ToString(), "", slot, Lineage.Predator, Color.white);
            mutation.SetLook(CreateController(), newMouthDistance: mouthDistance);
            return mutation;
        }

        // Un AnimatorOverrideController vacío no se puede asignar a un Animator: hace falta un controlador de verdad.
        // Las pruebas PlayMode se ejecutan en el editor, así que se crea en memoria con la API del editor.
        RuntimeAnimatorController CreateController()
        {
            var controller = Track(new UnityEditor.Animations.AnimatorController());
            controller.AddLayer("Base");
            controller.layers[0].stateMachine.AddState("Idle");
            return controller;
        }

        [UnityTest]
        public IEnumerator Piece_ShowsWhenEquipped_AndHidesWhenRemoved()
        {
            yield return null;
            Assert.IsFalse(look.LimbsVisible);

            mutations.Equip(Mutation(MutationSlot.Limbs));
            yield return null;
            Assert.IsTrue(look.LimbsVisible);
            Assert.IsFalse(look.OrganVisible);

            mutations.Unequip(MutationSlot.Limbs);
            yield return null;
            Assert.IsFalse(look.LimbsVisible);
        }

        [UnityTest]
        public IEnumerator Dodge_HidesThePieces()
        {
            mutations.Equip(Mutation(MutationSlot.Limbs));
            mutations.Equip(Mutation(MutationSlot.Mouth));
            controller.SetInput(Vector2.right, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            yield return null;

            Assert.IsFalse(look.LimbsVisible);
            Assert.IsFalse(look.MouthVisible);

            yield return new WaitForSeconds(0.35f);

            Assert.IsTrue(look.LimbsVisible);
            Assert.IsTrue(look.MouthVisible);
        }

        [UnityTest]
        public IEnumerator Mouth_GoesBehindTheBody_WhenFacingUp()
        {
            mutations.Equip(Mutation(MutationSlot.Mouth));

            controller.SetInput(Vector2.zero, Vector2.up);
            yield return null;
            Assert.IsTrue(look.MouthBehindBody);

            controller.SetInput(Vector2.zero, Vector2.down);
            yield return null;
            Assert.IsFalse(look.MouthBehindBody);
        }

        [Test]
        public void Skin_MovesTheMouthToItsEdge()
        {
            mutations.Equip(Mutation(MutationSlot.Skin, mouthDistance: 0.53f));
            Assert.AreEqual(0.53f, look.MouthDistance, 0.0001f);

            mutations.Unequip(MutationSlot.Skin);
            // Sin Piel, la Boca vuelve al borde de la célula (6 px).
            Assert.AreEqual(0.375f, look.MouthDistance, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Death_HidesThePieces()
        {
            mutations.Equip(Mutation(MutationSlot.Organ));
            yield return null;
            Assert.IsTrue(look.OrganVisible);

            health.TakeDamage(health.Max);
            yield return null;

            Assert.IsFalse(look.OrganVisible);
        }
    }
}
