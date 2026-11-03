using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Nombres en español de ranuras, niveles y linajes, para la interfaz.</summary>
    public static class MutationText
    {
        public static string SlotName(MutationSlot slot) => slot switch
        {
            MutationSlot.Mouth => "Boca",
            MutationSlot.Limbs => "Extremidades",
            MutationSlot.Skin => "Piel",
            MutationSlot.Organ => "Órgano especial",
            _ => slot.ToString()
        };

        public static string FamilyName(WeaponFamily family) => family switch
        {
            WeaponFamily.Melee => "Cuerpo a cuerpo",
            WeaponFamily.Projectile => "Proyectil",
            WeaponFamily.Area => "Área",
            _ => family.ToString()
        };

        /// <summary>
        /// Aviso de la ventana de elección de Boca: qué pasa con los niveles de la Boca actual. Vacío si no hay Boca.
        /// </summary>
        public static string MouthChangeNote(PlayerMutations player)
        {
            var old = player != null ? player.Get(MutationSlot.Mouth) : null;
            if (old == null)
                return "";

            int extra = MouthChange.ExtraLevels(player);
            string current = $"Tu {old.DisplayName} {LevelName(player.LevelOf(MutationSlot.Mouth))}";
            if (extra <= 0)
                return $"{current} se queda atrás.";

            int room = 0;
            foreach (var slot in new[] { MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ })
            {
                if (player.Get(slot) != null)
                    room += MutationData.MaxLevel - player.LevelOf(slot);
            }

            string levels = extra == 1 ? "1 nivel" : $"{extra} niveles";
            if (room == 0)
                return $"{current}: {levels} se perderá{(extra == 1 ? "" : "n")}, tus otras mutaciones ya están al máximo.";
            string spread = $"{current}: {levels} se repartirá{(extra == 1 ? "" : "n")} entre tus otras mutaciones";
            return room < extra ? $"{spread}; los que no quepan se perderán." : $"{spread}.";
        }

        /// <summary>Nivel en números romanos: I, II, III.</summary>
        public static string LevelName(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => level.ToString()
        };

        /// <summary>"Nivel I > II": la fuente pixel no tiene flecha.</summary>
        public static string LevelChange(int from, int to) => $"Nivel {LevelName(from)} > {LevelName(to)}";

        /// <summary>Descripción con lo que añade cada nivel alcanzado: "…\nNivel II: …\nNivel III: …".</summary>
        public static string FullDescription(MutationData mutation, int level)
        {
            var text = mutation.Description;
            for (int l = 2; l <= level && l <= MutationData.MaxLevel; l++)
            {
                var extra = mutation.LevelDescription(l);
                if (!string.IsNullOrEmpty(extra))
                    text += $"\nNivel {LevelName(l)}: {extra}";
            }
            return text;
        }

        /// <summary>
        /// Lo que hace ahora: la descripción y, desde el nivel II, solo la mejora de su nivel ("Nivel III: Desgarro…").
        /// </summary>
        public static string CurrentDescription(MutationData mutation, int level)
        {
            var text = mutation.Description;
            var extra = level >= 2 ? mutation.LevelDescription(System.Math.Min(level, MutationData.MaxLevel)) : "";
            return string.IsNullOrEmpty(extra) ? text : $"{text}\nNivel {LevelName(level)}: {extra}";
        }

        /// <summary>"Tiburón 4/4 · Calamar 3/4": sus linajes y cuántas de sus 4 mutaciones lleva el jugador.</summary>
        public static string LineageProgress(MutationData mutation, PlayerMutations player)
        {
            if (mutation != null && mutation.IsSpecial)
                return SpecialLine;
            if (mutation == null || mutation.Lineages.Count == 0)
                return "";

            var parts = new System.Collections.Generic.List<string>();
            foreach (var lineage in mutation.Lineages)
            {
                if (lineage != null)
                    parts.Add(player != null ? $"{lineage.DisplayName} {lineage.Progress(player)}/{LineageData.SlotCount}" : lineage.DisplayName);
            }
            return string.Join(" · ", parts);
        }

        public static string ClassName(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => "Carnívoro",
            LineageClass.Herbivore => "Herbívoro",
            LineageClass.Venomous => "Venenoso",
            LineageClass.Fungal => "Hongo",
            LineageClass.Scavenger => "Carroñero",
            LineageClass.Parasite => "Parásito",
            _ => lineageClass.ToString()
        };

        /// <summary>
        /// Color de cada clase (rótulo del HUD, cartel, pausa, evolución). Lo usan las interfaces cuando su lista de
        /// colores no llega a esa clase (las hechas antes del Carroñero).
        /// </summary>
        public static Color ClassColor(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => new Color(0.95f, 0.45f, 0.4f),
            LineageClass.Herbivore => new Color(0.55f, 0.9f, 0.45f),
            LineageClass.Venomous => new Color(0.8f, 0.6f, 0.95f),
            LineageClass.Fungal => new Color(0.95f, 0.75f, 0.4f),
            LineageClass.Scavenger => new Color(0.78f, 0.64f, 0.42f),
            LineageClass.Parasite => new Color(0.82f, 0.29f, 0.38f), // granate #d04a62
            _ => Color.white
        };

        /// <summary>Color de la clase en esa lista (la de cada interfaz) o, si no llega, el de <see cref="ClassColor"/>.</summary>
        public static Color ClassColor(LineageClass lineageClass, Color[] colors)
        {
            int index = (int)lineageClass;
            return colors != null && index >= 0 && index < colors.Length ? colors[index] : ClassColor(lineageClass);
        }

        /// <summary>Beneficio común de cada clase de linaje en ese piso: nombre y lo que hace.</summary>
        public static (string name, string description) ClassBenefit(LineageClass lineageClass, int floor = 1) =>
            floor >= 3 ? ClassBenefitFloor3(lineageClass) :
            floor >= 2 ? ClassBenefitFloor2(lineageClass) : ClassBenefitFloor1(lineageClass);

        static (string name, string description) ClassBenefitFloor3(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => ("Rematar", "+100 % de daño a los heridos (+50 % al jefe). Los aturdidos, los quebrados y los que arden cuentan como heridos."),
            LineageClass.Venomous => ("Toxina", "Los enemigos envenenados reciben +50 % de daño y van un 20 % más lentos. Si uno muere envenenado, deja un charco de veneno."),
            LineageClass.Fungal => ("Colonizar", "En cada combate brotan matas de hongo que frenan y dañan. Un enemigo que muere sobre una mata hace brotar otra (hasta 6)."),
            LineageClass.Parasite => ("Chupar", "Cada 15 de daño a un enemigo le pega un parásito que le quita 1 por segundo. Cada 15 de vida chupada, medio corazón (30 con un jefe). Salta a otro al morir; 3 como máximo."),
            _ => ClassBenefitFloor2(lineageClass)
        };

        static (string name, string description) ClassBenefitFloor2(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => ("Rematar", "+100 % de daño a los heridos (+50 % al jefe). Los aturdidos y los quebrados cuentan como heridos."),
            LineageClass.Herbivore => ("Pastar biomasa", "Cada 12 de biomasa, medio corazón. Con la vida llena, hasta 2 corazones verdes que paran un golpe."),
            LineageClass.Venomous => ("Toxina débil", "Los enemigos envenenados reciben +50 % de daño y van un 20 % más lentos."),
            LineageClass.Scavenger => ("Carroña", "Los enemigos dejan restos 4 s; al comerlos, +20 % de daño durante 5 s (hasta 3 veces)."),
            _ => ClassBenefitFloor1(lineageClass)
        };

        static (string name, string description) ClassBenefitFloor1(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => ("Rematar", "+100 % de daño a los enemigos con la mitad de vida o menos (+50 % al jefe)."),
            LineageClass.Herbivore => ("Pastar biomasa", "Cada 10 de biomasa, medio corazón. Con la vida llena, un corazón verde que para un golpe."),
            LineageClass.Venomous => ("Toxina débil", "Los enemigos envenenados reciben +50 % de daño."),
            LineageClass.Fungal => ("Colonizar la sala", "En cada combate brotan matas de hongo que frenan y dañan."),
            _ => ("", "")
        };

        /// <summary>"Rematar · Sangre en el agua": el nombre del beneficio de la clase y el del toque propio.</summary>
        public static string BenefitTitle(LineageData lineage)
        {
            if (lineage == null)
                return "";
            string classBenefit = ClassBenefit(lineage.Class, lineage.Floor).name;
            return string.IsNullOrEmpty(lineage.TouchName) ? classBenefit : $"{classBenefit} · {lineage.TouchName}";
        }

        /// <summary>
        /// "Linajes: Tiburón, Calamar". Con jugador, cuántas de las 4 lleva: "Linajes: Tiburón 3/4, Calamar
        /// (completo)". Vacío si la mutación no pertenece a ningún linaje.
        /// </summary>
        public static string LineageList(MutationData mutation, PlayerMutations player = null)
        {
            if (mutation != null && mutation.IsSpecial)
                return SpecialLine;
            if (mutation == null || mutation.Lineages.Count == 0)
                return "";

            var names = new System.Collections.Generic.List<string>();
            foreach (var lineage in mutation.Lineages)
            {
                if (lineage == null)
                    continue;

                if (player == null)
                {
                    names.Add(lineage.DisplayName);
                    continue;
                }

                int progress = lineage.Progress(player);
                names.Add(progress == LineageData.SlotCount
                    ? $"{lineage.DisplayName} (completo)"
                    : $"{lineage.DisplayName} {progress}/{LineageData.SlotCount}");
            }
            return names.Count > 0 ? "Linajes: " + string.Join(", ", names) : "";
        }

        /// <summary>Lo que tienen en común las mutaciones especiales de las almejas, en lugar de su lista de linajes.</summary>
        public const string SpecialLine =
            "Especial: completa cualquier linaje del que lleves las otras 3. Inestable: cada golpe te quita medio corazón más.";

        /// <summary>"+½", "+1", "+1½": medios corazones de más por golpe (precio Inestable).</summary>
        public static string HalfHearts(int halves)
        {
            int whole = halves / 2;
            bool half = halves % 2 == 1;
            return whole == 0 ? (half ? "+½" : "0") : half ? $"+{whole}½" : $"+{whole}";
        }

        /// <summary>Texto con una línea más al final (si no está vacía), separada por una línea en blanco.</summary>
        public static string WithLine(string text, string line) =>
            string.IsNullOrEmpty(line) ? text : string.IsNullOrEmpty(text) ? line : $"{text}\n\n{line}";
    }
}
