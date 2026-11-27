using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Contadores de la partida en curso (pestaña Partida de la pausa): tiempo jugado, enemigos derrotados y jefes
    /// vencidos. Se ponen a cero al empezar, reiniciar o volver al menú.
    /// </summary>
    public static class RunStats
    {
        public static float PlayTime { get; private set; }
        public static int EnemiesDefeated { get; private set; }
        public static int BossesDefeated { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            PlayTime = 0f;
            EnemiesDefeated = 0;
            BossesDefeated = 0;
        }

        public static void AddTime(float seconds) => PlayTime += Mathf.Max(0f, seconds);

        public static void AddDefeat(bool boss)
        {
            if (boss)
                BossesDefeated++;
            else
                EnemiesDefeated++;
        }

        /// <summary>"24:13", o "1:02:09" a partir de una hora.</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int hours = total / 3600, minutes = total / 60 % 60;
            return hours > 0 ? $"{hours}:{minutes:00}:{total % 60:00}" : $"{minutes:00}:{total % 60:00}";
        }
    }
}
