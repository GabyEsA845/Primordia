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
    /// <summary>Mutaciones especiales del ámbar del piso 3: Cercos de cucaracha, Cápsula de ámbar y Gota de ámbar (subpasos 2b-2d).</summary>
    public class AmberSpecialsTests
    {
        readonly List<Object> created = new();
        GameObject player;
        Health health;
        PlayerController controller;
        PlayerMutations mutations;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            health = player.AddComponent<Health>();
            player.AddComponent<Weapon>().Attack = Track(ScriptableObject.CreateInstance<AttackData>());
            controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
            mutations = player.AddComponent<PlayerMutations>();
            player.AddComponent<PlayerAbility>().readInput = false;
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
            foreach (var shell in Object.FindObjectsByType<AmberShell>(FindObjectsInactive.Include))
                Object.DestroyImmediate(shell.gameObject);
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        T Mutation<T>(MutationSlot slot) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase("Especial", "", slot, Color.white);
            mutation.SetSpecial(true);
            return mutation;
        }

        void Equip(MutationData mutation, int level)
        {
            mutations.Equip(mutation);
            while (mutations.LevelOf(mutation.Slot) < level && mutations.LevelUp(mutation.Slot))
            {
            }
        }

        Projectile EnemyProjectile()
        {
            var shot = Track(new GameObject("Shot"));
            shot.AddComponent<Rigidbody2D>();
            shot.AddComponent<CircleCollider2D>().radius = 0.1f;
            return shot.AddComponent<Projectile>();
        }

        // ---------- Cercos ----------

        [Test]
        public void Cerci_Levels_RechargeFasterAndGainTwoChargesAndSpeedAtThree()
        {
            var cerci = Mutation<CerciMutation>(MutationSlot.Limbs);
            Assert.AreEqual(8f, cerci.ValuesAt(1).cooldown);
            Assert.AreEqual(6f, cerci.ValuesAt(2).cooldown);
            Assert.AreEqual(2, cerci.ValuesAt(3).charges);
            Assert.AreEqual(0.3f, cerci.ValuesAt(3).speedBonus, 0.001f);
            Assert.AreEqual(0f, cerci.ValuesAt(2).speedBonus);
        }

        [Test]
        public void Cerci_ProjectileAboutToHit_UsesTheCharge_AndTheHitDoesNoDamage()
        {
            var cerci = Mutation<CerciMutation>(MutationSlot.Limbs);
            Equip(cerci, 1);
            var dodge = player.GetComponent<CerciDodge>();
            Assert.AreEqual(1, dodge.ReadyCharges);
            Assert.IsFalse(dodge.TryReflect(EnemyProjectile()), "El proyectil no se devuelve: sigue de largo.");
            Assert.AreEqual(0, dodge.ReadyCharges, "Gasta la carga.");
            Assert.IsFalse(health.CanTakeDamage, "Durante el quiebro es invulnerable.");
            Assert.IsFalse(health.TakeDamage(2));
            Assert.AreEqual(health.Max, health.Current);
        }

        [UnityTest]
        public IEnumerator Cerci_LevelThree_KeepsTwoDodges_AndSpeedsYouUpAfterEach()
        {
            var cerci = Mutation<CerciMutation>(MutationSlot.Limbs);
            Equip(cerci, 3);
            var dodge = player.GetComponent<CerciDodge>();
            Assert.AreEqual(2, dodge.Charges);

            dodge.TryReflect(EnemyProjectile());
            Assert.AreEqual(1, dodge.ReadyCharges, "Cada carga se recarga por su cuenta.");
            Assert.AreEqual(1.3f, controller.BoostSpeedFactor, 0.001f, "+30 % de velocidad.");

            yield return new WaitForSeconds(0.6f);
            dodge.TryReflect(EnemyProjectile());
            Assert.AreEqual(0, dodge.ReadyCharges);
            Assert.AreEqual(2, dodge.DodgesMade);
        }

        // ---------- Cápsula ----------

        [Test]
        public void Capsule_Levels_HealMoreAndGetTwoUsesPerRoomAtThree()
        {
            var capsule = Mutation<AmberCapsuleMutation>(MutationSlot.Skin);
            Assert.AreEqual(1, capsule.ValuesAt(1).heal);
            Assert.AreEqual(2, capsule.ValuesAt(2).heal);
            Assert.AreEqual(1f, capsule.ValuesAt(2).shardStun);
            Assert.AreEqual(3, capsule.ValuesAt(3).heal);
            Assert.AreEqual(2, capsule.ValuesAt(3).perRoom);
            Assert.AreEqual(1, capsule.ValuesAt(1).perRoom);
        }

        [UnityTest]
        public IEnumerator Capsule_EncasesYou_Heals_LocksYou_AndLetsYouGoAfterTwoSeconds()
        {
            var capsule = Mutation<AmberCapsuleMutation>(MutationSlot.Skin);
            Equip(capsule, 1);
            var shield = player.GetComponent<AmberCapsuleShield>();
            health.Drain(health.Current - 1);
            Assert.AreEqual(1, health.Current);

            shield.Encase();
            Assert.IsTrue(shield.IsEncased);
            Assert.AreEqual(2, health.Current, "Cura medio corazón al encerrarte.");
            Assert.IsTrue(controller.MovementLocked, "Inmóvil.");
            Assert.IsFalse(health.CanTakeDamage, "Invulnerable.");
            Assert.AreEqual(0, shield.ActivationsLeft, "Una vez por sala.");

            yield return new WaitForSeconds(2.3f);
            Assert.IsFalse(controller.MovementLocked, "Se rompe el bloque y vuelves a moverte.");
            Assert.IsFalse(health.CanTakeDamage, "Y sigues invencible 2 s más.");
        }

        [Test]
        public void Capsule_AHitThatLeavesYouAtHalfAHeart_EncasesYouOnTheNextFrame()
        {
            var capsule = Mutation<AmberCapsuleMutation>(MutationSlot.Skin);
            Equip(capsule, 1);
            var shield = player.GetComponent<AmberCapsuleShield>();
            health.TakeDamage(health.Max - 1);
            Assert.AreEqual(1, health.Current);
            Assert.AreEqual(0, shield.Encasements, "Aún no: espera al siguiente cuadro.");
        }

        // ---------- Gota ----------

        [Test]
        public void Gland_Levels_ChangeCooldownDurationAndKillAtThree()
        {
            var gland = Mutation<AmberGlandMutation>(MutationSlot.Organ);
            Assert.AreEqual(20f, gland.CooldownAt(1));
            Assert.AreEqual(18f, gland.CooldownAt(2));
            Assert.AreEqual(15f, gland.CooldownAt(3));
            Assert.AreEqual(5f, gland.ValuesAt(1).stunSeconds);
            Assert.AreEqual(1.5f, gland.ValuesAt(1).bossSeconds);
            Assert.AreEqual(8f, gland.ValuesAt(2).stunSeconds);
            Assert.AreEqual(2f, gland.ValuesAt(2).bossSeconds);
            Assert.IsFalse(gland.ValuesAt(2).kills);
            Assert.IsTrue(gland.ValuesAt(3).kills);
            Assert.IsTrue(gland.HoldToCharge);
        }

        [UnityTest]
        public IEnumerator Gland_HoldToCharge_ReleasingTooSoonDoesNothing_ReleasingCharged_StartsTheCooldown()
        {
            var gland = Mutation<AmberGlandMutation>(MutationSlot.Organ);
            Equip(gland, 1);
            var ability = player.GetComponent<PlayerAbility>();
            int uses = 0;
            ability.Used += () => uses++;

            // Suelta antes de tiempo: no sale la gota ni se gasta.
            ability.specialPressed = true;
            ability.specialHeld = true;
            yield return null;
            ability.specialHeld = false;
            yield return null;
            Assert.AreEqual(0, uses);
            Assert.IsTrue(ability.IsReady);

            // Mantiene el tiempo necesario y suelta.
            ability.specialPressed = true;
            ability.specialHeld = true;
            yield return null;
            ability.specialHeld = true;
            yield return new WaitForSeconds(gland.ChargeSeconds + 0.1f);
            ability.specialHeld = false;
            yield return null;
            Assert.AreEqual(1, uses);
            Assert.IsFalse(ability.IsReady, "Empieza la recarga.");
            Assert.AreEqual(20f, gland.CooldownAt(1));
        }
    }
}
