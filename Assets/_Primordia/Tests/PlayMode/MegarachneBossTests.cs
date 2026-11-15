#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class MegarachneBossTests
    {
        GameObject room, actor;
        MegarachneBoss boss;
        [SetUp]
        public void SetUp()
        {
            room = new GameObject("SalaMegarachnePrueba");
            room.transform.position = new Vector2(200f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Enemies/Boss_Megarachne.prefab");
            Assert.IsNotNull(prefab, "Ejecuta Primordia > Boss > Crear sala Megarachne.");
            boss = Object.Instantiate(prefab, room.transform).GetComponent<MegarachneBoss>();
            boss.SetArena(room.transform, new Vector2(32f, 20f));
            Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown()
        {
            if (actor != null) Object.DestroyImmediate(actor);
            if (room != null) Object.DestroyImmediate(room);
        }
        [Test]
        public void Vida150SinAtaquesYConLimitador()
        {
            Assert.AreEqual(150, boss.Health.Max);
            Assert.AreEqual(150, boss.Health.Current);
            Assert.IsNotNull(boss.GetComponent<BossDamageLimiter>());
            Assert.AreEqual(0, boss.GetComponent<Enemy>().Data.ContactDamage);
            Assert.AreEqual(5, boss.CableCount);
            Assert.IsTrue(boss.Health.TakeDamage(10));
            Assert.AreEqual(140, boss.Health.Current);
        }
        [Test]
        public void ArteTieneNueveFilasYDropClimbUna()
        {
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>("Assets/_Primordia/Data/Bosses/MegarachneArt.asset");
            Assert.AreEqual(22, art.Sheets.Count);
            foreach (var sheet in art.Sheets)
            {
                Assert.AreEqual(sheet.name.EndsWith("_Drop") || sheet.name.EndsWith("_Climb") ? 1 : 9, sheet.rows);
                Assert.AreEqual(sheet.rows * sheet.frames, sheet.sprites.Length);
                Assert.IsTrue(sheet.sprites.All(s => s != null && s.pixelsPerUnit == 16f));
            }
            Assert.AreEqual(4, DirectionalArt.RowFor(180f, out bool flip));
            Assert.IsTrue(flip);
        }
        [UnityTest]
        public IEnumerator SubeInvulnerableYBajaVulnerableSinTeletransporte()
        {
            Assert.IsTrue(boss.ForceClimb());
            Vector2 previous = boss.transform.position;
            float until = Time.time + 12f;
            while (boss.CurrentMode == MegarachneBoss.Mode.Ground && Time.time < until)
            {
                yield return new WaitForFixedUpdate();
                Vector2 current = boss.transform.position;
                Assert.LessOrEqual(Vector2.Distance(previous, current), MegarachneBoss.GroundSpeed * Time.fixedDeltaTime + 0.05f);
                previous = current;
            }
            Assert.AreEqual(MegarachneBoss.Mode.OnCable, boss.CurrentMode);
            Assert.IsFalse(boss.Health.TakeDamage(10));
            until = Time.time + 8f;
            while (boss.CurrentMode == MegarachneBoss.Mode.OnCable && Time.time < until) yield return null;
            Assert.AreEqual(MegarachneBoss.Mode.Ground, boss.CurrentMode);
            Assert.AreEqual(0f, boss.VisualHeight);
            Assert.IsTrue(boss.Health.TakeDamage(10));
        }
        [UnityTest]
        public IEnumerator CicloAutomaticoYBajadaForzada()
        {
            float until = Time.time + 18f;
            while (boss.CurrentMode != MegarachneBoss.Mode.OnCable && Time.time < until) yield return null;
            Assert.AreEqual(MegarachneBoss.Mode.OnCable, boss.CurrentMode);
            yield return new WaitForSeconds(0.5f);
            boss.ForceDrop();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(MegarachneBoss.Mode.Ground, boss.CurrentMode);
            Assert.IsTrue(boss.Health.CanTakeDamage);
        }
        [Test]
        public void CopaDetectaEnemigoYRestauraOpacidad()
        {
            actor = new GameObject("EnemigoBajoCopa");
            actor.AddComponent<Enemy>();
            actor.AddComponent<CircleCollider2D>();
            actor.transform.position = (Vector2)room.transform.position + Vector2.up * 4f;
            Physics2D.SyncTransforms();
            boss.Canopy.Refresh();
            Assert.AreEqual(0.45f, boss.Canopy.Alpha, 0.001f);
            var trunk = room.transform.Find("NidoMegarachne/BaseTronco").GetComponent<SpriteRenderer>();
            Assert.AreEqual(1f, trunk.color.a);
            actor.SetActive(false);
            Physics2D.SyncTransforms();
            boss.Canopy.Refresh();
            Assert.AreEqual(1f, boss.Canopy.Alpha);
        }
        [Test]
        public void CopaDetectaJugador()
        {
            actor = new GameObject("JugadorBajoCopa");
            var player = actor.AddComponent<PlayerController>();
            player.readInput = false;
            actor.AddComponent<CircleCollider2D>();
            actor.transform.position = (Vector2)room.transform.position + Vector2.up * 4f;
            Physics2D.SyncTransforms();
            boss.Canopy.Refresh();
            Assert.AreEqual(0.45f, boss.Canopy.Alpha, 0.001f);
        }
        [UnityTest]
        public IEnumerator RodeaTroncoYAvanzaSeisUnidadesEnTresSegundos()
        {
            actor = new GameObject("ObjetivoTrasTronco");
            actor.transform.position = boss.transform.position + Vector3.up * 8f;
            boss.Target = actor.transform;
            Vector2 start = boss.GetComponent<Rigidbody2D>().position;
            var trunk = room.transform.Find("NidoMegarachne/Tronco").GetComponent<Collider2D>();
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                yield return new WaitForFixedUpdate();
                var separation = boss.GetComponent<Collider2D>().Distance(trunk);
                Assert.GreaterOrEqual(separation.distance, -0.001f, $"Posicion {boss.GetComponent<Rigidbody2D>().position}, separacion {separation.distance}");
            }
            Assert.GreaterOrEqual(Vector2.Distance(start, boss.GetComponent<Rigidbody2D>().position), 6f);
            Assert.Less(Vector2.Distance(boss.transform.position, actor.transform.position), 2f);
        }

        [Test]
        public void CablesAmarradosAlOchentaPorCientoDelSprite()
        {
            for (int i = 0; i < boss.CableCount; i++)
            {
                var stump = room.transform.Find("NidoMegarachne/Sigillaria_" + i).GetComponent<SpriteRenderer>();
                var line = room.transform.Find("NidoMegarachne/Cable_" + i).GetComponent<LineRenderer>();
                Vector3 end = line.GetPosition(1);
                float fraction = (end.y - stump.bounds.min.y) / stump.bounds.size.y;
                Assert.That(fraction, Is.InRange(0.75f, 0.85f));
                Assert.That(end.x, Is.InRange(stump.bounds.min.x, stump.bounds.max.x));
            }
        }

        [UnityTest]
        public IEnumerator JugadorNoAtraviesaTroncoNiLasCincoSigillarias()
        {
            boss.enabled = false;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/Player/Player.prefab");
            actor = Object.Instantiate(prefab);
            var player = actor.GetComponent<PlayerController>();
            player.readInput = false;
            var rb = actor.GetComponent<Rigidbody2D>();
            var boxes = room.transform.Find("NidoMegarachne").GetComponentsInChildren<BoxCollider2D>();
            Assert.AreEqual(6, boxes.Length);
            foreach (var box in boxes)
            {
                Assert.IsFalse(box.isTrigger);
                Assert.AreEqual(LayerMask.NameToLayer("Default"), box.gameObject.layer);
                Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(actor.layer, box.gameObject.layer));
                Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(boss.gameObject.layer, box.gameObject.layer));
                Vector2 start = new Vector2(box.bounds.center.x, box.bounds.min.y - 2f);
                rb.position = start;
                actor.transform.position = start;
                rb.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                player.SetInput(Vector2.up, Vector2.up);
                yield return new WaitForSeconds(1f);
                Assert.Greater(rb.position.y, start.y + 0.1f, box.name);
                Assert.Less(rb.position.y, box.bounds.min.y, box.name);
                player.SetInput(Vector2.zero, Vector2.up);
            }
        }

        [UnityTest]
        public IEnumerator PersigueJugadorEnSuelo()
        {
            actor = new GameObject("Objetivo");
            actor.transform.position = boss.transform.position + Vector3.right * 5f;
            boss.Target = actor.transform;
            float before = Vector2.Distance(boss.transform.position, actor.transform.position);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Vector2.Distance(boss.transform.position, actor.transform.position), before - 1f);
        }
    }
    // Se ejecuta sola: la escena real contiene gestores globales de partida.
    [Explicit("Ejecutar aislada con testFilter MegarachneBossSceneTests.")]
    public class MegarachneBossSceneTests
    {
        const string ScenePath = "Assets/_Primordia/Scenes/Test_BossMegarachne.unity";
        [UnityTest]
        public IEnumerator EscenaRealPersigueOchoUnidadesEnTresSegundos()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var boss = Object.FindAnyObjectByType<MegarachneBoss>(FindObjectsInactive.Include);
            Assert.IsNotNull(boss);
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player);
            player.readInput = false;
            var rb = player.GetComponent<Rigidbody2D>();
            rb.position = (Vector2)boss.transform.position + Vector2.up * 8f;
            player.transform.position = rb.position;
            boss.transform.parent.gameObject.SetActive(true);
            yield return null;
            Physics2D.SyncTransforms();
            Vector2 start = boss.GetComponent<Rigidbody2D>().position;
            var hits = new RaycastHit2D[32];
            int count = boss.GetComponent<Rigidbody2D>().Cast(Vector2.up, new ContactFilter2D { useTriggers = false }, hits, 0.12f);
            for (int i = 0; i < count; i++)
                Debug.Log($"Diagnostico M1: {hits[i].collider.name}, distancia {hits[i].distance}, trigger {hits[i].collider.isTrigger}");
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(player.transform, boss.Target);
            float moved = Vector2.Distance(start, boss.GetComponent<Rigidbody2D>().position);
            Debug.Log($"Diagnostico M1: desplazamiento {moved} u en 3 s; inicio {start}; final {boss.transform.position}");
            Assert.GreaterOrEqual(moved, 6f);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                // Camara de inspeccion temporal: no modifica la escena guardada.
                var cameraObject = new GameObject("CapturaMegarachneM1");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12f;
                camera.depth = 100f;
                camera.transform.position = new Vector3(boss.ArenaCenter.x, boss.ArenaCenter.y, -10f);
                yield return null;
                var texture = new RenderTexture(1600, 1000, 24);
                var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                try
                {
                    camera.aspect = 1.6f;
                    cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = texture };
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                    RenderTexture.active = texture;
                    pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                    pixels.Apply();
                    System.IO.File.WriteAllBytes("Logs/mega14-scene.png", pixels.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    texture.Release();
                    Object.Destroy(texture);
                    Object.Destroy(pixels);
                    Object.Destroy(cameraObject);
                }
            }
        }
    }

}
#endif
