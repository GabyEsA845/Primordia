using System.Collections;
using NUnit.Framework;
using Primordia.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Temblor de pantalla: golpes que se apagan, temblores con dueño y el ajuste de la pausa.</summary>
    public class ScreenShakeTests
    {
        ScreenShake.Level saved;
        readonly object owner = new();

        [SetUp]
        public void SetUp()
        {
            saved = ScreenShake.Setting;
            ScreenShake.Setting = ScreenShake.Level.Normal;
        }

        [TearDown]
        public void TearDown()
        {
            ScreenShake.ClearRumble(owner);
            ScreenShake.Setting = saved;
        }

        [UnityTest]
        public IEnumerator Pulse_FadesOutOverItsDuration()
        {
            ScreenShake.Pulse(3f, 0.3f);
            Assert.AreEqual(3f, ScreenShake.Amplitude, 0.2f, "Empieza con toda su fuerza.");
            yield return new WaitForSeconds(0.15f);
            Assert.Less(ScreenShake.Amplitude, 2.5f, "Se va apagando.");
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(0f, ScreenShake.Amplitude, 0.001f, "Y se acaba.");
            Assert.AreEqual(Vector2.zero, ScreenShake.Offset);
        }

        [Test]
        public void Rumble_LastsUntilItsOwnerClearsIt_AndMovesByWholePixels()
        {
            ScreenShake.SetRumble(owner, 2f);
            Assert.AreEqual(2f, ScreenShake.RumbleOf(owner));
            Assert.GreaterOrEqual(ScreenShake.Amplitude, 2f);
            var offset = ScreenShake.Offset * 16f;
            Assert.AreEqual(Mathf.Round(offset.x), offset.x, 0.001f, "A píxeles enteros.");
            Assert.AreEqual(Mathf.Round(offset.y), offset.y, 0.001f);
            Assert.LessOrEqual(Mathf.Abs(offset.x), 2f);

            ScreenShake.ClearRumble(owner);
            Assert.AreEqual(0f, ScreenShake.RumbleOf(owner));
        }

        [Test]
        public void Setting_ReducedHalves_AndOffStopsIt()
        {
            ScreenShake.SetRumble(owner, 2f);
            ScreenShake.Setting = ScreenShake.Level.Reduced;
            Assert.AreEqual(1f, ScreenShake.Amplitude, 0.001f, "Suave: la mitad.");
            ScreenShake.Setting = ScreenShake.Level.Off;
            Assert.AreEqual(0f, ScreenShake.Amplitude, "Apagado: nada.");
            Assert.AreEqual(Vector2.zero, ScreenShake.Offset);
        }
    }
}
