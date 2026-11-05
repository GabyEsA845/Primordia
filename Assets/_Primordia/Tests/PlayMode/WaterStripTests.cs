using NUnit.Framework;
using Primordia.Combat;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>Franja de agua en las patas (agua de charca) y rastro de fuego de los enemigos secos, del piso 3.</summary>
    public class WaterStripTests
    {
        static readonly Color32 Body = new(200, 160, 40, 255);
        static readonly Color32 Outline = new(24, 20, 37, 255);

        // Cuerpo de 8×8: columnas 2-5, filas 0-6; la fila 0 es contorno.
        static MudBodyPaint.Data Sprite8()
        {
            var data = new MudBodyPaint.Data { width = 8, height = 8, pixels = new Color32[64], minRow = 0, maxRow = 6, lo = 100f, hi = 180f };
            for (int y = 0; y <= 6; y++)
                for (int x = 2; x <= 5; x++)
                    data.pixels[y * 8 + x] = y == 0 ? Outline : Body;
            return data;
        }

        static Color32[] Run(bool water)
        {
            var result = new Color32[64];
            MudBodyPaint.Paint(Sprite8(), result, new MudBodyPaint.Settings { water = water, t = 0 },
                new MudBodyPaint.Tables { noise = new Color32[32 * 32], noiseSize = 32, texIndex = new int[256], texSize = 16 });
            return result;
        }

        [Test]
        public void Water_TintsTheBottomFourRows_WithALineOnTop_AndKeepsTheOutline()
        {
            var dry = Run(false);
            var wet = Run(true);
            Assert.AreEqual(Outline, wet[0 * 8 + 3], "El contorno no se tiñe.");
            Assert.AreNotEqual(dry[1 * 8 + 3], wet[1 * 8 + 3], "La fila 1 se ve bajo el agua.");
            Assert.Less(wet[1 * 8 + 3].r, dry[1 * 8 + 3].r, "Y se aleja del color del cuerpo hacia el verde del agua.");
            Assert.AreNotEqual(dry[3 * 8 + 3], wet[3 * 8 + 3], "La fila 3 es la línea de agua.");
            Assert.AreEqual(dry[4 * 8 + 3], wet[4 * 8 + 3], "Por encima de 4 px el cuerpo no cambia.");
        }

        [Test]
        public void FireTrail_OnlyForDryBodies()
        {
            var go = new GameObject("Plain");
            Assert.IsFalse(Burn.LeavesFireTrail(go));
            go.AddComponent<Primordia.Enemies.MedullosaBrain>();
            Assert.IsTrue(Burn.LeavesFireTrail(go));
            Object.DestroyImmediate(go);
        }
    }
}
