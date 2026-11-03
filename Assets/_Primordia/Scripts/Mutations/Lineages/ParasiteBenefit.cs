using System;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Chupar (Parásito, piso 3): cada cierto daño a un enemigo le pega un <see cref="Parasite"/> que le quita 1 por segundo;
    /// cada cierta vida chupada cura medio corazón (el doble con un jefe). El parásito salta a otro enemigo al morir el
    /// suyo y hay un máximo de parásitos vivos en total. Al perder el linaje se quitan todos.
    /// </summary>
    public class ParasiteBenefit : MonoBehaviour
    {
        // Cada cuánto quita de la cuenta a los enemigos ya destruidos.
        const float PruneInterval = 5f;

        // Lo que tarda la gota de vida en llegar al jugador.
        const float DropSeconds = 0.45f;

        int damagePerParasite = 15;
        int parasiteDamage = 1;
        int drainPerHeal = 15;
        int bossDrainPerHeal = 30;
        Health player;
        GameObject healEffect;
        GameObject markerPrefab;
        GameObject jumpEffect;
        GameObject drainDrop;
        float nextPrune;
        Func<Health, int, int> modifier;

        // Daño hecho a cada enemigo desde el último parásito, y el aviso con el que se vigila.
        readonly Dictionary<Health, int> dealt = new();
        readonly Dictionary<Health, Action<int>> handlers = new();
        int drained;
        int bossDrained;

        /// <summary>Medios corazones curados chupando (para las pruebas).</summary>
        public int Healed { get; private set; }

        /// <summary>Daño que falta para pegar parásito a ese enemigo.</summary>
        public int DamageTowards(Health target) => dealt.TryGetValue(target, out var value) ? value : 0;

        public void Configure(Health newPlayer, int newDamagePerParasite, int newParasiteDamage, int newDrainPerHeal,
            int newBossDrainPerHeal, int newLimit, GameObject newHealEffect, GameObject newMarkerPrefab, GameObject newJumpEffect,
            GameObject newDrainDrop = null)
        {
            drainDrop = newDrainDrop;
            Unsubscribe();
            player = newPlayer;
            damagePerParasite = Mathf.Max(1, newDamagePerParasite);
            parasiteDamage = Mathf.Max(1, newParasiteDamage);
            drainPerHeal = Mathf.Max(1, newDrainPerHeal);
            bossDrainPerHeal = Mathf.Max(1, newBossDrainPerHeal);
            healEffect = newHealEffect;
            markerPrefab = newMarkerPrefab;
            jumpEffect = newJumpEffect;
            SetLimit(newLimit);

            // El modificador solo sirve para enterarse de a quién se hace daño; la cuenta sale del daño que de verdad quita.
            modifier = Watch;
            Health.AddDamageModifier(modifier);
            Parasite.Drained += OnDrained;
        }

        /// <summary>Máximo de parásitos vivos (la Lamprea lo sube a 6 al girar).</summary>
        public void SetLimit(int limit) => Parasite.Limit = Mathf.Max(1, limit);

        int Watch(Health target, int amount)
        {
            if (!handlers.ContainsKey(target) && target.TryGetComponent<Enemy>(out _))
            {
                Action<int> handler = damage => OnDamaged(target, damage);
                handlers[target] = handler;
                target.Damaged += handler;
            }
            return amount;
        }

        void OnDamaged(Health target, int damage)
        {
            // El daño de los propios parásitos no pega más parásitos.
            // Tampoco el golpe que lo mata: no hay a quién pegárselo (el parásito saltaría en el acto).
            if (Parasite.Ticking || target == null || target.Current <= 0)
                return;

            dealt.TryGetValue(target, out int total);
            total += damage;
            if (total >= damagePerParasite)
            {
                total %= damagePerParasite;
                Parasite.Attach(target, parasiteDamage, markerPrefab, jumpEffect);
            }
            dealt[target] = total;
        }

        void OnDrained(Parasite parasite, int amount)
        {
            if (player == null || player.IsDead)
                return;

            // Una gota de vida sube del enemigo al jugador con cada tic.
            var drop = OneShotEffect.Spawn(drainDrop, parasite.transform.position);
            if (drop != null && drop.TryGetComponent<ArcFlight>(out var flight))
                flight.Launch(parasite.transform.position, transform, DropSeconds, 0.4f);

            if (parasite.OnBoss)
            {
                bossDrained += amount;
                while (bossDrained >= bossDrainPerHeal)
                {
                    bossDrained -= bossDrainPerHeal;
                    Heal();
                }
            }
            else
            {
                drained += amount;
                while (drained >= drainPerHeal)
                {
                    drained -= drainPerHeal;
                    Heal();
                }
            }
        }

        void Heal()
        {
            Healed++;
            if (player.Current >= player.Max)
                return;
            player.Heal(1);
            OneShotEffect.Spawn(healEffect, transform.position, parent: transform);
        }

        void Update()
        {
            if (Time.time < nextPrune)
                return;
            nextPrune = Time.time + PruneInterval;

            var gone = new List<Health>();
            foreach (var target in handlers.Keys)
            {
                if (target == null)
                    gone.Add(target);
            }
            foreach (var target in gone)
            {
                handlers.Remove(target);
                dealt.Remove(target);
            }
        }

        void Unsubscribe()
        {
            if (modifier != null)
                Health.RemoveDamageModifier(modifier);
            modifier = null;
            Parasite.Drained -= OnDrained;
            foreach (var pair in handlers)
            {
                if (pair.Key != null)
                    pair.Key.Damaged -= pair.Value;
            }
            handlers.Clear();
            dealt.Clear();
        }

        void OnDestroy()
        {
            Unsubscribe();
            Parasite.RemoveAll();
            Parasite.Limit = Parasite.DefaultLimit;
        }
    }
}
