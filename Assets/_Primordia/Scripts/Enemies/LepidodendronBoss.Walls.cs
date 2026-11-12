using System;
using System.Collections.Generic;
using System.Linq;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        public const int WallHealth = 150;
        public const float WaveInterval = 12f;
        public const int AliveLimit = 6, WavesPerWall = 3;
        const float SpawnPlayerClearance = 5f, SpawnRadius = 0.65f, SpawnSpacing = 2f, SpawnMargin = 2f;
        const float SupplyWest = 2.5f; // Provisional: distancia al eje de la pared.
        // Provisionales: tamaños y composición, odres/helechos y destello de aparición.
        const int FirstWaveSize = 3, LaterWaveSize = 4, LastWallWaveSize = 4;
        const float SpawnFlashSeconds = 0.25f;
        static int[] WaveSpecies(int wall) => wall == 0 ? new[] { 0, 1 } : wall == 1 ? new[] { 2, 3 } : new[] { 4, 0, 1, 2, 3 };
        static float[] SacRows => new[] { -5f, 5f }; // Provisionales; deja el eje del pasillo despejado.
        static float[] FernRows => new[] { -7f, -3f, 3f }; // Provisionales.

        [SerializeField] FloorConfig encounterConfig;
        [SerializeField] Enemy[] waveEnemies = Array.Empty<Enemy>(); // Calamites, sapo, araña, escarabajo, libélula.
        [SerializeField] FernSpec wallFern;
        [SerializeField] MethaneSpec wallMethane;
        readonly List<LepidodendronWall> walls = new();
        readonly List<WallWave> wallWaves = new();
        readonly HashSet<Enemy> waveFamily = new();
        readonly List<(SpriteRenderer renderer, Color color, float until)> spawning = new();
        Transform wallRoom;
        BossDamageLimiter wallLimiter;

        public IReadOnlyList<LepidodendronWall> Walls => walls;
        public bool FinalPhaseReady { get; private set; }
        public event Action FinalPhaseBecameReady;
        public FloorConfig EncounterConfig => encounterConfig;

        internal void ConfigureWallCombat(FloorConfig config, Enemy[] enemies, FernSpec fern, MethaneSpec methane)
        {
            encounterConfig = config;
            waveEnemies = enemies;
            wallFern = fern;
            wallMethane = methane;
        }

        internal int LimitWallDamage(int amount, DamageSource source)
        {
            if (wallLimiter == null)
                wallLimiter = GetComponent<BossDamageLimiter>();
            return wallLimiter != null ? wallLimiter.Apply(amount, source, Time.time) : amount;
        }

        internal void WallLostHealth(int amount) => Health.Drain(amount);

        internal void WallFell()
        {
            if (FinalPhaseReady || walls.Count != 3 || walls.Any(w => !w.Fallen))
                return;
            FinalPhaseReady = true;
            FinalPhaseBecameReady?.Invoke();
        }

        internal void StartWallWaves(LepidodendronWall wall)
        {
            if (wallWaves.Any(w => w.wall == wall) || wall.Fallen)
                return;
            var wave = new WallWave { wall = wall, nextAt = Time.time };
            wallWaves.Add(wave);
            TickWave(wave);
        }

        public LepidodendronWall NearestWall(Vector2 position) => walls.Where(w => !w.Fallen)
            .OrderBy(w => Vector2.Distance(w.BodyCollider.ClosestPoint(position), position)).FirstOrDefault();

        internal int LivingWaveEnemies => wallRoom == null ? 0 : wallRoom.GetComponentsInChildren<Enemy>()
            .Count(e => e != GetComponent<Enemy>() && e.Health != null && !e.Health.IsDead);

        internal int PendingFor(LepidodendronWall wall) => wallWaves.FirstOrDefault(w => w.wall == wall)?.pending.Count ?? 0;
        internal int WavesCompletedFor(LepidodendronWall wall) => wallWaves.FirstOrDefault(w => w.wall == wall)?.completed ?? 0;

        internal void CancelWallWaves(LepidodendronWall wall)
        {
            var wave = wallWaves.FirstOrDefault(w => w.wall == wall);
            wave?.pending.Clear();
        }

        void UpdateWallWaves()
        {
            foreach (var wave in wallWaves)
                TickWave(wave);
            for (int i = spawning.Count - 1; i >= 0; i--)
            {
                var flash = spawning[i];
                if (flash.renderer != null)
                    flash.renderer.color = Time.time < flash.until ? Color.Lerp(Color.white, flash.color,
                        1f - (flash.until - Time.time) / SpawnFlashSeconds) : flash.color;
                if (flash.renderer == null || Time.time >= flash.until)
                    spawning.RemoveAt(i);
            }
        }

        void TickWave(WallWave wave)
        {
            if (wave.wall.Fallen)
            {
                wave.pending.Clear();
                return;
            }
            if (encounterConfig == null || waveEnemies.Length != 5)
                return;
            if (wave.pending.Count == 0)
            {
                if (wave.completed >= WavesPerWall || Time.time < wave.nextAt)
                    return;
                int size = wave.wall.Index == 2 ? LastWallWaveSize : wave.completed == 0 ? FirstWaveSize : LaterWaveSize;
                int[] pool = WaveSpecies(wave.wall.Index);
                for (int i = 0; i < size; i++)
                    wave.pending.Enqueue(waveEnemies[pool[(wave.completed * size + i) % pool.Length]]);
            }
            while (wave.pending.Count > 0 && LivingWaveEnemies < AliveLimit)
            {
                if (!FindWallSpawn(wave.wall.Index, out var position))
                    return;
                var enemies = EncounterPlanner.Spawn(encounterConfig, wave.pending.Dequeue(), position, wallRoom);
                foreach (var enemy in enemies)
                {
                    enemy.DropsBiomass = false;
                    GetComponent<Enemy>().RegisterChild(enemy);
                    TrackWaveFamily(enemy);
                    if (enemy.TryGetComponent<EnemyAI>(out var ai))
                    {
                        if (player != null) ai.Target = player;
                        ai.Alert();
                    }
                    foreach (var r in enemy.GetComponentsInChildren<SpriteRenderer>())
                        spawning.Add((r, r.color, Time.time + SpawnFlashSeconds));
                }
            }
            if (wave.pending.Count == 0)
            {
                wave.completed++;
                // La cola conserva el orden. Los 12 s cuentan desde el ÚLTIMO enemigo que pudo salir, no desde el anuncio.
                wave.nextAt = Time.time + WaveInterval;
            }
        }

        bool FindWallSpawn(int index, out Vector2 position)
        {
            Physics2D.SyncTransforms(); // Las apariciones de esta misma oleada ya deben ocupar su sitio en la consulta siguiente.
            if (player == null)
            {
                var controller = FindAnyObjectByType<Primordia.Player.PlayerController>();
                if (controller != null) player = controller.transform;
            }
            float west = index == 0 ? 0f : WallX(index - 1) + WallThickness(index - 1) / 2f;
            float east = WallX(index) - WallThickness(index) / 2f;
            for (float x = west + SpawnMargin; x <= east - SpawnMargin; x += SpawnSpacing)
                for (float y = -CorridorHeight / 2f + SpawnMargin; y <= CorridorHeight / 2f - SpawnMargin; y += SpawnSpacing)
                {
                    var spot = entrance + new Vector2(x, y);
                    if (player != null && Vector2.Distance(player.position, spot) < SpawnPlayerClearance)
                        continue;
                    if (Physics2D.OverlapCircleAll(spot, SpawnRadius).Any(c => !c.isTrigger))
                        continue;
                    position = spot;
                    return true;
                }
            position = default;
            return false;
        }

        void TrackWaveFamily(Enemy enemy)
        {
            if (waveFamily.Add(enemy))
                enemy.ChildSpawned += OnWaveChild;
        }

        void OnWaveChild(Enemy child)
        {
            child.DropsBiomass = false;
            // También cuentan los retoños que ya produce la IA de Calamites. No se modifica su cerebro.
            if (LivingWaveEnemies > AliveLimit)
            {
                child.Health.Kill(); // Avisa a Room para deshacer su registro, sin biomasa ni un enemigo invisible vivo.
                child.gameObject.SetActive(false);
                return;
            }
            TrackWaveFamily(child);
        }

        void ClearWaveFamily()
        {
            foreach (var enemy in waveFamily)
                if (enemy != null)
                    enemy.ChildSpawned -= OnWaveChild;
            waveFamily.Clear();
        }

        void BuildWallSupplies(int index)
        {
            if (wallMethane == null || wallFern == null)
                return;
            var holder = new GameObject($"Wall{index + 1}Supplies").transform;
            holder.SetParent(scenery, true);
            foreach (float y in SacRows)
                MethaneSac.Create(holder, entrance + new Vector2(WallX(index) - SupplyWest, y),
                    wallMethane.sacIdle, wallMethane.sacBurst, wallMethane.sacRemains);
            foreach (float y in FernRows)
            {
                var obj = new GameObject("WallFern");
                obj.transform.SetParent(holder, true);
                obj.transform.position = entrance + new Vector2(WallX(index) - SupplyWest, y);
                var renderer = obj.AddComponent<SpriteRenderer>();
                var trigger = obj.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                trigger.radius = wallFern.footRadius;
                trigger.offset = new Vector2(0f, wallFern.footRadius * 0.6f);
                obj.AddComponent<Fern>().Configure(renderer, wallFern, index);
            }
        }

        sealed class WallWave
        {
            public LepidodendronWall wall;
            public readonly Queue<Enemy> pending = new();
            public int completed;
            public float nextAt;
        }
    }
}
