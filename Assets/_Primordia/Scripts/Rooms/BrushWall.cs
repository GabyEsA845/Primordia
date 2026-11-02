using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Maleza quemable (piso 3): una maraña de tallos secos que sella una puerta de atajo. Bloquea el paso hasta que arde:
    /// <see cref="Ignite"/> reproduce la animación de fuego y, en el cuadro que cuenta, deja de bloquear; luego se queda como
    /// paso abierto para siempre. Hasta que el jugador se acerca, el atajo no aparece en el mapa (<see cref="Discovered"/>).
    /// </summary>
    public class BrushWall : MonoBehaviour, IFlammable
    {
        // Cuánto hay que acercarse para descubrirlo, en unidades.
        const float DiscoverDistance = 7f;
        const float DiscoverCheckSeconds = 0.25f;

        [Tooltip("Colisionador que bloquea el paso hasta que arde.")]
        [SerializeField] Collider2D blocker;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] BrushAnimation brush;

        Coroutine burning;
        float nextCheck;
        Transform player;

        /// <summary>Celdas del mapa que cubre (para marcarlas en el mapa).</summary>
        public readonly List<Vector2Int> Cells = new();

        /// <summary>True cuando el jugador lo ha visto de cerca: desde entonces el mapa lo marca.</summary>
        public bool Discovered { get; private set; }

        /// <summary>True cuando ya no bloquea (desde el cuadro que cuenta del fuego).</summary>
        public bool IsOpen { get; private set; }

        public bool IsBurning => burning != null;

        public bool CanIgnite => !IsOpen && !IsBurning;

        public event System.Action Burned;

        internal void Configure(Collider2D newBlocker, SpriteRenderer newRenderer, BrushAnimation newAnimation, IEnumerable<Vector2Int> cells)
        {
            blocker = newBlocker;
            spriteRenderer = newRenderer;
            brush = newAnimation;
            Cells.Clear();
            Cells.AddRange(cells);
        }

        void Awake()
        {
            if (spriteRenderer != null && brush != null && brush.idle != null)
                spriteRenderer.sprite = brush.idle;
        }

        public void Ignite()
        {
            if (CanIgnite && isActiveAndEnabled)
                burning = StartCoroutine(Burn());
        }

        IEnumerator Burn()
        {
            var frames = brush != null ? brush.burn : null;
            int count = frames != null ? frames.Length : 0;
            int blockingEnds = Mathf.Clamp(brush != null ? brush.opensAtFrame : 0, 0, Mathf.Max(0, count - 1));
            for (int i = 0; i < count; i++)
            {
                if (spriteRenderer != null)
                    spriteRenderer.sprite = frames[i];
                if (i == blockingEnds)
                    Open();
                float seconds = brush.durations != null && i < brush.durations.Length ? brush.durations[i] : 0.1f;
                yield return new WaitForSeconds(seconds);
            }
            if (!IsOpen)
                Open();
            if (spriteRenderer != null)
                spriteRenderer.sprite = brush != null && brush.gone != null ? brush.gone : null;
            burning = null;
        }

        void Open()
        {
            IsOpen = true;
            if (blocker != null)
                blocker.enabled = false;
            Discovered = true;
            Burned?.Invoke();
        }

        void Update()
        {
            if (Discovered || Time.time < nextCheck)
                return;
            nextCheck = Time.time + DiscoverCheckSeconds;
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
            }
            if (player != null && ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude <= DiscoverDistance * DiscoverDistance)
                Discovered = true;
        }
    }
}
