using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Nube de tinta que se queda en el sitio: aparece, se arremolina en bucle mientras dura y se disipa.
    /// Mientras está, ciega a los enemigos que hay dentro (con su marca) y, con Tinta corrosiva, les quita vida
    /// cada segundo. Va en el prefab del efecto; <see cref="InkCloudMutation"/> la configura al crearla.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InkCloudArea : MonoBehaviour
    {
        // Cada cuánto vuelve a mirar quién está dentro, y cuánto dura la ceguera que da estar dentro.
        const float CheckInterval = 0.25f;
        const float BlindWhileInside = 0.5f;

        [SerializeField] Sprite[] startFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float startFrameDuration = 0.09f;
        [SerializeField] Sprite[] loopFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float loopFrameDuration = 0.15f;
        [SerializeField] Sprite[] endFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float endFrameDuration = 0.11f;

        SpriteRenderer spriteRenderer;
        float radius = 3f;
        float duration = 3f;
        int damagePerSecond;
        GameObject blindMarker;
        float startTime;
        float nextCheck;
        float nextDamage;
        readonly HashSet<Health> damaged = new();

        public float Radius => radius;
        public float Duration => duration;
        public int DamagePerSecond => damagePerSecond;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            startTime = Time.time;
            nextDamage = Time.time + 1f;
        }

        internal void ConfigureLook(Sprite[] start, float startDuration, Sprite[] loop, float loopDuration, Sprite[] end, float endDuration)
        {
            startFrames = start;
            startFrameDuration = startDuration;
            loopFrames = loop;
            loopFrameDuration = loopDuration;
            endFrames = end;
            endFrameDuration = endDuration;
        }

        public void Configure(float newRadius, float newDuration, int newDamagePerSecond, GameObject newBlindMarker)
        {
            radius = newRadius;
            duration = newDuration;
            damagePerSecond = newDamagePerSecond;
            blindMarker = newBlindMarker;

            // Al aparecer, ciega del todo a los que alcanza; luego, mientras sigan dentro.
            ForEachEnemyInside((enemy, _) => enemy.Blind(duration, blindMarker));
            nextCheck = Time.time + CheckInterval;
        }

        void Update()
        {
            float elapsed = Time.time - startTime;
            float endLength = endFrames.Length * endFrameDuration;
            if (elapsed >= duration + endLength)
            {
                Destroy(gameObject);
                return;
            }

            ShowFrame(elapsed);

            if (elapsed >= duration)
                return;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + CheckInterval;
                ForEachEnemyInside((enemy, _) => enemy.Blind(BlindWhileInside, blindMarker));
            }

            if (damagePerSecond > 0 && Time.time >= nextDamage)
            {
                nextDamage += 1f;
                damaged.Clear();
                ForEachEnemyInside((_, health) =>
                {
                    if (health != null && damaged.Add(health))
                        health.TakeDamage(damagePerSecond);
                });
            }
        }

        /// <summary>Aparece, se mantiene en bucle hasta el final de la duración y se disipa.</summary>
        void ShowFrame(float elapsed)
        {
            Sprite[] frames;
            float local;
            float frameDuration;
            float startLength = startFrames.Length * startFrameDuration;

            if (elapsed < startLength)
            {
                frames = startFrames;
                local = elapsed;
                frameDuration = startFrameDuration;
            }
            else if (elapsed < duration || endFrames.Length == 0)
            {
                frames = loopFrames.Length > 0 ? loopFrames : startFrames;
                frameDuration = loopFrameDuration;
                local = (elapsed - startLength) % Mathf.Max(0.01f, frames.Length * frameDuration);
            }
            else
            {
                frames = endFrames;
                local = elapsed - duration;
                frameDuration = endFrameDuration;
            }

            if (frames.Length == 0)
                return;
            int frame = Mathf.Clamp(Mathf.FloorToInt(local / frameDuration), 0, frames.Length - 1);
            spriteRenderer.sprite = frames[frame];
        }

        void ForEachEnemyInside(System.Action<EnemyAI, Health> action)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent<EnemyAI>(out var enemy))
                    action(enemy, body.GetComponent<Health>());
            }
        }
    }
}
