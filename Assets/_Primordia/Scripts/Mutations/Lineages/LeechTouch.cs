using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Charca de sanguijuelas (Sanguijuela): los charcos y la baba del jugador pegan un parásito a quien los pisa y hacen más daño
    /// (<see cref="PoisonPuddle.PlayerBonus"/>); al reventar la bola del Escupitajo saltan parásitos a los enemigos más cercanos de
    /// la sala hasta llenar el máximo. El dibujo rojizo y que no arda lo ponen la bola, la banda y los charcos
    /// (<see cref="PoisonSpitBall"/>). Los parásitos son los de <see cref="ParasiteBenefit"/> (cuentan para su máximo).
    /// </summary>
    public class LeechTouch : MonoBehaviour
    {
        ParasiteBenefit benefit;
        Weapon weapon;
        int puddleBonus = 1;

        /// <summary>Parásitos pegados al reventar la bola o pisar un charco (para las pruebas).</summary>
        public int Attached { get; private set; }

        public void Configure(ParasiteBenefit newBenefit, int newPuddleBonus)
        {
            Unsubscribe();
            benefit = newBenefit;
            weapon = GetComponent<Weapon>();
            puddleBonus = Mathf.Max(0, newPuddleBonus);
            PoisonPuddle.PlayerBonus = puddleBonus;
            PoisonPuddle.EnemyAffected += OnPuddleStep;
            PoisonSpitBurst.Burst += OnBurst;
        }

        void OnPuddleStep(Health enemy)
        {
            if (benefit != null && benefit.AttachTo(enemy) != null)
                Attached++;
        }

        // Al reventar la bola grande, los parásitos saltan a los enemigos más cercanos del reventón (uno por enemigo) hasta el máximo.
        void OnBurst(Vector2 position, Weapon source, bool mini)
        {
            if (benefit == null || mini || source == null || source != weapon)
                return;

            var candidates = new List<Enemy>();
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                if (enemy.Health != null && !enemy.Health.IsDead && !Parasite.Has(enemy.Health))
                    candidates.Add(enemy);
            }
            candidates.Sort((a, b) => ((Vector2)a.transform.position - position).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - position).sqrMagnitude));

            foreach (var enemy in candidates)
            {
                if (benefit.AttachTo(enemy.Health) == null)
                    break;
                Attached++;
                // La sanguijuela cruza en arco del reventón al enemigo.
                var leap = OneShotEffect.Spawn(benefit.JumpEffect, position);
                if (leap != null && leap.TryGetComponent<ArcFlight>(out var flight))
                    flight.Launch(position, enemy.transform, Parasite.LeapSeconds, Parasite.LeapArc);
            }
        }

        void Unsubscribe()
        {
            PoisonPuddle.EnemyAffected -= OnPuddleStep;
            PoisonSpitBurst.Burst -= OnBurst;
        }

        void OnDestroy()
        {
            Unsubscribe();
            PoisonPuddle.PlayerBonus = 0;
        }
    }
}
