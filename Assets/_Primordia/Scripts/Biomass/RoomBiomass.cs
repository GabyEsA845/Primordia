using System.Collections;
using System.Collections.Generic;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Biomass
{
    /// <summary>
    /// Reparte la biomasa de una sala: cada enemigo, al morir, tira su probabilidad (<see cref="EnemyData"/>;
    /// las invocaciones del jefe no sueltan nada);
    /// el último completa lo que falte para el mínimo de la sala. Al limpiarla, toda la biomasa que siga en
    /// el suelo vuela hacia el jugador y se recoge sola.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class RoomBiomass : MonoBehaviour
    {
        public const int SmallValue = 1;
        public const int BigValue = 5;

        [SerializeField] BiomassPickup smallPrefab;
        [SerializeField] BiomassPickup bigPrefab;
        [Tooltip("Biomasa mínima que da la sala entre todos sus enemigos.")]
        [SerializeField, Min(0)] int minimum = 2;
        [Tooltip("Segundos entre limpiar la sala y que la biomasa vuele hacia el jugador (para verla caer).")]
        [SerializeField, Min(0f)] float attractDelay = 0.4f;

        Room room;
        readonly List<BiomassPickup> dropped = new();

        /// <summary>Valor total que ha soltado la sala hasta ahora.</summary>
        public int DroppedValue { get; private set; }

        /// <summary>La biomasa de esta sala que sigue en el suelo.</summary>
        public IReadOnlyList<BiomassPickup> OnFloor => dropped;

        void Awake() => room = GetComponent<Room>();

        void OnEnable()
        {
            room.EnemyDied += OnEnemyDied;
            room.Cleared += OnCleared;
        }

        void OnDisable()
        {
            room.EnemyDied -= OnEnemyDied;
            room.Cleared -= OnCleared;
        }

        internal void Configure(BiomassPickup newSmall, BiomassPickup newBig, int newMinimum = 2, float newAttractDelay = 0.4f)
        {
            smallPrefab = newSmall;
            bigPrefab = newBig;
            minimum = Mathf.Max(0, newMinimum);
            attractDelay = Mathf.Max(0f, newAttractDelay);
        }

        /// <summary>Lo que suelta un enemigo de esos datos con esa tirada (0 a 1): 5, 1 o 0, más su biomasa fija.</summary>
        public static int Roll(EnemyData data, float roll)
        {
            if (data == null)
                return 0;

            int value = data.FixedBiomass;
            if (roll < data.BigBiomassChance)
                value += BigValue;
            else if (roll < data.BigBiomassChance + data.SmallBiomassChance)
                value += SmallValue;
            return value;
        }

        /// <summary>
        /// Cuántas piezas grandes y pequeñas forman ese valor: 5 = 1 grande, 6 = 1 grande y 1 pequeña.
        /// Desde 15, cambia una grande por 5 pequeñas para que una cantidad grande (el jefe) se vea como un
        /// puñado: 15 = 2 grandes y 5 pequeñas.
        /// </summary>
        public static (int big, int small) Split(int value)
        {
            if (value <= 0)
                return (0, 0);

            int big = value / BigValue;
            if (big >= 3)
                big--;
            return (big, value - big * BigValue);
        }

        void OnEnemyDied(Room _, Enemy enemy)
        {
            int value = enemy.DropsBiomass ? Roll(enemy.Data, Random.value) : 0;

            // El último enemigo completa el mínimo de la sala.
            if (room.AliveEnemies == 0)
                value = Mathf.Max(value, minimum - DroppedValue);

            Drop(value, enemy.transform.position);
        }

        /// <summary>Suelta biomasa por ese valor en esa posición, en piezas grandes y pequeñas.</summary>
        internal void Drop(int value, Vector2 position)
        {
            var (big, small) = Split(value);
            for (int i = 0; i < big; i++)
                Spawn(bigPrefab, position);
            for (int i = 0; i < small; i++)
                Spawn(smallPrefab, position);
        }

        void Spawn(BiomassPickup prefab, Vector2 position)
        {
            if (prefab == null)
                return;

            var pickup = Instantiate(prefab, position, Quaternion.identity, transform);
            pickup.Pop();
            pickup.Collected += OnCollected;
            dropped.Add(pickup);
            DroppedValue += pickup.Value;
        }

        void OnCollected(BiomassPickup pickup) => dropped.Remove(pickup);

        void OnCleared(Room _) => StartCoroutine(AttractAfterDelay());

        IEnumerator AttractAfterDelay()
        {
            if (attractDelay > 0f)
                yield return new WaitForSeconds(attractDelay);

            foreach (var pickup in dropped)
            {
                if (pickup != null)
                    pickup.Attract();
            }
        }
    }
}
