using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Peonza lanzada del nivel III de la Peonza (Bumerán): sale hacia el cursor, da la vuelta al llegar a su alcance o chocar con una
    /// pared y vuelve al lanzador. Daña y aturde a cada enemigo una vez a la ida y otra a la vuelta. Se dibuja como un clon translúcido
    /// (docs/multimedia-temp/floor3-top-clone): aparece desde el jugador, gira con dos ecos detrás y un remolino propio, y al recogerlo
    /// se funde en el jugador. Todo el dibujo es opcional.
    /// </summary>
    public class SpinBoomerang : MonoBehaviour
    {
        const float Speed = 14f;
        const float HitRadius = 0.7f;
        const float WallRadius = 0.3f;
        const float CatchDistance = 0.5f;
        // Por si el lanzador desaparece o se queda atascado.
        const float MaxSeconds = 4f;
        const float SpinFrameSeconds = 0.04f;
        const float AppearFrameSeconds = 0.05f;
        // El anillo del clon va a 40 ms por cuadro (apertura de 4, bucle de 8 con un destello, final de 3); el daño empieza en el cuadro 2.
        const float RingFrameSeconds = 0.04f;
        const int RingDamageFrame = 2;
        const int RingOrder = 18;
        // Los ecos van en las dos posiciones anteriores: el primero al 100 % y el segundo al 55 %.
        static readonly float[] EchoAlpha = { 1f, 0.55f };

        [Header("Dibujo")]
        [SerializeField] SpriteRenderer body;
        [Tooltip("Los dos ecos, detrás del clon (el primero es el más cercano).")]
        [SerializeField] SpriteRenderer[] echoRenderers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] SpriteRenderer whirlRenderer;
        [SerializeField] Sprite[] spinFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] echoFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] whirlFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] appearFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] mergeFrames = System.Array.Empty<Sprite>();
        [Tooltip("Anillo de alcance del clon (docs/multimedia-temp/floor3-spin-ring): sale con el clon, da vueltas y se deshace al fundirse.")]
        [SerializeField] SpriteRenderer ringRenderer;
        [SerializeField] Sprite[] ringAppear = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] ringLoop = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] ringEnd = System.Array.Empty<Sprite>();

        Transform owner;
        Weapon weapon;
        Vector2 direction;
        float range;
        int damage;
        float stun;
        int ringDamage;
        float ringTick;
        float ringRadius;
        float nextRingStrike;
        float traveled;
        float age;
        bool returning;
        readonly HashSet<Health> struck = new();
        // Dónde estaba el clon en los últimos cuadros del giro (el último es el actual), para poner los ecos.
        readonly List<Vector2> trail = new();
        int lastTick = -1;

        public bool IsReturning => returning;

        internal void ConfigureArt(SpriteRenderer newBody, SpriteRenderer[] newEchoes, SpriteRenderer newWhirl, Sprite[] spin, Sprite[] echo,
            Sprite[] whirl, Sprite[] appear, Sprite[] merge)
        {
            body = newBody;
            echoRenderers = newEchoes;
            whirlRenderer = newWhirl;
            spinFrames = spin;
            echoFrames = echo;
            whirlFrames = whirl;
            appearFrames = appear;
            mergeFrames = merge;
        }

        internal void ConfigureRing(SpriteRenderer newRenderer, Sprite[] appear, Sprite[] loop, Sprite[] end)
        {
            ringRenderer = newRenderer;
            ringAppear = appear;
            ringLoop = loop;
            ringEnd = end;
        }

        /// <summary>
        /// Lanza el clon. Con <paramref name="ringDamageAmount"/> &gt; 0 lleva además el anillo de alcance: daña lo que roza en
        /// <paramref name="ringReach"/> con el daño y el intervalo del giro, a la par que su golpe fuerte (que no cambia).
        /// </summary>
        public void Launch(Transform thrower, Vector2 aim, float maxRange, int hitDamage, float stunSeconds, Weapon source,
            int ringDamageAmount = 0, float ringInterval = 0.25f, float ringReach = 2f)
        {
            ringDamage = ringDamageAmount;
            ringTick = Mathf.Max(0.05f, ringInterval);
            ringRadius = ringReach;
            nextRingStrike = Time.time + RingDamageFrame * RingFrameSeconds;
            owner = thrower;
            weapon = source;
            direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            range = maxRange;
            damage = hitDamage;
            stun = stunSeconds;
        }

        void Update()
        {
            float step = Speed * Time.deltaTime * TimeWarp.PlayerSpeed;
            age += Time.deltaTime;
            Vector2 position = transform.position;

            if (!returning)
            {
                position += direction * step;
                traveled += step;
                if (traveled >= range || HitsWall(position))
                    Turn();
            }
            else
            {
                if (owner == null || age > MaxSeconds)
                {
                    Destroy(gameObject);
                    return;
                }
                Vector2 toOwner = (Vector2)owner.position - position;
                if (toOwner.magnitude <= step + CatchDistance)
                {
                    MergeIntoOwner();
                    Destroy(gameObject);
                    return;
                }
                position += toOwner.normalized * step;
            }

            transform.position = position;
            Animate(position);
            StrikeNearby(position);
            RingStrike(position);
        }

        // Aparece como la silueta clara del jugador y pasa a girar; los ecos y el remolino acompañan al clon.
        void Animate(Vector2 position)
        {
            int tick = Mathf.FloorToInt(age / SpinFrameSeconds);
            if (tick != lastTick)
            {
                lastTick = tick;
                trail.Add(position);
                if (trail.Count > 3)
                    trail.RemoveAt(0);
            }

            AnimateRing();
            bool appearing = appearFrames.Length > 0 && age < appearFrames.Length * AppearFrameSeconds;
            if (body != null)
            {
                if (appearing)
                    body.sprite = appearFrames[Mathf.Min(Mathf.FloorToInt(age / AppearFrameSeconds), appearFrames.Length - 1)];
                else if (spinFrames.Length > 0)
                    body.sprite = spinFrames[tick % spinFrames.Length];
            }

            if (whirlRenderer != null && whirlFrames.Length > 0)
            {
                whirlRenderer.enabled = !appearing;
                whirlRenderer.sprite = whirlFrames[tick % whirlFrames.Length];
            }

            for (int i = 0; i < echoRenderers.Length && i < EchoAlpha.Length; i++)
            {
                var echo = echoRenderers[i];
                if (echo == null)
                    continue;
                int back = i + 1;
                echo.enabled = !appearing && echoFrames.Length > 0 && trail.Count > back;
                if (!echo.enabled)
                    continue;
                echo.transform.position = trail[trail.Count - 1 - back];
                echo.sprite = echoFrames[((tick - back) % echoFrames.Length + echoFrames.Length) % echoFrames.Length];
                var color = echo.color;
                color.a = EchoAlpha[i];
                echo.color = color;
            }
        }

        // El anillo daña a cada enemigo de su radio cada intervalo, con el daño del giro (aparte del golpe fuerte de una vez por tramo).
        void RingStrike(Vector2 position)
        {
            if (ringDamage <= 0 || Time.time < nextRingStrike)
                return;
            nextRingStrike = Time.time + ringTick;
            foreach (var hit in Physics2D.OverlapCircleAll(position, ringRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead)
                    continue;
                if (target.TakeDamage(ringDamage, DamageKind.Hit, DamageSource.Spin, hit.ClosestPoint(position)) && weapon != null)
                    weapon.ReportHit(target);
            }
        }

        // Anillo de alcance: la apertura y luego el bucle, centrado en el clon y debajo de los ecos.
        void AnimateRing()
        {
            if (ringRenderer == null || ringLoop.Length == 0)
                return;
            ringRenderer.enabled = true;
            int frame = Mathf.FloorToInt(age / RingFrameSeconds);
            ringRenderer.sprite = frame < ringAppear.Length ? ringAppear[frame] : ringLoop[(frame - ringAppear.Length) % ringLoop.Length];
        }

        // Al recogerlo, un destello se recoge en el jugador (el clon desaparece en el cuadro 0) y el anillo se deshace.
        void MergeIntoOwner()
        {
            if (owner == null)
                return;
            if (ringEnd.Length > 0 && ringRenderer != null)
            {
                var ring = new GameObject("SpinCloneRingEnd");
                ring.transform.SetParent(owner, false);
                ring.AddComponent<MergeFlash>().Play(ringEnd, RingFrameSeconds, RingOrder);
            }
            if (mergeFrames.Length == 0)
                return;
            var flash = new GameObject("SpinCloneMerge");
            flash.transform.SetParent(owner, false);
            flash.AddComponent<MergeFlash>().Play(mergeFrames, AppearFrameSeconds);
        }

        // Pasa los cuadros del destello una vez, sobre el jugador, y se quita.
        sealed class MergeFlash : MonoBehaviour
        {
            Sprite[] frames;
            float frameSeconds;
            float elapsed;
            SpriteRenderer render;

            public void Play(Sprite[] newFrames, float newFrameSeconds, int sortingOrder = 22)
            {
                frames = newFrames;
                frameSeconds = newFrameSeconds;
                render = gameObject.AddComponent<SpriteRenderer>();
                render.sortingOrder = sortingOrder;
                render.sprite = frames[0];
            }

            void Update()
            {
                elapsed += Time.deltaTime;
                int frame = Mathf.FloorToInt(elapsed / frameSeconds);
                if (frame >= frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                render.sprite = frames[frame];
            }
        }

        void Turn()
        {
            returning = true;
            struck.Clear();
        }

        static bool HitsWall(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position, WallRadius))
            {
                if (!hit.isTrigger && hit.GetComponentInParent<Health>() == null && hit.GetComponentInParent<Projectile>() == null)
                    return true;
            }
            return false;
        }

        void StrikeNearby(Vector2 position)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(position, HitRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !struck.Add(target))
                    continue;

                if (target.TakeDamage(damage, DamageKind.Hit, DamageSource.Spin, hit.ClosestPoint(position)) && weapon != null)
                    weapon.ReportHit(target);
                if (!target.IsDead && stun > 0f && hit.GetComponentInParent<IPushable>() is { } pushable)
                    pushable.Knockback(Vector2.zero, stun);
            }
        }
    }
}
