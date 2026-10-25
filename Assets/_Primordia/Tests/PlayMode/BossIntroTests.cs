using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class BossIntroTests
    {
        readonly List<Object> created = new();
        GameManager manager;
        GameObject canvas;

        [SetUp]
        public void SetUp()
        {
            manager = Track(new GameObject("GameManager")).AddComponent<GameManager>();
            canvas = Track(new GameObject("GameUI", typeof(RectTransform), typeof(Canvas)));
            canvas.AddComponent<CinematicOverlay>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
                Object.DestroyImmediate(enemy.gameObject);
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include))
                Object.DestroyImmediate(projectile.gameObject);
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            ProjectilePool.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        AttackData CreateAttack()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), fireRate: 1f, damage: 1, speed: 5f, lifetime: 5f);
            return attack;
        }

        /// <summary>
        /// Virus con presentación corta (cámara 0,1 s, rótulo de 0,15 a 0,5 s), inactivo como en su sala hasta que
        /// entra el jugador: se activa con <see cref="GameObject.SetActive"/>. Se monta activo (si no, sus componentes
        /// no hacen su Awake y el Enemy no encuentra su vida) y se desactiva al final.
        /// </summary>
        (BossAI boss, BossIntro intro) CreateBoss(AttackData spiral = null, float startDelay = 0f)
        {
            var go = new GameObject("Boss");
            go.transform.position = new Vector3(300f, 300f, 0f);
            go.AddComponent<Weapon>();
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, 50, moveSpeed: 0f, detectionRange: 0f, contactDamage: 0);
            go.AddComponent<Enemy>().SetData(data);
            var boss = go.AddComponent<BossAI>();
            boss.Configure(spiral, null, null, delay: startDelay);
            var introData = Track(ScriptableObject.CreateInstance<BossIntroData>());
            introData.Configure(null, "Plaga primigenia", null, Color.black, Color.magenta);
            var intro = go.AddComponent<BossIntro>();
            intro.Configure(introData, newPanTime: 0.1f, newBannerAt: 0.15f, newBannerUntil: 0.5f);
            go.SetActive(false);
            return (boss, intro);
        }

        static int LiveProjectiles() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Count(p => p.gameObject.activeInHierarchy);

        [UnityTest]
        public IEnumerator Intro_PlaysAsACinematic_WithTheBanner_AndReturnsToPlay()
        {
            var (_, intro) = CreateBoss();
            intro.gameObject.SetActive(true);
            yield return null;
            yield return null;

            Assert.IsTrue(intro.Playing);
            Assert.AreEqual(GameState.Cinematic, manager.State, "El jugador no puede hacer nada mientras dura.");

            yield return new WaitForSeconds(0.3f);
            var banner = canvas.GetComponentInChildren<BossIntroBanner>();
            Assert.IsNotNull(banner, "El rótulo se crea solo en el lienzo del HUD.");
            Assert.IsTrue(banner.Showing);

            yield return new WaitForSeconds(0.9f);
            Assert.IsFalse(intro.Playing);
            Assert.IsTrue(intro.Played);
            Assert.IsFalse(intro.Skipped);
            Assert.AreEqual(GameState.Playing, manager.State);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(banner.Showing, "El rótulo se ha ido.");
        }

        [UnityTest]
        public IEnumerator Intro_CanBeSkipped()
        {
            var (_, intro) = CreateBoss();
            intro.Configure(intro.Data, newPanTime: 0.1f, newBannerAt: 0.15f, newBannerUntil: 5f);
            intro.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(intro.Playing);

            intro.skipPressed = true;
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(intro.Playing, "Saltarla la acaba enseguida, sin esperar al rótulo.");
            Assert.IsTrue(intro.Skipped);
            Assert.AreEqual(GameState.Playing, manager.State);
        }

        [UnityTest]
        public IEnumerator Boss_WaitsForTheIntro_BeforeItsFirstAttack()
        {
            var (_, intro) = CreateBoss(CreateAttack(), startDelay: 0.2f);
            intro.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(intro.Playing);
            Assert.AreEqual(0, LiveProjectiles(), "No dispara durante la presentación.");

            while (intro.Playing)
                yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(0, LiveProjectiles(), "Su espera antes del primer ataque empieza al acabar la presentación.");

            yield return new WaitForSeconds(0.4f);
            Assert.Greater(LiveProjectiles(), 0, "Después ya dispara.");
        }

        [UnityTest]
        public IEnumerator Intro_PlaysOnlyOncePerBoss()
        {
            var (_, intro) = CreateBoss();
            intro.gameObject.SetActive(true);
            while (!intro.Played || intro.Playing)
                yield return null;

            intro.gameObject.SetActive(false);
            intro.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.IsFalse(intro.Playing);
            Assert.AreEqual(GameState.Playing, manager.State);
        }

        [UnityTest]
        public IEnumerator Boss_WithoutIntro_StartsAsBefore()
        {
            var (boss, intro) = CreateBoss();
            Object.DestroyImmediate(intro);
            boss.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.AreEqual(GameState.Playing, manager.State);
        }
    }
}
