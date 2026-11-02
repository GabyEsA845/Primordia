using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de las reacciones del piso 3 (fuego, metano, veneno) y los helechos altos de cada tramo. Parte del arte que ya
    /// importó Primordia > Floor 3 > Importar todo el arte del piso 3.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor3ReactionKitPath = "Assets/_Primordia/Data/Rooms/ReactionKit_Floor3.asset";
        const string Floor3EffectsPrefabsFolder = "Assets/_Primordia/Prefabs/Effects";
        const string BurningMarkerPrefabPath = Floor3EffectsPrefabsFolder + "/Effect_Burning.prefab";

        // Fuego del suelo y llama de los enemigos: hojas genéricas de las mutaciones; el apagado, de las reacciones.
        const string Floor3MutationEffects = Floor3ArtRoot + "/floor3-mutations/Effects";
        const string Floor3ReactionEffects = Floor3ArtRoot + "/floor3-reactions/Effects";
        const float BurningMarkerFrameSeconds = 0.1f;
        const string PoisonedMarkerPrefabPath = Floor3EffectsPrefabsFolder + "/Effect_Poisoned.prefab";

        [MenuItem("Primordia/Floor 3/Crear reacciones (fuego)")]
        static void CreateFloor3ReactionKitMenu()
        {
            var kit = CreateFloor3ReactionKit();
            Debug.Log(kit != null ? "ArtSetup: arte de las reacciones del piso 3 listo (fuego)." : "ArtSetup: faltan hojas de fuego; ejecuta antes Primordia > Floor 3 > Importar todo el arte del piso 3.");
        }

        /// <summary>El <see cref="ReactionKit"/> del piso 3 con el arte del fuego. Null si falta alguna hoja.</summary>
        internal static ReactionKit CreateFloor3ReactionKit()
        {
            string groundFire = $"{Floor3MutationEffects}/Fx_GroundFire.png";
            string burning = $"{Floor3MutationEffects}/Fx_Burning.png";
            string fireOut = $"{Floor3ReactionEffects}/Fx_FireOut.png";
            if (!File.Exists(groundFire) || !File.Exists(burning) || !File.Exists(fireOut))
                return null;

            Directory.CreateDirectory(Floor3EffectsPrefabsFolder);
            // Pivote en la base de las llamas, como dice el README de las reacciones.
            var groundFireFrames = SliceSheet(groundFire, 32, new Vector2(0.5f, 0f));
            var fireOutFrames = SliceSheet(fireOut, 32, new Vector2(0.5f, 0f));
            var burningFrames = SliceSheet(burning, 16);

            var marker = EditPrefab(BurningMarkerPrefabPath, obj =>
            {
                var renderer = GetOrAdd<SpriteRenderer>(obj);
                renderer.sprite = burningFrames.Length > 0 ? burningFrames[0] : null;
                renderer.sortingOrder = 8;
                GetOrAdd<SpriteLoop>(obj).Configure(renderer, burningFrames, BurningMarkerFrameSeconds, 0f);
            }, create: true);

            var kit = AssetDatabase.LoadAssetAtPath<ReactionKit>(Floor3ReactionKitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<ReactionKit>();
                AssetDatabase.CreateAsset(kit, Floor3ReactionKitPath);
            }
            kit.ConfigureFire(groundFireFrames, fireOutFrames, marker);

            // Veneno: el charco grande (48×48, centro), la nube tóxica (48×64, pivote en la base de la columna, a 58 px de arriba) y la
            // marca de gotas de los envenenados (16×16).
            string bodiesEffects = $"{Floor3ArtRoot}/floor3-bodies/Effects";
            string[] poisonSheets =
            {
                $"{bodiesEffects}/Fx_PoisonPuddleBig_Appear.png", $"{bodiesEffects}/Fx_PoisonPuddleBig_Idle.png",
                $"{bodiesEffects}/Fx_PoisonPuddleBig_Fade.png", $"{Floor3ReactionEffects}/Fx_PoisonPuddle_Ignite.png",
                $"{Floor3ReactionEffects}/Fx_ToxicCloud_Form.png", $"{Floor3ReactionEffects}/Fx_ToxicCloud_Idle.png",
                $"{Floor3ReactionEffects}/Fx_ToxicCloud_Fade.png", $"{Floor3ReactionEffects}/Fx_Poisoned.png",
            };
            if (poisonSheets.All(File.Exists))
            {
                var cloudPivot = new Vector2(0.5f, (64 - 58) / 64f);
                var poisonedFrames = SliceSheet(poisonSheets[7], 16);
                var poisonedMarker = EditPrefab(PoisonedMarkerPrefabPath, obj =>
                {
                    var renderer = GetOrAdd<SpriteRenderer>(obj);
                    renderer.sprite = poisonedFrames.Length > 0 ? poisonedFrames[0] : null;
                    renderer.sortingOrder = 8;
                    GetOrAdd<SpriteLoop>(obj).Configure(renderer, poisonedFrames, 0.12f, 0f);
                }, create: true);
                kit.ConfigurePoison(SliceSheet(poisonSheets[0], 48), SliceSheet(poisonSheets[1], 48), SliceSheet(poisonSheets[2], 48),
                    SliceSheet(poisonSheets[3], 48), SliceSheet(poisonSheets[4], 48, cloudPivot), SliceSheet(poisonSheets[5], 48, cloudPivot),
                    SliceSheet(poisonSheets[6], 48, cloudPivot), poisonedMarker);
            }
            else
                Debug.LogWarning("ArtSetup: faltan hojas del veneno (charco, nube o marca); ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");

            // Metano: la explosión del odre (8 cuadros de 64×64, centro) y el fogonazo de la niebla (pivote abajo).
            string explode = $"{Floor3MutationEffects}/Fx_MethaneSac_Explode.png";
            string mistIgnite = $"{Floor3ReactionEffects}/Fx_MistIgnite.png";
            string mistFlare = $"{Floor3ReactionEffects}/Fx_MistFlare.png";
            if (File.Exists(explode) && File.Exists(mistIgnite) && File.Exists(mistFlare))
            {
                kit.ConfigureMethane(SliceSheet(explode, 64), SliceSheet(mistIgnite, 32, new Vector2(0.5f, 0f)),
                    SliceSheet(mistFlare, 24, new Vector2(0.5f, 0f)));
            }
            else
                Debug.LogWarning("ArtSetup: faltan hojas del metano (explosión o niebla); ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            return kit;
        }

        // Helechos altos (README del escenario): nombre, ancho de la celda, alto, y píxeles desde arriba hasta el pie, más cuántos
        // salen respecto a los demás y el radio de su base (lo que se sacude al pasar).
        static readonly (string name, int width, int height, int foot, float weight, float radius)[] FernTypes =
        {
            ("Leafy", 84, 50, 45, 3f, 0.4f),
            ("Sparse", 92, 50, 45, 2f, 0.35f),
            ("Young", 44, 34, 29, 3f, 0.25f),
            ("Bent", 88, 64, 45, 1.5f, 0.4f),
            ("Tree", 84, 74, 69, 1f, 0.5f),
        };

        // Helechos por tile de suelo libre de la sala: el pantano muy poblado, el bosque humeante menos y la tierra quemada
        // casi sin ellos (los que quedan, carbonizados).
        static readonly Dictionary<string, float> FernDensities = new()
        {
            [SwampThemeName] = 0.030f, [SmokeThemeName] = 0.018f, [BurntThemeName] = 0.006f,
        };

        // Odres por sala (mínimo y máximo) y manchas de niebla por tile libre en cada tramo: el pantano tiene niebla y algún odre, el
        // bosque humeante más odres, y la tierra quemada casi nada (ya ardió).
        static readonly Dictionary<string, (Vector2Int sacs, float mist)> MethaneAmounts = new()
        {
            [SwampThemeName] = (new Vector2Int(1, 2), 0.004f),
            [SmokeThemeName] = (new Vector2Int(1, 3), 0.003f),
            [BurntThemeName] = (new Vector2Int(0, 1), 0f),
        };

        // Obstáculos de cobertura (README del escenario): archivo, ancho y alto de la imagen, y si bloquean 1×1 o 2×2. La colisión es
        // la base del dibujo (con unos píxeles de margen y sombra debajo), así que el pivote se pone para que el pie quede en
        // la base de la caja de colisión que centra el generador.
        static readonly (string name, int width, int height, int tiles)[] ObstacleTypes =
        {
            ("Obstacle_Rock", 24, 18, 1),
            ("Obstacle_Stump", 24, 22, 1),
            ("Obstacle_Sigillaria", 48, 66, 2),
            ("Obstacle_BrokenTrunk", 40, 46, 2),
            ("Obstacle_FallenLog", 48, 30, 2),
        };

        // Margen de sombra bajo el pie de los sprites, en píxeles.
        const int ObstacleFootMargin = 2;

        static void ImportObstacles(RoomTheme theme, string folder)
        {
            var small = new List<Sprite>();
            var big = new List<Sprite>();
            foreach (var (name, width, height, tiles) in ObstacleTypes)
            {
                string path = $"{folder}/props/{name}.png";
                if (!File.Exists(path))
                    continue;
                // El centro de la caja de colisión está a medio tile por cada tile de lado sobre la base.
                float centerFromFoot = tiles * 16f / 2f;
                var pivot = new Vector2(0.5f, (ObstacleFootMargin + centerFromFoot) / height);
                var sprite = SliceSheet(path, width, pivot).FirstOrDefault();
                if (sprite != null)
                    (tiles == 1 ? small : big).Add(sprite);
            }
            theme.ConfigureObstacles(small.ToArray(), big.ToArray());
        }

        // Bulbos venenosos por sala en cada tramo: el pantano tiene alguno, el bosque humeante menos y la tierra quemada ninguno.
        static readonly Dictionary<string, Vector2Int> BulbAmounts = new()
        {
            [SwampThemeName] = new Vector2Int(0, 2), [SmokeThemeName] = new Vector2Int(0, 1), [BurntThemeName] = Vector2Int.zero,
        };

        static void ImportPoisonBulbs(RoomTheme theme, string themeName)
        {
            string Sheet(string name) => $"{Floor3ArtRoot}/floor3-reactions/{themeName}/{name}.png";
            var pivot = new Vector2(0.5f, 0f);
            Sprite[] Frames(string name) => File.Exists(Sheet(name)) ? SliceSheet(Sheet(name), 32, pivot) : new Sprite[0];
            theme.ConfigurePoison(new PoisonSpec
            {
                bulbIdle = Frames("PoisonBulb_Idle"),
                bulbBurst = Frames("PoisonBulb_Burst"),
                bulbStump = Frames("PoisonBulb_Stump").FirstOrDefault(),
                bulbRegrow = Frames("PoisonBulb_Regrow"),
                bulbCount = BulbAmounts.GetValueOrDefault(themeName),
            });
        }

        // Escenario vivo (README del escenario): cuántos de cada cosa por sala en cada tramo. El pantano está lleno de charcos y
        // musgo, el bosque humeante tiene más fumarolas y la tierra quemada muchas, brasas y humo de tocones.
        static readonly Dictionary<string, (Vector2Int vents, Vector2Int puddles, float water, float gnats, Vector2Int beetles, Vector2Int embers, float moss)>
            SceneryAmounts = new()
            {
                [SwampThemeName] = (new Vector2Int(0, 1), new Vector2Int(3, 5), 0.6f, 0.7f, new Vector2Int(2, 4), Vector2Int.zero, 0.08f),
                [SmokeThemeName] = (new Vector2Int(1, 2), new Vector2Int(1, 3), 0.4f, 0.5f, new Vector2Int(1, 3), Vector2Int.zero, 0.04f),
                [BurntThemeName] = (new Vector2Int(2, 3), new Vector2Int(1, 2), 0.3f, 0f, new Vector2Int(0, 2), new Vector2Int(6, 10), 0f),
            };

        static readonly string[] PuddleShapes = { "Round", "Long", "Lobed", "Chain", "Island", "Splash", "Kidney" };

        static void ImportScenery(RoomTheme theme, string folder, string themeName)
        {
            string Sheet(string name) => $"{folder}/props/{name}.png";
            // Cuadros: ancho de cada uno = ancho de la hoja entre su número de cuadros.
            Sprite[] Frames(string name, int count, Vector2 pivot)
            {
                string path = Sheet(name);
                if (!File.Exists(path))
                    return new Sprite[0];
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.GetSourceTextureWidthAndHeight(out int width, out _);
                return SliceSheet(path, width / count, pivot);
            }

            var center = new Vector2(0.5f, 0.5f);
            var foot = new Vector2(0.5f, 0f);
            var puddles = new List<PuddleType>();
            foreach (var shape in PuddleShapes)
            {
                // Lodo: brillo y una burbuja, 260 ms; agua: reflejos que se deslizan, 220 ms.
                var sludgeFrames = Frames($"Puddle_Sludge_{shape}", 4, center);
                if (sludgeFrames.Length > 0)
                    puddles.Add(new PuddleType { name = $"Sludge_{shape}", frames = sludgeFrames, frameSeconds = 0.26f, sludge = true });
                var waterFrames = Frames($"Puddle_Water_{shape}", 4, center);
                if (waterFrames.Length > 0)
                    puddles.Add(new PuddleType { name = $"Water_{shape}", frames = waterFrames, frameSeconds = 0.22f, sludge = false });
            }

            var amounts = SceneryAmounts.GetValueOrDefault(themeName);
            var stumpPath = Sheet("Obstacle_Stump");
            theme.ConfigureScenery(new SceneryArt
            {
                ventIdle = Frames("Vent_Idle", 4, foot),
                ventWarn = Frames("Vent_Warn", 4, foot),
                ventBlast = Frames("Vent_Blast", 6, foot),
                ventCount = amounts.vents,
                puddles = puddles.ToArray(),
                puddleCount = amounts.puddles,
                waterShare = amounts.water,
                gnatChance = amounts.gnats,
                gnats = Frames("Critter_Gnats", 4, center),
                beetleIdle = Frames("Critter_Beetle_Idle", 2, center),
                beetleRun = Frames("Critter_Beetle_Run", 4, center),
                beetleCount = amounts.beetles,
                embers = Frames("Deco_Embers", 4, center),
                emberCount = amounts.embers,
                dripMoss = Frames("Deco_DripMoss", 6, foot),
                mossChance = amounts.moss,
                // El tocón quemado (humo encima) es el mismo sprite que importa ImportObstacles; solo la tierra quemada tiene el humo.
                stump = themeName == BurntThemeName ? StumpSpriteOf(theme) : null,
                stumpSmoke = themeName == BurntThemeName ? Frames("Fx_StumpSmoke", 6, foot) : new Sprite[0],
            });
        }

        // El sprite del tocón entre las rocas pequeñas del tema (el segundo, tras la roca, si ya se importaron los obstáculos).
        static Sprite StumpSpriteOf(RoomTheme theme)
        {
            var stump = theme.SmallObstacles.FirstOrDefault(sprite => sprite != null && sprite.name.StartsWith("Obstacle_Stump"));
            return stump;
        }

        static void ImportMethane(RoomTheme theme, string folder, string themeName)
        {
            string idle = $"{folder}/props/Methane_Sac_Idle.png";
            string mist = $"{folder}/props/Methane_Mist.png";
            string burst = $"{Floor3ArtRoot}/floor3-reactions/{themeName}/Methane_Sac_Burst.png";
            string remains = $"{Floor3ArtRoot}/floor3-reactions/{themeName}/Methane_Sac_Remains.png";
            var pivot = new Vector2(0.5f, 0f);
            var amounts = MethaneAmounts.GetValueOrDefault(themeName);
            theme.ConfigureMethane(new MethaneSpec
            {
                sacIdle = File.Exists(idle) ? SliceSheet(idle, 24, pivot) : new Sprite[0],
                sacBurst = File.Exists(burst) ? SliceSheet(burst, 32, pivot) : new Sprite[0],
                sacRemains = File.Exists(remains) ? SliceSheet(remains, 32, pivot).FirstOrDefault() : null,
                mist = File.Exists(mist) ? SliceSheet(mist, 64, pivot) : new Sprite[0],
                sacCount = amounts.sacs,
                mistPerCell = amounts.mist,
            });
        }

        static void ImportFerns(RoomTheme theme, string folder, string themeName)
        {
            var specs = FernTypes.Select(type =>
            {
                string Sheet(string state) => $"{folder}/props/Fern_{type.name}_{state}.png";
                if (!File.Exists(Sheet("Sway")))
                    return null;

                // Pivote: el pie, en el centro de abajo (los sprites llevan margen y sombra en el suelo).
                var pivot = new Vector2(0.5f, (type.height - type.foot) / (float)type.height);
                var spec = new FernSpec
                {
                    name = type.name,
                    sway = SliceSheet(Sheet("Sway"), type.width, pivot),
                    rustle = File.Exists(Sheet("Rustle")) ? SliceSheet(Sheet("Rustle"), type.width, pivot) : new Sprite[0],
                    // Solo el pantano y el bosque humeante tienen helechos que arden; en la tierra quemada ya están carbonizados.
                    burn = File.Exists(Sheet("Burn")) ? SliceSheet(Sheet("Burn"), type.width, pivot) : new Sprite[0],
                    burnt = File.Exists(Sheet("Burnt")) ? SliceSheet(Sheet("Burnt"), type.width, pivot).FirstOrDefault() : null,
                    footRadius = type.radius,
                    weight = type.weight,
                };
                return spec;
            }).Where(spec => spec != null).ToArray();

            theme.ConfigureFerns(specs, FernDensities.GetValueOrDefault(themeName));
        }
    }
}
