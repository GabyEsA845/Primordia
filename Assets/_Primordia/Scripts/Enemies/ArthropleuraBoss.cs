using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, jefe principal del piso 3: cadena de cabeza + 28 piezas + cola. Hunt/Charge llevan la embestida de F1;
    /// Fire lleva la mecha, las placas, los odres y la transición pendiente al 70 %.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Health), typeof(Enemy), typeof(Rigidbody2D))]
    public partial class ArthropleuraBoss : MonoBehaviour, IBoss, IPhasedBoss
    {
        public const int MaxHealth = 600, SegmentCount = 28;
        public const float ArenaWidth = 60f, ArenaHeight = 33f;
        /// <summary>Distancia entre nudos de la cadena (un segmento mide 4 u y se solapan).</summary>
        public const float Pitch = 2.6f;
        /// <summary>El arte está en picada: la y de pantalla es la y del suelo por sen 55°.</summary>
        public const float GroundSquash = 0.82f;
        public static readonly float[] Thresholds = { 0.7f, 0.35f, 0.1f };

        const int FirstOrder = 12;
        const float TrailSpacing = 0.1f;
        const float RidgeStep = 0.14f;
        const float HeadStep = 0.11f, WalkStep = 0.09f, IdleStep = 0.2f;

        [SerializeField] DirectionalArt art;
        readonly List<Vector2> trail = new(); // Suelo, relativo al centro; el [0] es la cabeza.
        readonly List<Piece> pieces = new();
        Transform player;
        Health playerHealth;
        DirectionalArt.Sheet ridge, ridgeCap;
        Vector2 center;
        Vector2 headPos; // Posición exacta de la cabeza; el rastro la muestrea cada 0,1 u.
        float heading; // Grados en el suelo, 0° = derecha.
        bool built, awake; // awake: el jefe no se ve ni se mueve hasta que el jugador entra al claro.

        public string DisplayName => "Arthropleura";
        public Health Health => GetComponent<Health>();
        public BossBarStyle BarStyle => Health.IsInvulnerable ? BossBarStyle.Immune : BossBarStyle.Normal;
        public Vector2 ArenaCenter => built ? center : (Vector2)transform.position;
        public float[] PhaseThresholds => Thresholds;
        public DirectionalArt Art => art;
        public int PieceCount => pieces.Count;
        /// <summary>Si anda hacia el jugador o se queda quieta (para pruebas).</summary>
        public bool Moving { get; set; } = true;

        /// <summary>0 = F1 (hasta el 70 %), 1 = F2 (hasta el 35 %), 2 = F3 (hasta el 10 %), 3 = rueda.</summary>
        public int PhaseIndex
        {
            get
            {
                float fraction = Health != null && Health.Max > 0 ? (float)Health.Current / Health.Max : 1f;
                int phase = 0;
                foreach (float threshold in Thresholds)
                    if (fraction <= threshold)
                        phase++;
                return phase;
            }
        }

        internal static readonly string[] RequiredSheets =
        {
            "Arthropleura_Head_Move", "Arthropleura_Segment_Idle", "Arthropleura_Segment_Walk", "Arthropleura_Tail_Idle", "Arthropleura_Tail_Walk",
        };

        internal void Configure(DirectionalArt newArt) => art = newArt;

        void Awake()
        {
            // El caparazón es invulnerable; Fire aplica por separado el daño limitado de las placas abiertas.
            Health.IsInvulnerable = true;
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        void OnEnable() { EnableFire(); if (awake && IntroStage == EntryStage.Complete) ActiveBoss.Set(this); }
        void OnDisable()
        {
            ActiveBoss.Clear(this);
            DisableFire();
            ReleaseCamera();
            ScreenShake.ClearRumble(this);
        }

        public void SetArena(Transform arena, Vector2 size)
        {
            if (built)
                return;
            center = arena.position;
            transform.position = center;
            wallRoom = arena;
            ridge = art != null ? art.Find("Arthropleura_CanopyRidge") : null;
            ridgeCap = art != null ? art.Find("Arthropleura_CanopyRidge_Cap") : null;
            BuildChain();
            BuildFire();
            built = true;
            Place();
        }


        /// <summary>Punto del suelo (relativo al centro de la sala) → posición en el mundo.</summary>
        public Vector2 GroundToWorld(Vector2 ground) => center + new Vector2(ground.x, ground.y * GroundSquash);
        public Vector2 WorldToGround(Vector2 world) { var d = world - center; return new Vector2(d.x, d.y / GroundSquash); }

        /// <summary>Posición en el suelo del nudo <paramref name="index"/> (0 = cabeza, 1..28 = tronco, 29 = cola).</summary>
        public Vector2 NodeGround(int index) => PointAlongTrail(index * Pitch);

        void BuildChain()
        {
            // Empieza estirada sobre el anillo de la espesura (cabeza hacia el este, en sentido horario).
            float theta = 0f, step = 0.00002f; // Fino: junto a los lados planos la curva casi no avanza por cada paso de ángulo.
            Vector2 last = RingPoint(theta);
            trail.Add(last);
            int samples = Mathf.CeilToInt(((SegmentCount + 1) * Pitch + 3f) / TrailSpacing);
            while (trail.Count < samples)
            {
                theta += step;
                Vector2 point = RingPoint(theta);
                if ((point - last).magnitude >= TrailSpacing)
                {
                    trail.Add(point);
                    last = point;
                }
            }
            headPos = trail[0];
            heading = -90f;
            pieces.Add(NewPiece("Head", "Arthropleura_Head_Move", "Arthropleura_Head_Move", HeadStep, 0));
            for (int i = 1; i <= SegmentCount; i++)
                pieces.Add(NewPiece($"Segment_{i:00}", "Arthropleura_Segment_Idle", "Arthropleura_Segment_Walk", WalkStep, i));
            pieces.Add(NewPiece("Tail", "Arthropleura_Tail_Idle", "Arthropleura_Tail_Walk", WalkStep, SegmentCount + 1));
        }

        Piece NewPiece(string pieceName, string idle, string walk, float step, int index)
        {
            var go = new GameObject(pieceName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            // De la cola a la cabeza: cada placa tapa a la de detrás.
            renderer.sortingOrder = FirstOrder + (SegmentCount + 1 - index);
            return new Piece { renderer = renderer, idle = art != null ? art.Find(idle) : null, walk = art != null ? art.Find(walk) : null, step = step, index = index };
        }

        void Update()
        {
            if (!built)
                return;
            if (!thicketBuilt)
            {
                // En el primer frame: las puertas de la sala ya existen y se les deja paso.
                thicketBuilt = true;
                BuildThicket();
            }
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                if (controller != null)
                {
                    player = controller.transform;
                    playerHealth = controller.GetComponent<Health>();
                }
            }
            SetSacsAvailable(FireAllowed);
            if (FireAllowed) TickFire(Time.deltaTime);
            if (!awake && Moving)
            {
                WatchEntry();
                if (entryCells.Count == 0 || entriesSealed && player != null && Vector2.Distance(player.position, center) < IntroCenterRadius)
                    WakeUp();
            }
            if (awake && Moving)
            {
                if (IntroStage != EntryStage.Complete) StepIntro(Time.deltaTime);
                else StepHunt(Time.deltaTime);
            }
            UpdateCamera();
            Place();
        }

        /// <summary>Avanza la cabeza hacia un punto del suelo girando como mucho <paramref name="turnRate"/> grados por segundo.</summary>
        void Advance(Vector2 target, float speed, float turnRate, float dt)
        {
            Vector2 head = headPos;
            float wanted = Mathf.Atan2(target.y - head.y, target.x - head.x) * Mathf.Rad2Deg;
            heading = Mathf.MoveTowardsAngle(heading, wanted, turnRate * dt);
            Vector2 next = head + new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad)) * (speed * dt);
            next.x = Mathf.Clamp(next.x, -ArenaWidth / 2f + 1f, ArenaWidth / 2f - 1f);
            next.y = Mathf.Clamp(next.y, -ArenaHeight / 2f / GroundSquash + 1f, ArenaHeight / 2f / GroundSquash - 1f);
            headPos = next;
            // Muestras a distancia fija: la cadena sigue exactamente el camino de la cabeza.
            Vector2 last = trail[0];
            float gap = (next - last).magnitude;
            while (gap >= TrailSpacing)
            {
                last += (next - last).normalized * TrailSpacing;
                trail.Insert(0, last);
                gap = (next - last).magnitude;
            }
            int max = Mathf.CeilToInt(((SegmentCount + 1) * Pitch + 3f) / TrailSpacing);
            if (trail.Count > max)
                trail.RemoveRange(max, trail.Count - max);
        }

        Vector2 PointAlongTrail(float distance)
        {
            float index = Mathf.Clamp(distance / TrailSpacing, 0f, trail.Count - 1f);
            int a = Mathf.FloorToInt(index);
            int b = Mathf.Min(a + 1, trail.Count - 1);
            return Vector2.Lerp(trail[a], trail[b], index - a);
        }

        void Place()
        {
            float time = Time.time;
            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (!awake && (Moving || !FireAllowed))
                {
                    piece.renderer.sprite = null; // Aún no ha salido por el borde.
                    continue;
                }
                Vector2 ground = NodeGround(i);
                // Dirección de la pieza: de la de detrás a la de delante, vista en pantalla (y × 0,82).
                Vector2 ahead = NodeGround(Mathf.Max(0, i - 1)), behind = NodeGround(i + 1);
                Vector2 look = Vector2.Scale(i == 0 ? ground - behind : ahead - behind, new Vector2(1f, GroundSquash));
                float angle = look.sqrMagnitude > 1e-6f ? Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg : 0f;
                int row = DirectionalArt.RowFor(angle, out bool flip);
                bool hidden = IsHidden(ground);
                if (IntroStage == EntryStage.Arriving && !hidden)
                {
                    piece.renderer.sprite = null;
                    continue;
                }
                bool walking = Moving && CurrentMode != Mode.Phase2Pending;
                var sheet = hidden ? (i == 0 ? ridgeCap : ridge) : OverrideSheet(i) ?? (walking ? piece.walk : piece.idle);
                if (sheet == null || sheet.sprites.Length == 0)
                    continue;
                // Ola de patas: un cuadro de desfase por pieza.
                float step = hidden ? RidgeStep : walking ? piece.step : IdleStep;
                int frame = Mathf.FloorToInt(time / step) + (hidden ? 0 : piece.index);
                if (i == 0 && IntroStage == EntryStage.Roaring)
                {
                    row = DirectionalArt.RowFor(-90f, out flip);
                    frame = Mathf.Min(3, Mathf.FloorToInt((Time.time - roarAt) / HeadStep));
                }
                var fireSheet = FireSheet(i, out int fireFrame);
                piece.renderer.sprite = !hidden && fireSheet != null ? fireSheet.Get(row, fireFrame) : sheet.Get(row, frame % sheet.frames);
                PaintFire(i, sheet, row, frame, flip, hidden);
                piece.renderer.flipX = flip;
                piece.renderer.transform.position = GroundToWorld(ground);
            }
        }

        sealed class Piece
        {
            public SpriteRenderer renderer;
            public DirectionalArt.Sheet idle, walk;
            public float step;
            public int index;
        }
    }
}
