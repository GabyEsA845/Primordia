using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Columna de algas de la sala del Anomalocaris (marca A): se mece por encima de los personajes y tapa la vista sin
    /// bloquear el paso. Si el jugador está debajo se vuelve translúcida, para que se vea a sí mismo; lo demás (el jefe)
    /// sigue tapado. El objeto está en la base de la columna.
    /// </summary>
    public class AlgaeColumn : MonoBehaviour
    {
        // Por encima de personajes, proyectiles y efectos; por debajo de la nieve marina (30) y los rayos de luz (40).
        public const int SortingOrder = 25;

        [SerializeField] SpriteRenderer target;
        [Tooltip("Zona que tapa, respecto a la base (en unidades): si el jugador está dentro, la columna se transparenta.")]
        [SerializeField] Rect cover = new(-1.8f, 0.1f, 3.6f, 5f);
        [Tooltip("Opacidad con el jugador debajo.")]
        [SerializeField, Range(0f, 1f)] float seeThrough = 0.4f;
        [Tooltip("Cuánto tarda en pasar de opaca a translúcida y al revés (segundos).")]
        [SerializeField, Min(0.01f)] float fadeTime = 0.2f;

        Transform player;

        /// <summary>Opacidad actual (1 opaca).</summary>
        public float Alpha => target != null ? target.color.a : 1f;

        internal void Configure(SpriteRenderer newTarget) => target = newTarget;

        /// <summary>True si <paramref name="point"/> queda tapado por la columna.</summary>
        public bool Covers(Vector2 point)
        {
            var local = point - (Vector2)transform.position;
            return cover.Contains(local);
        }

        void Update()
        {
            if (target == null)
                return;
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }

            float goal = Covers(player.position) ? seeThrough : 1f;
            var color = target.color;
            color.a = Mathf.MoveTowards(color.a, goal, (1f - seeThrough) / fadeTime * Time.deltaTime);
            target.color = color;
        }
    }
}
