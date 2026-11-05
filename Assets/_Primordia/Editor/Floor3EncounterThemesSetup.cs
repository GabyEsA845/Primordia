using System.Linq;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Las 5 salas con tema del piso 3 (paso 6, bloque 1): Pradera de Calamites, Nido de bombarderos, Charca de sapos, Telar de la
    /// araña y Cielo de libélulas. Cada una es una sala de combate del camino con el protagonista del tema y un apoyo, su cueva y su
    /// reparto del escenario; los números se retocan a mano en Floor_3 (Encounter Themes) y el menú los repone.
    /// </summary>
    public static class Floor3EncounterThemesSetup
    {
        const string EnemyPrefabsFolder = "Assets/_Primordia/Prefabs/Enemies";

        [MenuItem("Primordia/Floor 3/Crear salas con tema")]
        static void Create()
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("Floor3EncounterThemesSetup: no existe Floor_3; créalo con Primordia > Floor 3 > Crear piso 3 y escena de prueba.");
                return;
            }

            Enemy Load(string name)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<Enemy>($"{EnemyPrefabsFolder}/{name}.prefab");
                if (enemy == null)
                    Debug.LogWarning($"Floor3EncounterThemesSetup: falta el prefab {name}; créalo con Primordia > Floor 3 > Crear enemigos del piso 3.");
                return enemy;
            }

            var beetle = Load("Enemy_Beetle");
            var toad = Load("Enemy_Toad");
            var medullosa = Load("Enemy_Medullosa");
            var spider = Load("Enemy_Spider");
            var calamites = Load("Enemy_Calamites");
            var dragonfly = Load("Enemy_Dragonfly");

            var themes = new[]
            {
                // Claro abierto: las matas de Calamites mandan y casi no hay helechos ni odres que estorben.
                Theme("Pradera de Calamites", new FloorConfig.ThemedEnemy[] { new(calamites, 4, 3), new(medullosa, 1) }, 1.25f, 3,
                    new FloorConfig.CaveSpec(new Vector2Int(44, 26), CaveGenerator.CaveShape.Oval, lakes: false),
                    fern: 0.4f, methane: 0.5f, bulb: 0.5f, vent: 0.5f, puddle: 0.5f),

                // Los bombarderos prenden todo: muchos odres y helechos, fumarolas, y la araña para tentar al fuego.
                Theme("Nido de bombarderos", new FloorConfig.ThemedEnemy[] { new(beetle, 4, 5), new(spider, 1) }, 1.25f, 3,
                    new FloorConfig.CaveSpec(new Vector2Int(38, 24), CaveGenerator.CaveShape.Oval, lakes: false),
                    fern: 1.3f, methane: 2f, bulb: 0f, vent: 1.5f, puddle: 0.5f),

                // Lodo profundo por todas partes y charcos: los sapos nadan, tú te hundes.
                Theme("Charca de sapos", new FloorConfig.ThemedEnemy[] { new(toad, 4, 5), new(medullosa, 1) }, 1.25f, 3,
                    new FloorConfig.CaveSpec(new Vector2Int(44, 26), CaveGenerator.CaveShape.Oval, lakes: true),
                    fern: 0.7f, methane: 0.7f, bulb: 1f, vent: 0.5f, puddle: 2f),

                // Helechos por todas partes para esconder arañas, y odres para que el fuego corra por las telarañas.
                Theme("Telar de la araña", new FloorConfig.ThemedEnemy[] { new(spider, 4, 5), new(calamites, 1, 1) }, 1.25f, 3,
                    new FloorConfig.CaveSpec(new Vector2Int(38, 24), CaveGenerator.CaveShape.Oval, lakes: false),
                    fern: 2f, methane: 1.5f, bulb: 1f, vent: 0.5f, puddle: 0.5f),

                // Sala alargada y despejada: sin cobertura, hay que leer los picados.
                Theme("Cielo de libélulas", new FloorConfig.ThemedEnemy[] { new(dragonfly, 4, 4), new(medullosa, 1) }, 1.25f, 3,
                    new FloorConfig.CaveSpec(new Vector2Int(50, 18), CaveGenerator.CaveShape.Oval, lakes: false),
                    fern: 0.3f, methane: 0.5f, bulb: 0f, vent: 0.5f, puddle: 1f),
            };

            config.ConfigureEncounterThemes(themes.Where(theme => theme.IsValid).ToArray(), new Vector2Int(2, 4));
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"Floor3EncounterThemesSetup: {config.EncounterThemes.Length} salas con tema en Floor_3 (2 a 4 por partida).");
        }

        // Clave de los dibujos del letrero (docs/multimedia-temp/floor3-room-signs).
        static string SignKeyOf(string name) => name switch
        {
            "Pradera de Calamites" => "CalamitesMeadow",
            "Nido de bombarderos" => "BeetleNest",
            "Charca de sapos" => "ToadPond",
            "Telar de la araña" => "SpiderLoom",
            "Cielo de libélulas" => "DragonflySky",
            _ => "",
        };

        static FloorConfig.EncounterTheme Theme(string name, FloorConfig.ThemedEnemy[] enemies, float multiplier, int minDepth,
            FloorConfig.CaveSpec cave, float fern, float methane, float bulb, float vent, float puddle) => new()
        {
            name = name,
            signKey = SignKeyOf(name),
            enemies = enemies.Where(enemy => enemy.prefab != null).ToArray(),
            enemyMultiplier = multiplier,
            minDepth = minDepth,
            customCave = true,
            cave = cave,
            fernScale = fern,
            methaneScale = methane,
            bulbScale = bulb,
            ventScale = vent,
            puddleScale = puddle,
        };
    }
}
