using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Anota cada enemigo, jefe o trampa superados en el bestiario y en los contadores de la partida.
    /// Lo añade <see cref="GameUI"/>: las escenas sin interfaz (pruebas) no escriben nada.
    /// </summary>
    public sealed class BestiaryTracker : MonoBehaviour
    {
        GameManager manager;

        void OnEnable()
        {
            Enemy.Defeated += OnDefeated;
            BestiaryProgress.TrapBeaten += OnTrap;
        }

        void Start()
        {
            manager = GameManager.Instance;
            if (manager != null)
                manager.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            Enemy.Defeated -= OnDefeated;
            BestiaryProgress.TrapBeaten -= OnTrap;
            BestiaryProgress.Save();
        }

        void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= OnStateChanged;
        }

        // Se escribe a disco al pausar, elegir mutación, morir o ganar: no en mitad del combate.
        void OnStateChanged(GameState previous, GameState current)
        {
            if (current is GameState.Paused or GameState.Defeat or GameState.Victory or GameState.MutationChoice)
                BestiaryProgress.Save();
        }

        void OnDefeated(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null)
                return;
            var id = BestiaryProgress.IdOf(enemy.Data.name);
            BestiaryProgress.RegisterKill(id);
            RunStats.AddDefeat(id.EndsWith("Boss"));
        }

        void OnTrap(string id) => BestiaryProgress.RegisterKill(id);
    }
}
