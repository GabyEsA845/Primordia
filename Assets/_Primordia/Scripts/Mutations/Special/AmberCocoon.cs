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
        // La formación empieza en el cuadro 2 de la salpicadura (2 × 60 ms).
        internal const float FormDelay = 0.12f;
        // Alto (en unidades) desde el que el enemigo recibe el bloque grande, el de las plantas; los jefes siempre llevan el normal.
        const float LargeHeight = 2.4f;

        Health health;
        AmberShell shell;
        AmberDropShot.Settings settings;
        float nextTick;
        // Jefes: sin bloque, sin daño y sin esquirlas; solo aturdidos con los destellos sobre la cabeza.
        bool stunOnly;

        /// <summary>El bloque grande (plantas) para lo que mide tanto de alto; el normal para el resto.</summary>
        internal static bool UsesLargeBlock(Health health)
        {
            if (health.GetComponentInParent<IBoss>() != null)
                return false;
            Bounds bounds = default;
            bool any = false;
            foreach (var renderer in health.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow"))
                    continue;
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }
            return any && bounds.size.y >= LargeHeight;
        }

        /// <summary>Arte solo con las estrellas de aturdido, colocadas sobre la cabeza del jefe (lo más alto de sus colliders).</summary>
        static AmberShellArt StarsOnly(Health boss, AmberShellArt source)
        {
            float feet = AmberShell.FeetOf(boss.transform).y;
            float top = feet;
            foreach (var collider in boss.GetComponentsInChildren<Collider2D>())
            {
                if (collider.enabled)
                    top = Mathf.Max(top, collider.bounds.max.y);
            }
            // Las estrellas apoyan su base en el anclaje: un poco por debajo del borde de arriba, como sobre el bloque.
            return new AmberShellArt { stars = source?.stars, starsRise = top - feet + 0.1f };
        }

        public static AmberCocoon Encase(Enemy enemy, Health health, AmberDropShot.Settings settings)
        {
            bool boss = health.GetComponentInParent<IBoss>() != null;
            bool large = UsesLargeBlock(health);

            if (settings.kills && !boss)
            {
                // Nivel III: muere al instante y su cadáver queda en ámbar.
                AmberCorpse.Create(health, settings, large);
                return null;
            }

            float seconds = boss ? settings.bossSeconds : settings.stunSeconds;
            var art = large ? settings.shellLarge : settings.shell;
            if (boss)
                art = StarsOnly(health, settings.shell);
            var cocoon = health.gameObject.AddComponent<AmberCocoon>();
            cocoon.health = health;
            cocoon.settings = settings;
            cocoon.nextTick = Time.time + TickSeconds;
            if (health.TryGetComponent<EnemyAI>(out var ai))
                ai.Knockback(Vector2.zero, seconds);
            // El aturdimiento va de la salpicadura a la rotura: lo que sobra tras formarse es el reposo.
            cocoon.stunOnly = boss;
            float hold = Mathf.Max(0.2f, seconds - (boss ? 0f : FormDelay) - (art?.FormSeconds ?? 0f));
            cocoon.shell = AmberShell.Create(health.transform, art, hold, AmberShell.Anchor.Feet, boss ? 0f : FormDelay);
            cocoon.shell.CrackByTime = !boss;
            cocoon.shell.Broken += cocoon.OnBroken;
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

            if (!stunOnly && Time.time >= nextTick && settings.damagePerSecond > 0 && shell != null && !shell.IsBroken)
            {
                nextTick = Time.time + TickSeconds;
                health.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(settings.damagePerSecond * TickSeconds)));
            }
        }

        void OnBroken(Vector2 center)
        {
            if (!stunOnly)
                AmberShards.Burst(center, settings.shardRadius, settings.shardDamage, settings.shardPush, 0f, health);
            Destroy(this);
        }
    }

    /// <summary>
    /// El cadáver de un enemigo en ámbar (Gota de ámbar, nivel III): un bloque sólido en ámbar antiguo con el enemigo fosilizado dentro,
    /// que para proyectiles, aguanta unos golpes (las grietas avanzan con ellos) y se rompe soltando esquirlas, o se deshace solo
    /// pasado un rato. Los proyectiles y los golpes del jugador le hacen daño como a un enemigo.
    /// </summary>
    public class AmberCorpse : MonoBehaviour
    {
        // El enemigo se fosiliza desde el cuadro 5 de la formación (350 ms) durante ~0,5 s, hasta el 75 % del color fósil.
        const float FossilStart = 0.35f;
        const float FossilSeconds = 0.5f;
        const float FossilStrength = 0.75f;
        static readonly Color FossilColor = new(0x3a / 255f, 0x1a / 255f, 0x08 / 255f, 1f);
        // Orden de dibujo del fósil: el bloque pone su capa de atrás en 9 y la de delante en 12.
        const int FossilOrder = 11;

        Health health;
        AmberShell shell;
        AmberDropShot.Settings settings;
        float until;
        readonly System.Collections.Generic.List<SpriteRenderer> fossil = new();

        /// <summary>Lo que se ve del enemigo fosilizado (para las pruebas).</summary>
        public int FossilPieces => fossil.Count;

        public static AmberCorpse Create(Health victim, AmberDropShot.Settings settings, bool large)
        {
            Vector2 feet = AmberShell.FeetOf(victim.transform);
            var go = new GameObject("AmberCorpse");
            go.transform.position = feet;

            // Capa Enemy: los golpes del jugador la encuentran; la sólida de siempre, para que pare proyectiles y pasos.
            var solid = go.AddComponent<CircleCollider2D>();
            solid.radius = 0.7f;
            solid.offset = Vector2.up * 0.7f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var hurtbox = new GameObject("Hurtbox");
            hurtbox.transform.SetParent(go.transform, false);
            int layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            if (layer >= 0)
                hurtbox.layer = layer;
            var hurtCircle = hurtbox.AddComponent<CircleCollider2D>();
            hurtCircle.radius = 0.7f;
            hurtCircle.offset = Vector2.up * 0.7f;

            var corpse = go.AddComponent<AmberCorpse>();
            corpse.settings = settings;
            corpse.until = Time.time + settings.corpseSeconds;
            corpse.health = go.AddComponent<Health>();
            corpse.health.SetMax(Mathf.Max(1, settings.corpseHealth), true);
            corpse.health.Died += corpse.OnDied;
            corpse.health.Changed += corpse.OnHealthChanged;

            // El enemigo se queda quieto dentro del bloque como un fósil: se copia su dibujo y se le da por muerto.
            corpse.Snapshot(victim);
            victim.Kill();

            var art = large ? settings.shellIIILarge : settings.shellIII;
            float hold = Mathf.Max(0.2f, settings.corpseSeconds - AmberCocoon.FormDelay - (art?.FormSeconds ?? 0f));
            corpse.shell = AmberShell.Create(go.transform, art, hold, AmberShell.Anchor.Feet, AmberCocoon.FormDelay, fixedFeet: feet);
            corpse.shell.Broken += corpse.OnBroken;
            corpse.ShowFossil();
            return corpse;
        }

        /// <summary>Copia los dibujos del enemigo en piezas fijas (sin sombra) y esconde los del enemigo.</summary>
        void Snapshot(Health victim)
        {
            int highest = int.MinValue;
            var sources = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var renderer in victim.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow"))
                    continue;
                sources.Add(renderer);
                highest = Mathf.Max(highest, renderer.sortingOrder);
            }

            foreach (var source in sources)
            {
                var piece = new GameObject("Fossil");
                piece.transform.SetParent(transform, true);
                piece.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                piece.transform.localScale = source.transform.lossyScale;
                var copy = piece.AddComponent<SpriteRenderer>();
                copy.sprite = source.sprite;
                copy.flipX = source.flipX;
                copy.flipY = source.flipY;
                copy.sortingLayerID = source.sortingLayerID;
                copy.sortingOrder = FossilOrder + source.sortingOrder - highest;
                fossil.Add(copy);
                // El original ya no se ve: lo que queda de él es el fósil.
                source.enabled = false;
            }
        }

        void ShowFossil()
        {
            float ramp = shell == null ? 1f : Mathf.Clamp01((shell.Clock - FossilStart) / FossilSeconds);
            var color = Color.Lerp(Color.white, FossilColor, FossilStrength * ramp);
            foreach (var piece in fossil)
            {
                if (piece != null)
                    piece.color = color;
            }
        }

        void Update()
        {
            if (shell == null)
                return;
            ShowFossil();
            if (Time.time >= until && !shell.IsBroken)
                shell.Break();
        }

        // Las grietas avanzan con los golpes: fase 1-3 según lo que queda de vida.
        void OnHealthChanged(int current, int max)
        {
            if (shell == null || max <= 0)
                return;
            shell.SetCrackPhase(Mathf.CeilToInt(3f * (max - current) / max));
        }

        void OnDied()
        {
            if (shell != null)
                shell.Break();
        }

        // En el cuadro que libera, el fósil ya no está y salen las esquirlas.
        void OnBroken(Vector2 center)
        {
            AmberShards.Burst(center, settings.shardRadius, settings.shardDamage, settings.shardPush, 0f);
            Destroy(gameObject);
        }
    }
}
