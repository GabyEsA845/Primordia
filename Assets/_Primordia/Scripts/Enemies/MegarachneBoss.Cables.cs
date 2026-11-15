using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // Provisionales: bases solidas y punto de amarre del subpaso M1.
        const float TrunkWidth = 2f, TrunkHeight = 1.2f, StumpWidth = 1f, StumpHeight = 0.8f;
        const float AnchorFraction = 0.8f, StumpLift = 1.5f;
        float[] cableHeights;
        [SerializeField] Sprite tree, trunkSprite, web, stump;
        [SerializeField] Material cableMaterial;
        Vector2[] starts, ends;
        int cableIndex;
        Vector2 routeStart, routeEnd;
        LineRenderer thread;
        float dropHeight;
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
                var line = MakeLine("Cable_" + i, 1f);
                line.SetPosition(0, starts[i]);
                line.SetPosition(1, ends[i]);
            }
            thread = MakeLine("HiloColgante", 0.08f);
            thread.enabled = false;
            // El centro está ocupado por el tronco; el jefe empieza delante del árbol.
            transform.position = ArenaCenter + Vector2.down * SpawnDistance;
            if (TryGetComponent<Rigidbody2D>(out var rb)) rb.position = transform.position;
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

        LineRenderer MakeLine(string objectName, float width)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(scenery.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = cableMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = width;
            line.textureMode = LineTextureMode.Tile;
            line.sortingOrder = 22;
            return line;
        }

        public bool ForceClimb()
        {
            if (CurrentMode != Mode.Ground || travel != Travel.None || CableCount == 0) return false;
            // Primero llega andando al pie del extremo más cercano; nunca salta de posición.
            float nearest = float.PositiveInfinity;
            int selected = -1;
            for (int i = 0; i < CableCount; i++)
            {
                Vector2 delta = ends[i] - Vector2.up * cableHeights[i] - body.position;
                float distance = delta.magnitude;
                if (distance < nearest) { nearest = distance; selected = i; }
            }
            if (selected < 0) return false;
            cableIndex = selected;
            detour = null;
            routeStart = ends[cableIndex];
            routeEnd = starts[cableIndex];
            travel = Travel.Approach;
            return true;
        }

        public void ForceDrop()
        {
            if (CurrentMode != Mode.OnCable)
            {
                if (travel == Travel.Approach) Land();
                return;
            }
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
                case Travel.Approach:
                    Vector2 delta = routeStart - Vector2.up * cableHeights[cableIndex] - body.position;
                    MoveGround(delta, GroundSpeed * dt);
                    Play("Walk", WalkMs);
                    if (delta.magnitude <= GroundSpeed * dt + ContactMargin)
                    {
                        CurrentMode = Mode.OnCable;
                        Health.IsInvulnerable = true;
                        travel = Travel.Climb;
                        travelTime = 0f;
                    }
                    break;
                case Travel.Climb:
                    SetHeight(Mathf.Lerp(0f, cableHeights[cableIndex] - SpinneretOffset, travelTime / ClimbSeconds));
                    Play("Climb", ClimbMs);
                    DrawThread(routeStart);
                    if (travelTime >= ClimbSeconds) { travel = Travel.Run; thread.enabled = false; }
                    break;
                case Travel.Run:
                    Vector2 destination = routeEnd - Vector2.up * cableHeights[cableIndex];
                    facing = (destination - body.position).normalized;
                    body.MovePosition(Vector2.MoveTowards(body.position, destination, CableSpeed * dt));
                    Play("Walk", WalkMs);
                    if (Vector2.Distance(body.position, destination) <= CableSpeed * dt) ForceDrop();
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

