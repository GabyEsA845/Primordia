using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Punto al que sigue la cámara: el jugador, adelantado un poco hacia el cursor. Así se ve más de lo que
    /// hay hacia donde apuntas (normalmente, los enemigos) sin perder de vista al jugador.
    /// </summary>
    // Antes que la cámara (CinemachineBrain también se actualiza en LateUpdate), para que no vaya un cuadro por detrás.
    [DefaultExecutionOrder(-50)]
    public class CameraLookAhead : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [Tooltip("Parte de la distancia al cursor que se adelanta la cámara.")]
        [SerializeField, Range(0f, 1f)] float fraction = 0.25f;
        [Tooltip("Adelanto máximo, en unidades.")]
        [SerializeField, Min(0f)] float maxDistance = 3f;

        public PlayerController Player => player;

        /// <summary>Punto al que se desvía la cámara (escenas de los jefes) y cuánto: 0 = sigue al jugador, 1 = el punto.</summary>
        public Vector2 FocusPoint { get; private set; }
        public float FocusWeight { get; private set; }

        Vector3 lastNormal;

        /// <summary>Desvía la cámara hacia ese punto; con peso 0 vuelve a seguir al jugador.</summary>
        public void SetFocus(Vector2 point, float weight)
        {
            FocusPoint = point;
            FocusWeight = Mathf.Clamp01(weight);
        }

        public void ClearFocus() => FocusWeight = 0f;

        internal void Configure(PlayerController newPlayer, float newFraction = 0.25f, float newMaxDistance = 3f)
        {
            player = newPlayer;
            fraction = newFraction;
            maxDistance = newMaxDistance;
            Snap();
        }

        void Start() => Snap();

        void LateUpdate()
        {
            // En pausa no sigue al ratón: la cámara se queda quieta mientras se usan los menús.
            if (player != null && Time.timeScale > 0f)
                lastNormal = Target(player.transform.position, player.AimOffset, fraction, maxDistance);

            if (player != null)
                transform.position = Vector3.Lerp(lastNormal, new Vector3(FocusPoint.x, FocusPoint.y, lastNormal.z), FocusWeight);
        }

        /// <summary>Posición del punto: el jugador más una parte del camino al cursor, sin pasar del máximo.</summary>
        public static Vector3 Target(Vector3 playerPosition, Vector2 aimOffset, float fraction, float maxDistance) =>
            playerPosition + (Vector3)Vector2.ClampMagnitude(aimOffset * fraction, maxDistance);

        // Al empezar, sin adelanto: la cámara arranca sobre el jugador.
        void Snap()
        {
            if (player != null)
                transform.position = lastNormal = player.transform.position;
        }
    }
}
