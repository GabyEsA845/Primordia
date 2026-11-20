using System;
using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // Provisionales para probar M4.
        internal const int CableNodeCount = 6;
        internal const float FuseSpeed = 6f, StunSeconds = 3f, StubSeconds = 3f;
        internal const float ReweaveDelay = 10f, ReweaveSeconds = 3f;
        const float EscapeRange = 4f, AshInterval = 1f, AshRadius = 1.2f;
        internal const float SacRegrowSeconds = ArthropleuraBoss.SacRegrowSeconds;
        const float NestFireInset = 0.5f, BurningLength = 2f, FlameSpacing = 1f;
        const float CharredSag = 0.35f;
        const int StubFrames = 4, AshFrames = 6;
        const int BurningFrames = 3, CharredFrames = 3, GrowFrames = 6;
        const float FallMs = 80f, StunnedMs = 140f, RecoverMs = 110f;
        const float FallSeconds = 4 * FallMs / 1000f, RecoverSeconds = 3 * RecoverMs / 1000f;
        const bool NestCanBurn = false;

        internal enum CableState { Intact, Burning, Missing, Growing }
        sealed class CableFire
        {
            public CableState state;
            public float length, low, high, age, nextAsh;
            public float nestLimit;
            public bool smoked;
            public Material growingMaterial;
            public LineRenderer normal, outer, charred, leftBurn, rightBurn;
            public SpriteRenderer leftFront, rightFront;
            public readonly List<SpriteRenderer> flames = new();
            public SpriteRenderer nestStub, outerStub;
        }

        sealed class FallingAsh
        {
            public SpriteRenderer sprite;
            public Vector2 start, ground;
            public float age;
        }

        sealed class FireSacSlot
        {
            public Vector2 position;
            public MethaneSac sac;
            public float regrowAt = float.PositiveInfinity;
        }

        [Serializable]
        internal sealed class CableFireArt
        {
            public Texture2D burning, charred, growing;
            public EffectSheet[] effects = Array.Empty<EffectSheet>();
            public Sprite[] sacIdle = Array.Empty<Sprite>(), sacBurst = Array.Empty<Sprite>();
            public Sprite sacRemains;
        }

        [SerializeField] CableFireArt cableFireArt = new();
        CableFire[] cableFire;
        Material burningCableMaterial, charredCableMaterial;
        readonly List<FallingAsh> fallingAsh = new();
        readonly List<FireSacSlot> fireSacs = new();
        float fireClock, fireFallHeight, fireReactionTime;
        bool fireHitPending;
        SpriteRenderer stunEffect;

        internal bool IsFireStunned => CurrentMode == Mode.Stunned;
        internal CableState FireStateAt(int i) => cableFire[i].state;
        internal Vector2 FireFrontsAt(int i) => new(cableFire[i].low, cableFire[i].high);
        internal int CurrentCable => cableIndex;
        internal Vector2 ProjectCablePointToGround(int i, Vector2 point) => point - Vector2.up * cableHeights[i];
        internal void ConfigureFire(CableFireArt value) => cableFireArt = value;

        void BuildCableFire()
        {
            cableFire = new CableFire[CableCount];
            burningCableMaterial = MakeFireMaterial(cableFireArt.burning, BurningFrames);
            charredCableMaterial = MakeFireMaterial(cableFireArt.charred, CharredFrames);
        }

        Material MakeFireMaterial(Texture2D texture, int frames)
        {
            if (cableMaterial == null || texture == null) return null;
            return new Material(cableMaterial)
            {
                mainTexture = texture,
                mainTextureScale = new Vector2(0.25f, 1f / frames)
            };
        }

        void BuildCableNodes(int i, LineRenderer line)
        {
            float length = Vector2.Distance(starts[i], ends[i]);
            var cable = new CableFire
            {
                normal = line, length = length, nestLimit = NestCanBurn ? 0f : Mathf.Min(NestFireInset, length)
            };
            cableFire[i] = cable;
            for (int node = 0; node < CableNodeCount; node++)
            {
                float distance = Mathf.Lerp(cable.nestLimit, length, (float)node / (CableNodeCount - 1));
                var anchor = new GameObject("Nodo_" + node);
                anchor.transform.SetParent(line.transform, false);
                anchor.transform.position = CablePoint(i, distance);
                anchor.AddComponent<MegarachneCableNode>().Configure(this, i, distance);
            }
        }

        bool CableAvailable(int i) => i >= 0 && i < CableCount && cableFire[i].state == CableState.Intact;

        internal bool CanIgniteCable(int i) => isActiveAndEnabled && !Health.IsDead && CableAvailable(i);

        Vector2 CablePoint(int i, float distance) => starts[i] + (ends[i] - starts[i]).normalized * distance;

        float AlongCable(int i, Vector2 point) =>
            Mathf.Clamp(Vector2.Dot(point - starts[i], (ends[i] - starts[i]).normalized), 0f, cableFire[i].length);

        internal bool IgniteCable(int i, float distance)
        {
            if (!CanIgniteCable(i)) return false;
            var cable = cableFire[i];
            cable.state = CableState.Burning;
            cable.low = cable.high = Mathf.Clamp(distance, cable.nestLimit, cable.length);
            cable.age = 0f;
            cable.nextAsh = AshInterval;
            cable.smoked = false;
            return true;
        }

        internal bool IgniteOccupiedCable() => CurrentMode == Mode.OnCable && travel == Travel.Run
            && IgniteCable(cableIndex, BossDistanceOnCable());

        void IgniteNearPlayer()
        {
            if (target == null) return;
            int i = NearestCable(target.position);
            if (i >= 0) IgniteCable(i, cableFire[i].length * 0.5f);
        }

        float BossDistanceOnCable() => AlongCable(cableIndex, body.position + Vector2.up * cableHeights[cableIndex]);

        void TickFire(float dt)
        {
            if (cableFire == null) return;
            fireClock += dt;
            TickFireSacs();
            TickAsh(dt);
            for (int i = 0; i < CableCount; i++)
            {
                var cable = cableFire[i];
                if (cable.state == CableState.Intact) continue;
                cable.age += dt;
                switch (cable.state)
                {
                    case CableState.Burning:
                        TickBurningCable(i, cable, dt);
                        break;
                    case CableState.Missing:
                        if (cable.age >= StubSeconds)
                        {
                            if (cable.nestStub != null) cable.nestStub.enabled = false;
                            if (cable.outerStub != null) cable.outerStub.enabled = false;
                        }
                        else
                        {
                            float age = cable.age / StubSeconds * StubFrames * FxFrameSeconds;
                            Frame(cable.nestStub, "Cable_Stub", age, false);
                            Frame(cable.outerStub, "Cable_Stub", age, false);
                        }
                        if (cable.age >= ReweaveDelay)
                        {
                            cable.state = CableState.Growing;
                            cable.age = 0f;
                            if (cable.growingMaterial == null) cable.growingMaterial = MakeFireMaterial(cableFireArt.growing, GrowFrames);
                            cable.outer.sharedMaterial = cable.growingMaterial;
                            cable.outer.widthCurve = cable.normal.widthCurve;
                        }
                        break;
                    case CableState.Growing:
                        SetCableSegment(cable.outer, cable.normal.GetPosition(0), ends[i]);
                        AnimateCable(cable.growingMaterial, GrowFrames, Mathf.Min(GrowFrames - 1,
                            Mathf.FloorToInt(cable.age / ReweaveSeconds * GrowFrames)));
                        if (cable.age >= ReweaveSeconds)
                        {
                            cable.state = CableState.Intact;
                            cable.outer.enabled = false;
                            SetCableSegment(cable.normal, cable.normal.GetPosition(0), ends[i]);
                            SetCableRootVisible(i, true);
                        }
                        break;
                }
            }
        }

        void TickBurningCable(int i, CableFire cable, float dt)
        {
            cable.low = Mathf.Max(cable.nestLimit, cable.low - FuseSpeed * dt);
            cable.high = Mathf.Min(cable.length, cable.high + FuseSpeed * dt);
            if (cableIndex == i && CurrentMode == Mode.OnCable && travel == Travel.Run && attack != AttackStage.JumpFall)
            {
                float position = BossDistanceOnCable();
                if (position >= cable.low && position <= cable.high) fireHitPending = true;
            }
            if (!NestCanBurn && !cable.smoked && cable.low <= cable.nestLimit)
            {
                cable.smoked = true;
                SpawnFx("Fx_CableSmoke", CablePoint(i, cable.low), AirFxOrder);
            }
            DrawBurningCable(i, cable);
            if (cable.low > cable.nestLimit || cable.high < cable.length) return;

            cable.state = CableState.Missing;
            cable.age = 0f;
            cable.normal.enabled = cable.outer.enabled = cable.charred.enabled = false;
            cable.leftBurn.enabled = cable.rightBurn.enabled = false;
            if (cable.leftFront != null) cable.leftFront.enabled = false;
            if (cable.rightFront != null) cable.rightFront.enabled = false;
            foreach (var flame in cable.flames) flame.enabled = false;
            SetCableRootVisible(i, false);
            ShowFireSprite(ref cable.nestStub, "Cable_Stub", starts[i], 0f);
            ShowFireSprite(ref cable.outerStub, "Cable_Stub", ends[i], 0f);
        }

        void DrawBurningCable(int i, CableFire cable)
        {
            cable.outer ??= MakeLine("CableExterior_" + i, CableWidth, CableOrder, cableMaterial);
            cable.charred ??= MakeLine("CableCarbonizado_" + i, CableWidth, CableOrder, charredCableMaterial);
            cable.leftBurn ??= MakeLine("CableArdiendoInterior_" + i, CableWidth, CableOrder, burningCableMaterial);
            cable.rightBurn ??= MakeLine("CableArdiendoExterior_" + i, CableWidth, CableOrder, burningCableMaterial);
            cable.outer.sharedMaterial = cableMaterial;
            cable.outer.widthCurve = cable.normal.widthCurve;
            Vector2 low = CablePoint(i, cable.low), high = CablePoint(i, cable.high);
            // El extremo interno conserva la extensión bajo la copa del dibujo original.
            Vector2 root = starts[i] - (ends[i] - starts[i]).normalized * CableRootInset;
            SetCableSegment(cable.normal, root, low);
            SetCableSegment(cable.outer, high, ends[i]);

            bool innerLit = cable.low > cable.nestLimit, outerLit = cable.high < cable.length;
            float span = cable.high - cable.low;
            float leftWidth = innerLit ? Mathf.Min(BurningLength, outerLit ? span / 2f : span) : 0f;
            float rightWidth = outerLit ? Mathf.Min(BurningLength, innerLit ? span / 2f : span) : 0f;
            float charredStart = cable.low + leftWidth, charredEnd = cable.high - rightWidth;
            if (cable.age >= cable.nextAsh && charredEnd > charredStart)
            {
                cable.nextAsh += AshInterval;
                DropAsh(i, Random.Range(charredStart, charredEnd));
            }
            Vector2 left = CablePoint(i, charredStart), right = CablePoint(i, charredEnd);
            SetCableSegment(cable.leftBurn, low, left);
            SetCableSegment(cable.rightBurn, right, high);
            cable.charred.positionCount = 3;
            cable.charred.enabled = (right - left).sqrMagnitude > Mathf.Epsilon;
            cable.charred.SetPosition(0, left);
            cable.charred.SetPosition(1, (left + right) * 0.5f + Vector2.down * CharredSag);
            cable.charred.SetPosition(2, right);
            int frame = Mathf.FloorToInt(fireClock / FxFrameSeconds);
            AnimateCable(burningCableMaterial, BurningFrames, frame % BurningFrames);
            AnimateCable(charredCableMaterial, CharredFrames, frame % CharredFrames);

            if (innerLit) ShowFireSprite(ref cable.leftFront, "Fx_CableFront", low, cable.age);
            else if (cable.leftFront != null) cable.leftFront.enabled = false;
            if (outerLit) ShowFireSprite(ref cable.rightFront, "Fx_CableFront", high, cable.age);
            else if (cable.rightFront != null) cable.rightFront.enabled = false;

            int count = DrawCableFlames(cable, low, left, leftWidth, 0);
            count = DrawCableFlames(cable, right, high, rightWidth, count);
            for (int j = count; j < cable.flames.Count; j++) cable.flames[j].enabled = false;
        }

        int DrawCableFlames(CableFire cable, Vector2 from, Vector2 to, float length, int index)
        {
            int count = Mathf.CeilToInt(length / FlameSpacing);
            for (int j = 0; j < count; j++, index++)
            {
                if (index == cable.flames.Count) cable.flames.Add(null);
                var flame = cable.flames[index];
                ShowFireSprite(ref flame, "Fx_CableFlame", Vector2.Lerp(from, to, (j + 0.5f) / count), cable.age);
                cable.flames[index] = flame;
            }
            return index;
        }

        static void SetCableSegment(LineRenderer line, Vector2 from, Vector2 to)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.enabled = (to - from).sqrMagnitude > Mathf.Epsilon;
        }

        static void AnimateCable(Material material, int frames, int frame)
        {
            if (material != null) material.mainTextureOffset = new Vector2(0f, (frames - 1 - frame) / (float)frames);
        }

        void ShowFireSprite(ref SpriteRenderer renderer, string effect, Vector2 position, float age)
        {
            if (renderer == null) renderer = CreateFx(effect, position, AirFxOrder);
            renderer.enabled = true;
            renderer.transform.position = position;
            Frame(renderer, effect, age);
        }

        void DropAsh(int i, float distance)
        {
            Vector2 point = CablePoint(i, distance);
            var ash = new FallingAsh { start = point, ground = ProjectCablePointToGround(i, point) };
            ShowFireSprite(ref ash.sprite, "Fx_CableAsh", point, 0f);
            fallingAsh.Add(ash);
        }

        void TickAsh(float dt)
        {
            for (int i = fallingAsh.Count - 1; i >= 0; i--)
            {
                var ash = fallingAsh[i];
                ash.age += dt;
                float progress = ash.age / (AshFrames * FxFrameSeconds);
                ash.sprite.transform.position = Vector2.Lerp(ash.start, ash.ground, progress * progress);
                Frame(ash.sprite, "Fx_CableAsh", ash.age, false);
                if (progress < 1f) continue;
                Flammables.IgniteNear(ash.ground, AshRadius);
                Destroy(ash.sprite.gameObject);
                fallingAsh.RemoveAt(i);
            }
        }

        bool TryFleeCable(float dt)
        {
            var cable = cableFire[cableIndex];
            if (cable.state != CableState.Burning) return false;
            float position = BossDistanceOnCable();
            bool escapeOutward = position > cable.high;
            float gap = escapeOutward ? position - cable.high : cable.low - position;
            if (gap >= EscapeRange) return false;

            Vector2 destination = (escapeOutward ? ends[cableIndex] : starts[cableIndex]) - Vector2.up * cableHeights[cableIndex];
            Vector2 delta = destination - body.position;
            if (delta.sqrMagnitude > Mathf.Epsilon) facing = delta.normalized;
            body.MovePosition(Vector2.MoveTowards(body.position, destination, CableSpeed * dt));
            Play("Walk", WalkMs);
            if (delta.magnitude <= CableSpeed * dt)
            {
                int next = NearestCable(destination);
                if (!escapeOutward && next >= 0) BeginCross(destination, next);
                else ForceDrop();
            }
            return true;
        }

        bool TickFireReaction(float dt)
        {
            if (fireHitPending)
            {
                fireHitPending = false;
                fireFallHeight = VisualHeight;
                fireReactionTime = 0f;
                body.linearVelocity = Vector2.zero;
                travel = Travel.None;
                thread.enabled = false;
                if (fireFallHeight > 0f)
                {
                    CurrentMode = Mode.Falling;
                    Play("Fall", FallMs);
                }
                else BeginStun();
                return true;
            }
            if (CurrentMode == Mode.OnCable || CurrentMode == Mode.Ground) return false;
            fireReactionTime += dt;
            switch (CurrentMode)
            {
                case Mode.Falling:
                    SetHeight(Mathf.Lerp(fireFallHeight, 0f, fireReactionTime / FallSeconds));
                    if (fireReactionTime >= FallSeconds)
                    {
                        fireReactionTime = 0f;
                        BeginStun();
                    }
                    break;
                case Mode.Stunned:
                    Play("Stunned", StunnedMs);
                    ShowFireSprite(ref stunEffect, "Fx_Megarachne_Stunned", body.position + Vector2.up * BodyScale * 2f, fireReactionTime);
                    if (fireReactionTime >= StunSeconds)
                    {
                        stunEffect.enabled = false;
                        CurrentMode = Mode.Recovering;
                        fireReactionTime = 0f;
                    }
                    break;
                case Mode.Recovering:
                    Play("Recover", RecoverMs);
                    if (fireReactionTime >= RecoverSeconds)
                    {
                        Land();
                        groundedTime = VulnerableSeconds;
                        ForceClimb();
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        void BeginStun()
        {
            CurrentMode = Mode.Stunned;
            Health.IsInvulnerable = false;
            hit.isTrigger = false;
            Play("Stunned", StunnedMs);
            ShowFireSprite(ref stunEffect, "Fx_Megarachne_Stunned", body.position + Vector2.up * BodyScale * 2f, 0f);
        }

        void BuildFireSacs()
        {
            if (cableFireArt.sacIdle.Length == 0) return;
            // Entre el tronco y los tocones, bajo los cables laterales y lejos de las paredes.
            foreach (var offset in new[] { new Vector2(-7f, 4.5f), new Vector2(7f, 4.5f), new Vector2(-8f, -2f) })
            {
                var slot = new FireSacSlot { position = ArenaCenter + offset };
                fireSacs.Add(slot);
                GrowFireSac(slot);
            }
        }

        void OnFireSacBurst(MethaneSac sac)
        {
            foreach (var slot in fireSacs)
                if (slot.sac == sac) slot.regrowAt = fireClock + SacRegrowSeconds;
        }

        void GrowFireSac(FireSacSlot slot)
        {
            if (slot.sac != null) Destroy(slot.sac.gameObject);
            slot.sac = MethaneSac.Create(scenery.transform, slot.position, cableFireArt.sacIdle, cableFireArt.sacBurst, cableFireArt.sacRemains);
            slot.regrowAt = float.PositiveInfinity;
        }

        void TickFireSacs()
        {
            foreach (var slot in fireSacs)
            {
                if (slot.sac.IsSpent && float.IsPositiveInfinity(slot.regrowAt))
                    slot.regrowAt = fireClock + SacRegrowSeconds;
                if (fireClock >= slot.regrowAt) GrowFireSac(slot);
            }
        }
    }
}
