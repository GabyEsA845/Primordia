using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Teletransportes (docs/multimedia-temp/floor3-environment/pieces/Teleport_Floor{N}_*): importa las hojas de los tres
    /// pisos, crea un prefab por piso y se lo pone al <see cref="FloorConfig"/> del piso 1 y del piso 2 (el del piso 3 se
    /// asignará al crear ese piso).
    /// </summary>
    public static partial class ArtSetup
    {
        const string TeleportSourceFolder = "docs/multimedia-temp/floor3-environment/pieces";
        const string TeleportArtFolder = "Assets/_Primordia/Art/Environment/Teleports";
        const string TeleportPrefabsFolder = "Assets/_Primordia/Prefabs/Rooms";
        const int TeleportCell = 32;
        // Debajo del jugador y de los enemigos, como las demás piezas planas del suelo.
        const int TeleportOrder = -4;

        [MenuItem("Primordia/Rooms/Crear teletransportes")]
        internal static void ImportTeleports()
        {
            System.IO.Directory.CreateDirectory(TeleportArtFolder);
            System.IO.Directory.CreateDirectory(TeleportPrefabsFolder);

            var prefabs = new Teleporter[4];
            for (int floor = 1; floor <= 3; floor++)
                prefabs[floor] = CreateTeleporterPrefab(floor);
            AssetDatabase.SaveAssets();

            foreach (var (floor, path) in new[] { (1, FloorSetup.FloorConfigPath), (2, Floor2Setup.Floor2ConfigPath) })
            {
                var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(path);
                if (config == null || prefabs[floor] == null)
                {
                    Debug.LogWarning($"ArtSetup: falta {path} o el teletransporte del piso {floor}; ese piso no tendrá teletransportes.");
                    continue;
                }
                config.SetTeleporter(prefabs[floor]);
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: teletransportes importados (3 prefabs) y asignados a Floor_1 y Floor_2. F11 viaja con ellos en Play.");
        }

        static Teleporter CreateTeleporterPrefab(int floor)
        {
            var sheets = new Sprite[4][];
            string[] names = { "Dormant", "Wake", "Idle", "Warp" };
            // El del piso 3 (tronco hueco) mide 32×40 con la base cerca de abajo; los otros son planos y van centrados.
            var pivot = floor == 3 ? new Vector2(0.5f, 10f / 40f) : new Vector2(0.5f, 0.5f);
            for (int i = 0; i < names.Length; i++)
            {
                string file = $"Teleport_Floor{floor}_{names[i]}.png";
                if (!CopyArt($"{TeleportSourceFolder}/{file}", $"{TeleportArtFolder}/{file}"))
                {
                    Debug.LogWarning($"ArtSetup: falta {TeleportSourceFolder}/{file}.");
                    return null;
                }
                sheets[i] = SliceSheet($"{TeleportArtFolder}/{file}", TeleportCell, pivot);
            }

            var root = EditPrefab($"{TeleportPrefabsFolder}/Teleporter_Floor{floor}.prefab", obj =>
            {
                var renderer = GetOrAdd<SpriteRenderer>(obj);
                renderer.sprite = sheets[0].Length > 0 ? sheets[0][0] : null;
                renderer.sortingOrder = TeleportOrder;
                GetOrAdd<Teleporter>(obj).Configure(renderer, sheets[0], sheets[1], sheets[2], sheets[3]);
            }, create: true);
            return root != null ? root.GetComponent<Teleporter>() : null;
        }
    }
}
