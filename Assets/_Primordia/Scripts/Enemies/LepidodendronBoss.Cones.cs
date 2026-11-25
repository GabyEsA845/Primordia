using Primordia.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        // Provisionales: ritmo, separación, margen y respuesta al golpe del tronco.
        public const float ConePause = 4f, InjuredConePause = 3f, MethaneWarning = 1f;
        public const float MethaneTrunkRadius = 5f, ConeSpacing = 3f;
        const float ConeWallMargin = 2f, ConeShakeSeconds = 1f;
        const float WarnVeinStep = 0.07f, HitVeinStep = 0.06f, WarnShroomStep = 0.07f;
        const int ConeCount = 3, HealthyConcurrency = 1, InjuredConcurrency = 2;
        const int TestArmorDamage = 10;
        readonly List<LepidodendronCone> cones = new();
        readonly List<MethaneSac> coneSacs = new();
        float nextConeAt, coneWarnAt = float.NegativeInfinity, coneWarnUntil, coneHitAt = float.NegativeInfinity;
        int nextCone;
        public IReadOnlyList<LepidodendronCone> Cones => cones;
        public bool AllConesDestroyed { get; private set; }
        public event Action ConesDestroyed;
        public float CurrentConePause => cones.Any(c => c.Broken) ? InjuredConePause : ConePause;
        public int ConeConcurrency => cones.Any(c => c.Broken) ? InjuredConcurrency : HealthyConcurrency;
        internal Transform ConePlayer { get => player; set => player = value; }
        internal float ConeTop => canopyRoot.position.y + ConeWallMargin;

        void MethaneSubscribe() => MethaneSac.BurstStarted += OnMethaneBurst;
        void MethaneUnsubscribe() => MethaneSac.BurstStarted -= OnMethaneBurst;

        void BuildCones()
        {
            for (int i = 0; i < ConeCount; i++)
            {
                var obj = new GameObject($"Cone{i + 1}");
                obj.transform.SetParent(scenery, false);
                var cone = obj.AddComponent<LepidodendronCone>();
                cone.Configure(this, i);
                cones.Add(cone);
            }
            scenery.Find("Trunk").GetComponent<PolygonCollider2D>().excludeLayers = LayerMask.GetMask(CombatLayers.PlayerProjectile, CombatLayers.EnemyProjectile);
            var trunkHit = new GameObject("TrunkTarget") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            trunkHit.transform.SetParent(scenery.Find("Trunk"), false);
            var hitbox = trunkHit.AddComponent<BoxCollider2D>();
            hitbox.size = new Vector2(TrunkHalfWidth * 2f, 4f);
            hitbox.excludeLayers = LayerMask.GetMask(CombatLayers.Player, CombatLayers.Enemy);
            if (wallMethane != null)
                foreach (var offset in new[] { new Vector2(-7.5f, 0f), new Vector2(7.5f, 0f), new Vector2(0f, -7.5f) })
                    coneSacs.Add(MethaneSac.Create(scenery, Foot + offset, wallMethane.sacIdle, wallMethane.sacBurst, wallMethane.sacRemains));
        }

        void BeginCones()
        {
            Health.IsInvulnerable = false;
            Health.DamageGate = FilterTrunkDamage;
        }

        /// <summary>El jugador ya llegó al centro de la arena: los conos y los ataques de la fase final están en marcha.</summary>
        public bool FinalEngaged { get; private set; }
        public const float EngageMargin = 2f; // Provisional: a cuántas u antes del centro de la arena se considera «llegado».

        internal void EngageFinalPhase()
        {
            if (!FinalPhaseReady || FinalEngaged || AllConesDestroyed) return;
            FinalEngaged = true;
            Sfx.Play(Sounds.BossLepidodendronPhaseFinal);
            nextConeAt = Time.time;
            StartFinalAttacks();
        }

        void CheckFinalEngagement()
        {
            if (!FinalPhaseReady || FinalEngaged || player == null) return;
            if (player.position.x >= ArenaCenter.x - EngageMargin && Mathf.Abs(player.position.y - ArenaCenter.y) <= ArenaHeight / 2f)
                EngageFinalPhase();
        }

        // El tronco no recibe daño en la fase final: la barra del jefe solo baja con la vida de los conos, y cada cono se rompe
        // con golpes directos en su ventana en el suelo. Hay que romper los tres para terminar con el jefe.
        int FilterTrunkDamage(int amount, DamageKind kind, DamageSource source)
        {
            // El tronco no recibe daño, pero el golpe suena (intervalo corto en los datos de audio).
            if (amount > 0)
                Sfx.Play(Sounds.BossLepidodendronTrunkHit);
            return 0;
        }

        internal void ConeLostHealth(int amount) => Health.Drain(amount, allowZero: true);

        internal void CheckConesDestroyed()
        {
            if (AllConesDestroyed || cones.Count != ConeCount || cones.Any(c => !c.Broken)) return;
            AllConesDestroyed = true;
            Health.IsInvulnerable = true;
            ConesDestroyed?.Invoke();
        }

        internal void ConeFinished(float now) => nextConeAt = now + CurrentConePause;

        internal void UpdateCones(float now)
        {
            if (!FinalEngaged || AllConesDestroyed || now < nextConeAt) return;
            int busy = cones.Count(c => !c.Available && !c.Broken);
            // Tras la primera rotura se admiten dos ciclos simultáneos al finalizar la pausa.
            while (busy < ConeConcurrency)
            {
                if (!DropNextCone(now)) break;
                busy++;
            }
            nextConeAt = float.PositiveInfinity; // La pausa empieza al terminar una subida o al romper un cono.
        }

        internal bool DropNextCone(float now)
        {
            if (!FinalPhaseReady || AllConesDestroyed) return false;
            for (int attempt = 0; attempt < cones.Count; attempt++)
            {
                var cone = cones[nextCone];
                nextCone = (nextCone + 1) % cones.Count;
                if (cone.BeginDrop(LepidodendronCone.WarningSeconds, Vector2.zero, now)) return true;
            }
            return false;
        }

        internal Vector2 ClampConePoint(Vector2 point) => new Vector2(
            Mathf.Clamp(point.x, ArenaCenter.x - ArenaWidth / 2f + ConeWallMargin, ArenaCenter.x + ArenaWidth / 2f - ConeWallMargin),
            Mathf.Clamp(point.y, ArenaCenter.y - ArenaHeight / 2f + ConeWallMargin, ArenaCenter.y + ArenaHeight / 2f - ConeWallMargin));

        internal Vector2 ConeTarget(Vector2 offset, bool spread)
        {
            // Reserva el ancho de la tanda ANTES de desplazarla; recortar cada punto por separado los superpondría.
            Vector2 center = ClampConePoint(player != null ? (Vector2)player.position : ArenaCenter);
            if (spread)
                center.x = Mathf.Clamp(center.x, ArenaCenter.x - ArenaWidth / 2f + ConeWallMargin + ConeSpacing,
                    ArenaCenter.x + ArenaWidth / 2f - ConeWallMargin - ConeSpacing);
            return center + offset;
        }

        void OnMethaneBurst(MethaneSac sac)
        {
            if (sac == null || wallRoom == null || !sac.transform.IsChildOf(wallRoom)) return;
            TriggerConeMethane(sac.Position, Time.time);
        }

        internal void TriggerConeMethane(Vector2 position, float now)
        {
            if (!FinalEngaged || AllConesDestroyed || Vector2.Distance(position, Foot) > MethaneTrunkRadius) return;
            var free = cones.Where(c => c.Available).ToArray();
            if (free.Length > 0)
                Sfx.Play(Sounds.BossLepidodendronMethaneWarn);
            for (int i = 0; i < free.Length; i++)
                free[i].BeginDrop(MethaneWarning, Vector2.right * ((i - (free.Length - 1) / 2f) * ConeSpacing), now, spread: true);
            if (free.Length > 0) nextConeAt = float.PositiveInfinity;
        }

        internal void WarnCones(float now, float duration)
        {
            coneWarnAt = now;
            coneWarnUntil = now + duration;
            Sfx.Play(Sounds.BossLepidodendronTreeShake);
            ShakeCanopy(ConeShakeSeconds);
        }

        internal void HitConeVeins() => coneHitAt = Time.time;

        void AnimateConeSignals()
        {
            var veins = scenery.Find("Veins").GetComponent<SpriteRenderer>();
            bool hit = Time.time - coneHitAt < HitVeinStep * 2f;
            bool warn = Time.time < coneWarnUntil;
            string sheet = hit ? "Lepido_Veins_Hit" : warn ? "Lepido_Veins_Warn" : "Lepido_Veins_Rest";
            var frames = art.Find(sheet).sprites;
            veins.sprite = !hit && !warn ? frames[0] : frames[Mathf.Clamp(Mathf.FloorToInt((Time.time - (hit ? coneHitAt : coneWarnAt)) / (hit ? HitVeinStep : WarnVeinStep)), 0, frames.Length - 1)];
            if (!warn) return;
            foreach (var visual in visuals)
                if (visual.sheet.StartsWith("Lepido_Shroom"))
                {
                    var sequence = art.Find(visual.sheet.Replace("_Idle", "_Warn")).sprites;
                    visual.renderer.sprite = sequence[Mathf.FloorToInt((Time.time - coneWarnAt) / WarnShroomStep) % sequence.Length];
                }
        }

        internal void TestFinalPhase()
        {
            foreach (var wall in walls) if (!wall.Fallen) wall.Health.Kill();
            EngageFinalPhase(); // Los atajos de prueba saltan directamente a la fase en marcha.
        }

        internal string TestConeAction(int action)
        {
            if (action == 0) { TestFinalPhase(); return "Fase final: 3 conos"; }
            if (!FinalPhaseReady) return "Primero salta a la fase final";
            if (action == 1) return DropNextCone(Time.time) ? "Cono: aviso y bajada" : "No hay conos disponibles";
            if (action == 5)
            {
                var sac = coneSacs.FirstOrDefault(s => s != null && s.CanIgnite);
                if (sac == null) return "No quedan odres de prueba";
                sac.transform.position = Foot + Vector2.down * ConeSpacing;
                sac.Ignite();
                return "Metano: odre trasladado y prendido junto al tronco";
            }
            var cone = cones.FirstOrDefault(c => c.State == LepidodendronCone.Phase.Grounded);
            if (cone == null) return "No hay un cono en el suelo";
            if (action == 2 && cone.ArmorRemaining > 0) cone.Health.TakeDamage(TestArmorDamage);
            if (action == 3) cone.Ignite();
            if (action == 4) cone.Health.Kill();
            return $"Cono: armadura {cone.ArmorRemaining}/30, vida {cone.Health.Current}/50";
        }
    }
}
