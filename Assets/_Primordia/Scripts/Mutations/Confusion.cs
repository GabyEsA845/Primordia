using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Enemigo confundido por las Feromonas: durante unos segundos deja de ir a por el jugador y ataca al enemigo más cercano. Lo
    /// persigue (o le dispara) con su comportamiento de siempre porque la IA toma a ese enemigo como objetivo
    /// (<see cref="EnemyAI.OverrideTarget"/>); el daño se lo hace este componente: un mordisco al tocarlo (el daño por contacto del
    /// enemigo, o el de su ataque) y sus proyectiles, que no chocan con los enemigos, los intercepta aquí. Con el Frenesí el daño entre
    /// ellos es doble. Los jefes no se confunden. Ver <see cref="PheromonesMutation"/>.
    /// </summary>
    public class Confusion : MonoBehaviour
    {
        const float SearchEvery = 0.25f;
        const float SearchRange = 12f;
        const float BiteInterval = 0.8f;
        const float BiteMargin = 0.4f;
        const float ProjectileReach = 0.3f;
        const int FxOrder = 21;

        EnemyAI ai;
        Health health;
        Weapon weapon;
        ContactDamage contact;
        float until;
        bool frenzy;
        Transform target;
        float nextSearch;
        float nextBite;
        GameObject auraObject;
        GameObject markObject;

        /// <summary>True mientras sigue confundido.</summary>
        public bool Active => Time.time < until;

        public bool Frenzy => frenzy;

        /// <summary>A quién ataca ahora.</summary>
        public Transform Target => target;

        /// <summary>True si ese enemigo está confundido ahora.</summary>
        public static bool IsConfused(Component target) =>
            target != null && target.GetComponentInParent<Confusion>() is { Active: true };

        /// <summary>
        /// Confunde a ese enemigo durante unos segundos (si ya lo estaba, se renueva). Devuelve null si no se puede: no es un enemigo
        /// con IA, ya murió o es un jefe.
        /// </summary>
        public static Confusion Apply(Health target, float seconds, bool frenzy, Sprite[] aura, Sprite[] mark, Sprite[] frenzyMark)
        {
            if (target == null || target.IsDead || !target.TryGetComponent<EnemyAI>(out var enemyAI)
                || target.GetComponentInParent<IBoss>() != null)
                return null;

            var confusion = target.GetComponent<Confusion>();
            if (confusion == null)
            {
                confusion = target.gameObject.AddComponent<Confusion>();
                confusion.ai = enemyAI;
                confusion.health = target;
                confusion.weapon = target.GetComponent<Weapon>();
                confusion.contact = target.GetComponent<ContactDamage>();
            }
            confusion.until = Mathf.Max(confusion.until, Time.time + seconds);
            confusion.frenzy |= frenzy;
            confusion.Begin(aura, frenzy && frenzyMark != null && frenzyMark.Length > 0 ? frenzyMark : mark);
            return confusion;
        }

        void Begin(Sprite[] aura, Sprite[] mark)
        {
            ai.IsConfused = true;
            ai.Alert();
            nextSearch = 0f;
            auraObject ??= LoopFx("ConfusionAura", aura, Vector2.zero);
            markObject ??= LoopFx("ConfusionMark", mark, new Vector2(0f, 0.7f));
        }

        void OnDestroy() => End();

        void Update()
        {
            if (health == null || health.IsDead || !Active)
            {
                Destroy(this);
                return;
            }

            if (Time.time >= nextSearch || target == null || !target.gameObject.activeInHierarchy)
            {
                nextSearch = Time.time + SearchEvery;
                target = NearestEnemy();
                ai.OverrideTarget = target;
            }

            Bite();
            InterceptProjectiles();
        }

        void End()
        {
            if (ai != null)
            {
                ai.IsConfused = false;
                ai.OverrideTarget = null;
            }
            if (auraObject != null)
                Destroy(auraObject);
            if (markObject != null)
                Destroy(markObject);
        }

        /// <summary>El enemigo vivo más cercano que no sea él (los demás confundidos también valen).</summary>
        Transform NearestEnemy()
        {
            Transform best = null;
            float bestDistance = SearchRange * SearchRange;
            foreach (var enemy in FindObjectsByType<Enemy>())
            {
                if (enemy.gameObject == gameObject || enemy.Health == null || enemy.Health.IsDead || enemy.TryGetComponent<ZombieAlly>(out _))
                    continue;
                float distance = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy.transform;
                }
            }
            return best;
        }

        int HitDamage()
        {
            int damage = contact != null && contact.Damage > 0 ? contact.Damage
                : weapon != null && weapon.Attack != null ? weapon.Attack.Damage : 1;
            damage = Mathf.Max(1, damage);
            return frenzy ? damage * 2 : damage;
        }

        /// <summary>Si toca al objetivo, le muerde con el daño de su ataque.</summary>
        void Bite()
        {
            if (target == null || Time.time < nextBite)
                return;
            float reach = ai.BodyRadius + 0.45f + BiteMargin;
            if (((Vector2)target.position - (Vector2)transform.position).sqrMagnitude > reach * reach)
                return;
            var victim = target.GetComponent<Health>();
            if (victim == null || victim.IsDead)
                return;
            nextBite = Time.time + BiteInterval;
            victim.TakeDamage(HitDamage());
        }

        static readonly Collider2D[] hits = new Collider2D[8];

        /// <summary>Los proyectiles que ha disparado este enemigo dañan al primer otro enemigo que tocan.</summary>
        void InterceptProjectiles()
        {
            if (weapon == null)
                return;
            int enemyLayer = CombatLayers.ProjectileLayer(Team.Enemy);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            foreach (var projectile in new List<Projectile>(Projectile.Active))
            {
                if (projectile == null || projectile.Source != weapon || projectile.gameObject.layer != enemyLayer)
                    continue;
                int count = Physics2D.OverlapCircle(projectile.transform.position, ProjectileReach, filter, hits);
                for (int i = 0; i < count; i++)
                {
                    var victim = hits[i].GetComponentInParent<Health>();
                    if (victim == null || victim == health || victim.IsDead || victim.GetComponentInParent<Enemy>() == null)
                        continue;
                    victim.TakeDamage(frenzy ? projectile.Damage * 2 : projectile.Damage);
                    projectile.Despawn();
                    break;
                }
            }
        }

        GameObject LoopFx(string fxName, Sprite[] frames, Vector2 offset)
        {
            if (frames == null || frames.Length == 0)
                return null;
            var obj = new GameObject(fxName);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = offset;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = FxOrder;
            obj.AddComponent<SpriteFramePlayer>().Play(renderer, frames, 0.1f, newLoop: true);
            return obj;
        }
    }
}
