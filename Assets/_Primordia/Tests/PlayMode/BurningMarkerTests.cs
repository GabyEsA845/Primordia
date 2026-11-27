using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Marca de «arde» del piso 3 (llamas por tamaño y velo naranja) e iconos de las barras de estado.</summary>
    public class BurningMarkerTests
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
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        [UnityTest]
        public IEnumerator BurningMarker_TintsTheBody_AndCleansUpWhenItGoes()
        {
            var art = PolishArt.Current;
            Assume.That(art != null && art.bodyFlame.m.Length > 0, "Sin importar el pulido no hay llamas nuevas.");

            var texture = new Texture2D(32, 32);
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f));
            var owner = Track(new GameObject("Burner"));
            var body = new GameObject("Body");
            body.transform.SetParent(owner.transform, false);
            body.AddComponent<SpriteRenderer>().sprite = sprite;
            owner.AddComponent<Health>();
            var prefab = Track(new GameObject("BurningPrefab"));
            prefab.AddComponent<BurningMarker>();

            StatusMarkers.Show(owner.GetComponent<Health>(), "Burn", prefab, 0f, overhead: false);
            yield return null;
            yield return null;

            Assert.IsNotNull(body.transform.Find("BurnTint"), "Cada sprite del cuerpo lleva su velo naranja.");
            var markers = owner.GetComponent<StatusMarkers>();
            Assert.IsTrue(markers.IsMarker(markers.EffectOf("Burn").transform));
            Assert.IsFalse(markers.IsMarker(body.transform));

            StatusMarkers.Hide(owner.GetComponent<Health>(), "Burn");
            yield return null;

            Assert.IsNull(body.transform.Find("BurnTint"), "Al apagarse, el velo desaparece con la marca.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BodyPoints_FallOnlyOnTheDrawing(bool unreadable)
        {
            if (unreadable)
                Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Sin gráficos no se puede copiar la textura.");

            // Solo está dibujado el cuadrante de abajo a la izquierda.
            var texture = Track(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            var pixels = new Color32[32 * 32];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                    pixels[y * 32 + x] = new Color32(255, 255, 255, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, unreadable);
            var sprite = Track(Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect));

            var points = SpriteBodyPoints.For(sprite);

            Assert.IsNotEmpty(points);
            foreach (var point in points)
                Assert.IsTrue(point.x < 0f && point.x > -1f && point.y < 0f && point.y > -1f, $"Fuera del dibujo: {point}");
        }

        [Test]
        public void BurnIcon_ShowsAndHidesWithTheBar_WithOrWithoutArt()
        {
            var target = Track(new GameObject("Target"));
            foreach (var shape in new[] { BurnIcon.Shape.Flame, BurnIcon.Shape.Cloud })
            {
                var icon = BurnIcon.Create(target.transform, shape, Color.white, Vector3.up);
                created.Add(icon.gameObject);
                icon.Set(0.5f, true, false);
                Assert.IsTrue(icon.gameObject.activeSelf);
                icon.Set(0f, false, false);
                Assert.IsFalse(icon.gameObject.activeSelf);
            }
        }
    }
}
