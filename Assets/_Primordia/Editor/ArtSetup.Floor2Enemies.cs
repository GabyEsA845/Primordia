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
    /// Enemigos del piso 2 (GDD, tabla de enemigos del océano cámbrico), por pasos: de momento el Trilobite, la Medusa azul, la Ottoia y la Esponja Vauxia. Importa su arte
    /// (docs/multimedia-temp/floor2-enemies, ritmos de su README), crea sus datos y su prefab con su cerebro
    /// (<see cref="EnemyBrain"/>) y los pone en Floor_2 con su coste y la sala desde la que salen. Rehace el prefab cada vez.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor2EnemiesSourceFolder = "docs/multimedia-temp/floor2-enemies";
        const string Floor2EnemiesArtFolder = CharactersFolder + "/Enemies/Floor2";
        const string Floor2EnemyDataFolder = "Assets/_Primordia/Data/Enemies/Floor2";

        /// <summary>Cuadros por segundo a partir de los milisegundos por cuadro del README.</summary>
        static float Fps(float ms) => 1000f / ms;

        [MenuItem("Primordia/Enemies/Crear enemigos del piso 2")]
        static void CreateFloor2Enemies()
        {
            Directory.CreateDirectory(Floor2EnemyDataFolder);
            Directory.CreateDirectory(EnemyPrefabsFolder);

            var entries = new List<FloorConfig.EnemyEntry>();
            var trilobite = CreateTrilobite();
            if (trilobite != null)
                entries.Add(new FloorConfig.EnemyEntry(trilobite, cost: 2, minDepth: 1));
            var jelly = CreateBlueJelly();
            if (jelly != null)
                entries.Add(new FloorConfig.EnemyEntry(jelly, cost: 2, minDepth: 2));
            var ottoia = CreateOttoia();
            if (ottoia != null)
                entries.Add(new FloorConfig.EnemyEntry(ottoia, cost: 3, minDepth: 2));
            var vauxia = CreateVauxia();
            if (vauxia != null)
                entries.Add(new FloorConfig.EnemyEntry(vauxia, cost: 3, minDepth: 3, maxPerRoom: 1));

            AssetDatabase.SaveAssets();
            PutInFloor2(entries);
            Debug.Log($"ArtSetup: {entries.Count} enemigos del piso 2 creados y puestos en Floor_2.");
        }

        // ---------- Trilobite ----------

        static Enemy CreateTrilobite()
        {
            var art = Import(Floor2Enemy("Trilobite",
                ("Move", Fps(120), true), ("Warn", Fps(150), true), ("Charge", Fps(70), true), ("Flip", Fps(60), false),
                ("Stunned", Fps(120), true), ("Recover", Fps(70), false), ("Hurt", Fps(90), false), ("Death", Fps(120), false)));
            if (art == null)
                return null;

            // 14 de vida, anda despacio, te ve a 8 u y hace medio corazón al tocarte (también embistiendo).
            var data = Floor2EnemyData("Enemy_Trilobite", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 14, moveSpeed: 3f, detectionRange: 8f, contactDamage: 1));
            return BuildFloor2Enemy("Enemy_Trilobite", data, art, colliderRadius: 0.5f, root => root.AddComponent<TrilobiteBrain>());
        }

        // ---------- Medusa azul ----------

        static Enemy CreateBlueJelly()
        {
            var art = Import(Floor2Enemy("BlueJelly",
                ("Move", Fps(120), true), ("Warn", Fps(150), false), ("Shoot", Fps(80), false), ("Hurt", Fps(90), false),
                ("Death", Fps(120), false)));
            if (art == null)
                return null;

            // Carga lenta y redonda: medio corazón, 3,5 u/s durante 3 s; al chocar, su salpicadura azul.
            string effects = $"{Floor2EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            var impact = Floor2MouthsSetup.CreateFrameEffect("Effect_BlueJelly_Impact",
                ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Fx_BlueJelly_Impact.png", $"{effects}/Fx_BlueJelly_Impact.png", 16), 0.06f);
            var projectile = CreateEnemyProjectile("Projectile_BlueJelly",
                ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Proj_BlueJelly.png", $"{effects}/Proj_BlueJelly.png", 8),
                radius: 0.2f, impact);
            var attack = CombatSetup.LoadAttack("Attack_BlueJelly");
            if (attack == null)
                attack = CombatSetup.CreateAttack("Attack_BlueJelly", projectile, 1f, 1, 3.5f, 3f);
            attack.Configure(projectile, fireRate: 1f / 3f, damage: 1, speed: 3.5f, lifetime: 3f);
            EditorUtility.SetDirty(attack);

            // 10 de vida, nada despacio por encima de las fosas, te ve a 9 u y hace medio corazón al tocarla.
            var data = Floor2EnemyData("Enemy_BlueJelly", d =>
            {
                d.Configure(EnemyBehaviour.Chaser, maxHealth: 10, moveSpeed: 1.6f, detectionRange: 9f, contactDamage: 1);
                d.ConfigureSwimming(true);
            });
            return BuildFloor2Enemy("Enemy_BlueJelly", data, art, colliderRadius: 0.45f, root =>
            {
                CombatSetup.ConfigureWeapon(root.AddComponent<Weapon>(), attack, Team.Enemy, null);
                root.AddComponent<BlueJellyBrain>();
            });
        }

        // ---------- Ottoia ----------

        static Enemy CreateOttoia()
        {
            // El agujero está en (16, 24) del lienzo de 32: el pivote, para que salga del punto donde está.
            var character = Floor2Enemy("Ottoia",
                ("Burrowed", Fps(160), true), ("Warn", Fps(200), false), ("Attack", Fps(70), false), ("Sink", Fps(90), false),
                ("Hurt", Fps(90), false), ("Death", Fps(120), false));
            character.DefaultState = "Burrowed";
            character.Pivot = new Vector2(0.5f, 0.25f);
            var art = Import(character);
            if (art == null)
                return null;

            // 9 de vida; enterrada avanza algo más despacio que el jugador y te ve a 9 u. Solo daña con el mordisco (un
            // corazón): tocarla asomada no hace daño, para poder ir a golpearla.
            var data = Floor2EnemyData("Enemy_Ottoia", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 9, moveSpeed: 2.8f, detectionRange: 9f, contactDamage: 0));
            // Asomada, el cuerpo queda por encima del agujero.
            return BuildFloor2Enemy("Enemy_Ottoia", data, art, colliderRadius: 0.4f, root => root.AddComponent<OttoiaBrain>(),
                colliderOffset: new Vector2(0f, 0.4f));
        }

        // ---------- Esponja Vauxia ----------

        static Enemy CreateVauxia()
        {
            var character = Floor2Enemy("Vauxia",
                ("Idle", Fps(180), true), ("Heal", Fps(110), false), ("Hurt", Fps(90), false), ("Death", Fps(120), false));
            character.DefaultState = "Idle";
            var art = Import(character);
            if (art == null)
                return null;

            // La onda (83 px de radio = 5,2 u) va por el suelo, bajo los enemigos; el escudo, sobre su cuerpo; las cruces, encima.
            string effects = $"{Floor2EnemiesArtFolder}/Effects";
            Directory.CreateDirectory(effects);
            var pulse = Floor2MouthsSetup.CreateFrameEffect("Effect_Vauxia_HealPulse",
                ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Fx_Vauxia_HealPulse.png", $"{effects}/Fx_Vauxia_HealPulse.png", 168),
                0.08f, sortingOrder: 4);
            var shield = Floor2MouthsSetup.CreateLoopEffect("Effect_Vauxia_Shield",
                ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Fx_Vauxia_Shield.png", $"{effects}/Fx_Vauxia_Shield.png", 32),
                0.1f, sortingOrder: 6);
            var motes = Floor2MouthsSetup.CreateFrameEffect("Effect_Vauxia_HealMotes",
                ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Fx_Vauxia_HealMotes.png", $"{effects}/Fx_Vauxia_HealMotes.png", 16),
                0.08f, sortingOrder: 22);

            var thread = ImportLooseSheet($"{Floor2EnemiesSourceFolder}/Effects/Fx_Vauxia_Thread.png", $"{effects}/Fx_Vauxia_Thread.png", 8);

            // 16 de vida, inmóvil, despierta al verte a 10 u y no hace daño al tocarla.
            var data = Floor2EnemyData("Enemy_Vauxia", d => d.Configure(EnemyBehaviour.Chaser,
                maxHealth: 16, moveSpeed: 0f, detectionRange: 10f, contactDamage: 0));
            return BuildFloor2Enemy("Enemy_Vauxia", data, art, colliderRadius: 0.7f,
                root => root.AddComponent<VauxiaBrain>().Configure(pulse, shield, motes, thread));
        }

        // ---------- Ayudas ----------

        /// <summary>Proyectil enemigo animado en bucle, con su efecto al chocar. Se rehace (conserva el GUID).</summary>
        static Projectile CreateEnemyProjectile(string prefabName, Sprite[] frames, float radius, GameObject impact)
        {
            var root = new GameObject(prefabName);
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames.Length > 0 ? frames[0] : null;
                renderer.sortingOrder = 20;
                if (frames.Length > 1)
                    root.AddComponent<SpriteLoop>().Configure(renderer, frames, 0.12f, 0f);

                var collider = root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = radius;
                root.AddComponent<Rigidbody2D>().gravityScale = 0f;

                var projectile = root.AddComponent<Projectile>();
                projectile.SetImpactEffect(impact);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectilePrefabsFolder}/{prefabName}.prefab").GetComponent<Projectile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Character Floor2Enemy(string name, params (string, float, bool)[] timings) => new()
        {
            Name = name,
            SourceFolder = $"{Floor2EnemiesSourceFolder}/{name}",
            ArtFolder = $"{Floor2EnemiesArtFolder}/{name}",
            DefaultState = "Move",
            Timings = timings,
        };

        /// <summary>Los datos del enemigo: se crean si no existen y se vuelven a configurar siempre.</summary>
        static EnemyData Floor2EnemyData(string assetName, System.Action<EnemyData> configure)
        {
            string path = $"{Floor2EnemyDataFolder}/{assetName}.asset";
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

        /// <summary>
        /// Prefab de un enemigo del piso 2, rehecho cada vez (conserva el GUID): capa Enemy, cuerpo físico, vida, daño por
        /// contacto, IA con su cerebro y el cuerpo animado.
        /// </summary>
        static Enemy BuildFloor2Enemy(string prefabName, EnemyData data, ImportedArt art, float colliderRadius,
            System.Action<GameObject> addBrain, Vector2 colliderOffset = default)
        {
            var root = new GameObject(prefabName) { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            try
            {
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = art.DefaultSprite;
                renderer.sortingOrder = 5;
                var animator = body.AddComponent<Animator>();
                animator.runtimeAnimatorController = art.Controller;

                var rigidbody = root.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0f;
                rigidbody.freezeRotation = true;
                var collider = root.AddComponent<CircleCollider2D>();
                collider.radius = colliderRadius;
                collider.offset = colliderOffset;
                root.AddComponent<Health>();

                var contact = root.AddComponent<ContactDamage>();
                SetMask(contact, "targets", LayerMask.GetMask(CombatLayers.Player));

                var enemy = root.AddComponent<Enemy>();
                SetFields(enemy, ("data", data));
                enemy.SetAnimationTimings(art.Length(EnemyAnimator.DeathState), 0f);

                root.AddComponent<EnemyAI>();
                addBrain(root);
                root.AddComponent<EnemyAnimator>().Configure(animator, renderer,
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectPrefabsFolder}/Effect_EnemyDeath.prefab"));

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{EnemyPrefabsFolder}/{prefabName}.prefab");
                return prefab.GetComponent<Enemy>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void SetMask(Object target, string field, int mask)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = mask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Los enemigos de Floor_2 pasan a ser estos (los provisionales del piso 1 salen).</summary>
        static void PutInFloor2(List<FloorConfig.EnemyEntry> entries)
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor2Setup.Floor2ConfigPath);
            if (config == null)
            {
                Debug.LogWarning("ArtSetup: no existe Floor_2; créalo con Primordia > Rooms > Crear piso 2 y escena de prueba.");
                return;
            }
            if (entries.Count == 0)
                return;
            config.SetEnemies(entries.ToArray());
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
