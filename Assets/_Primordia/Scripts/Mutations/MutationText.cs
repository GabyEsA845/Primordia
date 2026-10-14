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

        public static string ClassName(LineageClass lineageClass) => lineageClass switch
        {
            LineageClass.Carnivore => "Carnívoro",
            LineageClass.Herbivore => "Herbívoro",
            LineageClass.Venomous => "Venenoso",
            LineageClass.Fungal => "Hongo",
            _ => lineageClass.ToString()
        };

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
