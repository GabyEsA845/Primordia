using System.Collections.Generic;
using System.IO;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Enemigos y trampas del piso 3 (GDD, tabla de enemigos del pantano carbonífero), uno a uno según se programan. Importa su arte
    /// (docs/multimedia-temp/floor3-enemies, ritmos de su README), crea sus datos y su prefab con su cerebro (<see cref="EnemyBrain"/>)
    /// y los pone en Floor_3 con su coste y la sala desde la que salen (<see cref="Floor3Roster"/>). Rehace el prefab cada vez.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor3EnemiesSourceFolder = "docs/multimedia-temp/floor3-enemies";
        const string Floor3EnemiesArtFolder = CharactersFolder + "/Enemies/Floor3";
        const string Floor3EnemyDataFolder = "Assets/_Primordia/Data/Enemies/Floor3";

        /// <summary>Prefab, coste, sala de combate desde la que salen, tope por sala (0 = sin tope) y cuántos salen juntos.</summary>
        static readonly (string prefab, int cost, int minDepth, int maxPerRoom, int groupSize)[] Floor3Roster =
        {
            ("Enemy_Beetle", 3, 1, 0, 1),
        };

        [MenuItem("Primordia/Floor 3/Crear enemigos del piso 3")]
        static void CreateFloor3Enemies()
        {
            Directory.CreateDirectory(Floor3EnemyDataFolder);
            Directory.CreateDirectory(EnemyPrefabsFolder);

            CreateBeetle();

            AssetDatabase.SaveAssets();
            var entries = new List<FloorConfig.EnemyEntry>();
            foreach (var (prefab, cost, minDepth, maxPerRoom, groupSize) in Floor3Roster)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<Enemy>($"{EnemyPrefabsFolder}/{prefab}.prefab");
                if (enemy != null)
                    entries.Add(new FloorConfig.EnemyEntry(enemy, cost, minDepth, maxPerRoom, groupSize));
            }
            PutInFloor3(entries.ToArray());
            Debug.Log($"ArtSetup: {entries.Count} enemigos del piso 3 creados y puestos en Floor_3.");
        }

        // ---------- Escarabajo bombardero ----------

        static Enemy CreateBeetle()
        {
            // Pivote a la altura de las patas (el cuerpo ocupa de la fila 9 a la 21 de 32 contando desde arriba).
            var character = Floor3Enemy("Beetle",
                ("Idle", Fps(180), true), ("Move", Fps(100), true), ("Turn", Fps(70), false), ("Warn", Fps(150), false),
                ("Spray", Fps(70), true), ("Hurt", Fps(90), false), ("Death", Fps(120), false));
            character.Pivot = new Vector2(0.5f, 0.34f);
            var art = Import(character);
            if (art == null)
                return null;

            // El cono nace en su borde izquierdo a media altura y va a la derecha: el cerebro lo gira hacia el objetivo.
            string effects = $"{Floor3EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            var spray = Floor2MouthsSetup.CreateLoopEffect("Effect_BeetleSpray",
                ImportLooseSheet($"{Floor3EnemiesSourceFolder}/Effects/Fx_BeetleSpray.png", $"{effects}/Fx_BeetleSpray.png", 56, new Vector2(0f, 0.5f)),
                0.06f, sortingOrder: 7);

            // 12 de vida, anda despacio, te ve a 8 u y hace medio corazón al tocarlo.
            var data = Floor3EnemyData("Enemy_Beetle", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 12, moveSpeed: 2.2f, detectionRange: 8f, contactDamage: 1));
            return BuildFloor2Enemy("Enemy_Beetle", data, art, colliderRadius: 0.5f,
                root => root.AddComponent<BeetleBrain>().Configure(spray), colliderOffset: new Vector2(0f, 0.4f));
        }

        // ---------- Ayudas ----------

        static Character Floor3Enemy(string name, params (string, float, bool)[] timings) => new()
        {
            Name = name,
            SourceFolder = $"{Floor3EnemiesSourceFolder}/{name}",
            ArtFolder = $"{Floor3EnemiesArtFolder}/{name}",
            DefaultState = "Move",
            Timings = timings,
        };

        /// <summary>Los datos del enemigo: se crean si no existen y se vuelven a configurar siempre.</summary>
        static EnemyData Floor3EnemyData(string assetName, System.Action<EnemyData> configure)
        {
            string path = $"{Floor3EnemyDataFolder}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, path);
            }
            configure(data);
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Los enemigos de Floor_3 pasan a ser estos. Si el piso aún no tenía presupuesto (sin enemigos), le da el del piso 2.</summary>
        static void PutInFloor3(FloorConfig.EnemyEntry[] entries)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("ArtSetup: no existe Floor_3; créalo con Primordia > Floor 3 > Crear piso 3 y escena de prueba.");
                return;
            }
            if (entries.Length == 0)
                return;
            if (config.BaseBudget <= 0f)
                config.ConfigureEnemies(entries, config.Boss, newBaseBudget: 3f, newBudgetPerRoom: 1.4f);
            else
                config.SetEnemies(entries);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
