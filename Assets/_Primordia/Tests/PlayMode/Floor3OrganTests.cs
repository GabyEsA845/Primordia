using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Órganos del piso 3. Saco de metano: suelta una nube (dos desde el nivel II) que explota con un golpe, daña a los enemigos y
    /// no al jugador, que además es inmune a las explosiones de gas. Feromonas: los enemigos del radio se atacan entre sí durante un
    /// rato (daño doble con el Frenesí) y los de fuera no se enteran.
    /// </summary>
    public class Floor3OrganTests
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
            playerObject.AddComponent<CircleCollider2D>().radius = 0.4f;
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
            foreach (var cloud in Object.FindObjectsByType<MethaneCloud>(FindObjectsSortMode.None))
                Object.DestroyImmediate(cloud.gameObject);
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

        /// <summary>Un enemigo con IA, quieto (sin datos no hace nada por sí solo).</summary>
        Health Enemy(Vector2 position, int health = 20)
        {
            var go = Track(new GameObject("Enemy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;
            var life = go.AddComponent<Health>();
            life.SetMax(health, healDifference: false);
            life.ResetHealth();
            go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>();
            return life;
        }

        // ---------- Saco de metano ----------

        [UnityTest]
        public IEnumerator MethaneSac_ReleasesACloud_ThatExplodesOnAHit_HurtingEnemiesButNotThePlayer()
        {
            var sac = Mutation<MethaneSacMutation>(MutationSlot.Organ);
            EquipAt(sac, 1);
            Assert.IsTrue(ExplosionImmune.Has(player), "Con el saco, las explosiones de gas no dañan al jugador.");
            player.transform.position = Vector2.zero;
            controller.SetInput(Vector2.zero, Vector2.right);
            var near = Enemy(new Vector2(2f, 0f), 30);
            var far = Enemy(new Vector2(8f, 0f), 30);

            sac.Activate(player);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1, MethaneCloud.All.Count, "Nivel I: una nube.");
            var cloud = MethaneCloud.All[0];

            // La nube deriva hacia el cursor.
            float x = cloud.transform.position.x;
            yield return new WaitForSeconds(1f);
            Assert.Greater(cloud.transform.position.x, x + 0.3f);

            // Se coloca junto al enemigo cercano y un golpe la hace explotar.
            cloud.transform.position = new Vector2(1.5f, 0f);
            int playerBefore = player.Health.Current;
            cloud.Health.TakeDamage(1);
            Assert.IsTrue(cloud.Exploded);
            Assert.AreEqual(near.Max - 6, near.Current, "6 de daño al enemigo cercano.");
            Assert.AreEqual(far.Max, far.Current, "Lejos no llega.");
            Assert.AreEqual(playerBefore, player.Health.Current, "Y el jugador no se daña.");
        }

        [UnityTest]
        public IEnumerator MethaneSac_LevelTwoReleasesTwoClouds_AndFireMakesThemExplode()
        {
            var sac = Mutation<MethaneSacMutation>(MutationSlot.Organ);
            EquipAt(sac, 2);
            player.transform.position = Vector2.zero;
            controller.SetInput(Vector2.zero, Vector2.right);

            sac.Activate(player);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, MethaneCloud.All.Count, "Nivel II: dos nubes.");

            var first = MethaneCloud.All[0];
            first.Ignite();
            Assert.IsTrue(first.Exploded, "El fuego la hace explotar.");
        }

        [UnityTest]
        public IEnumerator MethaneSac_Unequip_RemovesTheExplosionImmunity()
        {
            var sac = Mutation<MethaneSacMutation>(MutationSlot.Organ);
            EquipAt(sac, 1);
            yield return null;
            player.Unequip(MutationSlot.Organ);
            yield return null;
            Assert.IsFalse(ExplosionImmune.Has(player));
        }

        // ---------- Feromonas ----------

        [UnityTest]
        public IEnumerator Pheromones_ConfuseEnemiesInRadius_AndTheyHurtEachOther()
        {
            var pheromones = Mutation<PheromonesMutation>(MutationSlot.Organ);
            EquipAt(pheromones, 1);
            Assert.AreEqual(3f, pheromones.SecondsAt(1), 0.001f);
            Assert.AreEqual(4f, pheromones.SecondsAt(2), 0.001f);

            var a = Enemy(new Vector2(0f, 0f));
            var b = Enemy(new Vector2(0.7f, 0f));
            var outside = Enemy(new Vector2(6f, 0f));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(2, pheromones.Confuse(new Vector2(0.4f, 0f), 3f, frenzy: false), "Dos dentro del radio 2.");
            Assert.IsTrue(Confusion.IsConfused(a));
            Assert.IsTrue(Confusion.IsConfused(b));
            Assert.IsFalse(Confusion.IsConfused(outside));

            yield return new WaitForSeconds(1.2f);
            Assert.Less(a.Current, a.Max, "Se muerden entre sí.");
            Assert.Less(b.Current, b.Max);
            Assert.AreEqual(outside.Max, outside.Current);
        }

        [UnityTest]
        public IEnumerator Pheromones_Frenzy_DoublesTheDamage()
        {
            var pheromones = Mutation<PheromonesMutation>(MutationSlot.Organ);
            EquipAt(pheromones, 1);
            var a = Enemy(new Vector2(0f, 0f), 100);
            var b = Enemy(new Vector2(0.7f, 0f), 100);
            var c = Enemy(new Vector2(20f, 0f), 100);
            var d = Enemy(new Vector2(20.7f, 0f), 100);
            yield return new WaitForFixedUpdate();

            pheromones.Confuse(new Vector2(0.4f, 0f), 3f, frenzy: false);
            pheromones.Confuse(new Vector2(20.4f, 0f), 3f, frenzy: true);
            yield return new WaitForSeconds(0.5f);
            int normal = (a.Max - a.Current) + (b.Max - b.Current);
            int frenzied = (c.Max - c.Current) + (d.Max - d.Current);
            Assert.AreEqual(normal * 2, frenzied, "Con el Frenesí hacen el doble.");
        }

        [UnityTest]
        public IEnumerator Confusion_EndsAfterItsSeconds_AndTheEnemyGoesBackToNormal()
        {
            var pheromones = Mutation<PheromonesMutation>(MutationSlot.Organ);
            var a = Enemy(Vector2.zero);
            Enemy(new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();

            pheromones.Confuse(new Vector2(1f, 0f), 0.4f, frenzy: false);
            var ai = a.GetComponent<EnemyAI>();
            yield return null;
            Assert.IsTrue(ai.IsConfused);
            Assert.IsNotNull(ai.OverrideTarget);

            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(Confusion.IsConfused(a));
            Assert.IsFalse(ai.IsConfused);
            Assert.IsNull(ai.OverrideTarget);
        }
    }
}
