using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de los niveles y evoluciones (docs/multimedia-temp/evolutions/): Aguijón y Caparazón rehechos (piezas,
    /// iconos y proyectil), efectos de las evoluciones, marcas de estado, nube de tinta que permanece, onda grande,
    /// púa del Erizo e insignias de nivel en la interfaz. Al final configura los niveles de las mutaciones con
    /// <see cref="MutationLevelsSetup.ConfigureLevels"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string EvolutionsSourceFolder = "docs/multimedia-temp/evolutions";
        const string EvolutionEffectsArtFolder = "Assets/_Primordia/Art/Effects/Evolutions";

        // Efectos de las evoluciones, a los ms por cuadro del README.
        static readonly Character EvolutionEffectsArt = new()
        {
            Name = "",
            SourceFolder = EvolutionsSourceFolder + "/Effects",
            ArtFolder = EvolutionEffectsArtFolder,
            ControllerPerClip = true,
            // La tinta no usa Animator: la anima InkCloudArea con sus tres fases.
            SkipClips = new HashSet<string>
            {
                "InkCloud_Start", "InkCloud_Loop", "InkCloud_End", "InkCloud_Large_Start", "InkCloud_Large_Loop", "InkCloud_Large_End",
            },
            Timings = new[]
            {
                ("Heal", 1f / 0.07f, false), ("PoisonBurst", 1f / 0.07f, false), ("Ambush", 1f / 0.09f, true),
                ("Stomp", 1f / 0.06f, false), ("PlatesBreak", 1f / 0.07f, false), ("Plates", 1f / 0.14f, true),
                ("Shockwave_Large", 1f / 0.06f, false), ("LevelUp", 1f / 0.07f, false),
            },
        };

        // Marcas de estado sobre los enemigos, en bucle.
        static readonly Character StatusArt = new()
        {
            Name = "",
            SourceFolder = EvolutionsSourceFolder + "/Status",
            ArtFolder = "Assets/_Primordia/Art/Effects/Status",
            ControllerPerClip = true,
            Timings = new[]
            {
                ("Bleed", 1f / 0.12f, true), ("Blind", 1f / 0.14f, true), ("Stun", 1f / 0.11f, true), ("Paralyzed", 1f / 0.07f, true),
            },
        };

        /// <summary>Mutaciones con arte rehecho en la carpeta de evoluciones (los menús antiguos no deben pisarlo).</summary>
        static readonly HashSet<string> RemadeMutations = new() { "Stinger", "Carapace" };

        /// <summary>Icono rehecho de 16 o 32 px, o null si no está en la carpeta de evoluciones.</summary>
        static Sprite RemadeIcon(string name, int size)
        {
            string source = $"{EvolutionsSourceFolder}/Icons/Icon_{name}_{size}.png";
            return File.Exists(source) ? ImportSprite(source, $"{UIArtFolder}/Icon_{name}_{size}.png", Vector4.zero) : null;
        }

        [MenuItem("Primordia/Art/Importar evoluciones")]
        static void ImportEvolutions()
        {
            ImportRemadeMutations();
            ImportEvolutionProjectiles();
            ImportEvolutionEffects();
            ImportInkClouds();
            ImportLevelBadges();
            AssetDatabase.SaveAssets();

            MutationLevelsSetup.ConfigureLevels();
            Debug.Log("ArtSetup: evoluciones importadas.");
        }

        /// <summary>Aguijón (cola de avispa) y Caparazón (concha de tortuga): las mismas piezas, desde la carpeta nueva.</summary>
        static void ImportRemadeMutations()
        {
            foreach (var name in new[] { "Stinger", "Carapace" })
            {
                var (slot, _, bodyRadius, mouthPixels) = MutationPieces.First(p => p.name == name);
                var piece = MutationPiece(slot, name);
                piece.SourceFolder = $"{EvolutionsSourceFolder}/{slot}";
                var art = Import(piece);

                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_{name}.asset");
                if (mutation == null)
                {
                    Debug.LogWarning($"ArtSetup: no existe Mutation_{name}; créalo con Primordia > Mutations.");
                    continue;
                }

                if (art != null)
                    mutation.SetLook(art.Controller, bodyRadius, mouthPixels / PixelsPerUnit);

                var small = ImportSprite($"{EvolutionsSourceFolder}/Icons/Icon_{name}_16.png", $"{UIArtFolder}/Icon_{name}_16.png", Vector4.zero);
                var large = ImportSprite($"{EvolutionsSourceFolder}/Icons/Icon_{name}_32.png", $"{UIArtFolder}/Icon_{name}_32.png", Vector4.zero);
                if (small != null)
                    mutation.SetIcons(small, large);
                EditorUtility.SetDirty(mutation);
            }
        }

        /// <summary>Proyectil nuevo del Aguijón y la púa del Erizo (crea su prefab si no existe).</summary>
        static void ImportEvolutionProjectiles()
        {
            CombatSetup.CreateProjectilePrefab("Projectile_Quill", new Color(0.6f, 0.35f, 0.8f), new Vector2(0.625f, 0.25f), CoreSetup.CreateSquareSprite());

            var projectiles = new Character
            {
                Name = ProjectilesArt.Name,
                SourceFolder = EvolutionsSourceFolder + "/Projectiles",
                ArtFolder = ProjectilesArt.ArtFolder,
                ControllerPerClip = true,
                CellWidths = new Dictionary<string, int>(ProjectilesArt.CellWidths) { ["Quill"] = 10 },
                SkipClips = ProjectilesArt.SkipClips,
                Timings = ProjectilesArt.Timings.Append(("Quill", 1f / 0.08f, true)).ToArray(),
            };
            var art = Import(projectiles);
            if (art == null)
                return;

            var impact = AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectPrefabsFolder}/Effect_Impact_Player.prefab");
            foreach (var name in new[] { "Stinger", "Quill" })
            {
                if (!art.Controllers.TryGetValue(name, out var controller))
                    continue;

                ApplyToProjectilePrefab($"Projectile_{name}", controller, art.FirstSprites[name]);
                EditPrefab($"{ProjectilePrefabsFolder}/Projectile_{name}.prefab", root => root.GetComponent<Projectile>().SetImpactEffect(impact));
            }
        }

        /// <summary>Prefabs de los efectos de las evoluciones y de las marcas de estado.</summary>
        static void ImportEvolutionEffects()
        {
            var effects = Import(EvolutionEffectsArt);
            var status = Import(StatusArt);
            Directory.CreateDirectory(EffectPrefabsFolder);

            if (effects != null)
            {
                // Sobre el jugador (10) y sus piezas (hasta 12); el pisotón, en el suelo, bajo los personajes.
                CreateEffectPrefab("Effect_Heal", effects, "Heal", sortingOrder: 13);
                CreateEffectPrefab("Effect_PoisonBurst", effects, "PoisonBurst", sortingOrder: 15);
                CreateEffectPrefab("Effect_Ambush", effects, "Ambush", sortingOrder: 13);
                CreateEffectPrefab("Effect_Stomp", effects, "Stomp", sortingOrder: 5);
                CreateEffectPrefab("Effect_Plates", effects, "Plates", sortingOrder: 13);
                CreateEffectPrefab("Effect_PlatesBreak", effects, "PlatesBreak", sortingOrder: 13);
                CreateEffectPrefab("Effect_Shockwave_Large", effects, "Shockwave_Large", sortingOrder: 15);
                CreateEffectPrefab("Effect_LevelUp", effects, "LevelUp", sortingOrder: 14);
                CreateEffectPrefab("Effect_LevelUp_Gold", effects, "LevelUp_Gold", sortingOrder: 14);
            }

            if (status != null)
            {
                // Encima de los enemigos y de sus proyectiles.
                CreateEffectPrefab("Effect_Status_Bleed", status, "Bleed", sortingOrder: 22);
                CreateEffectPrefab("Effect_Status_Blind", status, "Blind", sortingOrder: 22);
                CreateEffectPrefab("Effect_Status_Stun", status, "Stun", sortingOrder: 22);
                CreateEffectPrefab("Effect_Status_Paralyzed", status, "Paralyzed", sortingOrder: 22);
                CreateEffectPrefab("Effect_Status_ParalyzedBody", status, "Paralyzed_Body", sortingOrder: 21);
            }
        }

        /// <summary>
        /// Nube de tinta en tres fases (aparece, se mantiene y se disipa), más transparente, a radio 3 y 4. La anima
        /// <see cref="InkCloudArea"/>, que también ciega y daña mientras dura.
        /// </summary>
        static void ImportInkClouds()
        {
            ImportInkCloud("Effect_InkCloud", "InkCloud", 96);
            ImportInkCloud("Effect_InkCloud_Large", "InkCloud_Large", 128);

            var ink = AssetDatabase.LoadAssetAtPath<AbilityMutation>($"{MutationDataFolder}/Mutation_InkSac.asset");
            if (ink != null)
            {
                ink.SetEffectRadius(3f);
                EditorUtility.SetDirty(ink);
            }
        }

        static void ImportInkCloud(string prefabName, string sheet, int cell)
        {
            Sprite[] Frames(string phase)
            {
                string target = $"{EvolutionEffectsArtFolder}/{sheet}_{phase}.png";
                return CopyArt($"{EvolutionsSourceFolder}/Effects/{sheet}_{phase}.png", target) ? SliceSheet(target, cell) : System.Array.Empty<Sprite>();
            }

            var start = Frames("Start");
            var loop = Frames("Loop");
            var end = Frames("End");
            if (loop.Length == 0)
                return;

            string path = $"{MutationPrefabsFolder}/{prefabName}.prefab";
            // El orden de dibujo de la nube de antes, si existía.
            var old = AssetDatabase.LoadAssetAtPath<GameObject>($"{MutationPrefabsFolder}/Effect_InkCloud.prefab");
            int order = old != null && old.TryGetComponent<SpriteRenderer>(out var oldRenderer) ? oldRenderer.sortingOrder : 15;

            EditPrefab(path, root =>
            {
                foreach (var component in new Component[]
                         {
                             root.GetComponent<Animator>(), root.GetComponent<OneShotEffect>(), root.GetComponent<FadeOutEffect>(),
                         })
                {
                    if (component != null)
                        Object.DestroyImmediate(component);
                }

                root.transform.localScale = Vector3.one;
                var renderer = GetOrAdd<SpriteRenderer>(root);
                renderer.sprite = start.Length > 0 ? start[0] : loop[0];
                renderer.color = Color.white;
                renderer.sortingOrder = order;
                GetOrAdd<InkCloudArea>(root).ConfigureLook(start, 0.09f, loop, 0.15f, end, 0.11f);
            }, create: true);
        }

        /// <summary>Insignias de nivel en el HUD, la pausa y las tarjetas, y la flechita de las mejoras.</summary>
        static void ImportLevelBadges()
        {
            Sprite Badge(string file) => ImportSprite($"{EvolutionsSourceFolder}/UI/{file}.png", $"{UIArtFolder}/{file}.png", Vector4.zero);
            var small = new[] { Badge("UI_Level_1"), Badge("UI_Level_2"), Badge("UI_Level_3") };
            var large = new[] { Badge("UI_Level_Large_1"), Badge("UI_Level_Large_2"), Badge("UI_Level_Large_3") };
            var arrow = Badge("UI_Level_Up");

            EditPrefab(UISetup.GameUIPath, root =>
            {
                foreach (var slots in root.GetComponentsInChildren<MutationSlotsDisplay>(true))
                    slots.SetLevelBadges(small);
                foreach (var callout in root.GetComponentsInChildren<SlotCallout>(true))
                    callout.SetLevelBadges(small);
                foreach (var card in root.GetComponentsInChildren<MutationCard>(true))
                    card.SetLevelSprites(large, arrow);
            });
        }
    }
}
