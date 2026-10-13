namespace Primordia.Mutations
{
    /// <summary>Nombres en español de ranuras y linajes, para la interfaz.</summary>
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

        public static string LineageName(Lineage lineage) => lineage switch
        {
            Lineage.Predator => "Depredador",
            Lineage.Armored => "Acorazado",
            Lineage.Toxic => "Tóxico",
            _ => lineage.ToString()
        };
    }
}
