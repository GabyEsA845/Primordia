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
    /// <summary>
    /// Mutaciones del piso 2 que no son Bocas. Cola propulsora: dos esquivas seguidas; en el nivel II la segunda deja
    /// burbujas que dañan y empujan. Espinas de Hallucigenia: al esquivar dejas púas que dañan una vez a quien las pisa y,
    /// con el Erizo, salen disparadas al desaparecer.
    /// </summary>
    public class Floor2MutationsTests
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
            foreach (var bubble in Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None))
                Object.DestroyImmediate(bubble.gameObject);
            foreach (var spine in Object.FindObjectsByType<GroundSpine>(FindObjectsSortMode.None))
                Object.DestroyImmediate(spine.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();
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

        /// <summary>Enemigo quieto: vida, cuerpo y capa Enemy.</summary>
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

        [UnityTest]
        public IEnumerator TailPropulsor_AllowsTwoDodgesInARow_ButNotThree()
        {
            var tail = Mutation<TailPropulsorMutation>(MutationSlot.Limbs);
            EquipAt(tail, 1);
            Assert.AreEqual(1, controller.ExtraDodges);

            Assert.IsTrue(controller.TryDodge());
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(controller.TryDodge(), "La segunda, encadenada antes de la recarga.");
            Assert.AreEqual(2, controller.DodgeInChain);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(controller.TryDodge(), "La tercera espera a la recarga.");

            player.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(0, controller.ExtraDodges);
        }

        [UnityTest]
        public IEnumerator TailPropulsor_LevelII_SecondDodgeLeavesBubbles()
        {
            var tail = Mutation<TailPropulsorMutation>(MutationSlot.Limbs);
            EquipAt(tail, 2);

            controller.TryDodge();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None).Length, "La primera no deja burbujas.");

            controller.TryDodge();
            yield return new WaitForSeconds(0.3f);
            var bubbles = Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None);
            Assert.AreEqual(3, bubbles.Length, "La segunda deja 3.");
            Assert.AreEqual(2, bubbles[0].Damage);
        }

        [UnityTest]
        public IEnumerator TailBubble_PopsOnTheFirstEnemy_DamagingIt()
        {
            var enemy = Enemy(new Vector2(10f, 0f));
            var bubble = TailBubble.Spawn(null, new Vector2(10.2f, 0f), 2, 3f, 6f);
            yield return null;
            yield return null;

            Assert.AreEqual(18, enemy.Current);
            Assert.IsTrue(bubble == null || bubble.Popped, "Revienta al tocarlo.");
        }

        [UnityTest]
        public IEnumerator HallucigeniaSpines_DodgeLeavesSpines_ThatHurtEachEnemyOnce()
        {
            var spines = Mutation<HallucigeniaSpinesMutation>(MutationSlot.Limbs);
            EquipAt(spines, 1);
            player.transform.position = new Vector2(20f, 0f);
            controller.SetInput(Vector2.right, Vector2.right);

            controller.TryDodge();
            yield return null;
            var placed = Object.FindObjectsByType<GroundSpine>(FindObjectsSortMode.None);
            Assert.AreEqual(2, placed.Length, "Nivel I: 2 púas.");
            foreach (var spine in placed)
                Assert.Less(Vector2.Distance(spine.transform.position, new Vector2(20f, 0f)), 0.6f, "Donde empezó la esquiva.");

            // Pequeño, para que no llegue a la púa de al lado.
            var enemy = Enemy(placed[0].transform.position, radius: 0.1f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(18, enemy.Current, "2 de daño, una sola vez aunque siga encima.");
        }

        [UnityTest]
        public IEnumerator GroundSpine_Hedgehog_ShootsAtTheNearestEnemy_WhenItExpires()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), 1f, 2, 12f, 0.8f);

            Enemy(new Vector2(33f, 0f));
            var spine = GroundSpine.Spawn(null, new Vector2(30f, 0f), 2, 0.1f, attack, 7f, player.GetComponent<Weapon>());
            Vector2? shot = null;
            spine.Shot += direction => shot = direction;
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(shot.HasValue, "Al desaparecer sale disparada.");
            Assert.Greater(shot.Value.x, 0.9f, "Hacia el enemigo.");
        }
    }
}
