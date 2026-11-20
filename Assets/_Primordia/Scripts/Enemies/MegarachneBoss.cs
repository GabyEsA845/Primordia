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
        public const float CableSpeed = 7f;
        // PROVISIONALES: tiempos visuales de subida y caida.
        const float ClimbSeconds = 0.4f, DropSeconds = 0.48f;
        // Tamaño visual del minijefe respecto al arte aprobado (6 u): más pequeño, para que camine sobre el cable.
        public const float BodyScale = 0.75f;
        const float SpinneretOffset = 1.5f * BodyScale;
        const float IdleMs = 180f, WalkMs = 80f, ClimbMs = 100f, DropMs = 120f;
        const float ShadowAlpha = 120f / 255f, DebugHealthFraction = 0.51f;
        const float HitRadius = 1f, ContactMargin = 0.02f;

        public enum Mode { Ground, OnCable }
        enum Travel { None, Climb, Run, Cross, Drop }
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
            if (bodyRenderer != null) bodyRenderer.transform.localScale = Vector3.one * BodyScale;
            if (shadowRenderer != null) shadowRenderer.transform.localScale = Vector3.one * BodyScale;
        }

        void OnEnable()
        {
            attackClock = 0f;
            nextAttack = AttackKind.Jump;
            attack = AttackStage.None;
            if (CableCount > 0) PlaceOnCable();
            else Health.IsInvulnerable = true;
        }
        void OnDisable()
        {
            ClearAttacks();
            if (ActiveSac != null)
            {
                ActiveSac.gameObject.SetActive(false);
                Destroy(ActiveSac.gameObject);
                ActiveSac = null;
            }
            KillAllBrood();
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
                if (keyboard != null && keyboard.f8Key.wasPressedThisFrame && ActiveSac != null) ActiveSac.Burst();
                if (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                    && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                {
                    if (keyboard.f1Key.wasPressedThisFrame) SetTestHealth();
                    if (keyboard.f2Key.wasPressedThisFrame) ForceClimb();
                    if (keyboard.f3Key.wasPressedThisFrame) ForceDrop();
                    if (keyboard.f4Key.wasPressedThisFrame) ForceAttack(AttackKind.Jump);
                    if (keyboard.f5Key.wasPressedThisFrame) ForceAttack(AttackKind.Silk);
                    if (keyboard.f6Key.wasPressedThisFrame) ForceAttack(AttackKind.Net);
                    if (keyboard.f7Key.wasPressedThisFrame) ForceAttack(AttackKind.Sac);
                }
            }
#endif
        }

        void FixedUpdate()
        {
            if (Health.IsDead) { ClearAttacks(); return; }
            float dt = Time.fixedDeltaTime;
            TickProjectiles(dt);
            TickAttack(dt);
            if (attack != AttackStage.None) return;
            if (travel != Travel.None) { TickCable(dt); return; }
            if (CurrentMode == Mode.Ground)
            {
                groundedTime += dt;
                Play(groundedTime < LandSeconds ? "Land" : "Idle", groundedTime < LandSeconds ? LandMs : IdleMs);
                if (groundedTime >= VulnerableSeconds) ForceClimb();
            }
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
            Health.IsInvulnerable = false;
            hit.isTrigger = false;
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
