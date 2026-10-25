using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Bocas del piso 2. Cnidocitos: el arpón atraviesa a tantos enemigos como diga el ataque (sin dar dos veces al mismo) y
    /// el Ramillete dispara un abanico cada N disparos. Placas mandibulares: Quebrar (+25 % de daño de todo durante unos
    /// segundos, con su marca), el empujón, el alcance medido desde la boca y la Guillotina (cada cuarto mordisco, una
    /// segunda dentellada que llega más lejos, sin daño extra). Corona de brazos: el remolino aparece en el punto apuntado
    /// (como mucho a su alcance), daña y atrae a los de dentro, y con la Marea estalla al acabar.
    /// </summary>
    public class Floor2MouthsTests
    {
        readonly List<Object> created = new();
        GameObject projectileTemplate;
        Weapon weapon;
        AttackData attack;

        [SetUp]
        public void SetUp()
        {
            projectileTemplate = Track(new GameObject("ProjectileTemplate"));
            projectileTemplate.SetActive(false);
            projectileTemplate.AddComponent<Rigidbody2D>();
            projectileTemplate.AddComponent<CircleCollider2D>().radius = 0.1f;
            var projectile = projectileTemplate.AddComponent<Projectile>();

            attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, fireRate: 3f, damage: 2, speed: 20f, lifetime: 1f);

            var shooter = Track(new GameObject("Shooter"));
            shooter.layer = LayerMask.NameToLayer(CombatLayers.Player);
            weapon = shooter.AddComponent<Weapon>();
            weapon.Attack = attack;
        }

        [TearDown]
        public void TearDown()
        {
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
            enemy.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 2f);
            var health = enemy.AddComponent<Health>();
            health.SetMax(100);
            return health;
        }

        static int Flying() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;

        [UnityTest]
        public IEnumerator Harpoon_PiercesOneEnemy_ThenStopsAtTheNext()
        {
            attack.SetPierce(1);
            var first = Enemy(new Vector2(2f, 0f));
            var second = Enemy(new Vector2(4f, 0f));
            var third = Enemy(new Vector2(6f, 0f));
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(98, first.Current, "Le da una sola vez al atravesarlo.");
            Assert.AreEqual(98, second.Current, "Atraviesa a 1 y se queda en el segundo.");
            Assert.AreEqual(100, third.Current);
            Assert.AreEqual(0, Flying());
        }

        [UnityTest]
        public IEnumerator WithoutPierce_StopsAtTheFirst()
        {
            var first = Enemy(new Vector2(2f, 0f));
            var second = Enemy(new Vector2(4f, 0f));
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(98, first.Current);
            Assert.AreEqual(100, second.Current);
        }

        /// <summary>Recoge los empujones que recibe (en lugar de un EnemyAI).</summary>
        class PushProbe : MonoBehaviour, IPushable
        {
            public readonly List<(Vector2 velocity, float stun)> Pushes = new();

            public void Knockback(Vector2 velocity, float stunDuration, GameObject stunMarker = null) =>
                Pushes.Add((velocity, stunDuration));
        }

        AttackData Jaws()
        {
            attack.Configure(null, fireRate: 2f, damage: 4, speed: 12f, lifetime: 0.25f);
            attack.ConfigureMelee(4.6f, 110f, null);
            attack.ConfigureJaws(6f, 2f, 1.25f);
            return attack;
        }

        [Test]
        public void Shatter_MakesTheEnemyTakeAQuarterMoreDamage_ForAWhile()
        {
            var enemy = Enemy(new Vector2(2f, 0f));
            Shatter.Apply(enemy, 2f, 1.25f);
            enemy.TakeDamage(4);
            Assert.AreEqual(95, enemy.Current, "4 × 1,25 = 5.");

            Object.DestroyImmediate(enemy.GetComponent<Shatter>());
            enemy.TakeDamage(4);
            Assert.AreEqual(91, enemy.Current, "Sin Quebrar, daño normal.");
        }

        [Test]
        public void Bite_Shatters_AndPushesAway()
        {
            Jaws();
            var enemy = Enemy(new Vector2(2f, 0f));
            var probe = enemy.gameObject.AddComponent<PushProbe>();
            Physics2D.SyncTransforms();

            weapon.Fire(Vector2.right);
            Assert.AreEqual(96, enemy.Current);
            Assert.IsTrue(enemy.GetComponent<Shatter>().IsActive, "Queda quebrado.");
            Assert.AreEqual(1, probe.Pushes.Count);
            Assert.Greater(probe.Pushes[0].velocity.x, 0f, "Hacia fuera del que muerde.");

            weapon.Fire(Vector2.right);
            Assert.AreEqual(91, enemy.Current, "Quebrado recibe 5 en lugar de 4.");
        }

        [UnityTest]
        public IEnumerator Guillotine_EveryFourthBite_BitesAgainFurther()
        {
            Jaws();
            attack.ConfigureMelee(1.9f, 110f, null);
            attack.ConfigureGuillotine(4, 3.3f, null);
            var near = Enemy(new Vector2(1.2f, 0f));
            var far = Enemy(new Vector2(3f, 0f));
            Physics2D.SyncTransforms();
            int guillotines = 0;
            weapon.Guillotined += () => guillotines++;

            for (int i = 0; i < 3; i++)
                weapon.Fire(Vector2.right);
            Assert.AreEqual(0, guillotines);
            Assert.AreEqual(100, far.Current, "El mordisco normal no llega tan lejos.");

            int nearBefore = near.Current;
            weapon.Fire(Vector2.right);
            Assert.AreEqual(1, guillotines);
            yield return new WaitForSeconds(Weapon.GuillotineGap + 0.05f);
            Assert.AreEqual(96, far.Current, "La segunda dentellada llega más lejos, con el daño normal.");
            Assert.AreEqual(10, nearBefore - near.Current, "Al cercano le dan las dos dentelladas, quebrado: 5 + 5.");
        }

        [Test]
        public void Reach_FromTheMouth_IsOnlyWhatIsDrawn()
        {
            Jaws();
            attack.ConfigureMelee(1.9f, 110f, null);
            attack.SetMeleeFromMuzzle(true);
            var mouth = new GameObject("Mouth").transform;
            mouth.SetParent(weapon.transform, false);
            mouth.localPosition = new Vector3(0.5f, 0f, 0f);
            weapon.SetMuzzle(mouth);
            var inside = Enemy(new Vector2(2.2f, 0f));
            var outside = Enemy(new Vector2(3.2f, 0f));
            Physics2D.SyncTransforms();

            weapon.Fire(Vector2.right);
            Assert.AreEqual(96, inside.Current);
            Assert.AreEqual(100, outside.Current);
        }

        [UnityTest]
        public IEnumerator Windup_StartsTheAttackAtOnce_ButTheShotLeavesOnTheHitFrame()
        {
            weapon.Windup = 2f / 14f;
            int started = 0, fired = 0;
            weapon.Started += _ => started++;
            weapon.Fired += _ => fired++;

            Assert.IsTrue(weapon.TryFire(Vector2.right));
            Assert.AreEqual(1, started, "La animación de ataque empieza al atacar.");
            Assert.AreEqual(0, fired);
            Assert.AreEqual(0, Flying(), "El arpón aún no ha salido.");
            Assert.IsTrue(weapon.IsWindingUp);

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(1, fired, "Sale en el cuadro del golpe.");
            Assert.AreEqual(1, Flying());
            Assert.IsFalse(weapon.IsWindingUp);
        }

        /// <summary>Recoge los tirones de la corriente (en lugar de un EnemyAI).</summary>
        class PullProbe : MonoBehaviour, IPullable
        {
            public readonly List<Vector2> Pulls = new();

            public void Pull(Vector2 velocity) => Pulls.Add(velocity);
        }

        AttackData Crown(float duration = 2f)
        {
            var template = Track(new GameObject("WhirlTemplate"));
            template.transform.position = new Vector2(100f, 100f);
            template.AddComponent<Whirlpool>();
            attack.Configure(null, fireRate: 1f, damage: 1, speed: 1f, lifetime: 2f);
            attack.ConfigureArea(template, range: 6f, radius: 1.6f, duration: duration, tick: 0.4f, pull: 3f);
            return attack;
        }

        static Whirlpool Spawned()
        {
            foreach (var whirlpool in Whirlpool.Active)
                if (whirlpool != null && whirlpool.Radius > 0f)
                    return whirlpool;
            return null;
        }

        [Test]
        public void Crown_AppearsAtTheAim_UpToItsRange()
        {
            Crown();
            weapon.AimPoint = new Vector2(3f, 1f);
            weapon.Fire(Vector2.right);
            Assert.AreEqual(new Vector2(3f, 1f), (Vector2)Spawned().transform.position);
            Object.DestroyImmediate(Spawned().gameObject);

            weapon.AimPoint = new Vector2(20f, 0f);
            weapon.Fire(Vector2.right);
            Assert.AreEqual(new Vector2(6f, 0f), (Vector2)Spawned().transform.position, "Como mucho a 6 u.");
            Object.DestroyImmediate(Spawned().gameObject);
        }

        [UnityTest]
        public IEnumerator Whirl_HurtsAndPullsThoseInside_ButNotOutside()
        {
            Crown();
            var inside = Enemy(new Vector2(4f, 0f));
            var probe = inside.gameObject.AddComponent<PullProbe>();
            var outside = Enemy(new Vector2(8f, 0f));
            Physics2D.SyncTransforms();
            int hits = 0;
            weapon.HitTarget += _ => hits++;

            weapon.AimPoint = new Vector2(3f, 0f);
            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.7f);

            Assert.AreEqual(98, inside.Current, "1 de daño a los 0,2 s y otro a los 0,6 s.");
            Assert.AreEqual(100, outside.Current);
            Assert.AreEqual(2, hits, "Cuenta como golpe del arma (Flagelos, curarse al matar...).");
            Assert.Greater(probe.Pulls.Count, 0);
            Assert.Less(probe.Pulls[0].x, 0f, "Tira hacia el centro.");
            Object.DestroyImmediate(Spawned().gameObject);
        }

        [UnityTest]
        public IEnumerator Tide_BurstsAtTheEnd_PushingOutAndHurting()
        {
            Crown(duration: 0.3f).ConfigureTide(2.5f, 4, 10f, null);
            var enemy = Enemy(new Vector2(4f, 0f));
            var probe = enemy.gameObject.AddComponent<PushProbe>();
            Physics2D.SyncTransforms();

            weapon.AimPoint = new Vector2(3f, 0f);
            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.5f);

            Assert.IsNull(Spawned(), "Al acabar desaparece.");
            Assert.AreEqual(95, enemy.Current, "1 del remolino y 4 de la Marea.");
            Assert.AreEqual(1, probe.Pushes.Count);
            Assert.Greater(probe.Pushes[0].velocity.x, 0f, "Hacia fuera.");
        }

        [Test]
        public void Volley_EveryFourthShot_FiresAFanOfThree()
        {
            attack.ConfigureVolley(4, 3, 30f);
            var counts = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                int before = Flying();
                weapon.Fire(Vector2.right);
                counts.Add(Flying() - before);
            }
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 3, 1, 1, 1, 3 }, counts);
        }
    }
}
