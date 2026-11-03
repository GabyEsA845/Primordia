using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Extremidades del piso 3. Patas de salto: la esquiva es un salto de 4/5/6 u que pasa sobre enemigos y obstáculos bajos (no
    /// sobre paredes), vacía el hundimiento del lodo, al caer daña (II) y, en el nivel III, mantener el botón carga un salto largo
    /// con impacto que daña y aturde. Patas hilanderas: la esquiva tiende un hilo que frena y daña una vez, el límite de hilos sube
    /// con el nivel y, en el III, tres hilos se unen en una red que daña e inmoviliza lo de dentro.
    /// </summary>
    public class Floor3LimbsTests
    {
        readonly List<Object> created = new();
        PlayerMutations player;
        PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            var playerObject = Track(new GameObject("Player"));
            playerObject.layer = LayerMask.NameToLayer(CombatLayers.Player);
            playerObject.AddComponent<Weapon>();
            controller = playerObject.AddComponent<PlayerController>();
            controller.readInput = false;
            player = playerObject.AddComponent<PlayerMutations>();
            playerObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
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
            foreach (var thread in Object.FindObjectsByType<SilkThread>(FindObjectsSortMode.None))
                Object.DestroyImmediate(thread.gameObject);
            foreach (var web in Object.FindObjectsByType<SilkWeb>(FindObjectsSortMode.None))
                Object.DestroyImmediate(web.gameObject);
            foreach (var net in Object.FindObjectsByType<SilkNet>(FindObjectsSortMode.None))
                Object.DestroyImmediate(net.gameObject);
            foreach (var marker in Object.FindObjectsByType<JumpMarker>(FindObjectsSortMode.None))
                Object.DestroyImmediate(marker.gameObject);
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        T Mutation<T>(MutationSlot slot) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase(typeof(T).Name, "", slot, Color.white);
            return mutation;
        }

        void EquipAt(MutationData mutation, int level)
        {
            player.Equip(mutation);
            for (int i = 1; i < level; i++)
                player.LevelUp(mutation.Slot);
        }

        Health Enemy(Vector2 position, int health = 20, float radius = 0.4f)
        {
            var go = Track(new GameObject("Enemy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = radius;
            var life = go.AddComponent<Health>();
            life.SetMax(health, healDifference: false);
            life.ResetHealth();
            return life;
        }

        GameObject Obstacle(Vector2 position, bool jumpable)
        {
            var go = Track(new GameObject("Obstacle"));
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            if (jumpable)
                go.AddComponent<JumpableObstacle>();
            return go;
        }

        void Dodge(Vector2 direction)
        {
            controller.SetInput(direction, direction);
            Assert.IsTrue(controller.TryDodge());
        }

        [UnityTest]
        public IEnumerator JumpingLegs_JumpsFourUnits_OverLowObstacle_ButNotOverWall()
        {
            EquipAt(Mutation<JumpingLegsMutation>(MutationSlot.Limbs), 1);
            Assert.AreEqual(4f, controller.DodgeDistance, 0.001f);
            player.transform.position = Vector2.zero;
            Obstacle(new Vector2(1.5f, 0f), jumpable: true);
            Obstacle(new Vector2(0f, 1.5f), jumpable: false);
            yield return new WaitForFixedUpdate();

            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(player.transform.position.x, 3.5f, "El salto cruza el obstáculo bajo y llega a 4 u.");

            player.transform.position = Vector2.zero;
            yield return new WaitForSeconds(0.5f);
            Dodge(Vector2.up);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(player.transform.position.y, 1.2f, "La pared (obstáculo sin marca) para el salto.");
        }

        [UnityTest]
        public IEnumerator JumpingLegs_DistanceGrowsWithLevel_AndGoesBackWhenRemoved()
        {
            var legs = Mutation<JumpingLegsMutation>(MutationSlot.Limbs);
            EquipAt(legs, 2);
            Assert.AreEqual(5f, controller.DodgeDistance, 0.001f);
            player.LevelUp(MutationSlot.Limbs);
            Assert.AreEqual(6f, controller.DodgeDistance, 0.001f);
            player.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(3f, controller.DodgeDistance, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator JumpingLegs_PassesOverEnemies_AndRestoresCollisionsAfterLanding()
        {
            EquipAt(Mutation<JumpingLegsMutation>(MutationSlot.Limbs), 1);
            var rb = player.GetComponent<Rigidbody2D>();
            LayerMask before = rb.excludeLayers;
            player.transform.position = Vector2.zero;
            Enemy(new Vector2(1.5f, 0f));
            yield return new WaitForFixedUpdate();

            Dodge(Vector2.right);
            yield return new WaitForFixedUpdate();
            Assert.AreNotEqual(before.value, rb.excludeLayers.value, "En el aire no choca con los enemigos.");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(before.value, rb.excludeLayers.value, "Al caer vuelve a chocar.");
        }

        [UnityTest]
        public IEnumerator JumpingLegs_LevelTwoDamagesOnLanding()
        {
            var legs = Mutation<JumpingLegsMutation>(MutationSlot.Limbs);
            EquipAt(legs, 1);
            player.transform.position = Vector2.zero;
            var near = Enemy(new Vector2(4.8f, 0f));
            yield return new WaitForFixedUpdate();
            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(near.Max, near.Current, "Nivel I: el aterrizaje no hace daño.");

            player.LevelUp(MutationSlot.Limbs);
            player.transform.position = Vector2.zero;
            yield return new WaitForSeconds(0.5f);
            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(near.Max - 3, near.Current, "Nivel II: 3 de daño al caer.");
        }

        [UnityTest]
        public IEnumerator JumpingLegs_DodgeResetsMudSink_AndRemoveRestoresIt()
        {
            var sink = player.gameObject.AddComponent<PlayerMudSink>();
            Assert.IsFalse(sink.DodgeResetsSink);
            EquipAt(Mutation<JumpingLegsMutation>(MutationSlot.Limbs), 1);
            Assert.IsTrue(sink.DodgeResetsSink);
            player.LevelUp(MutationSlot.Limbs);
            yield return null;
            Assert.IsTrue(sink.DodgeResetsSink, "Subir de nivel no lo apaga.");

            player.Unequip(MutationSlot.Limbs);
            yield return null;
            Assert.IsFalse(sink.DodgeResetsSink);
        }

        [UnityTest]
        public IEnumerator JumpingLegs_LevelThree_QuickTapIsANormalJump()
        {
            EquipAt(Mutation<JumpingLegsMutation>(MutationSlot.Limbs), 3);
            Assert.IsTrue(controller.ManualDodge);
            player.transform.position = Vector2.zero;
            controller.SetInput(Vector2.zero, Vector2.right);
            yield return new WaitForFixedUpdate();

            controller.SetDodgeHeld(true);
            Assert.IsFalse(controller.IsDodging, "Pulsar no esquiva todavía.");
            yield return new WaitForSeconds(0.3f);
            controller.SetDodgeHeld(false);
            Assert.IsTrue(controller.IsDodging, "Soltar pronto es un salto normal.");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(6f, player.transform.position.x, 0.5f);
        }

        [UnityTest]
        public IEnumerator JumpingLegs_LevelThree_HoldChargesMarkerAndJumpsFarWithStunningImpact()
        {
            EquipAt(Mutation<JumpingLegsMutation>(MutationSlot.Limbs), 3);
            player.transform.position = Vector2.zero;
            controller.SetInput(Vector2.zero, Vector2.right);
            var target = Enemy(new Vector2(9f, 0f), health: 30);
            yield return new WaitForFixedUpdate();

            controller.SetDodgeHeld(true);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, Object.FindObjectsByType<JumpMarker>(FindObjectsSortMode.None).Length, "Antes de 1 s no hay marcador.");
            yield return new WaitForSeconds(2.2f);
            var markers = Object.FindObjectsByType<JumpMarker>(FindObjectsSortMode.None);
            Assert.AreEqual(1, markers.Length, "Con la carga empezada se ve el marcador.");
            Assert.AreEqual(8f, markers[0].transform.position.x, 0.2f, "A carga completa, 8 u hacia el apuntado.");

            controller.SetDodgeHeld(false);
            Assert.IsTrue(controller.IsDodging);
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(8f, player.transform.position.x, 0.5f);
            Assert.AreEqual(0, Object.FindObjectsByType<JumpMarker>(FindObjectsSortMode.None).Length, "Soltar quita el marcador.");
            Assert.AreEqual(target.Max - 6, target.Current, "El impacto hace 6 de daño.");
        }

        [UnityTest]
        public IEnumerator SpinnerLegs_DodgeLaysThread_ThatHarmsOnceAndSlows()
        {
            EquipAt(Mutation<SpinnerLegsMutation>(MutationSlot.Limbs), 1);
            Assert.AreEqual(4f, controller.DodgeDistance, 0.001f);
            player.transform.position = Vector2.zero;
            var enemy = Enemy(new Vector2(1.5f, 0.1f));
            yield return new WaitForFixedUpdate();

            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, Object.FindObjectsByType<SilkThread>(FindObjectsSortMode.None).Length);

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(enemy.Max - 3, enemy.Current, "El hilo hace 3 de daño, una sola vez.");
        }

        [UnityTest]
        public IEnumerator SpinnerLegs_LimitOfThreadsGrowsWithLevel()
        {
            var legs = Mutation<SpinnerLegsMutation>(MutationSlot.Limbs);
            Assert.AreEqual(1, legs.MaxThreadsAt(1));
            Assert.AreEqual(2, legs.MaxThreadsAt(2));
            Assert.AreEqual(3, legs.MaxThreadsAt(3));
            EquipAt(legs, 1);
            player.transform.position = Vector2.zero;
            yield return new WaitForFixedUpdate();

            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.7f);
            Dodge(Vector2.up);
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(1, Object.FindObjectsByType<SilkThread>(FindObjectsSortMode.None).Length, "Nivel I: el hilo nuevo rompe el viejo.");
        }

        [Test]
        public void SilkNet_ConvexHullAndContains()
        {
            var hull = SilkNet.ConvexHull(new[]
            {
                new Vector2(0, 0), new Vector2(5, 0), new Vector2(5, 0), new Vector2(5, 5), new Vector2(5, 5), new Vector2(0, 5), new Vector2(2, 2)
            });
            Assert.AreEqual(4, hull.Count, "El punto de dentro no es del contorno.");
            var net = SilkNet.Spawn(hull, 1f, 0, 0f, null, null, null);
            Assert.IsNotNull(net);
            Assert.IsTrue(net.Contains(new Vector2(2.5f, 2.5f)));
            Assert.IsFalse(net.Contains(new Vector2(6f, 2.5f)));
            Object.DestroyImmediate(net.gameObject);
        }

        [UnityTest]
        public IEnumerator SpinnerLegs_LevelThree_ThreeThreadsFormANetThatHoldsWhatIsInside()
        {
            EquipAt(Mutation<SpinnerLegsMutation>(MutationSlot.Limbs), 3);
            var inside = Enemy(new Vector2(2.5f, 2.5f), health: 30);
            player.transform.position = new Vector2(0f, 0f);
            yield return new WaitForFixedUpdate();

            Dodge(Vector2.right);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1, Object.FindObjectsByType<SilkThread>(FindObjectsSortMode.None).Length);

            player.transform.position = new Vector2(5f, 0f);
            Dodge(Vector2.up);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(2, Object.FindObjectsByType<SilkThread>(FindObjectsSortMode.None).Length, "En el nivel III los hilos no desaparecen.");

            player.transform.position = new Vector2(5f, 5f);
            Dodge(Vector2.left);
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(1, Object.FindObjectsByType<SilkNet>(FindObjectsSortMode.None).Length, "Al tender el tercer hilo se forma la red.");
            Assert.AreEqual(inside.Max - 4, inside.Current, "El atrapado recibe 4 de daño.");
        }
    }
}
