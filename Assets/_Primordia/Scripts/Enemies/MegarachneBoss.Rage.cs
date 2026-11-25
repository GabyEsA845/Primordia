using System;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // PROVISIONALES: valores de M5 y tiempos de las hojas aprobadas.
        public const float RageSpeedMultiplier = 1.4f, RageBroodSpeedMultiplier = 1.2f;
        public const float RageAttackInterval = 3f, ScreechSeconds = 2.5f, NestStunSeconds = 6f;
        internal const float RageReweaveDelay = 7f, PostNestWeaveDelay = 4f, PostNestGrowSeconds = 2f;
        internal const int RageSacEvery = 3, RageBroodPerSac = 5, RageBroodLimit = 10;
        const float EmergeMs = 110f, EmergeSeconds = 8f * EmergeMs / 1000f;
        const float ScreechMs = 90f, FunnelHeight = 4f, GroundApproach = 1.5f;
        const float ShakePixels = 4f, ShakeSeconds = 0.35f, AnchorSeparation = 6f;
        const float NestBurnSeconds = 8f * FxFrameSeconds;
        const int ExtraCables = 2, DryingFrames = 6;
        const float RageAnchorY = -7f, RageBranchX = 2f, RageBranchY = 2.5f;
        enum RageStage { None, Retreat, Funnel, Screech, Emerge, Descend }
        RageStage rageStage;
        bool rageTriggered, nestStun;
        float rageTime, nestTime, postNestWeaveTime = -1f;
        [SerializeField] EffectSheet[] rageEffects = Array.Empty<EffectSheet>();
        SpriteRenderer nestRain, screechWave;
        public bool IsRage { get; private set; }
        public bool IsEnraging => rageStage != RageStage.None;
        public bool NestBurned { get; private set; }
        public float MovementMultiplier => IsRage ? RageSpeedMultiplier : 1f;
        public float CurrentCableSpeed => CableSpeed * MovementMultiplier;
        public float CurrentAttackInterval => IsRage ? RageAttackInterval : AttackInterval;
        public Vector2 NestPosition => ArenaCenter + Vector2.up * FunnelHeight;
        internal void ConfigureRage(EffectSheet[] sheets) => rageEffects = sheets;

        void BuildNest()
        {
            var node = new GameObject("NodoNido");
            node.transform.SetParent(scenery.transform, false);
            node.transform.position = NestPosition;
            node.AddComponent<MegarachneBossNest>().Configure(this);
        }

        void CheckRageHealth(int current, int maximum)
        {
            if (current <= MaxHealth / 2 && !Health.IsDead) ForceRage();
        }

        public bool ForceRage()
        {
            if (rageTriggered || Health.IsDead || Canopy == null
                || (CurrentMode != Mode.Ground && CurrentMode != Mode.OnCable)) return false;
            rageTriggered = true;
            ClearAttacks(false); // Conserva telarañas, saco y crías ya presentes.
            fireHitPending = false;
            travel = Travel.None;
            body.linearVelocity = Vector2.zero;
            Health.IsInvulnerable = true;
            hit.isTrigger = true;
            CurrentMode = Mode.Enraging;
            rageStage = RageStage.Retreat;
            rageTime = 0f;
            cableIndex = NearestCable(body.position);
            return true;
        }

        bool TickRage(float dt)
        {
            if (!IsEnraging) return false;
            rageTime += dt;
            switch (rageStage)
            {
                case RageStage.Retreat:
                    if (cableIndex >= 0 && CableAvailable(cableIndex))
                    {
                        // Primero alcanza el cable; luego corre hacia la rama y el embudo.
                        Vector2 onCable = ClosestOnSegment(body.position,
                            starts[cableIndex] - Vector2.up * cableHeights[cableIndex],
                            ends[cableIndex] - Vector2.up * cableHeights[cableIndex]);
                        if (VisualHeight < cableHeights[cableIndex])
                        {
                            body.position = Vector2.MoveTowards(body.position, onCable, CableSpeed * dt);
                            SetHeight(Mathf.MoveTowards(VisualHeight, cableHeights[cableIndex], cableHeights[cableIndex] / ClimbSeconds * dt));
                            Play("Climb", ClimbMs);
                            break;
                        }
                        Vector2 destination = ProjectCablePointToGround(cableIndex, starts[cableIndex]);
                        facing = (destination - body.position).normalized;
                        body.position = Vector2.MoveTowards(body.position, destination, CableSpeed * dt);
                        Play("Walk", WalkMs);
                        if (Vector2.Distance(body.position, destination) > CableSpeed * dt) break;
                    }
                    else
                    {
                        cableIndex = -1;
                        // Sin cable, baja y cruza por el suelo a la velocidad normal.
                        if (VisualHeight > 0f)
                        {
                            SetHeight(Mathf.MoveTowards(VisualHeight, 0f, FunnelHeight / DropSeconds * dt));
                            Play("Drop", DropMs);
                            break;
                        }
                        Vector2 destination = ArenaCenter + Vector2.down * GroundApproach;
                        facing = (destination - body.position).normalized;
                        body.position = Vector2.MoveTowards(body.position, destination, CableSpeed * dt);
                        Play("Walk", WalkMs);
                        if (Vector2.Distance(body.position, destination) > CableSpeed * dt) break;
                    }
                    rageStage = RageStage.Funnel;
                    break;
                case RageStage.Funnel:
                    if (cableIndex < 0)
                    {
                        float height = FunnelHeight + GroundApproach;
                        SetHeight(Mathf.MoveTowards(VisualHeight, height, height / ClimbSeconds * dt));
                        Play("Climb", ClimbMs);
                        if (VisualHeight < height) break;
                    }
                    Vector2 funnel = NestPosition - Vector2.up * VisualHeight;
                    facing = (funnel - body.position).normalized;
                    body.position = Vector2.MoveTowards(body.position, funnel, CableSpeed * dt);
                    Play("Walk", WalkMs);
                    if (Vector2.Distance(body.position, funnel) > CableSpeed * dt) break;
                    body.position = funnel;
                    thread.enabled = false;
                    if (bodyRenderer != null) bodyRenderer.enabled = false;
                    if (shadowRenderer != null) shadowRenderer.enabled = false;
                    rageStage = RageStage.Screech;
                    rageTime = 0f;
                    ScreenShake.Pulse(ShakePixels, ShakeSeconds);
                    break;
                case RageStage.Screech:
                    if (bodyRenderer != null)
                    {
                        bodyRenderer.enabled = true;
                        bodySprite.Play("Megarachne_Screech", ScreechMs, true);
                    }
                    ShowNestTree(rageTime < ScreechSeconds * 0.5f ? "Nido_Tree_Shake" : "Nido_Tree_Drying",
                        rageTime < ScreechSeconds * 0.5f ? rageTime / FxFrameSeconds :
                        (rageTime / ScreechSeconds * 2f - 1f) * DryingFrames, rageTime < ScreechSeconds * 0.5f);
                    ShowFireSprite(ref nestRain, "Fx_Nido_Rain", ArenaCenter, rageTime);
                    ShowFireSprite(ref screechWave, "Fx_Megarachne_Screech", NestPosition, rageTime);
                    if (rageTime < ScreechSeconds) break;
                    IsRage = true;
                    nextAttack = AttackKind.Jump;
                    rageAttackSlot = 0;
                    ShowNestTree("Nido_Tree_Dry", 0f, false);
                    nestRain.enabled = screechWave.enabled = false;
                    foreach (var cable in cableFire) cable.nestLimit = 0f;
                    AddRageCables();
                    rageStage = RageStage.Emerge;
                    rageTime = 0f;
                    if (bodySprite != null) bodySprite.Play("Megarachne_IntroEmerge", EmergeMs, false, restart: true);
                    break;
                case RageStage.Emerge:
                    if (rageTime < EmergeSeconds) break;
                    RestoreRageVisibility();
                    cableIndex = NearestCable(body.position);
                    if (cableIndex >= 0)
                    {
                        body.position = ProjectCablePointToGround(cableIndex, starts[cableIndex]);
                        SetHeight(cableHeights[cableIndex]);
                    }
                    else
                    {
                        body.position = ArenaCenter + Vector2.down * GroundApproach;
                        SetHeight(FunnelHeight + GroundApproach);
                    }
                    routeStart = body.position + Vector2.up * (VisualHeight + SpinneretOffset);
                    dropHeight = VisualHeight;
                    rageStage = RageStage.Descend;
                    rageTime = 0f;
                    break;
                case RageStage.Descend:
                    SetHeight(Mathf.Lerp(dropHeight, 0f, rageTime / DropSeconds));
                    Play("Drop", DropMs);
                    DrawThread(routeStart);
                    if (rageTime < DropSeconds) break;
                    rageStage = RageStage.None;
                    Land();
                    attackClock = 0f;
                    break;
            }
            return true;
        }

        void RestoreRageVisibility()
        {
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            if (shadowRenderer != null) shadowRenderer.enabled = true;
            if (nestRain != null) nestRain.enabled = false;
            if (screechWave != null) screechWave.enabled = false;
        }

        void AddRageCables()
        {
            int first = CableCount;
            Array.Resize(ref starts, first + ExtraCables);
            Array.Resize(ref ends, first + ExtraCables);
            Array.Resize(ref cableHeights, first + ExtraCables);
            Array.Resize(ref cableRoots, first + ExtraCables);
            Array.Resize(ref cableFire, first + ExtraCables);
            // Dos anclas nuevas en la parte sur libre, separadas de los cinco tocones originales.
            for (int n = 0; n < ExtraCables; n++)
            {
                int i = first + n;
                float side = n == 0 ? -1f : 1f;
                var anchor = MakeSprite("Sigillaria_Rage_" + n, stump, new Vector2(side * AnchorSeparation, RageAnchorY) + Vector2.up * StumpLift, 4);
                anchor.gameObject.layer = LayerMask.NameToLayer("Default");
                var bounds = anchor.bounds;
                starts[i] = ArenaCenter + new Vector2(side * RageBranchX, RageBranchY);
                ends[i] = new Vector2(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, AnchorFraction));
                var collider = anchor.gameObject.AddComponent<BoxCollider2D>();
                float height = ends[i].y - bounds.min.y;
                collider.size = new Vector2(StumpWidth, height);
                collider.offset = new Vector2(0f, bounds.min.y - anchor.transform.position.y + height * 0.5f);
                cableHeights[i] = ends[i].y - (bounds.min.y - HitRadius - ContactMargin * 2f);
                var line = MakeLine("Cable_" + i, CableWidth, CableOrder, cableMaterial);
                line.widthCurve = cableFire[0].normal.widthCurve;
                line.SetPosition(0, starts[i]);
                line.SetPosition(1, ends[i]);
                BuildCableNodes(i, line);
                BeginGrowingCable(i, ReweaveSeconds);
            }
        }

        void BeginGrowingCable(int index, float seconds)
        {
            var cable = cableFire[index];
            cable.retired = false;
            cable.state = CableState.Growing;
            cable.age = 0f;
            cable.growSeconds = seconds;
            cable.normal.enabled = false;
            cable.outer ??= MakeLine("CableExterior_" + index, CableWidth, CableOrder, cableMaterial);
            cable.growingMaterial ??= MakeFireMaterial(cableFireArt.growing, GrowFrames);
            cable.outer.sharedMaterial = cable.growingMaterial;
            cable.outer.widthCurve = cable.normal.widthCurve;
            SetCableSegment(cable.outer, starts[index], ends[index]);
            AnimateCable(cable.growingMaterial, GrowFrames, 0);
        }

        public bool IgniteNest()
        {
            if (!isActiveAndEnabled || Health.IsDead || !NestCanBurn) return false;
            NestBurned = true;
            nestTime = 0f;
            ClearAttacks(false);
            for (int i = 0; i < CableCount; i++)
            {
                var cable = cableFire[i];
                cable.low = 0f;
                cable.high = cable.length;
                DrawBurningCable(i, cable);
                cable.state = CableState.Missing;
                cable.age = 0f;
                cable.retired = true;
                foreach (var line in new[] { cable.normal, cable.outer, cable.charred, cable.leftBurn, cable.rightBurn }) line.enabled = false;
                if (cable.leftFront != null) cable.leftFront.enabled = false;
                if (cable.rightFront != null) cable.rightFront.enabled = false;
                foreach (var flame in cable.flames) flame.enabled = false;
                SetCableRootVisible(i, false);
                ShowFireSprite(ref cable.nestStub, "Cable_Stub", starts[i], 0f);
                ShowFireSprite(ref cable.outerStub, "Cable_Stub", ends[i], 0f);
            }
            nestStun = true;
            fireHitPending = true;
            TickFireReaction(0f);
            return true;
        }

        void ShowNestTree(string name, float frame, bool loop)
        {
            Canopy.ShowTree(EffectFrames(name), frame, loop);
            Canopy.ShowTrunk(EffectFrames(name + "_Tronco"), frame, loop);
        }

        void TickNest(float dt)
        {
            if (!NestBurned) return;
            nestTime += dt;
            ShowNestTree(nestTime < NestBurnSeconds ? "Nido_Burn" : "Nido_Tree_Charred_Smolder",
                (nestTime < NestBurnSeconds ? nestTime : nestTime - NestBurnSeconds) / FxFrameSeconds, nestTime >= NestBurnSeconds);
            Canopy.ShowWeb(EffectFrames("Nido_WebDecay"), nestTime / FxFrameSeconds);
            if (postNestWeaveTime < 0f) return;
            postNestWeaveTime -= dt;
            if (postNestWeaveTime > 0f) return;
            postNestWeaveTime = -1f;
            // Reutiliza las dos Sigillarias nuevas; el resto de cables queda retirado.
            for (int i = CableCount - ExtraCables; i < CableCount; i++) BeginGrowingCable(i, PostNestGrowSeconds);
        }
    }
}
