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

        public static string LineageName(Lineage lineage) => lineage switch
        {
            Lineage.Predator => "Depredador",
            Lineage.Armored => "Acorazado",
            Lineage.Toxic => "Tóxico",
            _ => lineage.ToString()
        };
    }
}
