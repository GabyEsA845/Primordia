using System.Collections.Generic;
using System.Text;

namespace Primordia.Rooms
{
    /// <summary>
    /// Catálogo de formas de sala, como texto para <see cref="RoomTemplate"/>. El menú del editor crea un asset
    /// por forma en Data/Rooms (sin sobrescribir los que ya existen, que se pueden retocar a mano).
    /// Reglas: pared de 3 filas encima de cualquier suelo que mire hacia arriba, de 1 en los demás lados;
    /// columnas de al menos 2×3; las puertas de arriba y abajo en las mismas columnas, y las de los lados
    /// en las mismas filas, para que los pasillos entre salas sean rectos.
    /// </summary>
    public static class RoomLayouts
    {
        public const string Small = "Room_Small";
        public const string Normal = "Room_Normal";
        public const string Large = "Room_Large";
        public const string Pillars = "Room_Pillars";
        public const string Cross = "Room_Cross";
        public const string LShape = "Room_L";
        public const string BossCircle = "Room_BossCircle";

        /// <summary>Diámetro del suelo de la sala circular de la ameba gigante, en tiles.</summary>
        public const int BossCircleDiameter = 30;

        public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
        {
            // 22×15 (suelo 20×11): pocos enemigos o recompensa. Cabe en pantalla con margen.
            [Small] = Rectangle(22, 15),

            // 28×16 (suelo 26×12): la sala estándar, cabe justo en la vista de 30×16,9.
            [Normal] = Rectangle(28, 16),

            // 40×24 (suelo 38×20): más grande que la pantalla; la cámara sigue al jugador.
            [Large] = Rectangle(40, 24),

            // Sala normal con dos columnas de 2×3 para cubrirse.
            [Pillars] = new Builder(28, 16).Floor(1, 1, 26, 12).Wall(7, 5, 8, 7).Wall(19, 5, 20, 7).CenteredDoors().ToString(),

            // Cruz de 30×22: brazos de 10 de ancho y 7 de alto; puertas al final de cada brazo.
            [Cross] = new Builder(30, 22).Floor(1, 7, 28, 13).Floor(10, 1, 19, 18)
                .Door('^', 14, 21).Door('v', 14, 0).Door('<', 0, 9).Door('>', 29, 9).ToString(),

            // L de 28×20: banda inferior de 26×8 y brazo superior izquierdo de 12×8.
            [LShape] = new Builder(28, 20).Floor(1, 1, 26, 8).Floor(1, 9, 12, 16)
                .Door('^', 6, 19).Door('v', 6, 0).Door('<', 0, 4).Door('>', 27, 4).ToString(),

            // Círculo de 30 tiles de diámetro (32×34 con las paredes): la sala de la ameba gigante, vacía.
            [BossCircle] = Circle(BossCircleDiameter),
        };

        /// <summary>Sala rectangular con un hueco de 2 tiles centrado en cada lado.</summary>
        public static string Rectangle(int width, int height) =>
            new Builder(width, height).Floor(1, 1, width - 2, height - 4).CenteredDoors().ToString();

        /// <summary>
        /// Sala circular con el borde escalonado y una puerta en cada punto cardinal. Pared de 3 filas arriba y de 1 en
        /// los demás lados, como las rectangulares; al ser convexa, las reglas de paredes tienen pieza para cada celda.
        /// </summary>
        public static string Circle(int diameter)
        {
            int width = diameter + 2;
            int height = diameter + 4;
            var builder = new Builder(width, height);
            float center = (diameter + 1) / 2f;
            float radius = diameter / 2f;
            for (int x = 1; x <= diameter; x++)
            {
                for (int y = 1; y <= diameter; y++)
                {
                    float dx = x - center, dy = y - center;
                    if (dx * dx + dy * dy <= radius * radius)
                        builder.Floor(x, y, x, y);
                }
            }
            return builder.CenteredDoors().ToString();
        }

        /// <summary>Monta una plantilla en una rejilla con y hacia arriba (fila 0 = abajo).</summary>
        sealed class Builder
        {
            readonly char[,] cells;
            readonly int width;
            readonly int height;

            public Builder(int width, int height)
            {
                this.width = width;
                this.height = height;
                cells = new char[width, height];
                Fill(0, 0, width - 1, height - 1, '#');
            }

            public Builder Floor(int xMin, int yMin, int xMax, int yMax) => Fill(xMin, yMin, xMax, yMax, '.');

            public Builder Wall(int xMin, int yMin, int xMax, int yMax) => Fill(xMin, yMin, xMax, yMax, '#');

            /// <summary>Hueco de 2 tiles: hacia la derecha para ^ y v, hacia arriba para &lt; y &gt;.</summary>
            public Builder Door(char mark, int x, int y)
            {
                bool horizontal = mark == '^' || mark == 'v';
                cells[x, y] = mark;
                cells[horizontal ? x + 1 : x, horizontal ? y : y + 1] = mark;
                return this;
            }

            /// <summary>Puertas centradas en una sala rectangular (suelo de la fila 1 a la height - 4).</summary>
            public Builder CenteredDoors()
            {
                int doorX = width / 2 - 1;
                int doorY = 1 + (height - 4 - 2) / 2;
                return Door('^', doorX, height - 1).Door('v', doorX, 0).Door('<', 0, doorY).Door('>', width - 1, doorY);
            }

            Builder Fill(int xMin, int yMin, int xMax, int yMax, char c)
            {
                for (int x = xMin; x <= xMax; x++)
                    for (int y = yMin; y <= yMax; y++)
                        cells[x, y] = c;
                return this;
            }

            public override string ToString()
            {
                var text = new StringBuilder();
                for (int y = height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < width; x++)
                        text.Append(cells[x, y]);
                    text.Append('\n');
                }
                return text.ToString();
            }
        }
    }
}
