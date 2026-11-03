using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="JumpingLegsMutation"/>: convierte la esquiva en un salto. Al despegar deja de chocar con los
    /// enemigos y con los obstáculos bajos (<see cref="JumpableObstacle"/>); al caer vuelve a chocar, esparce los charcos de veneno
    /// de debajo y, según el nivel, daña a los de alrededor. En el nivel III, mantener el botón de esquiva al menos
    /// <see cref="Settings.minHold"/> segundos carga un salto: un marcador señala dónde caerás (más lejos cuanto más aguantes, hacia
    /// donde apuntas) y al soltar saltas ahí con un impacto que daña, empuja y aturde. Un toque corto es un salto normal.
    /// </summary>
    public class JumpingLegs : MonoBehaviour
    {
        /// <summary>Valores del salto en este nivel (los pone la mutación).</summary>
        public struct Settings
        {
            public int landDamage;
            public float landRadius;
            public bool charged;
            public float minHold;
            public float chargeSeconds;
            public float chargedMinDistance;
            public float chargedMaxDistance;
            public int chargedDamage;
            public float chargedRadius;
            public float chargedStun;
            public float chargedPush;
        }

        const int FxOrder = 8;
        // El arco del salto: altura a medio vuelo = 0,375 por unidad de distancia (1,5 u a 4 u), con tope.
        const float ArcPerUnit = 0.375f;
        const float ArcMax = 2.2f;
        // Los pies respecto al centro del cuerpo, donde queda la sombra.
        static readonly Vector2 FootOffset = new(0f, -0.3f);
        const float FrameSeconds = 0.06f;
        // Lo que se queda corto el marcador ante una pared, para no caer dentro de ella.
        const float WallMargin = 0.5f;

        PlayerController controller;
        Rigidbody2D body;
        Collider2D bodyCollider;
        Settings settings;
        Sprite[] takeoff, land, quake, stunned;
        LayerMask previousExclude;
        bool airborne;
        bool restoring;
        bool chargedLanding;
        bool holding;
        float pressedAt;
        JumpMarker marker;
        PlayerAnimator playerAnimator;
        Transform bodyTransform;
        Vector3 bodyBase;
        bool raised;
        Sprite[] markerSpot, markerFull, markerCorners;
        SpriteRenderer shadow;

        /// <summary>Se lanza al caer, con el punto de aterrizaje (los toques de los linajes se cuelgan de aquí).</summary>
        public event Action<Vector2> Landed;

        /// <summary>True mientras está en el aire.</summary>
        public bool IsAirborne => airborne;

        /// <summary>True mientras mantiene el botón y el marcador está a la vista (carga empezada).</summary>
        public bool IsCharging => holding && Time.time - pressedAt >= settings.minHold;

        public Settings Current => settings;

        public void Configure(PlayerController newController, Settings newSettings, Sprite[] newTakeoff, Sprite[] newLand,
            Sprite[] newQuake, Sprite[] newStunned, Sprite[] newMarkerSpot = null, Sprite[] newMarkerFull = null,
            Sprite[] newMarkerCorners = null)
        {
            Unsubscribe();
            markerSpot = newMarkerSpot;
            markerFull = newMarkerFull;
            markerCorners = newMarkerCorners;
            playerAnimator = GetComponent<PlayerAnimator>();
            controller = newController;
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            settings = newSettings;
            takeoff = newTakeoff;
            land = newLand;
            quake = newQuake;
            stunned = newStunned;
            if (controller != null)
            {
                controller.DodgeStarted += OnDodgeStarted;
                controller.DodgeEnded += OnDodgeEnded;
                controller.DodgePressed += OnPressed;
                controller.DodgeReleased += OnReleased;
            }
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (airborne)
                Land(false);
            HideMarker();
            ResetJumpVisual();
            if (controller != null)
                controller.MoveFrozen = false;
            JumpableObstacle.SetPassable(bodyCollider, false);
        }

        void Unsubscribe()
        {
            if (controller == null)
                return;
            controller.DodgeStarted -= OnDodgeStarted;
            controller.DodgeEnded -= OnDodgeEnded;
            controller.DodgePressed -= OnPressed;
            controller.DodgeReleased -= OnReleased;
        }

        // ---------- Salto cargado ----------

        void OnPressed()
        {
            if (!settings.charged || !controller.CanDodge)
                return;
            holding = true;
            pressedAt = Time.time;
        }

        void OnReleased()
        {
            if (!settings.charged || !holding)
                return;
            holding = false;
            float held = Time.time - pressedAt;
            HideMarker();
            if (held < settings.minHold)
            {
                controller.TryDodge();
                return;
            }
            var target = ChargedTarget(held);
            var delta = target - (Vector2)transform.position;
            if (controller.TryDodge(delta, delta.magnitude, JumpingLegsMutation.DurationFor(delta.magnitude)))
                chargedLanding = true;
        }

        /// <summary>Dónde caería un salto cargado soltado ahora: hacia el apuntado, parándose ante una pared.</summary>
        public Vector2 ChargedTarget(float heldSeconds)
        {
            float t = Mathf.Clamp01((heldSeconds - settings.minHold) / Mathf.Max(0.01f, settings.chargeSeconds));
            float distance = Mathf.Lerp(settings.chargedMinDistance, settings.chargedMaxDistance, t);
            Vector2 origin = transform.position;
            Vector2 direction = controller.AimDirection.sqrMagnitude > 0.0001f ? controller.AimDirection.normalized : Vector2.right;
            distance = Mathf.Min(distance, FreeDistance(origin, direction, distance));
            return origin + direction * distance;
        }

        static readonly RaycastHit2D[] rays = new RaycastHit2D[16];

        /// <summary>Hasta dónde se puede saltar en esa dirección: lo que choca con el jugador y no es un obstáculo bajo ni un enemigo.</summary>
        float FreeDistance(Vector2 origin, Vector2 direction, float distance)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer) & ~LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.Raycast(origin, direction, filter, rays, distance + WallMargin);
            float free = distance;
            for (int i = 0; i < count; i++)
            {
                var hit = rays[i];
                if (hit.collider == bodyCollider || hit.collider.GetComponent<JumpableObstacle>() != null)
                    continue;
                free = Mathf.Min(free, hit.distance - WallMargin);
            }
            return Mathf.Max(free, 0.5f);
        }

        void Update()
        {
            if (holding && !airborne && IsCharging && controller.CanAct)
            {
                if (marker == null)
                    marker = JumpMarker.Create(settings.chargedRadius, markerSpot, markerFull, markerCorners);
                float held = Time.time - pressedAt;
                marker.transform.position = ChargedTarget(held);
                marker.SetFull(held - settings.minHold >= settings.chargeSeconds);
            }
            else if (marker != null)
                HideMarker();

            if (restoring && !airborne && JumpableObstacle.Restore(bodyCollider) == 0)
                restoring = false;

            UpdateJumpVisual();
        }

        /// <summary>
        /// El salto se ve alto: el cuerpo sube en arco (más alto cuanto más lejos salta) con la pose que toca a su avance y deja una
        /// sombra en el suelo que se encoge al subir. Mientras carga, el cuerpo hace la pose de carga.
        /// </summary>
        void UpdateJumpVisual()
        {
            // Mientras carga el salto no se anda (se puede apuntar, atacar y soltar el salto).
            if (controller != null)
                controller.MoveFrozen = IsCharging && !airborne && controller.CanAct;
            if (playerAnimator == null)
                return;
            playerAnimator.JumpCharging = IsCharging && !airborne && controller.CanAct;
            if (!airborne)
            {
                ResetJumpVisual(keepCharge: true);
                return;
            }

            float t = controller.DodgeProgress;
            float height = Mathf.Min(ArcMax, ArcPerUnit * controller.DodgeLength);
            float lift = height * Mathf.Sin(Mathf.PI * t);
            playerAnimator.JumpProgress = t;
            if (bodyTransform == null && playerAnimator.Body != null)
                bodyTransform = playerAnimator.Body.transform;
            if (bodyTransform != null)
            {
                if (!raised)
                    bodyBase = bodyTransform.localPosition;
                raised = true;
                bodyTransform.localPosition = bodyBase + Vector3.up * lift;
            }

            if (shadow == null)
                shadow = JumpMarker.CreateShadow(transform);
            shadow.transform.position = (Vector2)transform.position + FootOffset;
            shadow.transform.localScale = Vector3.one * (1f - 0.45f * (height > 0f ? lift / height : 0f));
            if (playerAnimator.Body != null)
                shadow.sortingLayerID = playerAnimator.Body.sortingLayerID;
        }

        void ResetJumpVisual(bool keepCharge = false)
        {
            if (playerAnimator != null)
            {
                playerAnimator.JumpProgress = null;
                if (!keepCharge)
                    playerAnimator.JumpCharging = false;
            }
            if (raised && bodyTransform != null)
                bodyTransform.localPosition = bodyBase;
            raised = false;
            if (shadow != null)
                Destroy(shadow.gameObject);
            shadow = null;
        }

        void HideMarker()
        {
            if (marker != null)
                Destroy(marker.gameObject);
            marker = null;
        }

        // ---------- Despegue y aterrizaje ----------

        void OnDodgeStarted()
        {
            if (airborne)
                return;
            airborne = true;
            if (body != null)
            {
                previousExclude = body.excludeLayers;
                body.excludeLayers = previousExclude | LayerMask.GetMask(CombatLayers.Enemy);
            }
            JumpableObstacle.SetPassable(bodyCollider, true);
            SpriteFramePlayer.PlayOnce(takeoff, FrameSeconds, (Vector2)transform.position + Vector2.down * 0.25f, FxOrder);
        }

        void OnDodgeEnded() => Land(true);

        void Land(bool withEffects)
        {
            if (!airborne)
                return;
            airborne = false;
            if (body != null)
                body.excludeLayers = previousExclude;
            // Los obstáculos bajos vuelven a chocar en cuanto el jugador ya no los pisa (si cayó encima, un poco después).
            restoring = true;
            bool big = chargedLanding;
            chargedLanding = false;
            if (!withEffects)
                return;

            Vector2 point = transform.position;
            SpriteFramePlayer.PlayOnce(land, FrameSeconds, point + Vector2.down * 0.25f, FxOrder);
            float radius = big ? settings.chargedRadius : settings.landRadius;
            SpreadPuddles(point, radius);
            int damage = big ? settings.chargedDamage : settings.landDamage;
            float stun = big ? settings.chargedStun : 0f;
            if (damage > 0 || stun > 0f)
                Shake(point, radius, damage, stun, big ? settings.chargedPush : 0f);
            Landed?.Invoke(point);
        }

        /// <summary>El aterrizaje esparce el charco de veneno que pisa: salen dos charcos pequeños a los lados que no te dañan.</summary>
        void SpreadPuddles(Vector2 point, float radius)
        {
            PoisonPuddle nearest = null;
            float best = radius * radius;
            foreach (var puddle in PoisonPuddle.All)
            {
                float distance = (puddle.Position - point).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = puddle;
                }
            }
            if (nearest == null)
                return;
            for (int i = 0; i < 2; i++)
            {
                var side = i == 0 ? Vector2.left : Vector2.right;
                PoisonPuddle.SpawnSplat(point + side * 0.9f, 6f, 0.6f, harmsPlayer: false);
            }
        }

        static readonly Collider2D[] hits = new Collider2D[24];

        void Shake(Vector2 point, float radius, int damage, float stun, float push)
        {
            SpriteFramePlayer.PlayOnce(quake, FrameSeconds, point, FxOrder - 1, radius / 1.5f);
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            int count = Physics2D.OverlapCircle(point, radius, filter, hits);
            var seen = new HashSet<Health>();
            for (int i = 0; i < count; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health))
                    continue;
                if (damage > 0)
                    health.TakeDamage(damage);
                if (health.IsDead || stun <= 0f)
                    continue;
                Vector2 away = (Vector2)health.transform.position - point;
                var velocity = (away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up) * push;
                if (health.TryGetComponent<IPushable>(out var pushable))
                    pushable.Knockback(velocity, stun);
                if (health.TryGetComponent<EnemyAI>(out var ai))
                    ShowStars(ai, stun);
            }
        }

        void ShowStars(EnemyAI ai, float seconds)
        {
            if (stunned == null || stunned.Length == 0)
                return;
            var stars = new GameObject("StunStars");
            stars.transform.SetParent(ai.transform, false);
            stars.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var renderer = stars.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = FxOrder + 4;
            stars.AddComponent<SpriteFramePlayer>().Play(renderer, stunned, 0.1f, newLoop: true);
            Destroy(stars, seconds);
        }
    }
}
