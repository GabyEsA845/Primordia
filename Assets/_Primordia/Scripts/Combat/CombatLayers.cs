using UnityEngine;

namespace Primordia.Combat
{
    public enum Team
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Nombres de las capas de física del combate. Las crea el menú
    /// Primordia > Combat > Configurar capas de física, que también decide qué choca con qué:
    /// los proyectiles de un equipo atraviesan a su propio equipo y a los demás proyectiles.
    /// </summary>
    public static class CombatLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string EnemyProjectile = "EnemyProjectile";

        public static int ProjectileLayer(Team team)
        {
            string layerName = team == Team.Player ? PlayerProjectile : EnemyProjectile;
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                return layer;

            Debug.LogWarning($"Falta la capa '{layerName}'. Usa Primordia > Combat > Configurar capas de física.");
            return 0;
        }
    }
}
