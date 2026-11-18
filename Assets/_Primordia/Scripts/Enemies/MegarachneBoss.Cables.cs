using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // Provisionales: bases solidas y punto de amarre del subpaso M1.
        const float TrunkWidth = 2f, TrunkHeight = 1.2f, StumpWidth = 1f, StumpHeight = 0.8f;
        const float AnchorFraction = 0.8f, StumpLift = 1.5f, CableWidth = 1.8f;
        // Cuánto queda el pivote del cuerpo por debajo de la línea central del cable (0 = patas repartidas a ambos lados).
        // SpinneretOffset solo sirve ahora para el hilo colgante.
        const float CableSeat = 0f;
        // Los cables van DETRÁS de la copa (orden 20) para que las hojas tapen la unión.
        const int CableOrder = 19, ThreadOrder = 22;
        float[] cableHeights;
        [SerializeField] Sprite tree, trunkSprite, web, stump;
        [SerializeField] Material cableMaterial;
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

        internal void ConfigureScenery(Sprite wetTree, Sprite wetTrunk, Sprite nestWeb, Sprite anchorStump, Material material)
        {
            tree = wetTree; trunkSprite = wetTrunk; web = nestWeb; stump = anchorStump; cableMaterial = material;
        }

        public void SetArena(Transform center, Vector2 size)
        {
            arena = center;
            if (scenery != null) return;
            scenery = new GameObject("NidoMegarachne");
            scenery.transform.SetParent(center, false);
            var trunk = new GameObject("Tronco");
            trunk.transform.SetParent(scenery.transform, false);
            var trunkHit = trunk.AddComponent<BoxCollider2D>();
            trunkHit.size = new Vector2(TrunkWidth, TrunkHeight);
            trunkHit.offset = Vector2.up * (TrunkHeight * 0.5f);
            // FloorBuilder crea Walls en Default; las bases comparten esa capa.
            trunk.layer = LayerMask.NameToLayer("Default");
            MakeSprite("BaseTronco", trunkSprite, Vector2.zero, 4);
            var crown = MakeSprite("Copa", tree, Vector2.up * 1.25f, 20);
            var nest = MakeSprite("Telarana", web, Vector2.zero, 21);
            Canopy = scenery.AddComponent<MegarachneBossCanopy>();
            Canopy.Configure(crown, nest);
            // Anclas exactas de anchor() del arte aprobado (coordenadas de pantalla, Y invertida).
            float[] angles = { 205f, 268f, 330f, 140f, 35f };
            Vector2[] outer = { new(-12f, 2f), new(0f, 4f), new(12f, 2f), new(-13f, -7f), new(13f, -7f) };
            starts = new Vector2[angles.Length];
            ends = new Vector2[angles.Length];
            cableHeights = new float[angles.Length];
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
                stumpHit.size = new Vector2(StumpWidth, StumpHeight);
                stumpHit.offset = new Vector2(0f, bounds.min.y - anchor.transform.position.y + StumpHeight * 0.5f);
                // El pie de subida queda delante de la base solida, a un radio del jefe.
                cableHeights[i] = ends[i].y - (bounds.min.y - HitRadius - ContactMargin * 2f);
                var line = MakeLine("Cable_" + i, CableWidth, CableOrder, true);
                line.SetPosition(0, starts[i]);
                line.SetPosition(1, ends[i]);
            }
            thread = MakeLine("HiloColgante", 0.08f, ThreadOrder, false);
            thread.enabled = false;
            // Vive en los cables: empieza sobre el más cercano al jugador (el centro lo ocupa el tronco).
            // Si la sala aún está desactivada (FloorBuilder), Awake no ha corrido: OnEnable lo colocará al entrar el jugador.
            if (isActiveAndEnabled) PlaceOnCable();
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

        LineRenderer MakeLine(string objectName, float width, int order, bool taper)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(scenery.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = cableMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.textureMode = LineTextureMode.Tile;
            line.sortingOrder = order;
            // Punta fina donde nace en la rama (índice 0) y un poco fina en el tocón: parece una rama que engorda.
            if (taper)
            {
                line.widthMultiplier = width;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(0.12f, 1f),
                    new Keyframe(0.9f, 1f), new Keyframe(1f, 0.7f));
            }
            return line;
        }

        static Vector2 ClosestOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            return a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(delta.sqrMagnitude, Mathf.Epsilon));
        }

        int NearestCable(Vector2 point)
        {
            int selected = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < CableCount; i++)
            {
                Vector2 lift = Vector2.up * cableHeights[i];
                float distance = (ClosestOnSegment(point, starts[i] - lift, ends[i] - lift) - point).sqrMagnitude;
                if (distance < best) { best = distance; selected = i; }
            }
            return selected;
        }

        void PlaceOnCable()
        {
            cableIndex = NearestCable(target != null ? (Vector2)target.position : ArenaCenter);
            Vector2 point = starts[cableIndex] - Vector2.up * cableHeights[cableIndex];
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
            cableIndex = NearestCable(body.position);
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

        void TickCable(float dt)
        {
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
                    if (wanted != cableIndex) outward = false;
                    Vector2 destination = (outward ? ends[cableIndex] : starts[cableIndex]) - Vector2.up * cableHeights[cableIndex];
                    facing = (destination - body.position).normalized;
                    body.MovePosition(Vector2.MoveTowards(body.position, destination, CableSpeed * dt));
                    Play("Walk", WalkMs);
                    if (Vector2.Distance(body.position, destination) <= CableSpeed * dt)
                    {
                        if (!outward && wanted != cableIndex)
                        {
                            // Cruza la copa entre las ramas; nunca baja al suelo para cambiar de cable.
                            routeStart = destination;
                            crossHeight = VisualHeight;
                            cableIndex = wanted;
                            routeEnd = starts[cableIndex] - Vector2.up * cableHeights[cableIndex];
                            travel = Travel.Cross;
                            travelTime = 0f;
                        }
                        outward = !outward;
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

        void DrawThread(Vector2 anchor)
        {
            thread.enabled = true;
            thread.SetPosition(0, anchor);
            thread.SetPosition(1, body.position + Vector2.up * (VisualHeight + SpinneretOffset));
        }
    }
}

