using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Un enemigo encerrado en ámbar por la Gota: aturdido mientras dura el bloque, recibiendo daño cada segundo, y al romperse el
    /// bloque suelta esquirlas alrededor. En el nivel III el enemigo muere al instante y su cadáver queda en un bloque
    /// (<see cref="AmberCorpse"/>) que sirve de escudo; los jefes solo se aturden (nunca mueren así).
    /// </summary>
    public class AmberCocoon : MonoBehaviour
    {
        const float TickSeconds = 0.5f;
        const int SortingOrder = 23;

        Health health;
        AmberShell shell;
        AmberDropShot.Settings settings;
        float nextTick;

        public static AmberCocoon Encase(Enemy enemy, Health health, AmberDropShot.Settings settings)
        {
            bool boss = health.GetComponentInParent<IBoss>() != null;
            Vector2 center = health.GetComponentInChildren<Collider2D>() != null
                ? (Vector2)health.GetComponentInChildren<Collider2D>().bounds.center
                : (Vector2)health.transform.position;

            if (settings.kills && !boss)
            {
                // Nivel III: muere al instante y su cadáver queda en ámbar.
                AmberCorpse.Create(center, settings);
                health.Kill();
                return null;
            }

            float seconds = boss ? settings.bossSeconds : settings.stunSeconds;
            var cocoon = health.gameObject.AddComponent<AmberCocoon>();
            cocoon.health = health;
            cocoon.settings = settings;
            cocoon.nextTick = Time.time + TickSeconds;
            if (health.TryGetComponent<EnemyAI>(out var ai))
                ai.Knockback(Vector2.zero, seconds);
            cocoon.shell = AmberShell.Create(health.transform, settings.shell, seconds);
            cocoon.shell.Broken += cocoon.OnBroken;
            FrameEffect.Play("AmberStars", settings.starFrames, 0.1f, SortingOrder, null, center + Vector2.up * 0.9f);
            return cocoon;
        }

        void Update()
        {
            if (health == null || health.IsDead)
            {
                if (shell != null)
                    shell.Break();
                Destroy(this);
                return;
            }

            // Dentro del bloque, el enemigo no se mueve aunque otra cosa le mueva.
            if (health.TryGetComponent<EnemyAI>(out var ai) && !ai.IsStunned && shell != null && !shell.IsBroken)
                ai.Knockback(Vector2.zero, 0.3f);

            if (Time.time >= nextTick && settings.damagePerSecond > 0 && shell != null && !shell.IsBroken)
            {
                nextTick = Time.time + TickSeconds;
                health.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(settings.damagePerSecond * TickSeconds)));
            }
        }

        void OnBroken(Vector2 center)
        {
            AmberShards.Burst(center, settings.shardRadius, settings.shardDamage, settings.shardPush, 0f, health);
            Destroy(this);
        }
    }

    /// <summary>
    /// El cadáver de un enemigo en ámbar (Gota de ámbar, nivel III): un bloque sólido que para proyectiles, aguanta unos golpes y se
    /// rompe soltando esquirlas, o se deshace solo pasado un rato. Los proyectiles y los golpes del jugador le hacen daño como a un enemigo.
    /// </summary>
    public class AmberCorpse : MonoBehaviour
    {
        const int SortingOrder = 5;

        Health health;
        AmberShell shell;
        AmberDropShot.Settings settings;
        float until;

        public static AmberCorpse Create(Vector2 center, AmberDropShot.Settings settings)
        {
            var go = new GameObject("AmberCorpse");
            go.transform.position = center;

            // Capa Enemy: los golpes del jugador la encuentran; la sólida de siempre, para que pare proyectiles y pasos.
            var solid = go.AddComponent<CircleCollider2D>();
            solid.radius = 0.7f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var hurtbox = new GameObject("Hurtbox");
            hurtbox.transform.SetParent(go.transform, false);
            int layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            if (layer >= 0)
                hurtbox.layer = layer;
            hurtbox.AddComponent<CircleCollider2D>().radius = 0.7f;

            var corpse = go.AddComponent<AmberCorpse>();
            corpse.settings = settings;
            corpse.until = Time.time + settings.corpseSeconds;
            corpse.health = go.AddComponent<Health>();
            corpse.health.SetMax(Mathf.Max(1, settings.corpseHealth), true);
            corpse.health.Died += corpse.OnDied;

            // Para el escudo hace falta algo que mostrar: la cáscara y, delante, la silueta fósil del cadáver.
            corpse.shell = AmberShell.Create(go.transform, settings.shell, settings.corpseSeconds, new Vector2(1.7f, 2.1f), settings.fossilFrames);
            corpse.shell.Broken += corpse.OnBroken;
            FrameEffect.Play("AmberCrystals", settings.crystalFrames, 0.07f, SortingOrder + 3, null, center);
            return corpse;
        }

        void Update()
        {
            if (Time.time >= until && shell != null && !shell.IsBroken)
                shell.Break();
        }

        void OnDied()
        {
            if (shell != null)
                shell.Break();
        }

        void OnBroken(Vector2 center)
        {
            AmberShards.Burst(center, settings.shardRadius, settings.shardDamage, settings.shardPush, 0f);
            Destroy(gameObject);
        }
    }
}
