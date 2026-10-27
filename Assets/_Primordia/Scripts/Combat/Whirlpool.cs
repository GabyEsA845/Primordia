using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Remolino de la Corona de brazos: aparece donde se apunta, atrae hacia el centro a los enemigos que hay dentro
    /// (<see cref="IPullable"/>) y les hace daño cada cierto tiempo, con los efectos de la Boca (cuenta como golpe del arma).
    /// Con la Marea (nivel III), al acabar estalla: empuja hacia fuera a todos los de alrededor y les hace daño.
    /// El dibujo del prefab mide <see cref="DrawnRadius"/>; se escala al radio del ataque.
    /// </summary>
    public class Whirlpool : MonoBehaviour
    {
        /// <summary>Radio con el que está dibujado el remolino (48 px de diámetro a 16 px por unidad).</summary>
        public const float DrawnRadius = 1.5f;
        /// <summary>Radio con el que está dibujado el estallido de la Marea (30 px).</summary>
        public const float TideDrawnRadius = 30f / 16f;
        /// <summary>Segundos finales en que se desvanece.</summary>
        const float FadeTime = 0.3f;
        /// <summary>Segundos que deja aturdido el empujón de la Marea.</summary>
        const float TideStun = 0.35f;

        static readonly List<Whirlpool> active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        AttackData attack;
        Weapon source;
        int damage;
        int targetMask;
        float endAt;
        float nextTick;
        bool ended;
        SpriteRenderer[] renderers;
        Color[] baseColors;

        public float Radius => attack != null ? attack.AreaRadius : 0f;

        /// <summary>Arma que lo creó (la Corriente nutritiva del Lirio de mar solo atrae con los remolinos del jugador).</summary>
        public Weapon Source => source;
        public int Ticks { get; private set; }

        /// <summary>Remolinos que hay ahora.</summary>
        public static IReadOnlyList<Whirlpool> Active => active;

        /// <summary>Crea el remolino del ataque en ese punto.</summary>
        public static Whirlpool Spawn(AttackData attack, Weapon source, Vector2 point)
        {
            var go = attack.AreaPrefab != null ? Instantiate(attack.AreaPrefab, point, Quaternion.identity) : new GameObject("Whirlpool");
            go.transform.position = point;
            if (!go.TryGetComponent<Whirlpool>(out var whirlpool))
                whirlpool = go.AddComponent<Whirlpool>();
            whirlpool.Begin(attack, source);
            return whirlpool;
        }

        /// <summary>El remolino de ese arma que tiene dentro a ese objetivo, o null.</summary>
        public static Whirlpool Containing(Health target, Weapon source = null)
        {
            if (target == null)
                return null;
            foreach (var whirlpool in active)
            {
                if (whirlpool != null && (source == null || whirlpool.source == source) && whirlpool.Contains(target))
                    return whirlpool;
            }
            return null;
        }

        void Begin(AttackData newAttack, Weapon newSource)
        {
            attack = newAttack;
            source = newSource;
            damage = source != null ? source.DamageOf(attack) : attack.Damage;
            var team = source != null ? source.Team : Team.Player;
            targetMask = LayerMask.GetMask(team == Team.Player ? CombatLayers.Enemy : CombatLayers.Player);
            endAt = Time.time + attack.AreaDuration;
            // El primer daño llega enseguida, para que el clic se note.
            nextTick = Time.time + attack.AreaTick * 0.5f;
            transform.localScale = Vector3.one * (attack.AreaRadius / DrawnRadius);
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].color;
            active.Add(this);
            // Un solo bucle mientras quede algún remolino.
            Sfx.StartLoop(Sounds.ArmCrownWhirlLoop);
        }

        void OnDestroy()
        {
            active.Remove(this);
            if (active.Count == 0)
                Sfx.StopLoop(Sounds.ArmCrownWhirlLoop);
        }

        /// <summary>True si ese objetivo está dentro (basta con que asome).</summary>
        public bool Contains(Health target)
        {
            foreach (var health in Inside(Radius))
            {
                if (health == target)
                    return true;
            }
            return false;
        }

        /// <summary>Los objetivos vivos del otro equipo dentro de ese radio, una vez cada uno.</summary>
        public List<Health> Inside(float radius)
        {
            var found = new List<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, radius, targetMask))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health != null && !health.IsDead && !found.Contains(health))
                    found.Add(health);
            }
            return found;
        }

        void FixedUpdate()
        {
            if (ended || attack == null || attack.AreaPull <= 0f)
                return;

            Vector2 center = transform.position;
            foreach (var health in Inside(Radius))
            {
                var pullable = health.GetComponentInParent<IPullable>();
                if (pullable == null)
                    continue;
                Vector2 toCenter = center - (Vector2)health.transform.position;
                // En el centro ya no tira (no tiembla de un lado a otro).
                if (toCenter.magnitude > 0.25f)
                    pullable.Pull(toCenter.normalized * attack.AreaPull);
            }
        }

        void Update()
        {
            if (ended || attack == null)
                return;

            if (Time.time >= nextTick)
            {
                nextTick += attack.AreaTick;
                Tick();
            }

            float left = endAt - Time.time;
            if (left <= FadeTime)
                SetAlpha(Mathf.Clamp01(left / FadeTime));
            if (left <= 0f)
                End();
        }

        void Tick()
        {
            Ticks++;
            foreach (var health in Inside(Radius))
                Hit(health, damage);
        }

        void Hit(Health health, int amount)
        {
            if (!health.TakeDamage(amount))
                return;
            attack.ApplyStatus(health, source != null ? source.PoisonMultiplier : 1f);
            if (source != null)
                source.ReportHit(health);
        }

        /// <summary>Al acabar: con la Marea, estalla y empuja hacia fuera. Luego desaparece.</summary>
        void End()
        {
            ended = true;
            if (attack.TideRadius > 0f)
            {
                var effect = OneShotEffect.Spawn(attack.TideEffect, transform.position);
                if (effect != null)
                    effect.transform.localScale = Vector3.one * (attack.TideRadius / TideDrawnRadius);
                Sfx.Play(attack.ImpactSound);

                int tideDamage = source != null ? Mathf.CeilToInt(attack.TideDamage * source.DamageMultiplier - 0.001f) : attack.TideDamage;
                Vector2 center = transform.position;
                foreach (var health in Inside(attack.TideRadius))
                {
                    Vector2 away = (Vector2)health.transform.position - center;
                    away = away.sqrMagnitude > 0.0001f ? away.normalized : Random.insideUnitCircle.normalized;
                    var pushable = health.GetComponentInParent<IPushable>();
                    pushable?.Knockback(away * attack.TidePush, TideStun);
                    Hit(health, tideDamage);
                }
            }
            Destroy(gameObject);
        }

        void SetAlpha(float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                var color = baseColors[i];
                color.a *= alpha;
                renderers[i].color = color;
            }
        }
    }
}
