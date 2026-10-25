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
    /// Yema clonal (especiales de las almejas, paso 4): clic derecho, brota una copia que ataca sola con la Boca y una parte
    /// del daño (sin avisar al arma del jugador), sin colisión, que los enemigos persiguen como al jugador; la recarga
    /// empieza cuando desaparece; en el nivel III brotan dos.
    /// </summary>
    public class ClonalBudTests
    {
        readonly List<Object> created = new();
        GameObject projectileTemplate;
        PlayerMutations mutations;
        PlayerAbility ability;
        ClonalBudMutation bud;

        [SetUp]
        public void SetUp()
        {
            projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = projectileTemplate.AddComponent<Projectile>();

            var player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, 4f, 4, 20f, 0.5f);
            player.AddComponent<Weapon>().Attack = attack;
            player.AddComponent<PlayerController>().readInput = false;
            mutations = player.AddComponent<PlayerMutations>();
            ability = player.AddComponent<PlayerAbility>();
            ability.readInput = false;

            bud = Track(ScriptableObject.CreateInstance<ClonalBudMutation>());
            bud.ConfigureBase("Yema clonal", "", MutationSlot.Organ, Color.white);
            bud.SetSpecial(true);
            bud.ConfigureAbility(15f, null);
            // Más cortas que en el juego para no alargar las pruebas.
            bud.ConfigureLevels(new ClonalBudMutation.BudLevel { duration = 1f, damageFraction = 0.5f },
                new ClonalBudMutation.BudLevel { duration = 1f, damageFraction = 0.65f },
                new ClonalBudMutation.BudLevel { duration = 1f, damageFraction = 0.65f, copies = 2 });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var copy in Object.FindObjectsByType<ClonalBudCopy>(FindObjectsSortMode.None))
                Object.DestroyImmediate(copy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();
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

        Health Enemy(Vector2 position)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            enemy.transform.position = position;
            enemy.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            Physics2D.SyncTransforms();
            return health;
        }

        [UnityTest]
        public IEnumerator Use_SproutsACopy_AndTheCooldownStartsWhenItGoes()
        {
            mutations.Equip(bud);
            yield return null;

            Assert.IsTrue(ability.TryUse());
            Assert.AreEqual(1, ClonalBudCopy.CountOf(mutations));
            var copy = ClonalBudCopy.Active[0];
            Assert.IsNull(copy.GetComponent<Collider2D>(), "Sin colisión: no recibe daño ni cae a las fosas.");

            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(bud.IsRunning(mutations));
            Assert.AreEqual(0f, ability.CooldownProgress, 0.01f, "La recarga espera a que la copia desaparezca.");

            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(0, ClonalBudCopy.CountOf(mutations), "Desaparece al acabar su tiempo.");
            yield return null;
            Assert.IsTrue(ability.IsCharging);
            Assert.Less(ability.CooldownProgress, 0.05f, "La recarga de 15 s acaba de empezar.");
        }

        [UnityTest]
        public IEnumerator Copy_AttacksTheNearestEnemy_WithHalfTheDamage_AndOnlyTheMouth()
        {
            int playerHits = 0;
            mutations.Weapon.HitTarget += _ => playerHits++;
            mutations.Equip(bud);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            ability.TryUse();
            yield return new WaitForSeconds(0.5f);

            int lost = 100 - enemy.Current;
            Assert.Greater(lost, 0, "La copia le dispara.");
            Assert.AreEqual(0, lost % 2, "Cada disparo hace la mitad: 2 de 4.");
            Assert.AreEqual(enemy, ClonalBudCopy.Active[0].Target);
            Assert.AreEqual(0, playerHits, "Sus golpes no cuentan para las otras mutaciones del jugador.");
        }

        [UnityTest]
        public IEnumerator Copy_IsADecoy_WhileItLasts()
        {
            mutations.Equip(bud);
            mutations.transform.position = new Vector2(-10f, 0f);
            yield return null;

            ability.TryUse();
            var copy = ClonalBudCopy.Active[0];
            copy.transform.position = new Vector2(5f, 0f);
            Assert.AreEqual(copy.transform, Decoys.Choose(new Vector2(6f, 0f), mutations.transform), "Los enemigos van a por el más cercano.");
            Assert.AreEqual(mutations.transform, Decoys.Choose(new Vector2(-11f, 0f), mutations.transform));

            copy.Dissolve();
            Assert.AreEqual(mutations.transform, Decoys.Choose(new Vector2(6f, 0f), mutations.transform), "Al deshacerse deja de ser señuelo.");
        }

        [UnityTest]
        public IEnumerator Gemmation_SproutsTwoCopies()
        {
            mutations.Equip(bud);
            mutations.LevelUp(MutationSlot.Organ);
            mutations.LevelUp(MutationSlot.Organ);
            yield return null;

            ability.TryUse();
            Assert.AreEqual(2, ClonalBudCopy.CountOf(mutations));
            Assert.AreNotEqual(ClonalBudCopy.Active[0].transform.position, ClonalBudCopy.Active[1].transform.position,
                "Una a cada lado.");
        }

        [UnityTest]
        public IEnumerator LevelTwo_HitsHarder()
        {
            mutations.Equip(bud);
            mutations.LevelUp(MutationSlot.Organ);
            yield return null;

            ability.TryUse();
            // 4 × 0,65 = 2,6, que el arma redondea hacia arriba.
            Assert.AreEqual(3, ClonalBudCopy.Active[0].Weapon.DamageOf(mutations.Weapon.Attack));
        }
    }
}
