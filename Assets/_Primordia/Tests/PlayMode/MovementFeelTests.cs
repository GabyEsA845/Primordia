using System.Collections;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Sensación de movimiento del jugador: arranque y frenada cortos en vez de velocidad instantánea (con el peso de cada
    /// cuerpo), y la sombra bajo sus pies, que se encoge en la esquiva.
    /// </summary>
    public class MovementFeelTests
    {
        GameObject player;
        PlayerController controller;
        Rigidbody2D body;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("Player");
            player.transform.position = new Vector2(300f, 300f);
            body = player.AddComponent<Rigidbody2D>();
            player.AddComponent<Health>();
            controller = player.AddComponent<PlayerController>();
            controller.readInput = false;
        }

        [TearDown]
        public void TearDown() => Object.Destroy(player);

        [UnityTest]
        public IEnumerator Walking_AcceleratesInsteadOfJumpingToFullSpeed()
        {
            controller.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            float early = body.linearVelocity.x;
            Assert.Greater(early, 0f, "Ya se mueve.");
            Assert.Less(early, controller.MoveSpeed * 0.9f, "No va a toda velocidad al segundo paso.");

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(controller.MoveSpeed, body.linearVelocity.x, 0.01f, "Alcanza la velocidad máxima enseguida.");
        }

        [UnityTest]
        public IEnumerator Stopping_DeceleratesInsteadOfStoppingDead()
        {
            controller.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForSeconds(0.3f);

            controller.SetInput(Vector2.zero, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.x, 0.1f, "Sigue deslizando un instante.");

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, body.linearVelocity.x, 0.01f, "Y acaba parándose.");
        }

        [UnityTest]
        public IEnumerator Inertia_ZeroGivesInstantSpeed_AndHigherSlides()
        {
            controller.Inertia = 0f;
            controller.SetInput(Vector2.right, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(controller.MoveSpeed, body.linearVelocity.x, 0.01f, "Sin inercia, arranca al instante.");

            controller.Inertia = 3f;
            controller.SetInput(Vector2.zero, Vector2.right);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.x, controller.MoveSpeed * 0.5f, "Con más inercia, frena más despacio.");
        }

        [UnityTest]
        public IEnumerator Shadow_ExistsUnderThePlayer_AndShrinksWhileDodging()
        {
            yield return null;
            var shadow = player.GetComponent<PlayerShadow>();
            Assert.IsNotNull(shadow, "El controlador añade la sombra.");
            Assert.Less(shadow.transform.GetChild(0).localPosition.y, 0f, "Está a los pies.");
            float rest = shadow.Renderer.transform.localScale.x;
            Assert.AreEqual(1f, rest, 0.001f);

            controller.SetInput(Vector2.right, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            yield return new WaitForSeconds(0.15f);
            Assert.Less(shadow.Renderer.transform.localScale.x, 0.9f, "Se encoge en la esquiva.");

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(1f, shadow.Renderer.transform.localScale.x, 0.01f, "Y vuelve a su tamaño.");
        }
    }
}
