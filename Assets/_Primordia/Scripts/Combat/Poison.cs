using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Daño en el tiempo: quita vida una vez por segundo, tantas veces como segundos dure.
    /// Se añade al objetivo con <see cref="Apply"/>; un veneno nuevo reinicia los golpes que quedan.
    /// Mientras dura, puede mostrar un efecto en bucle sobre el objetivo.
    /// </summary>
    public class Poison : MonoBehaviour
    {
        // Altura del efecto sobre el centro del objetivo, en unidades.
        const float EffectHeight = 0.25f;

        Health health;
        int damagePerTick;
        int ticksLeft;
        float tickTimer;
        GameObject effect;

        public int TicksLeft => ticksLeft;
        public GameObject Effect => effect;

        public static Poison Apply(Health target, int damagePerSecond, float duration, GameObject effectPrefab = null)
        {
            if (!target.TryGetComponent<Poison>(out var poison))
                poison = target.gameObject.AddComponent<Poison>();

            poison.health = target;
            poison.damagePerTick = Mathf.Max(poison.damagePerTick, damagePerSecond);
            // Se cuentan golpes enteros y no segundos con decimales, para no perder el último por redondeo.
            poison.ticksLeft = Mathf.Max(poison.ticksLeft, Mathf.Max(1, Mathf.RoundToInt(duration)));

            if (poison.effect == null && effectPrefab != null)
            {
                Vector2 position = target.transform.position + Vector3.up * EffectHeight;
                poison.effect = OneShotEffect.Spawn(effectPrefab, position, parent: target.transform);
            }

            return poison;
        }

        void Update()
        {
            if (health == null || health.IsDead)
            {
                Destroy(this);
                return;
            }

            tickTimer += Time.deltaTime;
            if (tickTimer < 1f)
                return;

            tickTimer -= 1f;
            ticksLeft--;
            health.TakeDamage(damagePerTick);

            if (ticksLeft <= 0)
                Destroy(this);
        }

        void OnDestroy()
        {
            if (effect != null)
                Destroy(effect);
        }
    }
}
