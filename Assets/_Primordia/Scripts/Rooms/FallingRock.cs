using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Lo que reacciona a su manera al recibir una piedra del techo (el Euriptérido: daño y aturdido).</summary>
    public interface IFallingRockTarget
    {
        void HitByFallingRock(int damage, float stunDuration);
    }

    /// <summary>
    /// Piedra que cae del techo en el derrumbe de la cueva del Euriptérido: primero una sombra con borde rojo que crece donde
    /// caerá; al final de la espera la piedra baja desde arriba de la pantalla y, al tocar el suelo, quita medio corazón al
    /// jugador que esté debajo y daña y aturde a los enemigos (el jefe incluido: meterlo debajo es una táctica).
    /// </summary>
    public class FallingRock : MonoBehaviour
    {
        public const int ShadowOrder = -12;
        public const int RockOrder = 22;
        public const float DefaultWarning = 0.8f;
        public const float DefaultRadius = 0.9f;

        [SerializeField] SpriteRenderer shadow;
        [SerializeField] SpriteRenderer rock;
        [SerializeField] Sprite[] shadowFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] rockFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] impactFrames = System.Array.Empty<Sprite>();
        [Tooltip("Segundos desde que aparece la sombra hasta que la piedra toca el suelo.")]
        [SerializeField, Min(0.1f)] float warning = DefaultWarning;
        [Tooltip("Lo que tarda la piedra en bajar, al final de la espera.")]
        [SerializeField, Min(0.05f)] float fallTime = 0.35f;
        [Tooltip("Desde qué altura (unidades) baja la piedra.")]
        [SerializeField, Min(0f)] float fallHeight = 9f;
        [SerializeField, Min(0.1f)] float radius = DefaultRadius;
        [Tooltip("Daño al jugador (1 = medio corazón).")]
        [SerializeField, Min(0)] int playerDamage = 1;
        [Tooltip("Daño a los enemigos y al jefe.")]
        [SerializeField, Min(0)] int enemyDamage = 8;
        [SerializeField, Min(0f)] float enemyStun = 1f;

        float elapsed;
        bool landed;

        public float Warning => warning;
        public float Radius => radius;
        public bool Landed => landed;

        /// <summary>Crea una piedra que caerá en <paramref name="position"/> dentro de <see cref="Warning"/> s.</summary>
        public static FallingRock Spawn(Transform parent, Vector2 position, Sprite[] shadowFrames, Sprite[] rockFrames,
            Sprite[] impactFrames, float warning = DefaultWarning)
        {
            var go = new GameObject("FallingRock");
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var falling = go.AddComponent<FallingRock>();
            falling.shadowFrames = shadowFrames ?? System.Array.Empty<Sprite>();
            falling.rockFrames = rockFrames ?? System.Array.Empty<Sprite>();
            falling.impactFrames = impactFrames ?? System.Array.Empty<Sprite>();
            falling.warning = Mathf.Max(0.1f, warning);
            falling.fallTime = Mathf.Min(falling.fallTime, falling.warning);

            falling.shadow = go.AddComponent<SpriteRenderer>();
            falling.shadow.sortingOrder = ShadowOrder;
            falling.shadow.sprite = falling.shadowFrames.Length > 0 ? falling.shadowFrames[0] : null;

            var rockObject = new GameObject("Rock");
            rockObject.transform.SetParent(go.transform, false);
            falling.rock = rockObject.AddComponent<SpriteRenderer>();
            falling.rock.sortingOrder = RockOrder;
            falling.rock.enabled = false;
            return falling;
        }

        void Update()
        {
            if (landed)
                return;
            elapsed += Time.deltaTime;

            if (shadow != null && shadowFrames.Length > 0)
                shadow.sprite = shadowFrames[Mathf.Min(shadowFrames.Length - 1, Mathf.FloorToInt(elapsed / warning * shadowFrames.Length))];

            float fallStart = warning - fallTime;
            if (rock != null && elapsed >= fallStart)
            {
                float t = Mathf.Clamp01((elapsed - fallStart) / fallTime);
                rock.enabled = rockFrames.Length > 0;
                if (rockFrames.Length > 0)
                    rock.sprite = rockFrames[Mathf.FloorToInt(elapsed / 0.08f) % rockFrames.Length];
                // Acelera al caer.
                rock.transform.localPosition = Vector3.up * (fallHeight * (1f - t * t));
            }

            if (elapsed >= warning)
                Land();
        }

        void Land()
        {
            landed = true;
            Vector2 center = transform.position;
            var hit = new HashSet<Health>();
            foreach (var collider in Physics2D.OverlapCircleAll(center, radius))
            {
                var health = collider.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !hit.Add(health))
                    continue;
                if (health.CompareTag("Player"))
                {
                    health.TakeDamage(playerDamage);
                }
                else if (health.TryGetComponent<IFallingRockTarget>(out var target))
                {
                    target.HitByFallingRock(enemyDamage, enemyStun);
                }
                else
                {
                    health.TakeDamage(enemyDamage);
                    if (health.TryGetComponent<EnemyAI>(out var ai))
                        ai.Knockback(Vector2.zero, enemyStun);
                }
            }

            ScreenShake.Pulse(2f, 0.25f);
            FrameEffect.Play("RockImpact", impactFrames, 0.07f, RockOrder - 1, null, center);
            Destroy(gameObject);
        }
    }
}
