using Primordia.Enemies;
using Primordia.Rooms;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Arte de la ameba gigante (docs/multimedia-temp/enemies/BossAmoeba y BossAmoebaHalf, generado con
    /// generar_ameba_gigante.py): animaciones del cuerpo grande y de las medianas, proyectiles de la lluvia final,
    /// candado de la barra del jefe y el tema Amoeba de su sala. Lo usa <see cref="AmoebaBossSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string BossAmoebaSourceFolder = EnemiesSourceFolder + "/BossAmoeba";
        internal const string AmoebaThemeName = "Amoeba";

        // Duraciones pensadas para AmoebaBoss: enfado 1 s, fusión 1 s, furia 2 s (en bucle), agotamiento 1 s.
        static readonly Character BossAmoebaArt = new()
        {
            Name = "BossAmoeba",
            SourceFolder = BossAmoebaSourceFolder,
            ArtFolder = CharactersFolder + "/Enemies/BossAmoeba",
            DefaultState = AmoebaBoss.CalmState,
            Timings = new[]
            {
                ("Angry_Transition", 8f, false), ("Angry", 8f, true), ("Idle", 6f, true), ("Hurt", 10f, false), ("Aim", 8f, true),
                ("Split", 8f, false), ("Merge", 8f, false), ("Rage", 4f, true), ("Cast_1", 8f, true), ("Cast_2", 10f, true),
                ("Cast_3", 12f, true), ("Exhaust", 8f, false), ("Tired", 4f, true), ("Death", 10f, false),
                ("Intro", IntroPoseFps, false),
            },
        };

        static readonly Character BossAmoebaHalfArt = new()
        {
            Name = "BossAmoebaHalf",
            SourceFolder = EnemiesSourceFolder + "/BossAmoebaHalf",
            ArtFolder = CharactersFolder + "/Enemies/BossAmoebaHalf",
            DefaultState = AmoebaBoss.HalfMoveState,
            Timings = new[] { ("Move", 10f, true), ("Hurt", 10f, false), ("Aim", 8f, true) },
        };

        // En su propia carpeta: así no se vuelven a importar (con otro ritmo) los demás proyectiles.
        static readonly Character AmoebaProjectilesArt = new()
        {
            Name = "Projectile",
            SourceFolder = BossAmoebaSourceFolder + "/projectiles",
            ArtFolder = ProjectilesArtFolder + "/Amoeba",
            ControllerPerClip = true,
            Timings = new[] { ("Amoeba", 8f, true) },
        };

        /// <summary>Animaciones de la ameba gigante, ya importadas.</summary>
        internal sealed class AmoebaArt
        {
            public RuntimeAnimatorController Big;
            public Sprite BigSprite;
            public float DeathLength;
            public RuntimeAnimatorController Half;
            public Sprite HalfSprite;
        }

        /// <summary>Importa las hojas de la ameba y sus proyectiles. Null si no hay hojas del cuerpo grande.</summary>
        internal static AmoebaArt ImportBossAmoebaArt()
        {
            var big = Import(BossAmoebaArt);
            if (big == null)
                return null;
            var half = Import(BossAmoebaHalfArt);

            var projectiles = Import(AmoebaProjectilesArt);
            if (projectiles != null)
            {
                foreach (var (name, controller) in projectiles.Controllers)
                    ApplyToProjectilePrefab($"Projectile_{name}", controller, projectiles.FirstSprites[name]);
            }

            return new AmoebaArt
            {
                Big = big.Controller,
                BigSprite = big.DefaultSprite,
                DeathLength = big.Length(AmoebaBoss.DeathState),
                Half = half?.Controller,
                HalfSprite = half?.DefaultSprite,
            };
        }

        /// <summary>Importa el tema de la sala de la ameba (Theme_Amoeba). Null si no hay tiles.</summary>
        internal static RoomTheme ImportAmoebaTheme()
        {
            var missing = CreateMissingTile();
            if (!ImportTheme(AmoebaThemeName, missing))
                return null;
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<RoomTheme>($"{EnvironmentArtFolder}/{AmoebaThemeName}/Theme_{AmoebaThemeName}.asset");
        }

        /// <summary>Pone el candado (Icon_Lock) a la barra del jefe del prefab de interfaz.</summary>
        internal static void ApplyBossBarLock()
        {
            var sprite = ImportUISprite("BossBar", "Icon_Lock");
            if (sprite == null)
                return;

            EditPrefab(UISetup.GameUIPath, root =>
            {
                var bar = root.GetComponent<BossHealthBar>();
                var lockObject = bar != null && bar.Root != null ? bar.Root.transform.Find("Lock") : null;
                if (lockObject != null)
                    lockObject.GetComponent<Image>().sprite = sprite;
            });
        }
    }
}
