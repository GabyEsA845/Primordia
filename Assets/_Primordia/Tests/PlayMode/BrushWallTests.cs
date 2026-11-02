using System.Collections;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Maleza quemable del piso 3: bloquea hasta el cuadro del fuego que cuenta y luego se queda abierta.</summary>
    public class BrushWallTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        static Sprite Sprite()
        {
            var texture = new Texture2D(2, 2);
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        BrushWall Make(out BoxCollider2D blocker)
        {
            root = new GameObject("Brush");
            var renderer = root.AddComponent<SpriteRenderer>();
            blocker = root.AddComponent<BoxCollider2D>();
            var animation = new BrushAnimation(Sprite(), new[] { Sprite(), Sprite(), Sprite(), Sprite() },
                new[] { 0.02f, 0.02f, 0.02f, 0.02f }, Sprite(), opensAtFrame: 2);
            var brush = root.AddComponent<BrushWall>();
            brush.Configure(blocker, renderer, animation, new[] { new Vector2Int(3, 4) });
            return brush;
        }

        [UnityTest]
        public IEnumerator Brush_BlocksUntilItBurns_ThenStaysOpen()
        {
            var brush = Make(out var blocker);
            Assert.IsTrue(blocker.enabled, "Cerrada bloquea el paso.");
            Assert.IsFalse(brush.IsOpen);
            Assert.IsTrue(brush.CanIgnite);

            brush.Ignite();
            Assert.IsFalse(brush.CanIgnite, "Mientras arde no se vuelve a prender.");
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(brush.IsOpen);
            Assert.IsFalse(blocker.enabled, "Quemada deja pasar.");
            Assert.IsFalse(brush.IsBurning);
            Assert.IsFalse(brush.CanIgnite, "Quemada no vuelve a arder.");
            Assert.IsTrue(brush.Discovered, "Una maleza quemada está descubierta.");
        }
    }
}
