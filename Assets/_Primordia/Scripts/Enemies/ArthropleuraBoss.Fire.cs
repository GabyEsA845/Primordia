using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class ArthropleuraBoss
    {
        // Provisionales: apertura, enfriamiento y colocación del decorado.
        public const float PlatesOpenSeconds = 6f, CharredCoolSeconds = 25f;
        public const float FuseSeconds = 0.4f, PieceBurnPerSecond = 1.5f, BurnCapPerSecond = 6f;
        public const float SacRegrowSeconds = 20f, SacIgnitionRadius = 2.5f;
        const float PlateFrameSeconds = 0.09f, PlateTransitionSeconds = 4 * PlateFrameSeconds;
        const float IgniteFrameSeconds = 0.05f;
        const float DebugHealthFraction = 0.71f;

        public enum FireState { Normal, Burning, Charred }
        [Serializable]
        public class ClearingArt
        {
            public Sprite[] sacIdle = Array.Empty<Sprite>(), sacBurst = Array.Empty<Sprite>(), ignite = Array.Empty<Sprite>();
            public Sprite sacRemains;
            public Sprite[] decor = Array.Empty<Sprite>();
            public Vector2[] decorPositions = Array.Empty<Vector2>();
        }

        [SerializeField] ClearingArt clearingArt = new();
        readonly List<FirePiece> firePieces = new();
        readonly List<SacSlot> sacs = new();
        BossDamageLimiter fireLimiter;
        float fireClock, burnFraction;
        bool reachedPhase2;
        public event Action ReachedPhase2;
        public bool Phase2Reached => reachedPhase2;
        internal void ConfigureClearing(ClearingArt value) => clearingArt = value;

        sealed class FirePiece : IFlammable
        {
            public ArthropleuraBoss owner;
            public int index;
            public FireState state;
            public float started, cooledAt, spreadAt = float.PositiveInfinity;
            public bool charred;
            public SpriteRenderer burning, charcoal, cracks;
            public Vector2 Position => owner.GroundToWorld(owner.NodeGround(index));
            public bool CanIgnite => owner.isActiveAndEnabled && owner.FireAllowed && !owner.reachedPhase2 &&
                owner.InClearing(Position - owner.center) && Ready;
            public bool Ready => state != FireState.Burning && owner.fireClock >= cooledAt;
            public bool Open => state == FireState.Burning && owner.fireClock >= started + PlateTransitionSeconds &&
                owner.fireClock < started + PlateTransitionSeconds + PlatesOpenSeconds;
            public void Ignite() { if (CanIgnite) owner.LightPiece(index); }
        }

        sealed class SacSlot
        {
            public Vector2 position;
            public MethaneSac sac;
            public float regrowAt = float.PositiveInfinity;
        }

        void BuildFire()
        {
            fireLimiter = GetComponent<BossDamageLimiter>();
            if (fireLimiter == null) fireLimiter = gameObject.AddComponent<BossDamageLimiter>();
            ConfigureBurnCap();
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].renderer.sortingOrder *= 4;
                var piece = new FirePiece { owner = this, index = i };
                piece.charcoal = NewLayer(i, "Charred", 1);
                piece.cracks = NewLayer(i, "Cracks1", 2);
                piece.burning = NewLayer(i, "Burning", 3);
                firePieces.Add(piece);
                Flammables.Register(piece);
            }
            // Fuera del eje central y a >= 4,5 u de la espesura (si no, se solapan con su borde); posiciones relativas al claro.
            foreach (var position in new[] { new Vector2(-8f, -3.5f), new Vector2(8f, 3.5f), new Vector2(-4f, 3.5f), new Vector2(4f, -3.5f) })
            {
                var slot = new SacSlot { position = center + position };
                sacs.Add(slot);
                GrowSac(slot);
            }
            for (int i = 0; i < clearingArt.decor.Length; i++)
            {
                var decor = new GameObject("DecoradoF1").AddComponent<SpriteRenderer>();
                decor.transform.SetParent(transform, false);
                decor.transform.position = center + clearingArt.decorPositions[i];
                decor.sprite = clearingArt.decor[i];
                decor.sortingOrder = 2;
                // Decoración sin colisión ni IFlammable: los helechos de F1 no arden.
            }
        }

        void ConfigureBurnCap()
        {
            var caps = BossDamageLimiter.DefaultCaps();
            for (int i = 0; i < caps.Length; i++)
                if (caps[i].source == DamageSource.Burn) caps[i].perSecond = BurnCapPerSecond;
            fireLimiter.Configure(caps);
        }

        SpriteRenderer NewLayer(int index, string label, int offset)
        {
            var layer = new GameObject(label).AddComponent<SpriteRenderer>();
            layer.transform.SetParent(pieces[index].renderer.transform, false);
            layer.sortingOrder = pieces[index].renderer.sortingOrder + offset;
            layer.enabled = false;
            return layer;
        }

        void EnableFire()
        {
            Health.Changed += CheckPhase2;
            MethaneSac.BurstStarted += SacBurstStarted;
            foreach (var piece in firePieces) Flammables.Register(piece);
        }

        void DisableFire()
        {
            Health.Changed -= CheckPhase2;
            MethaneSac.BurstStarted -= SacBurstStarted;
            foreach (var piece in firePieces) Flammables.Unregister(piece);
        }

        void SacBurstStarted(MethaneSac sac)
        {
            foreach (var slot in sacs)
                if (slot.sac == sac) slot.regrowAt = fireClock + SacRegrowSeconds;
            // Explosion.Detonate prende IFlammable en su cuadro de impacto, con radio 2,5 u.
        }

        void GrowSac(SacSlot slot)
        {
            if (slot.sac != null) Destroy(slot.sac.gameObject);
            slot.sac = MethaneSac.Create(transform, slot.position, clearingArt.sacIdle, clearingArt.sacBurst, clearingArt.sacRemains);
            slot.regrowAt = float.PositiveInfinity;
            SetSacsAvailable(FireAllowed);
        }

        public bool IgnitePiece(int index)
        {
            if (index < 0 || index >= firePieces.Count || !firePieces[index].CanIgnite) return false;
            LightPiece(index);
            return true;
        }

        void LightPiece(int index)
        {
            if (index < 0 || index >= firePieces.Count || reachedPhase2) return;
            var piece = firePieces[index];
            if (!piece.Ready) return;
            piece.state = FireState.Burning;
            piece.started = fireClock;
            piece.spreadAt = fireClock + FuseSeconds;
            if (!IsHidden(NodeGround(index)))
                SpawnEffect(clearingArt.ignite, piece.Position, 0f, IgniteFrameSeconds);
        }

        internal FireState PieceFire(int index) => firePieces[index].state;
        internal bool PieceOpen(int index) => firePieces[index].Open;
        internal MethaneSac SacAt(int index) => sacs[index].sac;

        internal void TickFire(float dt)
        {
            fireClock += dt;
            int open = 0;
            foreach (var piece in firePieces)
            {
                if (piece.state != FireState.Burning) continue;
                if (fireClock >= piece.spreadAt)
                {
                    piece.spreadAt = float.PositiveInfinity;
                    LightPiece(piece.index - 1);
                    LightPiece(piece.index + 1);
                }
                if (piece.Open) open++;
                if (fireClock >= piece.started + PlatesOpenSeconds + 2 * PlateTransitionSeconds)
                {
                    piece.state = FireState.Charred;
                    piece.charred = true;
                    piece.cooledAt = fireClock + CharredCoolSeconds;
                }
            }
            if (!reachedPhase2 && open > 0)
            {
                burnFraction += open * PieceBurnPerSecond * dt;
                int requested = Mathf.FloorToInt(burnFraction);
                burnFraction -= requested; // El exceso de la limitadora se descarta, no se acumula.
                int allowed = fireLimiter.Apply(requested, DamageSource.Burn, fireClock);
                int untilPhase2 = Mathf.Max(0, Health.Current - Mathf.RoundToInt(Health.Max * Thresholds[0]));
                Health.Drain(Mathf.Min(allowed, untilPhase2));
            }
            else burnFraction = 0f;
            foreach (var slot in sacs)
            {
                // También recupera odres desinflados por gas para no agotar los recursos.
                if (slot.sac != null && slot.sac.IsSpent && float.IsPositiveInfinity(slot.regrowAt))
                    slot.regrowAt = fireClock + SacRegrowSeconds;
                if (!reachedPhase2 && fireClock >= slot.regrowAt) GrowSac(slot);
            }
            if (reachedPhase2 && CurrentMode != Mode.Charge && CurrentMode != Mode.Phase2Pending)
            {
                HideAim();
                SetMode(Mode.Phase2Pending);
                ClearBlockers();
            }
        }

        void CheckPhase2(int current, int max)
        {
            if (reachedPhase2 || current > Mathf.RoundToInt(max * Thresholds[0])) return;
            reachedPhase2 = true;
            ClearBlockers();
            ReachedPhase2?.Invoke();
        }

        DirectionalArt.Sheet FireSheet(int index, out int frame)
        {
            frame = 0;
            var p = firePieces[index];
            // El arte aprobado solo tiene placas articuladas para el tronco; cabeza y cola conservan su hoja propia.
            if (p.state != FireState.Burning || index == 0 || index == pieces.Count - 1) return null;
            float elapsed = fireClock - p.started;
            string pose;
            if (elapsed < PlateTransitionSeconds)
            {
                pose = "PlatesOpen";
                frame = Mathf.FloorToInt(elapsed / PlateFrameSeconds);
            }
            else if (p.Open)
            {
                pose = "OpenWalk";
                frame = Moving && CurrentMode != Mode.Phase2Pending ? Mathf.FloorToInt(elapsed / PlateFrameSeconds) % 6 : 0;
            }
            else
            {
                pose = "PlatesClose";
                frame = Mathf.Clamp(Mathf.FloorToInt((elapsed - PlateTransitionSeconds - PlatesOpenSeconds) / PlateFrameSeconds), 0, 3);
            }
            return art?.Find("Arthropleura_Segment_" + pose);
        }

        void PaintFire(int index, DirectionalArt.Sheet baseSheet, int row, int frame, bool flip, bool hidden)
        {
            var p = firePieces[index];
            // Las capas existentes corresponden a la hoja cerrada, incluso durante la apertura del tronco.
            bool openPose = p.state == FireState.Burning;
            PaintLayer(p.burning, baseSheet, "Burning", p.state == FireState.Burning && !hidden, row, frame, flip);
            PaintLayer(p.charcoal, baseSheet, "Charred", p.charred && !openPose && !hidden, row, frame, flip);
            PaintLayer(p.cracks, baseSheet, "Cracks1", reachedPhase2 && !openPose && !hidden, row, frame, flip);
        }

        void PaintLayer(SpriteRenderer renderer, DirectionalArt.Sheet sheet, string suffix, bool visible, int row, int frame, bool flip)
        {
            var layer = visible && sheet != null ? art?.Find(sheet.name + "_" + suffix) : null;
            renderer.enabled = layer != null;
            renderer.sprite = layer?.Get(row, frame % layer.frames);
            renderer.flipX = flip;
        }

        public void BurstAllSacs() { if (!FireAllowed) return; foreach (var slot in sacs) if (slot.sac != null) slot.sac.Ignite(); }
        public void ResetFire()
        {
            foreach (var p in firePieces)
            {
                p.state = FireState.Normal;
                p.charred = false;
                p.cooledAt = 0f;
                p.spreadAt = float.PositiveInfinity;
            }
            burnFraction = 0f;
            ConfigureBurnCap();
        }

        public string DebugFireAction(int action, Vector2 position)
        {
            if (action == 0)
            {
                int closest = -1;
                float distance = float.PositiveInfinity;
                foreach (var p in firePieces)
                    if (p.CanIgnite && (p.Position - position).sqrMagnitude < distance)
                    { closest = p.index; distance = (p.Position - position).sqrMagnitude; }
                return IgnitePiece(closest) ? $"Arthropleura: pieza {closest} encendida" : "No hay piezas disponibles dentro del claro";
            }
            if (action == 1) Health.Drain(Mathf.Max(0, Health.Current - Mathf.RoundToInt(Health.Max * DebugHealthFraction)));
            if (action == 2) BurstAllSacs();
            if (action == 3) ResetFire();
            return "Arthropleura: acción de fuego " + action;
        }
    }
}
