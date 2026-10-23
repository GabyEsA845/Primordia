using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Cómo se ve la barra de vida del jefe.</summary>
    public enum BossBarStyle
    {
        Normal,
        /// <summary>Fase más agresiva: otro color.</summary>
        Enraged,
        /// <summary>No recibe daño: gris y con candado.</summary>
        Immune,
        /// <summary>Agotado, a punto de caer: roja y palpitando.</summary>
        Weak
    }

    /// <summary>
    /// Lo que tienen en común los jefes (virus gigante, ameba gigante): nombre y vida para la barra, centro de su
    /// sala para soltar la recompensa, y la sala que les da el generador del piso.
    /// </summary>
    public interface IBoss
    {
        string DisplayName { get; }
        Health Health { get; }
        BossBarStyle BarStyle { get; }

        /// <summary>Centro de su sala (o donde apareció, si no tiene): ahí deja caer su recompensa.</summary>
        Vector2 ArenaCenter { get; }

        /// <summary>Centro y tamaño (en unidades) del suelo de su sala.</summary>
        void SetArena(Transform center, Vector2 size);
    }

    /// <summary>Jefe activo ahora mismo, para la barra de vida. Null si no hay ninguno.</summary>
    public static class ActiveBoss
    {
        public static IBoss Current { get; private set; }

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        public static void Set(IBoss boss) => Current = boss;

        public static void Clear(IBoss boss)
        {
            if (Current == boss)
                Current = null;
        }
    }
}
