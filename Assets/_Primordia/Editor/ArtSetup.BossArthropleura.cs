using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Arthropleura (jefe principal del piso 3, arte en docs/multimedia-temp/floor3-bosses/arthropleura): reúne en un
    /// <see cref="DirectionalArt"/> las hojas de Sheets/ (9 filas de dirección: cabeza 160 px, piezas 112 px, rueda 144 px;
    /// incluye las capas Burning/Charred/Cracks). Efectos, barra y flechas se cortan al usarlos.
    /// </summary>
    public static partial class ArtSetup
    {
        internal const string ArthropleuraArtPath = "Assets/_Primordia/Data/Bosses/ArthropleuraArt.asset";
        const string ArthropleuraArtFolder = Floor3ArtRoot + "/floor3-bosses/arthropleura";

        [MenuItem("Primordia/Boss/Importar hojas de la Arthropleura")]
        internal static DirectionalArt ImportArthropleuraSheets()
        {
            ImportFloor3Art();
            string path = ArthropleuraArtFolder + "/Sheets";
            if (!Directory.Exists(path))
                throw new FileNotFoundException($"Falta el arte aprobado: {path}");
            Directory.CreateDirectory(Path.GetDirectoryName(ArthropleuraArtPath));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(ArthropleuraArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<DirectionalArt>();
                AssetDatabase.CreateAsset(art, ArthropleuraArtPath);
            }
            var sheets = new List<DirectionalArt.Sheet>();
            foreach (string file in Directory.GetFiles(path, "*.png").OrderBy(f => f))
                sheets.Add(SliceDirectional(file.Replace('\\', '/')));
            foreach (string required in ArthropleuraBoss.RequiredSheets)
                if (!sheets.Any(s => s.name == required && s.sprites.Length > 0))
                    throw new FileNotFoundException($"Falta la hoja aprobada {required}.");
            art.SetSheets(sheets.ToArray());
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: Arthropleura importada, {sheets.Count} hojas en {ArthropleuraArtPath}.");
            return art;
        }
    }
}
