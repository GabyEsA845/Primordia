using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Importa las hojas de sprites desde docs/multimedia-temp (fuera del repositorio), las corta en
    /// cuadros, crea un clip por hoja y el Animator de cada personaje, y se los pone a su prefab.
    /// Se puede ejecutar de nuevo cada vez que cambie el arte: sobrescribe las hojas y actualiza los clips.
    /// </summary>
    public static class ArtSetup
    {
        const string CharactersFolder = "Assets/_Primordia/Art/Characters";
        const string EnemiesSourceFolder = "docs/multimedia-temp/enemies";
        const string EnemyPrefabsFolder = "Assets/_Primordia/Prefabs/Enemies";
        const int PixelsPerUnit = 16;

        // Cuadro (desde 0) en el que sale el disparo o aparece la invocación, según el README del arte.
        const int ShootFireFrame = 2;
        const int BurstFireFrame = 2;
        const int SummonSpawnFrame = 1;

        /// <summary>Un personaje: de dónde salen sus hojas, adónde van y a qué ritmo va cada animación.</summary>
        sealed class Character
        {
            public string Name;
            public string SourceFolder;
            public string ArtFolder;
            public string DefaultState;

            // Cuadros por segundo según el prefijo del clip; gana el primero que encaje.
            public (string prefix, float fps, bool loop)[] Timings;

            public string SheetPrefix => Name + "_";
            public string AnimationsFolder => ArtFolder + "/Animations";
            public string ControllerPath => $"{AnimationsFolder}/{Name}.controller";
        }

        /// <summary>Resultado de importar un personaje.</summary>
        sealed class ImportedArt
        {
            public AnimatorController Controller;
            public Sprite DefaultSprite;
            public readonly Dictionary<string, AnimationClip> Clips = new();

            public float Length(string clip) => Clips.TryGetValue(clip, out var c) ? c.length : 0f;

            /// <summary>Segundos desde el inicio del clip hasta ese cuadro; 0 si el clip no existe.</summary>
            public float FrameTime(string clip, int frame) => Clips.TryGetValue(clip, out var c) ? frame / c.frameRate : 0f;
        }

        // La esquiva dura 0,25 s en PlayerController: 5 cuadros a 20 fps.
        static readonly Character PlayerArt = new()
        {
            Name = "Player",
            SourceFolder = "docs/multimedia-temp/player",
            ArtFolder = CharactersFolder + "/Player",
            DefaultState = "Idle_down",
            Timings = new[] { ("Idle", 6f, true), ("Move", 10f, true), ("Dodge", 20f, false), ("Hurt", 10f, false), ("Death", 10f, false) },
        };

        static readonly Character BacteriaArt = EnemyCharacter("Bacteria", ("Move", 10f, true), ("Hurt", 10f, false), ("Death", 10f, false));

        static readonly Character AmebaArt = EnemyCharacter("Ameba",
            ("Move", 8f, true), ("Shoot", 10f, false), ("Hurt", 10f, false), ("Split", 10f, false), ("Death", 10f, false));

        static readonly Character AmebaSmallArt = EnemyCharacter("AmebaSmall", ("Move", 12f, true), ("Hurt", 10f, false), ("Death", 10f, false));

        // En la fase 2 la espiral gira más rápido, y su reposo también.
        static readonly Character BossVirusArt = new()
        {
            Name = "BossVirus",
            SourceFolder = EnemiesSourceFolder + "/BossVirus",
            ArtFolder = CharactersFolder + "/Enemies/BossVirus",
            DefaultState = "Idle_P1",
            Timings = new[]
            {
                ("Idle_P1", 6f, true), ("Idle_P2", 10f, true), ("Burst", 10f, false), ("Summon", 10f, false),
                ("Hurt", 10f, false), ("Phase2_Transition", 8f, false), ("Death", 10f, false),
            },
        };

        static readonly EditorCurveBinding SpriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        static Character EnemyCharacter(string name, params (string, float, bool)[] timings) => new()
        {
            Name = name,
            SourceFolder = $"{EnemiesSourceFolder}/{name}",
            ArtFolder = $"{CharactersFolder}/Enemies/{name}",
            DefaultState = "Move",
            Timings = timings,
        };

        [MenuItem("Primordia/Art/Importar animaciones del jugador")]
        static void ImportPlayer()
        {
            var art = Import(PlayerArt);
            if (art == null)
                return;

            ApplyToPlayerPrefab(art.Controller, art.DefaultSprite);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Primordia/Art/Importar animaciones de enemigos y jefe")]
        static void ImportEnemies()
        {
            // Radios de colisión: los mismos que tenían con la escala provisional (0,45 × escala).
            var bacteria = Import(BacteriaArt);
            if (bacteria != null)
                ApplyToEnemyPrefab("Enemy_Chaser", bacteria, colliderRadius: 0.45f);

            var ameba = Import(AmebaArt);
            if (ameba != null)
                ApplyToEnemyPrefab("Enemy_Shooter", ameba, colliderRadius: 0.45f);

            var small = Import(AmebaSmallArt);
            if (small != null)
                ApplyToEnemyPrefab("Enemy_Small", small, colliderRadius: 0.28f);

            var boss = Import(BossVirusArt);
            if (boss != null)
                ApplyToBossPrefab(boss);

            AssetDatabase.SaveAssets();
        }

        /// <summary>Copia, corta y anima las hojas del personaje. Devuelve null si no hay hojas.</summary>
        static ImportedArt Import(Character character)
        {
            var sheets = CopySheets(character);
            if (sheets.Count == 0)
            {
                EditorUtility.DisplayDialog("Primordia",
                    $"No hay hojas {character.SheetPrefix}*.png en {character.SourceFolder} ni en {character.ArtFolder}.", "Vale");
                return null;
            }

            Directory.CreateDirectory(character.AnimationsFolder);
            var art = new ImportedArt();
            foreach (var sheet in sheets)
            {
                var sprites = SliceSheet(sheet);
                var clip = CreateClip(character, sheet, sprites);
                art.Clips[clip.name] = clip;
                if (clip.name == character.DefaultState)
                    art.DefaultSprite = sprites[0];
            }

            AssetDatabase.SaveAssets();
            art.Controller = CreateController(character, art.Clips.Values);

            Debug.Log($"ArtSetup: {art.Clips.Count} animaciones de {character.Name} importadas en {character.AnimationsFolder}.");
            return art;
        }

        /// <summary>Copia las hojas de la carpeta temporal (si existe) y devuelve las que hay en el proyecto.</summary>
        static List<string> CopySheets(Character character)
        {
            Directory.CreateDirectory(character.ArtFolder);
            string pattern = character.SheetPrefix + "*.png";

            if (Directory.Exists(character.SourceFolder))
            {
                foreach (var source in Directory.GetFiles(character.SourceFolder, pattern))
                {
                    var target = $"{character.ArtFolder}/{Path.GetFileName(source)}";
                    File.Copy(source, target, overwrite: true);
                    AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                }
            }
            else
            {
                Debug.LogWarning($"ArtSetup: no existe {character.SourceFolder}; se usan las hojas que ya están en {character.ArtFolder}.");
            }

            return Directory.GetFiles(character.ArtFolder, pattern)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path)
                .ToList();
        }

        /// <summary>
        /// Configura la hoja como pixel art y la corta en cuadros cuadrados del alto de la imagen,
        /// con pivote centrado. Conserva los identificadores de los cuadros que ya existían.
        /// </summary>
        static Sprite[] SliceSheet(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int cell = height;
            int count = width / cell;
            string baseName = Path.GetFileNameWithoutExtension(path);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var previousIds = new Dictionary<string, GUID>();
            foreach (var rect in provider.GetSpriteRects())
                previousIds[rect.name] = rect.spriteID;

            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                string name = $"{baseName}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * cell, 0, cell, cell),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = previousIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

        /// <summary>Crea o actualiza el clip de una hoja: Bacteria_Move.png → Move.anim.</summary>
        static AnimationClip CreateClip(Character character, string sheetPath, Sprite[] sprites)
        {
            string name = Path.GetFileNameWithoutExtension(sheetPath).Substring(character.SheetPrefix.Length);
            var timing = character.Timings.FirstOrDefault(t => name.StartsWith(t.prefix));
            if (timing.prefix == null)
            {
                Debug.LogWarning($"ArtSetup: {character.Name}/{name} no tiene ritmo definido; se usa 10 fps sin bucle.");
                timing = (name, 10f, false);
            }

            string clipPath = $"{character.AnimationsFolder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            bool isNew = clip == null;
            if (isNew)
                clip = new AnimationClip { name = name };

            clip.frameRate = timing.fps;

            // El último cuadro se repite al final para que dure lo mismo que los demás.
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i <= sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / timing.fps, value = sprites[Mathf.Min(i, sprites.Length - 1)] };
            AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = timing.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew)
                AssetDatabase.CreateAsset(clip, clipPath);
            else
                EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>Animator con un estado por clip y sin transiciones: el código elige el estado.</summary>
        static AnimatorController CreateController(Character character, IEnumerable<AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(character.ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(character.ControllerPath);
            var machine = controller.layers[0].stateMachine;

            foreach (var clip in clips)
            {
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name)
                    ?? machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name == character.DefaultState)
                    machine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void ApplyToPlayerPrefab(AnimatorController controller, Sprite idleSprite)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSetup.PlayerPath) == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {PlayerSetup.PlayerPath}; crea antes el jugador con Primordia > Player.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                var bodyRenderer = PrepareBody(root, controller, idleSprite, out var animator);

                // La boca sigue siendo el punto de disparo, pero ya no se dibuja: la célula mira hacia el cursor.
                var mouth = root.transform.Find("AimPivot/Mouth");
                if (mouth != null && mouth.TryGetComponent<SpriteRenderer>(out var mouthRenderer))
                    mouthRenderer.enabled = false;

                var playerAnimator = GetOrAdd<PlayerAnimator>(root);
                SetFields(playerAnimator, ("animator", animator), ("body", bodyRenderer));

                // El cuerpo tiene su animación Hurt; el destello blanco queda para los aspectos de mutación.
                if (root.TryGetComponent<DamageFlash>(out var flash))
                {
                    var flashSerialized = new SerializedObject(flash);
                    var excluded = flashSerialized.FindProperty("excluded");
                    excluded.arraySize = 1;
                    excluded.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
                    flashSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ApplyToEnemyPrefab(string prefabName, ImportedArt art, float colliderRadius)
        {
            EditEnemyPrefab(prefabName, art, colliderRadius, root =>
            {
                var bodyRenderer = root.transform.Find("Body").GetComponent<SpriteRenderer>();
                var animator = bodyRenderer.GetComponent<Animator>();

                SetFields(root.GetComponent<Enemy>(),
                    ("deathDuration", art.Length(EnemyAnimator.DeathState)),
                    ("splitDuration", art.Length(EnemyAnimator.SplitState)));

                if (root.TryGetComponent<EnemyAI>(out var ai))
                    SetFields(ai, ("fireWindup", art.FrameTime(EnemyAnimator.ShootState, ShootFireFrame)));

                SetFields(GetOrAdd<EnemyAnimator>(root), ("animator", animator), ("body", bodyRenderer));
            });
        }

        static void ApplyToBossPrefab(ImportedArt art)
        {
            EditEnemyPrefab("Boss_Virus", art, colliderRadius: 1.35f, root =>
            {
                var animator = root.transform.Find("Body").GetComponent<Animator>();

                SetFields(root.GetComponent<Enemy>(), ("deathDuration", art.Length(BossAnimator.DeathState)));
                SetFields(root.GetComponent<BossAI>(),
                    ("burstWindup", art.FrameTime("Burst_P1", BurstFireFrame)),
                    ("summonWindup", art.FrameTime(BossAnimator.SummonState, SummonSpawnFrame)),
                    ("phase2TransitionDuration", art.Length(BossAnimator.TransitionState)));

                SetFields(GetOrAdd<BossAnimator>(root), ("animator", animator));
            });
        }

        /// <summary>
        /// Cambios comunes a enemigos y jefe: escala 1 (el sprite ya tiene su tamaño), radio de colisión,
        /// cuerpo animado, sin las púas provisionales y sin destello blanco (lo sustituye Hurt).
        /// </summary>
        static void EditEnemyPrefab(string prefabName, ImportedArt art, float colliderRadius, System.Action<GameObject> configure)
        {
            string path = $"{EnemyPrefabsFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {path}; créalo antes con Primordia > Enemies o Primordia > Boss.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.transform.localScale = Vector3.one;
                root.GetComponent<CircleCollider2D>().radius = colliderRadius;

                foreach (Transform child in root.transform.Cast<Transform>().ToList())
                {
                    if (child.name == "Spike")
                        Object.DestroyImmediate(child.gameObject);
                }

                PrepareBody(root, art.Controller, art.DefaultSprite, out _);

                if (root.TryGetComponent<DamageFlash>(out var flash))
                    Object.DestroyImmediate(flash);

                configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Deja el hijo Body centrado, a escala 1, en blanco y con el Animator del personaje.</summary>
        static SpriteRenderer PrepareBody(GameObject root, AnimatorController controller, Sprite sprite, out Animator animator)
        {
            var body = root.transform.Find("Body");
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one;

            var renderer = body.GetComponent<SpriteRenderer>();
            if (sprite != null)
                renderer.sprite = sprite;
            renderer.color = Color.white;

            animator = GetOrAdd<Animator>(body.gameObject);
            animator.runtimeAnimatorController = controller;
            return renderer;
        }

        static T GetOrAdd<T>(GameObject gameObject) where T : Component =>
            gameObject.TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();

        /// <summary>Asigna campos serializados (también privados) de un componente del prefab.</summary>
        static void SetFields(Object target, params (string field, object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var property = serialized.FindProperty(field);
                if (value is float number)
                    property.floatValue = number;
                else
                    property.objectReferenceValue = (Object)value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
