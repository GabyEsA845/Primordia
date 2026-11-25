using Primordia.Audio;
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
        /// <summary>El fuego puede prender y dañar en F1 y F2, salvo durante la transición del 70 %.</summary>
        bool FireOpen => PhaseIndex <= 2 && CurrentMode != Mode.Phase2Pending && CurrentMode != Mode.Phase3Pending;
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
            public bool CanIgnite => owner.isActiveAndEnabled && owner.FireAllowed && owner.FireOpen &&
                (owner.PhaseIndex >= 1 || owner.InClearing(Position - owner.center)) && Ready;
            public bool Ready => state != FireState.Burning && owner.fireClock >= cooledAt;
            public float forcedStart = -1000f, forcedSeconds;
            /// <summary>Tramo que se abre solo (F3): hay animación de placas aunque no arda.</summary>
            public bool Forced => owner.fireClock >= forcedStart && owner.fireClock < forcedStart + 2 * PlateTransitionSeconds + forcedSeconds;
            public bool Open => ForcedOpen || state == FireState.Burning && owner.fireClock >= started + PlateTransitionSeconds &&
                owner.fireClock < started + PlateTransitionSeconds + PlatesOpenSeconds;
            public bool SectionOpen => ForcedOpen;
            bool ForcedOpen => owner.fireClock >= forcedStart + PlateTransitionSeconds && owner.fireClock < forcedStart + PlateTransitionSeconds + forcedSeconds;
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
            Health.Changed += CheckPhase3;
            Health.Changed += CheckPhase4;
            MethaneSac.BurstStarted += SacBurstStarted;
            foreach (var piece in firePieces) Flammables.Register(piece);
        }

        void DisableFire()
        {
            Health.Changed -= CheckPhase2;
            Health.Changed -= CheckPhase3;
            Health.Changed -= CheckPhase4;
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
            if (index < 0 || index >= firePieces.Count || !FireOpen) return;
            var piece = firePieces[index];
            if (!piece.Ready) return;
            piece.state = FireState.Burning;
            Sfx.Play(Sounds.BossArthropleuraIgnite);
            Sfx.Play(Sounds.BossArthropleuraHiss);
            Sfx.Play(Sounds.BossArthropleuraPlateOpen);
            piece.started = fireClock;
            piece.spreadAt = fireClock + FuseSeconds;
            if (!IsHidden(NodeGround(index)))
                SpawnEffect(clearingArt.ignite, piece.Position, 0f, IgniteFrameSeconds);
        }

        internal FireState PieceFire(int index) => firePieces[index].state;
        internal bool PieceOpen(int index) => firePieces[index].Open;
        internal MethaneSac SacAt(int index) => sacs[index].sac;

        // F3: cada tanto se abre solo un tramo de 2-3 piezas (carne rosada que late): el punto débil.
        // Provisionales (a ojo, pendientes de la prueba de la usuaria).
        const float SectionEverySeconds = 4f, SectionOpenSeconds = 5f;
        // En la F3 solo dañan los tramos que se abren solos (el fuego abre placas pero no daña) y más despacio que en F1-F2.
        const float Phase3PieceDamagePerSecond = 0.8f;
        float nextSectionAt;
        public int LastSectionStart { get; private set; } = -1;
        public int LastSectionLength { get; private set; }
        public int SectionsOpened { get; private set; }

        void TickSections()
        {
            if (PhaseIndex < 2 || !transition3Started || CurrentMode == Mode.Phase3Pending || fireClock < nextSectionAt)
                return;
            OpenSection(UnityEngine.Random.Range(2, 4));
        }

        void PlayPlateClose() => Sfx.Play(Sounds.BossArthropleuraPlateClose);

        void OpenSection(int length)
        {
            // Solo el tronco (1..28): la cabeza y la cola tienen hoja propia sin placas articuladas.
            int start = UnityEngine.Random.Range(1, pieces.Count - 1 - length + 1);
            for (int i = start; i < start + length; i++)
            {
                firePieces[i].forcedStart = fireClock;
                firePieces[i].forcedSeconds = SectionOpenSeconds;
            }
            // Un tramo de la F3 abre y cierra sus placas: el cierre cae al acabar el tiempo abierto.
            Sfx.Play(Sounds.BossArthropleuraPlateOpen);
            Invoke(nameof(PlayPlateClose), SectionOpenSeconds + PlateTransitionSeconds);
            LastSectionStart = start;
            LastSectionLength = length;
            SectionsOpened++;
            nextSectionAt = fireClock + SectionEverySeconds;
        }

        internal void TickFire(float dt)
        {
            fireClock += dt;
            TickScenery();
            ExtinguishInMud();
            TickTrees();
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
                    Sfx.Play(Sounds.BossArthropleuraPlateClose);
                    piece.charred = true;
                    piece.cooledAt = fireClock + CharredCoolSeconds;
                }
            }
            foreach (var forced in firePieces)
                if (forced.state != FireState.Burning && forced.Open) open++;
            TickSections();
            if (PhaseIndex >= 2)
            {
                open = 0;
                foreach (var section in firePieces)
                    if (section.SectionOpen) open++;
            }
            if (FireOpen && open > 0)
            {
                burnFraction += open * (PhaseIndex >= 2 ? Phase3PieceDamagePerSecond : PieceBurnPerSecond) * dt;
                int requested = Mathf.FloorToInt(burnFraction);
                burnFraction -= requested; // El exceso de la limitadora se descarta, no se acumula.
                int allowed = fireLimiter.Apply(requested, DamageSource.Burn, fireClock);
                int untilPhase2 = Mathf.Max(0, Health.Current - Mathf.RoundToInt(Health.Max * Thresholds[Mathf.Min(PhaseIndex, 2)]));
                int drained = Mathf.Min(allowed, untilPhase2);
                if (drained > 0)
                    Sfx.Play(Sounds.BossArthropleuraHurt);
                Health.Drain(drained);
            }
            else burnFraction = 0f;
            foreach (var slot in sacs)
            {
                // También recupera odres desinflados por gas para no agotar los recursos.
                if (slot.sac != null && slot.sac.IsSpent && float.IsPositiveInfinity(slot.regrowAt))
                    slot.regrowAt = fireClock + SacRegrowSeconds;
                if (PhaseIndex <= 1 && fireClock >= slot.regrowAt)
                {
                    GrowSac(slot);
                    Sfx.Play(Sounds.MethaneGasBagRegrow);
                }
            }
            UpdateFuseSound();
            if (reachedPhase2 && !transitionStarted && CurrentMode != Mode.Charge)
            {
                BeginTransition();
            }
            if (reachedPhase3 && !transition3Started && CurrentMode != Mode.Charge && CurrentMode != Mode.Phase2Pending)
                BeginTransition3();
            if (reachedPhase4 && !transition4Started && transition3Started && CurrentMode != Mode.Charge && CurrentMode != Mode.Phase3Pending)
                BeginTransition4();
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
            bool burning = p.state == FireState.Burning;
            if (!burning && !p.Forced || index == 0 || index == pieces.Count - 1) return null;
            float start = burning ? p.started : p.forcedStart, openSeconds = burning ? PlatesOpenSeconds : p.forcedSeconds;
            float elapsed = fireClock - start;
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
                frame = Mathf.Clamp(Mathf.FloorToInt((elapsed - PlateTransitionSeconds - openSeconds) / PlateFrameSeconds), 0, 3);
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
            PaintLayer(p.cracks, baseSheet, reachedPhase3 ? "Cracks2" : "Cracks1", reachedPhase2 && !openPose && !hidden, row, frame, flip);
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
