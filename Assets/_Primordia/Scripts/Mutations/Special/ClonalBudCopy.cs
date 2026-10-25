using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Una copia de la Yema clonal: brota con su animación, durante unos segundos va sola a por el enemigo más cercano con la
    /// Boca del jugador (se acerca con un ataque cuerpo a cuerpo, guarda distancia con uno a distancia) y una parte de su
    /// daño, y luego se deshace en burbujas. Se ve como el jugador (copia sus sprites cada cuadro) en verde agua y
    /// translúcida. No tiene colisión: no recibe daño ni cae a las fosas, pero no atraviesa paredes. Mientras está, los
    /// enemigos la atacan como al jugador (<see cref="Decoys"/>). Se deshace antes si se limpia la sala.
    /// </summary>
    public class ClonalBudCopy : MonoBehaviour
    {
        internal struct Settings
        {
            public float duration;
            public float damageFraction;
            public Material material;
            public Sprite[] sproutFrames;
            public float appearDelay;
            public Sprite[] dissolveFrames;
            public float frameDuration;
        }

        enum Phase { Sprouting, Active, Gone }

        /// <summary>Tinte si falta el material: verde agua al 60 %.</summary>
        static readonly Color FallbackTint = new(0.49f, 0.91f, 0.8f, 0.6f);
        const int EffectSortingOrder = 22;
        const float Radius = 0.35f;
        const float SearchRadius = 10f;
        const float FollowDistance = 1.5f;

        static readonly List<ClonalBudCopy> active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        PlayerMutations owner;
        Settings settings;
        Weapon weapon;
        Phase phase;
        float appearAt;
        float endAt;
        int enemyMask;
        readonly List<(SpriteRenderer source, SpriteRenderer copy)> renderers = new();

        public bool IsActive => phase == Phase.Active;
        public Weapon Weapon => weapon;
        public Health Target { get; private set; }

        /// <summary>Copias del jugador que siguen en pie (brotando o activas).</summary>
        public static int CountOf(PlayerMutations player)
        {
            int count = 0;
            foreach (var copy in active)
            {
                if (copy != null && copy.owner == player)
                    count++;
            }
            return count;
        }

        internal static IReadOnlyList<ClonalBudCopy> Active => active;

        internal static ClonalBudCopy Sprout(PlayerMutations player, Vector2 position, Settings settings)
        {
            // Si el sitio cae dentro de una pared, brota junto al jugador.
            var grid = Grid;
            if (grid != null && !grid.HasLineOfSight(player.transform.position, position, Radius))
                position = player.transform.position;

            var go = new GameObject("ClonalBud");
            go.transform.position = position;
            var copy = go.AddComponent<ClonalBudCopy>();
            copy.Begin(player, settings);
            return copy;
        }

        // Flota: la rejilla de los nadadores deja pasar las fosas pero no las paredes.
        static NavigationGrid Grid => NavigationGrid.Swimming ?? NavigationGrid.Current;

        void Begin(PlayerMutations player, Settings newSettings)
        {
            owner = player;
            settings = newSettings;
            enemyMask = LayerMask.GetMask(CombatLayers.Enemy);
            weapon = gameObject.AddComponent<Weapon>();
            // Solo el daño de la Boca: sin los multiplicadores ni los avisos de golpe de las otras mutaciones del jugador.
            weapon.DamageMultiplier = settings.damageFraction;

            phase = Phase.Sprouting;
            appearAt = Time.time + settings.appearDelay;
            endAt = appearAt + settings.duration;
            active.Add(this);
            Room.ClearedAny += OnRoomCleared;
            FrameEffect.Play("ClonalBudSprout", settings.sproutFrames, settings.frameDuration, EffectSortingOrder, null, transform.position);
            if (settings.appearDelay <= 0f)
                Appear();
        }

        void OnDestroy()
        {
            active.Remove(this);
            Decoys.Remove(transform);
            Room.ClearedAny -= OnRoomCleared;
        }

        void OnRoomCleared(Room _) => Dissolve();

        void Appear()
        {
            phase = Phase.Active;
            Decoys.Add(transform);
            BuildRenderers();
        }

        /// <summary>Se deshace en burbujas y desaparece (al acabar su tiempo, al limpiar la sala o si el jugador muere).</summary>
        public void Dissolve()
        {
            if (phase == Phase.Gone)
                return;
            phase = Phase.Gone;
            Decoys.Remove(transform);
            active.Remove(this);
            FrameEffect.Play("ClonalBudDissolve", settings.dissolveFrames, settings.frameDuration, EffectSortingOrder, null, transform.position);
            Destroy(gameObject);
        }

        void Update()
        {
            if (owner == null || owner.Health == null || owner.Health.IsDead)
            {
                Dissolve();
                return;
            }

            if (phase == Phase.Sprouting)
            {
                if (Time.time < appearAt)
                    return;
                Appear();
            }

            if (phase != Phase.Active)
                return;

            if (Time.time >= endAt)
            {
                Dissolve();
                return;
            }

            Think();
        }

        void LateUpdate()
        {
            if (phase == Phase.Active)
                CopySprites();
        }

        void Think()
        {
            var attack = owner.Weapon != null ? owner.Weapon.Attack : null;
            weapon.Attack = attack;

            if (Target == null || Target.IsDead || ((Vector2)Target.transform.position - (Vector2)transform.position).sqrMagnitude > SearchRadius * SearchRadius)
                Target = Nearest();

            float speed = owner.Controller != null ? owner.Controller.MoveSpeed : 5f;
            if (Target == null)
            {
                // Sin enemigos, se queda cerca del jugador.
                Vector2 toPlayer = (Vector2)owner.transform.position - (Vector2)transform.position;
                if (toPlayer.magnitude > FollowDistance)
                    Move(DirectionTo(owner.transform.position), speed);
                return;
            }

            Vector2 targetPosition = Target.transform.position;
            Vector2 toTarget = targetPosition - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.right;

            if (attack != null && attack.IsUsable)
            {
                // Cuerpo a cuerpo se pega; a distancia (o en área) se queda a media distancia de su alcance.
                float wanted = attack.IsMelee ? attack.Range * 0.6f : Mathf.Clamp(attack.Range * 0.6f, 2.5f, 5f);
                if (distance > wanted + 0.3f)
                    Move(DirectionTo(targetPosition), speed);
                else if (!attack.IsMelee && distance < wanted - 0.8f)
                    Move(-direction, speed * 0.8f);

                bool inReach = distance <= attack.Range + (attack.IsMelee ? 0.3f : 0f);
                var grid = Grid;
                if (inReach && (grid == null || grid.HasLineOfSight(transform.position, targetPosition, 0.1f)))
                    weapon.TryFire(direction);
            }
            else
            {
                Move(DirectionTo(targetPosition), speed);
            }
        }

        Vector2 DirectionTo(Vector2 point)
        {
            var grid = Grid;
            Vector2 straight = point - (Vector2)transform.position;
            if (grid != null)
            {
                var path = grid.DirectionTo(transform.position, point, Radius);
                if (path.sqrMagnitude > 0.0001f)
                    return path.normalized;
            }
            return straight.sqrMagnitude > 0.0001f ? straight.normalized : Vector2.zero;
        }

        /// <summary>Avanza sin meterse en las paredes; si choca de frente, prueba deslizándose por un eje.</summary>
        void Move(Vector2 direction, float speed)
        {
            Vector2 from = transform.position;
            Vector2 step = direction * speed * Time.deltaTime;
            var grid = Grid;
            foreach (var attempt in new[] { step, new Vector2(step.x, 0f), new Vector2(0f, step.y) })
            {
                if (attempt.sqrMagnitude < 0.000001f)
                    continue;
                if (grid == null || grid.HasLineOfSight(from, from + attempt, Radius))
                {
                    transform.position = from + attempt;
                    return;
                }
            }
        }

        Health Nearest()
        {
            Health best = null;
            float bestDistance = float.MaxValue;
            Vector2 from = transform.position;
            foreach (var hit in Physics2D.OverlapCircleAll(from, SearchRadius, enemyMask))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                float distance = ((Vector2)hit.transform.position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = health;
                }
            }
            return best;
        }

        // ---------- Aspecto ----------

        /// <summary>Un sprite por cada uno de los que se ven en el jugador: el cuerpo y las piezas de las mutaciones.</summary>
        void BuildRenderers()
        {
            var sources = new List<SpriteRenderer>();
            var animator = owner.GetComponent<PlayerAnimator>();
            if (animator != null && animator.Body != null)
                sources.Add(animator.Body);
            var look = owner.GetComponent<MutationLook>();
            if (look != null)
                sources.AddRange(look.PieceRenderers);
            if (sources.Count == 0)
                sources.AddRange(owner.GetComponentsInChildren<SpriteRenderer>());

            foreach (var source in sources)
            {
                var child = new GameObject(source.name);
                child.transform.SetParent(transform, false);
                var copy = child.AddComponent<SpriteRenderer>();
                if (settings.material != null)
                    copy.sharedMaterial = settings.material;
                else
                    copy.color = FallbackTint;
                renderers.Add((source, copy));
            }
            CopySprites();
        }

        /// <summary>Mismo cuadro, posición relativa, giro y orden que en el jugador.</summary>
        void CopySprites()
        {
            var origin = owner.transform;
            foreach (var (source, copy) in renderers)
            {
                if (source == null)
                {
                    copy.enabled = false;
                    continue;
                }
                copy.enabled = source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
                copy.sprite = source.sprite;
                copy.flipX = source.flipX;
                copy.flipY = source.flipY;
                copy.sortingLayerID = source.sortingLayerID;
                copy.sortingOrder = source.sortingOrder;
                var t = copy.transform;
                t.position = transform.position + (source.transform.position - origin.position);
                t.rotation = source.transform.rotation;
                t.localScale = source.transform.lossyScale;
            }
        }
    }
}
