#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using Primordia.Rooms;
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
            boss.AutomaticAttacks = false;
            Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown()
        {
            if (actor != null) Object.DestroyImmediate(actor);
            if (room != null) Object.DestroyImmediate(room);
        }
        [Test]
        public void Vida150EnCableYConLimitador()
        {
            Assert.AreEqual(150, boss.Health.Max);
            Assert.AreEqual(150, boss.Health.Current);
            Assert.IsNotNull(boss.GetComponent<BossDamageLimiter>());
            Assert.AreEqual(0, boss.GetComponent<Enemy>().Data.ContactDamage);
            Assert.AreEqual(5, boss.CableCount);
            Assert.IsFalse(boss.Health.TakeDamage(10));
            Assert.AreEqual(150, boss.Health.Current);
        }
        [Test]
        public void ArteTieneNueveFilasYDropClimbUna()
        {
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>("Assets/_Primordia/Data/Bosses/MegarachneArt.asset");
            Assert.AreEqual(24, art.Sheets.Count);
            foreach (var sheet in art.Sheets)
            {
                Assert.AreEqual(sheet.name.EndsWith("_Drop") || sheet.name.EndsWith("_Climb") ? 1 : 9, sheet.rows);
                Assert.AreEqual(sheet.rows * sheet.frames, sheet.sprites.Length);
                Assert.IsTrue(sheet.sprites.All(s => s != null && s.pixelsPerUnit == 16f));
            }
            Assert.AreEqual(4, DirectionalArt.RowFor(180f, out bool flip));
            Assert.IsTrue(flip);
        }
        PlayerController MakePlayer(Vector2 point)
        {
            actor = new GameObject("JugadorPrueba");
            actor.transform.position = point;
            var player = actor.AddComponent<PlayerController>();
            player.readInput = false;
            player.Inertia = 0f;
            actor.AddComponent<CircleCollider2D>().radius = 0.2f;
            actor.GetComponent<Health>().SetMax(100, false);
            actor.GetComponent<Health>().ResetHealth();
            boss.Target = actor.transform;
            Physics2D.SyncTransforms();
            return player;
        }

        static IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float end = Time.time + seconds;
            while (!condition() && Time.time < end) yield return new WaitForFixedUpdate();
            Assert.IsTrue(condition(), "No se alcanz? el estado esperado dentro del tiempo l?mite.");
        }

        [UnityTest]
        public IEnumerator SaltoHaceDosUnaVezYVentanaDeUnSegundoYMedioSinPerseguir()
        {
            var player = MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            Assert.IsTrue(boss.ForceAttack(MegarachneBoss.AttackKind.Jump));
            Assert.IsFalse(boss.Health.TakeDamage(10));
            yield return Until(() => boss.CurrentMode == MegarachneBoss.Mode.Ground, 2f);
            float landed = Time.time;
            Assert.AreEqual(98, actor.GetComponent<Health>().Current);
            Assert.IsTrue(boss.Health.TakeDamage(10));
            Vector2 landing = boss.GetComponent<Rigidbody2D>().position;
            player.SetInput(Vector2.right, Vector2.right);
            while (Time.time - landed < 1.3f)
            {
                yield return new WaitForFixedUpdate();
                Assert.AreEqual(MegarachneBoss.Mode.Ground, boss.CurrentMode);
                Assert.Less(Vector2.Distance(landing, boss.GetComponent<Rigidbody2D>().position), 0.001f);
                Assert.AreEqual(98, actor.GetComponent<Health>().Current);
            }
            yield return Until(() => boss.CurrentMode == MegarachneBoss.Mode.OnCable, 0.4f);
            Assert.That(Time.time - landed, Is.InRange(1.45f, 1.6f));
            Assert.IsFalse(boss.Health.TakeDamage(10));
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(boss.VisualHeight, 0f);
        }

        [UnityTest]
        public IEnumerator MarcaSaltoSeFijaLosUltimosTresDecimosYSePuedeEsquivar()
        {
            MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            Assert.IsTrue(boss.ForceAttack(MegarachneBoss.AttackKind.Jump));
            yield return new WaitForSeconds(0.4f);
            actor.transform.position += Vector3.right;
            yield return new WaitForSeconds(0.2f);
            Assert.Less(Vector2.Distance(actor.transform.position, boss.AttackPoint), 0.01f);
            yield return new WaitForSeconds(0.2f);
            Vector2 locked = boss.AttackPoint;
            actor.transform.position += Vector3.right * 3f;
            Physics2D.SyncTransforms();
            yield return Until(() => boss.CurrentMode == MegarachneBoss.Mode.Ground, 1f);
            Assert.Less(Vector2.Distance(locked, boss.transform.position), 0.01f);
            Assert.AreEqual(100, actor.GetComponent<Health>().Current);
        }

        [UnityTest]
        public IEnumerator SaltoNoDanyaFueraDelRadio()
        {
            MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            boss.ForceAttack(MegarachneBoss.AttackKind.Jump);
            yield return new WaitForSeconds(1.1f);
            actor.transform.position = boss.AttackPoint + Vector2.right * (MegarachneBoss.JumpRadius + 0.05f);
            Physics2D.SyncTransforms();
            yield return Until(() => boss.CurrentMode == MegarachneBoss.Mode.Ground, 1f);
            Assert.AreEqual(100, actor.GetComponent<Health>().Current);
        }

        [UnityTest]
        public IEnumerator RafagaCreaSeisBolitasYUnaTelaranaPorBolita()
        {
            MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            Assert.IsTrue(boss.ForceAttack(MegarachneBoss.AttackKind.Silk));
            yield return Until(() => boss.ActiveSilkCount > 0, 0.6f);
            Assert.AreEqual(6, boss.ActiveSilkCount);
            yield return Until(() => boss.ActiveSilkCount == 0, 2f);
            Assert.AreEqual(6, room.GetComponentsInChildren<MegarachneBossWeb>().Length);
        }

        [UnityTest]
        public IEnumerator TelaranaFrenaCuarentaPorCientoSinAcumularNiDejarResiduo()
        {
            Vector2 point = (Vector2)room.transform.position + new Vector2(-6f, -5f);
            var player = MakePlayer(point);
            boss.CreateWeb(point);
            boss.CreateWeb(point);
            player.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(player.MoveSpeed * 0.6f, actor.GetComponent<Rigidbody2D>().linearVelocity.x, 0.01f);
            actor.transform.position = point + Vector2.right * 3f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(player.MoveSpeed, actor.GetComponent<Rigidbody2D>().linearVelocity.x, 0.01f);
            Assert.IsFalse(player.IsSlowed);
        }

        [UnityTest]
        public IEnumerator TelaranaNoAlargaNiIntensificaVenenoAlSalir()
        {
            Vector2 point = (Vector2)room.transform.position + new Vector2(-6f, -5f);
            var player = MakePlayer(point);
            player.Slow(0.7f, 5f);
            boss.CreateWeb(point);
            player.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            actor.transform.position = point + Vector2.right * 3f;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0.7f, player.SlowFactor, 0.001f);
            Assert.AreEqual(player.MoveSpeed * 0.7f, actor.GetComponent<Rigidbody2D>().linearVelocity.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator TelaranaArdeConElSistemaExistenteYDejaDeFrenar()
        {
            Vector2 point = (Vector2)room.transform.position + new Vector2(-6f, -5f);
            var player = MakePlayer(point);
            var web = boss.CreateWeb(point);
            Assert.AreEqual(1, Flammables.IgniteNear(point, 0.1f));
            Assert.IsTrue(web.IsBurning);
            player.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(player.MoveSpeed, actor.GetComponent<Rigidbody2D>().linearVelocity.x, 0.01f);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(web == null);
        }

        [UnityTest]
        public IEnumerator TelaranasDuranOchoSegundosYElLimiteRetiraLaMasAntigua()
        {
            var first = boss.CreateWeb(room.transform.position);
            yield return new WaitForSeconds(0.05f);
            for (int i = 0; i < 18; i++) boss.CreateWeb(room.transform.position);
            yield return null;
            Assert.IsTrue(first == null);
            Assert.AreEqual(18, room.GetComponentsInChildren<MegarachneBossWeb>().Length);
            yield return new WaitForSeconds(7.8f);
            Assert.AreEqual(18, room.GetComponentsInChildren<MegarachneBossWeb>().Length);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, room.GetComponentsInChildren<MegarachneBossWeb>().Length);
        }

        [UnityTest]
        public IEnumerator RedHaceUnoEInmovilizaUnSegundoSinBloquearAtaques()
        {
            var player = MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            Assert.IsTrue(boss.ForceAttack(MegarachneBoss.AttackKind.Net));
            yield return Until(() => player.MovementLocked, 2f);
            float trappedAt = Time.time;
            Assert.AreEqual(99, actor.GetComponent<Health>().Current);
            Assert.IsTrue(player.CanAct);
            Assert.IsFalse(player.CanDodge);
            Vector2 point = actor.transform.position;
            player.SetInput(Vector2.right, Vector2.up);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(player.MovementLocked);
            Assert.Less(Vector2.Distance(point, actor.transform.position), 0.01f);
            Assert.AreEqual(Vector2.up, player.AimDirection);
            yield return Until(() => !player.MovementLocked, 0.4f);
            Assert.That(Time.time - trappedAt, Is.InRange(0.95f, 1.1f));
            yield return new WaitForSeconds(0.1f);
            Assert.Greater(Vector2.Distance(point, actor.transform.position), 0.1f);
            Assert.AreEqual(99, actor.GetComponent<Health>().Current);
        }

        [UnityTest]
        public IEnumerator RedFijaSuCentroYNoAtrapaFuera()
        {
            var player = MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            boss.ForceAttack(MegarachneBoss.AttackKind.Net);
            yield return new WaitForSeconds(0.9f);
            Vector2 locked = boss.AttackPoint;
            actor.transform.position = locked + Vector2.right * 3f;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.9f);
            Assert.AreEqual(locked, boss.AttackPoint);
            Assert.IsFalse(player.MovementLocked);
            Assert.AreEqual(100, actor.GetComponent<Health>().Current);
        }

        [UnityTest]
        public IEnumerator DesactivarLiberaRedYLimpiaEfectos()
        {
            var player = MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            var constraints = actor.GetComponent<Rigidbody2D>().constraints;
            boss.CreateWeb(actor.transform.position);
            boss.ForceAttack(MegarachneBoss.AttackKind.Net);
            yield return Until(() => player.MovementLocked, 2f);
            boss.enabled = false;
            yield return null;
            Assert.IsFalse(player.MovementLocked);
            Assert.AreEqual(constraints, actor.GetComponent<Rigidbody2D>().constraints);
            Assert.AreEqual(0, room.GetComponentsInChildren<MegarachneBossWeb>().Length);
        }

        [UnityTest]
        public IEnumerator CicloAlternaSaltoRafagaRedCadaCuatroSegundos()
        {
            MakePlayer((Vector2)room.transform.position + new Vector2(-6f, -5f));
            boss.AutomaticAttacks = true;
            float previous = Time.time;
            var expected = new[] { MegarachneBoss.AttackKind.Jump, MegarachneBoss.AttackKind.Silk,
                MegarachneBoss.AttackKind.Net, MegarachneBoss.AttackKind.Jump };
            for (int i = 0; i < expected.Length; i++)
            {
                int count = i + 1;
                yield return Until(() => boss.AttacksStarted == count, 4.2f);
                Assert.AreEqual(expected[i], boss.LastAttack);
                Assert.That(Time.time - previous, Is.InRange(3.8f, 4.15f));
                previous = Time.time;
            }
        }

        SpriteRenderer[] CableRoots() => boss.Canopy.GetComponentsInChildren<SpriteRenderer>()
            .Where(renderer => renderer.name.StartsWith("Nido_CableRoot_"))
            .OrderBy(renderer => renderer.name).ToArray();

        [Test]
        public void CincoArranquesConSpriteSobreLaTelarana()
        {
            var roots = CableRoots();
            Assert.AreEqual(5, roots.Length);
            for (int i = 0; i < roots.Length; i++)
            {
                Assert.AreEqual("Nido_CableRoot_" + i, roots[i].name);
                Assert.IsNotNull(roots[i].sprite);
                Assert.AreEqual(roots[i].name, roots[i].sprite.name);
                Assert.AreEqual(22, roots[i].sortingOrder);
            }
        }

        [Test]
        public void MaterialDelCableRepiteCadaCuatroUnidades()
        {
            var cable = boss.Canopy.transform.Find("Cable_0").GetComponent<LineRenderer>();
            Assert.AreEqual(new Vector2(0.25f, 1f), cable.sharedMaterial.mainTextureScale);
        }

        [Test]
        public void HiloUsaMaterialFinoPropio()
        {
            var cable = boss.Canopy.transform.Find("Cable_0").GetComponent<LineRenderer>();
            var thread = boss.Canopy.transform.Find("HiloColgante").GetComponent<LineRenderer>();
            Assert.AreNotSame(cable.sharedMaterial, thread.sharedMaterial);
            Assert.AreEqual("Cable_Thin", thread.sharedMaterial.mainTexture.name);
            Assert.AreEqual(23, thread.sortingOrder);
        }

        [Test]
        public void ArranquesSiguenElAlfaDeLaCopa()
        {
            actor = new GameObject("EnemigoBajoCopa");
            actor.AddComponent<Enemy>();
            actor.AddComponent<CircleCollider2D>();
            actor.transform.position = (Vector2)room.transform.position + Vector2.up * 4f;
            Physics2D.SyncTransforms();

            var roots = CableRoots();
            Assert.AreEqual(5, roots.Length);
            boss.Canopy.Refresh();
            Assert.AreEqual(MegarachneBossCanopy.CoveredAlpha, boss.Canopy.Alpha);
            foreach (var root in roots) Assert.AreEqual(boss.Canopy.Alpha, root.color.a);

            actor.SetActive(false);
            Physics2D.SyncTransforms();
            boss.Canopy.Refresh();
            Assert.AreEqual(1f, boss.Canopy.Alpha);
            foreach (var root in roots) Assert.AreEqual(boss.Canopy.Alpha, root.color.a);
        }

        [Test]
        public void OcultarArranqueSoloApagaElCableElegido()
        {
            var roots = CableRoots();
            Assert.AreEqual(5, roots.Length);
            boss.SetCableRootVisible(2, false);
            for (int i = 0; i < roots.Length; i++)
                Assert.AreEqual(i != 2, roots[i].enabled);

            boss.SetCableRootVisible(2, true);
            Assert.IsTrue(roots[2].enabled);
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
        public IEnumerator PermaneceEnCablesSinBajadaAutomaticaYCorreASiete()
        {
            Vector2 previous = boss.GetComponent<Rigidbody2D>().position;
            float distance = 0f;
            float end = Time.time + 7f;
            while (Time.time < end)
            {
                yield return new WaitForFixedUpdate();
                Vector2 point = boss.GetComponent<Rigidbody2D>().position;
                float step = Vector2.Distance(previous, point);
                Assert.LessOrEqual(step, MegarachneBoss.CableSpeed * Time.fixedDeltaTime + 0.01f);
                distance += step;
                previous = point;
                Assert.AreEqual(MegarachneBoss.Mode.OnCable, boss.CurrentMode);
                Assert.IsFalse(boss.Health.CanTakeDamage);
            }
            Assert.Greater(distance, 40f);
        }
    }
}
#endif
