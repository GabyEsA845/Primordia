using System.Collections.Generic;
using System.IO;
using Primordia.Combat;
using Primordia.Core;
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

        /// <summary>
        /// Prefab, coste, sala de combate desde la que salen, tope por sala (0 = sin tope), cuántos salen juntos y si solo salen en
        /// salas con lodo profundo, junto a él, y si es una planta (solo sobre tierra, no sobre charcos).
        /// </summary>
        static readonly (string prefab, int cost, int minDepth, int maxPerRoom, int groupSize, bool needsMud, bool dryGround)[] Floor3Roster =
        {
            ("Enemy_Beetle", 3, 1, 0, 1, false, false),
            ("Enemy_Toad", 3, 1, 3, 1, true, false),
            ("Enemy_Medullosa", 4, 2, 2, 1, false, true),
            ("Enemy_Calamites", 5, 3, 1, 1, false, true),
        };

        [MenuItem("Primordia/Floor 3/Crear enemigos del piso 3")]
        static void CreateFloor3Enemies()
        {
            Directory.CreateDirectory(Floor3EnemyDataFolder);
            Directory.CreateDirectory(EnemyPrefabsFolder);

            CreateBeetle();
            CreateToad();
            CreateMedullosa();
            CreateCalamites();

            AssetDatabase.SaveAssets();
            var entries = new List<FloorConfig.EnemyEntry>();
            foreach (var (prefab, cost, minDepth, maxPerRoom, groupSize, needsMud, dryGround) in Floor3Roster)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<Enemy>($"{EnemyPrefabsFolder}/{prefab}.prefab");
                if (enemy != null)
                    entries.Add(new FloorConfig.EnemyEntry(enemy, cost, minDepth, maxPerRoom, groupSize, needsMud, dryGround));
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

        // ---------- Sapo de fango ----------

        static Enemy CreateToad()
        {
            // Pivote a la altura de las patas (3 px sobre el borde de abajo de un cuadro de 32).
            var character = Floor3Enemy("Toad",
                ("Idle", Fps(180), true), ("Hop", Fps(90), true), ("Spit", Fps(90), false), ("Inflate", Fps(120), true),
                ("Dive", Fps(90), false), ("Emerge", Fps(90), false), ("Swim", Fps(160), true), ("Hurt", Fps(90), false),
                ("Death", Fps(120), false));
            character.DefaultState = "Idle";
            character.Pivot = new Vector2(0.5f, 0.1f);
            var art = Import(character);
            if (art == null)
                return null;

            string effects = $"{Floor3EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            string source = $"{Floor3EnemiesSourceFolder}/Effects";
            var splat = Floor2MouthsSetup.CreateFrameEffect("Effect_ToadMud_Splat",
                ImportLooseSheet($"{source}/Fx_ToadMud_Splat.png", $"{effects}/Fx_ToadMud_Splat.png", 24), 0.07f);
            var burst = Floor2MouthsSetup.CreateFrameEffect("Effect_Toad_Burst",
                ImportLooseSheet($"{source}/Fx_Toad_Burst.png", $"{effects}/Fx_Toad_Burst.png", 48), 0.07f);
            var shadowSprite = ImportLooseSheet($"{source}/Fx_FlyerShadow.png", $"{effects}/Fx_FlyerShadow.png", 24);
            var ballFrames = ImportLooseSheet($"{source}/Proj_ToadMud.png", $"{effects}/Proj_ToadMud.png", 12);
            var puddle = SludgePuddleFrames();
            var ball = CreateToadMudBall(ballFrames, shadowSprite, splat, puddle);

            // 10 de vida, salta despacio, te ve a 9 u y hace medio corazón al tocarlo. Nada: no se hunde en el lodo.
            var data = Floor3EnemyData("Enemy_Toad", d =>
            {
                d.Configure(EnemyBehaviour.Chaser, maxHealth: 10, moveSpeed: 3f, detectionRange: 9f, contactDamage: 1);
                d.ConfigureSwimming(true);
            });
            return BuildFloor2Enemy("Enemy_Toad", data, art, colliderRadius: 0.45f,
                root => root.AddComponent<ToadBrain>().Configure(ball, burst, puddle, 0.26f), colliderOffset: new Vector2(0f, 0.4f));
        }

        /// <summary>Cuadros del charco de lodo del escenario (el salpicado, el más pequeño): el que deja el sapo.</summary>
        static Sprite[] SludgePuddleFrames()
        {
            string path = $"{Floor3ArtRoot}/floor3-environment/Swamp/props/Puddle_Sludge_Splash.png";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"ArtSetup: falta {path}; el sapo no dejará charco (Primordia > Floor 3 > Importar todo el arte del piso 3).");
                return new Sprite[0];
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out _);
            return SliceSheet(path, width / 4, new Vector2(0.5f, 0.5f));
        }

        /// <summary>Bola de lodo del sapo: la sombra pegada al suelo y el cuerpo de la bola, que sube y baja con el arco. Se rehace.</summary>
        static ToadMudBall CreateToadMudBall(Sprite[] ballFrames, Sprite[] shadow, GameObject splat, Sprite[] puddle)
        {
            var root = new GameObject("Projectile_ToadMud");
            try
            {
                var shadowObject = new GameObject("Shadow");
                shadowObject.transform.SetParent(root.transform, false);
                var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = shadow.Length > 0 ? shadow[0] : null;
                shadowRenderer.sortingOrder = 3;

                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = ballFrames.Length > 0 ? ballFrames[0] : null;
                renderer.sortingOrder = 20;
                if (ballFrames.Length > 1)
                    body.AddComponent<SpriteLoop>().Configure(renderer, ballFrames, 0.09f, 0f);

                var ball = root.AddComponent<ToadMudBall>();
                ball.Configure(body.transform, shadowObject.transform, splat, puddle, 0.26f);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilePrefabsFolder}/Projectile_ToadMud.prefab").GetComponent<ToadMudBall>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Medullosa ----------

        static Enemy CreateMedullosa()
        {
            // Cuadros de 32×40 (la muerte, de 40×48). Pivote al pie: el dibujo acaba 2 px sobre el borde de abajo.
            var character = Floor3Enemy("Medullosa",
                ("Idle", Fps(180), true), ("Aim", Fps(150), false), ("Shoot", Fps(70), false), ("Hurt", Fps(90), false),
                ("Death", Fps(120), false));
            character.DefaultState = "Idle";
            character.Pivot = new Vector2(0.5f, 0.05f);
            character.CellWidths = new Dictionary<string, int> { ["Idle"] = 32, ["Aim"] = 32, ["Shoot"] = 32, ["Hurt"] = 32, ["Death"] = 40 };
            var art = Import(character);
            if (art == null)
                return null;

            string effects = $"{Floor3EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            string source = $"{Floor3EnemiesSourceFolder}/Effects";
            var burst = Floor2MouthsSetup.CreateFrameEffect("Effect_MedullosaSeed_Burst",
                ImportLooseSheet($"{source}/Fx_MedullosaSeed_Burst.png", $"{effects}/Fx_MedullosaSeed_Burst.png", 24), 0.07f);
            var marker = Floor2MouthsSetup.CreateLoopEffect("Effect_MedullosaSeed_Target",
                ImportLooseSheet($"{source}/Fx_MedullosaSeed_Target.png", $"{effects}/Fx_MedullosaSeed_Target.png", 24), 0.12f, sortingOrder: 2);
            var podFrames = ImportLooseSheet($"{source}/Proj_MedullosaSeed.png", $"{effects}/Proj_MedullosaSeed.png", 12);
            var shadow = ImportLooseSheet($"{source}/Fx_FlyerShadow.png", $"{effects}/Fx_FlyerShadow.png", 24);
            var seed = CreateMedullosaSeed(podFrames, shadow, burst);

            // 16 de vida, inmóvil, te ve a 15 u (el alcance de su mortero) y no hace daño al tocarla (solo sus vainas).
            var data = Floor3EnemyData("Enemy_Medullosa", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 16, moveSpeed: 0f, detectionRange: 15f, contactDamage: 0));
            return BuildFloor2Enemy("Enemy_Medullosa", data, art, colliderRadius: 0.6f,
                root => root.AddComponent<MedullosaBrain>().Configure(seed, marker), colliderOffset: new Vector2(0f, 0.7f));
        }

        /// <summary>Vaina de la Medullosa: sombra al suelo y cuerpo que sube y baja con el arco, dando vueltas. Se rehace.</summary>
        static MedullosaSeed CreateMedullosaSeed(Sprite[] podFrames, Sprite[] shadow, GameObject burst)
        {
            var root = new GameObject("Projectile_MedullosaSeed");
            try
            {
                var shadowObject = new GameObject("Shadow");
                shadowObject.transform.SetParent(root.transform, false);
                var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = shadow.Length > 0 ? shadow[0] : null;
                shadowRenderer.sortingOrder = 3;

                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = podFrames.Length > 0 ? podFrames[0] : null;
                renderer.sortingOrder = 20;
                if (podFrames.Length > 1)
                    body.AddComponent<SpriteLoop>().Configure(renderer, podFrames, 0.09f, 0f);

                var seed = root.AddComponent<MedullosaSeed>();
                seed.Configure(body.transform, shadowObject.transform, burst);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilePrefabsFolder}/Projectile_MedullosaSeed.prefab").GetComponent<MedullosaSeed>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Calamites ----------

        static Enemy CreateCalamites()
        {
            // El retoño primero: la madre lo lleva en su cerebro. No está en la tabla del piso: solo lo echa la madre.
            var sprout = CreateCalamitesSprout();

            // Cuadros de 32×48 (la muerte, de 48×56). Pivote al pie: el dibujo acaba 1–2 px sobre el borde de abajo.
            var character = Floor3Enemy("Calamites",
                ("Idle", Fps(180), true), ("Spawn", Fps(110), false), ("Spores", Fps(110), false), ("Hurt", Fps(90), false),
                ("Death", Fps(120), false));
            character.DefaultState = "Idle";
            character.Pivot = new Vector2(0.5f, 0.03f);
            character.CellWidths = new Dictionary<string, int> { ["Idle"] = 32, ["Spawn"] = 32, ["Spores"] = 32, ["Hurt"] = 32, ["Death"] = 48 };
            var art = Import(character);
            if (art == null || sprout == null)
                return null;

            // Espora del anillo: lenta, 1 de daño, se deshace al chocar.
            string effects = $"{Floor3EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            string source = $"{Floor3EnemiesSourceFolder}/Effects";
            var pop = Floor2MouthsSetup.CreateFrameEffect("Effect_CalamitesSpore_Pop",
                ImportLooseSheet($"{source}/Fx_CalamitesSpore_Pop.png", $"{effects}/Fx_CalamitesSpore_Pop.png", 12), 0.07f);
            var spore = CreateEnemyProjectile("Projectile_CalamitesSpore",
                ImportLooseSheet($"{source}/Proj_CalamitesSpore.png", $"{effects}/Proj_CalamitesSpore.png", 8), radius: 0.2f, pop);
            var attack = CombatSetup.LoadAttack("Attack_CalamitesSpore");
            if (attack == null)
                attack = CombatSetup.CreateAttack("Attack_CalamitesSpore", spore, 1f / 7f, 1, 3f, 3.5f);
            attack.Configure(spore, fireRate: 1f / 7f, damage: 1, speed: 3f, lifetime: 3.5f);
            EditorUtility.SetDirty(attack);

            // 30 de vida, inmóvil, te ve a 11 u y no hace daño al tocarla (sus retoños y esporas sí).
            var data = Floor3EnemyData("Enemy_Calamites", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 30, moveSpeed: 0f, detectionRange: 11f, contactDamage: 0));
            return BuildFloor2Enemy("Enemy_Calamites", data, art, colliderRadius: 0.7f, root =>
            {
                // Las esporas salen de los estróbilos, en lo alto de las cañas.
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(root.transform, false);
                muzzle.localPosition = new Vector3(0f, 2f, 0f);
                CombatSetup.ConfigureWeapon(root.AddComponent<Weapon>(), attack, Team.Enemy, muzzle);
                root.AddComponent<CalamitesBrain>().Configure(sprout);
            }, colliderOffset: new Vector2(0f, 0.8f));
        }

        static CalamitesSproutBrain CreateCalamitesSprout()
        {
            // Cuadros de 16×24; pivote al pie.
            var character = Floor3Enemy("CalamitesSprout",
                ("Emerge", Fps(90), false), ("Idle", Fps(180), true), ("Move", Fps(100), true), ("Wither", Fps(140), false),
                ("Hurt", Fps(90), false), ("Death", Fps(120), false));
            character.DefaultState = "Idle";
            character.Pivot = new Vector2(0.5f, 0.04f);
            character.CellWidths = new Dictionary<string, int>
            {
                ["Emerge"] = 16, ["Idle"] = 16, ["Move"] = 16, ["Wither"] = 16, ["Hurt"] = 16, ["Death"] = 16
            };
            var art = Import(character);
            if (art == null)
                return null;

            // 3 de vida, rápido, siempre sabe dónde estás y hace medio corazón al tocarte.
            var data = Floor3EnemyData("Enemy_CalamitesSprout", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 3, moveSpeed: 3.4f, detectionRange: 30f, contactDamage: 1));
            var enemy = BuildFloor2Enemy("Enemy_CalamitesSprout", data, art, colliderRadius: 0.3f,
                root => root.AddComponent<CalamitesSproutBrain>(), colliderOffset: new Vector2(0f, 0.35f));
            return enemy != null ? enemy.GetComponent<CalamitesSproutBrain>() : null;
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
            // Las recompensas, la tienda y la sala de cambio salen de la reserva del piso 3, no de la del 2 (Floor_3 la tenía mal).
            var pool = Floor3MutationsSetup.EnsurePool();
            if (config.RewardPool != pool)
            {
                config.ConfigureRewards(pool, config.RewardAfter);
                Debug.Log("ArtSetup: Floor_3 usaba otra reserva de mutaciones; ahora usa MutationPool_Floor3.");
            }
            if (entries.Length == 0)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                return;
            }
            if (config.BaseBudget <= 0f)
                config.ConfigureEnemies(entries, config.Boss, newBaseBudget: 3f, newBudgetPerRoom: 1.4f);
            else
                config.SetEnemies(entries);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
