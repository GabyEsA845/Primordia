using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Reproduce una hoja con direcciones (<see cref="DirectionalArt"/>) en un SpriteRenderer: elige la fila según el ángulo (con
    /// Flip X a la izquierda) y el cuadro según el tiempo. Una hoja se reproduce en bucle o una vez (se queda en el último cuadro).
    /// </summary>
    public class DirectionalSprite : MonoBehaviour
    {
        [SerializeField] SpriteRenderer target;
        [SerializeField] DirectionalArt art;

        DirectionalArt.Sheet sheet;
        float frameSeconds = 0.1f;
        bool loop = true;
        float startedAt;
        float angle = -90f;

        /// <summary>Nombre de la hoja en curso.</summary>
        public string Current { get; private set; } = "";

        /// <summary>True cuando una hoja sin bucle ha enseñado su último cuadro.</summary>
        public bool Finished => sheet != null && !loop && Time.time - startedAt >= sheet.frames * frameSeconds;

        /// <summary>Segundos que dura una pasada de la hoja en curso.</summary>
        public float Length => sheet != null ? sheet.frames * frameSeconds : 0f;

        public float Angle
        {
            get => angle;
            set
            {
                angle = value;
                Refresh();
            }
        }

        internal void Configure(SpriteRenderer newTarget, DirectionalArt newArt)
        {
            target = newTarget;
            art = newArt;
        }

        /// <summary>
        /// Empieza <paramref name="sheetName"/> (si no existe, <paramref name="fallback"/>; si tampoco, no cambia nada) a
        /// <paramref name="milliseconds"/> por cuadro. Con el mismo nombre y sin <paramref name="restart"/> sigue donde iba.
        /// </summary>
        public bool Play(string sheetName, float milliseconds, bool looping, string fallback = null, bool restart = false)
        {
            var found = art != null ? art.Find(sheetName) ?? (fallback != null ? art.Find(fallback) : null) : null;
            if (found == null)
                return false;
            if (!restart && sheet == found && loop == looping)
                return true;
            sheet = found;
            Current = found.name;
            frameSeconds = Mathf.Max(0.005f, milliseconds / 1000f);
            loop = looping;
            startedAt = Time.time;
            Refresh();
            return true;
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (sheet == null || target == null)
                return;
            int frame = Mathf.FloorToInt((Time.time - startedAt) / frameSeconds);
            frame = loop ? frame % sheet.frames : Mathf.Min(frame, sheet.frames - 1);
            int row = 0;
            bool flip = false;
            if (sheet.rows == DirectionalArt.DirectionRows)
                row = DirectionalArt.RowFor(angle, out flip);
            target.sprite = sheet.Get(row, frame);
            target.flipX = flip;
        }
    }
}
