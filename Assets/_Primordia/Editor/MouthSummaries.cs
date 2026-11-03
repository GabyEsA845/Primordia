using UnityEditor;
using UnityEngine;
using Primordia.Mutations;

namespace Primordia.Editor
{
    /// <summary>
    /// Resúmenes de una frase de las Bocas que se ofrecen en la ventana de la sala de paso (la descripción entera se sale de la carta).
    /// Solo cambia el campo de resumen: no toca ataques, descripciones ni niveles. Se puede repetir sin miedo.
    /// </summary>
    public static class MouthSummaries
    {
        static readonly (string path, string summary)[] Summaries =
        {
            ("Floor2/Mutation_Cnidocytes", "Arpón urticante que atraviesa y envenena."),
            ("Floor2/Mutation_JawPlates", "Mordisco de guillotina en arco ancho; el mordido recibe más daño."),
            ("Floor2/Mutation_ArmCrown", "Remolino que atrae a los enemigos y les hace daño."),
            ("Floor3/Mutation_PoisonSpit", "Bola de veneno que rueda dejando baba y estalla."),
            ("Floor3/Mutation_SpinningTop", "Gira como una peonza golpeando y empujando a los cercanos."),
            ("Floor3/Mutation_VinegarJet", "Chorro continuo desde la cola que sigue al cursor."),
        };

        [MenuItem("Primordia/Mutations/Resumir las descripciones de las Bocas")]
        internal static void Apply()
        {
            int done = 0;
            foreach (var (path, summary) in Summaries)
            {
                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"Assets/_Primordia/Data/Mutations/{path}.asset");
                if (mutation == null)
                    continue;
                mutation.SetSummary(summary);
                EditorUtility.SetDirty(mutation);
                done++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"MouthSummaries: {done} de {Summaries.Length} Bocas con resumen para las cartas.");
        }
    }
}
