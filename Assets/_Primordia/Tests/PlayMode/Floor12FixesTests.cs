using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class Floor12FixesTests
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
            var flash = GameObject.Find("PurpleFlash");
            if (flash != null)
                Object.DestroyImmediate(flash);
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        Health CreateHealth(int max, params System.Type[] extra)
        {
            var go = Track(new GameObject("Target"));
            foreach (var type in extra)
                go.AddComponent(type);
            var health = go.AddComponent<Health>();
            health.SetMax(max);
            health.ResetHealth();
            health.InvulnerabilityAfterHit = 0f;
            return health;
        }

        class StubBoss : MonoBehaviour, IBoss
        {
            public string DisplayName => "Stub";
            public Health Health => GetComponent<Health>();
            public BossBarStyle BarStyle => default;
            public Vector2 ArenaCenter => Vector2.zero;
            public void SetArena(Transform center, Vector2 size) { }
            public void PlayIntroPose(float hold) { }
            public void BeginFight() { }
        }

        [UnityTest]
        public IEnumerator Poison_OnABoss_NeverHurtsMoreThanTheCapPerSecond_ButNormalTargetsTakeAll()
        {
            var boss = CreateHealth(100, typeof(StubBoss));
            var normal = CreateHealth(100);
            Poison.Apply(boss, 6, 3f);
            Poison.Apply(normal, 6, 3f);

            yield return new WaitForSeconds(1.3f);

            Assert.AreEqual(100 - Poison.BossDamagePerSecondCap, boss.Current, "El jefe solo recibe el tope por golpe.");
            Assert.AreEqual(100 - 6, normal.Current, "Un enemigo normal recibe el veneno entero.");
        }

        [Test]
        public void PurpleHeart_GivesTheAuraAndBlockingDoesNotBreakAnything()
        {
            var health = CreateHealth(6);
            Assert.IsNull(health.GetComponent<PurpleHeartFx>());

            health.AddPurpleHeart();
            Assert.IsNotNull(health.GetComponent<PurpleHeartFx>(), "Con un corazón morado se pone el efecto.");
            Assert.IsTrue(health.transform.Find("PurpleAura").GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(health.TakeDamage(2));
            Assert.AreEqual(6, health.Current, "El corazón morado sigue parando el golpe.");
            Assert.IsNotNull(GameObject.Find("PurpleFlash"), "Al bloquear hay un destello en pantalla.");
            Assert.IsFalse(health.transform.Find("PurpleAura").GetComponent<SpriteRenderer>().enabled, "Gastado, el aura se apaga.");
        }

        [UnityTest]
        public IEnumerator GreenHalfHearts_PairUpIntoWholeGreenHearts()
        {
            var health = CreateHealth(6);
            var hud = Track(new GameObject("Hearts", typeof(RectTransform)));
            var display = hud.AddComponent<UI.HeartsDisplay>();
            Sprite Frame(string name) => Track(Sprite.Create(Track(new Texture2D(16, 16)), new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f));
            display.Configure(health, Frame("Full"), Frame("Half"), Frame("Empty"));
            yield return null;

            int Icons() => hud.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i => i.name == "BarrierHeart" && i.gameObject.activeSelf);

            health.Barrier = 1;
            Assert.AreEqual(1, Icons(), "Un medio verde es un icono.");
            health.Barrier = 2;
            Assert.AreEqual(1, Icons(), "Dos medios forman un corazón verde entero.");
            health.Barrier = 3;
            Assert.AreEqual(2, Icons(), "Tres: un entero y un medio.");
            Assert.AreEqual(3, display.BarrierShown);
            health.Barrier = 4;
            Assert.AreEqual(2, Icons());
            Assert.AreEqual(4, display.BarrierShown);
        }
    }
}
