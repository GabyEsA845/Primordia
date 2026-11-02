using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>Fuego del piso 3: el registro de lo inflamable y la barra de quemado.</summary>
    public class FireTests
    {
        class Dummy : IFlammable
        {
            public Vector2 Position { get; set; }
            public bool CanIgnite { get; set; } = true;
            public int Ignited;

            public void Ignite()
            {
                Ignited++;
                CanIgnite = false;
            }
        }

        [Test]
        public void Flammables_FindsOnlyWhatCanBurnWithinTheRadius_AndIgnitesIt()
        {
            var near = new Dummy { Position = new Vector2(1f, 0f) };
            var far = new Dummy { Position = new Vector2(5f, 0f) };
            var burnt = new Dummy { Position = new Vector2(0.5f, 0f), CanIgnite = false };
            Flammables.Register(near);
            Flammables.Register(far);
            Flammables.Register(burnt);
            try
            {
                var found = Flammables.Near(Vector2.zero, 2f);
                Assert.AreEqual(1, found.Count);
                Assert.AreSame(near, found[0]);

                Assert.AreEqual(1, Flammables.IgniteNear(Vector2.zero, 2f));
                Assert.AreEqual(1, near.Ignited);
                Assert.AreEqual(0, far.Ignited);
                Assert.AreEqual(0, Flammables.IgniteNear(Vector2.zero, 2f), "Lo que ya arde no se vuelve a prender.");
            }
            finally
            {
                Flammables.Unregister(near);
                Flammables.Unregister(far);
                Flammables.Unregister(burnt);
            }
        }

        [Test]
        public void GroundFire_WithoutArt_DoesNothing()
        {
            var previous = ReactionKit.Current;
            ReactionKit.Current = null;
            try
            {
                Assert.IsNull(GroundFire.Spawn(Vector2.zero));
            }
            finally
            {
                ReactionKit.Current = previous;
            }
        }
    }
}
