using System.IO;
using Primordia.Enemies;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Presentación de los jefes (docs/multimedia-temp/boss-intros, generado con generar_presentaciones.py): la pose
    /// "Intro" de cada jefe (el generador la deja junto a sus demás hojas), los retratos y los nombres rotulados del
    /// rótulo, la fuente Jersey 10 con su licencia, un <see cref="BossIntroData"/> por jefe y el componente
    /// <see cref="BossIntro"/> en su prefab. Los dos jefes del piso 1 y el Anomalocaris.
    /// </summary>
    public static partial class ArtSetup
    {
        const string BossIntroSourceFolder = "docs/multimedia-temp/boss-intros";
        const string BossIntroArtFolder = "Assets/_Primordia/Art/UI/BossIntro";
        internal const string BossIntroDataFolder = "Assets/_Primordia/Data/Bosses";
        const string FontsArtFolder = "Assets/_Primordia/Art/Fonts";

        // La pose dura 8 cuadros de 130 ms; el último se queda mientras se ve el rótulo.
        const float IntroPoseFps = 1f / 0.13f;

        // Banda y filo de cada piso: granate y rosa en la sopa primordial, azul y turquesa en el océano.
        static readonly Color Floor1Band = new(30 / 255f, 14 / 255f, 26 / 255f);
        static readonly Color Floor1Accent = new(1f, 122 / 255f, 168 / 255f);
        static readonly Color Floor2Band = new(8 / 255f, 16 / 255f, 30 / 255f);
        static readonly Color Floor2Accent = new(120 / 255f, 220 / 255f, 210 / 255f);

        static readonly (string key, string prefab, string title, Color band, Color accent)[] BossIntros =
        {
            ("Virus", "Boss_Virus", "Plaga primigenia", Floor1Band, Floor1Accent),
            ("Amoeba", "Boss_Amoeba", "La que todo lo engulle", Floor1Band, Floor1Accent),
            ("Anomalocaris", "Boss_Anomalocaris", "Terror del Cámbrico", Floor2Band, Floor2Accent),
        };

        [MenuItem("Primordia/Art/Importar presentaciones de jefes")]
        static void ImportBossIntros()
        {
            // Las poses entran como un clip más de cada jefe: se vuelven a importar sus animaciones.
            var virus = Import(BossVirusArt);
            if (virus != null)
                ApplyToBossPrefab(virus);
            var amoeba = ImportBossAmoebaArt();
            if (amoeba != null)
                AmoebaBossSetup.ApplyArt(amoeba);

            foreach (var file in new[] { "Jersey10-Regular.ttf", "Jersey10-OFL.txt" })
                CopyArt($"{BossIntroSourceFolder}/fuentes/{file}", $"{FontsArtFolder}/{file}");

            foreach (var intro in BossIntros)
                ImportBossIntro(intro);

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: presentaciones de los jefes importadas (virus, ameba gigante y Anomalocaris).");
        }

        /// <summary>La presentación de un jefe (por su clave: "Anomalocaris"), si su prefab ya existe.</summary>
        internal static void ImportBossIntro(string key)
        {
            foreach (var intro in BossIntros)
            {
                if (intro.key == key)
                    ImportBossIntro(intro);
            }
            AssetDatabase.SaveAssets();
        }

        static void ImportBossIntro((string key, string prefab, string title, Color band, Color accent) intro)
        {
            var (key, prefab, title, band, accent) = intro;
            Directory.CreateDirectory(BossIntroArtFolder);
            Directory.CreateDirectory(BossIntroDataFolder);
            var portrait = ImportSprite($"{BossIntroSourceFolder}/Portraits/Portrait_{key}.png",
                $"{BossIntroArtFolder}/Portrait_{key}.png", Vector4.zero);
            var name = ImportSprite($"{BossIntroSourceFolder}/Titles/Title_{key}.png",
                $"{BossIntroArtFolder}/Title_{key}.png", Vector4.zero);
            if (portrait == null || name == null)
            {
                Debug.LogWarning($"ArtSetup: falta el retrato o el nombre de {key} en {BossIntroSourceFolder}; " +
                    "lanza generar_presentaciones.py y vuelve a ejecutar el menú.");
                return;
            }

            string dataPath = $"{BossIntroDataFolder}/BossIntro_{key}.asset";
            var data = AssetDatabase.LoadAssetAtPath<BossIntroData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<BossIntroData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }
            data.Configure(name, title, portrait, band, accent);
            EditorUtility.SetDirty(data);

            string prefabPath = $"{EnemyPrefabsFolder}/{prefab}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {prefabPath}; crea antes el jefe con su menú de Primordia > Boss.");
                return;
            }
            EditPrefab(prefabPath, root => SetFields(GetOrAdd<BossIntro>(root), ("data", data)));
        }
    }
}
