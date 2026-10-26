using UnityEngine;
using UnityEngine.SceneManagement;

namespace Primordia.Core
{
    /// <summary>
    /// Cámara lenta de los Ojos compuestos: todo el juego va a <see cref="Scale"/> (0,6 = al 60 %) salvo el jugador, que se
    /// compensa con <see cref="PlayerSpeed"/> (movimiento, esquiva, cadencia, sus proyectiles y sus animaciones). La pausa y
    /// los menús siguen parando el tiempo del todo: <see cref="GameManager"/> usa esta escala cuando el juego corre.
    /// </summary>
    public static class TimeWarp
    {
        /// <summary>Escala del tiempo del mundo cuando el juego corre (1 = normal).</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>Multiplicador de las velocidades del jugador para que vaya a su ritmo normal (1 / <see cref="Scale"/>).</summary>
        public static float PlayerSpeed => 1f / Scale;

        public static bool IsActive => Scale < 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Scale = 1f;
            // Una escena nueva nunca empieza en cámara lenta.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Scale = 1f;

        /// <summary>Pone la cámara lenta (o la quita con 1). Si el juego está parado (pausa, menú), se nota al reanudar.</summary>
        public static void Set(float scale)
        {
            Scale = Mathf.Clamp(scale, 0.05f, 1f);
            if (Time.timeScale > 0f)
                Time.timeScale = Scale;
        }

        public static void Clear() => Set(1f);
    }
}
