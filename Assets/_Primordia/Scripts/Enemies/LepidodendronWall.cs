using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Una sola vida por pared: el impacto distingue nudo/cuerpo sin multiplicar contactos de un mismo ataque.</summary>
    [RequireComponent(typeof(Health))]
    public class LepidodendronWall : MonoBehaviour
    {
        public const int KnotEndurance = 22; // Provisional, daño sin multiplicar.
        public const int FireDamagePerSecond = 12; // Provisional: vía rápida, fuera del tope Burn.
        public const float SpreadSeconds = 3f;
        public const float SegmentHeight = 1.5f; // Cubre también el radio de ignición de 1,3 u del escarabajo.
        const int KnotMultiplier = 2, CharredMultiplier = 2;
        const float D1Threshold = 0.75f, D2Threshold = 0.5f, D3Threshold = 0.25f;
        const float HitPointTolerance = 0.01f, ShakeRate = 60f;
        const int BandLayerOffset = 1, HurtLayerOffset = 2, KnotLayerOffset = 3, EffectLayerOffset = 4, ScarSortOrder = -12;
        const int KnotHitFrames = 2;
        const float HurtStep = 0.06f, KnotBurstStep = 0.07f, KnotIdleStep = 0.16f, SmolderStep = 0.1f;
        const float ShakeSeconds = 0.3f, ShakePixels = 2f, FallStep = 0.09f, Ppu = 16f;
        const float Height = LepidodendronBoss.CorridorHeight;
        const float FlameWidth = 2f;

        LepidodendronBoss owner;
        DirectionalArt art;
        ReactionKit reactions;
        SpriteRenderer body, hurt;
        Mesh charMesh;
        Material charMaterial;
        MeshRenderer charDrawing;
        readonly List<Knot> knots = new();
        readonly List<SpriteRenderer> fronts = new();
        readonly List<SpriteRenderer> smolder = new();
        Health health;
        BoxCollider2D blocker;
        Collider2D target;
        float burnStart, burnLastTick, burnAtY, burnRemainder, hurtUntil, fellAt;
        float burntLow, burntHigh;
        int lastHealth;
        bool burning, applyingFire, fallCharred, fallDone;
        Vector3 bodyOrigin;

        public int Index { get; private set; }
        public Health Health => health;
        public BoxCollider2D BodyCollider => blocker;
        public bool Fallen { get; private set; }
        public bool FullyCharred => burning && Time.time - burnStart >= SpreadSeconds;
        public bool IsBurning => burning && !Fallen;
        public int DamageState => health.Current > health.Max * D1Threshold ? 0 : health.Current > health.Max * D2Threshold ? 1 : health.Current > health.Max * D3Threshold ? 2 : 3;
        public bool UsesAshScar => fallCharred;
        public int KnotCount => knots.Count;
        public float KnotRemaining(int index) => knots[index].remaining;
        public Vector2 KnotPosition(int index) => knots[index].renderer.transform.position;

        internal void Configure(LepidodendronBoss boss, int index, SpriteRenderer renderer, BoxCollider2D collider,
            DirectionalArt sprites, ReactionKit kit, Vector3Int[] crops)
        {
            owner = boss;
            Index = index;
            body = renderer;
            blocker = collider;
            art = sprites;
            reactions = kit;
            health = GetComponent<Health>();
            health.SetMax(LepidodendronBoss.WallHealth, false);
            health.ResetHealth();
            lastHealth = health.Current;
            health.DamageGate = FilterDamage;
            health.Damaged += OnDamaged;
            health.Died += Fall;
            bodyOrigin = body.transform.localPosition;
            // El bloqueo físico sigue en Default; el objetivo Enemy comparte exactamente la misma vida.
            // Así llega el mordisco y el proyectil no encuentra antes el bloqueo de Default.
            blocker.excludeLayers = LayerMask.GetMask(CombatLayers.PlayerProjectile, CombatLayers.EnemyProjectile);
            var hitbox = new GameObject("WallTarget") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            hitbox.transform.SetParent(transform, false);
            var hit = hitbox.AddComponent<BoxCollider2D>();
            hit.size = blocker.size;
            hit.offset = blocker.offset;
            hit.excludeLayers = LayerMask.GetMask(CombatLayers.Player, CombatLayers.Enemy);
            target = hit;
            hurt = NewRenderer("Hurt", body.sortingOrder + HurtLayerOffset);
            for (int i = 0; i < crops.Length; i++)
            {
                var node = transform.Find($"Knot{i + 1}").GetComponent<SpriteRenderer>();
                node.sortingOrder = body.sortingOrder + KnotLayerOffset;
                knots.Add(new Knot { renderer = node, side = crops[i].z / Ppu });
            }
            // Registro por la superficie oeste, no por el centro: cualquier fuente de fuego puede alcanzarla.
            for (float y = SegmentHeight / 2f; y < Height; y += SegmentHeight)
            {
                var obj = new GameObject("IgnitionSegment");
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = new Vector2(-blocker.size.x / 2f, y);
                obj.AddComponent<LepidodendronBossWallIgnition>().Configure(this, y);
            }
            // Burn propaga desde el transform de su Health. Este punto real de la base permite que lo encuentre
            // sin fabricar un punto de impacto ni cambiar el código común de quemadura.
            var baseIgnition = new GameObject("IgnitionBase");
            baseIgnition.transform.SetParent(transform, false);
            baseIgnition.AddComponent<LepidodendronBossWallIgnition>().Configure(this, 0f);
        }

        SpriteRenderer NewRenderer(string name, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        Sprite[] Sheet(string suffix) => art.Find($"Lepido_Wall{Index + 1}_{suffix}").sprites;

        int FilterDamage(int amount, DamageKind kind, DamageSource source)
        {
            if (Fallen) return 0;
            if (applyingFire) return amount;
            if (source == DamageSource.Burn)
            {
                if (health.LastImpactPoint.HasValue)
                    IgniteAt(health.LastImpactPoint.Value.y - transform.position.y);
                // Sin punto no se inventa una altura. Si ya arde, no se duplican sus 12/s con ticks de Burn externos.
                return burning ? 0 : owner.LimitWallDamage(amount, source);
            }
            int node = kind == DamageKind.Hit && health.LastImpactPoint.HasValue ? KnotAt(health.LastImpactPoint.Value) : -1;
            int factor = node >= 0 && knots[node].remaining > 0 ? KnotMultiplier : 1;
            if (kind == DamageKind.Hit && health.LastImpactPoint.HasValue && CharredAt(health.LastImpactPoint.Value.y - transform.position.y))
                factor *= CharredMultiplier;
            else if (kind == DamageKind.Hit && !health.LastImpactPoint.HasValue && FullyCharred)
                factor *= CharredMultiplier;
            int allowed = owner.LimitWallDamage(Mathf.Min(health.Current, amount * factor), source);
            if (allowed > 0 && node >= 0 && knots[node].remaining > 0)
            {
                var knot = knots[node];
                // Resistencia sin multiplicar, contando solo la parte que admitió el tope compartido.
                knot.remaining = Mathf.Max(0f, knot.remaining - allowed / (float)factor);
                knot.hitAt = Time.time;
                if (knot.remaining == 0)
                {
                    knot.burst = NewRenderer("KnotBurst", body.sortingOrder + EffectLayerOffset);
                    knot.burst.transform.position = knot.renderer.transform.position;
                }
            }
            return allowed;
        }

        int KnotAt(Vector2 point)
        {
            var local = point - (Vector2)transform.position;
            if (Mathf.Abs(local.x) > blocker.size.x / 2f + HitPointTolerance) return -1;
            // El recorte da la altura de la zona débil; su volumen se proyecta hasta ambas caras sólidas de la pared.
            for (int i = 0; i < knots.Count; i++)
                if (Mathf.Abs(point.y - knots[i].renderer.transform.position.y) <= knots[i].side / 2f)
                    return i;
            return -1;
        }

        void OnDamaged(int ignored)
        {
            int lost = lastHealth - health.Current;
            lastHealth = health.Current;
            owner.WallLostHealth(lost); // Solo vida realmente absorbida, incluso con sobre-daño o Kill de depuración.
            if (health.Current > 0)
            {
                hurtUntil = Time.time + Sheet($"Hurt_D{DamageState}").Length * HurtStep;
                owner.StartWallWaves(this);
            }
            Show();
        }

        public void IgniteAt(float height)
        {
            if (burning || Fallen) return;
            burning = true;
            burnStart = Time.time;
            burnLastTick = Time.time;
            burnAtY = Mathf.Clamp(height, 0f, Height);
            burntLow = burntHigh = burnAtY;
            BuildCharredDrawing();
            int across = Mathf.CeilToInt(blocker.size.x / FlameWidth);
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < across; i++)
                {
                    var flame = NewRenderer(side == 0 ? "FireNorth" : "FireSouth", body.sortingOrder + EffectLayerOffset);
                    flame.transform.localPosition = new Vector2((i + 0.5f) * blocker.size.x / across - blocker.size.x / 2f, height);
                    fronts.Add(flame);
                }
            for (float y = SegmentHeight / 2f; y < Height; y += SegmentHeight)
            {
                var ember = NewRenderer("RootSmolder", body.sortingOrder + KnotLayerOffset);
                ember.transform.localPosition = new Vector2(0f, y);
                smolder.Add(ember);
            }
        }

        bool CharredAt(float y) => burning && (FullyCharred || y >= burntLow && y <= burntHigh);

        void Update()
        {
            if (Fallen) { ShowFall(); return; }
            if (burning)
            {
                float reach = Mathf.Max(burnAtY, Height - burnAtY) * Mathf.Clamp01((Time.time - burnStart) / SpreadSeconds);
                burntLow = Mathf.Max(0f, burnAtY - reach);
                burntHigh = Mathf.Min(Height, burnAtY + reach);
                burnRemainder += (Time.time - burnLastTick) * FireDamagePerSecond;
                burnLastTick = Time.time;
                int damage = Mathf.FloorToInt(burnRemainder);
                burnRemainder -= damage;
                if (damage > 0)
                {
                    applyingFire = true;
                    try { health.TakeDamage(damage, DamageKind.Tick, DamageSource.Burn); }
                    finally { applyingFire = false; }
                }
            }
            if (!Fallen) Show();
        }

        void Show()
        {
            if (body == null || Fallen) return;
            body.sprite = Sheet($"{(FullyCharred ? "Charred_" : "")}D{DamageState}")[0];
            hurt.enabled = Time.time < hurtUntil;
            if (hurt.enabled)
            {
                var frames = Sheet($"Hurt_D{DamageState}");
                int frame = Mathf.Clamp(frames.Length - Mathf.CeilToInt((hurtUntil - Time.time) / HurtStep), 0, frames.Length - 1);
                hurt.sprite = frames[frame];
            }
            if (burning)
            {
                UpdateCharredDrawing();
                int half = fronts.Count / 2;
                for (int i = 0; i < fronts.Count; i++)
                {
                    var flame = fronts[i];
                    flame.enabled = !FullyCharred;
                    var p = flame.transform.localPosition;
                    p.y = i < half ? burntHigh : burntLow;
                    flame.transform.localPosition = p;
                    if (reactions != null && reactions.GroundFire.Length > 0)
                        flame.sprite = reactions.GroundFire[Mathf.FloorToInt(Time.time / reactions.GroundFireFrameSeconds) % reactions.GroundFire.Length];
                }
                var embers = art.Find("Fx_RootSmolder").sprites;
                foreach (var ember in smolder)
                {
                    ember.enabled = CharredAt(ember.transform.localPosition.y);
                    ember.sprite = embers[Mathf.FloorToInt(Time.time / SmolderStep) % embers.Length];
                }
            }
            for (int i = 0; i < knots.Count; i++)
            {
                var knot = knots[i];
                string suffix = knot.remaining == 0 ? (CharredAt(knot.renderer.transform.localPosition.y) ? "WoundCharred" : "Wound")
                    : Time.time - knot.hitAt < HurtStep * KnotHitFrames ? "Hit" : "Idle";
                var frames = Sheet($"Knot{i}_{suffix}");
                knot.renderer.sprite = frames[Mathf.FloorToInt((suffix == "Hit" ? Time.time - knot.hitAt : Time.time) / (suffix == "Idle" ? KnotIdleStep : HurtStep)) % frames.Length];
                if (knot.burst != null)
                {
                    var burst = art.Find("Fx_KnotBurst").sprites;
                    int frame = Mathf.FloorToInt((Time.time - knot.hitAt) / KnotBurstStep);
                    if (frame >= burst.Length) { Destroy(knot.burst.gameObject); knot.burst = null; }
                    else knot.burst.sprite = burst[frame];
                }
            }
        }

        void Fall()
        {
            if (Fallen) return;
            fallCharred = FullyCharred;
            Fallen = true;
            fellAt = Time.time;
            blocker.enabled = false;
            target.enabled = false;
            hurt.enabled = false;
            foreach (var knot in knots)
            {
                knot.renderer.enabled = false;
                if (knot.burst != null) Destroy(knot.burst.gameObject);
            }
            foreach (var flame in fronts) Destroy(flame.gameObject);
            foreach (var ember in smolder) Destroy(ember.gameObject);
            fronts.Clear(); smolder.Clear();
            FreeDrawing();
            owner.CancelWallWaves(this);
            owner.WallFell();
            ShowFall();
        }

        void ShowFall()
        {
            if (fallDone) return;
            float elapsed = Time.time - fellAt;
            if (!fallCharred && elapsed < ShakeSeconds)
            {
                body.transform.localPosition = bodyOrigin + Vector3.right * (Mathf.FloorToInt(elapsed * ShakeRate) % 2 == 0 ? ShakePixels : -ShakePixels) / Ppu;
                return;
            }
            body.transform.localPosition = bodyOrigin;
            float time = elapsed - (fallCharred ? 0f : ShakeSeconds);
            var frames = Sheet(fallCharred ? "Crumble" : "Retract");
            int frame = Mathf.FloorToInt(time / FallStep);
            if (frame < frames.Length) body.sprite = frames[Mathf.Max(0, frame)];
            else
            {
                body.sprite = Sheet(fallCharred ? "AshScar" : "Scar")[0];
                body.sortingOrder = ScarSortOrder;
                fallDone = true;
            }
        }

        void BuildCharredDrawing()
        {
            var obj = new GameObject("CharredBand");
            obj.transform.SetParent(transform, false);
            charMesh = new Mesh { name = "LepidodendronWallBand", hideFlags = HideFlags.HideAndDontSave };
            obj.AddComponent<MeshFilter>().sharedMesh = charMesh;
            charDrawing = obj.AddComponent<MeshRenderer>();
            charDrawing.sortingOrder = body.sortingOrder + BandLayerOffset;
            charMaterial = new Material(Shader.Find("Sprites/Default")) { name = "LepidodendronWallBand", hideFlags = HideFlags.HideAndDontSave };
            charDrawing.sharedMaterial = charMaterial;
        }

        void UpdateCharredDrawing()
        {
            if (charDrawing == null) return;
            charDrawing.enabled = !FullyCharred && burntHigh > burntLow;
            var sprite = Sheet($"Charred_D{DamageState}")[0];
            charMaterial.mainTexture = sprite.texture;
            float left = -sprite.pivot.x / Ppu, right = (sprite.rect.width - sprite.pivot.x) / Ppu;
            float bottom = -sprite.pivot.y / Ppu, top = (sprite.rect.height - sprite.pivot.y) / Ppu;
            float low = burntLow <= 0f ? bottom : burntLow, high = burntHigh >= Height ? top : burntHigh;
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            float v0 = Mathf.Lerp(uv.y, uv.w, (low - bottom) / (top - bottom));
            float v1 = Mathf.Lerp(uv.y, uv.w, (high - bottom) / (top - bottom));
            charMesh.Clear();
            charMesh.vertices = new[] { new Vector3(left, low), new Vector3(right, low), new Vector3(left, high), new Vector3(right, high) };
            charMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white }; // Conserva la paleta del sprite recortado.
            charMesh.uv = new[] { new Vector2(uv.x, v0), new Vector2(uv.z, v0), new Vector2(uv.x, v1), new Vector2(uv.z, v1) };
            charMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        }

        void FreeDrawing()
        {
            if (charDrawing != null) { charDrawing.enabled = false; Destroy(charDrawing.gameObject); }
            if (charMesh != null) Destroy(charMesh);
            if (charMaterial != null) Destroy(charMaterial);
            charDrawing = null; charMesh = null; charMaterial = null;
        }

        void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= Fall;
                health.DamageGate = null;
            }
            FreeDrawing();
        }

        sealed class Knot
        {
            public SpriteRenderer renderer, burst;
            public float side, hitAt = float.NegativeInfinity;
            public float remaining = KnotEndurance;
        }
    }
}
