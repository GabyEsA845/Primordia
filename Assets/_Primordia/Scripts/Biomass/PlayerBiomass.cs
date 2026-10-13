using System;
using UnityEngine;

namespace Primordia.Biomass
{
    /// <summary>
    /// Biomasa que lleva el jugador: la moneda de la partida. Va en el jugador, así que se pierde al morir
    /// (la escena se recarga con un jugador nuevo).
    /// </summary>
    public class PlayerBiomass : MonoBehaviour
    {
        [Tooltip("Biomasa al empezar. Útil para probar la tienda.")]
        [SerializeField, Min(0)] int startingAmount;

        public int Amount { get; private set; }

        /// <summary>Se lanza al cambiar la cantidad: (nueva cantidad, cambio).</summary>
        public event Action<int, int> Changed;

        void Awake() => Amount = startingAmount;

        public void Add(int value)
        {
            if (value <= 0)
                return;

            Amount += value;
            Changed?.Invoke(Amount, value);
        }

        public bool CanAfford(int price) => Amount >= price;

        /// <summary>Gasta esa cantidad si la tiene. Devuelve false (y no gasta nada) si no le llega.</summary>
        public bool TrySpend(int price)
        {
            if (price < 0 || !CanAfford(price))
                return false;

            if (price == 0)
                return true;

            Amount -= price;
            Changed?.Invoke(Amount, -price);
            return true;
        }
    }
}
