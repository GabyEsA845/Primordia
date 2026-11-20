using System.Collections.Generic;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class MegarachneBoss
    {
        // Provisionales.
        public const int BroodLimit = 8;
        const int BroodPerSac = 4, CrowdedBroodPerSac = 3, CrowdedBroodThreshold = 6;
        const float SacMinRange = 3f, SacMaxRange = 8f, SacWeaveSeconds = 8f * WeaveMs / 1000f;
        const float SacSearchStep = 0.5f, SacBelowCable = 0.5f, BroodFanAngle = 120f;
        const int SacSearchDirections = 32;

        readonly List<MegarachneBrood> brood = new();
        Vector2 arenaSize, sacAnchor, sacPoint;
        internal MegarachneBossSac ActiveSac { get; private set; }

        bool WeaveSac()
        {
            if (ActiveSac != null || !FindSacPoint(out sacPoint)) return false;
            sacAnchor = body.position + Vector2.up * VisualHeight;
            facing = (sacPoint - sacAnchor).normalized;
            attack = AttackStage.SacWeave;
            Play("Weave", WeaveMs);
            return true;
        }

        bool FindSacPoint(out Vector2 point)
        {
            Vector2 player = target.position;
            Vector2 desired = body.position + Vector2.up * (VisualHeight - SacBelowCable);
            Vector2 size = MegarachneBossSac.Size;
            // La distancia se mide al cuerpo del saco, debajo de su pivote.
            Vector2 offset = Vector2.down * (size.y * 0.5f);
            point = default;
            float best = float.PositiveInfinity;
            for (float radius = SacMinRange; radius <= SacMaxRange; radius += SacSearchStep)
            {
                for (int i = 0; i < SacSearchDirections; i++)
                {
                    float angle = i * 2f * Mathf.PI / SacSearchDirections;
                    Vector2 center = player + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 pivot = center - offset;
                    float score = (pivot - desired).sqrMagnitude;
                    if (score >= best || pivot.y > desired.y || !InsideArena(center, size * 0.5f)) continue;
                    if (System.Array.Exists(Physics2D.OverlapBoxAll(center, size, 0f), IsScenerySolid)) continue;
                    point = pivot;
                    best = score;
                }
            }
            return best < float.PositiveInfinity;
        }

        bool InsideArena(Vector2 point, Vector2 margin)
        {
            Vector2 relative = point - ArenaCenter;
            return Mathf.Abs(relative.x) + margin.x <= arenaSize.x * 0.5f
                && Mathf.Abs(relative.y) + margin.y <= arenaSize.y * 0.5f;
        }

        void TickSacWeave()
        {
            DrawThread(Vector2.Lerp(sacAnchor, sacPoint, Mathf.Clamp01(stageTime / SacWeaveSeconds)), sacAnchor);
            if (stageTime < SacWeaveSeconds) return;
            var renderer = CreateFx("SacoMegarachne", sacPoint, AirFxOrder);
            var hanging = MakeLine("HiloSaco", ThreadWidth, ThreadOrder, threadMaterial ?? cableMaterial);
            hanging.transform.SetParent(renderer.transform, true);
            hanging.SetPosition(0, sacAnchor);
            hanging.SetPosition(1, sacPoint);
            renderer.gameObject.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            renderer.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            ActiveSac = renderer.gameObject.AddComponent<MegarachneBossSac>();
            ActiveSac.Configure(this);
            thread.enabled = false;
            attack = AttackStage.None;
            Play("Walk", WalkMs);
        }

        internal void ReleaseBrood(Vector2 center)
        {
            brood.RemoveAll(child => child == null);
            int living = brood.FindAll(child => !child.Health.IsDead).Count;
            int count = Mathf.Min(BroodLimit - living, living >= CrowdedBroodThreshold ? CrowdedBroodPerSac : BroodPerSac);
            Vector2 forward = target != null ? ((Vector2)target.position - center).normalized : Vector2.down;
            if (forward == Vector2.zero) forward = Vector2.down;
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-BroodFanAngle * 0.5f, BroodFanAngle * 0.5f, i / (float)(count - 1));
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * forward;
                float spread = MegarachneBrood.Radius * count;
                Vector2 margin = Vector2.one * MegarachneBrood.Radius;
                Vector2 point = center + direction * spread;
                // Buscar hacia el saco si el extremo del abanico cae sobre un obstáculo.
                while (spread > 0f && (!InsideArena(point, margin)
                    || SolidAt(center, direction, spread, MegarachneBrood.Radius)))
                {
                    spread = Mathf.Max(0f, spread - MegarachneBrood.Radius);
                    point = center + direction * spread;
                }
                var renderer = CreateFx("CriaMegarachne", point, 5);
                renderer.gameObject.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
                var child = renderer.gameObject.AddComponent<MegarachneBrood>();
                child.Configure(this);
                brood.Add(child);
            }
        }

        public void KillAllBrood()
        {
            foreach (var child in brood)
                if (child != null) child.Remove();
            brood.Clear();
        }

        static bool IsScenerySolid(Collider2D collider) =>
            !collider.isTrigger && collider.gameObject.layer == LayerMask.NameToLayer("Default")
            && (collider.attachedRigidbody == null || collider.attachedRigidbody.bodyType == RigidbodyType2D.Static);

        internal static bool SolidAt(Vector2 origin, Vector2 direction, float distance, float radius = 0f)
        {
            var contacts = radius > 0f ? Physics2D.CircleCastAll(origin, radius, direction, distance)
                : Physics2D.RaycastAll(origin, direction, distance);
            return System.Array.Exists(contacts, contact => IsScenerySolid(contact.collider));
        }
    }
}
