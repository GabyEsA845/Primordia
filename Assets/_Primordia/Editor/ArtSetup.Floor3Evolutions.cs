using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        /// <summary>
        /// Evoluciones de los linajes del piso 3 (docs/multimedia-temp/floor3-lineages/Evolution, 80×80, mismas duraciones que
        /// los pisos 1 y 2): un prefab <c>Effect_Evolution_&lt;Linaje&gt;</c> por hoja. Devuelve los prefabs por nombre del linaje.
        /// </summary>
        internal static Dictionary<string, GameObject> ImportFloor3Evolutions(IEnumerable<string> lineageNames)
        {
            var names = lineageNames.ToArray();
            var art = Import(new Character
            {
                Name = "",
                SourceFolder = "docs/multimedia-temp/floor3-lineages/Evolution",
                ArtFolder = "Assets/_Primordia/Art/Effects/Evolution/Floor3",
                ControllerPerClip = true,
                Timings = new[] { ("Evolution", 10f, false) },
                FrameDurations = names.ToDictionary(n => $"Evolution_{n}", _ => EvolutionDurations),
            });
            var prefabs = new Dictionary<string, GameObject>();
            if (art == null)
                return prefabs;
            foreach (var name in names)
                prefabs[name] = CreateEffectPrefab($"Effect_Evolution_{name}", art, $"Evolution_{name}", sortingOrder: 15);
            return prefabs;
        }
    }
}
