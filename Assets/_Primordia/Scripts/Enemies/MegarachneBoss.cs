using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Enemies
{
    [RequireComponent(typeof(Enemy), typeof(BossDamageLimiter), typeof(CircleCollider2D))]
    public partial class MegarachneBoss : MonoBehaviour, IBoss
    {
        public const int MaxHealth = 150;
        // PROVISIONALES: movimiento del subpaso M1, sin ataques.
        public const float GroundSpeed = 5f, CableSpeed = 7f, GroundSeconds = 6f;
        const float ClimbSeconds = 0.4f, DropSeconds = 0.48f;
        const float SpawnDistance = 4f, SpinneretOffset = 1.5f;
        const float IdleMs = 180f, WalkMs = 80f, ClimbMs = 100f, DropMs = 120f;
        const float ShadowAlpha = 120f / 255f, DebugHealthFraction = 0.51f;
        const float HitRadius = 1f, ContactMargin = 0.02f;

        public enum Mode { Ground, OnCable }
        enum Travel { None, Approach, Climb, Run, Drop }
        [SerializeField] DirectionalSprite bodySprite, shadowSprite;
        [SerializeField] SpriteRenderer bodyRenderer, shadowRenderer;
        [SerializeField] bool testKeys;
        Rigidbody2D body;
        CircleCollider2D hit;
        Transform target;
        Transform arena;
        float groundedTime, travelTime;
        Travel travel;
        Vector2 facing = Vector2.down;
        Vector2? detour;
        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        public Mode CurrentMode { get; private set; }
        public Health Health { get; private set; }
        public string DisplayName => "Megarachne";
        public BossBarStyle BarStyle => CurrentMode == Mode.OnCable ? BossBarStyle.Immune : BossBarStyle.Normal;
        public Vector2 ArenaCenter => arena != null ? (Vector2)arena.position : Vector2.zero;
        public Transform Target { get => target; set => target = value; }
        public float VisualHeight { get; private set; }

        internal void Configure(DirectionalSprite sprite, DirectionalSprite shadow, bool keys)
        {
            bodySprite = sprite;
            shadowSprite = shadow;
            bodyRenderer = sprite.GetComponent<SpriteRenderer>();
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            testKeys = keys;
        }

        void Awake()
        {
            Health = GetComponent<Health>();
            Health.SetMax(MaxHealth, false);
            Health.ResetHealth();
            Health.InvulnerabilityAfterHit = 0f;
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            hit = GetComponent<CircleCollider2D>();
            hit.radius = HitRadius;
            GetComponent<Enemy>().DropsBiomass = false;
        }

        void OnEnable() => Land();
        void OnDisable()
        {
            if (Health != null) Health.IsInvulnerable = false;
            if (thread != null) thread.enabled = false;
        }

        public void PlayIntroPose(float hold) { }
        public void BeginFight() { }

        void Update()
        {
            if (Health.IsDead) return;
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (testKeys && gameObject.scene.name == "Test_BossMegarachne")
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                    && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                {
                    if (keyboard.f1Key.wasPressedThisFrame) SetTestHealth();
                    if (keyboard.f2Key.wasPressedThisFrame) ForceClimb();
                    if (keyboard.f3Key.wasPressedThisFrame) ForceDrop();
                }
            }
#endif
        }

        void FixedUpdate()
        {
            if (Health.IsDead) return;
            float dt = Time.fixedDeltaTime;
            if (travel != Travel.None) { TickCable(dt); return; }
            groundedTime += dt;
            if (groundedTime >= GroundSeconds && ForceClimb()) return;
            if (target != null)
            {
                Vector2 delta = (Vector2)target.position - body.position;
                MoveGround(delta, GroundSpeed * dt);
                Play(delta.sqrMagnitude > ContactMargin ? "Walk" : "Idle", delta.sqrMagnitude > ContactMargin ? WalkMs : IdleMs);
            }
            else Play("Idle", IdleMs);
        }

        void MoveGround(Vector2 delta, float distance)
        {
            if (delta.sqrMagnitude < ContactMargin * ContactMargin) return;
            Vector2 destination = body.position + delta;
            float direct = GroundCast(delta.normalized, delta.magnitude, out var obstacle);
            if (direct >= delta.magnitude || obstacle != null && obstacle.transform == target) detour = null;
            if (detour.HasValue && Vector2.Distance(body.position, detour.Value) <= ContactMargin)
                detour = null;
            if (!detour.HasValue && obstacle != null && obstacle.transform != target)
            {
                // Conserva la esquina elegida para no alternar de lado al llegar al tronco.
                var bounds = obstacle.bounds;
                bounds.Expand((HitRadius + ContactMargin * 2f) * 2f);
                float best = float.PositiveInfinity;
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                    {
                        Vector2 corner = new Vector2(x == 0 ? bounds.min.x : bounds.max.x, y == 0 ? bounds.min.y : bounds.max.y);
                        Vector2 step = corner - body.position;
                        if (step.magnitude <= ContactMargin) continue;
                        if (GroundCast(step.normalized, step.magnitude, out _) < step.magnitude) continue;
                        float cost = step.magnitude + Vector2.Distance(corner, destination);
                        if (cost < best) { best = cost; detour = corner; }
                    }
            }
            if (detour.HasValue) delta = detour.Value - body.position;
            facing = delta.normalized;
            distance = Mathf.Min(distance, delta.magnitude);
            float allowed = GroundCast(facing, distance, out _);
            Vector2 movement = facing * allowed;
            if (allowed < distance)
            {
                // Desliza por un eje libre sin atravesar el collider.
                Vector2 horizontal = new Vector2(facing.x, 0f).normalized;
                Vector2 vertical = new Vector2(0f, facing.y).normalized;
                float dx = horizontal == Vector2.zero ? 0f : GroundCast(horizontal, distance, out _);
                float dy = vertical == Vector2.zero ? 0f : GroundCast(vertical, distance, out _);
                if (dx > allowed && dx >= dy) movement = horizontal * dx;
                else if (dy > allowed) movement = vertical * dy;
            }
            body.MovePosition(body.position + movement);
        }

        float GroundCast(Vector2 direction, float distance, out Collider2D obstacle)
        {
            obstacle = null;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            int count = body.Cast(direction, filter, hits, distance + ContactMargin);
            for (int i = 0; i < count; i++)
            {
                var other = hits[i].collider;
                if (other == null || other.isTrigger || other.attachedRigidbody == body || other.transform.IsChildOf(transform)) continue;
                float available = Mathf.Max(0f, hits[i].distance - ContactMargin);
                if (available < distance) { distance = available; obstacle = other; }
            }
            return distance;
        }

        void Play(string animation, float milliseconds)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            if (bodySprite != null)
            {
                bodySprite.Angle = angle;
                bodySprite.Play("Megarachne_" + animation, milliseconds, true);
            }
            if (shadowSprite != null)
            {
                shadowSprite.Angle = angle;
                shadowSprite.Play("Megarachne_Shadow_" + animation, milliseconds, true);
            }
        }

        void SetHeight(float height)
        {
            VisualHeight = height;
            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localPosition = Vector3.up * height;
                bodyRenderer.sortingOrder = height > 0f ? 30 : 5;
            }
            if (shadowRenderer != null) shadowRenderer.color = new Color(1f, 1f, 1f, ShadowAlpha);
            if (hit != null) hit.offset = Vector2.up * height;
        }

        void Land()
        {
            CurrentMode = Mode.Ground;
            travel = Travel.None;
            groundedTime = 0f;
            detour = null;
            Health.IsInvulnerable = false;
            SetHeight(0f);
            if (thread != null) thread.enabled = false;
            Play("Idle", IdleMs);
        }

        internal void SetTestHealth()
        {
            // La vida es entera: 77 PV es el entero más próximo por encima del 51 %.
            int desired = Mathf.CeilToInt(MaxHealth * DebugHealthFraction);
            Health.SetMax(desired, false);
            Health.ResetHealth();
            Health.SetMax(MaxHealth, false);
        }
    }
}
