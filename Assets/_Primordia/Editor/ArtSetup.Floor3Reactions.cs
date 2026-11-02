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
