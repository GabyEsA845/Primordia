using System.Collections.Generic;
using System.Linq;
using Primordia.Mutations;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Metamorfosis del piso 2 al 3 (docs/multimedia-temp/floor3-metamorphosis): una silueta por cada pareja de cuerpos, la revelación
    /// de los 3 cuerpos del piso 3 y el fondo de la subida del abismo a la superficie del pantano.
    /// </summary>
    public static partial class ArtSetup
    {
        const string Floor3MetamorphosisSourceFolder = "docs/multimedia-temp/floor3-metamorphosis";
        const string Floor3MetamorphosisArtFolder = "Assets/_Primordia/Art/Metamorphosis/Floor3";
        const int MetamorphosisCell = 32;

        // Cuerpo de cada Boca (el nombre de las hojas): piso 2 y piso 3.
        internal static readonly (string asset, string body)[] Floor2MouthBodies =
        {
            ("Floor2/Mutation_Cnidocytes", "Jellyfish"), ("Floor2/Mutation_JawPlates", "Placoderm"), ("Floor2/Mutation_ArmCrown", "Crinoid"),
        };

        internal static readonly (string asset, string body)[] Floor3MouthBodies =
        {
            ("Floor3/Mutation_PoisonSpit", "Frog"), ("Floor3/Mutation_SpinningTop", "Diplocaulus"), ("Floor3/Mutation_VinegarJet", "Whipscorpion"),
        };

        /// <summary>Fondo, rayos y motas de la subida (más el aura y el estallido de siempre). Null si falta el arte.</summary>
        internal static MetamorphosisArt ImportRisingMetamorphosisArt()
        {
            var common = ImportMetamorphosisArt();
            Sprite Single(string file) => ImportSprite($"{Floor3MetamorphosisSourceFolder}/{file}.png",
                $"{Floor3MetamorphosisArtFolder}/{file}.png", Vector4.zero);
            Sprite[] Sheet(string file) => ImportLooseSheet($"{Floor3MetamorphosisSourceFolder}/{file}.png",
                $"{Floor3MetamorphosisArtFolder}/{file}.png", 4);

            var background = Single("Surface_Background");
            var rays = Single("Surface_Rays");
            // Burbujas del abismo abajo y hojitas del pantano arriba, mezcladas.
            var motes = common.Motes.Concat(Sheet("Surface_Mote")).ToArray();
            if (background == null || rays == null)
            {
                Debug.LogWarning($"ArtSetup: falta el fondo de la subida en {Floor3MetamorphosisSourceFolder}.");
                return null;
            }
            return new MetamorphosisArt { Background = background, Rays = rays, Motes = motes, Aura = common.Aura, Burst = common.Burst };
        }

        /// <summary>
        /// Pone en las Bocas del piso 3 la revelación de su cuerpo y una silueta desde cada Boca del piso 2 y de las otras del piso 3
        /// (9 + 6 hojas). Lo que no esté importado se salta con un aviso.
        /// </summary>
        internal static void ApplyFloor3Metamorphosis()
        {
            MutationData Load(string asset) => UnityEditor.AssetDatabase.LoadAssetAtPath<MutationData>($"Assets/_Primordia/Data/Mutations/{asset}.asset");
            Sprite[] Sheet(string file, int cell) => ImportLooseSheet($"{Floor3MetamorphosisSourceFolder}/{file}.png",
                $"{Floor3MetamorphosisArtFolder}/{file}.png", cell);

            foreach (var (asset, body) in Floor3MouthBodies)
            {
                var mouth = Load(asset);
                if (mouth == null)
                {
                    Debug.LogWarning($"ArtSetup: falta la Boca {asset}; crea antes las Bocas del piso 3.");
                    continue;
                }

                var paths = new List<MutationData.MorphPath>();
                foreach (var (fromAsset, fromBody) in Floor2MouthBodies.Concat(Floor3MouthBodies))
                {
                    var from = Load(fromAsset);
                    if (from == null || fromBody == body)
                        continue;
                    var frames = Sheet($"Morph_{fromBody}_{body}", MetamorphosisCell);
                    if (frames.Length > 0)
                        paths.Add(new MutationData.MorphPath { from = from, frames = frames });
                }

                var reveal = Sheet($"Reveal_{body}", MetamorphosisCell);
                mouth.SetMorphPaths(paths.ToArray());
                mouth.SetCreature(mouth.CreatureBody, mouth.MorphFrames, reveal);
                UnityEditor.EditorUtility.SetDirty(mouth);
            }
            UnityEditor.AssetDatabase.SaveAssets();
        }
    }
}
