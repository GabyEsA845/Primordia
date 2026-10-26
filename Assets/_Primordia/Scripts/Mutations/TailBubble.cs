using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Burbuja de la Cola propulsora: flota en su sitio con un vaivén y revienta contra el primer enemigo que la toca,
    /// haciéndole daño y empujándolo hacia fuera. Si nadie la toca, revienta sola al acabar su tiempo.
    /// </summary>
    public class TailBubble : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] float radius = 0.35f;
        [SerializeField] GameObject popEffect;
        [Tooltip("Segundos que aturde el empujón.")]
        [SerializeField, Min(0f)] float pushStun = 0.25f;

        int damage;
        float push;
        float until;
        Vector2 origin;
        float phase;
        static readonly Collider2D[] hits = new Collider2D[8];

        public int Damage => damage;
        public bool Popped { get; private set; }

        /// <summary>Crea una burbuja (sin prefab, una vacía con su radio: así funciona también en las pruebas).</summary>
        public static TailBubble Spawn(TailBubble prefab, Vector2 position, int damage, float lifetime, float push)
        {
            var bubble = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : new GameObject("TailBubble").AddComponent<TailBubble>();
            bubble.transform.position = position;
            bubble.damage = damage;
            bubble.push = push;
            bubble.until = Time.time + lifetime;
            bubble.origin = position;
            bubble.phase = Random.value * 10f;
            return bubble;
        }

        internal void SetPopEffect(GameObject effect) => popEffect = effect;

        void Update()
        {
            if (Popped)
                return;
            // Vaivén suave en su sitio.
            transform.position = origin + new Vector2(Mathf.Sin((Time.time + phase) * 2.1f) * 0.06f, Mathf.Sin((Time.time + phase) * 3f) * 0.08f);

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, hits);
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                Hit(health);
                return;
            }

            if (Time.time >= until)
                Pop();
        }

        void Hit(Health target)
        {
            target.TakeDamage(damage);
            if (target.TryGetComponent<IPushable>(out var pushable))
            {
                Vector2 away = (Vector2)target.transform.position - (Vector2)transform.position;
                pushable.Knockback((away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up) * push, pushStun);
            }
            Pop();
        }

        void Pop()
        {
            Popped = true;
            OneShotEffect.Spawn(popEffect, transform.position);
            Destroy(gameObject);
        }
    }
}
