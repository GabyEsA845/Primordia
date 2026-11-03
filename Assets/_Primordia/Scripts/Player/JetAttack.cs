using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// El Chorro de vinagre del vinagrillo (<see cref="AttackData.IsJet"/>): mientras se mantiene el botón y queda carga, sale un chorro
    /// desde la punta de la cola alzada que sigue al cursor girando a una velocidad limitada, y daña a cada golpe a lo que toca (se corta
    /// en el primer enemigo o pared; en el nivel II atraviesa a los enemigos). El chorro crece desde la cola hasta su alcance y, al soltar,
    /// se recoge hacia el final. La barra de carga se gasta mientras sale y se rellena al soltar; si se vacía del todo no se puede usar
    /// hasta que se llene. En el nivel III, la salpicadura daña alrededor del final del chorro. Esquivar lo corta.
    /// <see cref="PlayerAttack"/> lo maneja (lo añade solo).
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Weapon), typeof(PlayerAnimator))]
    public class JetAttack : MonoBehaviour
    {
        public enum Phase { Idle, Starting, Firing, Ending }

        // Grosor del chorro para dar a lo que toca (mitad del ancho dibujado, 8 px), y duraciones de reserva sin animación.
        const float JetRadius = 0.25f;
        const float DefaultStartingSeconds = 0.21f;
        const float DefaultEndingSeconds = 0.16f;
        // Cuánto crece el chorro hacia su alcance y cuánto se recoge al soltar, en unidades por segundo.
        const float ExtendSpeed = 30f;
        const float RetractSpeed = 40f;
        // Los tramos del dibujo miden 1 u (16 px) y la salpicadura ya está dibujada al radio del juego: nada se escala.
        const int SegmentPixels = 16;
        // Punta del abdomen en píxeles desde el centro del lienzo de 40×40 (x a la derecha, y hacia arriba), según el README del cuerpo.
        // Con el cuerpo volteado a la izquierda se invierte la x.
        static readonly Vector2 SideTip = new(1f, 9f);
        static readonly Vector2 DownTip = new(-1f, 5f);
        static readonly Vector2 UpTip = new(-1f, 9f);
        const float PixelsPerUnit = 16f;

        PlayerController controller;
        PlayerAnimator animator;
        Weapon weapon;
        Health health;
        AttackData attack;
        float charge;
        bool initialized;
        bool locked;
        float jetAngle;
        float phaseTimer;
        float tickTimer;
        float splashTimer;
        string loopClip;
        // Hasta dónde llega el chorro ahora (crece hacia el alcance) y desde dónde se ve (sube al recogerse).
        float reach;
        float tailDistance;
        bool retracting;
        // El último tramo es la punta (el chorro llegó a su alcance sin chocar) y qué salpicadura toca.
        bool freeTip;
        int splashVariant;
        Vector2 lastDirection = Vector2.right;
        readonly HashSet<Health> struck = new();
        readonly List<SpriteRenderer> segments = new();
        // Los tramos recortados a lo que llega el chorro, para no crear el mismo sprite cada cuadro.
        static readonly Dictionary<(Sprite, int, int), Sprite> croppedSprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => croppedSprites.Clear();

        readonly List<(Health health, float distance)> targets = new();
        GameObject hitEffect;
        GameObject muzzleEffect;
        JetBar bar;

        public Phase State { get; private set; } = Phase.Idle;

        /// <summary>True mientras sale el chorro (o se prepara o se baja la cola).</summary>
        public bool IsActive => State != Phase.Idle;

        /// <summary>Carga que queda, de 0 a 1.</summary>
        public float Charge01 => attack != null ? Mathf.Clamp01(charge / attack.JetMaxSeconds) : 1f;

        /// <summary>True si se vació del todo y no se puede usar hasta que se llene.</summary>
        public bool IsLocked => locked;

        /// <summary>Hacia dónde sale ahora el chorro (grados), que persigue al cursor.</summary>
        public float JetAngle => jetAngle;

        /// <summary>Hasta dónde llega el chorro ahora, en unidades desde la cola (crece hasta el alcance o lo que lo corte).</summary>
        public float Reach => reach;

        /// <summary>Dónde empieza el chorro en este momento (la punta de la cola).</summary>
        public Vector2 Origin => (Vector2)transform.position + TipOffset();

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponent<PlayerAnimator>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
        }

        void OnDisable() => Cancel();

        void OnDestroy()
        {
            if (bar != null)
                Destroy(bar.gameObject);
        }

        // Al soltar, el chorro se recoge de la cola hacia el final; esto sigue aunque el ataque ya no esté en marcha.
        void Update()
        {
            if (!retracting)
                return;

            tailDistance += RetractSpeed * Time.deltaTime * TimeWarp.PlayerSpeed;
            if (tailDistance >= reach)
            {
                ClearDrawing();
                return;
            }
            DrawSegments(Origin, lastDirection, tailDistance, reach, freeTip);
        }

        /// <summary>Se llama cada fotograma mientras el ataque equipado es un chorro. <paramref name="held"/>: el botón está pulsado.</summary>
        public void Tick(bool held)
        {
            var equipped = weapon.Attack;
            if (equipped == null || !equipped.IsJet)
                return;
            attack = equipped;
            float dt = Time.deltaTime * TimeWarp.PlayerSpeed;
            float max = attack.JetMaxSeconds;
            if (!initialized)
            {
                charge = max;
                initialized = true;
            }
            charge = Mathf.Min(charge, max);
            EnsureBar();

            bool canAct = !health.IsDead && controller.CanAct && !controller.IsDodging;
            if (!canAct && State != Phase.Idle)
                Cancel();

            bool firing = State == Phase.Firing;
            if (!firing)
                Refill(dt, max);

            if (!canAct)
                return;

            switch (State)
            {
                case Phase.Idle:
                    if (held && !locked && charge > 0f)
                        Begin();
                    break;

                case Phase.Starting:
                    phaseTimer -= dt;
                    if (!held)
                        Stop();
                    else if (phaseTimer <= 0f)
                        StartFiring();
                    break;

                case Phase.Firing:
                    Fire(held, dt);
                    break;

                case Phase.Ending:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                        Rest();
                    break;
            }
        }

        // Se rellena sola al soltar: llena del todo en la mitad de tiempo normal; si se vació, más despacio hasta llenarse entera.
        void Refill(float dt, float max)
        {
            float seconds = locked ? attack.JetEmptyRefillSeconds : attack.JetRefillSeconds;
            charge = Mathf.Min(max, charge + max / seconds * dt);
            if (charge >= max)
                locked = false;
        }

        void Begin()
        {
            // Un chorro nuevo no hereda lo que quedaba del anterior recogiéndose.
            ClearDrawing();
            jetAngle = Mathf.Atan2(controller.AimDirection.y, controller.AimDirection.x) * Mathf.Rad2Deg;
            string clip = $"AttackStart_{FacingName()}";
            bool hasStart = animator.Hold(clip);
            phaseTimer = hasStart ? animator.ClipLength(clip, DefaultStartingSeconds) : 0f;
            loopClip = null;
            State = Phase.Starting;
            if (!hasStart)
                StartFiring();
        }

        void StartFiring()
        {
            State = Phase.Firing;
            tickTimer = 0f;
            splashTimer = 0f;
            reach = 0f;
            tailDistance = 0f;
            struck.Clear();
            PlayLoop();
        }

        void Fire(bool held, float dt)
        {
            charge -= dt;
            PlayLoop();

            // El chorro persigue al cursor a una velocidad de giro limitada.
            float wanted = Mathf.Atan2(controller.AimDirection.y, controller.AimDirection.x) * Mathf.Rad2Deg;
            jetAngle = Mathf.MoveTowardsAngle(jetAngle, wanted, attack.JetTurnRate * dt);
            Vector2 direction = new(Mathf.Cos(jetAngle * Mathf.Deg2Rad), Mathf.Sin(jetAngle * Mathf.Deg2Rad));
            Vector2 origin = Origin;
            lastDirection = direction;

            // Crece desde la cola hasta el alcance (o hasta lo que lo corte); si algo se le pone delante, se acorta al instante.
            float length = Trace(origin, direction);
            reach = Mathf.Min(reach, length);
            reach = Mathf.MoveTowards(reach, length, ExtendSpeed * dt);
            tailDistance = 0f;
            Vector2 end = origin + direction * reach;
            bool blocked = length < attack.JetRange - 0.01f && reach >= length - 0.01f;
            freeTip = length >= attack.JetRange - 0.01f && reach >= length - 0.01f;
            DrawSegments(origin, direction, 0f, reach, freeTip);
            DrawEffects(origin, end, blocked, direction);

            tickTimer -= dt;
            if (tickTimer <= 0f)
            {
                tickTimer += attack.JetTick;
                Strike();
            }

            if (attack.JetSplashRadius > 0f && reach > 0.5f)
            {
                splashTimer -= dt;
                if (splashTimer <= 0f)
                {
                    splashTimer += attack.JetSplashTick;
                    Splash(end);
                }
            }

            if (charge <= 0f)
            {
                charge = 0f;
                locked = true;
                Stop();
            }
            else if (!held)
            {
                Stop();
            }
        }

        /// <summary>
        /// Mira hasta dónde llega el chorro desde el origen y deja en <see cref="targets"/> los enemigos que toca, con su distancia. Se corta
        /// en la primera pared u obstáculo y, sin atravesar, en el primer enemigo.
        /// </summary>
        float Trace(Vector2 origin, Vector2 direction)
        {
            float range = attack.JetRange;
            targets.Clear();
            var hits = Physics2D.CircleCastAll(origin, JetRadius, direction, range);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            int enemyLayer = LayerMask.NameToLayer(CombatLayers.Enemy);
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider == null || collider.isTrigger)
                    continue;
                if (collider.GetComponentInParent<PlayerController>() != null || collider.GetComponentInParent<Projectile>() != null)
                    continue;

                var target = collider.GetComponentInParent<Health>();
                if (target == null || collider.gameObject.layer != enemyLayer)
                    return Mathf.Max(0f, hit.distance);
                if (target.IsDead || targets.Exists(t => t.health == target))
                    continue;

                targets.Add((target, hit.distance));
                if (!attack.JetPierces)
                    return Mathf.Max(0f, hit.distance);
            }
            return range;
        }

        // Solo da a lo que el chorro ya ha alcanzado al crecer.
        void Strike()
        {
            int damage = Mathf.Max(0, Mathf.CeilToInt(attack.Damage * weapon.DamageMultiplier - 0.001f));
            foreach (var (target, distance) in targets)
            {
                if (target == null || target.IsDead || distance > reach + 0.05f)
                    continue;
                if (target.TakeDamage(damage))
                    weapon.ReportHit(target);
            }
        }

        // Nivel III: daño alrededor del final del chorro.
        void Splash(Vector2 end)
        {
            int damage = Mathf.Max(0, Mathf.CeilToInt(attack.JetSplashDamage * weapon.DamageMultiplier - 0.001f));
            // Las tres salpicaduras se alternan en cada golpe.
            var splashes = attack.JetSplashEffects;
            if (splashes.Length > 0)
                OneShotEffect.Spawn(splashes[splashVariant++ % splashes.Length], end);
            struck.Clear();
            foreach (var hit in Physics2D.OverlapCircleAll(end, attack.JetSplashRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                if (hit.isTrigger)
                    continue;
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !struck.Add(target))
                    continue;
                if (target.TakeDamage(damage))
                    weapon.ReportHit(target);
            }
        }

        // ---------- Dibujo ----------

        // El chorro son huecos fijos de 1 u desde la cola, girados hacia el cursor, entre dos distancias: el primero es el inicio, los
        // siguientes alternan los tramos A, B y C y, si el chorro no choca, el último es la punta. Los extremos se recortan (no se
        // aplastan) a lo visible. Todos los tramos van en el mismo cuadro, con el mismo reloj.
        void DrawSegments(Vector2 origin, Vector2 direction, float from, float to, bool tip)
        {
            var art = attack != null ? attack.JetArt : null;
            if (art == null || !art.IsComplete)
            {
                foreach (var segment in segments)
                    segment.enabled = false;
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int frame = JetArt.CurrentFrame;
            int first = Mathf.FloorToInt(from);
            int last = Mathf.CeilToInt(to) - 1;
            int count = Mathf.Max(0, last - first + 1);
            for (int i = 0; i < count; i++)
            {
                while (segments.Count <= i)
                {
                    var created = new GameObject("JetSegment");
                    created.transform.SetParent(transform.parent, false);
                    var renderer = created.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 19;
                    segments.Add(renderer);
                }

                int slot = first + i;
                // Lo visible del hueco, en píxeles desde su borde izquierdo.
                int left = Mathf.RoundToInt((Mathf.Max(slot, from) - slot) * SegmentPixels);
                int right = Mathf.RoundToInt((Mathf.Min(slot + 1, to) - slot) * SegmentPixels);
                var segmentRenderer = segments[i];
                if (right - left <= 0)
                {
                    segmentRenderer.enabled = false;
                    continue;
                }

                var sprite = slot == 0 ? art.StartFrame(frame)
                    : tip && slot == last ? art.Tip((slot - 1) % JetArt.Variants, frame)
                    : art.Stream((slot - 1) % JetArt.Variants, frame);
                segmentRenderer.enabled = true;
                segmentRenderer.sprite = Crop(sprite, left, right);
                segmentRenderer.transform.SetPositionAndRotation(origin + direction * (slot + left / (float)SegmentPixels), Quaternion.Euler(0f, 0f, angle));
            }
            for (int i = count; i < segments.Count; i++)
                segments[i].enabled = false;
        }

        // El cuadro recortado a los píxeles de left a right, con el pivote en su borde izquierdo a media altura.
        static Sprite Crop(Sprite sprite, int left, int right)
        {
            if (left <= 0 && right >= sprite.rect.width)
                return sprite;
            var key = (sprite, left, right);
            if (croppedSprites.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var rect = new Rect(sprite.rect.x + left, sprite.rect.y, right - left, sprite.rect.height);
            var cropped = Sprite.Create(sprite.texture, rect, new Vector2(0f, 0.5f), sprite.pixelsPerUnit);
            croppedSprites[key] = cropped;
            return cropped;
        }

        // Impacto cuando el chorro ya ha llegado a lo que lo corta (girado con el chorro: está dibujado llegando desde la izquierda), y
        // salida en la punta de la cola.
        void DrawEffects(Vector2 origin, Vector2 end, bool blocked, Vector2 direction)
        {
            if (blocked && attack.JetHitEffect != null)
            {
                if (hitEffect == null)
                    hitEffect = Instantiate(attack.JetHitEffect, transform.parent);
                hitEffect.SetActive(true);
                hitEffect.transform.SetPositionAndRotation(end, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            }
            else if (hitEffect != null)
            {
                hitEffect.SetActive(false);
            }

            if (attack.MuzzleEffect != null)
            {
                if (muzzleEffect == null)
                    muzzleEffect = Instantiate(attack.MuzzleEffect, transform.parent);
                muzzleEffect.transform.position = origin;
            }
        }

        void ClearDrawing()
        {
            retracting = false;
            foreach (var segment in segments)
            {
                if (segment != null)
                    Destroy(segment.gameObject);
            }
            segments.Clear();
            ClearEffects();
        }

        void ClearEffects()
        {
            if (hitEffect != null)
                Destroy(hitEffect);
            if (muzzleEffect != null)
                Destroy(muzzleEffect);
            hitEffect = muzzleEffect = null;
        }

        // ---------- Animación del cuerpo ----------

        string FacingName() => animator.CurrentFacing.ToString().ToLowerInvariant();

        // La cola alzada sigue la orientación del cuerpo: si cambia (lado, arriba, abajo), cambia el bucle.
        void PlayLoop()
        {
            string clip = $"AttackLoop_{FacingName()}";
            if (clip == loopClip)
                return;
            loopClip = clip;
            animator.Hold(clip);
        }

        Vector2 TipOffset()
        {
            Vector2 tip = animator.CurrentFacing switch
            {
                PlayerAnimator.Facing.Down => DownTip,
                PlayerAnimator.Facing.Up => UpTip,
                _ => SideTip,
            };
            if (animator.Body != null && animator.Body.flipX)
                tip.x = -tip.x;
            return tip / PixelsPerUnit;
        }

        // ---------- Fin ----------

        /// <summary>Deja de echar chorro: el chorro se recoge hacia el final, baja la cola y vuelve a reposo.</summary>
        void Stop()
        {
            ClearEffects();
            if (reach > 0.05f && attack != null && attack.JetArt != null)
            {
                retracting = true;
                tailDistance = 0f;
            }
            else
            {
                ClearDrawing();
            }

            string clip = $"AttackEnd_{FacingName()}";
            if (animator.Hold(clip))
            {
                phaseTimer = animator.ClipLength(clip, DefaultEndingSeconds);
                State = Phase.Ending;
            }
            else
            {
                Rest();
            }
        }

        void Rest()
        {
            animator.Release();
            State = Phase.Idle;
            loopClip = null;
        }

        /// <summary>Corta el chorro ya (esquiva, muerte): desaparece sin recogerse y la animación vuelve a la normal.</summary>
        public void Cancel()
        {
            ClearDrawing();
            if (State == Phase.Idle)
                return;
            Rest();
        }

        // ---------- Barra de carga ----------

        void EnsureBar()
        {
            if (bar != null)
                return;
            bar = JetBar.Create(transform, this);
        }

        /// <summary>
        /// Barra de carga bajo el jugador (provisional, dibujada por código): solo se ve cuando no está llena. Se pone algo más abajo que la
        /// de recarga del Órgano para no pisarla.
        /// </summary>
        class JetBar : MonoBehaviour
        {
            const float Width = 16f;
            static readonly Color Back = new(0.05f, 0.05f, 0.08f, 0.85f);
            static readonly Color Fill = new(0.95f, 0.85f, 0.45f);
            static readonly Color Empty = new(0.9f, 0.35f, 0.25f);
            static Sprite pixel;

            JetAttack source;
            SpriteRenderer background;
            SpriteRenderer fill;

            internal static JetBar Create(Transform player, JetAttack source)
            {
                var root = new GameObject("JetBar");
                root.transform.SetParent(player, false);
                root.transform.localPosition = new Vector3(0f, -1.05f, 0f);
                var bar = root.AddComponent<JetBar>();
                bar.source = source;
                bar.background = Part(root.transform, "Background", Back, 30);
                bar.fill = Part(root.transform, "Fill", Fill, 31);
                return bar;
            }

            static SpriteRenderer Part(Transform parent, string name, Color color, int order)
            {
                var part = new GameObject(name);
                part.transform.SetParent(parent, false);
                part.transform.localScale = new Vector3(Width, 2f, 1f);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = Pixel();
                renderer.color = color;
                renderer.sortingOrder = order;
                return renderer;
            }

            static Sprite Pixel()
            {
                if (pixel != null)
                    return pixel;
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
                return pixel;
            }

            void LateUpdate()
            {
                float amount = source != null ? source.Charge01 : 1f;
                bool visible = source != null && (amount < 0.999f || source.IsLocked || source.State == Phase.Firing);
                background.enabled = fill.enabled = visible;
                if (!visible)
                    return;

                // Anclada a la izquierda; si se vació del todo, en rojo hasta que se llene.
                fill.transform.localScale = new Vector3(Width * amount, 2f, 1f);
                fill.transform.localPosition = new Vector3(-Width / 16f * (1f - amount) / 2f, 0f, 0f);
                fill.color = source.IsLocked ? Empty : Fill;
            }
        }
    }
}
