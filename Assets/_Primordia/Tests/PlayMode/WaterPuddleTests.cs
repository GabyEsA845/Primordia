using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Agua de charca del piso 3: apaga el quemado de quien la pisa (el lodo ya lo hacía) y no frena.</summary>
    public class WaterPuddleTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            foreach (var fx in Object.FindObjectsByType<SpriteFramePlayer>(FindObjectsInactive.Include))
                Object.DestroyImmediate(fx.gameObject);
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        GroundPuddle Puddle(bool sludge)
        {
            var texture = new Texture2D(64, 32);
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 16f));
            var puddle = GroundPuddle.Create(Track(new GameObject("Holder")).transform, Vector2.zero, new[] { sprite }, 0.2f, sludge);
            created.Add(puddle.gameObject);
            return puddle;
        }

        Health BurningEnemy(Vector2 position)
        {
            var enemy = Track(new GameObject("Enemy"));
            enemy.transform.position = position;
            enemy.AddComponent<BoxCollider2D>().size = Vector2.one * 0.5f;
            var health = enemy.AddComponent<Health>();
            enemy.AddComponent<Enemy>();
            Burn.Apply(health, 1, 10f);
            return health;
        }

        [UnityTest]
        public IEnumerator WaterPuddle_PutsOutAnEnemyThatBurns()
        {
            Puddle(sludge: false);
            var health = BurningEnemy(Vector2.zero);
            Physics2D.SyncTransforms();
            Assert.IsNotNull(health.GetComponent<Burn>(), "Antes de entrar, arde.");

            yield return new WaitForSeconds(0.3f);

            Assert.IsNull(health.GetComponent<Burn>(), "El agua apaga el quemado del enemigo.");
        }

        [UnityTest]
        public IEnumerator SludgePuddle_DoesNotPutOutTheFire()
        {
            Puddle(sludge: true);
            var health = BurningEnemy(Vector2.zero);
            Physics2D.SyncTransforms();

            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(health.GetComponent<Burn>(), "El lodo de charco solo frena: lo que apaga es el lodo profundo.");
        }

        [UnityTest]
        public IEnumerator WaterPuddle_LeavesAnEnemyOutsideItBurning()
        {
            Puddle(sludge: false);
            var health = BurningEnemy(new Vector2(20f, 0f));
            Physics2D.SyncTransforms();

            yield return new WaitForSeconds(0.3f);

            Assert.IsNotNull(health.GetComponent<Burn>());
        }

        [Test]
        public void PolishArt_PicksTheSizeByBodyHeight()
        {
            var small = new Sprite[1];
            var medium = new Sprite[2];
            var large = new Sprite[3];
            var sized = new PolishArt.Sized { s = small, m = medium, l = large };
            Assert.AreSame(small, sized.ForHeight(10f));
            Assert.AreSame(medium, sized.ForHeight(20f));
            Assert.AreSame(large, sized.ForHeight(40f));
        }

        [Test]
        public void PolishArt_FindsDoorLayersByTheme()
        {
            var art = Track(ScriptableObject.CreateInstance<PolishArt>());
            art.doors = new[] { new PolishArt.DoorLayers { theme = "Swamp" }, new PolishArt.DoorLayers { theme = "Soup" } };
            Assert.AreEqual("Soup", art.Door("Soup").theme);
            Assert.IsNull(art.Door("Nope"));
        }
    }
}
