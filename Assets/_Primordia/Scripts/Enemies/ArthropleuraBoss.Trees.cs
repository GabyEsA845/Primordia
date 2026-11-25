using Primordia.Audio;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso C2b-2 (F2): las Sigillarias de la sala abierta. Son grandes y la cabeza las rodea al perseguir; si la
    /// embestida pasa junto a una, la tumba y la cabeza queda aturdida unos segundos. El tronco tumbado es un obstáculo el resto del
    /// combate y cubre al jugador de las ondas.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria). Pies de los árboles, relativos al centro de la sala.
        static readonly Vector2[] TreeSpots =
        {
            new(-9f, 10f), new(9f, -10f), new(-22f, -6f), new(22f, 6f),
        };
        /// <summary>Tamaño de la Sigillaria de la F2 respecto al arte (claro para el jugador: es el blanco de la embestida).</summary>
        const float TreeScale = 1.6f;
        const float TreeHitRadius = 3f, StunSeconds = 3f, FallFrameSeconds = 0.08f;
        // El tronco entero es sólido (no solo la base): ancho y alto en unidades del arte, antes de la escala.
        const float TrunkWidth = 0.8f, TrunkHeight = 2.8f;
        /// <summary>Daño que se hace el jefe al embestir un árbol (no baja del 35 %: ahí empieza la F3).</summary>
        public const int TreeImpactDamage = 30;
        const int FallHitFrame = 6, TreeDamage = 1;
        // Tronco tumbado (u, ya a escala): 3,5 de largo y 1 de ancho en el arte, saliendo del pie hacia el lado de la caída.
        static readonly Vector2 LogSize = new Vector2(3.5f, 1f) * TreeScale;
        const float LogLift = 0.35f * TreeScale, LogDamageMargin = 0.8f;
        // Cómo los esquiva al perseguir: empieza a desviarse a esta distancia (suelo) de un árbol que tiene delante.
        const float TreeAvoidRange = 8f, TreeAvoidPush = 2.6f;

        [System.Serializable]
        public class TreeArt
        {
            public Sprite[] fall = System.Array.Empty<Sprite>();
            public Sprite fallen;
        }

        enum TreeState { Standing, Falling, Fallen }

        sealed class TreeSlot
        {
            public Vector2 foot;
            public SpriteRenderer renderer;
            public BoxCollider2D collider;
            public TreeState state;
            public int direction = 1;
            public float fallStart;
            public bool dealt;
            public Rect Log => new(direction > 0 ? foot.x : foot.x - LogSize.x, foot.y + LogLift - LogSize.y * 0.5f, LogSize.x, LogSize.y);
        }

        [SerializeField] TreeArt treeArt = new();
        readonly List<TreeSlot> trees = new();

        internal void ConfigureTrees(TreeArt art) => treeArt = art;

        public int StandingTrees
        {
            get
            {
                int count = 0;
                foreach (var tree in trees)
                    if (tree.state == TreeState.Standing) count++;
                return count;
            }
        }

        public int FelledTrees => trees.Count - StandingTrees;

        void BuildTrees()
        {
            if (treeArt.fall == null || treeArt.fall.Length == 0)
                return;
            int layer = wallMap != null ? wallMap.gameObject.layer : 0;
            foreach (var spot in TreeSpots)
            {
                var obj = new GameObject("SigillariaF2") { layer = layer };
                obj.transform.SetParent(transform, true);
                obj.transform.position = center + spot;
                obj.transform.localScale = Vector3.one * TreeScale;
                var tree = new TreeSlot { foot = center + spot, renderer = obj.AddComponent<SpriteRenderer>() };
                tree.renderer.sprite = treeArt.fall[0];
                tree.renderer.sortingOrder = 6;
                tree.collider = obj.AddComponent<BoxCollider2D>();
                tree.collider.size = new Vector2(TrunkWidth, TrunkHeight);
                tree.collider.offset = new Vector2(0f, TrunkHeight * 0.5f);
                trees.Add(tree);
            }
        }

        /// <summary>Punto del suelo (en suelo, no en mundo) del centro del tronco de un árbol.</summary>
        Vector2 TreeGround(TreeSlot tree) => WorldToGround(tree.foot + Vector2.up * TrunkHeight * 0.5f * TreeScale);

        /// <summary>
        /// Desvía el objetivo para rodear los árboles en pie que tiene delante: empuja de lado, hacia el lado por el que ya pasa el árbol.
        /// Solo la embestida (recta) los tumba; al perseguir, comer o ir al lodo los esquiva.
        /// </summary>
        Vector2 AvoidTrees(Vector2 target)
        {
            Vector2 toTarget = target - headPos;
            float distance = toTarget.magnitude;
            if (distance < 0.01f)
                return target;
            Vector2 direction = toTarget / distance, push = Vector2.zero;
            foreach (var tree in trees)
            {
                if (tree.state != TreeState.Standing)
                    continue;
                Vector2 offset = TreeGround(tree) - headPos;
                float range = offset.magnitude;
                // Solo los que están delante y a tiro (y no más allá del objetivo).
                if (range > TreeAvoidRange || range < 0.01f || Vector2.Dot(direction, offset / range) < 0.1f || range > distance + 2f)
                    continue;
                float side = Mathf.Sign(direction.x * offset.y - direction.y * offset.x); // + = árbol a la izquierda
                if (side == 0f) side = 1f;
                float strength = 1f - range / TreeAvoidRange;
                push += new Vector2(direction.y, -direction.x) * side * (strength * TreeAvoidPush + 0.4f);
            }
            if (push == Vector2.zero)
                return target;
            return headPos + (direction + push).normalized * Mathf.Max(distance, 4f);
        }

        /// <summary>Distancia de un punto del mundo al tronco (el segmento del pie a la copa).</summary>
        static float DistanceToTrunk(Vector2 point, TreeSlot tree)
        {
            Vector2 a = tree.foot, b = tree.foot + Vector2.up * TrunkHeight * TreeScale;
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(point, a + ab * t);
        }

        /// <summary>El golpe contra el tronco le hace daño al propio jefe (sin bajar del umbral de la F3).</summary>
        void HurtByTree()
        {
            int floor = Mathf.RoundToInt(Health.Max * Thresholds[1]);
            int amount = Mathf.Clamp(Health.Current - floor, 0, TreeImpactDamage);
            if (amount > 0)
                Health.Drain(amount);
        }

        /// <summary>La embestida pasa junto a un árbol en pie: lo tumba y la cabeza queda aturdida.</summary>
        bool TryFellTree(bool fromWheel = false)
        {
            Vector2 head = GroundToWorld(headPos);
            foreach (var tree in trees)
            {
                if (tree.state != TreeState.Standing || DistanceToTrunk(head, tree) > TreeHitRadius)
                    continue;
                tree.direction = tree.foot.x >= head.x ? 1 : -1;
                tree.state = TreeState.Falling;
                Sfx.Play(Sounds.BossArthropleuraTreeFall);
                tree.fallStart = fireClock;
                tree.renderer.flipX = tree.direction < 0;
                if (tree.collider != null) tree.collider.enabled = false;
                ScreenShake.Pulse(3f, 0.3f);
                if (!fromWheel) HurtByTree();
                if (!fromWheel) StartStun();
                return true;
            }
            return false;
        }

        void StartStun()
        {
            HideAim();
            firstCharge = false;
            SetMode(Mode.Stunned);
        }

        void StepStun()
        {
            if (modeTimer >= StunSeconds)
                StartChase();
        }

        void TickTrees()
        {
            foreach (var tree in trees)
            {
                if (tree.state != TreeState.Falling)
                    continue;
                int frame = Mathf.FloorToInt((fireClock - tree.fallStart) / FallFrameSeconds);
                tree.renderer.sprite = treeArt.fall[Mathf.Clamp(frame, 0, treeArt.fall.Length - 1)];
                if (frame >= FallHitFrame && !tree.dealt)
                {
                    tree.dealt = true;
                    OnTreeLanded(tree);
                }
                if (frame >= treeArt.fall.Length)
                {
                    tree.state = TreeState.Fallen;
                    if (treeArt.fallen != null) tree.renderer.sprite = treeArt.fallen;
                }
            }
        }

        /// <summary>Cuadro 6: el tronco golpea el suelo (daña si hay alguien debajo) y queda tumbado como obstáculo.</summary>
        void OnTreeLanded(TreeSlot tree)
        {
            var rect = tree.Log;
            ScreenShake.Pulse(4f, 0.3f);
            if (player != null && playerHealth != null && !playerHealth.IsDead)
            {
                var expanded = new Rect(rect.x - LogDamageMargin, rect.y - LogDamageMargin, rect.width + 2f * LogDamageMargin, rect.height + 2f * LogDamageMargin);
                if (expanded.Contains(player.position))
                    playerHealth.TakeDamage(TreeDamage, DamageKind.Hit, DamageSource.Unknown, player.position);
            }
            // La colisión del tronco va en un objeto aparte, sin la escala del árbol (el tamaño ya está en mundo).
            var log = new GameObject("SigillariaFallenCollider") { layer = tree.renderer.gameObject.layer };
            log.transform.SetParent(transform, true);
            log.transform.position = rect.center;
            var box = log.AddComponent<BoxCollider2D>();
            box.size = rect.size;
        }

        /// <summary>Si un tronco tumbado queda entre dos puntos del mundo (cubre al jugador de las ondas).</summary>
        internal bool LogBlocks(Vector2 from, Vector2 to)
        {
            foreach (var tree in trees)
            {
                if (tree.state == TreeState.Standing || !tree.dealt)
                    continue;
                if (SegmentHitsRect(from, to, tree.Log))
                    return true;
            }
            return false;
        }

        static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
        {
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            for (int axis = 0; axis < 2; axis++)
            {
                float min = axis == 0 ? rect.xMin : rect.yMin, max = axis == 0 ? rect.xMax : rect.yMax;
                float o = a[axis], delta = d[axis];
                if (Mathf.Abs(delta) < 1e-6f)
                {
                    if (o < min || o > max) return false;
                    continue;
                }
                float ta = (min - o) / delta, tb = (max - o) / delta;
                if (ta > tb) (ta, tb) = (tb, ta);
                t0 = Mathf.Max(t0, ta);
                t1 = Mathf.Min(t1, tb);
                if (t0 > t1) return false;
            }
            return true;
        }

        /// <summary>Para pruebas: pie del árbol indicado.</summary>
        internal Vector2 TreeFoot(int index) => trees[index].foot;
    }
}
