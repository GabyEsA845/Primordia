using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // Provisionales: bases solidas y punto de amarre del subpaso M1.
        const float TrunkWidth = 2f, TrunkBottom = -0.45f, TrunkTop = 1.25f, StumpWidth = 1f;
        const float AnchorFraction = 0.8f, StumpLift = 1.5f, CableWidth = 1.8f;
        // Cuánto queda el pivote del cuerpo por debajo de la línea central del cable (0 = patas repartidas a ambos lados).
        // SpinneretOffset solo sirve ahora para el hilo colgante.
        const float CableSeat = 0f;
        // Los cables van DETRÁS de la copa (orden 20) para que las hojas tapen la unión.
        const int CableOrder = 19, CableRootOrder = 22, ThreadOrder = 23;
        const float CableRootInset = 2f;
        const float CableTaperStart = 0.9f, CableTipWidth = 0.7f, ThreadWidth = 0.08f;
        const int ThreadFrames = 3, ThreadFps = 8;
        float[] cableHeights;
        [SerializeField] Sprite tree, trunkSprite, web, stump;
        [SerializeField] Sprite[] cableRootSprites;
        [SerializeField] Material cableMaterial, threadMaterial;
        SpriteRenderer[] cableRoots;
        Material animatedThreadMaterial;
        Vector2[] starts, ends;
        int cableIndex;
        Vector2 routeStart, routeEnd;
        LineRenderer thread;
        float dropHeight;
        Vector2 climbFrom;
        bool outward;
        float crossHeight;
        GameObject scenery;
        public MegarachneBossCanopy Canopy { get; private set; }
        public int CableCount => starts != null ? starts.Length : 0;

        internal void ConfigureScenery(Sprite wetTree, Sprite wetTrunk, Sprite nestWeb, Sprite anchorStump, Sprite[] roots, Material material, Material thinMaterial)
        {
            tree = wetTree; trunkSprite = wetTrunk; web = nestWeb; stump = anchorStump;
            ConfigureCableArt(roots, material, thinMaterial);
        }

        internal void ConfigureCableArt(Sprite[] roots, Material material, Material thinMaterial)
        {
            cableRootSprites = roots;
            cableMaterial = material;
            threadMaterial = thinMaterial;
        }

        public void SetArena(Transform center, Vector2 size)
        {
            arena = center;
            arenaSize = size;
            if (scenery != null) return;
            scenery = new GameObject("NidoMegarachne");
            scenery.transform.SetParent(center, false);
            var trunk = new GameObject("Tronco");
            trunk.transform.SetParent(scenery.transform, false);
            var trunkHit = trunk.AddComponent<BoxCollider2D>();
            // Todo el tronco visible: desde el pie (bajo el origen) hasta donde empieza la copa (la copa es aérea, se pasa por debajo).
            trunkHit.size = new Vector2(TrunkWidth, TrunkTop - TrunkBottom);
            trunkHit.offset = new Vector2(0f, (TrunkTop + TrunkBottom) * 0.5f);
            // FloorBuilder crea Walls en Default; las bases comparten esa capa.
            trunk.layer = LayerMask.NameToLayer("Default");
            MakeSprite("BaseTronco", trunkSprite, Vector2.zero, 4);
            var crown = MakeSprite("Copa", tree, Vector2.up * 1.25f, 20);
            var nest = MakeSprite("Telarana", web, Vector2.zero, 21);
            // Anclas exactas de anchor() del arte aprobado (coordenadas de pantalla, Y invertida).
            float[] angles = { 205f, 268f, 330f, 140f, 35f };
            Vector2[] outer = { new(-12f, 2f), new(0f, 4f), new(12f, 2f), new(-13f, -7f), new(13f, -7f) };
            starts = new Vector2[angles.Length];
            ends = new Vector2[angles.Length];
            cableHeights = new float[angles.Length];
            cableRoots = new SpriteRenderer[angles.Length];
            BuildCableFire();
            for (int i = 0; i < angles.Length; i++)
            {
                float angle = angles[i] * Mathf.Deg2Rad;
                Vector2 branch = new Vector2(60f * Mathf.Cos(angle), 64f - 40f * Mathf.Sin(angle)) / 16f;
                starts[i] = ArenaCenter + branch;
                var anchor = MakeSprite("Sigillaria_" + i, stump, outer[i] + Vector2.up * StumpLift, 4);
                anchor.gameObject.layer = trunk.layer;
                var bounds = anchor.bounds;
                ends[i] = new Vector2(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, AnchorFraction));
                var stumpHit = anchor.gameObject.AddComponent<BoxCollider2D>();
                // Sigillaria entera: del pie hasta el punto de amarre del cable (arriba del tocón).
                float stumpHeight = ends[i].y - bounds.min.y;
                stumpHit.size = new Vector2(StumpWidth, stumpHeight);
                stumpHit.offset = new Vector2(0f, bounds.min.y - anchor.transform.position.y + stumpHeight * 0.5f);
                // El pie de subida queda delante de la base solida, a un radio del jefe.
                cableHeights[i] = ends[i].y - (bounds.min.y - HitRadius - ContactMargin * 2f);
                if (cableRootSprites != null && i < cableRootSprites.Length)
                    cableRoots[i] = MakeSprite("Nido_CableRoot_" + i, cableRootSprites[i], Vector2.zero, CableRootOrder);
                var line = MakeLine("Cable_" + i, CableWidth, CableOrder, cableMaterial);
                line.widthMultiplier = CableWidth;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f),
                    new Keyframe(CableTaperStart, 1f), new Keyframe(1f, CableTipWidth));
                // Solo el dibujo: el corte recto del cable se mete bajo la copa y la telaraña; el recorrido del jefe sigue en starts[i].
                line.SetPosition(0, starts[i] + (starts[i] - ends[i]).normalized * CableRootInset);
                line.SetPosition(1, ends[i]);
                BuildCableNodes(i, line);
            }
            Canopy = scenery.AddComponent<MegarachneBossCanopy>();
            Canopy.Configure(crown, nest, cableRoots);
            if (threadMaterial != null) animatedThreadMaterial = new Material(threadMaterial);
            thread = MakeLine("HiloColgante", ThreadWidth, ThreadOrder, animatedThreadMaterial ?? cableMaterial);
            thread.enabled = false;
            BuildFireSacs();
            // Vive en los cables: empieza sobre el más cercano al jugador (el centro lo ocupa el tronco).
            // Si la sala aún está desactivada (FloorBuilder), Awake no ha corrido: OnEnable lo colocará al entrar el jugador.
            if (isActiveAndEnabled) PlaceOnCable();
        }

        public void SetCableRootVisible(int i, bool visible)
        {
            if (cableRoots == null || i < 0 || i >= cableRoots.Length || cableRoots[i] == null) return;
            cableRoots[i].enabled = visible;
        }

        void OnDestroy()
        {
            if (animatedThreadMaterial != null) Destroy(animatedThreadMaterial);
            if (burningCableMaterial != null) Destroy(burningCableMaterial);
            if (charredCableMaterial != null) Destroy(charredCableMaterial);
            if (cableFire != null)
                foreach (var cable in cableFire)
                    if (cable.growingMaterial != null) Destroy(cable.growingMaterial);
        }

        SpriteRenderer MakeSprite(string objectName, Sprite sprite, Vector2 position, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(scenery.transform, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        LineRenderer MakeLine(string objectName, float width, int order, Material material)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(scenery.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Tile;
            line.sortingOrder = order;
            return line;
        }

        static Vector2 ClosestOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            return a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(delta.sqrMagnitude, Mathf.Epsilon));
        }

        internal int NearestCable(Vector2 point)
        {
            int selected = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < CableCount; i++)
            {
                if (!CableAvailable(i)) continue;
                Vector2 lift = Vector2.up * cableHeights[i];
                float distance = (ClosestOnSegment(point, starts[i] - lift, ends[i] - lift) - point).sqrMagnitude;
                if (distance < best) { best = distance; selected = i; }
            }
            return selected;
        }

        void PlaceOnCable()
        {
            cableIndex = NearestCable(target != null ? (Vector2)target.position : ArenaCenter);
            if (cableIndex < 0) { Land(); return; }
            Vector2 point = ProjectCablePointToGround(cableIndex, starts[cableIndex]);
            body.position = point;
            transform.position = point;
            SetHeight(cableHeights[cableIndex] - CableSeat);
            CurrentMode = Mode.OnCable;
            Health.IsInvulnerable = true;
            hit.isTrigger = true;
            outward = true;
            travel = Travel.Run;
            thread.enabled = false;
        }

        public bool ForceClimb()
        {
            if (CurrentMode != Mode.Ground || travel != Travel.None || CableCount == 0) return false;
            int next = NearestCable(body.position);
            if (next < 0) return false;
            cableIndex = next;
            Vector2 lift = Vector2.up * cableHeights[cableIndex];
            routeStart = ClosestOnSegment(body.position, starts[cableIndex] - lift, ends[cableIndex] - lift) + lift;
            climbFrom = body.position;
            travelTime = 0f;
            travel = Travel.Climb;
            CurrentMode = Mode.OnCable;
            Health.IsInvulnerable = true;
            hit.isTrigger = true;
            return true;
        }

        public void ForceDrop()
        {
            if (CurrentMode != Mode.OnCable || attack != AttackStage.None) return;
            routeStart = body.position + Vector2.up * (VisualHeight + SpinneretOffset);
            dropHeight = VisualHeight;
            travelTime = 0f;
            travel = Travel.Drop;
        }

        void BeginCross(Vector2 origin, int next)
        {
            routeStart = origin;
            crossHeight = VisualHeight;
            cableIndex = next;
            routeEnd = starts[next] - Vector2.up * cableHeights[next];
            travel = Travel.Cross;
            travelTime = 0f;
            outward = true;
        }

        void TickCable(float dt)
        {
            if (travel != Travel.Drop && !CableAvailable(cableIndex))
            {
                if (IsAttacking && (fireHitPending || cableFire[cableIndex].state != CableState.Burning)) return;
                if (!IsAttacking && travel == Travel.Run && TryFleeCable(dt)) return;
                if (travel != Travel.Run || cableFire[cableIndex].state != CableState.Burning) ForceDrop();
            }
            travelTime += dt;
            switch (travel)
            {
                case Travel.Climb:
                    float progress = Mathf.Clamp01(travelTime / ClimbSeconds);
                    body.MovePosition(Vector2.Lerp(climbFrom, routeStart - Vector2.up * cableHeights[cableIndex], progress));
                    SetHeight(Mathf.Lerp(0f, cableHeights[cableIndex] - CableSeat, progress));
                    Play("Climb", ClimbMs);
                    DrawThread(routeStart);
                    if (progress >= 1f) { travel = Travel.Run; outward = false; thread.enabled = false; }
                    break;
                case Travel.Run:
                    int wanted = target != null ? NearestCable(target.position) : cableIndex;
                    if (wanted >= 0 && wanted != cableIndex && CableAvailable(cableIndex)) outward = false;
                    Vector2 destination = ProjectCablePointToGround(cableIndex, outward ? ends[cableIndex] : starts[cableIndex]);
                    facing = (destination - body.position).normalized;
                    body.MovePosition(Vector2.MoveTowards(body.position, destination, CableSpeed * dt));
                    Play("Walk", WalkMs);
                    if (Vector2.Distance(body.position, destination) <= CableSpeed * dt)
                    {
                        // Cruza la copa entre ramas que aún se pueden usar.
                        if (!outward && wanted >= 0 && wanted != cableIndex) BeginCross(destination, wanted);
                        else outward = !outward;
                    }
                    break;
                case Travel.Cross:
                    float length = Vector2.Distance(routeStart, routeEnd);
                    float t = length > 0f ? Mathf.Clamp01(travelTime * CableSpeed / length) : 1f;
                    facing = (routeEnd - routeStart).normalized;
                    body.MovePosition(Vector2.Lerp(routeStart, routeEnd, t));
                    SetHeight(Mathf.Lerp(crossHeight, cableHeights[cableIndex] - CableSeat, t));
                    Play("Walk", WalkMs);
                    if (t >= 1f) travel = Travel.Run;
                    break;
                case Travel.Drop:
                    SetHeight(Mathf.Lerp(dropHeight, 0f, travelTime / DropSeconds));
                    Play("Drop", DropMs);
                    DrawThread(routeStart);
                    if (travelTime >= DropSeconds) Land();
                    break;
            }
        }

        void DrawThread(Vector2 anchor) => DrawThread(anchor, body.position + Vector2.up * (VisualHeight + SpinneretOffset));

        void DrawThread(Vector2 anchor, Vector2 end)
        {
            thread.enabled = true;
            if (animatedThreadMaterial != null)
            {
                int frame = Mathf.FloorToInt(Time.time * ThreadFps) % ThreadFrames;
                animatedThreadMaterial.mainTextureOffset = new Vector2(0f, (ThreadFrames - 1 - frame) / (float)ThreadFrames);
            }
            thread.SetPosition(0, anchor);
            thread.SetPosition(1, end);
        }
    }
}

