using System.Collections;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Puerta de una sala. Cerrada, bloquea el paso y se ve; abierta, deja pasar y se oculta.
    /// Con cuadros de animación (membrana de abierta a cerrada), se anima al cerrarse y al abrirse.
    /// El paso se bloquea o se libera en el acto; la animación es solo visual.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Tooltip("Colisionador que bloquea el paso. Si está vacío, usa el del objeto.")]
        [SerializeField] Collider2D blocker;
        [Tooltip("Sprite de la puerta cerrada. Si está vacío, usa el del objeto.")]
        [SerializeField] SpriteRenderer closedSprite;
        [Tooltip("Cuadros de la membrana, de abierta (0) a cerrada (último). Vacío = sin animación.")]
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(0.01f)] float frameDuration = 0.08f;

        public bool IsOpen { get; private set; }

        /// <summary>True mientras se ve la animación de abrir o cerrar.</summary>
        public bool IsAnimating => animationRoutine != null;

        Coroutine animationRoutine;

        void Awake()
        {
            if (blocker == null)
                blocker = GetComponent<Collider2D>();
            if (closedSprite == null)
                closedSprite = GetComponent<SpriteRenderer>();

            // Al empezar ya está abierta, sin animación.
            SetOpen(true, animate: false);
        }

        public void Open() => SetOpen(true, animate: true);

        public void Close() => SetOpen(false, animate: true);

        internal void Configure(Collider2D newBlocker, SpriteRenderer newClosedSprite, Sprite[] newFrames, float newFrameDuration = 0.08f)
        {
            blocker = newBlocker;
            closedSprite = newClosedSprite;
            frames = newFrames;
            frameDuration = Mathf.Max(0.01f, newFrameDuration);
        }

        void SetOpen(bool open, bool animate)
        {
            IsOpen = open;
            if (blocker != null)
                blocker.enabled = !open;
            if (closedSprite == null)
                return;

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            bool hasFrames = frames != null && frames.Length > 0;
            if (animate && hasFrames && isActiveAndEnabled)
            {
                animationRoutine = StartCoroutine(Animate(open));
                return;
            }

            if (hasFrames)
                closedSprite.sprite = frames[open ? 0 : frames.Length - 1];
            closedSprite.enabled = !open;
        }

        IEnumerator Animate(bool open)
        {
            closedSprite.enabled = true;
            for (int i = 0; i < frames.Length; i++)
            {
                closedSprite.sprite = frames[open ? frames.Length - 1 - i : i];
                yield return new WaitForSeconds(frameDuration);
            }

            closedSprite.enabled = !open;
            animationRoutine = null;
        }
    }
}
