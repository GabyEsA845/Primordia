using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Banda de baba del Escupitajo venenoso: un sello cada 0,25 u (0,125 la fina), cada sello se seca a los segundos del ataque y la
    /// banda desaparece sola, arde, se quita de golpe al cambiar de sala y la deja la bola al rodar.
    /// </summary>
    public class SlimeTrailTests
    {
        readonly List<Object> created = new();
        ReactionKit kit;

        [SetUp]
        public void SetUp()
        {
            // Dos variantes de 8 cuadros para cada banda (1×1 px bastan).
            kit = Track(ScriptableObject.CreateInstance<ReactionKit>());
            kit.ConfigureSlime(Sheet(16), Sheet(16), Sheet(16), Sheet(16), Sheet(4));
            ReactionKit.Current = kit;
        }

        [TearDown]
        public void TearDown()
        {
            ReactionKit.Current = null;
            foreach (var trail in Object.FindObjectsByType<SlimeTrail>(FindObjectsInactive.Include))
                Object.DestroyImmediate(trail.gameObject);
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

        Sprite[] Sheet(int count)
        {
            var texture = Track(new Texture2D(1, 1));
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
                sprites[i] = Track(Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f)));
            return sprites;
        }

        [Test]
        public void Begin_WithoutSlimeArt_ReturnsNull()
        {
            ReactionKit.Current = null;
            Assert.IsNull(SlimeTrail.Begin(Vector2.zero, Vector2.right, false, SlimeTrail.Spacing, 3f, false));
        }

        [Test]
        public void Extend_PaintsAStampEveryQuarterUnit()
        {
            var trail = SlimeTrail.Begin(new Vector2(100f, 100f), Vector2.right, false, SlimeTrail.Spacing, 3f, false);
            Assert.AreEqual(1, trail.StampCount, "Un sello al empezar.");

            trail.Extend(new Vector2(101f, 100f));
            Assert.AreEqual(5, trail.StampCount, "Un metro más: sellos a 0,25, 0,5, 0,75 y 1.");

            trail.Extend(new Vector2(101.1f, 100f));
            Assert.AreEqual(5, trail.StampCount, "Un tramo corto no pinta nada hasta completar la distancia.");
            trail.Extend(new Vector2(101.3f, 100f));
            Assert.AreEqual(6, trail.StampCount, "La distancia sobrante se acumula.");
        }

        [Test]
        public void ThinTrail_PaintsTwiceAsDense()
        {
            var trail = SlimeTrail.Begin(new Vector2(100f, 100f), Vector2.right, true, SlimeTrail.ThinSpacing, 3f, false);
            trail.Extend(new Vector2(101f, 100f));
            Assert.AreEqual(9, trail.StampCount);
        }

        [UnityTest]
        public IEnumerator Stamps_DryFromTheTip_AndTheTrailDisappears()
        {
            var trail = SlimeTrail.Begin(new Vector2(100f, 100f), Vector2.right, false, SlimeTrail.Spacing, 0.4f, false);
            trail.Extend(new Vector2(100.5f, 100f));
            trail.Finish();
            Assert.AreEqual(3, trail.StampCount);

            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(trail == null, "Sin sellos y sin bola, la banda se quita sola.");
        }

        [Test]
        public void Clear_RemovesEveryStampAtOnce()
        {
            var trail = SlimeTrail.Begin(new Vector2(100f, 100f), Vector2.right, false, SlimeTrail.Spacing, 3f, false);
            trail.Extend(new Vector2(101f, 100f));

            trail.Clear();

            Assert.AreEqual(0, trail.StampCount);
            Assert.AreEqual(0, Flammables.Near(new Vector2(100.5f, 100f), 2f).Count, "Ya no se puede prender nada de la banda.");
        }

        [Test]
        public void Fire_EatsTheStampsItTouches()
        {
            var trail = SlimeTrail.Begin(new Vector2(100f, 100f), Vector2.right, false, SlimeTrail.Spacing, 3f, false);
            trail.Extend(new Vector2(102f, 100f));
            int before = trail.StampCount;
            Assert.AreEqual(9, before);

            int ignited = Flammables.IgniteNear(new Vector2(100f, 100f), 0.6f);

            Assert.Greater(ignited, 0, "El fuego encuentra sellos cerca.");
            Assert.AreEqual(before - ignited, trail.StampCount, "Cada sello prendido desaparece.");
            Assert.AreEqual(0, Flammables.Near(new Vector2(100f, 100f), 0.6f).Count);
        }

        [UnityTest]
        public IEnumerator Ball_LeavesATrailAsItRolls_AndItDriesAfterwards()
        {
            var template = Track(new GameObject("SpitTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.2f;
            var projectile = template.AddComponent<Projectile>();
            template.AddComponent<PoisonSpitBall>();

            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(projectile, fireRate: 0.4f, damage: 1, speed: 2f, lifetime: 1f);
            attack.SetPierce(99);
            attack.ConfigureSpit(SlimeTrail.Spacing, 0.5f, burstDamage: 0, burstRadius: 1.5f, burstPuddleSeconds: 0f);

            var shooter = Track(new GameObject("Shooter"));
            shooter.layer = LayerMask.NameToLayer(CombatLayers.Player);
            var weapon = shooter.AddComponent<Weapon>();
            weapon.Attack = attack;
            shooter.transform.position = new Vector2(100f, 100f);
            yield return null;

            weapon.Fire(Vector2.right);
            yield return new WaitForSeconds(0.6f);

            var trails = Object.FindObjectsByType<SlimeTrail>(FindObjectsSortMode.None);
            Assert.AreEqual(1, trails.Length, "Una banda por bola.");
            Assert.GreaterOrEqual(trails[0].StampCount, 3, "A 2 u/s, un sello cada 0,25 u.");

            yield return new WaitForSeconds(1.4f);
            Assert.AreEqual(0, Object.FindObjectsByType<SlimeTrail>(FindObjectsSortMode.None).Length, "La bola estalló y la baba se secó.");
        }
    }
}
