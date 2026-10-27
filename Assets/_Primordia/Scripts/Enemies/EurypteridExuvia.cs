using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Muda vacía del Euriptérido: queda en la sala tras la muda como cobertura sólida que para los proyectiles de los dos
    /// bandos. Se rompe a golpes: cada impacto cuenta uno, sea del daño que sea (<see cref="Hits"/>, unos 15), y se va
    /// oscureciendo; al romperse tiembla, se deshace y desaparece. No cuenta como enemigo de la sala.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EurypteridExuvia : MonoBehaviour
    {
        static readonly HashSet<Health> shells = new();
        static bool hooked;

        [SerializeField, Min(1)] int hits = 15;
        [SerializeField] SpriteRenderer sprite;
        [Tooltip("Color con el último golpe que aguanta (se oscurece y agrieta poco a poco).")]
        [SerializeField] Color worn = new(0.45f, 0.4f, 0.35f);
        [SerializeField, Min(0.05f)] float breakTime = 0.45f;

        Health health;
        float flashUntil;

        public int Hits => hits;
        public int HitsLeft => health != null ? health.Current : hits;
        public bool Broken { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            shells.Clear();
            if (hooked)
                Health.RemoveDamageModifier(OneHitEach);
            hooked = false;
        }

        internal void Configure(SpriteRenderer newSprite, int newHits)
        {
            sprite = newSprite;
            hits = Mathf.Max(1, newHits);
        }

        void Awake()
        {
            health = GetComponent<Health>();
            health.SetMax(hits, healDifference: false);
            health.ResetHealth();
            if (sprite == null)
                sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            shells.Add(health);
            if (!hooked)
            {
                Health.AddDamageModifier(OneHitEach);
                hooked = true;
            }
            health.Damaged += OnDamaged;
            health.Died += OnBroken;
        }

        void OnDisable()
        {
            shells.Remove(health);
            health.Damaged -= OnDamaged;
            health.Died -= OnBroken;
        }

        /// <summary>Cada impacto en una muda vacía cuenta uno, sea del daño que sea.</summary>
        static int OneHitEach(Health target, int amount) => amount > 0 && shells.Contains(target) ? 1 : amount;

        void OnDamaged(int _) => flashUntil = Time.time + 0.08f;

        void LateUpdate()
        {
            if (sprite == null || Broken)
                return;
            float wear = 1f - (float)health.Current / Mathf.Max(1, health.Max);
            var color = Color.Lerp(Color.white, worn, wear);
            sprite.color = Time.time < flashUntil ? Color.Lerp(color, Color.white, 0.6f) : color;
        }

        void OnBroken()
        {
            if (Broken)
                return;
            Broken = true;
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            ScreenShake.Pulse(1.5f, 0.3f);
            StartCoroutine(Crumble());
        }

        IEnumerator Crumble()
        {
            Vector3 origin = transform.position;
            var start = sprite != null ? sprite.color : Color.white;
            for (float t = 0f; t < breakTime; t += Time.deltaTime)
            {
                float k = t / breakTime;
                transform.position = origin + (Vector3)(Random.insideUnitCircle * 0.06f * (1f - k));
                if (sprite != null)
                    sprite.color = new Color(start.r, start.g, start.b, 1f - k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
