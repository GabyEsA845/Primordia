using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Mutations;
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
    public static partial class ArtSetup
    {
        const string CharactersFolder = "Assets/_Primordia/Art/Characters";
        const string EnemiesSourceFolder = "docs/multimedia-temp/enemies";
        const string EnemyPrefabsFolder = "Assets/_Primordia/Prefabs/Enemies";
        const string ProjectilePrefabsFolder = "Assets/_Primordia/Prefabs/Combat";
        const string ProjectilesSourceFolder = "docs/multimedia-temp/projectiles";
        const string ProjectilesArtFolder = "Assets/_Primordia/Art/Projectiles";
        const string EffectPrefabsFolder = "Assets/_Primordia/Prefabs/Effects";
        const string MutationPrefabsFolder = "Assets/_Primordia/Prefabs/Mutations";
        const string MutationDataFolder = "Assets/_Primordia/Data/Mutations";
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

            // Ancho de celda por clip, para las hojas cuyos cuadros no son cuadrados (por defecto, el alto).
            public Dictionary<string, int> CellWidths = new();

            // True: un controlador por clip, que se reproduce solo (proyectiles). False: uno con todos los clips.
            public bool ControllerPerClip;

            // Duración de cada cuadro en segundos, para los clips que no van a ritmo fijo (sustituye a los fps).
            public Dictionary<string, float[]> FrameDurations = new();

            // Hojas de la carpeta que no se importan (por nombre de clip).
            public HashSet<string> SkipClips = new();

            // Pivote de los cuadros, de 0 a 1 (por defecto, el centro).
            public Vector2 Pivot = new(0.5f, 0.5f);

            // Sin nombre, las hojas no comparten prefijo y el clip se llama como la hoja (efectos).
            public string SheetPrefix => string.IsNullOrEmpty(Name) ? "" : Name + "_";
            public string AnimationsFolder => ArtFolder + "/Animations";
            public string ControllerPath => $"{AnimationsFolder}/{Name}.controller";
        }

        /// <summary>Resultado de importar un personaje.</summary>
        sealed class ImportedArt
        {
            public AnimatorController Controller;
            public Sprite DefaultSprite;
            public readonly Dictionary<string, AnimationClip> Clips = new();

            // Solo con ControllerPerClip: controlador y primer cuadro de cada clip.
            public readonly Dictionary<string, AnimatorController> Controllers = new();
            public readonly Dictionary<string, Sprite> FirstSprites = new();

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
                ("Hurt", 10f, false), ("Phase2_Transition", 8f, false), ("Death", 10f, false), ("Intro", IntroPoseFps, false),
            },
        };

        // Un clip por proyectil (Projectile_Spore.png → Spore). Todos miran a la derecha; Projectile los gira.
        // Projectile_Mandibles ya no se usa: Mandíbulas muerde, y el mordisco se ve en su Boca (Mouth_Mandibles_Attack).
        static readonly Character ProjectilesArt = new()
        {
            Name = "Projectile",
            SourceFolder = ProjectilesSourceFolder,
            ArtFolder = ProjectilesArtFolder,
            ControllerPerClip = true,
            CellWidths = new Dictionary<string, int> { ["Stinger"] = 12 },
            SkipClips = new HashSet<string> { "Mandibles" },
            Timings = new[]
            {
                ("Player", 10f, true), ("Stinger", 12f, true), ("Spore", 6f, true),
                ("Enemy", 6f, true), ("BossSpiral", 10f, true), ("BossBurst", 6f, true),
            },
        };

        // Efectos sueltos (Impact_Player.png → Impact_Player). La onda dura 0,35 s: 6 cuadros a 6 / 0,35 fps;
        // la tinta, 1 s: 9 cuadros a 9 fps.
        static readonly Character EffectsArt = new()
        {
            Name = "",
            SourceFolder = "docs/multimedia-temp/effects",
            ArtFolder = "Assets/_Primordia/Art/Effects",
            ControllerPerClip = true,
            SkipClips = new HashSet<string> { "vista_previa_efectos" },
            Timings = new[]
            {
                ("Impact", 20f, false), ("EnemyDeath_Bubbles", 12f, false), ("DodgeTrail", 20f, false),
                ("Poison", 8f, true), ("InkCloud", 9f, false), ("Shockwave", 6f / 0.35f, false),
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

        [MenuItem("Primordia/Art/Importar efectos")]
        static void ImportEffects()
        {
            var art = Import(EffectsArt);
            if (art == null)
                return;

            Directory.CreateDirectory(EffectPrefabsFolder);
            var playerImpact = CreateEffectPrefab("Effect_Impact_Player", art, "Impact_Player", sortingOrder: 21);
            var enemyImpact = CreateEffectPrefab("Effect_Impact_Enemy", art, "Impact_Enemy", sortingOrder: 21);
            var deathBubbles = CreateEffectPrefab("Effect_EnemyDeath", art, "EnemyDeath_Bubbles", sortingOrder: 7);
            var dodgeTrail = CreateEffectPrefab("Effect_DodgeTrail", art, "DodgeTrail", sortingOrder: 9);
            var poison = CreateEffectPrefab("Effect_Poison", art, "Poison", sortingOrder: 8);
            AssetDatabase.SaveAssets();

            // El color del impacto dice de quién era el disparo: lima del jugador, rosa de los enemigos.
            foreach (var name in new[] { "Projectile_Player", "Projectile_Stinger", "Projectile_Spore" })
                EditPrefab($"{ProjectilePrefabsFolder}/{name}.prefab", root => root.GetComponent<Projectile>().SetImpactEffect(playerImpact));
            foreach (var name in new[] { "Projectile_Enemy", "Projectile_BossSpiral", "Projectile_BossBurst" })
                EditPrefab($"{ProjectilePrefabsFolder}/{name}.prefab", root => root.GetComponent<Projectile>().SetImpactEffect(enemyImpact));

            EditAttack("Attack_Mandibles", attack => attack.SetMeleeHitEffect(playerImpact));
            EditAttack("Attack_Stinger", attack => attack.SetPoisonEffect(poison));

            foreach (var name in new[] { "Enemy_Chaser", "Enemy_Shooter", "Enemy_Small" })
                EditPrefab($"{EnemyPrefabsFolder}/{name}.prefab", root =>
                {
                    if (root.TryGetComponent<EnemyAnimator>(out var animator))
                        SetFields(animator, ("deathEffect", deathBubbles));
                    else
                        Debug.LogWarning($"ArtSetup: {name} no tiene EnemyAnimator; importa antes las animaciones de enemigos.");
                });

            EditPrefab(PlayerSetup.PlayerPath, root => GetOrAdd<DodgeTrail>(root).Configure(dodgeTrail));

            // Dibujados a su tamaño real: tinta de radio 3 y onda de radio 4.
            var inkCloud = AssetDatabase.LoadAssetAtPath<GameObject>($"{MutationPrefabsFolder}/Effect_InkCloud.prefab");
            if (inkCloud == null || !inkCloud.TryGetComponent<InkCloudArea>(out _))
                UpdateAbilityEffect("Effect_InkCloud", art, "InkCloud", "Mutation_InkSac", drawnRadius: 3f);
            UpdateAbilityEffect("Effect_Shockwave", art, "Shockwave", "Mutation_ShockGland", drawnRadius: 4f);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Crea o actualiza el prefab de un efecto: sprite animado y, si no va en bucle,
        /// <see cref="OneShotEffect"/> para que se destruya al terminar la animación.
        /// </summary>
        static GameObject CreateEffectPrefab(string prefabName, ImportedArt art, string clip, int sortingOrder)
        {
            if (!art.Controllers.TryGetValue(clip, out var controller))
            {
                Debug.LogWarning($"ArtSetup: falta la hoja {clip}.png; no se crea {prefabName}.");
                return null;
            }

            return EditPrefab($"{EffectPrefabsFolder}/{prefabName}.prefab", root =>
            {
                SetAnimatedSprite(root, controller, art.FirstSprites[clip], sortingOrder);

                bool loops = AnimationUtility.GetAnimationClipSettings(art.Clips[clip]).loopTime;
                if (loops)
                {
                    if (root.TryGetComponent<OneShotEffect>(out var oneShot))
                        Object.DestroyImmediate(oneShot);
                }
                else
                {
                    GetOrAdd<OneShotEffect>(root).SetDuration(art.Length(clip));
                }
            }, create: true);
        }

        /// <summary>
        /// Cambia el círculo provisional de una habilidad por su animación. Quita el crecimiento de
        /// FadeOutEffect y le dice a la mutación a qué radio está dibujado, para que no se agrande.
        /// </summary>
        static void UpdateAbilityEffect(string prefabName, ImportedArt art, string clip, string mutationName, float drawnRadius)
        {
            if (!art.Controllers.TryGetValue(clip, out var controller))
            {
                Debug.LogWarning($"ArtSetup: falta la hoja {clip}.png; {prefabName} no cambia.");
                return;
            }

            EditPrefab($"{MutationPrefabsFolder}/{prefabName}.prefab", root =>
            {
                if (root.TryGetComponent<FadeOutEffect>(out var fade))
                    Object.DestroyImmediate(fade);

                var renderer = root.GetComponent<SpriteRenderer>();
                SetAnimatedSprite(root, controller, art.FirstSprites[clip], renderer.sortingOrder);
                GetOrAdd<OneShotEffect>(root).SetDuration(art.Length(clip));
            });

            var mutation = AssetDatabase.LoadAssetAtPath<AbilityMutation>($"{MutationDataFolder}/{mutationName}.asset");
            if (mutation == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {mutationName}; créalo con Primordia > Mutations.");
                return;
            }

            mutation.SetEffectRadius(drawnRadius);
            EditorUtility.SetDirty(mutation);
        }

        static void SetAnimatedSprite(GameObject root, AnimatorController controller, Sprite sprite, int sortingOrder)
        {
            root.transform.localScale = Vector3.one;
            var renderer = GetOrAdd<SpriteRenderer>(root);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            GetOrAdd<Animator>(root).runtimeAnimatorController = controller;
        }

        static void EditAttack(string attackName, System.Action<AttackData> edit)
        {
            var attack = CombatSetup.LoadAttack(attackName);
            if (attack == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {attackName}; créalo con Primordia > Combat o Mutations.");
                return;
            }

            edit(attack);
            EditorUtility.SetDirty(attack);
        }

        /// <summary>
        /// Abre el prefab, aplica los cambios y lo guarda. Si no existe, lo crea (con <paramref name="create"/>)
        /// o avisa y devuelve null.
        /// </summary>
        static GameObject EditPrefab(string path, System.Action<GameObject> edit, bool create = false)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            if (!exists && !create)
            {
                Debug.LogWarning($"ArtSetup: no existe {path}.");
                return null;
            }

            var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(Path.GetFileNameWithoutExtension(path));
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        [MenuItem("Primordia/Art/Importar animaciones de proyectiles")]
        static void ImportProjectiles()
        {
            var art = Import(ProjectilesArt);
            if (art == null)
                return;

            foreach (var (name, controller) in art.Controllers)
                ApplyToProjectilePrefab($"{ProjectilesArt.SheetPrefix}{name}", controller, art.FirstSprites[name]);

            // El Aguijón se rehízo: su proyectil bueno (y la púa del Erizo) está en la carpeta de evoluciones.
            if (Directory.Exists(EvolutionsSourceFolder + "/Projectiles"))
                ImportEvolutionProjectiles();

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
                string clipName = ClipName(character, sheet);
                var sprites = SliceSheet(sheet, character.CellWidths.TryGetValue(clipName, out int width) ? width : 0, character.Pivot);
                var clip = CreateClip(character, clipName, sprites);
                art.Clips[clip.name] = clip;
                art.FirstSprites[clip.name] = sprites[0];
                if (clip.name == character.DefaultState)
                    art.DefaultSprite = sprites[0];
            }

            AssetDatabase.SaveAssets();
            if (character.ControllerPerClip)
            {
                foreach (var clip in art.Clips.Values)
                    art.Controllers[clip.name] = CreateController($"{character.AnimationsFolder}/{clip.name}.controller", new[] { clip }, clip.name);
            }
            else
            {
                art.Controller = CreateController(character.ControllerPath, art.Clips.Values, character.DefaultState);
            }

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
                    if (character.SkipClips.Contains(ClipName(character, source)))
                        continue;

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
                .Where(path => !character.SkipClips.Contains(ClipName(character, path)))
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path)
                .ToList();
        }

        /// <summary>Nombre del clip de una hoja: Bacteria_Move.png → Move.</summary>
        static string ClipName(Character character, string sheetPath) =>
            Path.GetFileNameWithoutExtension(sheetPath).Substring(character.SheetPrefix.Length);

        /// <summary>
        /// Configura la hoja como pixel art y la corta en cuadros del alto de la imagen y del ancho indicado
        /// (0 = cuadrados), con ese pivote. Conserva los identificadores de los cuadros que ya existían.
        /// </summary>
        static Sprite[] SliceSheet(string path, int cellWidth) => SliceSheet(path, cellWidth, new Vector2(0.5f, 0.5f));

        static Sprite[] SliceSheet(string path, int cellWidth, Vector2 pivot)
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
            int cell = cellWidth > 0 ? cellWidth : height;
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
                    rect = new Rect(i * cell, 0, cell, height),
                    alignment = pivot == new Vector2(0.5f, 0.5f) ? SpriteAlignment.Center : SpriteAlignment.Custom,
                    pivot = pivot,
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
        static AnimationClip CreateClip(Character character, string name, Sprite[] sprites)
        {
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

            // Con duraciones por cuadro, el clip se muestrea cada 10 ms para que cada cambio caiga en su sitio.
            bool customDurations = character.FrameDurations.TryGetValue(name, out var durations);
            if (customDurations && durations.Length != sprites.Length)
            {
                Debug.LogWarning($"ArtSetup: {character.Name}/{name} tiene {sprites.Length} cuadros y {durations.Length} duraciones; se usa ritmo fijo.");
                customDurations = false;
            }
            clip.frameRate = customDurations ? 100f : timing.fps;

            // El último cuadro se repite al final para que dure lo mismo que los demás (o lo que diga su duración).
            var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
            float time = 0f;
            for (int i = 0; i <= sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = time, value = sprites[Mathf.Min(i, sprites.Length - 1)] };
                if (i < sprites.Length)
                    time += customDurations ? durations[i] : 1f / timing.fps;
            }
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

        /// <summary>
        /// Animator con un estado por clip y sin transiciones: el código elige el estado.
        /// Con un solo clip, ese estado es el inicial y se reproduce solo.
        /// </summary>
        static AnimatorController CreateController(string path, IEnumerable<AnimationClip> clips, string defaultState)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;

            foreach (var clip in clips)
            {
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name)
                    ?? machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name == defaultState)
                    machine.defaultState = state;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// Pone el sprite animado al proyectil y deja su escala en 1 (el sprite ya tiene su tamaño).
        /// El radio de colisión se multiplica por la escala que tenía, para que el juego no cambie.
        /// </summary>
        static void ApplyToProjectilePrefab(string prefabName, AnimatorController controller, Sprite sprite)
        {
            string path = $"{ProjectilePrefabsFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"ArtSetup: no existe {path}; créalo antes con Primordia > Combat, Mutations o Boss.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var scale = root.transform.localScale;
                var collider = root.GetComponent<CircleCollider2D>();
                collider.radius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                root.transform.localScale = Vector3.one;

                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;

                // Al sacarlo del pool el Animator empieza de nuevo: el mordisco de Mandíbulas se ve en cada disparo.
                var animator = GetOrAdd<Animator>(root);
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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

                // El cuerpo tiene su animación Hurt; el destello queda para las piezas de mutación.
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
