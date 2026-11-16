using System;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        public const float AttackInterval = 4f, JumpWarning = 1f, JumpLocked = 0.3f, VulnerableSeconds = 1.5f;
        public const float NetWarning = 1.2f, NetLocked = 0.4f, TrappedSeconds = 1f;
        public const int JumpDamage = 2, SilkDamage = 1, NetDamage = 1, SilkCount = 6;
        // PROVISIONALES: radios, alcance, velocidad y tiempos visuales; alcance/limite aprobados para probar M2.
        public const float JumpRadius = 1.8f, NetRadius = 1.5f, SilkSpeed = 10f, SilkSpread = 30f;
        public const float SilkMinRange = 3f, SilkMaxRange = 14f, SilkRadius = 0.15f;
        public const int WebLimit = 18;
        const float SilkMs = 90f, LandMs = 90f, LandSeconds = 0.27f, WarnMs = 120f, WeaveMs = 110f;
        const float SilkRelease = 4f * SilkMs / 1000f, SilkSeconds = 8f * SilkMs / 1000f;
        const float NetFallSeconds = 0.4f, FxFrameSeconds = 0.08f, PixelsPerUnit = 16f;
        const int GroundFxOrder = 2, AirFxOrder = 32;

        public enum AttackKind { Jump, Silk, Net }
        enum AttackStage { None, JumpWarn, JumpFall, Silk, NetWarn, NetFall, NetTrap }
        [Serializable]
        public struct EffectSheet { public string name; public Sprite[] frames; }
        [SerializeField] EffectSheet[] attackEffects = Array.Empty<EffectSheet>();
        readonly List<SilkBall> silkBalls = new();
        readonly List<TransientFx> transientEffects = new();
        readonly List<MegarachneBossWeb> groundWebs = new();
        AttackStage attack;
        AttackKind nextAttack;
        float attackClock, stageTime;
        Vector2 attackPoint, jumpFrom, jumpAnchor;
        float jumpHeight;
        bool silkReleased;
        SpriteRenderer marker;
        PlayerController trappedPlayer;
        RigidbodyConstraints2D savedConstraints;
        bool savedMovementLock;
        public AttackKind? LastAttack { get; private set; }
        public int AttacksStarted { get; private set; }
        public int ActiveSilkCount => silkBalls.Count;
        public bool IsAttacking => attack != AttackStage.None;
        internal bool AutomaticAttacks { get; set; } = true;
        internal Vector2 AttackPoint => attackPoint;
        internal void ConfigureAttacks(EffectSheet[] sheets) => attackEffects = sheets;

        Sprite[] EffectFrames(string name)
        {
            foreach (var sheet in attackEffects)
                if (sheet.name == name) return sheet.frames;
            return Array.Empty<Sprite>();
        }

        SpriteRenderer CreateFx(string name, Vector2 position, int order = GroundFxOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(scenery != null ? scenery.transform : transform.parent, true);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        void Frame(SpriteRenderer renderer, string name, float age, bool loop = true)
        {
            if (renderer == null) return;
            var frames = EffectFrames(name);
            if (frames.Length == 0) return;
            int index = Mathf.FloorToInt(age / FxFrameSeconds);
            renderer.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }

        void SpawnFx(string name, Vector2 position, int order = GroundFxOrder)
        {
            var renderer = CreateFx(name, position, order);
            Frame(renderer, name, 0f);
            transientEffects.Add(new TransientFx { renderer = renderer, name = name });
        }

        public bool ForceAttack(AttackKind kind)
        {
            if (Health.IsDead || target == null || CurrentMode != Mode.OnCable || travel != Travel.Run || IsAttacking) return false;
            LastAttack = kind;
            AttacksStarted++;
            attackClock = 0f;
            stageTime = 0f;
            attackPoint = target.position;
            facing = (attackPoint - body.position).normalized;
            if (kind == AttackKind.Jump)
            {
                attack = AttackStage.JumpWarn;
                marker = CreateFx("MarcaSalto", attackPoint);
                Play("DropWarn", WarnMs);
            }
            else if (kind == AttackKind.Silk)
            {
                attack = AttackStage.Silk;
                silkReleased = false;
                Play("SilkShoot", SilkMs);
            }
            else
            {
                attack = AttackStage.NetWarn;
                marker = CreateFx("Red", attackPoint);
                Play("Weave", WeaveMs);
            }
            return true;
        }

        void TickAttack(float dt)
        {
            attackClock += dt;
            if (attack == AttackStage.None)
            {
                if (AutomaticAttacks && attackClock >= AttackInterval && ForceAttack(nextAttack))
                    nextAttack = (AttackKind)(((int)nextAttack + 1) % 3);
                return;
            }
            stageTime += dt;
            switch (attack)
            {
                case AttackStage.JumpWarn:
                    if (stageTime < JumpWarning - JumpLocked && target != null) attackPoint = target.position;
                    marker.transform.position = attackPoint;
                    Frame(marker, stageTime < JumpWarning - JumpLocked ? "Fx_MegarachneDrop_Track" : "Fx_MegarachneDrop_Locked", stageTime);
                    if (stageTime >= JumpWarning)
                    {
                        jumpFrom = body.position;
                        jumpHeight = VisualHeight;
                        jumpAnchor = body.position + Vector2.up * (VisualHeight + SpinneretOffset);
                        attack = AttackStage.JumpFall;
                        stageTime = 0f;
                        Play("Drop", DropMs);
                    }
                    break;
                case AttackStage.JumpFall:
                    float t = Mathf.Clamp01(stageTime / DropSeconds);
                    body.position = Vector2.Lerp(jumpFrom, attackPoint, t);
                    SetHeight(Mathf.Lerp(jumpHeight, 0f, t));
                    DrawThread(jumpAnchor);
                    if (t >= 1f)
                    {
                        RemoveMarker();
                        Land();
                        Play("Land", LandMs);
                        SpawnFx("Fx_MegarachneDust", attackPoint);
                        DamageAt(attackPoint, JumpRadius, JumpDamage);
                        attack = AttackStage.None;
                    }
                    break;
                case AttackStage.Silk:
                    if (!silkReleased && stageTime >= SilkRelease) { FireSilk(); silkReleased = true; }
                    if (stageTime >= SilkSeconds) { attack = AttackStage.None; Play("Walk", WalkMs); }
                    break;
                case AttackStage.NetWarn:
                    if (stageTime < NetWarning - NetLocked && target != null) attackPoint = target.position;
                    marker.transform.position = attackPoint;
                    // La hoja aprobada ya incluye el crecimiento de la sombra.
                    Frame(marker, "Fx_MegarachneNet_Shadow", stageTime / NetWarning * 6f * FxFrameSeconds, false);
                    if (stageTime >= NetWarning) { attack = AttackStage.NetFall; stageTime = 0f; }
                    break;
                case AttackStage.NetFall:
                    Frame(marker, "Fx_MegarachneNet_Fall", stageTime, false);
                    if (stageTime >= NetFallSeconds)
                    {
                        TrapAt(attackPoint);
                        attack = AttackStage.NetTrap;
                        stageTime = 0f;
                    }
                    break;
                case AttackStage.NetTrap:
                    if (trappedPlayer != null)
                    {
                        marker.transform.position = trappedPlayer.transform.position;
                        marker.sortingOrder = AirFxOrder;
                        Frame(marker, "Fx_MegarachneNet_Trapped", stageTime);
                    }
                    else Frame(marker, "Fx_MegarachneNet_Idle", stageTime);
                    // Mientras la red esta en el suelo, ella ya vuelve a correr por el cable.
                    TickCable(dt);
                    if (stageTime >= TrappedSeconds)
                    {
                        ReleasePlayer();
                        SpawnFx("Fx_MegarachneNet_Break", marker.transform.position);
                        RemoveMarker();
                        attack = AttackStage.None;
                    }
                    break;
            }
        }

        static void DamageAt(Vector2 point, float radius, int damage)
        {
            var seen = new HashSet<Health>();
            foreach (var collider in Physics2D.OverlapCircleAll(point, radius))
            {
                var player = collider.GetComponentInParent<PlayerController>();
                if (player == null || Vector2.Distance(player.transform.position, point) >= radius) continue;
                var health = player.GetComponent<Health>();
                if (seen.Add(health)) health.TakeDamage(damage);
            }
        }

        void TrapAt(Vector2 point)
        {
            foreach (var collider in Physics2D.OverlapCircleAll(point, NetRadius))
            {
                var player = collider.GetComponentInParent<PlayerController>();
                if (player == null || Vector2.Distance(player.transform.position, point) >= NetRadius) continue;
                var health = player.GetComponent<Health>();
                if (health.IsDead || player.IsDodging || player.IsLunging) continue;
                health.TakeDamage(NetDamage);
                trappedPlayer = player;
                savedMovementLock = player.MovementLocked;
                player.MovementLocked = true;
                player.StopLunge();
                var rb = player.GetComponent<Rigidbody2D>();
                savedConstraints = rb.constraints;
                rb.linearVelocity = Vector2.zero;
                rb.constraints |= RigidbodyConstraints2D.FreezePosition;
                return;
            }
        }

        void ReleasePlayer()
        {
            if (trappedPlayer == null) return;
            trappedPlayer.MovementLocked = savedMovementLock;
            trappedPlayer.GetComponent<Rigidbody2D>().constraints = savedConstraints;
            trappedPlayer = null;
        }

        internal Vector2 SilkOrigin()
        {
            // Proyeccion exacta de spinneret(SilkShoot[4]) del arte aprobado, sin ejecutar su generador.
            const float hingeX = 6f, hingeZ = 9.5f, localX = -38f, localZ = -2f, abdomenAngle = 102f, dz = -4f;
            const float elevation = 55f, directionStep = 22.5f;
            float a = abdomenAngle * Mathf.Deg2Rad;
            float x = hingeX + localX * Mathf.Cos(a) + localZ * Mathf.Sin(a);
            float z = hingeZ - localX * Mathf.Sin(a) + localZ * Mathf.Cos(a) + dz;
            int row = DirectionalArt.RowFor(Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg, out bool flip);
            float yaw = (90f - row * directionStep) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Round(x * Mathf.Cos(yaw)) * (flip ? -1f : 1f),
                Mathf.Round(x * Mathf.Sin(yaw) * Mathf.Sin(elevation * Mathf.Deg2Rad) + z * Mathf.Cos(elevation * Mathf.Deg2Rad))) / PixelsPerUnit;
            return body.position + Vector2.up * VisualHeight + offset;
        }

        void FireSilk()
        {
            Vector2 origin = SilkOrigin();
            Vector2 aim = (target != null ? (Vector2)target.position : attackPoint) - origin;
            float range = Mathf.Clamp(aim.magnitude, SilkMinRange, SilkMaxRange);
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            SpawnFx("Fx_MegarachneSilk_Muzzle", origin, AirFxOrder);
            for (int i = 0; i < SilkCount; i++)
            {
                float radians = (angle - SilkSpread / 2f + i * SilkSpread / (SilkCount - 1)) * Mathf.Deg2Rad;
                var renderer = CreateFx("BolitaSeda", origin, AirFxOrder);
                Frame(renderer, "Proj_MegarachneSilk", 0f);
                silkBalls.Add(new SilkBall { renderer = renderer, position = origin,
                    direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), remaining = range });
            }
        }

        void TickProjectiles(float dt)
        {
            for (int i = transientEffects.Count - 1; i >= 0; i--)
            {
                var fx = transientEffects[i];
                fx.age += dt;
                Frame(fx.renderer, fx.name, fx.age, false);
                if (fx.age >= EffectFrames(fx.name).Length * FxFrameSeconds)
                { Destroy(fx.renderer.gameObject); transientEffects.RemoveAt(i); }
            }
            for (int i = silkBalls.Count - 1; i >= 0; i--)
            {
                var ball = silkBalls[i];
                float step = Mathf.Min(SilkSpeed * dt, ball.remaining);
                bool impact = false;
                Vector2 end = ball.position + ball.direction * step;
                // Barrido por paso: no atraviesa paredes finas; ignora al jefe, su equipo y los triggers de sala.
                foreach (var contact in Physics2D.CircleCastAll(ball.position, SilkRadius, ball.direction, step))
                {
                    var collider = contact.collider;
                    if (collider.isTrigger || collider.GetComponentInParent<Enemy>() != null) continue;
                    var player = collider.GetComponentInParent<PlayerController>();
                    if (player != null)
                    {
                        player.GetComponent<Health>().TakeDamage(SilkDamage);
                        end = player.transform.position;
                    }
                    else
                    {
                        if (collider.attachedRigidbody != null) continue;
                        end = contact.point;
                    }
                    impact = true;
                    break;
                }
                ball.position = end;
                ball.remaining -= step;
                ball.age += dt;
                ball.renderer.transform.position = end;
                Frame(ball.renderer, "Proj_MegarachneSilk", ball.age);
                if (impact || ball.remaining <= Mathf.Epsilon)
                {
                    CreateWeb(end);
                    Destroy(ball.renderer.gameObject);
                    silkBalls.RemoveAt(i);
                }
            }
        }

        internal MegarachneBossWeb CreateWeb(Vector2 point)
        {
            groundWebs.RemoveAll(w => w == null);
            // El limite es por sala, tambi?n si hubiese m?s de una Megarachne.
            var webs = scenery.transform.parent.GetComponentsInChildren<MegarachneBossWeb>();
            if (webs.Length >= WebLimit)
            {
                Array.Sort(webs, (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                for (int i = 0; i <= webs.Length - WebLimit; i++)
                { webs[i].gameObject.SetActive(false); Destroy(webs[i].gameObject); }
            }
            var renderer = CreateFx("TelaranaMegarachne", point, -4);
            var result = renderer.gameObject.AddComponent<MegarachneBossWeb>();
            result.Configure(EffectFrames("Fx_MegarachneWeb_Appear"), EffectFrames("Fx_MegarachneWeb_Idle"), EffectFrames("Fx_MegarachneWeb_Burn"));
            groundWebs.Add(result);
            return result;
        }

        void RemoveMarker()
        {
            if (marker != null) Destroy(marker.gameObject);
            marker = null;
        }

        void ClearAttacks()
        {
            ReleasePlayer();
            RemoveMarker();
            foreach (var ball in silkBalls) if (ball.renderer != null) Destroy(ball.renderer.gameObject);
            foreach (var fx in transientEffects) if (fx.renderer != null) Destroy(fx.renderer.gameObject);
            foreach (var groundWeb in groundWebs)
                if (groundWeb != null) { groundWeb.gameObject.SetActive(false); Destroy(groundWeb.gameObject); }
            silkBalls.Clear();
            transientEffects.Clear();
            groundWebs.Clear();
            attack = AttackStage.None;
        }

        sealed class SilkBall
        {
            public SpriteRenderer renderer;
            public Vector2 position, direction;
            public float remaining, age;
        }
        sealed class TransientFx
        {
            public SpriteRenderer renderer;
            public string name;
            public float age;
        }
    }
}
