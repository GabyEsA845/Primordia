using NUnit.Framework;
using Primordia.Audio;

namespace Primordia.Tests
{
    /// <summary>Efectos de escenario que solo se oyen cerca (fumarolas y burbujas de metano).</summary>
    public class NearbySoundTests
    {
        [Test]
        public void Volume_IsFullClose_FadesWithDistance_AndIsSilentFar()
        {
            Assert.AreEqual(1f, NearbySound.VolumeAt(0f));
            Assert.AreEqual(1f, NearbySound.VolumeAt(NearbySound.FullRange));
            Assert.AreEqual(0.5f, NearbySound.VolumeAt((NearbySound.FullRange + NearbySound.Range) / 2f), 0.001f);
            Assert.AreEqual(0f, NearbySound.VolumeAt(NearbySound.Range));
            Assert.AreEqual(0f, NearbySound.VolumeAt(100f));
        }
    }
}
