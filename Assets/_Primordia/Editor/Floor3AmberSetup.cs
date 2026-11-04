using System.Linq;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// La sala del ámbar del piso 3 (paso 6, bloque 2): prefab del bloque de ámbar con su cucaracha (40×44 px, 4 cuadros de reposo y 6 de
    /// rotura), la plantilla de la sala (lateral, sin enemigos, siempre una) y su configuración en Floor_3. Las mutaciones especiales que
    /// guarda salen de <c>Data/Mutations/Floor3/Mutation_Cerci|AmberCapsule|AmberGland</c>; mientras no existan (subpasos 2b-2d), el bloque
    /// guarda como provisionales las especiales de las almejas del piso 2.
    /// </summary>
    public static class Floor3AmberSetup
    {
        const string SourceFolder = "docs/multimedia-temp/floor3-environment/pieces";
        const string ArtFolder = "Assets/_Primordia/Art/Environment/Amber";
        const string PrefabPath = "Assets/_Primordia/Prefabs/Rooms/AmberBlock.prefab";
        const string TemplatePath = "Assets/_Primordia/Data/Rooms/Floor3/Room_Special_Amber.asset";
        const string MutationPickupPath = "Assets/_Primordia/Prefabs/Mutations/MutationPickup.prefab";
        const string Floor3MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string Floor2MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor2";
        static readonly string[] AmberMutations = { "Mutation_Cerci", "Mutation_AmberCapsule", "Mutation_AmberGland" };
        static readonly string[] PlaceholderMutations = { "Mutation_LeanchoiliaFlagella", "Mutation_MarrellaCuticle", "Mutation_ClonalBud" };

        [MenuItem("Primordia/Floor 3/Crear sala del ámbar")]
        static void Create()
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("Floor3AmberSetup: no existe Floor_3; créalo con Primordia > Floor 3 > Crear piso 3 y escena de prueba.");
                return;
            }

            var block = CreateBlockPrefab();
            var template = CreateTemplate();
            if (block == null || template == null)
                return;

            var pickup = AssetDatabase.LoadAssetAtPath<MutationPickup>(MutationPickupPath);
            var mutations = AmberMutations.Select(name => AssetDatabase.LoadAssetAtPath<MutationData>($"{Floor3MutationsFolder}/{name}.asset"))
                .Where(mutation => mutation != null).ToArray();
            if (mutations.Length < AmberMutations.Length)
            {
                // Provisional hasta tener las tres del ámbar: las especiales de las almejas del piso 2 (solo para probar la sala).
                var placeholders = PlaceholderMutations.Select(name => AssetDatabase.LoadAssetAtPath<MutationData>($"{Floor2MutationsFolder}/{name}.asset"))
                    .Where(mutation => mutation != null);
                mutations = mutations.Concat(placeholders).ToArray();
                Debug.LogWarning($"Floor3AmberSetup: solo hay {mutations.Count(m => AmberMutations.Any(n => m.name == n))} de las 3 mutaciones del ámbar; el resto es provisional (especiales del piso 2).");
            }
            config.ConfigureAmber(new FloorConfig.AmberContents { block = block, mutations = mutations, mutationPickup = pickup });

            // La sala del ámbar: lateral, sin enemigos y siempre una. Se conservan las demás especiales del piso.
            var amberRoom = new FloorConfig.SpecialRoom
            {
                template = template,
                placement = FloorConfig.SpecialPlacement.Side,
                countsAsCombat = false,
                enemyMultiplier = 0f,
                always = true,
                band = -1,
                amber = true,
            };
            var others = config.SpecialRooms.Where(room => !room.amber).ToList();
            others.Add(amberRoom);
            var count = config.SpecialRoomCount;
            config.ConfigureSpecialRooms(others.ToArray(), others.Count == 1 ? new Vector2Int(1, 1) : count);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3AmberSetup: sala del ámbar lista en Floor_3 (lateral, sin enemigos, una por partida).");
        }

        static AmberBlock CreateBlockPrefab()
        {
            System.IO.Directory.CreateDirectory(ArtFolder);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
            foreach (var file in new[] { "AmberBlock_Idle.png", "AmberBlock_Break.png" })
            {
                string source = $"{SourceFolder}/{file}";
                string target = $"{ArtFolder}/{file}";
                if (System.IO.File.Exists(source))
                {
                    System.IO.File.Copy(source, target, overwrite: true);
                    AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                }
                else if (!System.IO.File.Exists(target))
                {
                    Debug.LogWarning($"Floor3AmberSetup: falta {source}.");
                    return null;
                }
            }

            // La base del bloque, cerca de abajo del cuadro de 40×44.
            var pivot = new Vector2(0.5f, 6f / 44f);
            var idle = Slice($"{ArtFolder}/AmberBlock_Idle.png", pivot);
            var shatter = Slice($"{ArtFolder}/AmberBlock_Break.png", pivot);

            var root = new GameObject("AmberBlock");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = idle.Length > 0 ? idle[0] : null;
                renderer.sortingOrder = -5;
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2.2f, 0.9f);
                collider.offset = new Vector2(0f, 0.2f);
                root.AddComponent<AmberBlock>().Configure(renderer, idle, shatter, null, null);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return saved != null ? saved.GetComponent<AmberBlock>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Sprite[] Slice(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            const int cell = 40;
            var sheet = new SpriteMetaData[width / cell];
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"{System.IO.Path.GetFileNameWithoutExtension(path)}_{i}",
                    rect = new Rect(i * cell, 0, cell, height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                };
            }
#pragma warning disable CS0618
            importer.spritesheet = sheet;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.rect.x).ToArray();
        }

        /// <summary>Sala de 24×15 con una puerta en cada lado (la que no se usa queda cerrada) y el bloque (CC) en el centro.</summary>
        static RoomTemplate CreateTemplate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RoomTemplate>(TemplatePath);
            if (existing != null)
                return existing;

            var rows = RoomLayouts.Rectangle(24, 15).TrimEnd('\n').Split('\n');
            // Dos casillas C seguidas, en la fila de suelo del medio (sin tapar las puertas de los lados).
            var row = rows[6].ToCharArray();
            row[11] = 'C';
            row[12] = 'C';
            rows[6] = new string(row);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TemplatePath));
            var template = ScriptableObject.CreateInstance<RoomTemplate>();
            template.SetLayout(string.Join("\n", rows) + "\n");
            AssetDatabase.CreateAsset(template, TemplatePath);
            var problems = template.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"Floor3AmberSetup: la plantilla tiene problemas:\n{string.Join("\n", problems)}");
            return template;
        }
    }
}
