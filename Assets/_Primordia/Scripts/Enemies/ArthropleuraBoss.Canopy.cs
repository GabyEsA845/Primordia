using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, arte final de la sala (README del jefe, «Arte final de la sala»): la espesura por autotile con sus coronas,
    /// las brechas rotas, la alfombra y los troncos que deja al caer en la F2 y los efectos del lodo. Solo dibuja: las celdas,
    /// las colisiones y las fases siguen en Hunt y Phase2. Sin el arte importado, la espesura sigue siendo el tile de pared.
    /// Las máscaras cuentan los lados abiertos con N=1, E=2, S=4, O=8 y las esquinas con NE=1, SE=2, SO=4, NO=8.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        [System.Serializable]
        public class CanopyArt
        {
            /// <summary>Casillas de las tablas por máscara: (m × 16 + c) × <see cref="Variants"/> + variante.</summary>
            public const int Variants = 3;
            public Sprite[] inner, edges, breaks, crowns, treeCrowns, spillTop, spillLeft, spillBottom;
            public Sprite[] debrisDense, debrisLight, tracks;
            /// <summary>Alfombra caída: interior, y bordes por máscara (m × 16 + c).</summary>
            public Sprite[] mats, matEdges, fallenTrees, fallenFerns;
            public Sprite fallenTreeVertical;
            public Sprite[] canopyShake, mudDouse, mudWake;

            public bool IsComplete => inner != null && inner.Length > 0 && edges != null && edges.Length == 256 * Variants;
        }

        // Entre las paredes (-10) y las piezas del jefe (12): lo del suelo, abajo; las coronas, sobre la espesura.
        const int BreakOrder = -9, MatOrder = -8, TrackOrder = -7, FallenOrder = -6, WakeOrder = 4, CrownOrder = 6;
        const float TrackEvery = 2f, FallenJitterPixels = 6f, CrownJitterPixels = 4f;
        const float ShakeEvery = 0.22f, ShakeFrameSeconds = 0.11f, DouseFrameSeconds = 0.09f, WakeFrameSeconds = 0.1f, WakeEvery = 0.3f;
        static readonly Vector2Int[] Sides = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };
        // Cada esquina: su desplazamiento y los dos lados que, abiertos, ya la incluyen.
        static readonly (Vector2Int offset, int sides)[] Corners =
        {
            (new Vector2Int(1, 1), 1 | 2), (new Vector2Int(1, -1), 4 | 2), (new Vector2Int(-1, -1), 4 | 8), (new Vector2Int(-1, 1), 1 | 8),
        };

        [SerializeField] CanopyArt canopy = new();
        Transform canopyRoot;
        readonly Dictionary<Sprite, Tile> canopyTiles = new();
        readonly HashSet<Vector2Int> breached = new();
        readonly Dictionary<Vector2Int, SpriteRenderer> breakMarks = new(), spills = new(), debris = new(), mats = new(), crowns = new();
        HashSet<Vector2Int> collapseSet;
        Sprite wakeMaskSprite;
        bool wakeMasksBuilt;
        float nextShake;
        readonly Dictionary<int, float> nextWake = new();

        internal void ConfigureCanopy(CanopyArt art) => canopy = art;

        bool HasCanopyArt => canopy != null && canopy.IsComplete;

        /// <summary>Para pruebas: cuántas piezas de cada capa hay puestas.</summary>
        internal int CrownCount => crowns.Count;
        internal int BreakMarkCount => breakMarks.Count;
        internal int DebrisCount => debris.Count;
        internal int MatCount => mats.Count;

        static int CellHash(int x, int y, int salt) => (int)(((x * 73856093L) ^ (y * 19349663L) ^ (salt * 83492791L)) & 0x7fffffff);

        static Sprite Pick(Sprite[] sprites, int hash) => sprites == null || sprites.Length == 0 ? null : sprites[hash % sprites.Length];

        Vector3Int WallCell(Vector2Int rel) => wallMap.WorldToCell(center + new Vector2(rel.x + 0.5f, rel.y + 0.5f));

        /// <summary>Suelo sin nada encima: ni espesura ni muro de la sala.</summary>
        bool IsOpenFloor(Vector2Int rel)
        {
            if (thicket.Contains(rel))
                return false;
            var cell = WallCell(rel);
            return floorMap.HasTile(cell) && !wallMap.HasTile(cell);
        }

        static int MaskFor(Vector2Int rel, System.Func<Vector2Int, bool> open, out int corners)
        {
            int mask = 0;
            for (int i = 0; i < Sides.Length; i++)
                if (open(rel + Sides[i]))
                    mask |= 1 << i;
            corners = 0;
            for (int i = 0; i < Corners.Length; i++)
                if ((mask & Corners[i].sides) == 0 && open(rel + Corners[i].offset))
                    corners |= 1 << i;
            return mask;
        }

        static Sprite ByMask(Sprite[] table, int mask, int corners, int variant)
        {
            if (table == null)
                return null;
            int index = (mask * 16 + corners) * CanopyArt.Variants;
            if (index >= table.Length)
                return null;
            return table[index + variant] != null ? table[index + variant] : table[index];
        }

        Tile CanopyTile(Sprite sprite)
        {
            if (!canopyTiles.TryGetValue(sprite, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                canopyTiles[sprite] = tile;
            }
            return tile;
        }

        /// <summary>Redibuja esas celdas y sus vecinas (todo, al construir la espesura).</summary>
        void RefreshCanopy(IEnumerable<Vector2Int> changed)
        {
            if (!HasCanopyArt)
                return;
            if (canopyRoot == null)
            {
                canopyRoot = new GameObject("ArthropleuraCanopy").transform;
                canopyRoot.SetParent(wallRoom, false);
            }
            var area = new HashSet<Vector2Int>();
            foreach (var rel in changed)
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        area.Add(rel + new Vector2Int(x, y));
            foreach (var rel in area)
                RefreshCell(rel);
            // Las coronas van por bloques de 2×2: se miran los bloques cuyo entorno de 4×4 toca algo cambiado.
            var blocks = new HashSet<Vector2Int>();
            foreach (var rel in area)
                for (int x = -2; x <= 2; x += 2)
                    for (int y = -2; y <= 2; y += 2)
                        blocks.Add(new Vector2Int(Even(rel.x) + x, Even(rel.y) + y));
            foreach (var block in blocks)
                RefreshCrown(block);
        }

        static int Even(int value) => value & ~1;

        void RefreshCell(Vector2Int rel)
        {
            if (thicket.Contains(rel))
            {
                int mask = MaskFor(rel, IsOpenFloor, out int corners);
                int hash = CellHash(rel.x, rel.y, 2);
                Sprite sprite = mask == 0 && corners == 0
                    ? canopy.inner[hash % 3 != 0 || canopy.inner.Length < 2 ? 0 : 1 + hash / 3 % (canopy.inner.Length - 1)]
                    : ByMask(canopy.edges, mask, corners, CellHash(rel.x, rel.y, 8) % CanopyArt.Variants);
                if (sprite != null)
                    wallMap.SetTile(WallCell(rel), CanopyTile(sprite));
                // El filo roto solo en la espesura que toca una brecha; el claro conserva el filo limpio.
                var broken = TouchesBreach(rel) ? ByMask(canopy.breaks, mask, corners, CellHash(rel.x, rel.y, 8) % CanopyArt.Variants) : null;
                PutPiece(breakMarks, rel, broken, BreakOrder);
                PutPiece(spills, rel, null, MatOrder);
                PutPiece(debris, rel, null, MatOrder);
                return;
            }
            PutPiece(breakMarks, rel, null, BreakOrder);
            if (!IsOpenFloor(rel))
                return;
            if (breached.Contains(rel))
            {
                bool near = false;
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        near |= thicket.Contains(rel + new Vector2Int(x, y));
                PutPiece(debris, rel, Pick(near ? canopy.debrisDense : canopy.debrisLight, CellHash(rel.x, rel.y, 21)), MatOrder);
            }
            PutPiece(spills, rel, SpillFor(rel), MatOrder + 1);
            if (spills.TryGetValue(rel, out var spill))
            {
                // La solapa derecha es la izquierda volteada.
                int hash = CellHash(rel.x * 3, rel.y * 5, 0);
                spill.flipX = hash % 3 == 1 && thicket.Contains(rel + Vector2Int.right) && !(hash % 2 == 0 && thicket.Contains(rel + Vector2Int.up));
            }
        }

        /// <summary>Las solapas del tramo sobre el suelo pegado a la espesura (como en las paredes).</summary>
        Sprite SpillFor(Vector2Int rel)
        {
            if (transitionStarted || breached.Contains(rel))
                return null;
            int hash = CellHash(rel.x * 3, rel.y * 5, 0);
            if (thicket.Contains(rel + Vector2Int.up) && hash % 2 == 0) return Pick(canopy.spillTop, hash / 2);
            if (thicket.Contains(rel + Vector2Int.left) && hash % 3 == 0) return Pick(canopy.spillLeft, hash / 3);
            if (thicket.Contains(rel + Vector2Int.right) && hash % 3 == 1) return Pick(canopy.spillLeft, hash / 3);
            if (thicket.Contains(rel + Vector2Int.down) && hash % 2 == 1) return Pick(canopy.spillBottom, hash / 2);
            return null;
        }

        bool TouchesBreach(Vector2Int rel)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    if (breached.Contains(rel + new Vector2Int(x, y)))
                        return true;
            return false;
        }

        /// <summary>Pone, cambia o quita (sprite null) la pieza suelta de una celda.</summary>
        void PutPiece(Dictionary<Vector2Int, SpriteRenderer> layer, Vector2Int rel, Sprite sprite, int order)
        {
            layer.TryGetValue(rel, out var renderer);
            if (sprite == null)
            {
                if (renderer != null)
                    Destroy(renderer.gameObject);
                layer.Remove(rel);
                return;
            }
            if (renderer == null)
                layer[rel] = renderer = NewCanopyPiece(sprite, center + new Vector2(rel.x + 0.5f, rel.y + 0.5f), order);
            renderer.sprite = sprite;
        }

        SpriteRenderer NewCanopyPiece(Sprite sprite, Vector2 world, int order)
        {
            var go = new GameObject(sprite.name);
            go.transform.SetParent(canopyRoot, false);
            go.transform.position = world;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>Corona de helecho o copa de árbol sobre un bloque de 2×2 rodeado de espesura; se quita al romperse.</summary>
        void RefreshCrown(Vector2Int block)
        {
            bool wanted = CellHash(block.x / 2, block.y / 2, 9) % 5 < 4;
            for (int x = -1; x <= 2 && wanted; x++)
                for (int y = -1; y <= 2 && wanted; y++)
                    wanted = thicket.Contains(block + new Vector2Int(x, y));
            bool has = crowns.TryGetValue(block, out var crown);
            if (has && !wanted)
            {
                if (crown != null)
                    Destroy(crown.gameObject);
                crowns.Remove(block);
                // Al caer la espesura (F2), donde había copa queda un tronco o un helecho tumbado.
                if (transitionStarted)
                    DropFallen(block);
                return;
            }
            if (has || !wanted)
                return;
            int hash = CellHash(block.x / 2, block.y / 2, 4);
            bool tree = CellHash(block.x / 2, block.y / 2, 6) % 5 < 2;
            var sprite = Pick(tree ? canopy.treeCrowns : canopy.crowns, hash / 81);
            if (sprite == null)
                return;
            var jitter = new Vector2(hash % 9 - 4, hash / 9 % 9 - 4) * (CrownJitterPixels / 4f / 16f);
            crowns[block] = NewCanopyPiece(sprite, center + (Vector2)(block + Vector2Int.one) + jitter, CrownOrder);
        }

        void DropFallen(Vector2Int block)
        {
            int hash = CellHash(block.x / 2, block.y / 2, 4);
            bool tree = CellHash(block.x / 2, block.y / 2, 6) % 5 < 2;
            Sprite sprite;
            if (tree && hash % 2 == 1)
            {
                int kind = hash / 81 % 3;
                sprite = kind == 2 ? canopy.fallenTreeVertical : Pick(canopy.fallenTrees, kind);
            }
            else if (hash % 3 == 1)
                sprite = Pick(canopy.fallenFerns, hash / 81);
            else
                return;
            var middle = center + (Vector2)(block + Vector2Int.one);
            if (sprite == null || MudProbe(middle))
                return;
            var jitter = new Vector2(hash % 13 - 6, hash / 13 % 13 - 6) * (FallenJitterPixels / 6f / 16f);
            var renderer = NewCanopyPiece(sprite, middle + jitter, FallenOrder);
            renderer.flipX = hash % 3 == 0 && sprite != canopy.fallenTreeVertical;
        }

        /// <summary>Anota las celdas que abre una embestida (restos en el suelo y filo roto alrededor).</summary>
        void MarkBreached(List<Vector2Int> cells)
        {
            breached.UnionWith(cells);
            RefreshCanopy(cells);
        }

        /// <summary>Surcos de las patas por el eje de la brecha, cada 2 u y solo sobre celdas rotas.</summary>
        void LayTracks(Vector2 originWorld, Vector2 directionWorld, float from, float to)
        {
            if (!HasCanopyArt || canopyRoot == null || canopy.tracks == null || canopy.tracks.Length == 0)
                return;
            float angle = Mathf.Atan2(directionWorld.y, directionWorld.x) * Mathf.Rad2Deg;
            int index = 0;
            for (float along = Mathf.Max(0f, from); along <= to; along += TrackEvery, index++)
            {
                Vector2 rel = originWorld + directionWorld * along;
                if (!breached.Contains(CellOf(rel)))
                    continue;
                var track = NewCanopyPiece(canopy.tracks[index % canopy.tracks.Length], center + rel, TrackOrder);
                track.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>Empieza la caída: qué celdas llevarán alfombra (la espesura que queda, sin los lagos que se destapan).</summary>
        void BeginCanopyFall()
        {
            if (!HasCanopyArt)
                return;
            collapseSet = new HashSet<Vector2Int>();
            foreach (var rel in thicket)
                if (!MudProbe(center + new Vector2(rel.x + 0.5f, rel.y + 0.5f)))
                    collapseSet.Add(rel);
            foreach (var rel in new List<Vector2Int>(spills.Keys))
                PutPiece(spills, rel, null, MatOrder);
        }

        /// <summary>Abierto para la alfombra: suelo que no era espesura al caer (claro, brechas) y los lagos. Junto a los muros sigue entera.</summary>
        bool IsOpenForMat(Vector2Int rel)
        {
            if (collapseSet.Contains(rel))
                return false;
            var cell = WallCell(rel);
            // Una celda de espesura que aún no ha caído tiene tile: solo cuenta si es lodo.
            return floorMap.HasTile(cell) && (!wallMap.HasTile(cell) || thicket.Contains(rel));
        }

        void LayMats(List<Vector2Int> fallen)
        {
            if (!HasCanopyArt || collapseSet == null)
                return;
            foreach (var rel in fallen)
            {
                if (!collapseSet.Contains(rel))
                    continue;
                int mask = MaskFor(rel, IsOpenForMat, out int corners);
                Sprite sprite = mask == 0 && corners == 0 ? Pick(canopy.mats, CellHash(rel.x, rel.y, 31))
                    : canopy.matEdges != null && canopy.matEdges.Length > mask * 16 + corners ? canopy.matEdges[mask * 16 + corners] : null;
                PutPiece(mats, rel, sprite, MatOrder);
            }
        }

        /// <summary>F1: hojas y ramitas que suelta la espesura por donde pasa el cuerpo escondido.</summary>
        void ShakeCanopy()
        {
            if (!HasCanopyArt || PhaseIndex > 0 || pieces.Count == 0 || Time.time < nextShake)
                return;
            nextShake = Time.time + ShakeEvery;
            Vector2 ground = NodeGround(Random.Range(0, pieces.Count));
            if (IsHidden(ground))
                SpawnEffect(canopy.canopyShake, GroundToWorld(ground) + Vector2.up, 0f, ShakeFrameSeconds);
        }

        /// <summary>Una pieza ardiendo entra en el lodo y se apaga.</summary>
        void SplashDouse(Vector2 world)
        {
            if (canopy != null)
                SpawnEffect(canopy.mudDouse, world, 0f, DouseFrameSeconds);
        }

        /// <summary>Onda en el lodo bajo cada pieza que avanza por él, recortada para no pisar la orilla.</summary>
        void StirMud()
        {
            if (PhaseIndex < 1 || !Moving || wheelActive || canopy == null || canopy.mudWake == null || canopy.mudWake.Length == 0)
                return;
            if (CurrentMode is Mode.Phase2Pending or Mode.Phase3Pending or Mode.Stunned or Mode.Dead)
                return;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (nextWake.TryGetValue(i, out float next) && Time.time < next)
                    continue;
                Vector2 world = GroundToWorld(NodeGround(i));
                if (!MudProbe(world))
                    continue;
                // Desfasadas entre piezas: no salen todas las ondas a la vez.
                nextWake[i] = Time.time + WakeEvery;
                BuildWakeMasks();
                var go = new GameObject("MudWake");
                go.transform.SetParent(transform, false);
                go.transform.position = world;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = WakeOrder;
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                go.AddComponent<OneShotSprite>().Play(canopy.mudWake, WakeFrameSeconds);
            }
        }

        /// <summary>Una máscara por celda de lodo rodeada de lodo, que solo afecta a las ondas.</summary>
        void BuildWakeMasks()
        {
            if (wakeMasksBuilt)
                return;
            wakeMasksBuilt = true;
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            wakeMaskSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            var parent = new GameObject("MudWakeMasks").transform;
            parent.SetParent(transform, true);
            for (int x = -Mathf.CeilToInt(ArenaWidth / 2f); x < Mathf.CeilToInt(ArenaWidth / 2f); x++)
                for (int y = -Mathf.CeilToInt(ArenaHeight / 2f); y < Mathf.CeilToInt(ArenaHeight / 2f); y++)
                {
                    Vector2 world = center + new Vector2(x + 0.5f, y + 0.5f);
                    if (!MudProbe(world) || !MudProbe(world + Vector2.up) || !MudProbe(world + Vector2.down)
                        || !MudProbe(world + Vector2.left) || !MudProbe(world + Vector2.right))
                        continue;
                    var go = new GameObject("Mask");
                    go.transform.SetParent(parent, false);
                    go.transform.position = world;
                    var mask = go.AddComponent<SpriteMask>();
                    mask.sprite = wakeMaskSprite;
                    mask.isCustomRangeActive = true;
                    mask.frontSortingOrder = WakeOrder;
                    mask.backSortingOrder = WakeOrder - 1;
                }
        }
    }
}
