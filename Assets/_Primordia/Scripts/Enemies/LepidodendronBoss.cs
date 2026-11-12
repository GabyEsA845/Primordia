using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>L1: escenario del jefe secreto, invulnerable y sin combate ni barra activa.</summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Health), typeof(Enemy), typeof(Rigidbody2D))]
    public partial class LepidodendronBoss : MonoBehaviour, IBoss
    {
        public const int MaxHealth = 1000; // Provisional hasta cerrar L2.
        public const float CorridorLength = 116f, CorridorHeight = 18f;
        public const float ArenaWidth = 40f, ArenaHeight = 26f, FootFromNorth = 10.5f;
        public const float Wall1X = 34f, Wall2X = 64f, Wall3X = 94f;
        public const float Wall1Thickness = 3f, Wall2Thickness = 3.5f, Wall3Thickness = 4f;
        public const float ArenaZoom = 0.75f;
        const float CameraTransition = 0.6f; // Provisional, solo suavizado visual.
        const float Ppu = 16f;
        const float CanopyAboveScreen = 10f / Ppu;
        const int RootsOrder = -15, MoundShadowOrder = -14, MoundOrder = -13, DappleOrder = -11;
        const int TrunkOrder = -5, VeinsOrder = -4, DecorOrder = -3, WallOrder = 4;
        const int LightOrder = 100, LeavesOrder = 101, CanopyOrder = 102;
        const int ShakeLeaves = 70;
        const float SporeFraction = 0.3f, IdleFallEvery = 0.8f; // Provisional: adorno en reposo.
        const float LeafMinSpeed = 1.6f * 60f / Ppu, LeafMaxSpeed = 3.2f * 60f / Ppu;
        const float CritterNear = 3f, CritterFleeSeconds = 1.2f; // Provisional: adorno, sin combate.
        const float RoachSpeed = 3f, DragonflySpeed = 5f, MillipedeSpeed = 0.3f;
        const float TestClearance = 2f, TestArenaSouth = 4f;
        // Ritmos de las hojas aprobadas (segundos por cuadro).
        const float CanopySwayStep = 0.15f, CanopyShakeStep = 0.06f, DappleStep = 0.3f, LightStep = 0.14f;
        const float KnotStep = 0.16f, ShroomStep = 0.16f, FlowerStep = 0.2f, PuddleStep = 0.18f;
        const float LeafStep = 0.07f, SporeStep = 0.12f;
        const float RoachIdleStep = 0.3f, RoachRunStep = 0.07f, DragonflyStep = 0.05f, MillipedeStep = 0.12f, SpringtailStep = 0.09f;
        // Provisionales: movimiento decorativo, sin efectos en el combate.
        const float LeafSwayRate = 3f, LeafSwaySpeed = 0.5f, LeafLife = 5f, LeafSpawnHeight = 4f;

        [SerializeField] DirectionalArt art;
        readonly List<Visual> visuals = new();
        readonly List<Falling> falling = new();
        readonly List<Critter> critters = new();
        readonly List<DappleQuad> dappleQuads = new();
        Material dappleMaterial;
        Transform scenery, canopyRoot;
        SpriteRenderer arenaCanopy;
        Transform player;
        CameraZoom zoom;
        CameraLookAhead lookAhead;
        float zoomAmount, shakeUntil, nextFall;
        Vector2 entrance;
        bool built;

        public string DisplayName => "Lepidodendron ancestral";
        public Health Health => GetComponent<Health>();
        public BossBarStyle BarStyle => BossBarStyle.Normal;
        public Vector2 ArenaCenter => entrance + Vector2.right * (CorridorLength + ArenaWidth / 2f);
        public Vector2 Foot => ArenaCenter + Vector2.up * (ArenaHeight / 2f - FootFromNorth);
        public bool IsCanopyShaking => Time.time < shakeUntil;
        public DirectionalArt Art => art;

        internal void Configure(DirectionalArt newArt) => art = newArt;

        void Awake()
        {
            Health.IsInvulnerable = true;
            var body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        void OnEnable() => Health.IsInvulnerable = true;

        void OnDisable()
        {
            zoom?.Restore();
            zoom = null;
            if (lookAhead != null)
                lookAhead.ClearFocus();
            zoomAmount = 0f;
        }

        void OnDestroy()
        {
            foreach (var quad in dappleQuads)
                if (quad.mesh != null)
                    Destroy(quad.mesh);
            if (dappleMaterial != null)
                Destroy(dappleMaterial);
        }

        public void SetArena(Transform center, Vector2 size)
        {
            if (built)
                return;
            entrance = (Vector2)center.position - Vector2.right * ((CorridorLength + ArenaWidth) / 2f);
            transform.position = Foot;
            BuildScenery();
            built = true;
        }

        public void PlayIntroPose(float hold) { }
        public void BeginFight() { }

        public Vector2 TestStop(int index) => index < 3
            ? entrance + Vector2.right * (WallX(index) - WallThickness(index) / 2f - TestClearance)
            : ArenaCenter + Vector2.down * TestArenaSouth;

        /// <summary>Adorno reutilizable por L3/L4: sacudida y lluvia de hojas, sin daño.</summary>
        public void ShakeCanopy(float seconds)
        {
            if (!built || seconds <= 0f)
                return;
            shakeUntil = Time.time + seconds;
            for (int i = 0; i < ShakeLeaves; i++)
                DropLeaf(false);
            int spores = Mathf.RoundToInt(ShakeLeaves * SporeFraction / (1f - SporeFraction));
            for (int i = 0; i < spores; i++)
                DropLeaf(true);
            foreach (var critter in critters)
                if (critter.kind == "Dragonfly")
                    critter.fleeUntil = Time.time + CritterFleeSeconds;
            Animate();
        }

        void Update()
        {
            if (!built)
                return;
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                if (controller != null)
                    player = controller.transform;
            }
            UpdateCamera();
            Animate();
            UpdateCritters();
            if (Time.time >= nextFall)
            {
                nextFall = Time.time + IdleFallEvery;
                DropLeaf(Random.value < SporeFraction);
            }
            for (int i = falling.Count - 1; i >= 0; i--)
            {
                var leaf = falling[i];
                if (leaf.renderer == null)
                {
                    falling.RemoveAt(i);
                    continue;
                }
                leaf.renderer.transform.position += new Vector3(Mathf.Sin(Time.time * LeafSwayRate + leaf.phase) * LeafSwaySpeed, -leaf.speed, 0f) * Time.deltaTime;
                if (Time.time > leaf.until)
                {
                    Destroy(leaf.renderer.gameObject);
                    falling.RemoveAt(i);
                }
            }
        }

        // Después de Cinemachine: la copa conserva la x del mundo y sigue el borde superior real en y.
        void LateUpdate()
        {
            if (canopyRoot == null || Camera.main == null)
                return;
            var camera = Camera.main;
            var p = canopyRoot.position;
            p.y = camera.transform.position.y + camera.orthographicSize + CanopyAboveScreen;
            canopyRoot.position = p;
            UpdateLightPositions();
        }

        void UpdateCamera()
        {
            if (player == null)
                return;
            float x = player.position.x - entrance.x;
            bool inside = x >= 0f && x <= CorridorLength + ArenaWidth && Mathf.Abs(player.position.y - entrance.y) <= ArenaHeight / 2f;
            float target = inside && x >= CorridorLength ? 1f : 0f;
            if (target > 0f && zoom == null)
            {
                zoom = new CameraZoom(1f);
                // 480×270 a 16 px/u, alejado ×0,75: 40×22,5 u incluso si Pixel Perfect redondeó la vista normal.
                zoom.Zoom = ArenaHalfViewHeight / Mathf.Max(0.01f, zoom.BaseSize);
                lookAhead = FindAnyObjectByType<CameraLookAhead>();
            }
            if (zoom == null)
                return;
            zoomAmount = Mathf.MoveTowards(zoomAmount, target, Time.deltaTime / CameraTransition);
            zoom.Set(zoomAmount);
            if (lookAhead != null)
            {
                float halfHeight = ArenaHalfViewHeight;
                float y = Mathf.Clamp(player.position.y, ArenaCenter.y - ArenaHeight / 2f + halfHeight,
                    ArenaCenter.y + ArenaHeight / 2f - halfHeight);
                lookAhead.SetFocus(new Vector2(ArenaCenter.x, y), zoomAmount);
            }
            if (target == 0f && zoomAmount <= 0f)
            {
                zoom.Restore();
                zoom = null;
                if (lookAhead != null)
                    lookAhead.ClearFocus();
            }
        }

        static float WallX(int i) => i == 0 ? Wall1X : i == 1 ? Wall2X : Wall3X;
        static float WallThickness(int i) => i == 0 ? Wall1Thickness : i == 1 ? Wall2Thickness : Wall3Thickness;

        sealed class Visual
        {
            public SpriteRenderer renderer;
            public string sheet;
            public float step, phase;
        }
        sealed class Falling
        {
            public SpriteRenderer renderer;
            public float speed, until, phase;
        }
        sealed class Critter
        {
            public SpriteRenderer renderer;
            public string kind;
            public Vector2 home;
            public float fleeUntil;
        }
    }
}
