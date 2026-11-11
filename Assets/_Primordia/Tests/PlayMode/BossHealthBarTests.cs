using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Primordia.Tests
{
    /// <summary>Barra de vida del jefe: barra fantasma y aspecto propio con fases y muescas (Anomalocaris).</summary>
    public class BossHealthBarTests
    {
        readonly List<Object> created = new();
        BossHealthBar bar;

        class FakeBoss : MonoBehaviour, IBoss, IPhasedBoss
        {
            public Health health;
            public BossBarStyle style;
            public int phase;
            public string DisplayName => "Prueba";
            public Health Health => health;
            public BossBarStyle BarStyle => style;
            public Vector2 ArenaCenter => Vector2.zero;
            public void SetArena(Transform center, Vector2 size) { }
            public void PlayIntroPose(float hold) { }
            public void BeginFight() { }
            public int PhaseIndex => phase;
            public float[] PhaseThresholds { get; } = { 0.6f, 0.25f };
        }

        [SetUp]
        public void SetUp()
        {
            // Inactiva mientras se monta: la barra necesita sus piezas en el Awake.
            var canvas = Track(new GameObject("GameUI", typeof(RectTransform), typeof(Canvas)));
            canvas.SetActive(false);
            var root = new GameObject("BossBar", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var back = new GameObject("Back", typeof(RectTransform));
            back.transform.SetParent(root.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(back.transform, false);
            var fillImage = fill.AddComponent<Image>();
            var title = new GameObject("Title", typeof(RectTransform)).AddComponent<Text>();
            title.transform.SetParent(root.transform, false);
            bar = canvas.AddComponent<BossHealthBar>();
            bar.Configure(root, (RectTransform)fill.transform, fillImage, title);
            canvas.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var boss in Object.FindObjectsByType<FakeBoss>(FindObjectsInactive.Include))
            {
                ActiveBoss.Clear(boss);
                Object.DestroyImmediate(boss.gameObject);
            }
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

        FakeBoss CreateBoss(BossBarSkin skin = null)
        {
            var go = new GameObject("Boss");
            var health = go.AddComponent<Health>();
            health.SetMax(100);
            var boss = go.AddComponent<FakeBoss>();
            boss.health = health;
            if (skin != null)
                go.AddComponent<BossBarAppearance>().Configure(skin);
            ActiveBoss.Set(boss);
            return boss;
        }

        Sprite MakeSprite(int width, int height)
        {
            var texture = Track(new Texture2D(width, height));
            return Track(Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f));
        }

        BossBarSkin CreateSkin()
        {
            Sprite[] Frames(int count, int width, int height)
            {
                var frames = new Sprite[count];
                for (int i = 0; i < count; i++)
                    frames[i] = MakeSprite(width, height);
                return frames;
            }

            var skin = Track(ScriptableObject.CreateInstance<BossBarSkin>());
            var phases = new[]
            {
                new BossBarSkin.PhaseLook { fill = Frames(1, 200, 8), eyes = Frames(1, 258, 32) },
                new BossBarSkin.PhaseLook { fill = Frames(1, 200, 8), eyes = Frames(1, 258, 32) },
                new BossBarSkin.PhaseLook { fill = Frames(2, 200, 8), eyes = Frames(2, 258, 32) },
            };
            var dark = new BossBarSkin.PhaseLook { fill = Frames(1, 200, 8), eyes = Frames(2, 258, 32) };
            skin.Configure(MakeSprite(258, 32), MakeSprite(258, 32), new RectInt(38, 12, 200, 8), MakeSprite(200, 8),
                MakeSprite(200, 8), phases, dark, MakeSprite(8, 32), MakeSprite(8, 32), Frames(5, 16, 34));
            return skin;
        }

        [UnityTest]
        public IEnumerator Ghost_FillDropsQuickly_TrailWaitsThenDrains()
        {
            var boss = CreateBoss();
            yield return null;
            Assert.AreEqual(1f, bar.ShownRatio, 1e-4f);

            boss.health.TakeDamage(40);
            yield return null;
            Assert.AreEqual(1f, bar.TrailRatio, 1e-4f, "El trazo se queda donde estaba la vida.");

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(0.6f, bar.ShownRatio, 1e-4f, "El relleno ya ha bajado (~0,12 s).");
            Assert.AreEqual(1f, bar.TrailRatio, 1e-4f, "El trazo aún espera.");

            yield return new WaitForSecondsRealtime(0.9f);
            Assert.AreEqual(0.6f, bar.TrailRatio, 1e-4f, "Pasada la espera, el trazo se vacía hasta la vida.");
        }

        [UnityTest]
        public IEnumerator Ghost_NewHitRestartsTheWait()
        {
            var boss = CreateBoss();
            yield return null;
            boss.health.TakeDamage(10);
            yield return new WaitForSecondsRealtime(0.3f);
            boss.health.TakeDamage(10);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(1f, bar.TrailRatio, 1e-4f, "Cada golpe vuelve a hacer esperar al trazo.");
        }

        [UnityTest]
        public IEnumerator Plain_WithoutAppearance_UsesThePlainBar()
        {
            CreateBoss();
            yield return null;
            Assert.IsNull(bar.ActiveSkin);
            Assert.IsTrue(bar.Root.transform.Find("Back").gameObject.activeSelf);
            Assert.IsNotNull(bar.Root.transform.Find("Back/Trail"), "La barra lisa también tiene trazo.");
        }

        [UnityTest]
        public IEnumerator Skin_ShowsFrameFillAndEyesOfThePhase()
        {
            var skin = CreateSkin();
            var boss = CreateBoss(skin);
            yield return null;

            Assert.AreSame(skin, bar.ActiveSkin);
            Assert.IsFalse(bar.Root.transform.Find("Back").gameObject.activeSelf, "La barra lisa se oculta.");
            Assert.AreSame(skin.Frame, bar.SkinFrameSprite);
            Assert.AreSame(skin.Phase(0).fill[0], bar.SkinFillSprite);
            Assert.AreSame(skin.Phase(0).eyes[0], bar.SkinEyesSprite);
            Assert.AreEqual(2, bar.NotchCount);
            Assert.IsFalse(bar.IsNotchBroken(0));

            boss.phase = 1;
            yield return null;
            Assert.AreSame(skin.Phase(1).fill[0], bar.SkinFillSprite);
            Assert.IsTrue(bar.IsNotchBroken(0), "Al pasar el umbral del 60 % se rompe su muesca.");
            Assert.IsTrue(bar.IsNotchBreaking(0), "Y se ve romperse.");
            Assert.IsFalse(bar.IsNotchBroken(1));

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(bar.IsNotchBreaking(0), "La rotura dura 5 cuadros de 70 ms.");
            Assert.IsTrue(bar.IsNotchBroken(0));
        }

        [UnityTest]
        public IEnumerator Skin_Immune_UsesTheSilhouetteWithoutLock()
        {
            var skin = CreateSkin();
            var boss = CreateBoss(skin);
            boss.phase = 2;
            boss.style = BossBarStyle.Immune;
            yield return null;

            Assert.AreSame(skin.FrameDark, bar.SkinFrameSprite);
            Assert.AreSame(skin.Dark.fill[0], bar.SkinFillSprite);
            Assert.Contains(bar.SkinEyesSprite, skin.Dark.eyes);
            Assert.IsTrue(bar.IsNotchBroken(0) && bar.IsNotchBroken(1));
            Assert.IsFalse(bar.IsNotchBreaking(0), "Si la barra aparece ya pasada la fase, las muescas salen rotas sin animación.");
        }

        [UnityTest]
        public IEnumerator Skin_PhaseFrames_AndThePhaseChangeEffect()
        {
            var skin = CreateSkin();
            var armor = MakeSprite(258, 32);
            var soft = MakeSprite(258, 32);
            var crack = new[] { MakeSprite(258, 32), MakeSprite(258, 32) };
            skin.Phase(0).frame = new[] { armor };
            skin.Phase(1).frame = new[] { soft };
            skin.Dark.frame = crack;
            skin.ConfigurePhaseChange(new[] { MakeSprite(258, 32), MakeSprite(258, 32), MakeSprite(258, 32) }, 0.08f);
            var boss = CreateBoss(skin);
            yield return null;

            Assert.AreSame(armor, bar.SkinFrameSprite, "Cada fase con su propio marco (el Euriptérido acorazado).");
            Assert.IsFalse(bar.ShowsPhaseChange, "Al aparecer la barra no hay efecto de cambio.");

            boss.style = BossBarStyle.Immune;
            yield return null;
            Assert.Contains(bar.SkinFrameSprite, crack, "Inmune, el marco de su aspecto a oscuras (el caparazón agrietándose).");

            boss.style = BossBarStyle.Normal;
            boss.phase = 1;
            yield return null;
            Assert.AreSame(soft, bar.SkinFrameSprite, "Pasa al marco de la fase siguiente.");
            Assert.IsTrue(bar.ShowsPhaseChange, "Y se ve el efecto de cambio (el caparazón saltando).");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(bar.ShowsPhaseChange, "Una sola vez: 3 cuadros de 80 ms.");
        }

        BossBarSkin CreateMeganeuraSkin()
        {
            var skin = CreateSkin();
            for (int i = 0; i < 3; i++)
                skin.Phase(i).frame = new[] { MakeSprite(300, 32) };
            skin.Dark.eyes = System.Array.Empty<Sprite>();
            skin.ConfigureMeganeura(new BossBarSkin.PhaseEffect[]
            {
                null,
                new() { frames = new[] { MakeSprite(300, 32), MakeSprite(300, 32) }, frameTime = 0.08f },
                new() { frames = new[] { MakeSprite(300, 32), MakeSprite(300, 32) }, frameTime = 0.07f },
            }, new[] { MakeSprite(300, 32), MakeSprite(300, 32) });
            return skin;
        }

        [UnityTest]
        public IEnumerator MeganeuraSkin_PhasesAndCloudsKeepTheCurrentFrame()
        {
            var skin = CreateMeganeuraSkin();
            var boss = CreateBoss(skin);
            for (int phase = 0; phase < 3; phase++)
            {
                boss.phase = phase;
                boss.style = BossBarStyle.Normal;
                yield return null;
                Assert.AreSame(skin, bar.ActiveSkin);
                Assert.AreSame(skin.Phase(phase).frame[0], bar.SkinFrameSprite);
                Assert.Contains(bar.SkinFillSprite, skin.Phase(phase).fill);
                boss.style = BossBarStyle.Immune;
                yield return null;
                Assert.AreSame(skin.Phase(phase).frame[0], bar.SkinFrameSprite);
                Assert.AreSame(skin.Dark.fill[0], bar.SkinFillSprite);
                Assert.Contains(bar.SkinEyesSprite, skin.Phase(phase).eyes, "Los ojos de la fase se conservan al volar.");
            }
        }

        [UnityTest]
        public IEnumerator MeganeuraSkin_BottomIsRelativeToScreenAndHidesAfterDeath()
        {
            var screen = (RectTransform)bar.transform;
            screen.sizeDelta = new Vector2(800f, 600f);
            var original = (RectTransform)bar.Root.transform;
            original.anchorMin = original.anchorMax = original.pivot = new Vector2(0.5f, 1f);
            original.anchoredPosition = new Vector2(0f, -6f);
            var boss = CreateBoss(CreateMeganeuraSkin());
            yield return null;
            var view = (RectTransform)screen.Find("BossBarSkin");
            Assert.IsNotNull(view);
            Assert.AreEqual(new Vector2(0.5f, 0f), view.anchorMin);
            Assert.AreEqual(view.anchorMin, view.anchorMax);
            Assert.AreEqual(view.anchorMin, view.pivot);
            Assert.AreEqual(new Vector2(0f, 24f), view.anchoredPosition);
            var corners = new Vector3[4];
            view.Find("Frame").GetComponent<RectTransform>().GetWorldCorners(corners);
            Assert.Less(screen.InverseTransformPoint(corners[1]).y, screen.rect.center.y);
            boss.health.TakeDamage(100);
            yield return null;
            Assert.IsFalse(view.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator MeganeuraSkin_SparksStartAtTenPercentWithoutWaitingForGhost()
        {
            var boss = CreateBoss(CreateMeganeuraSkin());
            yield return null;
            var view = (RectTransform)bar.transform.Find("BossBarSkin");
            var sparks = view.Find("LowSparks").GetComponent<Image>();
            boss.health.TakeDamage(89);
            yield return null;
            Assert.IsFalse(sparks.enabled);
            boss.health.TakeDamage(1);
            yield return null;
            Assert.IsTrue(sparks.enabled);
            Assert.IsNotNull(sparks.sprite);
            Assert.LessOrEqual(Mathf.Abs(view.anchoredPosition.x), 1f);
            Assert.LessOrEqual(Mathf.Abs(view.anchoredPosition.y - 24f), 1f);
            boss.health.TakeDamage(1);
            yield return null;
            Assert.IsTrue(sparks.enabled);
        }

        [UnityTest]
        public IEnumerator LegacySkin_StaysAtTopAndHasNoSparksAtLowHealth()
        {
            var skin = CreateSkin();
            var boss = CreateBoss(skin);
            boss.health.TakeDamage(90);
            yield return null;
            var view = (RectTransform)bar.Root.transform.Find("BossBarSkin");
            Assert.IsFalse(skin.BottomAnchored);
            Assert.AreEqual(new Vector2(0.5f, 1f), view.anchorMin);
            Assert.AreEqual(new Vector2(0f, -12f), view.anchoredPosition);
            Assert.IsFalse(view.Find("LowSparks").GetComponent<Image>().enabled);
        }

        [Test]
        public void PhaseEffects_UseDestinationPhaseTimingAndLegacyFallback()
        {
            var skin = CreateMeganeuraSkin();
            var legacy = MakeSprite(300, 32);
            skin.ConfigurePhaseChange(new[] { legacy }, 0.08f);
            Assert.AreSame(legacy, skin.PhaseChangeFrame(0f, 0));
            Assert.IsNotNull(skin.PhaseChangeFrame(0.15f, 1));
            Assert.IsNull(skin.PhaseChangeFrame(0.16f, 1));
            Assert.IsNull(skin.PhaseChangeFrame(0.14f, 2));
            Assert.AreSame(legacy, skin.PhaseChangeFrame(0f, 3));
            Assert.IsNull(skin.PhaseChangeFrame(float.PositiveInfinity, 1));
        }

#if UNITY_EDITOR
        [Test]
        public void MeganeuraPrefab_HasSkinAndReportsHealthThresholds()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Primordia/Prefabs/Enemies/Boss_Meganeura.prefab");
            Assert.IsNotNull(prefab);
            var appearance = prefab.GetComponent<BossBarAppearance>();
            Assert.IsNotNull(appearance);
            Assert.IsNotNull(appearance.Skin);
            Assert.IsTrue(appearance.Skin.BottomAnchored);
            var instance = Track(Object.Instantiate(prefab));
            instance.SetActive(false);
            var boss = instance.GetComponent<MeganeuraBoss>();
            var phased = boss as IPhasedBoss;
            Assert.IsNotNull(phased);
            CollectionAssert.AreEqual(new[] { 0.65f, 0.30f }, phased.PhaseThresholds);
            var health = instance.GetComponent<Health>();
            health.IsInvulnerable = false;
            health.SetMax(100);
            Assert.AreEqual(0, phased.PhaseIndex);
            health.TakeDamage(35);
            Assert.AreEqual(1, phased.PhaseIndex);
            health.TakeDamage(35);
            Assert.AreEqual(2, phased.PhaseIndex);
            Assert.AreEqual(3, appearance.Skin.PhaseCount);
            Assert.AreEqual(new RectInt(43, 12, 241, 8), appearance.Skin.Window);
        }
#endif

        [UnityTest]
        public IEnumerator Anomalocaris_ReportsPhasesForTheBar()
        {
            var go = new GameObject("Anomalocaris");
            go.SetActive(false);
            var boss = go.AddComponent<AnomalocarisBoss>();
            Assert.AreEqual(0, boss.PhaseIndex);
            CollectionAssert.AreEqual(new[] { 0.6f, 0.25f }, boss.PhaseThresholds);
            boss.ForceStage(AnomalocarisBoss.Stage.Encircle);
            Assert.AreEqual(1, boss.PhaseIndex);
            boss.ForceStage(AnomalocarisBoss.Stage.Eyes);
            Assert.AreEqual(2, boss.PhaseIndex);
            boss.ForceStage(AnomalocarisBoss.Stage.Frenzy);
            Assert.AreEqual(2, boss.PhaseIndex);
            Object.DestroyImmediate(go);
            yield break;
        }
    }
}
