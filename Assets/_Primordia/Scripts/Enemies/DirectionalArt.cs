using System;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Hojas de un jefe dibujado en picada con una fila por dirección (la Meganeura): 9 filas dibujadas, de arriba (90°) a abajo
    /// (-90°) pasando por el lado derecho (0°); las 7 direcciones de la izquierda son las mismas filas con Flip X. Cada hoja guarda
    /// sus cuadros fila a fila. Lo rellena el importador (ArtSetup.ImportMeganeuraSheets).
    /// </summary>
    public class DirectionalArt : ScriptableObject
    {
        /// <summary>Filas dibujadas en una hoja con direcciones.</summary>
        public const int DirectionRows = 9;
        const float RowStep = 22.5f;

        [Serializable]
        public class Sheet
        {
            public string name;
            [Tooltip("Cuadros por fila.")] public int frames = 1;
            [Tooltip("Filas: 9 con direcciones, 1 si es una sola fila.")] public int rows = DirectionRows;
            [Tooltip("Cuadros fila a fila (fila 0 primero).")] public Sprite[] sprites = Array.Empty<Sprite>();

            public Sprite Get(int row, int frame)
            {
                if (sprites.Length == 0)
                    return null;
                row = Mathf.Clamp(row, 0, rows - 1);
                frame = Mathf.Clamp(frame, 0, frames - 1);
                return sprites[row * frames + frame];
            }
        }

        [SerializeField] Sheet[] sheets = Array.Empty<Sheet>();
        Dictionary<string, Sheet> byName;

        public IReadOnlyList<Sheet> Sheets => sheets;

        /// <summary>La hoja con ese nombre (por ejemplo «Meganeura_Fly_Torn1»), o null.</summary>
        public Sheet Find(string sheetName)
        {
            if (byName == null || byName.Count != sheets.Length)
            {
                byName = new Dictionary<string, Sheet>(sheets.Length);
                foreach (var sheet in sheets)
                    byName[sheet.name] = sheet;
            }
            return byName.TryGetValue(sheetName, out var found) ? found : null;
        }

        internal void SetSheets(Sheet[] newSheets)
        {
            sheets = newSheets ?? Array.Empty<Sheet>();
            byName = null;
        }

        /// <summary>
        /// Fila de la hoja y si va con Flip X para mirar hacia <paramref name="angle"/> (grados, 0° = derecha, 90° = arriba),
        /// ajustada a la dirección dibujada más cercana (cada 22,5°).
        /// </summary>
        public static int RowFor(float angle, out bool flipX)
        {
            angle = Mathf.Repeat(angle + 180f, 360f) - 180f;
            flipX = angle > 90f || angle < -90f;
            // Mirando a la izquierda se usa la fila del ángulo reflejado.
            float mirrored = flipX ? (angle > 0f ? 180f - angle : -180f - angle) : angle;
            return Mathf.Clamp(Mathf.RoundToInt((90f - mirrored) / RowStep), 0, DirectionRows - 1);
        }
    }
}
