using System.Collections;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Ninfa de Meganeura (nace de los huevos de la fase 2): persigue al jugador y, cerca, avisa 0,4 s y lanza el labio (~2,5 u); si
    /// acierta, daña y lo tira hacia ella. Muere con la hoja de muerte (o la de fuego si arde). Vista en picada: sus hojas tienen
    /// una fila por dirección (<see cref="DirectionalArt"/>).
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Rigidbody2D))]
    public class MeganeuraNymph : MonoBehaviour
    {
        const float WarnSeconds = 0.4f;
        const float StrikeSeconds = 0.24f;
        const float HitAt = 0.06f;
        const float Reach = 2.6f;
        const float StrikeRange = 2.2f;
        const float Cooldown = 1.2f;
        const int StrikeDamage = 1;
        const float PullSpeed = 7f;
        const float FlashTime = 0.12f;

        [SerializeField] float speed = 3.4f;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] DirectionalSprite bodySprite;
        [SerializeField] DirectionalSprite shadowSprite;

        public enum Mode { Chase, Warn, Strike, Dead }

        public Mode CurrentMode { get; private set; } = Mode.Chase;

        Enemy enemy;
        Rigidbody2D rb;
        Transform player;
        float busyUntil;
        float nextStrike;
        float flashUntil;
        bool struck;
        bool burning;

        static bool Playing => GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        internal void Configure(SpriteRenderer newBody, DirectionalSprite newBodySprite, DirectionalSprite newShadowSprite)
        {
            bodyRenderer = newBody;
            bodySprite = newBodySprite;
            shadowSprite = newShadowSprite;
        }

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            rb = GetComponent<Rigidbody2D>();
        }

        void OnEnable()
        {
            enemy.Died += OnDied;
            enemy.Health.Damaged += OnDamaged;
            Play("Idle", 200f, true);
        }

        void OnDisable()
        {
            enemy.Died -= OnDied;
            enemy.Health.Damaged -= OnDamaged;
        }

        void Play(string animation, float milliseconds, bool loop, string fallback = null)
        {
            bodySprite?.Play($"Meganeura_Nymph_{animation}", milliseconds, loop, fallback != null ? $"Meganeura_Nymph_{fallback}" : null, restart: !loop);
            shadowSprite?.Play($"Meganeura_Nymph_Shadow_{animation}", milliseconds, loop, restart: !loop);
        }

        void SetAngle(float angle)
        {
            if (bodySprite != null)
                bodySprite.Angle = angle;
            if (shadowSprite != null)
                shadowSprite.Angle = angle;
        }

        void FixedUpdate()
        {
            if (CurrentMode == Mode.Dead)
                return;
            if (!Playing)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }

            // Confundida por las Feromonas: va a por el enemigo más cercano y su labio le hace daño a él (con Frenesí, el doble).
            var confusion = GetComponent<Primordia.Mutations.Confusion>();
            bool confused = confusion != null && confusion.Active;
            Transform aimAt = confused ? confusion.Target : player;
            if (aimAt == null)
            {
                // Confundida y sin nadie a quien atacar: se queda quieta.
                rb.linearVelocity = Vector2.zero;
                return;
            }
            Vector2 toPlayer = (Vector2)aimAt.position - rb.position;
            float distance = toPlayer.magnitude;
            switch (CurrentMode)
            {
                case Mode.Chase:
                    SetAngle(Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg);
                    if (distance <= StrikeRange && Time.time >= nextStrike)
                    {
                        rb.linearVelocity = Vector2.zero;
                        CurrentMode = Mode.Warn;
                        busyUntil = Time.time + WarnSeconds;
                        Play("Warn", 130f, false);
                    }
                    else
                    {
                        rb.linearVelocity = distance > 0.5f ? toPlayer.normalized * speed : Vector2.zero;
                        Play("Move", 80f, true);
                    }
                    break;

                case Mode.Warn:
                    rb.linearVelocity = Vector2.zero;
                    if (Time.time >= busyUntil)
                    {
                        CurrentMode = Mode.Strike;
                        struck = false;
                        busyUntil = Time.time + StrikeSeconds;
                        Play("Strike", 60f, false);
                    }
                    break;

                case Mode.Strike:
                    rb.linearVelocity = Vector2.zero;
                    if (!struck && Time.time >= busyUntil - StrikeSeconds + HitAt)
                    {
                        struck = true;
                        TryHit(toPlayer, aimAt, confused ? confusion : null);
                    }
                    if (Time.time >= busyUntil)
                    {
                        CurrentMode = Mode.Chase;
                        nextStrike = Time.time + Cooldown;
                    }
                    break;
            }
        }

        /// <summary>El labio sale disparado: si el jugador está a su alcance, daña y lo tira un poco hacia la ninfa.</summary>
        void TryHit(Vector2 toPlayer, Transform aimAt, Primordia.Mutations.Confusion confusion)
        {
            if (toPlayer.magnitude > Reach || !aimAt.TryGetComponent<Health>(out var health))
                return;
            int damage = confusion != null && confusion.Frenzy ? StrikeDamage * 2 : StrikeDamage;
            if (health.TakeDamage(damage) && aimAt.TryGetComponent<PlayerController>(out var controller))
                controller.Shove(-toPlayer.normalized * PullSpeed, 0.25f);
        }

        void OnDamaged(int _) => flashUntil = Time.time + FlashTime;

        void OnDied(Enemy dead)
        {
            burning = TryGetComponent<Burn>(out _);
            CurrentMode = Mode.Dead;
            rb.linearVelocity = Vector2.zero;
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            Play(burning ? "DeathBurn" : "Death", burning ? 110f : 110f, false, "Death");
        }

        void LateUpdate()
        {
            if (bodyRenderer != null)
                bodyRenderer.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        }
    }
}
