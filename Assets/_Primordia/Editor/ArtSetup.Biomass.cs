using Primordia.Biomass;
using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Biomasa (docs/multimedia-temp/ui/Items y HUD): importa sus animaciones, crea las dos piezas del suelo y
    /// el efecto al recogerla, pone las probabilidades de cada enemigo, da al jugador su <see cref="PlayerBiomass"/>,
    /// al piso las piezas que sueltan las salas y al HUD el contador.
    /// </summary>
    public static partial class ArtSetup
    {
        const string BiomassArtFolder = ItemsArtFolder + "/Biomass";
        const string ItemPrefabsFolder = "Assets/_Primordia/Prefabs/Items";

        // Item_Biomass_Idle.png → Idle. 150 ms por cuadro en reposo, 60 al recogerla.
        static readonly Character BiomassArt = new()
        {
            Name = "Item_Biomass",
            SourceFolder = UISourceFolder + "/Items",
            ArtFolder = BiomassArtFolder,
            ControllerPerClip = true,
            Timings = new[] { ("Idle", 1f / 0.15f, true), ("Big_Idle", 1f / 0.15f, true), ("Collect", 1f / 0.06f, false) },
        };

        [MenuItem("Primordia/Art/Importar biomasa")]
        static void ImportBiomass()
        {
            var art = Import(BiomassArt);
            if (art == null)
                return;

            System.IO.Directory.CreateDirectory(EffectPrefabsFolder);
            System.IO.Directory.CreateDirectory(ItemPrefabsFolder);
            var collect = CreateEffectPrefab("Effect_BiomassCollect", art, "Collect", sortingOrder: 12);
            var small = CreateBiomassPrefab("Biomass_Small", art, "Idle", RoomBiomass.SmallValue, collect);
            var big = CreateBiomassPrefab("Biomass_Big", art, "Big_Idle", RoomBiomass.BigValue, collect);
            AssetDatabase.SaveAssets();

            ConfigureEnemyBiomass();

            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(FloorSetup.FloorConfigPath);
            if (config != null && small != null && big != null)
            {
                config.ConfigureBiomass(small.GetComponent<BiomassPickup>(), big.GetComponent<BiomassPickup>(), newMinimum: 2);
                EditorUtility.SetDirty(config);
            }
            else
            {
                Debug.LogWarning("ArtSetup: falta Floor_1 (créalo con Primordia > Floor) o las piezas de biomasa; las salas no soltarán biomasa.");
            }

            EditPrefab(PlayerSetup.PlayerPath, root => GetOrAdd<PlayerBiomass>(root));

            UISetup.EnsureBiomassCounter();
            var icon = ImportUISprite("HUD", "UI_Biomass");
            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var counter in root.GetComponentsInChildren<BiomassCounter>(true))
                    counter.SetIcon(icon);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("ArtSetup: biomasa importada (piezas, efecto, enemigos, jugador, piso y HUD).");
        }

        /// <summary>Pieza de biomasa en el suelo: sprite animado, por debajo del jugador y de las burbujas de mutación.</summary>
        static GameObject CreateBiomassPrefab(string prefabName, ImportedArt art, string clip, int value, GameObject collectEffect)
        {
            if (!art.Controllers.TryGetValue(clip, out var controller))
            {
                Debug.LogWarning($"ArtSetup: falta la hoja Item_Biomass_{clip}.png; no se crea {prefabName}.");
                return null;
            }

            return EditPrefab($"{ItemPrefabsFolder}/{prefabName}.prefab", root =>
            {
                SetAnimatedSprite(root, controller, art.FirstSprites[clip], sortingOrder: 5);
                GetOrAdd<BiomassPickup>(root).Configure(value, collectEffect);
            }, create: true);
        }

        /// <summary>
        /// Probabilidades del GDD: perseguidor y tirador 40 % pequeña y 10 % grande; crías 10 % pequeña;
        /// el jefe, 10 fijos (sus invocaciones no sueltan nada).
        /// </summary>
        static void ConfigureEnemyBiomass()
        {
            foreach (var (name, smallChance, bigChance, fixedValue) in new[]
                     {
                         ("Enemy_Chaser", 0.4f, 0.1f, 0), ("Enemy_Shooter", 0.4f, 0.1f, 0),
                         ("Enemy_Small", 0.1f, 0f, 0), ("Enemy_VirusBoss", 0f, 0f, 10),
                     })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemySetup.DataFolder}/{name}.asset");
                if (data == null)
                {
                    Debug.LogWarning($"ArtSetup: no existe {name}; créalo con Primordia > Enemies.");
                    continue;
                }

                data.ConfigureBiomass(smallChance, bigChance, fixedValue);
                EditorUtility.SetDirty(data);
            }
        }
    }
}
