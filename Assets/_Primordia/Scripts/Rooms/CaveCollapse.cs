using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Derrumbe de la cueva del Euriptérido, que va a más con el combate (lo enciende el jefe por fases):
    /// <list type="bullet">
    /// <item><b>Crujidos</b> (fase acorazada): un temblor suelto cada 6–8 s y unas chinas que caen, sin daño.</item>
    /// <item><b>Terremoto</b> (escena de la muda): temblor fuerte, lluvia de chinas y se derrumban las rocas agrietadas.</item>
    /// <item><b>Derrumbe</b> (fase blanda, hasta que acaba): temblor suave continuo, chinas, piedras del techo cada 2–3 s
    /// (1–3, la primera hacia el jugador) y, cada 10–12 s, una de las zonas Z del plano se agrieta y se hunde en fosa
    /// (3–4 por combate, nunca la que pisa el jugador).</item>
    /// </list>
    /// Al despejar la sala todo se calma.
    /// </summary>
    public class CaveCollapse : MonoBehaviour
    {
        public enum Mode { Calm, Creaking, Quake, Collapse }

        [System.Serializable]
        public struct Art
        {
            public Sprite[] fallShadow;
            public Sprite[] fallingRock;
            public Sprite[] rockImpact;
            public Sprite[] pebbles;
            public Sprite[] pebblePuff;
            public Sprite[] floorCrack;
        }

        // Cuadros del aviso de las grietas (el resto, el hundimiento).
        const int CrackWarningFrames = 6;

        [SerializeField] Room room;
        [SerializeField] RectInt floorArea;
        [SerializeField] List<RectInt> zones = new();
        [SerializeField] RoomTheme theme;
        [SerializeField] Tilemap ground;
        [SerializeField] Tilemap decoration;
        [SerializeField] Art art;

        [Header("Crujidos")]
        [SerializeField] Vector2 creakInterval = new(6f, 8f);
        [SerializeField, Min(0f)] float creakShake = 1.5f;
        [SerializeField, Min(0)] int creakPebbles = 8;

        [Header("Terremoto")]
        [SerializeField, Min(0f)] float quakeRumble = 3f;
        [SerializeField, Min(0f)] float quakePebblesPerSecond = 25f;

        [Header("Derrumbe")]
        [SerializeField, Min(0f)] float collapseRumble = 1f;
        [SerializeField, Min(0f)] float collapsePebblesPerSecond = 3f;
        [SerializeField] Vector2 rockInterval = new(2f, 3f);
        [SerializeField] Vector2Int rocksPerVolley = new(1, 3);
        [Tooltip("Las piedras que no apuntan al jugador caen a esta distancia de él como mucho.")]
        [SerializeField, Min(1f)] float rockSpread = 7f;
        [SerializeField] Vector2 sinkInterval = new(10f, 12f);
        [SerializeField] Vector2Int zonesPerFight = new(3, 4);
        [SerializeField, Min(0.1f)] float sinkWarning = 1.5f;

        Mode mode;
        float nextCreak;
        float nextRock;
        float nextSink;
        float pebbleDebt;
        readonly List<RectInt> pending = new();
        int sunk;
        Transform player;

        public Mode Current => mode;
        public IReadOnlyList<RectInt> Zones => zones;
        /// <summary>Zonas que aún se pueden hundir en este combate.</summary>
        public int PendingZones => pending.Count;
        public int SunkZones => sunk;
        public float SinkWarning => sinkWarning;

        public static event System.Action<CaveCollapse, Mode> ModeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ModeChanged = null;

        internal void Configure(Room newRoom, RectInt newFloorArea, List<RectInt> newZones, RoomTheme newTheme, Tilemap newGround,
            Tilemap newDecoration, Art newArt)
        {
            if (room != null)
                room.Cleared -= OnCleared;
            room = newRoom;
            floorArea = newFloorArea;
            zones = newZones ?? new List<RectInt>();
            theme = newTheme;
            ground = newGround;
            decoration = newDecoration;
            art = newArt;
            if (isActiveAndEnabled && room != null)
                room.Cleared += OnCleared;
        }

        void OnEnable()
        {
            if (room != null)
                room.Cleared += OnCleared;
        }

        void OnDisable()
        {
            if (room != null)
                room.Cleared -= OnCleared;
            ScreenShake.ClearRumble(this);
        }

        void OnCleared(Room _) => SetMode(Mode.Calm);

        /// <summary>Cambia de fase del derrumbe. El terremoto derrumba las rocas agrietadas de la sala.</summary>
        public void SetMode(Mode newMode)
        {
            if (newMode == mode)
                return;
            mode = newMode;
            float now = Time.time;
            switch (mode)
            {
                case Mode.Calm:
                    ScreenShake.ClearRumble(this);
                    break;
                case Mode.Creaking:
                    ScreenShake.ClearRumble(this);
                    nextCreak = now + Random.Range(creakInterval.x, creakInterval.y) * 0.5f;
                    break;
                case Mode.Quake:
                    ScreenShake.SetRumble(this, quakeRumble);
                    Sfx.Play(Sounds.CaveQuake);
                    CollapsingRock.CollapseAll(this);
                    break;
                case Mode.Collapse:
                    ScreenShake.SetRumble(this, collapseRumble);
                    Sfx.Play(Sounds.CaveCollapse);
                    if (pending.Count == 0 && sunk == 0)
                        ChooseZones();
                    nextRock = now + 1.5f;
                    nextSink = now + Random.Range(sinkInterval.x, sinkInterval.y) * 0.5f;
                    break;
            }
            ModeChanged?.Invoke(this, mode);
        }

        /// <summary>Elige al azar las zonas Z que se hundirán en este combate (de 3 a 4, o las que haya).</summary>
        void ChooseZones()
        {
            pending.Clear();
            var shuffled = new List<RectInt>(zones);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            int count = Mathf.Min(shuffled.Count, Random.Range(zonesPerFight.x, zonesPerFight.y + 1));
            for (int i = 0; i < count; i++)
                pending.Add(shuffled[i]);
        }

        void Update()
        {
            float now = Time.time;
            switch (mode)
            {
                case Mode.Creaking:
                    if (now >= nextCreak)
                    {
                        nextCreak = now + Random.Range(creakInterval.x, creakInterval.y);
                        ScreenShake.Pulse(creakShake, 0.6f);
                        Sfx.Play(Sounds.CaveCreak);
                        for (int i = 0; i < creakPebbles; i++)
                            DropPebble();
                    }
                    break;
                case Mode.Quake:
                    RainPebbles(quakePebblesPerSecond);
                    break;
                case Mode.Collapse:
                    RainPebbles(collapsePebblesPerSecond);
                    if (now >= nextRock)
                    {
                        nextRock = now + Random.Range(rockInterval.x, rockInterval.y);
                        DropVolley();
                    }
                    if (now >= nextSink)
                    {
                        nextSink = now + Random.Range(sinkInterval.x, sinkInterval.y);
                        SinkNext();
                    }
                    break;
            }
        }

        void RainPebbles(float perSecond)
        {
            pebbleDebt += perSecond * Time.deltaTime;
            while (pebbleDebt >= 1f)
            {
                pebbleDebt -= 1f;
                DropPebble();
            }
        }

        /// <summary>Una china decorativa que cae por la parte de la pantalla que se ve.</summary>
        void DropPebble()
        {
            if (art.pebbles == null || art.pebbles.Length == 0)
                return;
            Rect view = View();
            float x = Random.Range(view.xMin, view.xMax);
            float landY = Random.Range(view.yMin + 0.5f, view.yMax - 1f);
            FallingPebble.Spawn(transform, new Vector2(x, view.yMax + 0.5f), landY, art.pebbles[Random.Range(0, art.pebbles.Length)], art.pebblePuff);
        }

        Rect View()
        {
            var camera = Camera.main;
            if (camera != null && camera.orthographic)
            {
                float height = camera.orthographicSize * 2f;
                float width = height * camera.aspect;
                Vector2 center = camera.transform.position;
                return new Rect(center.x - width / 2f, center.y - height / 2f, width, height);
            }
            return new Rect(floorArea.x, floorArea.y, floorArea.width, floorArea.height);
        }

        Transform Player()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                player = found != null ? found.transform : null;
            }
            return player;
        }

        /// <summary>Una tanda de 1–3 piedras: la primera hacia donde va el jugador, las demás cerca de él.</summary>
        public void DropVolley()
        {
            var target = Player();
            Vector2 center = target != null ? (Vector2)target.position : floorArea.center;
            int count = Random.Range(rocksPerVolley.x, rocksPerVolley.y + 1);
            var used = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                Vector2 point;
                if (i == 0 && target != null)
                {
                    // Adelantada a medio camino de donde estará: quedarse quieto o seguir recto, las dos cosas castigan.
                    var velocity = target.TryGetComponent<Rigidbody2D>(out var body) ? body.linearVelocity : Vector2.zero;
                    point = center + velocity * FallingRock.DefaultWarning * 0.5f;
                    if (!IsGround(point))
                        point = center;
                }
                else if (!TryRandomPoint(center, used, out point))
                {
                    continue;
                }
                used.Add(point);
                DropRockAt(point);
            }
        }

        public FallingRock DropRockAt(Vector2 point) =>
            FallingRock.Spawn(transform, point, art.fallShadow, art.fallingRock, art.rockImpact);

        bool TryRandomPoint(Vector2 center, List<Vector2> used, out Vector2 point)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                point = center + Random.insideUnitCircle * rockSpread;
                if (!IsGround(point))
                    continue;
                bool apart = true;
                foreach (var other in used)
                    apart &= Vector2.Distance(other, point) >= FallingRock.DefaultRadius * 2f;
                if (apart)
                    return true;
            }
            point = default;
            return false;
        }

        /// <summary>Suelo de la sala (no pared, obstáculo ni fosa).</summary>
        bool IsGround(Vector2 point)
        {
            var cell = NavigationGrid.CellOf(point);
            if (!floorArea.Contains(cell))
                return false;
            return Pits.Map == null || Pits.Map.Get(cell) == FloorMap.Kind.Floor;
        }

        /// <summary>
        /// Empieza a hundir la siguiente zona que no pisa el jugador: se agrieta durante <see cref="SinkWarning"/> s y se
        /// vuelve fosa. Devuelve false si no queda ninguna disponible.
        /// </summary>
        public bool SinkNext()
        {
            var target = Player();
            for (int i = 0; i < pending.Count; i++)
            {
                var zone = pending[i];
                if (target != null && Contains(zone, target.position, 0.5f))
                    continue;
                pending.RemoveAt(i);
                StartCoroutine(Sink(zone));
                return true;
            }
            return false;
        }

        static bool Contains(RectInt zone, Vector2 point, float margin) =>
            point.x >= zone.xMin - margin && point.x <= zone.xMax + margin && point.y >= zone.yMin - margin && point.y <= zone.yMax + margin;

        IEnumerator Sink(RectInt zone)
        {
            Vector2 center = zone.center;
            var frames = art.floorCrack ?? System.Array.Empty<Sprite>();
            int warningFrames = Mathf.Min(CrackWarningFrames, frames.Length);
            var warning = new Sprite[warningFrames];
            System.Array.Copy(frames, warning, warningFrames);
            FrameEffect.Play("FloorCrack", warning, sinkWarning / Mathf.Max(1, warningFrames), FallingRock.ShadowOrder, null, center);
            Sfx.Play(Sounds.CaveFloorCrack);
            yield return new WaitForSeconds(sinkWarning);

            var cells = new List<Vector2Int>();
            foreach (var cell in zone.allPositionsWithin)
            {
                cells.Add(cell);
                Pits.Map?.Set(cell, FloorMap.Kind.Pit);
                NavigationGrid.Current?.SetWalkable(cell, false);
            }
            RoomPainter.RepaintPits(Pits.Map, theme, ground, decoration, cells);
            if (frames.Length > warningFrames)
            {
                var falling = new Sprite[frames.Length - warningFrames];
                System.Array.Copy(frames, warningFrames, falling, 0, falling.Length);
                FrameEffect.Play("FloorSink", falling, 0.12f, FallingRock.ShadowOrder, null, center);
            }
            ScreenShake.Pulse(2.5f, 0.4f);
            Sfx.Play(Sounds.CaveFloorSink);
            sunk++;
        }
    }
}
