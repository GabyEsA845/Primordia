using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Camuflaje (Piel): tras unos segundos sin atacar, sin usar la habilidad y sin recibir golpes, el jugador se
    /// vuelve casi invisible y su siguiente ataque hace más daño. Los enemigos lo siguen buscando (ver
    /// <see cref="IConcealable"/>) y lo descubren si se les acerca demasiado tiempo. Atacar, usar la habilidad,
    /// recibir un golpe o ser descubierto lo deja a la vista y la cuenta vuelve a empezar. Con pánico (nivel III),
    /// matar a un enemigo con ese ataque asusta a los enemigos de la sala.
    /// </summary>
    public class Camouflage : MonoBehaviour, IConcealable
    {
        // Segundos tras atacar desde el camuflaje en los que una muerte cuenta como suya (el proyectil viaja).
        const float AmbushWindow = 1f;
        // Lo que tarda en desvanecerse o volver a verse, en segundos.
        const float FadeTime = 0.25f;

        Weapon weapon;
        Health health;
        PlayerAbility ability;
        float delay;
        float damageMultiplier;
        float hiddenAlpha;
        bool panic;
        float panicDuration;
        float panicSpeed;
        float panicRadius;
        GameObject hideEffect;
        GameObject scaredMarker;

        float exposedAt;
        float ambushUntil = -1f;
        float alpha = 1f;
        readonly List<SpriteRenderer> renderers = new();

        public bool IsConcealed { get; private set; }
        public Vector2 LastSeenPosition { get; private set; }
        public float NoticeRange { get; private set; } = 2.5f;
        public float NoticeTime { get; private set; } = 1.5f;
        public GameObject SuspectMarker { get; private set; }

        /// <summary>Veces que ha asustado a la sala (para las pruebas).</summary>
        public int Panics { get; private set; }

        public void Configure(PlayerMutations player, float newDelay, float newDamageMultiplier, float newHiddenAlpha,
            float newNoticeRange, float newNoticeTime, GameObject newSuspectMarker, GameObject newHideEffect)
        {
            Unsubscribe();
            weapon = player.Weapon;
            health = player.Health;
            ability = player.Ability;
            delay = Mathf.Max(0f, newDelay);
            damageMultiplier = newDamageMultiplier;
            hiddenAlpha = Mathf.Clamp01(newHiddenAlpha);
            NoticeRange = newNoticeRange;
            NoticeTime = newNoticeTime;
            SuspectMarker = newSuspectMarker;
            hideEffect = newHideEffect;
            exposedAt = Time.time;

            weapon.Fired += OnFired;
            weapon.HitTarget += OnHitTarget;
            health.Damaged += OnDamaged;
            if (ability != null)
                ability.Used += Expose;
        }

        /// <summary>Nivel III: matar desde el camuflaje asusta a los enemigos en ese radio.</summary>
        public void ConfigurePanic(float duration, float speedFactor, float radius, GameObject marker)
        {
            panic = true;
            panicDuration = duration;
            panicSpeed = speedFactor;
            panicRadius = radius;
            scaredMarker = marker;
        }

        void Update()
        {
            if (!IsConcealed && health != null && !health.IsDead && Time.time >= exposedAt + delay)
                Conceal();
        }

        void LateUpdate()
        {
            float target = IsConcealed ? hiddenAlpha : 1f;
            if (!IsConcealed && Mathf.Approximately(alpha, 1f))
                return;

            // Cada cuadro mientras dura: las piezas o efectos nuevos también se desvanecen.
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * (1f - hiddenAlpha) / FadeTime);
            ApplyAlpha(alpha);
        }

        void Conceal()
        {
            IsConcealed = true;
            LastSeenPosition = transform.position;
            weapon.SetDamageBonus(this, damageMultiplier);
            OneShotEffect.Spawn(hideEffect, transform.position, parent: transform);
        }

        /// <summary>Un enemigo lo ha descubierto: vuelve a verse y la cuenta empieza de nuevo.</summary>
        public void Reveal() => Expose();

        void Expose()
        {
            exposedAt = Time.time;
            if (!IsConcealed)
                return;

            IsConcealed = false;
            weapon.ClearDamageBonus(this);
        }

        void OnFired(Vector2 _)
        {
            // El daño ya se calculó con el bonus al lanzar; a partir de aquí, a la vista.
            if (IsConcealed)
                ambushUntil = Time.time + AmbushWindow;
            Expose();
        }

        void OnDamaged(int _) => Expose();

        void OnHitTarget(Health target)
        {
            // El mordisco avisa del golpe antes que del disparo, todavía camuflado.
            bool fromHiding = IsConcealed || Time.time <= ambushUntil;
            if (!panic || !fromHiding || !target.IsDead)
                return;

            ambushUntil = -1f;
            Panic();
        }

        void Panic()
        {
            Panics++;
            float radiusSqr = panicRadius * panicRadius;
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                if (ai == null || ai.GetComponent<Health>().IsDead)
                    continue;
                if (((Vector2)ai.transform.position - (Vector2)transform.position).sqrMagnitude <= radiusSqr)
                    ai.Scare(panicDuration, panicSpeed, scaredMarker);
            }
        }

        /// <summary>Transparencia de todos los sprites del jugador (cuerpo y piezas).</summary>
        void ApplyAlpha(float value)
        {
            renderers.Clear();
            GetComponentsInChildren(true, renderers);
            foreach (var sprite in renderers)
            {
                var color = sprite.color;
                color.a = value;
                sprite.color = color;
            }
        }

        void Unsubscribe()
        {
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.HitTarget -= OnHitTarget;
                weapon.ClearDamageBonus(this);
            }
            if (health != null)
                health.Damaged -= OnDamaged;
            if (ability != null)
                ability.Used -= Expose;
        }

        void OnDestroy()
        {
            Unsubscribe();
            IsConcealed = false;
            ApplyAlpha(1f);
        }
    }
}
