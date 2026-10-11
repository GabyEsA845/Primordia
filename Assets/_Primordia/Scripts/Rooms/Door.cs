using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Puerta de una sala. Cerrada, bloquea el paso y se ve; abierta, deja pasar y se oculta.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Tooltip("Colisionador que bloquea el paso. Si está vacío, usa el del objeto.")]
        [SerializeField] Collider2D blocker;
        [Tooltip("Sprite de la puerta cerrada. Si está vacío, usa el del objeto.")]
        [SerializeField] SpriteRenderer closedSprite;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (blocker == null)
                blocker = GetComponent<Collider2D>();
            if (closedSprite == null)
                closedSprite = GetComponent<SpriteRenderer>();

            Open();
        }

        public void Open() => SetOpen(true);

        public void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            IsOpen = open;
            if (blocker != null)
                blocker.enabled = !open;
            if (closedSprite != null)
                closedSprite.enabled = !open;
        }
    }
}
