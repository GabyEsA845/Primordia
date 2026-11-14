using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso B1 (F1): el claro de 28×16 u cerrado por una espesura (tiles de pared que el jefe quita), el cuerpo
    /// escondido rodeando la sala bajo la cresta de frondas, y el ciclo rodear → abrir brecha y asomar → aviso → embestir → retirarse.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        public enum Mode { Circling, Warn, Charge, Phase2Pending }

        /// <summary>Medio claro, en unidades de mundo (28×16 u).</summary>
        public static readonly Vector2 ClearingHalf = new(14f, 8f);
        /// <summary>Medio anillo por el que rodea escondido, en unidades de suelo.</summary>
        public static readonly Vector2 RingHalf = new(17.5f, 13.2f);
        public const float BreachWidth = 6f; // ≥ 5 u: el cuerpo mide 4 u más las patas.
        public const int ContactDamage = 1, ChargeDamage = 2;

        const float HitRadius = 1.8f, ChargeHitRadius = 2.2f;
        const float Camera1Zoom = 1.3f, CameraSeconds = 0.8f;

        [System.Serializable]
        public class Effects
        {
            public Sprite[] edgeBreak, tremor, chargeAim, chargeDust;
        }

        [SerializeField] Effects effects = new();
        Transform wallRoom;
        Tilemap wallMap, floorMap, blockMap; // blockMap: colision sin dibujo en los agujeros que deja el jefe.
        readonly HashSet<Vector2Int> thicket = new();
        TileBase thicketTile;
        bool thicketBuilt, entriesSealed;
        readonly List<Vector2Int> entryCells = new();
        float modeTimer;
        bool chargeHit;
        CameraZoom zoom;
        float zoomAmount;

        public Mode CurrentMode { get; private set; }
        /// <summary>Celdas de espesura que quedan (para pruebas).</summary>
        public int ThicketCells => thicket.Count;
        /// <summary>Celdas de agujero que siguen bloqueadas con la pared invisible (para pruebas).</summary>
        public int BlockedCells { get; private set; }

        internal void ConfigureEffects(Effects newEffects) => effects = newEffects;

        static Vector2 RingPoint(float theta)
        {
            float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
            return new Vector2(RingHalf.x * Mathf.Sign(c) * Mathf.Sqrt(Mathf.Abs(c)), RingHalf.y * Mathf.Sign(s) * Mathf.Sqrt(Mathf.Abs(s)));
        }

        static float RingAngle(Vector2 ground)
        {
            float c = ground.x / RingHalf.x, s = ground.y / RingHalf.y;
            return Mathf.Atan2(Mathf.Sign(s) * s * s, Mathf.Sign(c) * c * c);
        }

        Vector2Int CellOf(Vector2 worldRelative) => new(Mathf.FloorToInt(worldRelative.x), Mathf.FloorToInt(worldRelative.y));

        bool InClearing(Vector2 worldRelative) => Mathf.Abs(worldRelative.x) < ClearingHalf.x && Mathf.Abs(worldRelative.y) < ClearingHalf.y;

        /// <summary>Si la pieza está bajo la espesura (se ve solo la cresta de frondas).</summary>
        bool IsHidden(Vector2 ground)
        {
            if (PhaseIndex > 0 || thicket.Count == 0)
                return false;
            return thicket.Contains(CellOf(new Vector2(ground.x, ground.y * GroundSquash)));
        }

        DirectionalArt.Sheet OverrideSheet(int piece)
        {
            if (piece != 0 || art == null)
                return null;
            if (IntroStage == EntryStage.Roaring) return art.Find("Arthropleura_Head_Rear");
            return CurrentMode switch
            {
                Mode.Warn => art.Find("Arthropleura_Head_ChargeWarn"),
                Mode.Charge => art.Find("Arthropleura_Head_Charge"),
                _ => null,
            };
        }

        /// <summary>Rellena de espesura todo el suelo de la sala salvo el claro (con los tiles de pared del propio piso).</summary>
        void BuildThicket()
        {
            thicket.Clear();
            var level = wallRoom != null ? wallRoom.parent : null;
            if (level == null)
                return;
            foreach (var map in level.GetComponentsInChildren<Tilemap>())
            {
                if (map.name == "Walls") wallMap = map;
                else if (map.name == "Ground") floorMap = map;
            }
            if (wallMap == null || floorMap == null)
                return;
            // Un tile de pared de esta sala, como espesura provisional (la espesura propia de la F1 queda para el arte).
            var cell0 = wallMap.WorldToCell(center);
            for (int r = 0; r < 40 && thicketTile == null; r++)
                for (int dx = -r; dx <= r && thicketTile == null; dx++)
                    foreach (int dy in new[] { -r, r })
                        if ((thicketTile = wallMap.GetTile(cell0 + new Vector3Int(dx, dy, 0))) != null)
                            break;
            if (thicketTile == null)
                return;
            CreateBlockMap();
            // Pasillo de 6 u desde cada puerta de la sala hasta el claro: la entrada nunca queda sellada.
            var entries = new List<Vector2>();
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                Vector2 rel = (Vector2)door.transform.position - center;
                if (Mathf.Abs(rel.x) <= ArenaWidth / 2f + 4f && Mathf.Abs(rel.y) <= ArenaHeight / 2f + 4f)
                    entries.Add(rel);
            }
            for (int x = -Mathf.CeilToInt(ArenaWidth / 2f); x < Mathf.CeilToInt(ArenaWidth / 2f); x++)
                for (int y = -Mathf.CeilToInt(ArenaHeight / 2f); y < Mathf.CeilToInt(ArenaHeight / 2f); y++)
                {
                    var rel = new Vector2Int(x, y);
                    var cell = wallMap.WorldToCell(center + new Vector2(x + 0.5f, y + 0.5f));
                    bool insideClearing = Mathf.Abs(x + 0.5f) < ClearingHalf.x && Mathf.Abs(y + 0.5f) < ClearingHalf.y;
                    if (insideClearing || !floorMap.HasTile(cell))
                        continue;
                    if (InEntryCorridor(entries, x + 0.5f, y + 0.5f))
                    {
                        entryCells.Add(rel);
                        continue;
                    }
                    wallMap.SetTile(cell, thicketTile);
                    thicket.Add(rel);
                }
            RefreshWallCollider();
        }

        /// <summary>Tilemap hermano de Walls, sin renderer: solo colisiona. Los agujeros de la espesura quedan cerrados al jugador.</summary>
        void CreateBlockMap()
        {
            var go = new GameObject("ThicketBlock", typeof(Tilemap), typeof(TilemapCollider2D));
            go.transform.SetParent(wallMap.transform.parent, false);
            go.layer = wallMap.gameObject.layer;
            blockMap = go.GetComponent<Tilemap>();
            blockMap.transform.position = wallMap.transform.position;
        }

        static bool InEntryCorridor(List<Vector2> entries, float x, float y)
        {
            foreach (var entry in entries)
            {
                bool horizontal = Mathf.Abs(entry.x) / (ArenaWidth / 2f) > Mathf.Abs(entry.y) / (ArenaHeight / 2f);
                if (horizontal ? Mathf.Abs(y - entry.y) <= 3f && x * entry.x > 0f : Mathf.Abs(x - entry.x) <= 3f && y * entry.y > 0f)
                    return true;
            }
            return false;
        }

        /// <summary>Con el jugador ya dentro del claro, la espesura se cierra tras él (los pasillos de las puertas).</summary>
        void SealEntries()
        {
            entriesSealed = true;
            foreach (var rel in entryCells)
            {
                wallMap.SetTile(wallMap.WorldToCell(center + new Vector2(rel.x + 0.5f, rel.y + 0.5f)), thicketTile);
                thicket.Add(rel);
            }
            RefreshWallCollider();
        }

        /// <summary>Quita la pared invisible de los agujeros (la pared derribada del 70 %).</summary>
        void ClearBlockers()
        {
            if (blockMap == null)
                return;
            blockMap.ClearAllTiles();
            BlockedCells = 0;
            if (blockMap.TryGetComponent<TilemapCollider2D>(out var collider))
                collider.ProcessTilemapChanges();
        }

        void RefreshWallCollider()
        {
            if (wallMap != null && wallMap.TryGetComponent<TilemapCollider2D>(out var collider))
                collider.ProcessTilemapChanges();
        }

        /// <summary>Quita las celdas de espesura que cumplan la condición (la brecha de la F1, la pared del 70 %).</summary>
        int ClearThicket(System.Func<Vector2Int, bool> where, bool block = true, List<Vector2Int> removed = null)
        {
            int count = 0;
            foreach (var rel in new List<Vector2Int>(thicket))
            {
                if (!where(rel))
                    continue;
                var cell = wallMap.WorldToCell(center + new Vector2(rel.x + 0.5f, rel.y + 0.5f));
                wallMap.SetTile(cell, null);
                if (blockMap != null && block)
                {
                    blockMap.SetTile(cell, thicketTile);
                    BlockedCells++;
                }
                thicket.Remove(rel);
                removed?.Add(rel);
                count++;
            }
            RefreshWallCollider();
            if (blockMap != null && blockMap.TryGetComponent<TilemapCollider2D>(out var blockCollider))
                blockCollider.ProcessTilemapChanges();
            return count;
        }

        void FaceHead(Vector2 direction) => heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        /// <summary>Contacto con el cuerpo visible (1) y con la cabeza al embestir (2).</summary>
        void DamagePlayer()
        {
            if (player == null || playerHealth == null || playerHealth.IsDead)
                return;
            Vector2 target = WorldToGround(player.position);
            if (CurrentMode == Mode.Charge && !chargeHit && Vector2.Distance(headPos, target) <= ChargeHitRadius)
            {
                chargeHit = true;
                playerHealth.TakeDamage(ChargeDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
                return;
            }
            for (int i = 0; i < pieces.Count; i++)
            {
                Vector2 node = NodeGround(i);
                if (IsHidden(node) || Vector2.Distance(node, target) > HitRadius)
                    continue;
                playerHealth.TakeDamage(ContactDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
                return;
            }
        }

        void SpawnEffect(Sprite[] frames, Vector2 world, float angle, float step)
        {
            if (frames == null || frames.Length == 0)
                return;
            var go = new GameObject("Fx_Arthropleura");
            go.transform.SetParent(transform, false);
            go.transform.position = world;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 60 * 4; // Por encima de las piezas y sus tres capas.
            go.AddComponent<OneShotSprite>().Play(frames, step);
        }

        void UpdateCamera()
        {
            if (player == null)
                return;
            Vector2 rel = (Vector2)player.position - center;
            bool inside = Mathf.Abs(rel.x) <= ArenaWidth / 2f && Mathf.Abs(rel.y) <= ArenaHeight / 2f;
            float target = inside && PhaseIndex == 0 ? 1f : 0f;
            if (target > 0f && zoom == null)
            {
                zoom = new CameraZoom(Camera1Zoom);
                // Fijo el tamaño real de la cámara de ahora: ×1,3.
            }
            if (zoom == null)
                return;
            zoomAmount = Mathf.MoveTowards(zoomAmount, target, Time.deltaTime / CameraSeconds);
            zoom.Set(zoomAmount);
            if (target == 0f && zoomAmount <= 0f)
                ReleaseCamera();
        }

        void ReleaseCamera()
        {
            zoom?.Restore();
            zoom = null;
            zoomAmount = 0f;
        }
    }
}
