using System;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Rematar (Carnívoro): más daño a los enemigos heridos, los que tienen la mitad de la vida o menos. Con
    /// Presa a ciegas (Calamar) los cegados cuentan como heridos; con Sangre en el agua (Tiburón), rematar a un
    /// enemigo recarga la esquiva al instante.
    /// </summary>
    public class FinisherBenefit : MonoBehaviour
    {
        float threshold = 0.5f;
        float multiplier = 2f;
        float bossMultiplier = 1.5f;
        bool blindCounts;
        bool dodgeOnFinish;
        GameObject finishEffect;
        PlayerController controller;
        Func<Health, int, int> modifier;

        /// <summary>Remates que recargaron la esquiva (para las pruebas).</summary>
        public int DodgeRefills { get; private set; }

        public void Configure(PlayerController newController, float newThreshold, float newMultiplier, float newBossMultiplier,
            bool newBlindCounts, bool newDodgeOnFinish, GameObject newFinishEffect)
        {
            Unsubscribe();
            controller = newController;
            threshold = newThreshold;
            multiplier = newMultiplier;
            bossMultiplier = newBossMultiplier;
            blindCounts = newBlindCounts;
            dodgeOnFinish = newDodgeOnFinish;
            finishEffect = newFinishEffect;

            modifier = Modify;
            Health.AddDamageModifier(modifier);
        }

        /// <summary>True si ese objetivo cuenta como herido.</summary>
        public bool IsWounded(Health target)
        {
            if (target.Current <= target.Max * threshold)
                return true;
            return blindCounts && target.TryGetComponent<EnemyAI>(out var ai) && ai.IsBlind;
        }

        int Modify(Health target, int amount)
        {
            if (!target.TryGetComponent<Enemy>(out _) || !IsWounded(target))
                return amount;

            int damage = Health.ScaleDamage(amount, target.TryGetComponent<IBoss>(out _) ? bossMultiplier : multiplier);
            Sfx.Play(Sounds.LineageFinisher);

            // El modificador se aplica justo antes de quitar la vida: si llega a 0, este golpe es el remate (aunque
            // la muerte se anuncie más tarde, como la de la ameba que se divide).
            if (damage >= target.Current)
                OnFinished();
            return damage;
        }

        void OnFinished()
        {
            if (!dodgeOnFinish || controller == null)
                return;

            controller.ResetDodgeCooldown();
            DodgeRefills++;
            Sfx.Play(Sounds.PlayerDodgeReady);
            OneShotEffect.Spawn(finishEffect, controller.transform.position, parent: controller.transform);
        }

        void Unsubscribe()
        {
            if (modifier != null)
                Health.RemoveDamageModifier(modifier);
        }

        void OnDestroy() => Unsubscribe();
    }
}
