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
