using System.Collections.Generic;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Primordia.Editor
{
    /// <summary>
    /// Construye un piso de prueba: salas en una rejilla unidas por pasillos, con puertas,
    /// enemigos y una salida en la última sala. Es provisional, con bloques en lugar de Tilemap,
    /// hasta que exista el arte de las salas.
    /// </summary>
    public static class FloorSetup
    {
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Floor.unity";

        // Interior de cada sala, en unidades. La vista mide 30×17.
        const float RoomWidth = 26f;
        const float RoomHeight = 18f;
        const float CorridorLength = 6f;
        const float DoorWidth = 3f;

        static readonly Color DoorColor = new(0.75f, 0.55f, 0.25f);
        static readonly Color FloorColor = new(0.1f, 0.14f, 0.12f);
        static readonly Color ExitColor = new(0.3f, 0.95f, 0.8f);

        static float CellWidth => RoomWidth + 2f + CorridorLength;
        static float CellHeight => RoomHeight + 2f + CorridorLength;

        /// <summary>Una sala del piso: su celda en la rejilla y sus enemigos (prefab y posición relativa).</summary>
        readonly struct RoomSpec
        {
            public readonly string Name;
            public readonly Vector2Int Cell;
            public readonly (string prefab, Vector2 position)[] Enemies;

            /// <summary>Al limpiarla, ofrece elegir 1 de 3 mutaciones.</summary>
            public readonly bool Reward;

            public RoomSpec(string name, Vector2Int cell, bool reward, params (string, Vector2)[] enemies)
            {
                Name = name;
                Cell = cell;
                Reward = reward;
                Enemies = enemies;
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

        /// <summary>Todo lo que el piso necesita: combate y enemigos.</summary>
        internal static void CreateAssets(Sprite square)
        {
            CombatSetup.CreateAssets(square);
            EnemySetup.CreateAssets(square);
            BossSetup.CreateAssets(square);
        }

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            BuildFloor(scene, rig, playerPrefab);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Construye el piso de prueba y coloca al jugador en la sala inicial, en la escena indicada.
        /// Con <paramref name="rewardPool"/>, las salas marcadas ofrecen mutaciones al limpiarlas.
        /// </summary>
        internal static void BuildFloor(Scene scene, GameObject rig, GameObject playerPrefab, MutationPool rewardPool = null)
        {
            // Se cargan desde disco después de abrir la escena, para que las referencias sean válidas.
            var square = CoreSetup.CreateSquareSprite();
            var prefabs = new Dictionary<string, Enemy>
            {
                ["Chaser"] = EnemySetup.LoadPrefab("Enemy_Chaser"),
                ["Shooter"] = EnemySetup.LoadPrefab("Enemy_Shooter"),
                ["Boss"] = BossSetup.LoadPrefab()
            };

            // Recorrido: Inicio → derecha → arriba → derecha → derecha → derecha (jefe y salida).
            var specs = new[]
            {
                new RoomSpec("Room_0_Start", new Vector2Int(0, 0), reward: false),
                new RoomSpec("Room_1", new Vector2Int(1, 0), reward: false,
                    ("Chaser", new Vector2(-6f, 4f)), ("Chaser", new Vector2(6f, 4f)), ("Chaser", new Vector2(0f, -5f))),
                new RoomSpec("Room_2", new Vector2Int(1, 1), reward: true,
                    ("Shooter", new Vector2(-8f, 5f)), ("Shooter", new Vector2(8f, 5f)), ("Chaser", new Vector2(0f, 3f))),
                new RoomSpec("Room_3", new Vector2Int(2, 1), reward: false,
                    ("Chaser", new Vector2(-4f, 5f)), ("Chaser", new Vector2(4f, -5f)),
                    ("Shooter", new Vector2(9f, 5f)), ("Shooter", new Vector2(9f, -5f))),
                new RoomSpec("Room_4", new Vector2Int(3, 1), reward: true,
                    ("Chaser", new Vector2(-5f, 6f)), ("Chaser", new Vector2(-5f, -6f)), ("Chaser", new Vector2(0f, 6f)),
                    ("Shooter", new Vector2(8f, 5f)), ("Shooter", new Vector2(8f, -5f))),
                new RoomSpec("Room_5_Boss", new Vector2Int(4, 1), reward: false,
                    ("Boss", new Vector2(0f, 4f)))
            };

            var floor = new GameObject("Floor").transform;
            var rooms = new List<Room>();

            for (int i = 0; i < specs.Length; i++)
            {
                var doorDirections = new List<Vector2Int>();
                if (i > 0)
                    doorDirections.Add(specs[i - 1].Cell - specs[i].Cell);
                if (i < specs.Length - 1)
                    doorDirections.Add(specs[i + 1].Cell - specs[i].Cell);

                var room = BuildRoom(floor, specs[i], doorDirections, prefabs, square);

                // El jefe orbita alrededor del centro de su sala.
                foreach (var boss in room.GetComponentsInChildren<BossAI>(true))
                    BossSetup.SetArenaCenter(boss, room.transform);

                if (specs[i].Reward && rewardPool != null)
                    room.gameObject.AddComponent<MutationReward>().Configure(rewardPool, 3);
                rooms.Add(room);
            }

            for (int i = 0; i < specs.Length - 1; i++)
                BuildCorridor(floor, specs[i].Cell, specs[i + 1].Cell, square);

            BuildExit(rooms[^1], square);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.position = CellCenter(specs[0].Cell);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;
            rig.GetComponentInChildren<CinemachineCamera>().transform.position = player.transform.position + Vector3.back * 10f;
        }

        static Vector2 CellCenter(Vector2Int cell) => new(cell.x * CellWidth, cell.y * CellHeight);

        static Room BuildRoom(Transform parent, RoomSpec spec, List<Vector2Int> doorDirections, Dictionary<string, Enemy> prefabs,
            Sprite square)
        {
            var center = CellCenter(spec.Cell);
            var roomObject = new GameObject(spec.Name);
            roomObject.transform.SetParent(parent, false);
            roomObject.transform.position = center;

            // Trigger 1,5 unidades más pequeño por lado: el jugador ya cruzó la puerta cuando se cierra.
            var trigger = roomObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(RoomWidth - 3f, RoomHeight - 3f);

            var floorTile = new GameObject("FloorTile");
            floorTile.transform.SetParent(roomObject.transform, false);
            floorTile.transform.localScale = new Vector3(RoomWidth, RoomHeight, 1f);
            var floorRenderer = floorTile.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = square;
            floorRenderer.color = FloorColor;
            floorRenderer.sortingOrder = -10;

            var walls = new GameObject("Walls").transform;
            walls.SetParent(roomObject.transform, false);

            float halfWidth = RoomWidth / 2f;
            float halfHeight = RoomHeight / 2f;
            float halfGap = DoorWidth / 2f;

            // Las paredes horizontales cubren también las esquinas.
            BuildWall(walls, center, horizontal: true, offset: halfHeight + 0.5f, from: -halfWidth - 1f, to: halfWidth + 1f,
                hasGap: doorDirections.Contains(Vector2Int.up), halfGap, square);
            BuildWall(walls, center, horizontal: true, offset: -halfHeight - 0.5f, from: -halfWidth - 1f, to: halfWidth + 1f,
                hasGap: doorDirections.Contains(Vector2Int.down), halfGap, square);
            BuildWall(walls, center, horizontal: false, offset: -halfWidth - 0.5f, from: -halfHeight, to: halfHeight,
                hasGap: doorDirections.Contains(Vector2Int.left), halfGap, square);
            BuildWall(walls, center, horizontal: false, offset: halfWidth + 0.5f, from: -halfHeight, to: halfHeight,
                hasGap: doorDirections.Contains(Vector2Int.right), halfGap, square);

            var doors = new List<Door>();
            foreach (var direction in doorDirections)
            {
                bool horizontalWall = direction.y != 0;
                var position = center + new Vector2(direction.x * (halfWidth + 0.5f), direction.y * (halfHeight + 0.5f));
                var size = horizontalWall ? new Vector2(DoorWidth, 1f) : new Vector2(1f, DoorWidth);
                doors.Add(BuildDoor(roomObject.transform, position, size, square));
            }

            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomObject.transform, false);
            foreach (var (prefabName, position) in spec.Enemies)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[prefabName].gameObject, enemiesRoot);
                enemy.transform.position = center + position;
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

        /// <summary>Pared de 1 unidad de grosor, con un hueco centrado para la puerta si hace falta.</summary>
        static void BuildWall(Transform parent, Vector2 center, bool horizontal, float offset, float from, float to,
            bool hasGap, float halfGap, Sprite square)
        {
            if (hasGap)
            {
                BuildWallSegment(parent, center, horizontal, offset, from, -halfGap, square);
                BuildWallSegment(parent, center, horizontal, offset, halfGap, to, square);
            }
            else
            {
                BuildWallSegment(parent, center, horizontal, offset, from, to, square);
            }
        }

        static void BuildWallSegment(Transform parent, Vector2 center, bool horizontal, float offset, float from, float to,
            Sprite square)
        {
            float middle = (from + to) / 2f;
            float length = to - from;
            var position = center + (horizontal ? new Vector2(middle, offset) : new Vector2(offset, middle));
            var size = horizontal ? new Vector2(length, 1f) : new Vector2(1f, length);
            PlayerSetup.CreateBlock(parent, "Wall", position, size, square);
        }

        static Door BuildDoor(Transform parent, Vector2 position, Vector2 size, Sprite square)
        {
            var doorObject = new GameObject("Door");
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.position = position;
            doorObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = doorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = DoorColor;
            renderer.sortingOrder = 1;

            doorObject.AddComponent<BoxCollider2D>();
            return doorObject.AddComponent<Door>();
        }

        static void BuildCorridor(Transform parent, Vector2Int fromCell, Vector2Int toCell, Sprite square)
        {
            var direction = toCell - fromCell;
            var start = CellCenter(fromCell);
            var corridor = new GameObject("Corridor").transform;
            corridor.SetParent(parent, false);

            float wallOffset = DoorWidth / 2f + 0.5f;

            if (direction.x != 0)
            {
                float x = start.x + direction.x * (RoomWidth / 2f + 1f + CorridorLength / 2f);
                foreach (float side in new[] { -1f, 1f })
                    PlayerSetup.CreateBlock(corridor, "Wall", new Vector2(x, start.y + side * wallOffset),
                        new Vector2(CorridorLength, 1f), square);
            }
            else
            {
                float y = start.y + direction.y * (RoomHeight / 2f + 1f + CorridorLength / 2f);
                foreach (float side in new[] { -1f, 1f })
                    PlayerSetup.CreateBlock(corridor, "Wall", new Vector2(start.x + side * wallOffset, y),
                        new Vector2(1f, CorridorLength), square);
            }
        }

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
