using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.Rooms;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Primordia.Editor
{
    /// <summary>
    /// Construye un piso: salas hechas con plantillas de texto (<see cref="RoomTemplate"/>) en una rejilla,
    /// unidas por pasillos, con puertas, enemigos y una salida en la última sala. Todo el piso se pinta en
    /// un único Tilemap con el arte del escenario (<see cref="RoomTheme"/>). La cámara sigue al jugador,
    /// adelantada hacia el cursor (<see cref="CameraLookAhead"/>).
    /// </summary>
    public static class FloorSetup
    {
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Floor.unity";
        const string TemplatesFolder = "Assets/_Primordia/Data/Rooms";
        internal const string SoupThemePath = "Assets/_Primordia/Art/Environment/Soup/Theme_Soup.asset";
        internal const string BossThemePath = "Assets/_Primordia/Art/Environment/Boss/Theme_Boss.asset";

        // Pasillo más corto entre dos salas vecinas; si una sala es más pequeña que su celda, el pasillo crece.
        const int CorridorLength = 4;

        // Sin decoración tan cerca de una puerta, para que no tape el paso.
        const int DoorClearance = 2;

        const int SoupTheme = 0;
        const int BossTheme = 1;

        static readonly Color ExitColor = new(0.3f, 0.95f, 0.8f);

        /// <summary>
        /// Una sala del piso: su celda en la rejilla, su forma (<see cref="RoomLayouts"/>), su tema y sus enemigos
        /// (prefab y posición relativa al centro del suelo).
        /// </summary>
        readonly struct RoomSpec
        {
            public readonly string Name;
            public readonly Vector2Int Cell;
            public readonly string Template;
            public readonly int Theme;
            public readonly (string prefab, Vector2 position)[] Enemies;

            /// <summary>Al limpiarla, ofrece elegir 1 de 3 mutaciones.</summary>
            public readonly bool Reward;

            public RoomSpec(string name, Vector2Int cell, string template, bool reward, int theme, params (string, Vector2)[] enemies)
            {
                Name = name;
                Cell = cell;
                Template = template;
                Reward = reward;
                Theme = theme;
                Enemies = enemies;
            }
        }

        /// <summary>Una sala ya colocada en el piso.</summary>
        sealed class PlacedRoom
        {
            public RoomSpec Spec;
            public RoomTemplate Template;
            public Vector2Int Origin;
            public readonly Dictionary<Vector2Int, List<Vector2Int>> DoorCells = new();
            public readonly Dictionary<Vector2Int, List<Vector2Int>> ArchCells = new();

            public RectInt Rect => new(Origin, new Vector2Int(Template.Width, Template.Height));

            public Vector2 FloorCenter
            {
                get
                {
                    var floor = Template.FloorBounds();
                    return new Vector2(Origin.x + floor.x + floor.width / 2f, Origin.y + floor.y + floor.height / 2f);
                }
            }
        }

        [MenuItem("Primordia/Rooms/Crear piso de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            CreateAssets(square);

            CreateTestScene(cameraRig, gameManager, player);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Todo lo que el piso necesita: combate, enemigos, jefe y plantillas de sala.</summary>
        internal static void CreateAssets(Sprite square)
        {
            CombatSetup.CreateAssets(square);
            EnemySetup.CreateAssets(square);
            BossSetup.CreateAssets(square);
            CreateTemplates();
        }

        /// <summary>Un asset por forma del catálogo. No sobrescribe los que ya existen, que se pueden retocar a mano.</summary>
        static void CreateTemplates()
        {
            Directory.CreateDirectory(TemplatesFolder);
            foreach (var (name, layout) in RoomLayouts.All)
            {
                string path = TemplatePath(name);
                if (AssetDatabase.LoadAssetAtPath<RoomTemplate>(path) != null)
                    continue;

                var template = ScriptableObject.CreateInstance<RoomTemplate>();
                template.SetLayout(layout);
                AssetDatabase.CreateAsset(template, path);
            }
            AssetDatabase.SaveAssets();

            foreach (var name in RoomLayouts.All.Keys)
            {
                var problems = AssetDatabase.LoadAssetAtPath<RoomTemplate>(TemplatePath(name)).Validate();
                if (problems.Count > 0)
                    Debug.LogWarning($"FloorSetup: la plantilla {name} tiene problemas:\n{string.Join("\n", problems)}");
            }
        }

        static string TemplatePath(string name) => $"{TemplatesFolder}/{name}.asset";

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            BuildFloor(scene, rig, playerPrefab);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void BuildFloor(Scene scene, GameObject rig, GameObject playerPrefab, MutationPool rewardPool = null)
        {
            // Se cargan desde disco después de abrir la escena, para que las referencias sean válidas.
            var square = CoreSetup.CreateSquareSprite();
            var themes = new[]
            {
                AssetDatabase.LoadAssetAtPath<RoomTheme>(SoupThemePath),
                AssetDatabase.LoadAssetAtPath<RoomTheme>(BossThemePath)
            };
            if (themes.Any(theme => theme == null))
            {
                Debug.LogError("FloorSetup: falta el arte del escenario. Ejecuta antes Primordia > Art > Importar escenario.");
                return;
            }

            var prefabs = new Dictionary<string, Enemy>
            {
                ["Chaser"] = EnemySetup.LoadPrefab("Enemy_Chaser"),
                ["Shooter"] = EnemySetup.LoadPrefab("Enemy_Shooter"),
                ["Boss"] = BossSetup.LoadPrefab()
            };

            // Recorrido: Inicio → derecha → arriba → derecha → derecha → derecha (jefe y salida).
            // Posiciones relativas al centro del suelo de cada sala.
            var specs = new[]
            {
                // Pequeña (suelo 20×11).
                new RoomSpec("Room_0_Start", new Vector2Int(0, 0), RoomLayouts.Small, reward: false, SoupTheme),
                // Cruz (suelo de 28×18 en total): un perseguidor en cada brazo.
                new RoomSpec("Room_1", new Vector2Int(1, 0), RoomLayouts.Cross, reward: false, SoupTheme,
                    ("Chaser", new Vector2(-9f, 0.5f)), ("Chaser", new Vector2(9f, 0.5f)), ("Chaser", new Vector2(0f, 6f))),
                // Normal con dos columnas (suelo 26×12) para cubrirse de los tiradores.
                new RoomSpec("Room_2", new Vector2Int(1, 1), RoomLayouts.Pillars, reward: true, SoupTheme,
                    ("Shooter", new Vector2(-9f, 3.5f)), ("Shooter", new Vector2(9f, 3.5f)), ("Chaser", new Vector2(0f, 2f))),
                // L (banda de 26×8 abajo y brazo de 12×8 arriba a la izquierda).
                new RoomSpec("Room_3", new Vector2Int(2, 1), RoomLayouts.LShape, reward: false, SoupTheme,
                    ("Chaser", new Vector2(-3f, 2f)), ("Chaser", new Vector2(4f, -4f)),
                    ("Shooter", new Vector2(-8f, 6f)), ("Shooter", new Vector2(10f, -6f))),
                // Grande (suelo 38×20): más que la pantalla; la cámara sigue al jugador.
                new RoomSpec("Room_4", new Vector2Int(3, 1), RoomLayouts.Large, reward: true, SoupTheme,
                    ("Chaser", new Vector2(-8f, 6f)), ("Chaser", new Vector2(-8f, -6f)), ("Chaser", new Vector2(0f, 7f)),
                    ("Shooter", new Vector2(12f, 5f)), ("Shooter", new Vector2(12f, -5f))),
                // El jefe, en una sala normal que cabe entera en pantalla.
                new RoomSpec("Room_5_Boss", new Vector2Int(4, 1), RoomLayouts.Normal, reward: false, BossTheme,
                    ("Boss", new Vector2(0f, 2.5f)))
            };

            var placed = PlaceRooms(specs);
            if (placed == null)
                return;

            var map = new FloorMap(MapBounds(placed));
            var noDecoration = new HashSet<Vector2Int>();

            for (int i = 0; i < placed.Count; i++)
                StampRoom(map, placed[i]);
            for (int i = 0; i < placed.Count - 1; i++)
            {
                var direction = placed[i + 1].Spec.Cell - placed[i].Spec.Cell;
                OpenDoor(map, placed[i], direction);
                OpenDoor(map, placed[i + 1], -direction);
                DigCorridor(map, placed[i], placed[i + 1], direction, noDecoration);
            }

            foreach (var room in placed)
            {
                foreach (var cell in room.DoorCells.Values.SelectMany(cells => cells).Concat(room.ArchCells.Values.SelectMany(cells => cells)))
                {
                    for (int dx = -DoorClearance; dx <= DoorClearance; dx++)
                        for (int dy = -DoorClearance - 2; dy <= DoorClearance; dy++)
                            noDecoration.Add(cell + new Vector2Int(dx, dy));
                }
            }

            var floor = new GameObject("Floor").transform;
            PaintLevel(floor, map, themes, noDecoration);

            var rooms = new List<Room>();
            foreach (var room in placed)
            {
                var built = BuildRoom(floor, room, themes[room.Spec.Theme], prefabs);

                // El jefe orbita alrededor del centro de su sala.
                foreach (var boss in built.GetComponentsInChildren<BossAI>(true))
                    BossSetup.SetArenaCenter(boss, built.transform);

                if (room.Spec.Reward && rewardPool != null)
                    built.gameObject.AddComponent<MutationReward>().Configure(rewardPool, 3);
                rooms.Add(built);
            }

            BuildExit(rooms[^1], square);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = placed[0].FloorCenter;

            // La cámara sigue a un punto entre el jugador y el cursor.
            var cameraTarget = new GameObject("CameraTarget").AddComponent<CameraLookAhead>();
            cameraTarget.Configure(player.GetComponent<PlayerController>());
            var vcam = rig.GetComponentInChildren<CinemachineCamera>();
            vcam.Follow = cameraTarget.transform;
            vcam.transform.position = player.transform.position + Vector3.back * 10f;
        }

        /// <summary>
        /// Coloca cada sala en su celda de la rejilla alineando su anclaje (el centro de sus puertas) con el de
        /// la celda: así las puertas de salas vecinas quedan enfrentadas aunque tengan tamaños distintos, y el
        /// pasillo entre ellas es recto. Cada columna mide lo que su sala más ancha y cada fila lo que su sala
        /// más alta, más el pasillo mínimo, para que los pasillos no se alarguen de más.
        /// </summary>
        static List<PlacedRoom> PlaceRooms(RoomSpec[] specs)
        {
            var templates = new Dictionary<string, RoomTemplate>();
            foreach (var name in specs.Select(spec => spec.Template).Distinct())
            {
                var template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(TemplatePath(name));
                if (template == null)
                {
                    Debug.LogError($"FloorSetup: falta la plantilla {name}. Ejecuta Primordia > Rooms > Crear piso de prueba o regenera la escena Game.");
                    return null;
                }
                templates[name] = template;
            }

            var anchorX = Anchors(specs, spec => spec.Cell.x, spec => templates[spec.Template].Anchor.x,
                spec => templates[spec.Template].Width);
            var anchorY = Anchors(specs, spec => spec.Cell.y, spec => templates[spec.Template].Anchor.y,
                spec => templates[spec.Template].Height);

            return specs.Select(spec =>
            {
                var template = templates[spec.Template];
                var anchor = new Vector2Int(anchorX[spec.Cell.x], anchorY[spec.Cell.y]);
                return new PlacedRoom { Spec = spec, Template = template, Origin = anchor - template.Anchor };
            }).ToList();
        }

        /// <summary>
        /// Posición del anclaje de cada columna (o fila) de la rejilla: tras la anterior, deja sitio para lo que
        /// sobresale de sus salas antes y después del anclaje, y el pasillo mínimo entre medias.
        /// </summary>
        static Dictionary<int, int> Anchors(RoomSpec[] specs, System.Func<RoomSpec, int> index,
            System.Func<RoomSpec, int> anchor, System.Func<RoomSpec, int> size)
        {
            var result = new Dictionary<int, int>();
            int next = 0;
            foreach (var group in specs.GroupBy(index).OrderBy(g => g.Key))
            {
                int before = group.Max(anchor);
                int after = group.Max(spec => size(spec) - anchor(spec));
                result[group.Key] = next + before;
                next += before + after + CorridorLength;
            }
            return result;
        }

        static RectInt MapBounds(List<PlacedRoom> rooms)
        {
            // Relleno de pared alrededor: media vista (15×8,5) más el adelanto de la cámara (3), con holgura,
            // para que nunca se vea el vacío al borde del piso.
            const int margin = 20;
            int minX = rooms.Min(r => r.Rect.xMin) - margin, minY = rooms.Min(r => r.Rect.yMin) - margin;
            int maxX = rooms.Max(r => r.Rect.xMax) + margin, maxY = rooms.Max(r => r.Rect.yMax) + margin;
            return new RectInt(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Copia la plantilla en el mapa. Los huecos de puerta quedan como pared hasta que se abren.</summary>
        static void StampRoom(FloorMap map, PlacedRoom room)
        {
            for (int y = 0; y < room.Template.Height; y++)
            {
                for (int x = 0; x < room.Template.Width; x++)
                {
                    var cell = room.Origin + new Vector2Int(x, y);
                    map.Set(cell, room.Template.Get(x, y) == RoomTemplate.Mark.Floor ? FloorMap.Kind.Floor : FloorMap.Kind.Wall);
                    map.SetTheme(cell, room.Spec.Theme);
                }
            }
        }

        /// <summary>
        /// Abre el hueco de puerta en esa dirección. Arriba: la fila de la tapa pasa a ser paso y la cara
        /// que hay debajo, arco. Abajo y a los lados: hueco de suelo.
        /// </summary>
        static void OpenDoor(FloorMap map, PlacedRoom room, Vector2Int direction)
        {
            var cells = room.Template.DoorCells(direction).Select(cell => room.Origin + cell).ToList();
            if (cells.Count == 0)
            {
                Debug.LogError($"FloorSetup: la plantilla de {room.Spec.Name} no tiene puerta hacia {direction}.");
                return;
            }

            room.DoorCells[direction] = cells;
            if (direction != Vector2Int.up)
            {
                foreach (var cell in cells)
                    map.Set(cell, FloorMap.Kind.DoorGap);
                return;
            }

            var arch = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                map.Set(cell, FloorMap.Kind.Passage);
                for (var below = cell + Vector2Int.down; map.Get(below) == FloorMap.Kind.Wall; below += Vector2Int.down)
                {
                    map.Set(below, FloorMap.Kind.DoorArch);
                    arch.Add(below);
                }
            }
            room.ArchCells[direction] = arch;
        }

        /// <summary>Pasillo recto de 2 tiles de ancho entre las puertas de dos salas vecinas.</summary>
        static void DigCorridor(FloorMap map, PlacedRoom from, PlacedRoom to, Vector2Int direction, HashSet<Vector2Int> corridorCells)
        {
            var start = from.DoorCells[direction];
            var end = to.DoorCells[-direction];

            foreach (var cell in start)
            {
                // Avanza desde el hueco de la primera sala hasta el de la segunda.
                for (var step = cell + direction; !end.Contains(step) && map.Get(step) == FloorMap.Kind.Wall; step += direction)
                {
                    map.Set(step, FloorMap.Kind.Floor);
                    map.SetTheme(step, SoupTheme);
                    corridorCells.Add(step);
                }
            }
        }

        static void PaintLevel(Transform parent, FloorMap map, RoomTheme[] themes, HashSet<Vector2Int> noDecoration)
        {
            var grid = new GameObject("Level", typeof(Grid));
            grid.transform.SetParent(parent, false);

            var floor = CreateTilemap(grid.transform, "Ground", -20);
            var decoration = CreateTilemap(grid.transform, "Decoration", -15);
            var walls = CreateTilemap(grid.transform, "Walls", -10);

            // Una sola forma de colisión para todas las paredes. En contornos y no en polígonos: las salas son
            // huecos dentro de la pared, y en modo polígonos esos huecos quedarían rellenos.
            var body = walls.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = walls.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            walls.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Outlines;

            var missing = RoomPainter.Paint(map, themes, floor, decoration, walls, noDecoration);
            if (missing.Count > 0)
                Debug.LogWarning($"FloorSetup: {missing.Count} celdas sin pieza de arte (en magenta), por ejemplo {string.Join(", ", missing.Take(5))}.");
        }

        static Tilemap CreateTilemap(Transform grid, string name, int sortingOrder)
        {
            var layer = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(grid, false);
            layer.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return layer.GetComponent<Tilemap>();
        }

        static Room BuildRoom(Transform parent, PlacedRoom placed, RoomTheme theme, Dictionary<string, Enemy> prefabs)
        {
            var center = placed.FloorCenter;
            var floorBounds = placed.Template.FloorBounds();
            var roomObject = new GameObject(placed.Spec.Name);
            roomObject.transform.SetParent(parent, false);
            roomObject.transform.position = center;

            // Trigger 1,5 unidades más pequeño por lado: el jugador ya cruzó la puerta cuando se cierra.
            var trigger = roomObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(floorBounds.width - 3f, floorBounds.height - 3f);

            var doors = new List<Door>();
            foreach (var (direction, cells) in placed.DoorCells)
            {
                var doorCells = direction == Vector2Int.up ? placed.ArchCells[direction] : cells;
                doors.Add(BuildDoor(roomObject.transform, theme, direction, doorCells));
            }

            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);
            foreach (var (prefabName, position) in placed.Spec.Enemies)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[prefabName].gameObject, enemiesRoot);
                enemy.transform.position = center + position;

                var cell = Vector2Int.FloorToInt(center + position) - placed.Origin;
                if (placed.Template.Get(cell.x, cell.y) != RoomTemplate.Mark.Floor)
                    Debug.LogWarning($"FloorSetup: {prefabName} de {placed.Spec.Name} queda fuera del suelo, en {position}.");
            }

            var room = roomObject.AddComponent<Room>();
            var serialized = new SerializedObject(room);
            serialized.FindProperty("enemiesRoot").objectReferenceValue = enemiesRoot;
            var doorsProperty = serialized.FindProperty("doors");
            doorsProperty.arraySize = doors.Count;
            for (int i = 0; i < doors.Count; i++)
                doorsProperty.GetArrayElementAtIndex(i).objectReferenceValue = doors[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return room;
        }

        /// <summary>
        /// Membrana animada sobre el hueco, con su colisión. Arriba, además, el arco fijo de la pared.
        /// </summary>
        static Door BuildDoor(Transform parent, RoomTheme theme, Vector2Int direction, List<Vector2Int> cells)
        {
            int minX = cells.Min(c => c.x), minY = cells.Min(c => c.y);
            int maxX = cells.Max(c => c.x), maxY = cells.Max(c => c.y);
            var size = new Vector2(maxX - minX + 1, maxY - minY + 1);
            var position = new Vector2(minX, minY) + size / 2f;

            if (direction == Vector2Int.up && theme.Doorway != null)
            {
                var doorway = new GameObject("Doorway");
                doorway.transform.SetParent(parent, false);
                doorway.transform.position = position;
                var doorwayRenderer = doorway.AddComponent<SpriteRenderer>();
                doorwayRenderer.sprite = theme.Doorway;
                doorwayRenderer.sortingOrder = -9;
            }

            var doorObject = new GameObject($"Door_{DirectionName(direction)}");
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.position = position;

            var frames = direction == Vector2Int.up ? theme.DoorTop : direction == Vector2Int.down ? theme.DoorBottom : theme.DoorSide;
            var renderer = doorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = frames != null && frames.Length > 0 ? frames[^1] : null;
            renderer.sortingOrder = -8;
            renderer.flipX = direction == Vector2Int.right;

            var blocker = doorObject.AddComponent<BoxCollider2D>();
            blocker.size = size;

            var door = doorObject.AddComponent<Door>();
            door.Configure(blocker, renderer, frames);
            return door;
        }

        static string DirectionName(Vector2Int direction) =>
            direction == Vector2Int.up ? "Up" : direction == Vector2Int.down ? "Down" : direction == Vector2Int.left ? "Left" : "Right";

        static void BuildExit(Room room, Sprite square)
        {
            var exitObject = new GameObject("Exit");
            exitObject.transform.SetParent(room.transform, false);
            exitObject.transform.localScale = new Vector3(2f, 2f, 1f);

            var renderer = exitObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = ExitColor;
            renderer.sortingOrder = 1;

            exitObject.AddComponent<BoxCollider2D>().isTrigger = true;

            var exit = exitObject.AddComponent<RoomExit>();
            var serialized = new SerializedObject(exit);
            serialized.FindProperty("room").objectReferenceValue = room;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
