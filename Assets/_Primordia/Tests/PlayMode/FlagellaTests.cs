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
    /// Flagelos de Leanchoilia (especiales de las almejas, paso 2): al golpear, latigazo junto al enemigo a los 0,4 s con el
    /// daño de 1 s de la Boca y sus efectos; uno cada 3 s (II: 2,5 s y +25 %); si el enemigo muere antes, va al más cercano;
    /// en el nivel III salta a los 2 más cercanos.
    /// </summary>
    public class FlagellaTests
    {
        readonly List<Object> created = new();
        PlayerMutations mutations;
        Weapon weapon;
        AttackData attack;
        LeanchoiliaFlagellaMutation flagella;

        [SetUp]
        public void SetUp()
        {
            var player = Track(new GameObject("Player"));
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            weapon = player.AddComponent<Weapon>();
            // 3 de daño, 2 golpes por segundo: el latigazo hace 6.
            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(null, 2f, 3, 10f, 1f);
            weapon.Attack = attack;
            player.AddComponent<PlayerController>().readInput = false;
            mutations = player.AddComponent<PlayerMutations>();

            flagella = Track(ScriptableObject.CreateInstance<LeanchoiliaFlagellaMutation>());
            flagella.ConfigureBase("Flagelos de Leanchoilia", "", MutationSlot.Limbs, Color.white);
            flagella.SetSpecial(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var strike in Object.FindObjectsByType<FlagellumStrike>(FindObjectsSortMode.None))
                Object.DestroyImmediate(strike.gameObject);
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

        Health Enemy(Vector2 position, int life = 100)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            enemy.transform.position = position;
            enemy.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = enemy.AddComponent<Health>();
            health.SetMax(life);
            Physics2D.SyncTransforms();
            return health;
        }

        FlagellaLash Lash => mutations.GetComponent<FlagellaLash>();

        [UnityTest]
        public IEnumerator Lash_HitsWithOneSecondOfMouthDamage_AfterTheDelay()
        {
            mutations.Equip(flagella);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            weapon.ReportHit(enemy);
            Assert.AreEqual(1, Lash.Lashes);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(100, enemy.Current, "El daño entra hacia los 0,4 s, no al aparecer.");

            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(94, enemy.Current, "3 de daño × 2 golpes por segundo = 6.");
        }

        [UnityTest]
        public IEnumerator Lash_OnlyOneEveryThreeSeconds()
        {
            mutations.Equip(flagella);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            weapon.ReportHit(enemy);
            weapon.ReportHit(enemy);
            Assert.AreEqual(1, Lash.Lashes, "Recargando, el segundo golpe no lanza otro.");
            Assert.IsFalse(Lash.IsReady);
            Assert.AreEqual(3f, Lash.Cooldown, 0.001f);

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1, Lash.Lashes, "Su propio latigazo no lanza otro.");
        }

        [UnityTest]
        public IEnumerator LevelTwo_IsFasterAndStronger()
        {
            mutations.Equip(flagella);
            mutations.LevelUp(MutationSlot.Limbs);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            Assert.AreEqual(2.5f, Lash.Cooldown, 0.001f);
            Assert.AreEqual(8, Lash.LashDamage(), "6 × 1,25 = 7,5, redondeado a 8.");
            weapon.ReportHit(enemy);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(92, enemy.Current);
        }

        [UnityTest]
        public IEnumerator Lash_GoesToTheNearest_IfTheEnemyDiesFirst()
        {
            mutations.Equip(flagella);
            var first = Enemy(new Vector2(3f, 0f));
            var near = Enemy(new Vector2(4.5f, 0f));
            var far = Enemy(new Vector2(8f, 0f));
            yield return null;

            weapon.ReportHit(first);
            first.Kill();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(94, near.Current, "Va al más cercano a 2 u.");
            Assert.AreEqual(100, far.Current);
        }

        [UnityTest]
        public IEnumerator Lash_WithNobodyLeft_DoesNotSpendTheCooldown()
        {
            mutations.Equip(flagella);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            enemy.Kill();
            weapon.ReportHit(enemy);
            Assert.AreEqual(0, Lash.Lashes);
            Assert.IsTrue(Lash.IsReady);
        }

        [UnityTest]
        public IEnumerator Evolution_ChainsToTheTwoNearest()
        {
            mutations.Equip(flagella);
            mutations.LevelUp(MutationSlot.Limbs);
            mutations.LevelUp(MutationSlot.Limbs);
            var target = Enemy(new Vector2(3f, 0f));
            var a = Enemy(new Vector2(4.5f, 0f));
            var b = Enemy(new Vector2(3f, 1.5f));
            var c = Enemy(new Vector2(6f, 0f));
            yield return null;

            Assert.AreEqual(2, Lash.ChainTargets);
            weapon.ReportHit(target);
            // El latigazo a los 0,4 s y la cadena 0,4 s después.
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(92, target.Current);
            Assert.AreEqual(92, a.Current, "Salta con el mismo daño.");
            Assert.AreEqual(92, b.Current);
            Assert.AreEqual(100, c.Current, "Solo a los 2 más cercanos.");
        }

        [UnityTest]
        public IEnumerator Lash_CarriesTheMouthPoison()
        {
            attack.SetPoison(2, 3f);
            mutations.Equip(flagella);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            weapon.ReportHit(enemy);
            yield return new WaitForSeconds(0.6f);
            Assert.IsNotNull(enemy.GetComponent<Poison>(), "Vuelve a envenenar como la Boca.");
        }

        [UnityTest]
        public IEnumerator Unequipping_StopsTheLashes()
        {
            mutations.Equip(flagella);
            var enemy = Enemy(new Vector2(3f, 0f));
            yield return null;

            mutations.Unequip(MutationSlot.Limbs);
            yield return null;
            Assert.IsNull(Lash);
            weapon.ReportHit(enemy);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(100, enemy.Current);
        }
    }
}
