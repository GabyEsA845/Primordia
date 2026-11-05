using NUnit.Framework;
using Primordia.UI;

namespace Primordia.Tests
{
    /// <summary>Animación del letrero de las salas con tema (piso 3, paso 6 bloque 1).</summary>
    public class RoomSignBannerTests
    {
        [Test]
        public void Sign_StartsAndEndsOffscreen()
        {
            Assert.Less(RoomSignBanner.TopAt(0f), 0f);
            Assert.Less(RoomSignBanner.TopAt(RoomSignBanner.Delay), 0f);
            Assert.Less(RoomSignBanner.TopAt(RoomSignBanner.TotalTime), 0f);
        }

        [Test]
        public void Sign_BouncesPastItsRestingPlaceAndSettlesAt18()
        {
            float max = float.MinValue;
            for (float t = 0f; t < RoomSignBanner.TotalTime; t += 0.01f)
                max = UnityEngine.Mathf.Max(max, RoomSignBanner.TopAt(t));
            Assert.Greater(max, 18f);
            Assert.AreEqual(18f, RoomSignBanner.TopAt(RoomSignBanner.Delay + RoomSignBanner.InTime + 1f), 0.001f);
        }
    }
}
