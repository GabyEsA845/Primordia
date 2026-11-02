using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>La pintura del lodo sobre el cuerpo (piso 3): hunde, cubre y respeta el contorno y los ojos.</summary>
    public class MudBodyPaintTests
    {
        static readonly Color32 Body = new(200, 160, 40, 255);
        static readonly Color32 Outline = new(24, 20, 37, 255);
        static readonly Color32 Eye = new(244, 244, 232, 255);

        // Un cuerpo de 8×8: bloque de 4×6 con contorno abajo (fila 0), un ojo arriba (fila 5) y el cuerpo entre medias.
        static MudBodyPaint.Data Sprite8()
        {
            var data = new MudBodyPaint.Data { width = 8, height = 8, pixels = new Color32[64], minRow = 0, maxRow = 5, lo = 100f, hi = 180f };
            for (int y = 0; y <= 5; y++)
            {
                for (int x = 2; x <= 5; x++)
                    data.pixels[y * 8 + x] = y == 0 ? Outline : y == 5 && x == 3 ? Eye : Body;
            }
            return data;
        }

        static MudBodyPaint.Tables Tables()
        {
            var noise = new Color32[32 * 32];
            for (int i = 0; i < noise.Length; i++)
                noise[i] = new Color32(128, 128, 128, 255);
            var texture = new int[16 * 16];
            for (int i = 0; i < texture.Length; i++)
                texture[i] = 2;
            return new MudBodyPaint.Tables { noise = noise, noiseSize = 32, texIndex = texture, texSize = 16 };
        }

        static bool SameColor(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        static Color32[] Run(MudBodyPaint.Settings settings)
        {
            var result = new Color32[64];
            MudBodyPaint.Paint(Sprite8(), result, settings, Tables());
            return result;
        }

        [Test]
        public void NoLevel_LeavesTheBodyAsItWas()
        {
            var result = Run(new MudBodyPaint.Settings());
            var source = Sprite8();
            for (int i = 0; i < 64; i++)
                Assert.IsTrue(SameColor(source.pixels[i], result[i]), $"El píxel {i} cambió.");
        }

        [Test]
        public void Sinking_MovesTheBodyDown_AndHidesWhatIsBelowTheSurface()
        {
            var result = Run(new MudBodyPaint.Settings { sunk = 2 });
            // Las dos filas de abajo (0 y 1) se pierden; la fila 2 del cuerpo pasa a la 0.
            Assert.IsTrue(SameColor(result[0 * 8 + 3], Body), "La fila 2 baja a la 0.");
            // La fila 5 (el ojo) baja a la 3.
            Assert.IsTrue(SameColor(result[3 * 8 + 3], Eye));
            // Arriba ya no queda nada.
            Assert.AreEqual(0, result[5 * 8 + 3].a);
            Assert.AreEqual(0, result[4 * 8 + 3].a);
        }

        [Test]
        public void Mud_CoversTheBody_ButNotTheOutlineOrTheEyes()
        {
            var result = Run(new MudBodyPaint.Settings { level = 20f });
            Assert.IsTrue(SameColor(result[0 * 8 + 3], Outline), "El contorno se queda.");
            Assert.IsTrue(SameColor(result[5 * 8 + 3], Eye), "El blanco del ojo asoma del lodo.");
            Assert.IsFalse(SameColor(result[2 * 8 + 3], Body), "El cuerpo se cubre.");
            Assert.AreEqual(255, result[2 * 8 + 3].a);
            Assert.AreEqual(0, result[2 * 8 + 0].a, "Fuera de la silueta no se pinta.");
        }

        [Test]
        public void Mud_OnlyReachesItsLevel()
        {
            // Nivel 2,5 píxeles sobre los pies (fila 0): el ruido gris deja el borde cerca de 2,5 ± 1,6.
            var result = Run(new MudBodyPaint.Settings { level = 2.5f });
            Assert.IsFalse(SameColor(result[1 * 8 + 3], Body), "Cerca de los pies, cubierto.");
            Assert.IsTrue(SameColor(result[4 * 8 + 3], Body), "Bien por encima del lodo, el cuerpo sigue entero.");
        }

        [Test]
        public void Stains_AppearAtStartAndVanishWhenClean()
        {
            var stained = Run(new MudBodyPaint.Settings { stain = 20f, clean = 0.1f });
            var clean = Run(new MudBodyPaint.Settings { stain = 20f, clean = 1f });
            int dirty = 0, same = 0;
            for (int y = 1; y <= 4; y++)
            {
                if (!SameColor(stained[y * 8 + 3], Body))
                    dirty++;
                if (SameColor(clean[y * 8 + 3], Body))
                    same++;
            }
            Assert.Greater(dirty, 0, "Recién salido, quedan manchas.");
            Assert.AreEqual(4, same, "Del todo limpio, el cuerpo vuelve a su color.");
        }

        [Test]
        public void Enemies_GetTheSimpleMud_WithoutStains()
        {
            var covered = Run(new MudBodyPaint.Settings { level = 20f, simple = true });
            Assert.IsFalse(SameColor(covered[2 * 8 + 3], Body));
            var stained = Run(new MudBodyPaint.Settings { stain = 20f, clean = 0f, simple = true });
            Assert.IsTrue(SameColor(stained[2 * 8 + 3], Body), "Los enemigos no dejan manchas.");
        }
    }
}
