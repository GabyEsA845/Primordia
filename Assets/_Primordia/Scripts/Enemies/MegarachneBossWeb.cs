using System;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    // Se aplica despues del movimiento, sin mezclar la duracion del veneno con la de la telarana.
    [DefaultExecutionOrder(1000)]
    public class MegarachneBossWeb : MonoBehaviour, IFlammable
    {
        public const float Lifetime = 8f, SpeedFactor = 0.6f;
        // PROVISIONAL: radio de la zona que frena. Tiempos visuales del efecto aprobado.
        public const float Radius = 0.8f;
        const float AppearFrame = 0.06f, BurnFrame = 0.1f;
        static readonly HashSet<Rigidbody2D> affected = new();
        static float affectedAt = float.NegativeInfinity;
        Sprite[] appear = Array.Empty<Sprite>(), idle = Array.Empty<Sprite>(), burn = Array.Empty<Sprite>();
        SpriteRenderer sprite;
        float burnedAt;
        public float CreatedAt { get; private set; }
        public bool IsBurning { get; private set; }
        public Vector2 Position => transform.position;
        public bool CanIgnite => isActiveAndEnabled && !IsBurning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { affected.Clear(); affectedAt = float.NegativeInfinity; }

        internal void Configure(Sprite[] appearFrames, Sprite[] idleFrames, Sprite[] burnFrames)
        {
            appear = appearFrames; idle = idleFrames; burn = burnFrames;
            Show();
        }

        void Awake() { sprite = GetComponent<SpriteRenderer>(); CreatedAt = Time.time; }
        void OnEnable() => Flammables.Register(this);
        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (!CanIgnite) return;
            IsBurning = true;
            burnedAt = Time.time;
            Show();
        }

        void Update()
        {
            if (IsBurning ? Time.time - burnedAt >= Mathf.Max(1, burn.Length) * BurnFrame : Time.time - CreatedAt >= Lifetime)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            Show();
        }

        void Show()
        {
            float age = Time.time - CreatedAt;
            var frames = IsBurning ? burn : age < appear.Length * AppearFrame ? appear : idle;
            if (frames.Length == 0) return;
            int frame = IsBurning ? Mathf.FloorToInt((Time.time - burnedAt) / BurnFrame) : Mathf.FloorToInt(age / AppearFrame);
            sprite.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
        }

        void FixedUpdate()
        {
            if (IsBurning || Time.time - CreatedAt >= Lifetime) return;
            if (affectedAt != Time.fixedTime) { affectedAt = Time.fixedTime; affected.Clear(); }
            foreach (var collider in Physics2D.OverlapCircleAll(Position, Radius))
            {
                if (collider.isTrigger) continue;
                var rb = collider.attachedRigidbody;
                if (rb == null || !rb.TryGetComponent<Health>(out var health) || health.IsDead) continue;
                if (rb.TryGetComponent<PlayerController>(out var player))
                {
                    if (player.IsDodging || player.IsLunging) continue;
                }
                else if (!rb.TryGetComponent<EnemyAI>(out _)) continue;
                // Una sola reducci?n por paso aunque pise varias; al salir no queda ning?n estado residual.
                if (affected.Add(rb)) rb.linearVelocity *= SpeedFactor;
            }
        }
    }
}
