using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Core;
using Primordia.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Importa las hojas de sprites del jugador desde docs/multimedia-temp/player (fuera del repositorio),
    /// las corta en cuadros, crea un clip por hoja y el Animator, y se los pone al prefab del jugador.
    /// Se puede ejecutar de nuevo cada vez que cambie el arte: sobrescribe las hojas y actualiza los clips.
    /// </summary>
    public static class ArtSetup
    {
        const string PlayerSourceFolder = "docs/multimedia-temp/player";
        const string PlayerArtFolder = "Assets/_Primordia/Art/Characters/Player";
        const string PlayerAnimationsFolder = PlayerArtFolder + "/Animations";
        const string PlayerControllerPath = PlayerAnimationsFolder + "/Player.controller";
        const string SheetPrefix = "Player_";
        const string DefaultState = "Idle_down";
        const int PixelsPerUnit = 16;

        // Cuadros por segundo de cada animación, según el prefijo del clip.
        // La esquiva dura 0,25 s en PlayerController: 5 cuadros a 20 fps.
        static readonly (string prefix, float fps, bool loop)[] Timings =
        {
            ("Idle", 6f, true),
            ("Move", 10f, true),
            ("Dodge", 20f, false),
            ("Hurt", 10f, false),
            ("Death", 10f, false),
        };

        static readonly EditorCurveBinding SpriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        [MenuItem("Primordia/Art/Importar animaciones del jugador")]
        static void ImportPlayer()
        {
            var sheets = CopySheets(PlayerSourceFolder, PlayerArtFolder);
            if (sheets.Count == 0)
            {
                EditorUtility.DisplayDialog("Primordia", $"No hay hojas {SheetPrefix}*.png en {PlayerSourceFolder} ni en {PlayerArtFolder}.", "Vale");
                return;
            }

            Directory.CreateDirectory(PlayerAnimationsFolder);
            var clips = new List<AnimationClip>();
            Sprite firstIdle = null;
            foreach (var sheet in sheets)
            {
                var sprites = SliceSheet(sheet);
                var clip = CreateClip(sheet, sprites);
                clips.Add(clip);
                if (clip.name == DefaultState)
                    firstIdle = sprites[0];
            }

            AssetDatabase.SaveAssets();
            var controller = CreateController(clips);
            ApplyToPlayerPrefab(controller, firstIdle);
            AssetDatabase.SaveAssets();

            Debug.Log($"ArtSetup: {clips.Count} animaciones del jugador importadas en {PlayerAnimationsFolder}.");
        }

        /// <summary>Copia las hojas de la carpeta temporal (si existe) y devuelve las que hay en el proyecto.</summary>
        static List<string> CopySheets(string sourceFolder, string targetFolder)
        {
            Directory.CreateDirectory(targetFolder);

            if (Directory.Exists(sourceFolder))
            {
                foreach (var source in Directory.GetFiles(sourceFolder, SheetPrefix + "*.png"))
                {
                    var target = $"{targetFolder}/{Path.GetFileName(source)}";
                    File.Copy(source, target, overwrite: true);
                    AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate);
                }
            }
            else
            {
                Debug.LogWarning($"ArtSetup: no existe {sourceFolder}; se usan las hojas que ya están en {targetFolder}.");
            }

            return Directory.GetFiles(targetFolder, SheetPrefix + "*.png")
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

        /// <summary>Crea o actualiza el clip de una hoja: Player_Idle_side.png → Idle_side.anim.</summary>
        static AnimationClip CreateClip(string sheetPath, Sprite[] sprites)
        {
            string name = Path.GetFileNameWithoutExtension(sheetPath).Substring(SheetPrefix.Length);
            var timing = Timings.FirstOrDefault(t => name.StartsWith(t.prefix));
            if (timing.prefix == null)
                timing = (name, 10f, false);

            string clipPath = $"{PlayerAnimationsFolder}/{name}.anim";
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

        /// <summary>Animator con un estado por clip y sin transiciones: PlayerAnimator elige el estado por código.</summary>
        static AnimatorController CreateController(List<AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            var machine = controller.layers[0].stateMachine;

            foreach (var clip in clips)
            {
                var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name)
                    ?? machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name == DefaultState)
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
                var body = root.transform.Find("Body");
                var bodyRenderer = body.GetComponent<SpriteRenderer>();
                if (idleSprite != null)
                    bodyRenderer.sprite = idleSprite;
                bodyRenderer.color = Color.white;

                if (!body.TryGetComponent<Animator>(out var animator))
                    animator = body.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                // La boca sigue siendo el punto de disparo, pero ya no se dibuja: la célula mira hacia el cursor.
                var mouth = root.transform.Find("AimPivot/Mouth");
                if (mouth != null && mouth.TryGetComponent<SpriteRenderer>(out var mouthRenderer))
                    mouthRenderer.enabled = false;

                if (!root.TryGetComponent<PlayerAnimator>(out var playerAnimator))
                    playerAnimator = root.AddComponent<PlayerAnimator>();
                var serialized = new SerializedObject(playerAnimator);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("body").objectReferenceValue = bodyRenderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();

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
    }
}
