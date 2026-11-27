using System;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Lo que el bestiario guarda entre partidas: cuántas veces se ha eliminado cada criatura (con 1 ya está
    /// descubierta). Se guarda en PlayerPrefs. El identificador es el nombre de su EnemyData sin "Enemy_".
    /// </summary>
    public static class BestiaryProgress
    {
        const string Prefix = "Primordia.Bestiary.";

        /// <summary>Una trampa o planta superada (no tienen EnemyData): se avisa con su identificador.</summary>
        public static event Action<string> TrapBeaten;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => TrapBeaten = null;

        public static int Kills(string id) => string.IsNullOrEmpty(id) ? 0 : PlayerPrefs.GetInt(Prefix + id, 0);

        public static bool IsDiscovered(string id) => Kills(id) > 0;

        public static void RegisterKill(string id) => Set(id, Kills(id) + 1);

        internal static void Set(string id, int kills)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (kills <= 0)
                PlayerPrefs.DeleteKey(Prefix + id);
            else
                PlayerPrefs.SetInt(Prefix + id, kills);
        }

        public static void ReportTrap(string id) => TrapBeaten?.Invoke(id);

        public static void Save() => PlayerPrefs.Save();

        public static string IdOf(string dataName) =>
            string.IsNullOrEmpty(dataName) ? "" : dataName.Replace("(Clone)", "").Trim().Replace("Enemy_", "");
    }
}
