using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>Cruce de las puertas de arriba: cuándo el jugador pasa por detrás del arco.</summary>
    public class DoorwayCrossingTests
    {
        static readonly Vector2 Center = new(10f, 20f);

        [Test]
        public void StandingInFrontOfTheDoor_IsNotInside()
        {
            Assert.IsFalse(DoorwayCrossing.IsInside(Center, new Vector2(10f, 18.5f)), "Delante de la pared, el jugador va por delante.");
        }

        [Test]
        public void FeetInTheOpening_AreInside_AndGoDarkerAsTheyEnter()
        {
            Assert.IsTrue(DoorwayCrossing.IsInside(Center, new Vector2(10f, 19.2f)));
            Assert.IsTrue(DoorwayCrossing.IsInside(Center, new Vector2(10.3f, 20.8f)));
            Assert.Less(DoorwayCrossing.Depth(Center, new Vector2(10f, 19.2f)), DoorwayCrossing.Depth(Center, new Vector2(10f, 20.8f)));
        }

        [Test]
        public void PastTheArchOrBesideIt_IsNotInside()
        {
            Assert.IsFalse(DoorwayCrossing.IsInside(Center, new Vector2(10f, 21.6f)), "Ya al otro lado.");
            Assert.IsFalse(DoorwayCrossing.IsInside(Center, new Vector2(12f, 20f)), "Junto a la puerta, no dentro.");
        }
    }
}
