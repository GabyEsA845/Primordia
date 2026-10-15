using Primordia.Combat;
using Primordia.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Mira dibujada por la interfaz que sigue al ratón y sustituye al cursor del sistema mientras se juega.
    /// En la pausa, la elección de mutación, la derrota y la victoria se oculta y vuelve el cursor normal.
    /// Se contrae un instante con cada disparo o mordisco del arma del jugador.
    /// Sin sprite, no se muestra y el cursor del sistema sigue visible.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] Sprite idleSprite;
        [Tooltip("Cuadros al disparar; al acabar vuelve a la mira normal. Vacío = sin animación.")]
        [SerializeField] Sprite[] fireFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float fireFrameDuration = 0.04f;
        [Tooltip("Arma que hace contraerse la mira. Si está vacía, usa la del objeto con la etiqueta Player.")]
        [SerializeField] Weapon weapon;

        /// <summary>Las pruebas lo desactivan para mover la mira con <see cref="MoveTo"/>.</summary>
        internal bool readPointer = true;

        Image image;
        RectTransform rect;
        GameManager manager;
        float fireStart = -1f;
        bool shown;

        public bool IsShown => shown;
        public bool IsFiring => fireStart >= 0f;
        public Sprite CurrentSprite => image.sprite;

        void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            rect = (RectTransform)transform;
            image.enabled = false;
        }

        void Start()
        {
            // Por encima del resto del HUD.
            transform.SetAsLastSibling();
            manager = GameManager.Instance;

            if (weapon == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    weapon = player.GetComponent<Weapon>();
            }

            if (weapon != null)
                weapon.Fired += OnFired;
        }

        void OnDestroy()
        {
            if (weapon != null)
                weapon.Fired -= OnFired;
        }

        void OnDisable()
        {
            if (shown)
                Cursor.visible = true;
            shown = false;
        }

        internal void ConfigureLook(Sprite idle, Sprite[] fire, float frameDuration)
        {
            idleSprite = idle;
            fireFrames = fire ?? System.Array.Empty<Sprite>();
            fireFrameDuration = Mathf.Max(0.01f, frameDuration);
        }

        internal void Configure(Weapon newWeapon) => weapon = newWeapon;

        /// <summary>Pone la mira en ese punto de la pantalla (en píxeles).</summary>
        internal void MoveTo(Vector2 screenPosition)
        {
            var parent = rect.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out var local))
                rect.localPosition = local;
            else
                rect.position = screenPosition;
        }

        void OnFired(Vector2 direction) => fireStart = Time.unscaledTime;

        void LateUpdate()
        {
            SetShown(idleSprite != null && (manager == null || manager.State is GameState.Playing or GameState.Evolution));
            if (!shown)
                return;

            if (readPointer && Pointer.current != null)
                MoveTo(Pointer.current.position.ReadValue());

            image.sprite = idleSprite;
            if (fireStart >= 0f)
            {
                int frame = Mathf.FloorToInt((Time.unscaledTime - fireStart) / fireFrameDuration);
                if (frame < fireFrames.Length)
                    image.sprite = fireFrames[frame];
                else
                    fireStart = -1f;
            }
        }

        void SetShown(bool value)
        {
            if (shown == value)
                return;

            shown = value;
            image.enabled = value;
            Cursor.visible = !value;
            if (!value)
                fireStart = -1f;
        }
    }
}
