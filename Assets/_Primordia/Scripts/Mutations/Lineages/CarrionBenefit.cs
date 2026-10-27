using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Carroña (Carroñero, piso 2): cada enemigo que muere deja sus restos unos segundos; al comerlos el jugador hace más
    /// daño durante un rato (+20 % por vez, hasta 3; cada vez renueva el tiempo). Una marca de colmillos sobre él dice
    /// cuántas lleva. Al perder el linaje desaparecen el bonus y los restos que quedaban.
    /// </summary>
    public class CarrionBenefit : MonoBehaviour
    {
        const string MarkerKey = "Carrion";

        CarrionRemains remainsPrefab;
        float remainsDuration = 4f;
        float bonusPerStack = 0.2f;
        float duration = 5f;
        int maxStacks = 3;
        GameObject eatEffect;
        GameObject[] markers = Array.Empty<GameObject>();
        Func<Health, int, int> modifier;
        readonly List<CarrionRemains> spawned = new();
        float until;

        /// <summary>Veces acumuladas que siguen activas (0 sin bonus).</summary>
        public int Stacks => Time.time < until ? stacks : 0;
        int stacks;

        /// <summary>Multiplicador de daño a los enemigos ahora mismo.</summary>
        public float Multiplier => 1f + bonusPerStack * Stacks;

        /// <summary>Se lanza al comer unos restos (los toques del Trilobite y la Hallucigenia).</summary>
        public event Action<CarrionRemains> Eaten;

        /// <summary>Restos que quedan en el suelo.</summary>
        public IReadOnlyList<CarrionRemains> Spawned => spawned;

        /// <summary>
        /// Prefab de los restos de la próxima muerte; si devuelve otro (la Doble ración), se usa ese. Se pregunta con el
        /// enemigo que muere.
        /// </summary>
        public Func<Enemy, CarrionRemains> RemainsOverride { get; set; }

        public void Configure(CarrionRemains newRemainsPrefab, float newRemainsDuration, float newBonusPerStack, float newDuration,
            int newMaxStacks, GameObject newEatEffect, GameObject[] newMarkers)
        {
            remainsPrefab = newRemainsPrefab;
            remainsDuration = Mathf.Max(0.1f, newRemainsDuration);
            bonusPerStack = Mathf.Max(0f, newBonusPerStack);
            duration = Mathf.Max(0.1f, newDuration);
            maxStacks = Mathf.Max(1, newMaxStacks);
            eatEffect = newEatEffect;
            markers = newMarkers ?? Array.Empty<GameObject>();

            if (modifier == null)
            {
                modifier = Modify;
                Health.AddDamageModifier(modifier);
                Enemy.AnyDied += OnEnemyDied;
            }
        }

        void OnEnemyDied(Enemy enemy)
        {
            if (enemy == null)
                return;
            var prefab = RemainsOverride?.Invoke(enemy) ?? remainsPrefab;
            var remains = CarrionRemains.Spawn(prefab, enemy.transform.position, remainsDuration, this, transform);
            if (remains != null)
                spawned.Add(remains);
        }

        /// <summary>Lo llaman los restos al comérselos.</summary>
        internal void OnEaten(CarrionRemains remains)
        {
            spawned.Remove(remains);
            AddStacks(remains.Value);
            OneShotEffect.Spawn(eatEffect, remains.transform.position);
            Sfx.Play(Sounds.CarrionEat, 1f, 1f + 0.08f * (Stacks - 1));
            Eaten?.Invoke(remains);
        }

        /// <summary>Suma veces al bonus (hasta el máximo) y renueva su tiempo.</summary>
        public void AddStacks(int count)
        {
            stacks = Mathf.Clamp(Stacks + count, 0, maxStacks);
            until = Time.time + duration;
            ShowMarker();
        }

        void ShowMarker()
        {
            StatusMarkers.Hide(this, MarkerKey);
            int index = Stacks - 1;
            if (index >= 0 && index < markers.Length && markers[index] != null)
                StatusMarkers.Show(this, MarkerKey, markers[index], until - Time.time);
        }

        int Modify(Health target, int amount)
        {
            int current = Stacks;
            if (current == 0 || !target.TryGetComponent<Enemy>(out _))
                return amount;
            return Health.ScaleDamage(amount, 1f + bonusPerStack * current);
        }

        void OnDestroy()
        {
            if (modifier != null)
            {
                Health.RemoveDamageModifier(modifier);
                Enemy.AnyDied -= OnEnemyDied;
            }
            StatusMarkers.Hide(this, MarkerKey);
            foreach (var remains in spawned)
            {
                if (remains != null)
                    Destroy(remains.gameObject);
            }
            spawned.Clear();
        }
    }
}
