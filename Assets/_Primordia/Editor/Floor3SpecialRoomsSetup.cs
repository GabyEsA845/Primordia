using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Salas especiales del piso 3, primera tanda (paso 6, bloque 4): Veta de carbón, Bosque en humo, Fumarolas y Ladronas. Importa el
    /// arte (docs/multimedia-temp/floor3-special-rooms) a Resources/SpecialRoomArt, crea sus plantillas, las registra en Floor_3 y monta
    /// la escena de prueba Test_Floor3SpecialRooms, que las recorre todas.
    /// </summary>
    public static class Floor3SpecialRoomsSetup
    {
        const string Source = "docs/multimedia-temp/floor3-special-rooms";
        const string ArtFolder = "Assets/_Primordia/Art/Floor3/SpecialRooms";
        const string AssetPath = "Assets/_Primordia/Resources/SpecialRoomArt.asset";
        const string TemplateFolder = "Assets/_Primordia/Data/Rooms/Floor3";
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Floor3SpecialRooms.unity";
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3_SpecialRooms.asset";

        static readonly string[] Bands = { "Swamp", "Smoke", "Burnt" };
        static readonly string[] CornerNames = { "NE", "NO", "SE", "SO" };

        [MenuItem("Primordia/Floor 3/Crear salas especiales (veta, humo, fumarolas, ladronas)")]
        internal static void CreateAll()
        {
            ImportArt();
            // Contorno de cueva fijo por semilla: la roca entra hasta 4 casillas por los lados y 2 por arriba y abajo (en el bosque, más).
            var veta = CreateTemplate("Room_Special_CoalSeam", 28, 17, Seam, 11, 4, 2);
            // El Bosque en humo es enorme (unas tres salas) para perderse en él.
            var humo = CreateTemplate("Room_Special_SmokeForest", 56, 35, Cover, 22, 8, 5);
            var fumarolas = CreateTemplate("Room_Special_Fumaroles", 28, 17, Vents, 33, 3, 2);
            var ladronas = CreateTemplate("Room_Special_Thieves", 24, 15, Piles, 44, 3, 2);
            // Segunda tanda: Tronco caído, Tesoro hundido y Tocón hueco.
            var tronco = CreateTemplate("Room_Special_FallenLog", 36, 24, Log, 55, 3, 2);
            var tesoro = CreateTemplate("Room_Special_SunkenTreasure", 28, 17, Treasure, 66, 3, 2);
            var tocon = CreateTemplate("Room_Special_HollowStump", 22, 14, Stump, 77, 3, 2);
            // Tercera tanda: Frente de fuego, Burbujeo y Claro de la carnívora.
            var frente = CreateTemplate("Room_Special_FireFront", 40, 24, Open, 88, 4, 3);
            var burbujeo = CreateTemplate("Room_Special_Bubbling", 32, 20, Bubbles, 99, 3, 2);
            var claro = CreateTemplate("Room_Special_PlantClearing", 36, 22, Clearing, 111, 3, 2);
            Register(veta, humo, fumarolas, ladronas, tronco, tesoro, tocon, frente, burbujeo, claro);
        }

        // ---------- Arte ----------

        [MenuItem("Primordia/Floor 3/Importar arte de las salas especiales")]
        internal static void ImportArt()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AssetPath));
            var art = AssetDatabase.LoadAssetAtPath<SpecialRoomArt>(AssetPath);
            bool isNew = art == null;
            if (isNew)
                art = ScriptableObject.CreateInstance<SpecialRoomArt>();

            art.coal = new SpecialRoomArt.CoalBand[SpecialRoomArt.Bands];
            for (int band = 0; band < Bands.Length; band++)
            {
                var tiles = new Sprite[16];
                for (int mask = 0; mask < 16; mask++)
                    tiles[mask] = Single($"coal/{Bands[band]}", $"Coal_{mask}");
                art.coal[band] = new SpecialRoomArt.CoalBand
                {
                    tiles = tiles,
                    corners = CornerNames.Select(name => Single($"coal/{Bands[band]}", $"Coal_Corner_{name}")).ToArray(),
                };
            }

            // Las capas de fuego (16×32) apoyan en su borde de abajo; las brasas (16×16) van centradas.
            var foot = new Vector2(0.5f, 0f);
            art.ignite = Sheet("coal", "Coal_Ignite", 16, foot);
            art.SetBurn(Sheet("coal", "Coal_Burn_0", 16, foot), Sheet("coal", "Coal_Burn_1", 16, foot), Sheet("coal", "Coal_Burn_2", 16, foot));
            art.embers = Sheet("coal", "Coal_Embers", 16, new Vector2(0.5f, 0.5f));
            art.coalOut = Single("coal", "Coal_Out");

            // Burbuja de metano de cada tramo (16×24, pivote abajo), del escenario del piso 3 ya aprobado.
            art.bubble = new SpecialRoomArt.BubbleBand[SpecialRoomArt.Bands];
            for (int band = 0; band < Bands.Length; band++)
            {
                art.bubble[band] = new SpecialRoomArt.BubbleBand
                {
                    frames = ArtSetup.ImportLooseSheet($"docs/multimedia-temp/floor3-environment/{Bands[band]}/props/Methane_Bubble.png",
                        $"{ArtFolder}/bubble/{Bands[band]}/Methane_Bubble.png", 16, new Vector2(0.5f, 0f)),
                };
            }

            art.smokeA = Single("smoke", "Fx_SmokeLayer_A");
            art.smokeB = Single("smoke", "Fx_SmokeLayer_B");
            // El humo es una malla con la textura repetida: la hoja tiene que envolver y el material (con color por vértice) se guarda con el arte.
            WrapRepeat("smoke", "Fx_SmokeLayer_A");
            WrapRepeat("smoke", "Fx_SmokeLayer_B");
            art.smokeMaterial = SmokeMaterial();
            art.wisp = Sheet("smoke", "Fx_SmokeWisp", 48, new Vector2(0.5f, 0.5f));
            art.floatingEmbers = Sheet("smoke", "Fx_FloatingEmbers", 32, new Vector2(0.5f, 0.5f));

            // Tronco caído, rayo de luz del Tocón hueco y cofres de cono (arte de floor3-log-stump y de floor3-environment).
            ArtSetup.ImportLogStumpArt(art);

            if (isNew)
                AssetDatabase.CreateAsset(art, AssetPath);
            else
                EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3SpecialRoomsSetup: arte importado (veta: {art.coal.Length} tramos, {art.ignite.Length}+{art.embers.Length} cuadros de fuego; humo: {art.wisp.Length} volutas, {art.floatingEmbers.Length} ascuas).");
        }

        static void WrapRepeat(string folder, string name)
        {
            string path = $"{ArtFolder}/{folder}/{name}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        static Material SmokeMaterial()
        {
            string path = $"{ArtFolder}/SmokeMesh.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        static Sprite Single(string folder, string name) =>
            ArtSetup.ImportLooseSprite($"{Source}/{folder}/{name}.png", $"{ArtFolder}/{folder}/{name}.png");

        static Sprite[] Sheet(string folder, string name, int cell, Vector2 pivot) =>
            ArtSetup.ImportLooseSheet($"{Source}/{folder}/{name}.png", $"{ArtFolder}/{folder}/{name}.png", cell, pivot);

        // ---------- Plantillas ----------

        /// <summary>
        /// Sala rectangular con una puerta en cada lado y lo que ponga <paramref name="decorate"/> sobre ella. Se trabaja con filas de
        /// texto: la fila 0 es la de arriba; el suelo va de la fila 3 a la penúltima y de la columna 1 a la penúltima.
        /// </summary>
        static RoomTemplate CreateTemplate(string name, int width, int height, System.Action<char[][], int, int> decorate, int seed, int maxSide, int maxEnd)
        {
            string path = $"{TemplateFolder}/{name}.asset";
            var rows = RoomLayouts.Rectangle(width, height).TrimEnd('\n').Split('\n').Select(row => row.ToCharArray()).ToArray();
            decorate(rows, width, height);
            rows = Irregular(rows, width, height, seed, maxSide, maxEnd);

            var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
            bool isNew = template == null;
            if (isNew)
                template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.SetLayout(string.Join("\n", rows.Select(row => new string(row))) + "\n");
            if (isNew)
                AssetDatabase.CreateAsset(template, path);
            else
                EditorUtility.SetDirty(template);

            var problems = template.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"Floor3SpecialRoomsSetup: {name} tiene problemas:\n{string.Join("\n", problems)}");
            return template;
        }

        // ---------- Contorno irregular ----------

        /// <summary>
        /// Deforma el contorno de la sala como una cueva: por cada lado la roca entra de 0 a <paramref name="maxSide"/> casillas (a los lados;
        /// <paramref name="maxEnd"/> arriba y abajo) con un perfil de tramos de 3 a 7 casillas (sin picos de una casilla). No toca las
        /// puertas (cuellos de 8 casillas) ni lo que ya hay colocado (con una casilla de margen). Si el resultado no se puede dibujar, entra
        /// menos hasta que sí.
        /// </summary>
        static char[][] Irregular(char[][] source, int width, int height, int seed, int maxSide, int maxEnd)
        {
            var protect = new bool[height, width];
            for (int r = 0; r < height; r++)
            {
                for (int x = 0; x < width; x++)
                {
                    char ch = source[r][x];
                    if (ch == '.' || ch == '#')
                        continue;
                    bool door = ch is '^' or 'v' or '<' or '>';
                    int reach = door ? 8 : 1;
                    for (int dr = -reach; dr <= reach; dr++)
                    {
                        for (int dx = -reach; dx <= reach; dx++)
                        {
                            int rr = r + dr, xx = x + dx;
                            if (rr < 0 || rr >= height || xx < 0 || xx >= width)
                                continue;
                            // El cuello de una puerta es un pasillo, no un cuadrado: solo se protege su ancho y su fondo.
                            if (door && (((ch is '^') || (ch is 'v')) ? Mathf.Abs(dx) > 3 : Mathf.Abs(dr) > 3))
                                continue;
                            protect[rr, xx] = true;
                        }
                    }
                }
            }

            for (int attempt = 0; attempt < 6; attempt++)
            {
                var random = new System.Random(seed + attempt * 101);
                int side = Mathf.Max(0, maxSide - attempt), end = Mathf.Max(0, maxEnd - attempt / 2);
                var top = Profile(random, width, end);
                var bottom = Profile(random, width, end);
                var left = Profile(random, height, side);
                var right = Profile(random, height, side);

                var rows = source.Select(row => (char[])row.Clone()).ToArray();
                for (int r = 3; r <= height - 2; r++)
                {
                    for (int x = 1; x <= width - 2; x++)
                    {
                        if (rows[r][x] != '.' || protect[r, x])
                            continue;
                        bool carved = r < 3 + top[x] || r > height - 2 - bottom[x] || x < 1 + left[r] || x > width - 2 - right[r];
                        if (carved)
                            rows[r][x] = '#';
                    }
                }

                if (IsDrawable(rows))
                    return rows;
            }
            return source;
        }

        // Perfil de profundidades en tramos de 3 a 7 casillas, que cambian de 0 a 2 casillas entre un tramo y el siguiente.
        static int[] Profile(System.Random random, int length, int max)
        {
            var profile = new int[length];
            int depth = max > 0 ? random.Next(max + 1) : 0;
            int x = 0;
            while (x < length)
            {
                int run = random.Next(3, 8);
                for (int i = 0; i < run && x < length; i++, x++)
                    profile[x] = depth;
                depth = Mathf.Clamp(depth + random.Next(-2, 3), 0, max);
            }
            return profile;
        }

        static bool IsDrawable(char[][] rows)
        {
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.SetLayout(string.Join("\n", rows.Select(row => new string(row))) + "\n");
            bool ok = template.Validate().Count == 0;
            Object.DestroyImmediate(template);
            return ok;
        }

        // Fila del primer hueco de puerta lateral: las puertas de los lados ocupan esa fila y la siguiente.
        static int SideDoorRow(char[][] rows)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r][0] == '<')
                    return r;
            }
            return rows.Length / 2;
        }

        static void Put(char[][] rows, int x, int row, char c)
        {
            if (rows[row][x] == '.')
                rows[row][x] = c;
        }

        // Veta de carbón: una franja irregular de pared a pared (grosor de 2 a 3 casillas que sube y baja y con ramales), por encima de
        // las puertas de los lados: parte la sala en dos; la puerta de arriba queda a un lado y las de los lados y la de abajo al otro.
        // Tres racimos de veta encendida (e) debajo de ella: sus chispas acaban prendiéndola. Unas rocas de cobertura.
        static void Seam(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int top = door - 4;
            var random = new System.Random(7);
            int center = top;
            int last = top;
            for (int x = 1; x < width - 1; x++)
            {
                // El centro de la franja sube o baja una casilla de vez en cuando; el grosor, 2 o 3.
                int step = random.Next(5);
                if (step == 0 && center > top - 1)
                    center--;
                else if (step == 1 && center < top + 1)
                    center++;
                int thickness = random.Next(10) < 3 ? 3 : 2;
                for (int r = center; r < center + thickness; r++)
                    Put(rows, x, r, 'c');
                // Ramales: salientes de una o dos casillas hacia arriba o hacia abajo.
                if (x % 7 == 3)
                {
                    bool up = random.Next(2) == 0;
                    int length = random.Next(1, 3);
                    for (int i = 1; i <= length; i++)
                        Put(rows, x, up ? center - i : center + thickness - 1 + i, 'c');
                }
                last = center;
            }

            // Racimos encendidos por debajo (el lado de las puertas), a unas 2 casillas de la franja.
            int[][] clusters = { new[] { 5, 2 }, new[] { 13, 3 }, new[] { 22, 2 } };
            foreach (var cluster in clusters)
                Ember(rows, cluster[0], top + cluster[1] + 3, cluster[0] % 2);

            int[] coverX = { 8, 19, 7, 20 };
            int[] coverRow = { top - 2, top - 2, top + 8, top + 8 };
            for (int i = 0; i < coverX.Length; i++)
                Put(rows, coverX[i], coverRow[i], 'o');
        }

        // Un racimo pequeño e irregular de veta encendida: tres o cuatro casillas según la forma.
        static void Ember(char[][] rows, int x, int row, int shape)
        {
            int[][][] shapes =
            {
                new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 } },
                new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 2, 0 }, new[] { 1, 1 } },
            };
            foreach (var offset in shapes[shape % shapes.Length])
                Put(rows, x + offset[0], row + offset[1], 'e');
        }

        // Bosque en humo: sala enorme con rocas sueltas y quince racimos de veta encendida bien repartidos (una cuadrícula de 5×3 con
        // desplazamientos al azar), de modo que se pasa de un resplandor al siguiente por el humo y se siente uno perdido.
        static void Cover(char[][] rows, int width, int height)
        {
            var random = new System.Random(5);
            int door = SideDoorRow(rows);
            for (int i = 0; i < 16; i++)
                Put(rows, random.Next(4, width - 4), random.Next(5, height - 4), 'o');

            int[] xs = { 8, 18, 28, 38, 48 };
            int[] ys = { door - 12, door, door + 12 };
            int shape = 0;
            for (int gy = 0; gy < ys.Length; gy++)
            {
                for (int gx = 0; gx < xs.Length; gx++)
                {
                    // Las del medio de la fila del medio se saltan una de cada dos para dejar sitio a las puertas de los lados.
                    int x = xs[gx] + random.Next(-3, 4) + (gy % 2) * 4;
                    int row = ys[gy] + random.Next(-2, 3);
                    Ember(rows, Mathf.Clamp(x, 4, width - 6), Mathf.Clamp(row, 5, height - 5), shape++);
                }
            }
        }

        // Fumarolas: cuatro filas de cinco fumarolas (20) que sueltan el chorro por turnos, una fila tras otra. Las filas de arriba y
        // de abajo van desplazadas respecto a las otras dos para que los huecos no se alineen.
        static void Vents(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int[] rowsAt = { door - 5, door - 2, door + 3, door + 6 };
            int[][] columns = { new[] { 6, 10, 14, 18, 22 }, new[] { 8, 12, 16, 20, 24 }, new[] { 6, 10, 14, 18, 22 }, new[] { 8, 12, 16, 20, 24 } };
            for (int i = 0; i < rowsAt.Length; i++)
            {
                foreach (int x in columns[i])
                    Put(rows, x, rowsAt[i], 'V');
            }
        }

        // Tronco caído: un gran lago de lodo profundo (borde irregular) que ocupa el centro de la sala y deja un reborde de suelo; un tronco de
        // 2 casillas cruza el lago de lado a lado, a la altura de las puertas de los lados, y es el paso seguro. Unas rocas en el reborde.
        static void Log(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            float cx = (width - 1) / 2f, cy = door + 0.5f;
            float rx = width * 0.3f, ry = (height - 5) * 0.32f;
            for (int r = 3; r <= height - 2; r++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float nx = (x - cx) / rx, ny = (r - cy) / ry;
                    float angle = Mathf.Atan2(ny, nx);
                    // Borde con ondulaciones para que no sea un óvalo perfecto.
                    float edge = 1f + 0.14f * Mathf.Sin(3f * angle + 0.6f) + 0.09f * Mathf.Sin(5f * angle + 2f);
                    if (nx * nx + ny * ny <= edge * edge)
                        Put(rows, x, r, 'm');
                }
            }
            // El tronco: las dos filas del lago que cruza, de pared a pared, son casillas T (se pintan de lodo, pero se andan sin hundirse).
            for (int x = 1; x < width - 1; x++)
            {
                foreach (int r in new[] { door, door + 1 })
                {
                    if (rows[r][x] == 'm')
                        rows[r][x] = 'T';
                }
            }
            int[][] rocks = { new[] { 5, 5 }, new[] { width - 6, 5 }, new[] { 5, height - 5 }, new[] { width - 6, height - 5 }, new[] { cx > 0 ? (int)cx : 10, 4 } };
            foreach (var rock in rocks)
                Put(rows, rock[0], rock[1], 'o');
        }

        // Tesoro hundido: un lago de lodo profundo y once montones de biomasa (M) a distintas distancias de la orilla; con esquivas se
        // llega a ellos sin hundirse. El del centro, el más hundido, es el grande.
        static void Treasure(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            float cx = (width - 1) / 2f, cy = door + 0.5f;
            float rx = width * 0.36f, ry = (height - 5) * 0.4f;
            for (int r = 3; r <= height - 2; r++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float nx = (x - cx) / rx, ny = (r - cy) / ry;
                    float angle = Mathf.Atan2(ny, nx);
                    float edge = 1f + 0.12f * Mathf.Sin(4f * angle + 1f);
                    if (nx * nx + ny * ny <= edge * edge)
                        Put(rows, x, r, 'm');
                }
            }
            int[][] offsets = { new[] { -9, 0 }, new[] { -6, -3 }, new[] { -6, 3 }, new[] { -3, 0 }, new[] { 0, -4 }, new[] { 0, 4 }, new[] { 3, 0 },
                new[] { 6, -3 }, new[] { 6, 3 }, new[] { 9, 0 }, new[] { 0, 0 } };
            foreach (var offset in offsets)
            {
                int x = Mathf.RoundToInt(cx) + offset[0], r = door + offset[1];
                if (rows[r][x] == 'm')
                    rows[r][x] = 'M';
            }
        }

        // Tocón hueco: sala pequeña con el cofre de cono grande (CC) en el centro y unas rocas alrededor. Se entra quemando la maleza.
        static void Stump(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int cx = width / 2;
            rows[door][cx - 1] = 'C';
            rows[door][cx] = 'C';
            int[][] rocks = { new[] { cx - 4, door - 2 }, new[] { cx + 4, door - 2 }, new[] { cx - 4, door + 3 }, new[] { cx + 4, door + 3 }, new[] { cx, door - 3 } };
            foreach (var rock in rocks)
                Put(rows, rock[0], rock[1], 'o');
        }

        // Frente de fuego: sala amplia y despejada (los helechos, muchísimos, los pone el generador; el fuego, el controlador).
        static void Open(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int[][] rocks = { new[] { 9, door - 5 }, new[] { width - 10, door - 5 }, new[] { 9, door + 6 }, new[] { width - 10, door + 6 } };
            foreach (var rock in rocks)
                Put(rows, rock[0], rock[1], 'o');
        }

        // Burbujeo: diez grietas de burbujas de metano repartidas en tres filas desplazadas.
        static void Bubbles(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int[][] bubblers =
            {
                new[] { 7, door - 5 }, new[] { 15, door - 5 }, new[] { 23, door - 5 },
                new[] { 4, door - 2 }, new[] { 11, door - 2 }, new[] { 19, door - 2 }, new[] { 27, door - 2 },
                new[] { 7, door + 3 }, new[] { 15, door + 3 }, new[] { 23, door + 3 },
            };
            foreach (var bubbler in bubblers)
                Put(rows, bubbler[0], bubbler[1], 'b');
        }

        // Claro de la carnívora: un claro en el centro rodeado por un anillo de seis carnívoras (Y), con unas rocas fuera del anillo.
        static void Clearing(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            float cx = (width - 1) / 2f, cy = door + 0.5f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + Mathf.PI / 6f;
                int x = Mathf.RoundToInt(cx + Mathf.Cos(angle) * 6f);
                int row = Mathf.RoundToInt(cy + Mathf.Sin(angle) * 4.5f);
                Put(rows, x, row, 'Y');
            }
            int[][] rocks = { new[] { 6, door - 4 }, new[] { width - 7, door - 4 }, new[] { 6, door + 5 }, new[] { width - 7, door + 5 } };
            foreach (var rock in rocks)
                Put(rows, rock[0], rock[1], 'o');
        }

        // Ladronas: seis montones de biomasa repartidos; el centro queda libre para la recompensa.
        static void Piles(char[][] rows, int width, int height)
        {
            int door = SideDoorRow(rows);
            int[][] piles = { new[] { 4, door - 4 }, new[] { 19, door - 4 }, new[] { 4, door + 4 }, new[] { 19, door + 4 }, new[] { 8, door }, new[] { 15, door } };
            foreach (var pile in piles)
                Put(rows, pile[0], pile[1], 'P');
        }

        // ---------- Registro en Floor_3 ----------

        static FloorConfig.SpecialRoom Entry(RoomTemplate template, bool combat, bool smoke = false, bool thieves = false, int minBand = 0) => new()
        {
            template = template,
            placement = combat ? FloorConfig.SpecialPlacement.Path : FloorConfig.SpecialPlacement.Side,
            countsAsCombat = combat,
            enemyMultiplier = combat ? 1f : 0f,
            band = -1,
            smoke = smoke,
            thieves = thieves,
            minBand = minBand,
        };

        static FloorConfig.SpecialRoom WithEnemies(FloorConfig.SpecialRoom room, float multiplier)
        {
            room.enemyMultiplier = multiplier;
            return room;
        }

        static void Register(RoomTemplate veta, RoomTemplate humo, RoomTemplate fumarolas, RoomTemplate ladronas, RoomTemplate tronco, RoomTemplate tesoro,
            RoomTemplate tocon, RoomTemplate frente, RoomTemplate burbujeo, RoomTemplate claro)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (config == null)
            {
                Debug.LogError("Floor3SpecialRoomsSetup: falta Floor_3 (Primordia > Floor 3 > ...).");
                return;
            }

            var mine = new[] { veta, humo, fumarolas, ladronas, tronco, tesoro, tocon, frente, burbujeo, claro };
            // Se conservan las demás especiales (la del ámbar) y se sustituyen estas cuatro si ya estaban.
            var rooms = config.SpecialRooms.Where(room => !mine.Contains(room.template)).ToList();
            rooms.Add(Entry(veta, true));
            // El Bosque en humo solo sale de la segunda franja en adelante.
            // Es unas tres veces más grande que una sala normal: el doble de enemigos.
            rooms.Add(WithEnemies(Entry(humo, true, smoke: true, minBand: 1), 2f));
            rooms.Add(Entry(fumarolas, true));
            rooms.Add(Entry(ladronas, false, thieves: true));

            // Tronco caído: sala de combate con un par de libélulas de más, que sobrevuelan el lodo.
            var dragonfly = config.Enemies.Select(entry => entry.prefab).FirstOrDefault(prefab => prefab != null && prefab.name.Contains("Dragonfly"));
            var logRoom = Entry(tronco, true);
            logRoom.bonusEnemy = dragonfly;
            logRoom.bonusCount = dragonfly != null ? 2 : 0;
            rooms.Add(logRoom);
            // Tesoro hundido: lateral sin enemigos. Tocón hueco: lateral oculta tras maleza, con el cofre de cono.
            rooms.Add(Entry(tesoro, false));
            var stumpRoom = Entry(tocon, false);
            stumpRoom.hidden = true;
            stumpRoom.coneChest = true;
            // La sala es el interior de un tocón gigante: tileset propio, igual en todos los tramos.
            stumpRoom.theme = ArtSetup.ImportStumpTheme();
            rooms.Add(stumpRoom);

            // Frente de fuego: helechos ×3 y focos al empezar; solo en las dos primeras franjas (en la tercera los helechos ya están quemados).
            var fireRoom = WithEnemies(Entry(frente, true), 1.2f);
            fireRoom.fireFront = true;
            fireRoom.fernScale = 3f;
            fireRoom.bandsBelow = 2;
            rooms.Add(fireRoom);
            rooms.Add(Entry(burbujeo, true));
            rooms.Add(WithEnemies(Entry(claro, true), 1.3f));
            // De las diez salas especiales salen 3-5 por partida (la del ámbar, que sale siempre, cuenta).
            config.ConfigureSpecialRooms(rooms.ToArray(), new Vector2Int(3, 5));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3SpecialRoomsSetup: salas especiales creadas y registradas en Floor_3 ({rooms.Count} especiales en total).");
        }

        // ---------- Escena de prueba ----------

        [MenuItem("Primordia/Floor 3/Crear escena de prueba de las salas especiales")]
        internal static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var floor1 = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            var floor3 = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (floor1 == null || floor3 == null || floor3.SpecialRooms.Length < 4)
            {
                Debug.LogError("Floor3SpecialRoomsSetup: faltan Floor_1/Floor_3 o las salas especiales (Primordia > Floor 3 > Crear salas especiales).");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FloorConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            EditorUtility.CopySerialized(floor3, config);

            // Un piso corto con las cuatro especiales sí o sí: salen todas, en salas de combate al azar.
            var specials = floor3.SpecialRooms.Select(room =>
            {
                room.always = true;
                return room;
            }).ToArray();
            config.ConfigureRooms(floor3.StartRoom, floor3.BossRoom, floor3.CombatRooms, floor3.SideRooms, new Vector2Int(10, 10), Vector2Int.zero);
            config.ConfigureShortcuts(Vector2Int.zero);
            config.ConfigureSpecialRooms(specials, new Vector2Int(specials.Length, specials.Length));
            config.ConfigureShop(null, null);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            // La creación de la escena no sobrescribe una existente: se borra para regenerarla.
            AssetDatabase.DeleteAsset(ScenePath);
            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            if (!CoreSetup.TryCreateSceneFromTemplate(ScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Al abrir la escena Unity descarga los assets sin usar: se vuelve a cargar la configuración.
            config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            FloorSetup.BuildFloor(scene, rig, player, floorConfig: config);
            Floor2Setup.AddTestLoadout(scene, floor1);
            Floor2Setup.AddHUD(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3SpecialRoomsSetup: escena Test_Floor3SpecialRooms lista ({specials.Length} salas especiales seguras).");
        }
    }
}
