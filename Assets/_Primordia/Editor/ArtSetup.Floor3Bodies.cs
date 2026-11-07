using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Cuerpos de criatura e iconos de las Bocas del piso 3 (docs/multimedia-temp/floor3-bodies: generar_cuerpos_piso3.py y
    /// generar_efectos_bocas_piso3.py): rana, Diplocaulus y vinagrillo. Lo usa <see cref="Floor3MouthsSetup"/>.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor3BodiesSourceFolder = "docs/multimedia-temp/floor3-bodies";
        const string Floor3BodiesArtFolder = CharactersFolder + "/Player/Floor3";

        // Ritmo de cada hoja (ms por cuadro del README). El primero que encaja por prefijo gana: los nombres largos, antes.
        static Character Floor3Body(string body, params (string prefix, float fps, bool loop)[] specific)
        {
            var common = new[]
            {
                ("Idle", body == "Frog" ? 6.25f : 5.9f, true), ("Move", body == "Diplocaulus" ? 10f : 11.1f, true),
                ("Dodge", body == "Diplocaulus" ? 18.2f : 16.7f, false), ("Hurt", 12.5f, false), ("Death", 8.3f, false),
                // Salto de las Patas de salto: el cuadro lo elige el código por el avance del salto; la carga va en bucle.
                ("JumpCharge", 9f, true), ("Jump", 16.7f, false),
            };
            var timings = new System.Collections.Generic.List<(string, float, bool)>(specific);
            timings.AddRange(common);
            return new Character
            {
                Name = $"Player_{body}",
                SourceFolder = $"{Floor3BodiesSourceFolder}/{body}",
                ArtFolder = $"{Floor3BodiesArtFolder}/{body}",
                DefaultState = "Idle_down",
                Timings = timings.ToArray(),
            };
        }

        static readonly Character FrogBody = Floor3Body("Frog", ("Attack", 12.5f, false));

        static readonly Character DiplocaulusBody = Floor3Body("Diplocaulus",
            ("AttackStart", 14.3f, false), ("SpinEnd", 12.5f, false), ("Spin", 25f, true), ("Dizzy", 8f, true));

        static readonly Character WhipscorpionBody = Floor3Body("Whipscorpion",
            ("AttackStart", 14.3f, false), ("AttackLoop", 16.7f, true), ("AttackEnd", 12.5f, false));

        /// <summary>
        /// La capa de una Piel del piso 3 sobre un cuerpo (docs/multimedia-temp/floor3-mutations/Skin, hojas <c>{prefix}_{cuerpo}_*</c>), con
        /// los mismos estados y ritmo que el cuerpo. Null si no están las hojas. <paramref name="root"/> es la carpeta temporal que las trae.
        /// </summary>
        internal static RuntimeAnimatorController ImportFloor3SkinLayer(string prefix, string body, string folder = "Skin", string root = "docs/multimedia-temp/floor3-mutations")
        {
            var template = body == "Frog" ? FrogBody : body == "Diplocaulus" ? DiplocaulusBody : body == "Whipscorpion" ? WhipscorpionBody : null;
            string source = $"{root}/{folder}";
            string name = $"{prefix}_{body}";
            if (template == null || !System.IO.Directory.Exists(source) || System.IO.Directory.GetFiles(source, name + "_*.png").Length == 0)
                return null;
            var art = Import(new Character
            {
                Name = name,
                SourceFolder = source,
                ArtFolder = $"{CharactersFolder}/Player/Floor3Skins/{prefix}/{body}",
                DefaultState = "Idle_down",
                Timings = template.Timings,
            });
            return art != null ? art.Controller : null;
        }

        /// <summary>Corta en cuadros una hoja de efectos de las Bocas del piso 3 ya importada (floor3-bodies/Effects). Vacío si no está.</summary>
        internal static Sprite[] Floor3BodyEffect(string name, int cell)
        {
            string path = $"{Floor3ArtRoot}/floor3-bodies/Effects/{name}.png";
            return System.IO.File.Exists(path) ? SliceSheet(path, cell) : System.Array.Empty<Sprite>();
        }

        /// <summary>Corta en cuadros una hoja de un cuerpo del piso 3 ya importada (p. ej. Diplocaulus, Spin). Vacío si no está.</summary>
        internal static Sprite[] Floor3BodySheet(string body, string animation)
        {
            string path = $"{Floor3BodiesArtFolder}/{body}/Player_{body}_{animation}.png";
            return System.IO.File.Exists(path) ? SliceSheet(path, 0) : System.Array.Empty<Sprite>();
        }

        /// <summary>
        /// Corta en cuadros una hoja del Escupitajo venenoso ya importada (floor3-spit/Effects) con ese pivote (el centro si no se dice).
        /// Vacío si no está.
        /// </summary>
        internal static Sprite[] Floor3SpitSheet(string name, int cell, Vector2? pivot = null)
        {
            string path = $"{Floor3ArtRoot}/floor3-spit/Effects/{name}.png";
            return System.IO.File.Exists(path) ? SliceSheet(path, cell, pivot ?? new Vector2(0.5f, 0.5f)) : System.Array.Empty<Sprite>();
        }

        /// <summary>
        /// Corta en cuadros una hoja del Chorro de vinagre ya importada (floor3-jet/Effects) con ese pivote (el centro si no se dice).
        /// Vacío si no está.
        /// </summary>
        internal static Sprite[] Floor3JetSheet(string name, int cell, Vector2? pivot = null)
        {
            string path = $"{Floor3ArtRoot}/floor3-jet/Effects/{name}.png";
            return System.IO.File.Exists(path) ? SliceSheet(path, cell, pivot ?? new Vector2(0.5f, 0.5f)) : System.Array.Empty<Sprite>();
        }

        /// <summary>Corta en cuadros una hoja del clon de la Peonza ya importada (floor3-top-clone/Effects). Vacío si no está.</summary>
        internal static Sprite[] Floor3CloneSheet(string name, int cell)
        {
            string path = $"{Floor3ArtRoot}/floor3-top-clone/Effects/{name}.png";
            return System.IO.File.Exists(path) ? SliceSheet(path, cell) : System.Array.Empty<Sprite>();
        }

        // Los cuadros de las variantes <prefix>_0 ... <prefix>_<count-1>, una variante detrás de otra. Vacío si falta alguna.
        static Sprite[] Floor3SpitVariants(string prefix, int count, int cell)
        {
            var all = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                var frames = Floor3SpitSheet($"{prefix}_{i}", cell);
                if (frames.Length != ReactionKit.SlimeFrames)
                    return System.Array.Empty<Sprite>();
                all.AddRange(frames);
            }
            return all.ToArray();
        }

        /// <summary>
        /// Pone en el kit de reacciones el arte del Escupitajo venenoso (floor3-spit): el reventón, el charco salpicado y los sellos de
        /// la banda de baba (normal y fina) con su brillo.
        /// </summary>
        internal static void ConfigureSpitKit(ReactionKit kit)
        {
            if (kit == null)
                return;
            kit.ConfigureSpit(Floor3SpitSheet("Fx_PoisonSpit_Burst", 64), Floor3SpitSheet("Fx_PoisonSplat_Appear", 64),
                Floor3SpitSheet("Fx_PoisonSplat_Idle", 64), Floor3SpitSheet("Fx_PoisonSplat_Fade", 64));
            kit.ConfigureSlime(Floor3SpitVariants("Fx_SlimeTrail_Rim", 6, 20), Floor3SpitVariants("Fx_SlimeTrail_Body", 6, 20),
                Floor3SpitVariants("Fx_SlimeTrailThin_Rim", 5, 14), Floor3SpitVariants("Fx_SlimeTrailThin_Body", 5, 14),
                Floor3SpitSheet("Fx_SlimeTrail_Shine", 8));
            if (!kit.HasSlime)
                Debug.LogWarning("ArtSetup: faltan las hojas de la banda de baba (floor3-spit); ejecuta Primordia > Floor 3 > Importar todo el arte del piso 3.");
            EditorUtility.SetDirty(kit);
        }

        internal sealed class Floor3MouthArt
        {
            public MouthArt PoisonSpit;
            public MouthArt SpinningTop;
            public MouthArt VinegarJet;
        }

        internal static Floor3MouthArt ImportFloor3MouthArt() => new()
        {
            PoisonSpit = ImportFloor3Mouth("PoisonSpit", "Frog", FrogBody),
            SpinningTop = ImportFloor3Mouth("SpinningTop", "Diplocaulus", DiplocaulusBody),
            VinegarJet = ImportFloor3Mouth("VinegarJet", "Whipscorpion", WhipscorpionBody),
        };

        // Las siluetas de la metamorfosis (Morph y Reveal) de estos cuerpos aún no están dibujadas: van vacías.
        static MouthArt ImportFloor3Mouth(string icon, string body, Character character)
        {
            Sprite Icon(int size) => ImportSprite($"{Floor3BodiesSourceFolder}/Icons/Icon_{icon}_{size}.png",
                $"{UIArtFolder}/Icon_{icon}_{size}.png", Vector4.zero);

            Sprite[] Sheet(string animation)
            {
                string file = $"Player_{body}_{animation}.png";
                string target = $"{Floor3BodiesArtFolder}/{body}/{file}";
                return CopyArt($"{Floor3BodiesSourceFolder}/{body}/{file}", target)
                    ? SliceSheet(target, 0)
                    : System.Array.Empty<Sprite>();
            }

            var animations = Import(character);
            return new MouthArt
            {
                Icon = Icon(16), LargeIcon = Icon(32), Idle = Sheet("Idle_side"),
                Attack = Sheet(body == "Diplocaulus" ? "Spin" : body == "Whipscorpion" ? "AttackLoop_side" : "Attack_side"),
                Body = animations != null ? animations.Controller : null,
                Morph = System.Array.Empty<Sprite>(), Reveal = System.Array.Empty<Sprite>(),
            };
        }
    }
}
