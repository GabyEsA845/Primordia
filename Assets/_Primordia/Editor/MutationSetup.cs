using System;
using System.Collections.Generic;
using System.IO;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace Primordia.Editor
{
    /// <summary>
    /// Crea las 9 mutaciones del GDD (con sus ataques y efectos; el aspecto lo pone
    /// Primordia > Art > Importar piezas de mutación), la reserva
    /// de mutaciones, añade al jugador las ranuras, la habilidad y la barra de recarga, y crea la
    /// escena de prueba Test_Mutations. No sobrescribe nada que ya exista.
    /// </summary>
    public static class MutationSetup
    {
        const string DataFolder = "Assets/_Primordia/Data/Mutations";
        const string PrefabsFolder = "Assets/_Primordia/Prefabs/Mutations";
        const string PoolPath = DataFolder + "/MutationPool.asset";
        internal const string CircleSpritePath = "Assets/_Primordia/Art/Placeholder/Circle32.png";
        const string PixelSpritePath = "Assets/_Primordia/Art/Placeholder/Pixel.png";
        const string TestScenePath = "Assets/_Primordia/Scenes/Test_Mutations.unity";

        // Color de los proyectiles provisionales del Aguijón (el de las mutaciones va por ranura: LineageSetup.SlotColors).
        static readonly Color ToxicColor = new(0.55f, 0.9f, 0.35f);

        [MenuItem("Primordia/Mutations/Crear mutaciones y escena de prueba")]
        static void CreateAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var square = CoreSetup.CreateSquareSprite();
            var cameraRig = CoreSetup.CreateCameraRigPrefab();
            var gameManager = CoreSetup.CreateGameManagerPrefab();
            var player = PlayerSetup.CreatePlayerPrefab(square);
            FloorSetup.CreateAssets(square);
            CreateAssets(square);
            UISetup.CreateUIAssets();

            CreateTestScene(cameraRig, gameManager, player);
            AssetDatabase.SaveAssets();
        }

        internal static MutationPool LoadPool() => AssetDatabase.LoadAssetAtPath<MutationPool>(PoolPath);

        /// <summary>
        /// Mandíbulas muerde delante de la boca: alcance 1,5 unidades desde el centro y arco de 180°.
        /// Sin efecto propio: el mordisco se ve en la animación Attack de la pieza de Boca.
        /// Sirve también para convertir los ataques de versiones anteriores (proyectil o efecto Effect_Bite).
        /// </summary>
        internal static void ConfigureMandiblesBite(AttackData attack)
        {
            if (attack == null)
                return;

            attack.ConfigureMelee(range: 1.5f, arc: 180f, effect: null);
            EditorUtility.SetDirty(attack);
        }

        /// <summary>
        /// Esporas: una bocanada de 5 esporas lentas en abanico de 75° que frenan en 0,6 s (unas 2,8 unidades) y
        /// flotan 1,5 s más mientras se dispersan (1,5 unidades por segundo, otras 2,2 unidades), desvaneciéndose al
        /// final; 1 de daño cada una y 1,25 bocanadas por segundo.
        /// Sirve también para convertir el abanico de versiones anteriores.
        /// </summary>
        internal static void ConfigureSporesPuff(AttackData attack)
        {
            if (attack == null)
                return;

            attack.Configure(attack.ProjectilePrefab, fireRate: 1.25f, damage: 1, speed: 8f, lifetime: SporesBrake + 1.5f,
                count: 5, spread: 75f);
            attack.ConfigureFloating(SporesBrake, speedVariation: 0.2f, angleVariation: 10f, fade: 0.3f, scatter: 1.5f);
            EditorUtility.SetDirty(attack);
        }

        /// <summary>Segundos que tardan las esporas en frenar.</summary>
        internal const float SporesBrake = 0.6f;

        /// <summary>Mutaciones, reserva y mejoras del jugador. Lo guarda todo antes de volver.</summary>
        internal static void CreateAssets(Sprite square)
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(PrefabsFolder);

            var pixel = CreatePixelSprite();
            var circle = CreateCircleSprite();

            // Ataques de Boca. Mandíbulas es un mordisco cuerpo a cuerpo: no tiene proyectil.
            var stingerProjectile = CombatSetup.CreateProjectilePrefab("Projectile_Stinger", ToxicColor, new Vector2(0.6f, 0.2f), square);
            var sporeProjectile = CombatSetup.CreateProjectilePrefab("Projectile_Spore", new Color(0.75f, 0.5f, 0.95f), 0.5f, square);

            var mandiblesAttack = CombatSetup.CreateAttack("Attack_Mandibles", null,
                fireRate: 2.5f, damage: 3, speed: 12f, lifetime: 0.35f);
            ConfigureMandiblesBite(mandiblesAttack);
            var stingerAttack = CombatSetup.CreateAttack("Attack_Stinger", stingerProjectile,
                fireRate: 4f, damage: 1, speed: 16f, lifetime: 2f, automatic: true, poisonDamagePerSecond: 1, poisonDuration: 3f);
            var sporesAttack = CombatSetup.CreateAttack("Attack_Spores", sporeProjectile,
                fireRate: 1.5f, damage: 1, speed: 8f, lifetime: 2.1f, count: 5, spread: 60f);
            ConfigureSporesPuff(sporesAttack);

            // Efectos de las habilidades.
            var inkEffect = CreateEffect("Effect_InkCloud", circle, new Color(0.1f, 0.1f, 0.25f, 0.8f), duration: 1f, endScale: 1.1f);
            var shockEffect = CreateEffect("Effect_Shockwave", circle, new Color(0.5f, 0.95f, 1f, 0.6f), duration: 0.35f, endScale: 1.3f);

            var mutations = new List<MutationData>
            {
                CreateMutation<AttackMutation>("Mutation_Mandibles", "Mandíbulas",
                    "Mordisco corto y potente (3 de daño). Cada 3 enemigos que mates, curas medio corazón.",
                    MutationSlot.Mouth,
                    m => m.Configure(mandiblesAttack, newHealEveryKills: 3)),

                CreateMutation<AttackMutation>("Mutation_Stinger", "Aguijón",
                    "Disparo automático y rápido que envenena: 1 de daño por segundo durante 3 s.",
                    MutationSlot.Mouth,
                    m => m.Configure(stingerAttack, newHealEveryKills: 0)),

                CreateMutation<AttackMutation>("Mutation_Spores", "Esporas",
                    "Bocanada de 5 esporas lentas que se quedan flotando como minas.",
                    MutationSlot.Mouth,
                    m => m.Configure(sporesAttack, newHealEveryKills: 0)),

                CreateMutation<StatMutation>("Mutation_Fins", "Aletas",
                    "+30 % de velocidad y esquiva un 50 % más larga.",
                    MutationSlot.Limbs,
                    m => m.Configure(speedMultiplier: 1.3f, dodgeMultiplier: 1.5f, healthBonus: 0)),

                CreateMutation<DodgePushMutation>("Mutation_JointedLegs", "Patas articuladas",
                    "Al esquivar contra un enemigo, lo empujas, lo aturdes y le haces 1 de daño.",
                    MutationSlot.Limbs,
                    m => m.Configure(newPushSpeed: 12f, newStunDuration: 0.5f, newDamage: 1)),

                CreateMutation<StatMutation>("Mutation_Carapace", "Caparazón",
                    "+1 corazón de vida máxima, pero −10 % de velocidad.",
                    MutationSlot.Skin,
                    m => m.Configure(speedMultiplier: 0.9f, dodgeMultiplier: 1f, healthBonus: 2)),

                CreateMutation<ThornsMutation>("Mutation_StingingSkin", "Piel urticante",
                    "Los enemigos que te tocan reciben 2 de daño.",
                    MutationSlot.Skin,
                    m => m.Configure(newDamage: 2, newInterval: 0.5f)),

                CreateMutation<InkCloudMutation>("Mutation_InkSac", "Vejiga de tinta",
                    "Clic derecho: nube que ciega 3 s a los enemigos cercanos. Recarga: 8 s.",
                    MutationSlot.Organ,
                    m =>
                    {
                        m.ConfigureAbility(newCooldown: 8f, inkEffect);
                        m.Configure(newRadius: 3f, newBlindDuration: 3f);
                    }),

                CreateMutation<ShockwaveMutation>("Mutation_ShockGland", "Glándula de choque",
                    "Clic derecho: onda que destruye los proyectiles enemigos y empuja a los enemigos. Recarga: 6 s.",
                    MutationSlot.Organ,
                    m =>
                    {
                        m.ConfigureAbility(newCooldown: 6f, shockEffect);
                        m.Configure(newRadius: 4f, newPushSpeed: 14f, newStunDuration: 0.4f);
                    })
            };

            if (LoadPool() == null)
            {
                var pool = ScriptableObject.CreateInstance<MutationPool>();
                pool.Configure(mutations);
                AssetDatabase.CreateAsset(pool, PoolPath);
            }

            AssetDatabase.SaveAssets();
            AddMutationsToPlayer(pixel);
            AssetDatabase.SaveAssets();
        }

        static T CreateMutation<T>(string assetName, string displayName, string description, MutationSlot slot,
            Action<T> configure) where T : MutationData
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var mutation = ScriptableObject.CreateInstance<T>();
            mutation.ConfigureBase(displayName, description, slot, LineageSetup.SlotColors[(int)slot]);
            configure(mutation);
            AssetDatabase.CreateAsset(mutation, path);
            return mutation;
        }

        // ---------- Sprites y prefabs provisionales ----------

        static Sprite CreatePixelSprite() =>
            CoreSetup.CreatePixelSprite(PixelSpritePath, 1, 1, new[] { new Color32(255, 255, 255, 255) });

        static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var pixels = new Color32[size * size];
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                bool inside = dx * dx + dy * dy <= radius * radius;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }

            return CoreSetup.CreatePixelSprite(CircleSpritePath, size, size, pixels);
        }

        /// <summary>Efecto de habilidad: un círculo de 2 unidades de diámetro que se desvanece.</summary>
        static GameObject CreateEffect(string prefabName, Sprite circle, Color color, float duration, float endScale)
        {
            string path = $"{PrefabsFolder}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var root = new GameObject(prefabName);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = color;
            renderer.sortingOrder = 15;

            var fade = root.AddComponent<FadeOutEffect>();
            var serialized = new SerializedObject(fade);
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("endScale").floatValue = endScale;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, path);
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- Jugador ----------

        /// <summary>Añade al prefab del jugador las ranuras, la habilidad y la barra de recarga.</summary>
        static void AddMutationsToPlayer(Sprite pixel)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerMutations>(out _))
                    root.AddComponent<PlayerMutations>();

                if (!root.TryGetComponent<PlayerAbility>(out var ability))
                    ability = root.AddComponent<PlayerAbility>();

                // La recarga del Órgano se ve en el recuadro del HUD (Primordia > Art > Importar recuadro del Órgano), no bajo el jugador.
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Barra de 1 × 0,125 unidades (16 × 2 px) bajo el jugador.</summary>
        static void CreateCooldownBar(Transform player, PlayerAbility ability, Sprite pixel)
        {
            var bar = new GameObject("CooldownBar");
            bar.transform.SetParent(player, false);
            bar.transform.localPosition = new Vector3(0f, -0.85f, 0f);

            SpriteRenderer Part(string name, Color color, int order)
            {
                var part = new GameObject(name);
                part.transform.SetParent(bar.transform, false);
                part.transform.localScale = new Vector3(16f, 2f, 1f);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = pixel;
                renderer.color = color;
                renderer.sortingOrder = order;
                return renderer;
            }

            var background = Part("Background", new Color(0.05f, 0.05f, 0.08f, 0.85f), 30);
            var fill = Part("Fill", new Color(0.45f, 0.85f, 1f), 31);
            bar.AddComponent<CooldownBar>().Configure(ability, background, fill);
        }

        // ---------- Escena de prueba ----------

        static void CreateTestScene(GameObject cameraRig, GameObject gameManager, GameObject playerPrefab)
        {
            if (!CoreSetup.TryCreateSceneFromTemplate(TestScenePath, cameraRig, gameManager, out var scene, out var rig))
                return;

            // Se cargan desde disco después de abrir la escena, para que las referencias sean válidas.
            var square = CoreSetup.CreateSquareSprite();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            var pool = LoadPool();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            rig.GetComponentInChildren<CinemachineCamera>().Follow = player.transform;

            PlayerSetup.CreateTestRoom(square);

            // Las 9 mutaciones en fila, abajo, con su nombre encima.
            var pickups = new GameObject("Pickups").transform;
            for (int i = 0; i < pool.Mutations.Count; i++)
                CreatePickup(pickups, pool.Mutations[i], new Vector2(-16f + i * 4f, -10f), circle);

            var enemies = new GameObject("Enemies").transform;
            var chaser = EnemySetup.LoadPrefab("Enemy_Chaser").gameObject;
            var shooter = EnemySetup.LoadPrefab("Enemy_Shooter").gameObject;
            foreach (var position in new[] { new Vector2(14f, 8f), new Vector2(15f, 3f), new Vector2(13f, -1f) })
                ((GameObject)PrefabUtility.InstantiatePrefab(chaser, enemies)).transform.position = position;
            foreach (var position in new[] { new Vector2(-14f, 8f), new Vector2(-15f, 1f) })
                ((GameObject)PrefabUtility.InstantiatePrefab(shooter, enemies)).transform.position = position;

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Primordia/Prefabs/UI/GameUI.prefab"), scene);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene);
        }

        internal static void CreatePickup(Transform parent, MutationData mutation, Vector2 position, Sprite circle)
        {
            var pickup = new GameObject($"Pickup_{mutation.name}");
            pickup.transform.SetParent(parent, false);
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.5f;

            var renderer = pickup.AddComponent<SpriteRenderer>();
            renderer.sprite = circle;
            renderer.color = mutation.Color;
            renderer.sortingOrder = 2;

            pickup.AddComponent<CircleCollider2D>().isTrigger = true;
            pickup.AddComponent<MutationPickup>().SetMutation(mutation);

            // Nombre flotante encima, con la fuente integrada.
            var label = new GameObject("Label");
            label.transform.SetParent(pickup.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var text = label.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.text = mutation.DisplayName;
            text.fontSize = 48;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            var labelRenderer = label.GetComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = font.material;
            labelRenderer.sortingOrder = 50;
        }
    }
}
