using System.Linq;
using Primordia.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Rooms
{
    /// <summary>
    /// Atajos de la escena de prueba de la sala secreta (Test_Floor3Secret). 1 da un corazón morado, 2 lo rompe (aparecen las tres
    /// flores), 3 da la semilla morada directamente, 4 lleva delante del muro de raíces y 5 junto a la siguiente flor (aparecen al
    /// romper el corazón). Para recorrer el piso sin pelear: F7 invulnerable y Mayús+F3 vacía la sala (DebugCheats). Solo para probar.
    /// </summary>
    public class SecretTestKeys : MonoBehaviour
    {
        Health health;
        int flowerIndex;
        string notice = "1 corazón · 2 romper corazón · 3 semilla · 4 ir al muro · 5 ir a una flor";
        float noticeUntil = float.MaxValue;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (health == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null || !player.TryGetComponent(out health))
                    return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                health.AddPurpleHeart();
                Say("Corazón morado");
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                health.AddPurpleHeart();
                health.PurpleHearts = 0;
                Say("Corazón roto: en unos segundos salen 3 flores moradas (5 para ir a ellas)");
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                health.PurpleSeed = true;
                Say("Semilla morada");
            }
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
            {
                var wall = FindAnyObjectByType<RootWall>();
                if (wall == null)
                    Say("No hay muro de raíces en este piso");
                else
                {
                    MoveTo((Vector2)wall.transform.position + Vector2.down * 2.5f);
                    Say("Delante del muro de raíces");
                }
            }
            else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
            {
                var flowers = FindObjectsByType<PurpleFlower>(FindObjectsInactive.Include)
                    .Where(flower => flower.IsAvailable)
                    .OrderBy(flower => flower.transform.position.y).ThenBy(flower => flower.transform.position.x).ToArray();
                if (flowers.Length == 0)
                    Say("No quedan flores");
                else
                {
                    var flower = flowers[flowerIndex++ % flowers.Length];
                    MoveTo((Vector2)flower.transform.position + Vector2.down * 1.5f);
                    Say($"Junto a una flor ({(flower.gameObject.activeInHierarchy ? "visible" : "aún oculta: pulsa 2")})");
                }
            }
        }

        void MoveTo(Vector2 position)
        {
            if (health.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
            health.transform.position = position;
        }

        void Say(string text)
        {
            notice = text;
            noticeUntil = Time.unscaledTime + 5f;
        }

        void OnGUI()
        {
            if (Time.unscaledTime > noticeUntil)
                return;
            GUI.Label(new Rect(10f, Screen.height - 30f, Screen.width - 20f, 24f), notice);
        }
    }
}
