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
        // Provisionales: cadencias, márgenes y radios de combate de L4.
        public const float WhipInterval = 7f, SpikesInterval = 9f, RainInterval = 12f, CornerInterval = 20f;
        public const float AttackStartDelay = 2f, AttackGap = 1.2f, WallRainInterval = 10f, WaveRainDelay = 3f;
        public const float WhipWarning = 0.8f, SpikesWarning = 0.78f, SporeFall = 1f, SporeDelay = 1.2f;
        public const float WhipWidth = 1.3f, SpikesRadius = 1.3f, SporeRadius = 0.9f;
        const float AttackReduction = 0.15f, MinimumAttackScale = 0.5f;
        const int RootDamage = 2, SporeDamage = 1, FinalSporeCount = 6, NearSporeCount = 2;
        const float WhipFrame = 0.055f, SpikesFrame = 0.08f, SplashFrame = 0.06f;
        const float FurrowFade = 1f, ScarFade = 0.6f, CornerWarning = 1f;
        const float AttackMargin = 2f, WhipMargin = 1f, WallRainReach = 4f, NearSporeRadius = 2f;
        const float FallingHeight = 18f, ArcHeight = 5f; // Provisionales, solo trayectoria visual.
        const int AttackGroundOrder = 7, AttackBackOrder = 9, AttackFrontOrder = 11;
        const int MarkerPixels = 32;
        readonly Queue<AttackKind> attackQueue = new();
        readonly List<AttackVisual> attacks = new();
        readonly Dictionary<LepidodendronWall, float> wallRainAt = new();
        readonly Dictionary<LepidodendronWall, float> wallWaveAt = new();
        readonly float[] attackRemaining = new float[4];
        float attackClock, nextAttackStart, finalAttacksAt, previousAttackScale = 1f;
        bool attacksStarted, attacksCancelled, nextWhipRight;
        Texture2D cornerTexture;
        Sprite cornerSprite;
        internal enum AttackKind { Whip, Spikes, Rain, Corners }
        internal bool AutomaticAttacks { get; private set; } = true;
        internal int ActiveAttackCount => attacks.Count;
        internal int PendingAttackCount => attackQueue.Count;
        internal float LastAttackStart { get; private set; } = float.NegativeInfinity;
        internal float AttackWarningUntil { get; private set; }
        // Cadencia activa: 100 %, 85 %, 70 %. El tercer cono termina la fase; el suelo del 50 % es una salvaguarda.
        internal static float AttackCadenceScale(int broken) => Mathf.Max(MinimumAttackScale, 1f - AttackReduction * broken);
        internal float AttackInterval(AttackKind kind) => (kind switch
        {
            AttackKind.Whip => WhipInterval, AttackKind.Spikes => SpikesInterval,
            AttackKind.Rain => RainInterval, _ => CornerInterval
        }) * AttackCadenceScale(cones.Count(c => c.Broken));

        internal static float WhipTip(int frame) => frame switch
        {
            1 => 14f, 2 => 32f, 3 => 84f, 4 => 132f, 5 => 175f, 6 => 214f, 7 => 247f,
            8 => 276f, 9 => 299f, 10 => 317f, 11 => 329f, 12 or 13 or 14 => 334f,
            15 => 292f, 16 => 236f, 17 => 172f, 18 => 108f, 19 => 52f, 20 => 22f, _ => 0f
        };

        void SubscribeAttacks()
        {
            FinalPhaseBecameReady += StartFinalAttacks;
            ConesDestroyed += FinishAttacks;
            Room.PlayerEnteredAny += AttackRoomEntered;
        }

        void UnsubscribeAttacks()
        {
            FinalPhaseBecameReady -= StartFinalAttacks;
            ConesDestroyed -= FinishAttacks;
            Room.PlayerEnteredAny -= AttackRoomEntered;
            CancelAttacks();
        }

        internal void AttackRoomEntered(Room room)
        {
            if (wallRoom != null && room != null && room.transform != wallRoom && !transform.IsChildOf(room.transform))
                CancelAttacks();
        }

        void StartFinalAttacks()
        {
            attacksStarted = true;
            finalAttacksAt = attackClock + AttackStartDelay;
            for (int i = 0; i < attackRemaining.Length; i++) attackRemaining[i] = AttackStartDelay;
        }

        void FinishAttacks()
        {
            attackQueue.Clear();
            foreach (var a in attacks) a.harmless = true;
            ClearAttackEnemies();
        }

        void ClearAttackEnemies()
        {
            foreach (var enemy in waveFamily.ToArray())
                if (enemy != null)
                {
                    enemy.Health.Kill();
                    if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
                }
        }

        void CancelAttacks()
        {
            attacksCancelled = true;
            attackQueue.Clear();
            foreach (var a in attacks) DestroyAttack(a);
            attacks.Clear();
            ClearAttackEnemies();
            ReleaseCornerMarker();
        }

        void ReleaseCornerMarker()
        {
            if (cornerSprite != null) Destroy(cornerSprite);
            if (cornerTexture != null) Destroy(cornerTexture);
            cornerSprite = null;
            cornerTexture = null;
        }

        // El reloj de L4 usa tiempo escalado; las pruebas pueden avanzar cuadros exactos.
        internal void TickAttacks(float delta)
        {
            if (attacksCancelled || !built) return;
            if (player != null && player.TryGetComponent<Health>(out var health) && health.IsDead)
            {
                CancelAttacks();
                return;
            }
            if (delta <= 0f || player == null) return;
            attackClock += delta;
            float scale = AttackCadenceScale(cones.Count(c => c.Broken));
            if (!AllConesDestroyed && scale != previousAttackScale)
            {
                for (int i = 0; i < attackRemaining.Length; i++)
                    attackRemaining[i] *= scale / previousAttackScale;
                previousAttackScale = scale;
            }
            if (!AllConesDestroyed && AutomaticAttacks)
            {
                if (attacksStarted)
                    for (int i = 0; i < attackRemaining.Length; i++)
                    {
                        attackRemaining[i] -= delta;
                        if (attackRemaining[i] <= 0f && attackClock >= finalAttacksAt && !attackQueue.Contains((AttackKind)i))
                            attackQueue.Enqueue((AttackKind)i);
                    }
                else TickWallRain();
            }
            if (!AllConesDestroyed && attackQueue.Count > 0 && attackClock >= nextAttackStart)
            {
                var kind = attackQueue.Dequeue();
                BeginAttack(kind);
                attackRemaining[(int)kind] = AttackInterval(kind);
            }
            for (int i = attacks.Count - 1; i >= 0; i--)
            {
                var a = attacks[i];
                if (a.wall != null && a.wall.Fallen) { DestroyAttack(a); attacks.RemoveAt(i); continue; }
                if (AnimateAttack(a)) { DestroyAttack(a); attacks.RemoveAt(i); }
            }
            if (cornerSprite != null && !attacks.Any(a => a.kind == AttackKind.Corners)) ReleaseCornerMarker();
        }

        internal bool QueueAttack(AttackKind kind)
        {
            if (!FinalPhaseReady || AllConesDestroyed || attacksCancelled) return false;
            if (!attackQueue.Contains(kind)) attackQueue.Enqueue(kind);
            return true;
        }

        internal string TestAttackAction(int action)
        {
            if (action == 4)
            {
                AutomaticAttacks = !AutomaticAttacks;
                attackQueue.Clear();
                return $"Cadencia L4: {(AutomaticAttacks ? "activada" : "desactivada")}";
            }
            return QueueAttack((AttackKind)action) ? $"Ataque L4 en cola: {(AttackKind)action}" : "Primero activa la fase final (Ctrl+F1)";
        }

        void BeginAttack(AttackKind kind)
        {
            LastAttackStart = attackClock;
            float warning = kind == AttackKind.Rain ? SporeDelay + SporeFall : kind == AttackKind.Whip ? WhipWarning
                : kind == AttackKind.Spikes ? SpikesWarning : CornerWarning;
            AttackWarningUntil = attackClock + warning;
            nextAttackStart = attackClock + Mathf.Max(AttackGap, warning);
            var target = ClampConePoint(player.position);
            if (kind == AttackKind.Rain) SpawnRain(null);
            else if (kind == AttackKind.Corners) SpawnCornerWarnings();
            else
            {
                bool right = kind == AttackKind.Whip && nextWhipRight;
                if (kind == AttackKind.Whip)
                {
                    target.x = ArenaCenter.x + (right ? 1f : -1f) * (ArenaWidth / 2f - WhipMargin);
                    target.y = Mathf.Clamp(player.position.y, ArenaCenter.y - ArenaHeight / 2f + WhipMargin,
                        ArenaCenter.y + ArenaHeight / 2f - WhipMargin);
                    nextWhipRight = !nextWhipRight;
                }
                var a = NewAttack(kind, target, attackClock);
                a.right = right;
                a.body.flipX = right;
            }
        }

        AttackVisual NewAttack(AttackKind kind, Vector2 point, float start)
        {
            var obj = new GameObject($"L4_{kind}");
            obj.transform.SetParent(scenery, true);
            obj.transform.position = point;
            var a = new AttackVisual { kind = kind, point = point, start = start, body = obj.AddComponent<SpriteRenderer>() };
            a.body.sortingOrder = AttackGroundOrder;
            attacks.Add(a);
            return a;
        }

        void TickWallRain()
        {
            foreach (var wave in wallWaves)
            {
                var wall = wave.wall;
                if (wall.Fallen) continue;
                if (!wallRainAt.ContainsKey(wall)) wallRainAt[wall] = attackClock + WallRainInterval;
                if (attackClock < wallRainAt[wall] || attackClock < nextAttackStart) continue;
                if (wallWaveAt.TryGetValue(wall, out float recent) && attackClock - recent < WaveRainDelay)
                {
                    wallRainAt[wall] = recent + WaveRainDelay;
                    continue;
                }
                SpawnRain(wall);
                LastAttackStart = attackClock;
                AttackWarningUntil = nextAttackStart = attackClock + SporeDelay + SporeFall;
                wallRainAt[wall] = attackClock + WallRainInterval;
            }
        }

        internal void NoteWallWave(LepidodendronWall wall) => wallWaveAt[wall] = attackClock;

        internal Vector2 WallSporePoint(LepidodendronWall wall)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float distance = Random.Range(WallThickness(wall.Index) / 2f + WhipMargin, WallRainReach);
            return entrance + new Vector2(WallX(wall.Index) + side * distance,
                Random.Range(-CorridorHeight / 2f + AttackMargin, CorridorHeight / 2f - AttackMargin));
        }

        void SpawnRain(LepidodendronWall wall)
        {
            int count = wall != null ? Random.Range(3, 5) : FinalSporeCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = wall != null ? WallSporePoint(wall) : i < NearSporeCount
                    ? ClampConePoint((Vector2)player.position + Random.insideUnitCircle * NearSporeRadius)
                    : ArenaCenter + new Vector2(Random.Range(-ArenaWidth / 2f + AttackMargin, ArenaWidth / 2f - AttackMargin),
                        Random.Range(-ArenaHeight / 2f + AttackMargin, ArenaHeight / 2f - AttackMargin));
                AddSpore(point, Random.Range(0f, SporeDelay), wall);
            }
        }

        internal void AddSpore(Vector2 point, float delay, LepidodendronWall wall = null)
        {
            var a = NewAttack(AttackKind.Rain, point, attackClock + delay);
            a.wall = wall;
            var obj = new GameObject("Espora");
            obj.transform.SetParent(a.body.transform, false);
            a.projectile = obj.AddComponent<SpriteRenderer>();
            a.projectile.sortingOrder = CanopyOrder - 1;
            a.projectile.enabled = false;
            a.body.enabled = false;
        }

        void SpawnCornerWarnings()
        {
            if (waveEnemies.Length != 5 || encounterConfig == null) return;
            int[] species = new[] { 0, 2, 3, 4 }.OrderBy(_ => Random.value).Take(2).ToArray();
            int corner = Random.Range(0, 4);
            for (int i = 0; i < Mathf.Min(2, AliveLimit - LivingWaveEnemies); i++)
            {
                int c = (corner + i * 2) % 4;
                var point = ArenaCenter + new Vector2((c % 2 == 0 ? -1f : 1f) * (ArenaWidth / 2f - AttackMargin),
                    (c < 2 ? -1f : 1f) * (ArenaHeight / 2f - AttackMargin));
                var a = NewAttack(AttackKind.Corners, point, attackClock);
                a.prefab = waveEnemies[species[i]];
                a.body.sprite = CornerMarker();
            }
        }

        Sprite CornerMarker()
        {
            if (cornerSprite != null) return cornerSprite;
            // Único gráfico provisional autorizado: brillo morado de aviso, propiedad de este jefe.
            cornerTexture = new Texture2D(MarkerPixels, MarkerPixels, TextureFormat.RGBA32, false);
            var pixels = new Color[MarkerPixels * MarkerPixels];
            for (int y = 0; y < MarkerPixels; y++)
                for (int x = 0; x < MarkerPixels; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * MarkerPixels / 2f) / (MarkerPixels / 2f);
                    pixels[y * MarkerPixels + x] = new Color(0.7f, 0.2f, 1f, Mathf.Clamp01(1f - radius));
                }
            cornerTexture.SetPixels(pixels);
            cornerTexture.Apply();
            cornerSprite = Sprite.Create(cornerTexture, new Rect(0, 0, MarkerPixels, MarkerPixels), Vector2.one / 2f, Ppu);
            return cornerSprite;
        }

        void SpawnCornerEnemy(AttackVisual a)
        {
            if (a.harmless || AllConesDestroyed || LivingWaveEnemies >= AliveLimit) return;
            foreach (var enemy in EncounterPlanner.Spawn(encounterConfig, a.prefab, a.point, wallRoom))
            {
                enemy.DropsBiomass = false;
                GetComponent<Enemy>().RegisterChild(enemy);
                TrackWaveFamily(enemy);
                if (enemy.TryGetComponent<EnemyAI>(out var ai)) { ai.Target = player; ai.Alert(); }
            }
        }

        bool AnimateAttack(AttackVisual a)
        {
            float age = attackClock - a.start;
            if (age < 0f) return false;
            a.body.enabled = true;
            if (a.kind == AttackKind.Corners)
            {
                a.body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(age / CornerWarning));
                if (age < CornerWarning) return false;
                SpawnCornerEnemy(a);
                return true;
            }
            if (a.kind == AttackKind.Rain) return AnimateSpore(a, age);
            bool whip = a.kind == AttackKind.Whip;
            float warning = whip ? WhipWarning : SpikesWarning;
            float step = whip ? WhipFrame : SpikesFrame;
            int frames = whip ? 23 : 8;
            string prefix = whip ? "Lepido_Whip_" : "Lepido_Spikes_";
            if (age < warning) SetAttackFrame(a.body, prefix + "Warn", Mathf.FloorToInt(age / (whip ? 0.1f : 0.13f)));
            else if (age < warning + frames * step)
            {
                int frame = Mathf.FloorToInt((age - warning) / step);
                SetAttackFrame(a.body, prefix + (whip ? "Sweep" : "Erupt"), frame);
                a.body.sortingOrder = player.position.y > a.point.y - (whip ? 6f / Ppu : 0f) ? AttackFrontOrder : AttackBackOrder;
                if (whip && frame >= 3 && frame <= 14 && !a.shook)
                {
                    a.shook = true;
                    ShakeCanopy((15 - frame) * WhipFrame);
                }
                if (whip ? frame >= 2 && frame <= 20 : frame <= 4) DamageAttack(a, frame);
            }
            else
            {
                float fade = whip ? FurrowFade : ScarFade;
                float elapsed = age - warning - frames * step;
                if (elapsed >= fade) return true;
                SetAttackFrame(a.body, prefix + (whip ? "Furrow" : "Scar"), 0);
                a.body.sortingOrder = AttackGroundOrder;
                a.body.color = new Color(1f, 1f, 1f, 1f - elapsed / fade);
            }
            return false;
        }

        bool AnimateSpore(AttackVisual a, float age)
        {
            if (age < SporeFall)
            {
                SetAttackFrame(a.body, "Fx_LepidoSporeMark", Mathf.FloorToInt(age / SporeFall * 6));
                a.projectile.enabled = true;
                SetAttackFrame(a.projectile, "Proj_LepidoSpore", Mathf.FloorToInt(age / 0.1f) % 4);
                float t = age / SporeFall;
                Vector2 position = a.point + Vector2.up * FallingHeight * (1f - t * t);
                if (a.wall != null)
                    position = Vector2.Lerp(new Vector2(a.wall.transform.position.x, a.point.y), a.point, t)
                        + Vector2.up * (ArcHeight * (1f - t) + ArcHeight * Mathf.Sin(t * Mathf.PI));
                a.projectile.transform.position = position;
            }
            else
            {
                a.projectile.enabled = false;
                int frame = Mathf.FloorToInt((age - SporeFall) / SplashFrame);
                if (frame >= 5) return true;
                SetAttackFrame(a.body, "Fx_LepidoSporeSplash", frame);
                if (frame == 0) DamageAttack(a, 0);
            }
            return false;
        }

        void DamageAttack(AttackVisual a, int frame)
        {
            if (a.harmless || AllConesDestroyed) return;
            Collider2D[] hits;
            if (a.kind == AttackKind.Whip)
            {
                float start = 10f / Ppu, end = WhipTip(frame) / Ppu;
                Vector2 center = a.point + Vector2.right * (a.right ? -1f : 1f) * (start + end) / 2f;
                hits = Physics2D.OverlapBoxAll(center, new Vector2(end - start, WhipWidth), 0f, LayerMask.GetMask(CombatLayers.Player));
            }
            else hits = Physics2D.OverlapCircleAll(a.point, a.kind == AttackKind.Rain ? SporeRadius : SpikesRadius, LayerMask.GetMask(CombatLayers.Player));
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Health>();
                if (health != null && a.victims.Add(health)) health.TakeDamage(a.kind == AttackKind.Rain ? SporeDamage : RootDamage);
            }
        }

        void SetAttackFrame(SpriteRenderer renderer, string sheet, int frame)
        {
            var sprites = art.Find(sheet).sprites;
            renderer.sprite = sprites[Mathf.Clamp(frame, 0, sprites.Length - 1)];
        }

        void DestroyAttack(AttackVisual a)
        {
            if (a.body != null) { a.body.gameObject.SetActive(false); Destroy(a.body.gameObject); }
        }

        sealed class AttackVisual
        {
            public AttackKind kind;
            public Vector2 point;
            public float start;
            public bool right, shook, harmless;
            public SpriteRenderer body, projectile;
            public LepidodendronWall wall;
            public Enemy prefab;
            public readonly HashSet<Health> victims = new();
        }
    }
}
