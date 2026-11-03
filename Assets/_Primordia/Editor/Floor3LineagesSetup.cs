using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Linajes del piso 3 (tabla del GDD): crea los 8 <see cref="LineageData"/> con sus mutaciones del piso 3 y su evolución
    /// (docs/multimedia-temp/floor3-lineages), los vincula a las mutaciones y monta el arte del Parásito (marca, salto, gota
    /// de vida y curación, docs/multimedia-temp/floor3-mutations) en los beneficios del jugador. Los toques propios de cada
    /// linaje llegan en el subpaso 4b, por tandas (lo ya programado está en <c>Touches</c>). Necesita haber creado antes las mutaciones del piso 3 (Bocas y las tres ranuras) y los
    /// linajes del piso 1 (el jugador con <see cref="PlayerLineage"/>). Se puede repetir.
    /// </summary>
    public static class Floor3LineagesSetup
    {
        const string MutationsFolder = "Assets/_Primordia/Data/Mutations/Floor3";
        const string LineagesFolder = MutationsFolder + "/Lineages";
        const string SourceFolder = "docs/multimedia-temp/floor3-mutations";
        const string KitPath = "Assets/_Primordia/Data/Rooms/ReactionKit_Floor3.asset";
        const string LineageArtFolder = "docs/multimedia-temp/floor3-lineage-art";
        const string EffectsArtFolder = "Assets/_Primordia/Art/Effects/Floor3Mutations";

        // Nombre del asset (el de la hoja de evolución), nombre, clase y mutaciones (Boca, Extremidades, Piel, Órgano), sin "Mutation_".
        static readonly (string asset, string name, LineageClass lineageClass, string[] mutations)[] Lineages =
        {
            ("Eryops", "Eryops", LineageClass.Carnivore, new[] { "SpinningTop", "JumpingLegs", "AmphibianSkin", "MethaneSac" }),
            ("Xenacanthus", "Xenacanthus", LineageClass.Carnivore, new[] { "VinegarJet", "JumpingLegs", "CharredScales", "Pheromones" }),
            ("Pulmonoscorpius", "Pulmonoscorpius", LineageClass.Venomous, new[] { "VinegarJet", "SpinnerLegs", "CharredScales", "MethaneSac" }),
            ("Centipede", "Ciempiés", LineageClass.Venomous, new[] { "PoisonSpit", "JumpingLegs", "CharredScales", "Pheromones" }),
            ("TinderFungus", "Yesquero", LineageClass.Fungal, new[] { "PoisonSpit", "JumpingLegs", "CharredScales", "MethaneSac" }),
            ("Cordyceps", "Cordyceps", LineageClass.Fungal, new[] { "VinegarJet", "SpinnerLegs", "AmphibianSkin", "Pheromones" }),
            ("Lamprey", "Lamprea", LineageClass.Parasite, new[] { "SpinningTop", "SpinnerLegs", "AmphibianSkin", "Pheromones" }),
            ("Leech", "Sanguijuela", LineageClass.Parasite, new[] { "PoisonSpit", "SpinnerLegs", "AmphibianSkin", "MethaneSac" }),
        };

        // Toque propio de cada linaje: nombre y lo que hace (corto: se lee en la pausa). Se añaden según se programan (subpaso 4b).
        static readonly Dictionary<string, (LineageTouch touch, string name, string description)> Touches = new()
        {
            ["Xenacanthus"] = (LineageTouch.CephalicSpine, "Espina cefálica", "Los enemigos confundidos cuentan como heridos."),
            ["Pulmonoscorpius"] = (LineageTouch.GasSting, "Aguijón de gas",
                "El chorro envenena a lo que toca y vuelve nube tóxica el metano que alcanza, en vez de explotarlo."),
            ["Eryops"] = (LineageTouch.MudStalker, "Acecho del fango",
                "Cruzas el lodo sin hundirte, medio sumergido y más despacio. Ganas +30 % de velocidad en el lodo y 5 s después de salir (se renueva al entrar)."),
            ["Cordyceps"] = (LineageTouch.Zombie, "Zombi",
                "Un enemigo confundido al que le toca morir se levanta 4 s como tu aliado: ataca a los demás enemigos y no se le puede dañar."),
            ["TinderFungus"] = (LineageTouch.Tinder, "Yesca",
                "La bola del Escupitajo va ardiendo: su rastro es de fuego en vez de veneno y al explotar deja fuego."),
            ["Leech"] = (LineageTouch.LeechPool, "Charca de sanguijuelas",
                "Tus charcos y tu baba pegan parásitos y hacen más daño, y no arden. Al explotar la bola saltan parásitos a los enemigos más cercanos."),
            ["Centipede"] =(LineageTouch.HundredLegs, "Cien patas",
                "Cada aterrizaje deja un charco de veneno durante 4 s. Eres inmune a todo el veneno y al gas tóxico."),
            ["Lamprey"] = (LineageTouch.SpinningSucker, "Ventosa giratoria",
                "Mientras giras, cada parásito que pegas pega otro al enemigo más cercano y el máximo sube a 6."),
        };

        [MenuItem("Primordia/Mutations/Crear linajes del piso 3")]
        static void CreateAll()
        {
            Directory.CreateDirectory(LineagesFolder);
            var evolutions = ArtSetup.ImportFloor3Evolutions(Lineages.Select(l => l.asset));
            var missing = new HashSet<string>();
            int count = 0;

            foreach (var (asset, name, lineageClass, mutationNames) in Lineages)
            {
                var mutations = mutationNames.Select(m =>
                {
                    var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationsFolder}/Mutation_{m}.asset");
                    if (mutation == null)
                        missing.Add(m);
                    return mutation;
                }).ToArray();

                string path = $"{LineagesFolder}/Lineage_{asset}.asset";
                var lineage = AssetDatabase.LoadAssetAtPath<LineageData>(path);
                if (lineage == null)
                {
                    lineage = ScriptableObject.CreateInstance<LineageData>();
                    AssetDatabase.CreateAsset(lineage, path);
                }
                lineage.Configure(name, lineageClass, mutations, "");
                if (Touches.TryGetValue(asset, out var touch))
                    lineage.ConfigureTouch(touch.touch, touch.name, touch.description);
                // Como en el piso 2, no cambian piezas del cuerpo: solo la animación de la evolución.
                lineage.ConfigureLook(null, 0f, null, evolutions.TryGetValue(asset, out var evolution) ? evolution : null);
                EditorUtility.SetDirty(lineage);
                count++;
            }

            AssetDatabase.SaveAssets();
            LineageSetup.LinkMutations();
            SetUpParasite();
            SetUpLeechKit();
            SetUpTinderKit();
            SetUpFungusMats();
            SetUpZombie();
            SetUpEryops();

            if (missing.Count > 0)
                Debug.LogWarning($"Floor3LineagesSetup: faltan las mutaciones {string.Join(", ", missing)}; créalas con Primordia > Mutations / Floor 3.");
            Debug.Log($"Floor3LineagesSetup: {count} linajes del piso 3 en {LineagesFolder}, con su evolución y el arte del Parásito.");
        }

        // ---------- Parásito ----------

        static void SetUpParasite()
        {
            var marker = Floor2MouthsSetup.CreateLoopEffect("Effect_Status_Parasite", Sheet("Parasite", "Parasite_Attached", 12), 0.12f, sortingOrder: 22);
            var leap = Floor2MouthsSetup.CreateFrameEffect("Effect_Parasite_Leap", Sheet("Parasite", "Parasite_Leap", 16), 0.07f, sortingOrder: 23);
            var drop = Floor2MouthsSetup.CreateLoopEffect("Effect_Parasite_Drain", Sheet("Effects", "Fx_Parasite_Drain", 8), 0.1f, sortingOrder: 24);
            var heal = Floor2MouthsSetup.CreateFrameEffect("Effect_Parasite_Heal", Sheet("Effects", "Fx_Parasite_Heal", 32), 0.07f, sortingOrder: 21);
            AddArcFlight(leap);
            AddArcFlight(drop);

            // El parásito grande de la Lamprea (docs/multimedia-temp/floor3-lineage-art).
            var markerLamprey = Floor2MouthsSetup.CreateLoopEffect("Effect_Status_Parasite_Lamprey",
                LineageSheet("Parasite", "Parasite_Attached_Lamprey", 16), 0.15f, sortingOrder: 22);
            var leapLamprey = Floor2MouthsSetup.CreateFrameEffect("Effect_Parasite_Leap_Lamprey",
                LineageSheet("Parasite", "Parasite_Leap_Lamprey", 20), 0.08f, sortingOrder: 23);
            AddArcFlight(leapLamprey);
            var poisonJet = BuildPoisonJet();
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerLineage>(out var lineage))
                {
                    Debug.LogWarning("Floor3LineagesSetup: el jugador no tiene PlayerLineage; créalo con Primordia > Mutations > Crear linajes.");
                    return;
                }
                var benefits = lineage.Benefits ?? new LineageBenefitSettings();
                benefits.parasiteMarker = marker;
                benefits.parasiteJumpEffect = leap;
                benefits.parasiteDrainDrop = drop;
                benefits.parasiteHealEffect = heal;
                benefits.parasiteMarkerLamprey = markerLamprey;
                benefits.parasiteLeapLamprey = leapLamprey;
                if (poisonJet.art != null)
                {
                    benefits.poisonJetArt = poisonJet.art;
                    benefits.poisonJetHit = poisonJet.hit;
                    benefits.poisonJetMuzzle = poisonJet.muzzle;
                    benefits.poisonJetSplashes = poisonJet.splashes;
                }
                lineage.ConfigureBenefits(benefits);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Eryops (Acecho del fango) ----------

        const string PolishArtPath = "Assets/_Primordia/Resources/PolishArt.asset";

        /// <summary>
        /// El arte del Eryops (Eryops/) en el <see cref="PolishArt"/>: el barro seco, la estela, los terrones y las escamas. El barro en el
        /// cuerpo lo pinta <see cref="MudBodyVisual"/> con el arte del pulido, que hay que haber importado antes.
        /// </summary>
        static void SetUpEryops()
        {
            var art = AssetDatabase.LoadAssetAtPath<PolishArt>(PolishArtPath);
            if (art == null)
            {
                Debug.LogWarning("Floor3LineagesSetup: no hay PolishArt; ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3 (pulido) antes.");
                return;
            }

            string source = $"{LineageArtFolder}/Eryops";
            art.mudTextureDry = ArtSetup.ImportLooseSprite($"{source}/Fx_MudBody_TexDry.png", $"{EffectsArtFolder}/Fx_MudBody_TexDry.png");
            // Estela: pivote a la derecha; terrones: abajo, a 16 de 24 px; escamas: arriba (README).
            art.eryopsWake = ArtSetup.ImportLooseSheet($"{source}/Fx_EryopsMudWake.png", $"{EffectsArtFolder}/Fx_EryopsMudWake.png", 32, new Vector2(1f, 0.5f));
            art.eryopsKick = ArtSetup.ImportLooseSheet($"{source}/Fx_EryopsMudKick.png", $"{EffectsArtFolder}/Fx_EryopsMudKick.png", 24, new Vector2(16f / 24f, 0f));
            art.eryopsFlake = ArtSetup.ImportLooseSheet($"{source}/Fx_EryopsMudFlake.png", $"{EffectsArtFolder}/Fx_EryopsMudFlake.png", 8, new Vector2(0.5f, 1f));
            if (art.mudTextureDry == null || art.eryopsWake.Length == 0 || art.eryopsKick.Length == 0 || art.eryopsFlake.Length == 0)
                Debug.LogWarning("Floor3LineagesSetup: faltan hojas del Eryops en docs/multimedia-temp/floor3-lineage-art/Eryops.");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        // ---------- Zombi (Cordyceps) ----------

        /// <summary>El arte de los aliados levantados (Zombie/): hongos del lomo, estela, levantarse y caer. Pivote abajo en el centro.</summary>
        static void SetUpZombie()
        {
            var feet = new Vector2(0.5f, 0f);
            Sprite[] Sheet(string name, int cell) => ArtSetup.ImportLooseSheet($"{LineageArtFolder}/Zombie/{name}.png",
                $"{EffectsArtFolder}/{name}.png", cell, feet);

            var sprout = new LineageBenefitSettings.FrameSet[4];
            var idle = new LineageBenefitSettings.FrameSet[4];
            for (int i = 0; i < 4; i++)
            {
                sprout[i] = new LineageBenefitSettings.FrameSet { frames = Sheet($"Fx_ZombieFungus_{i}_Sprout", 8) };
                idle[i] = new LineageBenefitSettings.FrameSet { frames = Sheet($"Fx_ZombieFungus_{i}_Idle", 8) };
            }
            var trail = Sheet("Fx_ZombieSporeTrail", 12);
            var rise = Sheet("Fx_ZombieRise", 48);
            var crumble = Sheet("Fx_ZombieCrumble", 48);

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerLineage>(out var lineage))
                    return;
                var benefits = lineage.Benefits ?? new LineageBenefitSettings();
                benefits.zombieFungusSprout = sprout;
                benefits.zombieFungusIdle = idle;
                benefits.zombieSporeTrail = trail;
                benefits.zombieRise = rise;
                benefits.zombieCrumble = crumble;
                lineage.ConfigureBenefits(benefits);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Matas de hongo del piso 3 (Colonizar la sala) ----------

        // Las 4 especies de mata y las dos tallas (celda 48 y 64), de docs/multimedia-temp/floor3-lineage-art/FungusMat.
        static readonly string[] MatSpecies = { "Bells", "Puffballs", "Earthstars", "Clubs" };

        /// <summary>
        /// Crea un prefab por especie y talla (mata con <see cref="MyceliumPatch"/> y <see cref="FungusMat"/>) y el de los hilos que enredan
        /// a los enemigos, y los pone en los beneficios del jugador (Colonizar los usa en el piso 3).
        /// </summary>
        static void SetUpFungusMats()
        {
            var snareFrames = ArtSetup.ImportLooseSheet($"{LineageArtFolder}/FungusMat/Fx_FungusMat_Snare.png",
                $"{EffectsArtFolder}/Fx_FungusMat_Snare.png", 16, new Vector2(0.5f, 0f));
            var snare = Floor2MouthsSetup.CreateLoopEffect("Effect_FungusMat_Snare", snareFrames, 0.12f, sortingOrder: 5);

            var medium = new List<MyceliumPatch>();
            var large = new List<MyceliumPatch>();
            foreach (string species in MatSpecies)
            {
                var m = CreateMat(species, "M", 48, snare);
                var l = CreateMat(species, "L", 64, snare);
                if (m != null)
                    medium.Add(m);
                if (l != null)
                    large.Add(l);
            }
            AssetDatabase.SaveAssets();

            var root = PrefabUtility.LoadPrefabContents(PlayerSetup.PlayerPath);
            try
            {
                if (!root.TryGetComponent<PlayerLineage>(out var lineage))
                    return;
                var benefits = lineage.Benefits ?? new LineageBenefitSettings();
                benefits.floor3MatsMedium = medium.ToArray();
                benefits.floor3MatsLarge = large.ToArray();
                lineage.ConfigureBenefits(benefits);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerSetup.PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            if (medium.Count < MatSpecies.Length || large.Count < MatSpecies.Length)
                Debug.LogWarning("Floor3LineagesSetup: faltan hojas de matas de hongo en docs/multimedia-temp/floor3-lineage-art/FungusMat.");
        }

        static MyceliumPatch CreateMat(string species, string size, int cell, GameObject snare)
        {
            Sprite[] Sheet(string anim) => ArtSetup.ImportLooseSheet($"{LineageArtFolder}/FungusMat/Fx_FungusMat_{species}_{size}_{anim}.png",
                $"{EffectsArtFolder}/Fx_FungusMat_{species}_{size}_{anim}.png", cell);

            var appear = Sheet("Appear");
            var idle = Sheet("Idle");
            var pulse = Sheet("Pulse");
            if (appear.Length == 0 || idle.Length == 0 || pulse.Length == 0)
                return null;

            string prefabName = $"Effect_FungusMat_{species}_{size}";
            var obj = new GameObject(prefabName);
            try
            {
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = idle[0];
                // En el suelo, bajo los personajes (como el micelio del piso 1).
                renderer.sortingOrder = 4;
                obj.AddComponent<MyceliumPatch>();
                obj.AddComponent<FungusMat>().Configure(appear, idle, pulse, snare);
                Directory.CreateDirectory("Assets/_Primordia/Prefabs/Effects");
                return PrefabUtility.SaveAsPrefabAsset(obj, $"Assets/_Primordia/Prefabs/Effects/{prefabName}.prefab").GetComponent<MyceliumPatch>();
            }
            finally
            {
                Object.DestroyImmediate(obj);
            }
        }

        // ---------- Escupitajo de la Sanguijuela ----------

        /// <summary>
        /// El arte rojizo del Escupitajo de la Sanguijuela (bola, bolitas, reventón, charco salpicado y banda de baba con coágulos y
        /// gusanillos) en el <see cref="ReactionKit"/> del piso 3, de donde lo toman la bola, la banda y los charcos.
        /// </summary>
        static void SetUpLeechKit()
        {
            var kit = AssetDatabase.LoadAssetAtPath<ReactionKit>(KitPath);
            if (kit == null)
            {
                Debug.LogWarning("Floor3LineagesSetup: no hay ReactionKit del piso 3; ejecuta Primordia > Floor 3 > Crear reacciones.");
                return;
            }

            // El pivote de las bolas es su punto de apoyo en el suelo (README de floor3-spit).
            var ballPivot = new Vector2(0.5f, 0.3125f);
            var miniPivot = new Vector2(0.5f, 0.29f);
            Sprite[] Ball(string name, int cell, Vector2 pivot) => ArtSetup.ImportLooseSheet(
                $"{LineageArtFolder}/Effects/{name}.png", $"{EffectsArtFolder}/{name}.png", cell, pivot);

            kit.ConfigureLeechSpit(LineageSheet("Effects", "Fx_PoisonSpit_Leech_Burst", 64), LineageSheet("Effects", "Fx_PoisonLeechSplat_Appear", 64),
                LineageSheet("Effects", "Fx_PoisonLeechSplat_Idle", 64), LineageSheet("Effects", "Fx_PoisonLeechSplat_Fade", 64));
            kit.ConfigureLeechBall(
                Ball("Proj_PoisonSpit_Leech_Roll_side", 24, ballPivot), Ball("Proj_PoisonSpit_Leech_Roll_down", 24, ballPivot),
                Ball("Proj_PoisonSpit_Leech_Roll_up", 24, ballPivot), Ball("Proj_PoisonSpit_Leech_Swell_side", 24, ballPivot),
                Ball("Proj_PoisonSpit_Leech_Swell_down", 24, ballPivot), Ball("Proj_PoisonSpit_Leech_Swell_up", 24, ballPivot),
                Ball("Proj_PoisonSpitSmall_Leech_Roll_side", 12, miniPivot), Ball("Proj_PoisonSpitSmall_Leech_Roll_down", 12, miniPivot),
                Ball("Proj_PoisonSpitSmall_Leech_Roll_up", 12, miniPivot));
            kit.ConfigureLeechSlime(LeechVariants("Fx_SlimeTrailLeech_Rim", 6, 20), LeechVariants("Fx_SlimeTrailLeech_Body", 6, 20),
                LeechVariants("Fx_SlimeTrailLeechThin_Rim", 5, 14), LeechVariants("Fx_SlimeTrailLeechThin_Body", 5, 14),
                LineageSheet("Effects", "Fx_SlimeTrailLeech_Shine", 8), LeechVariants("Fx_SlimeTrailLeech_Clot", 3, 10),
                LineageSheet("Effects", "Fx_SlimeTrailLeech_Worm", 12), LineageSheet("Effects", "Fx_SlimeTrailLeech_WormSink", 12));
            if (!kit.HasLeechSlime || !kit.HasLeechSplat)
                Debug.LogWarning("Floor3LineagesSetup: faltan hojas del Escupitajo de la Sanguijuela en docs/multimedia-temp/floor3-lineage-art/Effects.");
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
        }

        // ---------- Escupitajo del Yesquero ----------

        /// <summary>
        /// El arte del Escupitajo del Yesquero (bola de brasa 32×32, bolitas, reventón de brasas y marcas chamuscadas) en el
        /// <see cref="ReactionKit"/> del piso 3, de donde lo toman la bola, el reventón y los fuegos del rastro.
        /// </summary>
        static void SetUpTinderKit()
        {
            var kit = AssetDatabase.LoadAssetAtPath<ReactionKit>(KitPath);
            if (kit == null)
                return;

            // El apoyo de la bola en el suelo: (16, 20,5) de 32 px desde arriba; el de las bolitas, como las verdes (README §7).
            var ballPivot = new Vector2(0.5f, 0.359f);
            var miniPivot = new Vector2(0.5f, 0.29f);
            Sprite[] Ball(string name, int cell, Vector2 pivot) => ArtSetup.ImportLooseSheet(
                $"{LineageArtFolder}/Effects/{name}.png", $"{EffectsArtFolder}/{name}.png", cell, pivot);

            kit.ConfigureTinderBall(
                Ball("Proj_PoisonSpit_Tinder_Roll_side", 32, ballPivot), Ball("Proj_PoisonSpit_Tinder_Roll_down", 32, ballPivot),
                Ball("Proj_PoisonSpit_Tinder_Roll_up", 32, ballPivot), Ball("Proj_PoisonSpit_Tinder_Swell_side", 32, ballPivot),
                Ball("Proj_PoisonSpit_Tinder_Swell_down", 32, ballPivot), Ball("Proj_PoisonSpit_Tinder_Swell_up", 32, ballPivot),
                Ball("Proj_PoisonSpitSmall_Tinder_Roll_side", 12, miniPivot), Ball("Proj_PoisonSpitSmall_Tinder_Roll_down", 12, miniPivot),
                Ball("Proj_PoisonSpitSmall_Tinder_Roll_up", 12, miniPivot));
            kit.ConfigureTinderEffects(LineageSheet("Effects", "Fx_PoisonSpit_Tinder_Burst", 64), LeechVariants("Fx_TinderScorch", 4, 20));
            if (!kit.HasTinderScorch || kit.TinderRollSide.Length == 0 || kit.TinderBurst.Length == 0)
                Debug.LogWarning("Floor3LineagesSetup: faltan hojas del Escupitajo del Yesquero en docs/multimedia-temp/floor3-lineage-art/Effects.");
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
        }

        // Las variantes <prefix>_0 ... <prefix>_<count-1>, una detrás de otra.
        static Sprite[] LeechVariants(string prefix, int count, int cell)
        {
            var all = new List<Sprite>();
            for (int i = 0; i < count; i++)
                all.AddRange(LineageSheet("Effects", $"{prefix}_{i}", cell));
            return all.ToArray();
        }

        // ---------- Chorro venenoso (Pulmonoscorpius) ----------

        /// <summary>
        /// El dibujo del Chorro de vinagre venenoso (las mismas 13 hojas que el normal, prefijo <c>Fx_VinegarJet_Poison_</c>) y sus efectos de
        /// salida, impacto y salpicadura. Arte nulo si falta alguna hoja.
        /// </summary>
        static (JetArt art, GameObject hit, GameObject muzzle, GameObject[] splashes) BuildPoisonJet()
        {
            // Los tramos tienen el pivote en el borde izquierdo, a media altura (README de floor3-jet).
            var pivot = new Vector2(0f, 0.5f);
            var start = JetSheet("Start", 16, pivot);
            var streams = new List<Sprite>();
            var tips = new List<Sprite>();
            foreach (string letter in new[] { "A", "B", "C" })
            {
                streams.AddRange(JetSheet($"Stream_{letter}", 16, pivot));
                tips.AddRange(JetSheet($"Tip_{letter}", 16, pivot));
            }
            var muzzleFrames = JetSheet("Muzzle", 16);
            var hitFrames = JetSheet("Hit", 24);
            var splashFrames = new Sprite[3][];
            for (int i = 0; i < 3; i++)
                splashFrames[i] = JetSheet($"Splash_{i}", 80);

            if (start.Length != JetArt.Frames || streams.Count != JetArt.Frames * 3 || tips.Count != JetArt.Frames * 3
                || muzzleFrames.Length == 0 || hitFrames.Length == 0 || splashFrames.Any(frames => frames.Length == 0))
            {
                Debug.LogWarning("Floor3LineagesSetup: faltan las hojas del Chorro venenoso en docs/multimedia-temp/floor3-lineage-art/Effects.");
                return (null, null, null, null);
            }

            const string path = "Assets/_Primordia/Data/Attacks/JetArt_VinegarJet_Poison.asset";
            Directory.CreateDirectory("Assets/_Primordia/Data/Attacks");
            var art = AssetDatabase.LoadAssetAtPath<JetArt>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<JetArt>();
                AssetDatabase.CreateAsset(art, path);
            }
            art.Configure(start, streams.ToArray(), tips.ToArray());
            EditorUtility.SetDirty(art);

            // Salida y salpicaduras a 50 y 65 ms por cuadro, impacto a 60 ms (como el chorro normal).
            var muzzle = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Poison_Muzzle", muzzleFrames, 0.05f, 21);
            var hit = Floor2MouthsSetup.CreateLoopEffect("Effect_VinegarJet_Poison_Hit", hitFrames, 0.06f, 21);
            var splashes = new GameObject[3];
            for (int i = 0; i < 3; i++)
                splashes[i] = Floor2MouthsSetup.CreateFrameEffect($"Effect_VinegarJet_Poison_Splash_{i}", splashFrames[i], 0.065f);
            return (art, hit, muzzle, splashes);
        }

        static Sprite[] JetSheet(string name, int cell, Vector2? pivot = null)
        {
            string file = $"Fx_VinegarJet_Poison_{name}";
            string source = $"{LineageArtFolder}/Effects/{file}.png";
            string target = $"{EffectsArtFolder}/{file}.png";
            return pivot.HasValue ? ArtSetup.ImportLooseSheet(source, target, cell, pivot.Value) : ArtSetup.ImportLooseSheet(source, target, cell);
        }

        /// <summary>Al prefab del efecto le pone el vuelo en arco (el código lo lanza al crearlo).</summary>
        static void AddArcFlight(GameObject prefab)
        {
            if (prefab == null)
                return;
            string path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!contents.TryGetComponent<ArcFlight>(out _))
                    contents.AddComponent<ArcFlight>();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Sprite[] LineageSheet(string folder, string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{LineageArtFolder}/{folder}/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);

        static Sprite[] Sheet(string folder, string name, int cell) =>
            ArtSetup.ImportLooseSheet($"{SourceFolder}/{folder}/{name}.png", $"{EffectsArtFolder}/{name}.png", cell);
    }
}
