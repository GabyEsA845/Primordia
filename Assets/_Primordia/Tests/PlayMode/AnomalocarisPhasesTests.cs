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
    /// <summary>Anomalocaris: cerco (60–25 %), escena de los ojos (25 %) y frenesí con sus zarpazos marcados.</summary>
    public class AnomalocarisPhasesTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            AnomalocarisBubble.PopAll();
            foreach (var markers in Object.FindObjectsByType<AnomalocarisMarkers>(FindObjectsInactive.Include))
                Object.DestroyImmediate(markers.gameObject);
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

        Health Player(Vector2 position)
        {
            var player = Track(new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer(CombatLayers.Player) });
            player.transform.position = position;
            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.position = position;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var health = player.AddComponent<Health>();
            health.SetMax(20, healDifference: false);
            health.ResetHealth();
            return health;
        }

        static void MovePlayer(Health player, Vector2 position)
        {
            player.transform.position = position;
            player.GetComponent<Rigidbody2D>().position = position;
        }

        AnomalocarisBubble BubbleTemplate()
        {
            var bubble = Track(new GameObject("BubbleTemplate") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) });
            bubble.transform.position = new Vector2(500f, 500f);
            bubble.AddComponent<Rigidbody2D>();
            bubble.AddComponent<CircleCollider2D>().radius = 0.28f;
            bubble.AddComponent<Health>();
            var component = bubble.AddComponent<AnomalocarisBubble>();
            component.Configure(1, 100f, null);
            return component;
        }

        Sprite[] Frames(string name, int count, int size)
        {
            var texture = Track(new Texture2D(size, size));
            return Enumerable.Range(0, count)
                .Select(i => Track(Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f)))
                .ToArray();
        }

        AnomalocarisBoss Boss(Vector2 position, Vector2 strikeInterval, AnomalocarisBubble bubble = null,
            AnomalocarisBoss.Stage stage = AnomalocarisBoss.Stage.Stalk)
        {
            var center = Track(new GameObject("ArenaCenter")).transform;
            var root = new GameObject("Boss_Anomalocaris") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            root.SetActive(false);
            Track(root);
            root.transform.position = position;
            root.AddComponent<Rigidbody2D>();
            var health = root.AddComponent<Health>();
            health.SetMax(300, healDifference: false);
            health.ResetHealth();
            root.AddComponent<Enemy>();
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            var renderer = body.gameObject.AddComponent<SpriteRenderer>();
            var boss = root.AddComponent<AnomalocarisBoss>();
            boss.Configure(body, null, renderer, bubble, null);
            boss.ConfigurePhases(null, Frames("Path", 4, 16), Frames("Area", 4, 64));
            boss.SetArena(center, new Vector2(40f, 31f));
            boss.ForceStage(stage);
            root.SetActive(true);
            boss.SetTimings(strikeInterval, 3.5f, 0f);
            return boss;
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            for (float t = 0f; t < timeout && !condition(); t += Time.deltaTime)
                yield return null;
        }

        [UnityTest]
        public IEnumerator Stages_ChangeWithHealth_AndHealthStopsAtAQuarter_UntilTheEyes()
        {
            Player(new Vector2(0f, -40f));
            var boss = Boss(new Vector2(5f, 0f), new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.1f);

            boss.Health.TakeDamage(130);
            yield return null;
            Assert.AreEqual(AnomalocarisBoss.Stage.Encircle, boss.CurrentStage, "Por debajo del 60 %, el cerco.");

            boss.Health.TakeDamage(1000);
            Assert.AreEqual(boss.EyesHealth, boss.Health.Current, "No baja del 25 % antes de la escena.");
            yield return null;
            Assert.AreEqual(AnomalocarisBoss.Mode.Eyes, boss.CurrentMode, "Al 25 %, la escena de los ojos.");
            Assert.IsTrue(boss.Health.IsInvulnerable, "Inmune durante la escena.");
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
        }

        [UnityTest]
        public IEnumerator EyesScene_EndsInTheFrenzy_AndCanBeSkipped()
        {
            Player(new Vector2(0f, -40f));
            var boss = Boss(new Vector2(-8f, 0f), new Vector2(100f, 100f));
            yield return new WaitForSeconds(0.1f);
            boss.Health.TakeDamage(1000);
            yield return null;
            Assert.AreEqual(AnomalocarisBoss.Mode.Eyes, boss.CurrentMode);

            yield return new WaitForSeconds(1.5f);
            Assert.Greater(boss.transform.position.x, -8f, "Los ojos cruzan la sala.");

            boss.skipRequested = true;
            yield return null;
            yield return null;
            Assert.AreEqual(AnomalocarisBoss.Stage.Frenzy, boss.CurrentStage, "Saltada, empieza el frenesí.");
            Assert.IsFalse(boss.Health.IsInvulnerable);
            Assert.AreEqual(BossBarStyle.Enraged, boss.BarStyle);
            boss.Health.TakeDamage(10);
            Assert.AreEqual(boss.EyesHealth - 10, boss.Health.Current, "Ya baja del 25 %.");
        }

        [UnityTest]
        public IEnumerator Encircle_SpiralsAroundThePlayer_WithABubbleWall_ThenStrikes()
        {
            var player = Player(Vector2.zero);
            var boss = Boss(new Vector2(9f, 0f), Vector2.zero, BubbleTemplate(), AnomalocarisBoss.Stage.Encircle);
            boss.Health.TakeDamage(130);

            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Circle, boss.CurrentMode);
            Assert.Less(Vector2.Distance(boss.transform.position, player.transform.position), 7.5f, "La espiral se cierra.");
            Assert.Greater(AnomalocarisBubble.All.Count, 8, "Deja un muro de burbujas.");

            yield return new WaitForSeconds(2f);
            CollectionAssert.Contains(new[] { AnomalocarisBoss.Mode.Warn, AnomalocarisBoss.Mode.Dash,
                AnomalocarisBoss.Mode.Strike, AnomalocarisBoss.Mode.Vulnerable }, boss.CurrentMode, "Acaba con el zarpazo.");
        }

        [Test]
        public void Frenzy_MarksAreShorter_WithLessHealth()
        {
            Player(new Vector2(0f, -40f));
            var boss = Boss(new Vector2(5f, 0f), new Vector2(100f, 100f), stage: AnomalocarisBoss.Stage.Frenzy);
            boss.Health.TakeDamage(300 - boss.EyesHealth);
            Assert.AreEqual(1f, boss.MarkDuration, 0.01f, "Al empezar el frenesí, el primer dash se ve 1 s.");
            Assert.AreEqual(0.5f, boss.SecondMarkDuration, 0.01f, "Y el segundo, medio.");
            boss.Health.TakeDamage(boss.EyesHealth - 1);
            Assert.AreEqual(0.6f, boss.MarkDuration, 0.02f, "Casi muerto, menos.");
            Assert.AreEqual(0.35f, boss.SecondMarkDuration, 0.02f);
        }

        [UnityTest]
        public IEnumerator Frenzy_MarksALongDash_ThenLeapsToBite_AndIsStunned()
        {
            var player = Player(Vector2.zero);
            var boss = Boss(new Vector2(8f, 0f), new Vector2(100f, 100f), stage: AnomalocarisBoss.Stage.Frenzy);
            boss.Health.TakeDamage(250);

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Charge, 1f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Charge, boss.CurrentMode, "Empieza marcando el dash.");
            Assert.IsNotNull(boss.Markers);
            Assert.AreEqual(1, boss.Markers.PathPieces, "Una línea recta.");
            Assert.IsTrue(boss.Markers.HasArea);
            Assert.Less(boss.FrenzyPath[1].x, -3f, "Pasa por el jugador y sigue bastante más allá.");
            Assert.AreEqual(player.Max, player.Current, "Marcar no hace daño.");

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Stun, 2f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Stun, boss.CurrentMode, "Tras el dash y el salto, aturdido.");
            Assert.LessOrEqual(player.Current, player.Max - 2, "Quedarse en la línea cuesta al menos un corazón.");
            Assert.IsTrue(boss.Markers == null, "La marca se quita al lanzarse.");
            Assert.IsFalse(boss.Health.IsInvulnerable, "Aturdido se le puede pegar.");

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Chase, 1f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Chase, boss.CurrentMode, "Y luego persigue.");
        }

        [UnityTest]
        public IEnumerator Frenzy_ChasesThenMarksASecondDash_ThenBecomesAnInvulnerableSilhouette_AndRepeats()
        {
            var player = Player(Vector2.zero);
            player.SetMax(500, healDifference: true);
            var boss = Boss(new Vector2(8f, 0f), new Vector2(100f, 100f), stage: AnomalocarisBoss.Stage.Frenzy);
            boss.Health.TakeDamage(250);

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Chase, 3f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Chase, boss.CurrentMode);
            int beforeChase = player.Current;
            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Charge, 3f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Charge, boss.CurrentMode, "Tras perseguir, marca el segundo dash.");
            Assert.AreEqual(1, boss.FrenzyDash);
            Assert.Less(player.Current, beforeChase, "Persiguiendo muerde a quien se queda quieto.");

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Silhouette, 2f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Silhouette, boss.CurrentMode, "Tras el segundo dash, silueta.");
            Assert.IsTrue(boss.Health.IsInvulnerable);
            Assert.AreEqual(BossBarStyle.Immune, boss.BarStyle);
            int health = boss.Health.Current;
            boss.Health.TakeDamage(10);
            Assert.AreEqual(health, boss.Health.Current, "En silueta no se le puede dañar.");

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Charge, 4f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Charge, boss.CurrentMode, "Y vuelve a empezar.");
            Assert.AreEqual(0, boss.FrenzyDash);
            Assert.IsFalse(boss.Health.IsInvulnerable);
            Assert.AreEqual(BossBarStyle.Enraged, boss.BarStyle);
        }

        [UnityTest]
        public IEnumerator Frenzy_FirstDashAndLeap_AreDodged_ByLeavingTheLineInTime()
        {
            var player = Player(Vector2.zero);
            var boss = Boss(new Vector2(8f, 0f), new Vector2(100f, 100f), stage: AnomalocarisBoss.Stage.Frenzy);
            boss.Health.TakeDamage(250);

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Charge, 1f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Charge, boss.CurrentMode);
            // Lejos de la línea y del final del dash, donde salta.
            MovePlayer(player, new Vector2(0f, 8f));

            yield return WaitFor(() => boss.CurrentMode == AnomalocarisBoss.Mode.Stun, 2f);
            Assert.AreEqual(AnomalocarisBoss.Mode.Stun, boss.CurrentMode);
            Assert.AreEqual(player.Max, player.Current, "Fuera de la línea a tiempo, sin daño.");
        }
    }
}
