using System;
using System.Collections.Generic;
using System.IO;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Enemies;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Menú Primordia > Audio > Importar audio: copia la música y los efectos de docs/multimedia-temp/audio (los genera
    /// generar_audio.py, que también escribe sonidos.json con el volumen, el grupo y el bucle de cada uno), crea la
    /// <see cref="SoundLibrary"/> en Resources y conecta los sonidos que van en los datos: el disparo de cada ataque, el
    /// daño y la muerte de cada enemigo, la esquiva de las Aletas y los efectos visuales que suenan al aparecer. El
    /// resto (salas, jefes, tienda, interfaz, música) los pide el código por su nombre (<see cref="Sounds"/>). Se puede
    /// repetir cada vez que cambie el audio.
    /// </summary>
    public static class AudioSetup
    {
        const string SourceFolder = "docs/multimedia-temp/audio";
        const string AudioFolder = "Assets/_Primordia/Audio";
        const string SfxFolder = AudioFolder + "/SFX";
        const string MusicFolder = AudioFolder + "/Music";
        const string ResourcesFolder = AudioFolder + "/Resources";
        const string LibraryPath = ResourcesFolder + "/" + SoundLibrary.ResourcePath + ".asset";
        const string AttacksFolder = "Assets/_Primordia/Data/Attacks";
        const string EnemiesFolder = "Assets/_Primordia/Data/Enemies";
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations";
        const string EffectsFolder = "Assets/_Primordia/Prefabs/Effects";

        const float DefaultInterval = 0.03f;
        const float DefaultPitchJitter = 0.04f;

        // Segundos mínimos entre dos veces seguidas, para los que se piden muchos a la vez.
        static readonly Dictionary<string, float> Intervals = new()
        {
            ["Enemy_Bacteria_Aggro"] = 0.25f, ["Enemy_Hit"] = 0.05f, ["Enemy_Death"] = 0.06f,
            ["Enemy_Amoeba_Windup"] = 0.15f, ["Enemy_Amoeba_Shot"] = 0.06f,
            ["Boss_Virus_Spiral"] = 0.06f, ["Boss_Virus_Burst"] = 0.2f, ["Boss_Amoeba_Barrage"] = 0.07f,
            ["Boss_Amoeba_Immune"] = 0.08f, ["Boss_Amoeba_Bounce"] = 0.2f,
            ["Status_Poison"] = 0.15f, ["Status_Bleed"] = 0.1f, ["Status_Blind"] = 0.3f, ["Status_Stun"] = 0.15f,
            ["Status_Panic"] = 0.3f, ["Status_Suspicion"] = 0.3f, ["Status_Discovered"] = 0.3f,
            ["Status_PoisonBurst"] = 0.1f, ["Status_SporeCloud"] = 0.1f,
            ["Organ_Shockwave_ProjectileBreak"] = 0.05f, ["Organ_Shockwave_Paralyze"] = 0.3f,
            ["Projectile_Impact"] = 0.05f, ["Mouth_Spore_Pop"] = 0.05f, ["Mutation_Bubble_Burst"] = 0.15f,
            ["Lineage_StingingTrail"] = 0.5f, ["Lineage_Finisher"] = 0.08f, ["Limbs_Hyphae_Trail"] = 0.1f,
            ["Skin_Thorns"] = 0.1f, ["Biomass_Pickup_Small"] = 0.04f, ["Organ_Vacuole_Charge"] = 0.05f,
            // Piso 2
            ["Enemy_Jelly_Ring"] = 0.2f, ["Enemy_Shell_Hit"] = 0.05f, ["Enemy_Jelly_Hit"] = 0.05f,
            ["Enemy_Sponge_Hit"] = 0.05f, ["Enemy_Pikaia_Death"] = 0.06f, ["Enemy_Pikaia_Dash"] = 0.3f,
            ["Enemy_Shield_Block"] = 0.08f, ["Enemy_Trilobite_Warn"] = 0.15f, ["Enemy_Ottoia_Warn"] = 0.15f,
            ["Boss_Anomalocaris_Bubble"] = 0.1f, ["Boss_Anomalocaris_BubblePop"] = 0.06f,
            ["Boss_Eurypterid_FuryShot"] = 0.07f, ["Boss_Eurypterid_Step"] = 0.2f, ["Boss_Eurypterid_ArmorBlock"] = 0.08f,
            ["Cave_Pebble"] = 0.05f, ["Cave_RockFall"] = 0.1f, ["Organ_BroodSac_LarvaBite"] = 0.08f,
            ["Limbs_Hallucigenia_Strike"] = 0.08f, ["Limbs_Hallucigenia_Place"] = 0.1f, ["Limbs_Hallucigenia_Launch"] = 0.1f,
            ["Skin_Trilobite_Bounce"] = 0.05f, ["Special_Cuticle_Reflect"] = 0.05f, ["Special_Flagella_Lash"] = 0.1f,
            ["Status_Shatter"] = 0.1f, ["Limbs_Tail_BubblePop"] = 0.05f, ["Lineage_Carrion_Eat"] = 0.1f,
            ["Mouth_ArmCrown_Tide"] = 0.1f,
        };

        // Variación de tono distinta de la normal (la biomasa ya sube de tono al encadenarse).
        static readonly Dictionary<string, float> PitchJitters = new()
        {
            ["Biomass_Pickup_Small"] = 0f, ["Biomass_Pickup_Big"] = 0f, ["Room_Cleared"] = 0f, ["Room_Exit"] = 0f,
            ["Mutation_LevelUp"] = 0f, ["Mutation_LevelUp_Gold"] = 0f, ["Mutation_ChoiceOpen"] = 0f,
            ["Lineage_Complete"] = 0f, ["Player_Heal"] = 0f, ["Shop_HeartContainer"] = 0f,
            // La Carroña sube de tono con cada carga; los acordes, en su nota.
            ["Lineage_Carrion_Eat"] = 0f, ["Room_HeartCoral"] = 0f, ["Exchange_Capsule_Accept"] = 0f,
            ["Exchange_Capsule_Open"] = 0f, ["Room_Clam_Open_Big"] = 0f, ["Room_WaveStart"] = 0f,
            ["Room_Slabs_Open"] = 0f, ["Special_Flagella_Ready"] = 0f,
        };

        // Ataque → (disparo, golpe, avance, impacto).
        static readonly (string asset, string fire, string hit, string lunge, string impact)[] AttackSounds =
        {
            ("Attack_PlayerBasic", "Player_Shot", null, null, "Projectile_Impact"),
            ("Attack_Mandibles", "Mouth_Mandibles_Bite", "Mouth_Mandibles_BiteHit", "Mouth_Mandibles_Lunge", null),
            ("Attack_Mandibles_2", "Mouth_Mandibles_Bite", "Mouth_Mandibles_BiteHit", "Mouth_Mandibles_Lunge", null),
            ("Attack_Mandibles_3", "Mouth_Mandibles_Bite", "Mouth_Mandibles_BiteHit", "Mouth_Mandibles_Lunge", null),
            ("Attack_Stinger", "Mouth_Stinger_Shot", null, null, "Projectile_Impact"),
            ("Attack_Stinger_2", "Mouth_Stinger_Shot", null, null, "Projectile_Impact"),
            ("Attack_Stinger_3", "Mouth_Stinger_Shot", null, null, "Projectile_Impact"),
            ("Attack_Spores", "Mouth_Spores_Puff", null, null, "Mouth_Spore_Pop"),
            ("Attack_Spores_2", "Mouth_Spores_Puff", null, null, "Mouth_Spore_Pop"),
            ("Attack_Spores_3", "Mouth_Spores_Puff", null, null, "Mouth_Spore_Pop"),
            // Las púas del Erizo suenan todas juntas en QuillBurst.
            ("Attack_Quill", null, null, null, "Projectile_Impact"),
            ("Attack_EnemyBasic", "Enemy_Amoeba_Shot", null, null, "Projectile_Impact"),
            ("Attack_EnemyFan", "Enemy_Amoeba_Shot", null, null, "Projectile_Impact"),
            ("Attack_BossSpiral", "Boss_Virus_Spiral", null, null, "Projectile_Impact"),
            ("Attack_BossBurst", "Boss_Virus_Burst", null, null, "Projectile_Impact"),
            ("Attack_AmoebaRing", "Boss_Amoeba_Barrage", null, null, null),
            ("Attack_AmoebaSpiral", "Boss_Amoeba_Barrage", null, null, null),
            ("Attack_AmoebaFan", "Boss_Amoeba_Barrage", null, null, null),
            // Piso 2. El Ramillete de los Cnidocitos y las púas de Hallucigenia suenan desde su código.
            ("Attack_Cnidocytes", "Mouth_Cnidocytes_Shot", null, null, "Projectile_Impact"),
            ("Attack_Cnidocytes_2", "Mouth_Cnidocytes_Shot", null, null, "Projectile_Impact"),
            ("Attack_Cnidocytes_3", "Mouth_Cnidocytes_Shot", null, null, "Projectile_Impact"),
            ("Attack_JawPlates", "Mouth_JawPlates_Bite", "Mouth_JawPlates_BiteHit", null, null),
            ("Attack_JawPlates_2", "Mouth_JawPlates_Bite", "Mouth_JawPlates_BiteHit", null, null),
            ("Attack_JawPlates_3", "Mouth_JawPlates_Bite", "Mouth_JawPlates_BiteHit", null, null),
            // Remolino: el disparo es su aparición y el impacto, la Marea del nivel III.
            ("Attack_ArmCrown", "Mouth_ArmCrown_Cast", null, null, "Mouth_ArmCrown_Tide"),
            ("Attack_ArmCrown_2", "Mouth_ArmCrown_Cast", null, null, "Mouth_ArmCrown_Tide"),
            ("Attack_ArmCrown_3", "Mouth_ArmCrown_Cast", null, null, "Mouth_ArmCrown_Tide"),
            ("Attack_HallucigeniaSpine", null, null, null, "Projectile_Impact"),
            ("Attack_WiwaxiaSclerite", null, null, null, "Projectile_Impact"),
            ("Attack_CuticleReflected", null, null, null, "Projectile_Impact"),
            // Los 8 proyectiles del anillo salen a la vez: el intervalo los junta en un solo latido.
            ("Attack_BlueJelly", "Enemy_Jelly_Ring", null, null, "Projectile_Impact"),
        };

        // Enemigo → (daño, muerte, división, alerta, aviso de disparo).
        static readonly (string asset, string hurt, string death, string split, string alert, string aim)[] EnemySounds =
        {
            ("Enemy_Chaser", "Enemy_Hit", "Enemy_Death", null, "Enemy_Bacteria_Aggro", null),
            ("Enemy_Shooter", "Enemy_Hit", "Enemy_Death", "Enemy_Amoeba_Split", null, "Enemy_Amoeba_Windup"),
            ("Enemy_Small", "Enemy_Hit", "Enemy_SmallAmoeba_Death", null, null, null),
            ("Enemy_VirusBoss", "Boss_Virus_Hurt", "Boss_Virus_Death", null, null, null),
            ("Enemy_AmoebaBoss", "Boss_Amoeba_Hurt", "Boss_Amoeba_Death", null, null, null),
            ("Floor2/Enemy_Trilobite", "Enemy_Shell_Hit", "Enemy_Trilobite_Death", null, null, null),
            ("Floor2/Enemy_BlueJelly", "Enemy_Jelly_Hit", "Enemy_Jelly_Death", null, null, null),
            ("Floor2/Enemy_Ottoia", "Enemy_Hit", "Enemy_Ottoia_Death", null, null, null),
            ("Floor2/Enemy_Vauxia", "Enemy_Sponge_Hit", "Enemy_Vauxia_Death", null, null, null),
            ("Floor2/Enemy_Pikaia", "Enemy_Hit", "Enemy_Pikaia_Death", null, null, null),
            ("Floor2/Enemy_AnomalocarisBoss", "Boss_Anomalocaris_Hurt", "Boss_Anomalocaris_Death", null, null, null),
            ("Floor2/Enemy_EurypteridBoss", "Boss_Eurypterid_Hurt", "Boss_Eurypterid_Death", null, null, null),
        };

        // Efectos visuales que suenan al aparecer.
        static readonly (string prefab, string sound)[] EffectSounds =
        {
            ("Effect_Stomp", "Limbs_Legs_Stomp"),
            ("Effect_Ambush", "Limbs_Fins_Ambush"),
            ("Effect_Plates", "Skin_Plates_Restore"),
            ("Effect_PlatesBreak", "Skin_Plates_Block"),
            ("Effect_LevelUp", "Mutation_LevelUp"),
            ("Effect_LevelUp_Gold", "Mutation_LevelUp_Gold"),
            ("Effect_LineageLost", "Lineage_Lost"),
            ("Effect_ShopPurchase", "Shop_Purchase"),
            ("Effect_Poison", "Status_Poison"),
            ("Effect_Status_Bleed", "Status_Bleed"),
            ("Effect_Status_Blind", "Status_Blind"),
            ("Effect_Status_Stun", "Status_Stun"),
            ("Effect_Status_Paralyzed", "Organ_Shockwave_Paralyze"),
            ("Effect_Status_Scared", "Status_Panic"),
            ("Effect_Status_Suspect", "Status_Suspicion"),
            ("Effect_MutationBurst_Mouth", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Limbs", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Skin", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Organ", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Predator", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Armored", "Mutation_Bubble_Burst"),
            ("Effect_MutationBurst_Toxic", "Mutation_Bubble_Burst"),
            // Piso 2
            ("Effect_JawPlates_Guillotine", "Mouth_JawPlates_Guillotine"),
            ("Effect_Status_Shatter", "Status_Shatter"),
            ("Effect_TailPropulsor_Pop", "Limbs_Tail_BubblePop"),
            ("Effect_BroodSac_Open", "Organ_BroodSac_Open"),
            ("Effect_BroodSac_Split", "Organ_BroodSac_Split"),
            ("Effect_Vauxia_HealPulse", "Enemy_Vauxia_Pulse"),
            ("Effect_AnomalocarisBubble_Pop", "Boss_Anomalocaris_BubblePop"),
            ("Effect_Anomalocaris_Swipe", "Boss_Anomalocaris_Swipe"),
        };

        // Los campos de sonidos.json los rellena JsonUtility.
#pragma warning disable 0649
        [Serializable]
        class Manifest
        {
            public ManifestSound[] sounds;
            public ManifestTrack[] tracks;
        }

        [Serializable]
        class ManifestSound
        {
            public string id;
            public string[] files;
            public float volume;
            public string group;
            public bool loop;
        }

        [Serializable]
        class ManifestTrack
        {
            public string id;
            public string file;
            public float volume;
            public bool loop;
        }
#pragma warning restore 0649

        [MenuItem("Primordia/Audio/Importar audio")]
        public static void ImportAudio()
        {
            string manifestPath = Path.Combine(SourceFolder, "sonidos.json");
            if (!File.Exists(manifestPath))
            {
                EditorUtility.DisplayDialog("Importar audio",
                    $"No está {manifestPath}. Ejecuta antes generar_audio.py en {SourceFolder}.", "Vale");
                return;
            }

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            EnsureFolder(SfxFolder);
            EnsureFolder(MusicFolder);
            EnsureFolder(ResourcesFolder);

            int copied = CopyAll(manifest);
            AssetDatabase.Refresh();
            ConfigureImporters(manifest);

            var library = BuildLibrary(manifest, out var missing);
            int attacks = ConnectAttacks();
            int enemies = ConnectEnemies();
            ConnectMutations();
            int effects = ConnectEffects();
            AssetDatabase.SaveAssets();

            Debug.Log($"AudioSetup: {copied} archivos copiados; biblioteca con {library.Sounds.Count} efectos y " +
                      $"{library.Tracks.Count} pistas; sonido en {attacks} ataques, {enemies} enemigos y {effects} efectos." +
                      (missing.Count > 0 ? $" Faltan: {string.Join(", ", missing)}." : ""));
            Selection.activeObject = library;
        }

        static int CopyAll(Manifest manifest)
        {
            int copied = 0;
            foreach (var sound in manifest.sounds)
            {
                foreach (var file in sound.files)
                    copied += Copy(Path.Combine(SourceFolder, "SFX", file), $"{SfxFolder}/{file}");
            }
            foreach (var track in manifest.tracks)
                copied += Copy(Path.Combine(SourceFolder, "Music", track.file), $"{MusicFolder}/{track.file}");
            return copied;
        }

        static int Copy(string from, string to)
        {
            if (!File.Exists(from))
            {
                Debug.LogWarning($"AudioSetup: no está {from}.");
                return 0;
            }
            File.Copy(from, to, overwrite: true);
            return 1;
        }

        static void ConfigureImporters(Manifest manifest)
        {
            foreach (var sound in manifest.sounds)
            {
                foreach (var file in sound.files)
                {
                    if (AssetImporter.GetAtPath($"{SfxFolder}/{file}") is not AudioImporter importer)
                        continue;
                    importer.forceToMono = true;
                    importer.loadInBackground = false;
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = sound.loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = sound.loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                    settings.quality = 0.7f;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            foreach (var track in manifest.tracks)
            {
                if (AssetImporter.GetAtPath($"{MusicFolder}/{track.file}") is not AudioImporter importer)
                    continue;
                importer.forceToMono = false;
                importer.loadInBackground = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = track.loop ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        static SoundLibrary BuildLibrary(Manifest manifest, out List<string> missing)
        {
            missing = new List<string>();
            var sounds = new List<SoundLibrary.Sound>();
            foreach (var entry in manifest.sounds)
            {
                var clips = new List<AudioClip>();
                foreach (var file in entry.files)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{file}");
                    if (clip != null)
                        clips.Add(clip);
                    else
                        missing.Add(file);
                }

                bool ui = entry.group == "UI";
                sounds.Add(new SoundLibrary.Sound
                {
                    id = entry.id,
                    clips = clips.ToArray(),
                    volume = entry.volume,
                    group = ui ? SoundGroup.UI : SoundGroup.Sfx,
                    loop = entry.loop,
                    minInterval = Intervals.TryGetValue(entry.id, out float interval) ? interval : DefaultInterval,
                    pitchJitter = ui || entry.loop ? 0f
                        : PitchJitters.TryGetValue(entry.id, out float jitter) ? jitter : DefaultPitchJitter
                });
            }

            var tracks = new List<SoundLibrary.Track>();
            foreach (var entry in manifest.tracks)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{MusicFolder}/{entry.file}");
                if (clip == null)
                    missing.Add(entry.file);
                tracks.Add(new SoundLibrary.Track { id = entry.id, clip = clip, volume = entry.volume, loop = entry.loop });
            }

            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.Configure(sounds.ToArray(), tracks.ToArray());
            EditorUtility.SetDirty(library);
            return library;
        }

        static int ConnectAttacks()
        {
            int count = 0;
            foreach (var (asset, fire, hit, lunge, impact) in AttackSounds)
            {
                var attack = AssetDatabase.LoadAssetAtPath<AttackData>($"{AttacksFolder}/{asset}.asset");
                if (attack == null)
                    continue;
                attack.ConfigureSounds(fire, hit, lunge, impact);
                EditorUtility.SetDirty(attack);
                count++;
            }
            return count;
        }

        static int ConnectEnemies()
        {
            int count = 0;
            foreach (var (asset, hurt, death, split, alert, aim) in EnemySounds)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemiesFolder}/{asset}.asset");
                if (data == null)
                    continue;
                data.ConfigureSounds(hurt, death, split, alert, aim);
                EditorUtility.SetDirty(data);
                count++;
            }
            return count;
        }

        // Extremidades con su propio sonido de esquiva.
        static readonly (string asset, string sound)[] DodgeSounds =
        {
            ("Mutation_Fins", "Limbs_Fins_Dodge"),
            ("Floor2/Mutation_TailPropulsor", "Limbs_Tail_Dodge"),
        };

        static void ConnectMutations()
        {
            foreach (var (asset, sound) in DodgeSounds)
            {
                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationsFolder}/{asset}.asset");
                if (mutation == null)
                    continue;
                mutation.SetDodgeSound(sound);
                EditorUtility.SetDirty(mutation);
            }
        }

        static int ConnectEffects()
        {
            int count = 0;
            foreach (var (prefabName, sound) in EffectSounds)
            {
                string path = $"{EffectsFolder}/{prefabName}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!root.TryGetComponent<EffectSound>(out var effectSound))
                        effectSound = root.AddComponent<EffectSound>();
                    effectSound.Configure(sound);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return count;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
