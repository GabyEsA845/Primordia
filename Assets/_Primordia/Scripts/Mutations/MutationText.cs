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
            _ => lineageClass.ToString()
        };

        /// <summary>Beneficio común de cada clase de linaje: nombre y lo que hace.</summary>
        public static (string name, string description) ClassBenefit(LineageClass lineageClass) => lineageClass switch
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
            string classBenefit = ClassBenefit(lineage.Class).name;
            return string.IsNullOrEmpty(lineage.TouchName) ? classBenefit : $"{classBenefit} · {lineage.TouchName}";
        }

        /// <summary>
        /// "Linajes: Tiburón, Calamar". Con jugador, cuántas de las 4 lleva: "Linajes: Tiburón 3/4, Calamar
        /// (completo)". Vacío si la mutación no pertenece a ningún linaje.
        /// </summary>
        public static string LineageList(MutationData mutation, PlayerMutations player = null)
        {
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

        /// <summary>Texto con una línea más al final (si no está vacía), separada por una línea en blanco.</summary>
        public static string WithLine(string text, string line) =>
            string.IsNullOrEmpty(line) ? text : string.IsNullOrEmpty(text) ? line : $"{text}\n\n{line}";
    }
}
