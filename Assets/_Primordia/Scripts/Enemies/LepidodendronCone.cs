using Primordia.Audio;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Conserva resistencia, carbonizado y vida durante todo el encuentro.</summary>
    [RequireComponent(typeof(Health))]
    public class LepidodendronCone : MonoBehaviour, IFlammable
    {
        public const int ArmorEndurance = 30, ConeHealth = 50, SlamDamage = 2;
        // Provisionales: tiempos de combate y dimensiones; el golpe es una elipse de semiejes SlamRadius × SlamRadiusY.
        public const float WarningSeconds = 1.5f, ShadowSeconds = 0.72f, DescentSeconds = 0.35f;
        public const float GroundSeconds = 8f, LiftSeconds = 1f, SlamRadius = 1.6f, SlamRadiusY = SlamRadius * 0.7f;
        const int ArmorStep = 10, CharredMultiplier = 2, ContactShadowFrame = 4;
        const float Ppu = 16f, BouncePixels = 2f, BodyWidth = 2.5f, BodyHeight = 0.8f;
        const float HitHeight = 5f, BehindOpacity = 0.5f, HurtStep = 0.06f, BurnStep = 0.15f;
        const float ShatterStep = 0.09f, BurstStep = 0.08f, IdleStep = 0.12f;
        const float SlamStep = 0.07f, ChipStep = 0.05f, ShadowStep = 0.09f;
        const int GroundOrder = 7, FrontOrder = 11, BackOrder = 9;

        public enum Phase { Hanging, Warning, Shadow, Descending, Grounded, Rising, Husk }
        public Phase State { get; private set; }
        public int ArmorRemaining { get; private set; } = ArmorEndurance;
        public int ArmorState => Mathf.Min(2, (ArmorEndurance - ArmorRemaining) / ArmorStep);
        public bool Charred { get; private set; }
        public Health Health { get; private set; }
        public bool Broken => Health != null && Health.Current == 0;
        public bool Available => !Broken && State == Phase.Hanging;
        public Vector2 LandingPoint { get; private set; }
        public float CurrentWarningSeconds { get; private set; }
        public Vector2 Position => transform.position;
        public bool CanIgnite => isActiveAndEnabled && State == Phase.Grounded && ArmorRemaining > 0 && !Charred;

        LepidodendronBoss owner;
        SpriteRenderer body, shadow, crater, branch, torn;
        BoxCollider2D blocker, target;
        float phaseAt, animationAt, animationStep, groundAt;
        string animation;
        int previousHealth;
        bool trunkDamage;
        Vector2 targetOffset;
        bool spreadTargets;
        readonly List<IFlammable> ignitionPoints = new();
        Sprite[] slamBack, slamFront;

        internal void Configure(LepidodendronBoss boss, int index)
        {
            owner = boss;
            Health = GetComponent<Health>();
            Health.SetMax(ConeHealth, false);
            Health.ResetHealth();
            previousHealth = Health.Current;
            Health.DamageGate = FilterDamage;
            Health.Damaged += LostHealth;
            Health.Died += Break;
            LandingPoint = owner.ClampConePoint(owner.ArenaCenter + Vector2.right * (index - 1) * LepidodendronBoss.ConeSpacing);
            transform.position = LandingPoint;
            BuildSlamFrames();
            body = Drawing("Body");
            shadow = Drawing("Shadow");
            BuildMarker();
            crater = Drawing("Crater");
            branch = Drawing("Branch");
            torn = Drawing("TornBranch");
            branch.sprite = Frames("Lepido_ConeBranch_Tile")[0];
            branch.drawMode = SpriteDrawMode.Tiled;
            torn.sprite = Frames("Lepido_ConeBranch_Torn")[0];
            blocker = gameObject.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(BodyWidth, BodyHeight);
            blocker.excludeLayers = LayerMask.GetMask(CombatLayers.PlayerProjectile, CombatLayers.EnemyProjectile);
            var hit = new GameObject("ConeTarget") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            hit.transform.SetParent(transform, false);
            target = hit.AddComponent<BoxCollider2D>();
            target.size = new Vector2(BodyWidth, HitHeight);
            target.offset = Vector2.up * HitHeight / 2f;
            target.excludeLayers = LayerMask.GetMask(CombatLayers.Player, CombatLayers.Enemy);
            // Dos puntos reales; no se registra ni se quema la rama.
            ignitionPoints.Add(this);
            ignitionPoints.Add(new IgnitionPoint(this));
            if (isActiveAndEnabled) RegisterIgnition();
            SetPhase(Phase.Hanging, Time.time);
            Render(Time.time);
        }

        SpriteRenderer Drawing(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            return obj.AddComponent<SpriteRenderer>();
        }

        // Marca de aviso en el suelo (arte aprobado, Cones/Fx_ConeMark_*): anillo de esporas rojas en elipse en picada (alto = 0,7 ×
        // ancho). Sigue al jugador durante el aviso, se cierra y se enciende al fijarse el punto y destella hasta el golpe. Su borde de
        // fuera es el área del daño (semiejes SlamRadius × SlamRadiusY). Sin giro ni escala: rompen el píxel.
        const float MarkerFollowStep = 0.11f, MarkerLockStep = 0.09f, MarkerHoldStep = 0.06f, MarkerFollowAlpha = 0.85f;
        SpriteRenderer marker;

        void BuildMarker()
        {
            marker = Drawing("Marker");
            marker.enabled = false;
        }

        void RenderMarker(float now)
        {
            bool show = State == Phase.Warning || State == Phase.Shadow || State == Phase.Descending;
            marker.enabled = show;
            if (!show) return;
            marker.sortingOrder = GroundOrder + 1;
            float age = now - phaseAt;
            float alpha = 1f;
            Sprite[] frames;
            int frame;
            if (State == Phase.Warning)
            {
                frames = Frames("Fx_ConeMark_Follow");
                frame = Mathf.FloorToInt(now / MarkerFollowStep);
                // Pulso lento que se acelera al acercarse el final del aviso.
                float left = Mathf.Max(0f, CurrentWarningSeconds - age);
                float rate = Mathf.Lerp(2f, 6f, 1f - Mathf.Clamp01(left / CurrentWarningSeconds));
                alpha = MarkerFollowAlpha * (0.8f + 0.2f * Mathf.Sin(now * rate * Mathf.PI * 2f));
            }
            else if (State == Phase.Shadow)
            {
                frames = Frames("Fx_ConeMark_Lock");
                frame = Mathf.FloorToInt(age / MarkerLockStep);
            }
            else
            {
                frames = Frames("Fx_ConeMark_Hold");
                frame = Mathf.FloorToInt(age / MarkerHoldStep);
            }
            marker.sprite = frames[Mathf.Clamp(State == Phase.Shadow ? frame : ((frame % frames.Length) + frames.Length) % frames.Length, 0, frames.Length - 1)];
            marker.color = new Color(1f, 1f, 1f, alpha);
            marker.transform.position = LandingPoint;
        }

        /// <summary>True si el punto más cercano a la marca de ese collider cae dentro de la elipse del golpe.</summary>
        bool InSlamArea(Collider2D collider)
        {
            var offset = collider.ClosestPoint(LandingPoint) - LandingPoint;
            return Mathf.Pow(offset.x / SlamRadius, 2f) + Mathf.Pow(offset.y / SlamRadiusY, 2f) <= 1f;
        }

        Sprite[] Frames(string name) => owner.Art.Find(name).sprites;
        string ArmorSheet => $"Lepido_Cone_Armor{(Charred ? "Charred" : "")}{ArmorState}";

        internal bool BeginDrop(float warning, Vector2 offset, float now, bool spread = false)
        {
            if (!Available) return false;
            CurrentWarningSeconds = warning;
            targetOffset = offset;
            spreadTargets = spread;
            SetPhase(Phase.Warning, now);
            owner.WarnCones(now, warning);
            Tick(now);
            return true;
        }

        void Update() { if (owner != null) Tick(Time.time); }

        // Reloj explícito compartido con las pruebas de límites de cuadros; en juego siempre recibe Time.time.
        internal void Tick(float now)
        {
            if (State == Phase.Warning)
            {
                LandingPoint = owner.ConeTarget(targetOffset, spreadTargets);
                transform.position = LandingPoint;
                if (now >= phaseAt + CurrentWarningSeconds)
                    SetPhase(Phase.Shadow, phaseAt + CurrentWarningSeconds);
            }
            if (State == Phase.Shadow && now >= phaseAt + ShadowSeconds)
                SetPhase(Phase.Descending, phaseAt + ShadowSeconds);
            if (State == Phase.Descending && now >= phaseAt + DescentSeconds)
            {
                SetPhase(Phase.Grounded, phaseAt + DescentSeconds);
                groundAt = phaseAt;
                SlamEffect();
                // Único instante con daño: cuadro 0. No hay daño de contacto ni de la onda posterior.
                var victims = new HashSet<Health>();
                foreach (var collider in Physics2D.OverlapCircleAll(LandingPoint, SlamRadius, LayerMask.GetMask(CombatLayers.Player)))
                {
                    if (!InSlamArea(collider)) continue;
                    var health = collider.GetComponentInParent<Health>();
                    if (health != null && victims.Add(health)) health.TakeDamage(SlamDamage);
                }
            }
            if (State == Phase.Grounded && now >= phaseAt + GroundSeconds)
            {
                SetPhase(Phase.Rising, phaseAt + GroundSeconds);
                Effect("Fx_ConeLift", SlamStep, LandingPoint);
            }
            if (State == Phase.Rising && now >= phaseAt + LiftSeconds)
            {
                float ended = phaseAt + LiftSeconds;
                SetPhase(Phase.Hanging, ended);
                owner.ConeFinished(ended);
            }
            Render(now);
        }

        void SetPhase(Phase phase, float now)
        {
            switch (phase)
            {
                case Phase.Warning: Sfx.Play(Sounds.BossLepidodendronConeWarn); break;
                // La sombra fija y la bajada suenan juntas (1,1 s).
                case Phase.Shadow: Sfx.Play(Sounds.BossLepidodendronConeDescend); break;
                case Phase.Grounded: Sfx.Play(Sounds.BossLepidodendronConeSlam); break;
                case Phase.Rising: Sfx.Play(Sounds.BossLepidodendronConeLift); break;
            }
            State = phase;
            phaseAt = now;
            target.enabled = phase == Phase.Grounded;
            blocker.enabled = phase == Phase.Grounded || phase == Phase.Husk;
        }

        int FilterDamage(int amount, DamageKind kind, DamageSource source)
        {
            if (Broken) return 0;
            if (trunkDamage) return amount; // El tronco ya pasó una vez por el limitador compartido.
            if (State != Phase.Grounded) return 0;
            if (source == DamageSource.Burn)
            {
                // Carboniza, pero nunca consume resistencia ni vida ni el presupuesto de daño de otro cono.
                Ignite();
                return 0;
            }
            if (ArmorRemaining > 0)
            {
                int damage = owner.LimitWallDamage(Mathf.Min(ArmorRemaining, amount * (Charred ? CharredMultiplier : 1)), source);
                if (damage <= 0) return 0;
                ArmorRemaining -= damage;
                Sfx.Play(ArmorRemaining == 0 ? Sounds.BossLepidodendronArmorBreak : Sounds.BossLepidodendronArmorHit);
                Effect(Charred ? "Fx_ArmorChipCharred" : "Fx_ArmorChip", ChipStep, Health.LastImpactPoint ?? Position);
                Play(ArmorRemaining == 0 ? $"Lepido_Cone_Shatter{(Charred ? "Charred" : "")}" : ArmorSheet + "_Hurt",
                    ArmorRemaining == 0 ? ShatterStep : HurtStep);
                // El exceso del golpe que rompe la armadura no se traslada a la vida.
                return 0;
            }
            return owner.LimitWallDamage(Mathf.Min(Health.Current, amount), source);
        }

        internal void DamageThroughTrunk(int amount)
        {
            trunkDamage = true;
            try { Health.TakeDamage(amount); }
            finally { trunkDamage = false; }
        }

        void LostHealth(int ignored)
        {
            int lost = previousHealth - Health.Current;
            previousHealth = Health.Current;
            owner.ConeLostHealth(lost);
            if (lost > 0)
            {
                Sfx.Play(Sounds.BossLepidodendronConeHurt);
                if (ArmorRemaining == 0) Play("Lepido_Cone_Exposed_Hurt", HurtStep);
                owner.HitConeVeins();
            }
        }

        void Break()
        {
            if (State == Phase.Husk) return;
            // También el cono roto por el tronco queda en su punto de suelo, nunca suspendido.
            transform.position = LandingPoint;
            SetPhase(Phase.Husk, Time.time);
            Sfx.Play(Sounds.BossLepidodendronConeBreak);
            Play("Lepido_Cone_Burst", BurstStep);
            owner.ConeFinished(Time.time);
            owner.CheckConesDestroyed();
        }

        public void Ignite()
        {
            if (!CanIgnite) return;
            Charred = true;
            Play($"Lepido_Cone_Burn{ArmorState}", BurnStep);
        }

        void Play(string sheet, float step)
        {
            animation = sheet;
            animationAt = Time.time;
            animationStep = step;
            Render(Time.time);
        }

        void BuildSlamFrames()
        {
            // Divide el arte aprobado por su pivote: polvo trasero detrás del cono y suelo delantero delante.
            var source = Frames("Fx_ConeSlam");
            slamBack = new Sprite[source.Length];
            slamFront = new Sprite[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var sprite = source[i];
                var rect = sprite.rect;
                float split = sprite.pivot.y;
                slamFront[i] = Sprite.Create(sprite.texture, new Rect(rect.x, rect.y, rect.width, split), new Vector2(0.5f, 1f), Ppu);
                slamBack[i] = Sprite.Create(sprite.texture, new Rect(rect.x, rect.y + split, rect.width, rect.height - split), new Vector2(0.5f, 0f), Ppu);
                slamFront[i].name = slamBack[i].name = "LepidodendronConeSlam";
                slamFront[i].hideFlags = slamBack[i].hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void SlamEffect()
        {
            FrameEffect.Play("Fx_ConeSlam", slamFront, SlamStep, FrontOrder + 1, transform);
            FrameEffect.Play("Fx_ConeSlamBack", slamBack, SlamStep, BackOrder - 1, transform);
        }

        void Effect(string sheet, float step, Vector2 position)
        {
            var fx = FrameEffect.Play(sheet, Frames(sheet), step, FrontOrder + 1, transform);
            fx.transform.position = position;
        }

        void Render(float now)
        {
            RenderMarker(now);
            bool ground = State == Phase.Grounded || State == Phase.Husk;
            bool visible = ground || State == Phase.Descending || State == Phase.Rising;
            body.enabled = visible;
            shadow.enabled = ground || State == Phase.Shadow || State == Phase.Descending;
            crater.enabled = ground || State == Phase.Rising;
            shadow.sortingOrder = crater.sortingOrder = GroundOrder;
            shadow.sprite = Frames("Fx_ConeShadow")[State == Phase.Shadow
                ? Mathf.Clamp(Mathf.FloorToInt((now - phaseAt) / ShadowStep), 0, 7) : State == Phase.Descending ? 7 : ContactShadowFrame];
            crater.sprite = Frames("Fx_ConeCrater")[0];
            crater.color = new Color(1f, 1f, 1f, State == Phase.Rising ? 1f - Mathf.Clamp01((now - phaseAt) / LiftSeconds) : 1f);
            string idle = Broken ? "Lepido_Cone_Husk" : ArmorRemaining > 0 ? ArmorSheet : "Lepido_Cone_Exposed_Idle";
            var frames = Frames(idle);
            body.sprite = frames[Mathf.FloorToInt(now / IdleStep) % frames.Length];
            if (animation != null)
            {
                var sequence = Frames(animation);
                int frame = Mathf.Max(0, Mathf.FloorToInt((now - animationAt) / animationStep));
                if (frame < sequence.Length) body.sprite = sequence[frame];
                else animation = null;
            }
            float travel = Mathf.Max(0f, owner.ConeTop - LandingPoint.y);
            float t = State == Phase.Descending ? Mathf.Clamp01((now - phaseAt) / DescentSeconds)
                : State == Phase.Rising ? Mathf.Clamp01((now - phaseAt) / LiftSeconds) : 0f;
            float height = State == Phase.Descending ? travel * (1f - t * t) : State == Phase.Rising ? travel * t : 0f;
            if (State == Phase.Grounded)
                height += BouncePixels / Ppu * Mathf.Sin(Mathf.Clamp01((now - groundAt) / (SlamStep * 2f)) * Mathf.PI);
            float tremble = State == Phase.Rising ? Mathf.Sin(t * 60f) / Ppu : 0f;
            body.transform.localPosition = new Vector2(tremble, height);
            var player = owner.ConePlayer;
            bool behind = player != null && player.position.y > LandingPoint.y;
            body.sortingOrder = behind ? FrontOrder : BackOrder;
            bool overlaps = behind && Mathf.Abs(player.position.x - LandingPoint.x) < BodyWidth && player.position.y < LandingPoint.y + HitHeight;
            body.color = new Color(1f, 1f, 1f, ground && overlaps ? BehindOpacity : 1f);
            float tornAge = now - phaseAt - Frames("Lepido_Cone_Burst").Length * BurstStep;
            torn.enabled = Broken && tornAge >= 0f && tornAge < LiftSeconds;
            torn.transform.localPosition = Vector2.up * travel * Mathf.Clamp01(tornAge / LiftSeconds);
            torn.sortingOrder = body.sortingOrder;
            float branchBase = (Broken ? torn.transform.localPosition.y : height) + 98f / Ppu;
            float length = Mathf.Max(0f, travel - branchBase);
            branch.enabled = length > 0f && (visible && !Broken || torn.enabled);
            branch.size = new Vector2(72f / Ppu, length);
            branch.transform.localPosition = new Vector2(0f, branchBase + length / 2f);
            branch.sortingOrder = body.sortingOrder;
        }

        void RegisterIgnition() { foreach (var point in ignitionPoints) Flammables.Register(point); }
        void OnEnable() => RegisterIgnition();
        void OnDisable() { foreach (var point in ignitionPoints) Flammables.Unregister(point); }
        void OnDestroy()
        {
            if (slamFront != null) foreach (var sprite in slamFront) if (sprite != null) Destroy(sprite);
            if (slamBack != null) foreach (var sprite in slamBack) if (sprite != null) Destroy(sprite);
            if (Health == null) return;
            Health.DamageGate = null;
            Health.Damaged -= LostHealth;
            Health.Died -= Break;
        }

        sealed class IgnitionPoint : IFlammable
        {
            readonly LepidodendronCone cone;
            public IgnitionPoint(LepidodendronCone value) => cone = value;
            public Vector2 Position => cone.Position + Vector2.up * HitHeight / 2f;
            public bool CanIgnite => cone != null && cone.CanIgnite;
            public void Ignite() => cone.Ignite();
        }
    }
}
