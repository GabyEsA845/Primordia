using UnityEngine;

namespace Primordia.Audio
{
    /// <summary>
    /// Efecto suelto de escenario que solo se oye cerca (fumarolas, burbujas de metano): suena entero a pocas casillas del
    /// jugador, baja con la distancia y, lejos, no suena. Para bucles, <see cref="NearbyLoop"/>.
    /// </summary>
    public static class NearbySound
    {
        // Hasta aquí, volumen pleno; desde aquí, silencio.
        public const float FullRange = 4f;
        public const float Range = 12f;

        /// <summary>Volumen (0-1) con que se oye una fuente a esa distancia del jugador.</summary>
        public static float VolumeAt(float distance) => Mathf.Clamp01((Range - distance) / (Range - FullRange));

        /// <summary>Toca el efecto <paramref name="id"/> como si saliera de <paramref name="source"/>. Sin jugador no suena.</summary>
        public static void Play(string id, Vector2 source)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                return;
            float volume = VolumeAt(Vector2.Distance(player.transform.position, source));
            if (volume > 0f)
                Sfx.Play(id, volume);
        }
    }
}
