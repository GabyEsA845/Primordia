#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Meganeura, paso 7a: hojas con direcciones, sala, presentación (sombra, posarse, abrir las alas) y variantes de alas.</summary>
    public class MeganeuraBossTests
    {
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Meganeura.prefab";
        const string ArtPath = "Assets/_Primordia/Data/Bosses/MeganeuraArt.asset";

        GameObject room;
        MeganeuraBoss boss;

        [SetUp]
        public void SetUp()
        {
            room = new GameObject("Room_Boss_Meganeura");
            room.transform.position = new Vector3(100f, 100f, 0f);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "falta el prefab de la Meganeura (Primordia > Boss > Crear Meganeura y escena de prueba)");
            // Sin presentación: el prefab lleva BossIntro y esperaría al GameManager.
            var instance = Object.Instantiate(prefab, room.transform);
            Object.DestroyImmediate(instance.GetComponent<BossIntro>());
            boss = instance.GetComponent<MeganeuraBoss>();
            boss.SetArena(room.transform, new Vector2(42f, 28f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(room);
        }

        [Test]
        public void LasFilasDeDireccionYElFlipCasanConLosAngulos()
        {
            Assert.AreEqual(0, DirectionalArt.RowFor(90f, out bool flip));
            Assert.IsFalse(flip);
            Assert.AreEqual(4, DirectionalArt.RowFor(0f, out flip));
            Assert.IsFalse(flip);
            Assert.AreEqual(8, DirectionalArt.RowFor(-90f, out flip));
            Assert.AreEqual(4, DirectionalArt.RowFor(180f, out flip));
            Assert.IsTrue(flip);
            // 135° = fila de 45° con Flip X.
            Assert.AreEqual(2, DirectionalArt.RowFor(135f, out flip));
            Assert.IsTrue(flip);
            Assert.AreEqual(6, DirectionalArt.RowFor(-135f, out flip));
            Assert.IsTrue(flip);
        }

        [Test]
        public void LasHojasEstanImportadasConSuTamano()
        {
            var art = UnityEditor.AssetDatabase.LoadAssetAtPath<DirectionalArt>(ArtPath);
            Assert.IsNotNull(art);
            var perch = art.Find("Meganeura_Perch");
            Assert.AreEqual(4, perch.frames);
            Assert.AreEqual(9, perch.rows);
            Assert.AreEqual(36, perch.sprites.Length);
            Assert.AreEqual(192f, perch.Get(4, 0).rect.width);
            // Variantes de alas y sombras con los mismos cuadros.
            Assert.IsNotNull(art.Find("Meganeura_Fly_Torn2Burnt"));
            Assert.AreEqual(perch.frames, art.Find("Meganeura_Shadow_Perch").frames);
            // DeathFall es una sola fila y la ninfa tiene celda de 64.
            Assert.AreEqual(1, art.Find("Meganeura_DeathFall").rows);
            Assert.AreEqual(64f, art.Find("Meganeura_Nymph_Idle").Get(0, 0).rect.width);
        }

        [Test]
        public void LasAlasSeRasganConLaVidaYSeQuemanConElMetano()
        {
            var health = boss.GetComponent<Health>();
            Assert.AreEqual("", boss.WingVariant);
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.40f));
            Assert.AreEqual("Torn1", boss.WingVariant);
            health.TakeDamage(Mathf.CeilToInt(health.Max * 0.30f));
            Assert.AreEqual("Torn2", boss.WingVariant);
            boss.Burnt = true;
            Assert.AreEqual("Torn2Burnt", boss.WingVariant);
        }

        [UnityTest]
        public IEnumerator LaPresentacionLaPosaEnElTroncoYAbreLasAlas()
        {
            var body = boss.transform.Find("Body").GetComponent<SpriteRenderer>();
            var shadow = boss.transform.Find("Shadow").GetComponent<SpriteRenderer>();
            boss.PlayIntroPose(2f);
            yield return null;
            // Mientras cruza la sala solo se ve su sombra.
            Assert.IsFalse(body.enabled);
            Assert.IsTrue(shadow.enabled);

            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(body.enabled, "tras cruzar la sala se posa");
            Assert.IsNotNull(body.sprite);
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Land") || body.sprite.name.StartsWith("Meganeura_Intro"), body.sprite.name);

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Intro"), body.sprite.name);

            boss.BeginFight();
            yield return null;
            Assert.IsTrue(body.sprite.name.StartsWith("Meganeura_Perch"), body.sprite.name);
        }

        [Test]
        public void ConLaSalaDesactivadaSeMontaSinFallar()
        {
            // La sala mantiene a sus enemigos desactivados hasta que entra el jugador: SetArena corre antes que el Awake del jefe.
            var other = new GameObject("Room_Hidden");
            other.transform.position = new Vector3(300f, 0f, 0f);
            var holder = new GameObject("Enemies");
            holder.transform.SetParent(other.transform);
            holder.SetActive(false);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var hidden = Object.Instantiate(prefab, holder.transform).GetComponent<MeganeuraBoss>();
            Assert.DoesNotThrow(() => hidden.SetArena(other.transform, new Vector2(42f, 28f)));
            Assert.AreEqual(308f, hidden.transform.position.x, 0.01f);
            holder.SetActive(true);
            Assert.AreEqual(308f, hidden.GetComponent<Rigidbody2D>().position.x, 0.01f);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void LaSalaTieneElTroncoPartidoSolidoYElJefeEmpiezaSobreEl()
        {
            var stump = room.transform.Find("LepidoSplit");
            Assert.IsNotNull(stump);
            Assert.IsFalse(stump.GetComponent<BoxCollider2D>().isTrigger);
            Assert.AreEqual((Vector2)stump.position, (Vector2)boss.transform.position);
        }
    }
}
#endif
