using Primordia.Audio;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Enemies
{
    [RequireComponent(typeof(Enemy), typeof(BossDamageLimiter), typeof(CircleCollider2D))]
    public partial class MegarachneBoss : MonoBehaviour, IBoss, IPhasedBoss
    {
        public const int MaxHealth = 150;
        public const float CableSpeed = 7f;
        // PROVISIONALES: tiempos visuales de subida y caida.
        const float ClimbSeconds = 0.4f, DropSeconds = 0.48f;
        // Tamaño visual del minijefe respecto al arte aprobado (6 u): más pequeño, para que camine sobre el cable.
        public const float BodyScale = 0.75f;
        const float SpinneretOffset = 1.5f * BodyScale;
        const float IdleMs = 180f, WalkMs = 80f, ClimbMs = 100f, DropMs = 120f;
        const float ShadowAlpha = 120f / 255f, DebugHealthFraction = 0.5f;
        const float HitRadius = 1f, ContactMargin = 0.02f;

        public enum Mode { Ground, OnCable, Falling, Stunned, Recovering, Enraging }
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
        public BossBarStyle BarStyle => CurrentMode == Mode.OnCable || IsEntering || IsEnraging ? BossBarStyle.Immune : BossBarStyle.Normal;
        // El enfado marca la fase de la barra: una muesca al 50 % de la vida.
        public int PhaseIndex => rageTriggered ? 1 : 0;
        public float[] PhaseThresholds { get; } = { 0.5f };
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
            MethaneSac.BurstStarted += OnFireSacBurst;
            Health.Changed += CheckRageHealth;
            Health.Died += StartDeath;
            attackClock = 0f;
            nextAttack = AttackKind.Jump;
            attack = AttackStage.None;
            if (CableCount > 0) PlaceOnCable();
            else Health.IsInvulnerable = true;
            BeginIntroIfAny();
            // Sin presentación (pruebas), la barra de vida aparece desde que la sala se activa.
            if (!IsEntering && !deathStarted) ActiveBoss.Set(this);
        }
        void OnDisable()
        {
            MethaneSac.BurstStarted -= OnFireSacBurst;
            Health.Changed -= CheckRageHealth;
            Health.Died -= StartDeath;
            ActiveBoss.Clear(this);
            RestoreRageVisibility();
            fireHitPending = false;
            if (stunEffect != null) stunEffect.enabled = false;
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
                    if (keyboard.f8Key.wasPressedThisFrame) KillForTest();
                    if (keyboard.f9Key.wasPressedThisFrame) IgniteOccupiedCable();
                    if (keyboard.f10Key.wasPressedThisFrame) IgniteNearPlayer();
                    if (keyboard.f11Key.wasPressedThisFrame) ForceRage();
                    if (keyboard.f12Key.wasPressedThisFrame) IgniteNest();
                }
            }
#endif
        }

        // Sonido del movimiento: el bucle del cable o el del suelo (excluyentes), y subir / bajar del cable.
        Vector2 lastSoundPosition;
        Travel lastSoundTravel;

        void UpdateMovementSound()
        {
            Vector2 position = body.position;
            bool moved = (position - lastSoundPosition).sqrMagnitude > 0.00001f;
            lastSoundPosition = position;

            bool onCable = CurrentMode == Mode.OnCable && travel is Travel.Run or Travel.Cross && attack != AttackStage.JumpFall;
            bool onGround = CurrentMode == Mode.Ground && travel == Travel.None && !IsEnraging && !IsAttacking && moved;
            if (onCable)
            {
                // Al enfadarse corre ×1,4 más rápido: el bucle suena algo más agudo.
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StartLoop(Sounds.BossMegarachneCableRunLoop, 1f, IsRage ? 1.1f : 1f);
            }
            else
                Sfx.StopLoop(Sounds.BossMegarachneCableRunLoop);
            if (onGround)
                Sfx.StartLoop(Sounds.BossMegarachneWalkLoop);
            else
                Sfx.StopLoop(Sounds.BossMegarachneWalkLoop);

            if (travel != lastSoundTravel)
            {
                if (travel == Travel.Climb)
                    Sfx.Play(Sounds.BossMegarachneClimb);
                else if (travel == Travel.Drop)
                    Sfx.Play(Sounds.BossMegarachneDrop);
                lastSoundTravel = travel;
            }
        }

        void StopMovementSound()
        {
            Sfx.StopLoop(Sounds.BossMegarachneCableRunLoop);
            Sfx.StopLoop(Sounds.BossMegarachneWalkLoop);
            Sfx.StopLoop(Sounds.BossMegarachneCableFuseLoop);
            cableFusing = false;
        }

        void FixedUpdate()
        {
            if (Health.IsDead) { ClearAttacks(); StopMovementSound(); return; }
            float dt = Time.fixedDeltaTime;
            UpdateMovementSound();
            if (TickEntry(dt)) return;
            CheckRageHealth(Health.Current, Health.Max);
            TickNest(dt);
            if (TickRage(dt)) { TickFire(dt); return; }
            TickProjectiles(dt);
            TickFire(dt);
            if (IsAttacking || !fireHitPending) TickAttack(dt);
            if (IsAttacking || TickFireReaction(dt)) return;
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
            if (animation == "Walk" || animation == "Climb") milliseconds /= MovementMultiplier;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            if (bodySprite != null)
            {
                bodySprite.Angle = angle;
                bodySprite.Play("Megarachne_" + animation + (IsRage ? "_Rage" : ""), milliseconds, true);
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

        // Atajo de prueba: la mata de golpe para ver la muerte y la recompensa.
        void KillForTest()
        {
            Health.IsInvulnerable = false;
            Health.TakeDamage(Health.Max);
        }

        internal void SetTestHealth()
        {
            // El atajo deja la vida justo en el umbral provisional de M5.
            int desired = Mathf.CeilToInt(MaxHealth * DebugHealthFraction);
            Health.SetMax(desired, false);
            Health.ResetHealth();
            Health.SetMax(MaxHealth, false);
        }
    }
}
