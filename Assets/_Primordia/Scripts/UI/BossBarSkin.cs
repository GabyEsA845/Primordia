using System;
using UnityEngine;

namespace Primordia.UI
{
    /// <summary>
    /// Aspecto propio de la barra de vida de un jefe: un marco dibujado (el propio animal) con una ventana donde va la vida,
    /// el relleno y los ojos de cada fase, las muescas que marcan los cambios de fase y la versión a oscuras para cuando es
    /// inmune. Opcionalmente, un marco distinto por fase (el Euriptérido verde y luego rosa) y un efecto al cambiar de fase
    /// (su caparazón saltando en pedazos). Lo lleva el jefe en <see cref="BossBarAppearance"/>; sin él, la barra es la lisa de siempre.
    /// Todas las medidas en píxeles de la interfaz (un píxel del dibujo = uno de la interfaz).
    /// </summary>
    public class BossBarSkin : ScriptableObject
    {
        [Serializable]
        public class PhaseLook
        {
            [Tooltip("Relleno de la ventana (uno o varios cuadros en bucle), del tamaño de la ventana.")]
            public Sprite[] fill = Array.Empty<Sprite>();
            public float fillFrameTime;
            public float eyesFrameTime;
            [Tooltip("Ojos encima del marco (uno o varios cuadros en bucle), del tamaño del marco.")]
            public Sprite[] eyes = Array.Empty<Sprite>();
            [Tooltip("Marco propio de esta fase (uno o varios cuadros en bucle). Vacío: el marco común (o el de silueta).")]
            public Sprite[] frame = Array.Empty<Sprite>();
        }

        [Serializable]
        public class PhaseEffect
        {
            public Sprite[] frames = Array.Empty<Sprite>();
            public float frameTime = 0.08f;
        }

        [SerializeField] bool bottomAnchored;
        [SerializeField] bool immuneUsesPhaseFrame;
        [SerializeField] PhaseEffect[] phaseChangeByPhase = Array.Empty<PhaseEffect>();
        [SerializeField] Sprite[] lowSparks = Array.Empty<Sprite>();
        [SerializeField] float lowSparksFrameTime = 0.09f;

        [SerializeField] Sprite[] knotIdle = Array.Empty<Sprite>(), knotHit = Array.Empty<Sprite>(), headHit = Array.Empty<Sprite>();
        [SerializeField] Sprite[] coneAppear = Array.Empty<Sprite>(), coneArmor = Array.Empty<Sprite>(), coneCharred = Array.Empty<Sprite>(), coneExposed = Array.Empty<Sprite>(), coneBroken = Array.Empty<Sprite>();
        public bool IsLepidodendron => coneArmor.Length > 0;
        public Sprite KnotFrame(float now, bool hit) => Loop(hit ? knotHit : knotIdle, now, hit ? 0.06f : 0.16f);
        public Sprite HeadHitFrame(float elapsed) => elapsed >= 0f && elapsed < 0.12f ? Loop(headHit, elapsed, 0.06f) : null;
        public Sprite ConeFrame(int state, float elapsed, float now) => elapsed < 0.36f
            ? Loop(coneAppear, elapsed, 0.09f) : Loop(state == 3 ? coneBroken : state == 2 ? coneExposed : state == 1 ? coneCharred : coneArmor, now, 0.12f);
        internal void ConfigureLepidodendron(Sprite[] idle, Sprite[] hit, Sprite[] head, Sprite[][] cones)
        {
            bottomAnchored = true;
            knotIdle = idle; knotHit = hit; headHit = head;
            coneAppear = cones[0]; coneArmor = cones[1]; coneCharred = cones[2]; coneExposed = cones[3]; coneBroken = cones[4];
        }

        public bool BottomAnchored => bottomAnchored;
        public bool ImmuneUsesPhaseFrame => immuneUsesPhaseFrame;
        public bool HasLowSparks => lowSparks.Length > 0;
        public Sprite LowSparksFrame(float time) => Loop(lowSparks, time, lowSparksFrameTime);

        [Header("Marco")]
        [SerializeField] Sprite frame;
        [Tooltip("Marco en silueta: mientras el jefe es inmune.")]
        [SerializeField] Sprite frameDark;
        [Tooltip("Ventana de la vida dentro del marco, desde su esquina de abajo a la izquierda.")]
        [SerializeField] RectInt window = new(38, 12, 200, 8);

        [Header("Ventana")]
        [SerializeField] Sprite back;
        [Tooltip("Trazo claro de la vida recién perdida (barra fantasma).")]
        [SerializeField] Sprite trail;
        [Tooltip("Relleno y ojos por fase; si el jefe tiene más fases que entradas, se queda la última.")]
        [SerializeField] PhaseLook[] phases = Array.Empty<PhaseLook>();
        [Tooltip("Relleno y ojos mientras es inmune (con el marco en silueta).")]
        [SerializeField] PhaseLook dark = new();
        [Tooltip("Segundos por cuadro de los rellenos y ojos animados.")]
        [SerializeField, Min(0.01f)] float frameTime = 0.15f;

        [Header("Muescas")]
        [Tooltip("Placa que marca un cambio de fase, centrada en su umbral, de la altura del marco.")]
        [SerializeField] Sprite notch;
        [SerializeField] Sprite notchBroken;
        [Tooltip("La muesca rompiéndose (centrada en el mismo punto).")]
        [SerializeField] Sprite[] notchBreak = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float notchBreakFrameTime = 0.07f;

        [Header("Cambio de fase")]
        [Tooltip("Efecto encima de todo, del tamaño del marco, que se ve una vez al pasar a la fase siguiente (opcional).")]
        [SerializeField] Sprite[] phaseChange = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float phaseChangeFrameTime = 0.08f;

        public Sprite Frame => frame;
        public Sprite FrameDark => frameDark != null ? frameDark : frame;
        public RectInt Window => window;
        public Sprite Back => back;
        public Sprite Trail => trail;
        public int PhaseCount => phases.Length;
        public PhaseLook Dark => dark;
        public float FrameTime => frameTime;
        public Sprite Notch => notch;
        public Sprite NotchBroken => notchBroken;
        public Sprite[] NotchBreak => notchBreak;
        public float NotchBreakTime => notchBreak.Length * notchBreakFrameTime;
        public Sprite[] PhaseChange => phaseChange;

        /// <summary>Cuadro del efecto de cambio de fase a esos segundos de empezar (null al acabar o si no hay).</summary>
        public Sprite PhaseChangeFrame(float elapsed) => PhaseChangeFrame(elapsed, -1);

        public Sprite PhaseChangeFrame(float elapsed, int phase)
        {
            var effect = phase >= 0 && phase < phaseChangeByPhase.Length ? phaseChangeByPhase[phase] : null;
            var frames = effect != null && effect.frames != null && effect.frames.Length > 0 ? effect.frames : phaseChange;
            float duration = ReferenceEquals(frames, phaseChange) ? phaseChangeFrameTime : effect.frameTime;
            // Evita convertir infinito a entero antes de que empiece el efecto.
            if (float.IsInfinity(elapsed) || elapsed < 0f)
                return null;
            int index = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, duration));
            return index < frames.Length ? frames[index] : null;
        }

        /// <summary>Aspecto de esa fase (la última si hay menos).</summary>
        public PhaseLook Phase(int index) =>
            phases.Length == 0 ? dark : phases[Mathf.Clamp(index, 0, phases.Length - 1)];

        /// <summary>Cuadro de la rotura de la muesca a esos segundos de empezar (null al acabar).</summary>
        public Sprite NotchBreakFrame(float elapsed)
        {
            int index = Mathf.FloorToInt(elapsed / notchBreakFrameTime);
            return index >= 0 && index < notchBreak.Length ? notchBreak[index] : null;
        }

        /// <summary>Cuadro de una animación en bucle de este aspecto a esa hora.</summary>
        public Sprite Loop(Sprite[] frames, float time, float duration = 0f)
        {
            if (frames == null || frames.Length == 0)
                return null;
            return frames[Mathf.FloorToInt(time / (duration > 0f ? duration : frameTime)) % frames.Length];
        }

        internal void Configure(Sprite newFrame, Sprite newFrameDark, RectInt newWindow, Sprite newBack, Sprite newTrail,
            PhaseLook[] newPhases, PhaseLook newDark, Sprite newNotch, Sprite newNotchBroken, Sprite[] newNotchBreak)
        {
            frame = newFrame;
            frameDark = newFrameDark;
            window = newWindow;
            back = newBack;
            trail = newTrail;
            phases = newPhases ?? Array.Empty<PhaseLook>();
            dark = newDark ?? new PhaseLook();
            notch = newNotch;
            notchBroken = newNotchBroken;
            notchBreak = newNotchBreak ?? Array.Empty<Sprite>();
        }

        /// <summary>Solo el efecto al pasar a cada fase (la Arthropleura: la pared que revienta y las placas que se abren).</summary>
        internal void ConfigurePhaseEffects(PhaseEffect[] effects) => phaseChangeByPhase = effects;

        internal void ConfigureMeganeura(PhaseEffect[] effects, Sprite[] sparks)
        {
            bottomAnchored = true;
            immuneUsesPhaseFrame = true;
            phaseChangeByPhase = effects;
            lowSparks = sparks;
            lowSparksFrameTime = 0.09f;
        }

        /// <summary>Efecto al pasar a la fase siguiente (el caparazón del Euriptérido saltando en pedazos).</summary>
        internal void ConfigurePhaseChange(Sprite[] newPhaseChange, float frameTime)
        {
            phaseChange = newPhaseChange ?? Array.Empty<Sprite>();
            phaseChangeFrameTime = Mathf.Max(0.01f, frameTime);
        }
    }
}
