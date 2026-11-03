using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Pieles del piso 3. Piel anfibia: el moco se carga en un charco de agua y, cargado, da inmunidad al fuego y al veneno hasta que
    /// un golpe lo rompe (con daño; sin daño desde el nivel III); el nivel II acelera. Escamas carbonizadas: inmune al fuego, los
    /// ataques queman tras pisar fuego, desde el nivel II inmune al veneno de las plantas y con la Brasa, y en el III siempre queman.
    /// </summary>
    public class Floor3SkinTests
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

        /// <summary>Un charco de agua de 4×4 u (el dibujo mide lo que el sprite) bajo el jugador.</summary>
        GroundPuddle WaterUnderPlayer()
        {
            var texture = Track(new Texture2D(64, 64));
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 16f));
            var parent = Track(new GameObject("Puddles"));
            var puddle = GroundPuddle.Create(parent.transform, player.transform.position, new[] { sprite }, 0.1f, sludge: false);
            return puddle;
        }

        [UnityTest]
        public IEnumerator AmphibianSkin_ChargedGivesImmunity_AndAHitBreaksItWithDamage()
        {
            var skin = Mutation<AmphibianSkinMutation>(MutationSlot.Skin);
            EquipAt(skin, 2);
            Assert.AreEqual(2f, skin.ChargeSecondsAt(2), 0.001f);
            var mucus = player.GetComponent<AmphibianMucus>();
            player.transform.position = Vector2.zero;
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(FireImmune.Has(player), "Sin cargar no protege.");

            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(mucus.Charged, "En seco no se carga.");

            WaterUnderPlayer();
            yield return new WaitForSeconds(2.3f);
            Assert.IsTrue(mucus.Charged, "En el agua se carga en 2 s (nivel II).");
            Assert.IsTrue(FireImmune.Has(player));
            Assert.IsTrue(PoisonImmune.Has(player));

            int before = player.Health.Current;
            player.Health.TakeDamage(1);
            Assert.AreEqual(before - 1, player.Health.Current, "Nivel I/II: el golpe hace daño.");
            Assert.IsFalse(mucus.Charged, "Y rompe el moco.");
            Assert.IsFalse(FireImmune.Has(player));
            Assert.IsFalse(PoisonImmune.Has(player));
        }

        [UnityTest]
        public IEnumerator AmphibianSkin_LevelThree_BlocksTheFirstHitOnly()
        {
            EquipAt(Mutation<AmphibianSkinMutation>(MutationSlot.Skin), 3);
            var mucus = player.GetComponent<AmphibianMucus>();
            player.transform.position = Vector2.zero;
            WaterUnderPlayer();
            yield return new WaitForSeconds(2.3f);
            Assert.IsTrue(mucus.Charged);

            int before = player.Health.Current;
            player.Health.TakeDamage(3);
            Assert.AreEqual(before, player.Health.Current, "Nivel III: el golpe no hace daño.");
            Assert.IsFalse(mucus.Charged, "Pero rompe el moco.");
            Assert.AreEqual(1, mucus.Breaks);

            yield return new WaitForSeconds(0.5f);
            player.Health.TakeDamage(1);
            Assert.AreEqual(before - 1, player.Health.Current, "Sin moco, el siguiente golpe hace daño.");
        }

        [UnityTest]
        public IEnumerator AmphibianSkin_LevelTwoAddsSpeed_AndRemoveRestoresIt()
        {
            float before = controller.MoveSpeed;
            var skin = Mutation<AmphibianSkinMutation>(MutationSlot.Skin);
            EquipAt(skin, 1);
            Assert.AreEqual(before, controller.MoveSpeed, 0.001f);
            player.LevelUp(MutationSlot.Skin);
            Assert.AreEqual(before * 1.2f, controller.MoveSpeed, 0.001f);
            player.Unequip(MutationSlot.Skin);
            Assert.AreEqual(before, controller.MoveSpeed, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharredScales_ImmuneToFire_AndToPlantPoisonFromLevelTwo()
        {
            var scales = Mutation<CharredScalesMutation>(MutationSlot.Skin);
            EquipAt(scales, 1);
            yield return null;
            Assert.IsTrue(FireImmune.Has(player));
            Assert.IsFalse(PoisonImmune.Has(player), "Nivel I: el veneno sí le afecta.");

            player.LevelUp(MutationSlot.Skin);
            yield return null;
            Assert.IsTrue(PoisonImmune.Has(player), "Nivel II: inmune al veneno de las plantas.");

            player.Unequip(MutationSlot.Skin);
            yield return null;
            Assert.IsFalse(FireImmune.Has(player));
            Assert.IsFalse(PoisonImmune.Has(player));
        }

        [UnityTest]
        public IEnumerator CharredScales_AttacksBurnOnlyAfterSteppingOnFire()
        {
            EquipAt(Mutation<CharredScalesMutation>(MutationSlot.Skin), 1);
            var scales = player.GetComponent<CharredScales>();
            var target = Enemy(new Vector2(3f, 0f));
            yield return null;

            player.Weapon.ReportHit(target);
            Assert.IsNull(target.GetComponent<Burn>(), "Sin pisar fuego no quema.");

            scales.Prime();
            Assert.IsTrue(scales.IsPrimed);
            player.Weapon.ReportHit(target);
            Assert.IsNotNull(target.GetComponent<Burn>(), "Cebado, el ataque quema.");
        }

        [UnityTest]
        public IEnumerator CharredScales_LevelTwo_BlazeHurtsNearbyEnemiesOnceEveryCooldown()
        {
            EquipAt(Mutation<CharredScalesMutation>(MutationSlot.Skin), 1);
            var scales = player.GetComponent<CharredScales>();
            player.transform.position = Vector2.zero;
            var near = Enemy(new Vector2(1f, 0f), health: 30);
            yield return new WaitForFixedUpdate();
            player.Health.TakeDamage(1);
            Assert.AreEqual(0, scales.Blazes, "Nivel I: sin Brasa.");

            player.LevelUp(MutationSlot.Skin);
            yield return new WaitForSeconds(0.5f);
            scales = player.GetComponent<CharredScales>();
            player.Health.TakeDamage(1);
            Assert.AreEqual(1, scales.Blazes);
            Assert.AreEqual(near.Max - 4, near.Current, "La Brasa hace 4 de daño cerca.");

            yield return new WaitForSeconds(0.5f);
            player.Health.TakeDamage(1);
            Assert.AreEqual(1, scales.Blazes, "Con la recarga de 5 s no vuelve a estallar enseguida.");
        }

        sealed class FakeFlammable : IFlammable
        {
            public Vector2 Position { get; set; }
            public bool CanIgnite => !Ignited;
            public bool Ignited { get; private set; }
            public void Ignite() => Ignited = true;
        }

        [UnityTest]
        public IEnumerator CharredScales_LevelThree_AttacksAlwaysBurnAndLightTheScenery()
        {
            EquipAt(Mutation<CharredScalesMutation>(MutationSlot.Skin), 3);
            var scales = player.GetComponent<CharredScales>();
            Assert.IsTrue(scales.IsPrimed, "Siempre ceba, sin pisar fuego.");
            var target = Enemy(new Vector2(3f, 0f));
            var fern = new FakeFlammable { Position = new Vector2(3.3f, 0f) };
            Flammables.Register(fern);
            yield return null;

            player.Weapon.ReportHit(target);
            Assert.IsNotNull(target.GetComponent<Burn>(), "Quema al enemigo.");
            Assert.IsTrue(fern.Ignited, "Y prende lo inflamable donde impacta.");
            Flammables.Unregister(fern);
        }
    }
}
