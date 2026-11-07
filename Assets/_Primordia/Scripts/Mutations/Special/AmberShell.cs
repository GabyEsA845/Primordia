using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Hojas de un bloque de ámbar de una talla (docs/multimedia-temp/floor3-capsule y floor3-drop), compartidas por la Cápsula y la Gota:
    /// formación, reposo, grietas, rotura y, en la Cápsula de nivel III, las vetas del suelo. Cada hoja trae su pivote: todas se colocan
    /// en el mismo punto (el centro del cuerpo en la Cápsula, los pies en la Gota).
    /// </summary>
    [Serializable]
    public class AmberShellArt
    {
        [Tooltip("Formación (sube de abajo arriba): detrás de la criatura, delante, resplandor y vetas del suelo (nivel III). Una duración por cuadro.")]
        public Sprite[] formBack = Array.Empty<Sprite>();
        public Sprite[] formFront = Array.Empty<Sprite>();
        public Sprite[] formGlow = Array.Empty<Sprite>();
        public Sprite[] veinsForm = Array.Empty<Sprite>();
        public float[] formDurations = Array.Empty<float>();

        [Tooltip("Reposo: la capa de detrás es fija y la de delante, un bucle.")]
        public Sprite[] holdBack = Array.Empty<Sprite>();
        public Sprite[] holdFront = Array.Empty<Sprite>();
        public Sprite[] veinsHold = Array.Empty<Sprite>();
        public float holdFrame = 0.12f;

        [Tooltip("Grietas (Gota): fase 1, 2 y 3, encima de la capa de delante.")]
        public Sprite[] crack = Array.Empty<Sprite>();

        [Tooltip("Rotura: detrás, delante, resplandor y vetas. Una duración por cuadro; el cuadro que cuenta es el que libera.")]
        public Sprite[] breakBack = Array.Empty<Sprite>();
        public Sprite[] breakFront = Array.Empty<Sprite>();
        public Sprite[] breakGlow = Array.Empty<Sprite>();
        public Sprite[] veinsBreak = Array.Empty<Sprite>();
        public float[] breakDurations = Array.Empty<float>();
        public int breakCountFrame;

        [Tooltip("Altura del centro del bloque sobre el punto de anclaje, en unidades (de ahí salen las esquirlas).")]
        public float centerRise;

        [Tooltip("Cuerpo sólido del bloque (tamaño y centro respecto al anclaje, en unidades): frena al jugador, a los enemigos y a los proyectiles. Cero = sin cuerpo.")]
        public Vector2 solidSize;
        public Vector2 solidOffset;

        [Tooltip("Estrellas de aturdido en bucle sobre el bloque (Gota), y la altura de su base sobre el anclaje.")]
        public Sprite[] stars = Array.Empty<Sprite>();
        public float starsRise;

        public bool IsEmpty => (holdFront == null || holdFront.Length == 0) && (holdBack == null || holdBack.Length == 0);

        /// <summary>Lo que dura la formación.</summary>
        public float FormSeconds => Total(formDurations, Mathf.Max(Len(formBack), Len(formFront)), 0.07f);

        /// <summary>Desde que empieza la rotura hasta el cuadro que libera.</summary>
        public float BreakReleaseSeconds => Total(breakDurations, Mathf.Min(breakCountFrame, BreakFrames), 0.06f);

        public int BreakFrames => Mathf.Max(Len(breakBack), Len(breakFront));

        static int Len(Sprite[] sprites) => sprites?.Length ?? 0;

        static float Total(float[] durations, int count, float fallback)
        {
            float total = 0f;
            for (int i = 0; i < count; i++)
                total += durations != null && i < durations.Length ? durations[i] : fallback;
            return total;
        }
    }

    /// <summary>Capas de la curación de la Cápsula (20 cuadros de 100 ms) que se superponen a la cápsula en reposo.</summary>
    [Serializable]
    public class AmberHealArt
    {
        public Sprite[] back = Array.Empty<Sprite>();
        public Sprite[] front = Array.Empty<Sprite>();
        public Sprite[] hearts = Array.Empty<Sprite>();
        public Sprite[] glow = Array.Empty<Sprite>();
        public float frameSeconds = 0.1f;
        [Tooltip("Cuadros en los que cura un punto de vida (medio corazón), uno por pulso.")]
        public int[] pulseFrames = Array.Empty<int>();
        [Tooltip("Cuánto se tiñe de dorado la criatura en cada cuadro (0-1).")]
        public float[] tint = Array.Empty<float>();
    }

    /// <summary>
    /// Bloque de ámbar de dos capas alrededor de algo (el jugador con la Cápsula, un enemigo con la Gota): la capa de atrás y la de
    /// delante con la criatura en medio. Se forma (con el tiempo detenido en la Cápsula), se mantiene, se agrieta y se rompe con
    /// las hojas dibujadas, sin escalar. Sigue a lo que encierra. <see cref="Broken"/> salta en el cuadro de la rotura que libera;
    /// el bloque se destruye al acabar la animación. <see cref="Break"/> lo revienta antes.
    /// </summary>
    public class AmberShell : MonoBehaviour
    {
        /// <summary>Dónde se ancla el dibujo: el centro de lo encerrado (Cápsula) o sus pies (Gota).</summary>
        public enum Anchor { Center, Feet }

        const float StarFrame = 0.1f;

        Transform target;
        AmberShellArt art;
        AmberHealArt heal;
        Anchor anchorMode;
        Vector2? fixedFeet;
        bool freezeWorld;
        bool worldFrozen;
        float holdSeconds;
        float formEnd;
        float clock;
        float breakStart;
        Vector2 anchorPosition;
        int order = 10;
        bool breaking;
        bool released;
        int crackPhase;
        SpriteRenderer back, front, glow, veins, crackLayer, stars, healBack, healFront, hearts, healGlow;
        GameObject solid;

        /// <summary>Al llegar el cuadro de la rotura que libera, con el centro del bloque.</summary>
        public event Action<Vector2> Broken;

        /// <summary>True desde que empieza a romperse.</summary>
        public bool IsBroken => breaking;

        /// <summary>Segundos del bloque (negativo mientras espera su turno).</summary>
        public float Clock => clock;

        /// <summary>Lo que dura la formación.</summary>
        public float FormSeconds => formEnd;

        /// <summary>Segundos que lleva en reposo (0 mientras se forma).</summary>
        public float HoldTime => Mathf.Max(0f, clock - formEnd);

        public bool IsForming => !breaking && clock < formEnd;

        /// <summary>Las grietas se ponen solas según el tiempo que queda (la Gota de nivel I-II); si no, las fija <see cref="SetCrackPhase"/>.</summary>
        public bool CrackByTime { get; set; }

        /// <summary>Centro del bloque.</summary>
        public Vector2 Center => anchorPosition + Vector2.up * (art?.centerRise ?? 0f);

        /// <param name="holdSeconds">Segundos en reposo, entre el final de la formación y el principio de la rotura.</param>
        /// <param name="startDelay">Segundos de espera antes de empezar a formarse (el impacto de la gota).</param>
        /// <param name="freezeWorld">Detiene el tiempo del juego mientras se forma.</param>
        /// <param name="fixedFeet">Pies del bloque si lo encerrado no tiene cuerpo que medir (un cadáver).</param>
        public static AmberShell Create(Transform target, AmberShellArt art, float holdSeconds, Anchor anchor = Anchor.Center,
            float startDelay = 0f, bool freezeWorld = false, Vector2? fixedFeet = null, AmberHealArt heal = null)
        {
            var go = new GameObject("AmberShell");
            var shell = go.AddComponent<AmberShell>();
            shell.target = target;
            shell.art = art ?? new AmberShellArt();
            shell.heal = heal;
            shell.anchorMode = anchor;
            shell.fixedFeet = fixedFeet;
            shell.freezeWorld = freezeWorld;
            shell.holdSeconds = Mathf.Max(0.05f, holdSeconds);
            shell.formEnd = shell.art.FormSeconds;
            shell.clock = -Mathf.Max(0f, startDelay);
            shell.Measure();
            shell.Build();
            shell.Draw();
            return shell;
        }

        /// <summary>Fase de grietas (0 = ninguna, 1 a 3) cuando no van por tiempo.</summary>
        public void SetCrackPhase(int phase) => crackPhase = Mathf.Clamp(phase, 0, 3);

        void Measure()
        {
            if (fixedFeet.HasValue)
            {
                anchorPosition = fixedFeet.Value;
                return;
            }
            if (target == null)
                return;

            bool any = false;
            var bounds = new Bounds(target.position, Vector3.zero);
            int top = int.MinValue;
            foreach (var renderer in target.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow") || renderer.gameObject.name.Contains("Halo"))
                    continue;
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
                top = Mathf.Max(top, renderer.sortingOrder);
            }
            if (top != int.MinValue)
                order = top;

            if (anchorMode == Anchor.Center)
            {
                anchorPosition = any ? (Vector2)bounds.center : (Vector2)target.position;
                return;
            }

            anchorPosition = FeetOf(target);
        }

        /// <summary>Pies de algo: la base de sus colliders (el dibujo trae aire por debajo) o, sin ellos, la de su dibujo; centrado en el dibujo.</summary>
        public static Vector2 FeetOf(Transform thing)
        {
            bool any = false;
            var bounds = new Bounds(thing.position, Vector3.zero);
            foreach (var renderer in thing.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow") || renderer.gameObject.name.Contains("Halo"))
                    continue;
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            float feetY = any ? bounds.min.y : thing.position.y;
            bool anyCollider = false;
            float colliderFeet = float.MaxValue;
            foreach (var collider in thing.GetComponentsInChildren<Collider2D>())
            {
                if (!collider.enabled)
                    continue;
                anyCollider = true;
                colliderFeet = Mathf.Min(colliderFeet, collider.bounds.min.y);
            }
            if (anyCollider)
                feetY = colliderFeet;
            return new Vector2(any ? bounds.center.x : thing.position.x, feetY);
        }

        void Build()
        {
            glow = NewLayer("Glow", order - 3);
            veins = NewLayer("Veins", order - 4);
            // Pegada a la criatura: por encima del anillo de atrás del lodo (mínimo - 1) y del agua (órdenes negativos).
            back = NewLayer("Back", order - 1);
            front = NewLayer("Front", order + 2);
            crackLayer = NewLayer("Crack", order + 3);
            if (art.stars != null && art.stars.Length > 0)
            {
                stars = NewLayer("Stars", order + 5);
                stars.transform.localPosition = Vector3.up * art.starsRise;
            }
            if (heal != null)
            {
                healBack = NewLayer("HealBack", order - 1);
                healFront = NewLayer("HealFront", order + 3);
                hearts = NewLayer("HealHearts", order + 4);
                healGlow = NewLayer("HealGlow", order - 3);
            }
            transform.position = anchorPosition;

            if (art.solidSize.x > 0f && art.solidSize.y > 0f)
            {
                // Sólido de verdad (capa por defecto, como las paredes y el cadáver de ámbar), activo desde que empieza a formarse hasta que libera.
                solid = new GameObject("Solid");
                solid.transform.SetParent(transform, false);
                solid.transform.localPosition = art.solidOffset;
                solid.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                var box = solid.AddComponent<BoxCollider2D>();
                box.size = art.solidSize;
                // Lo encerrado está DENTRO del bloque: sin esto la física lo empuja hacia fuera y, como el bloque lo sigue, se escapa
                // sin parar (el enemigo se caía de la pantalla). El bloque solo frena a los demás.
                if (target != null)
                {
                    foreach (var inside in target.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(box, inside);
                }
                solid.SetActive(clock >= 0f);
            }
        }

        SpriteRenderer NewLayer(string layerName, int sortingOrder)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        void Update()
        {
            bool paused = GameManager.Instance != null && GameManager.Instance.State != GameState.Playing;
            bool freeze = freezeWorld && !breaking && clock >= 0f && clock < formEnd && !paused;
            SetWorldFrozen(freeze);
            clock += paused ? 0f : freeze ? Time.unscaledDeltaTime : Time.deltaTime;

            if (target != null)
            {
                Measure();
                transform.position = anchorPosition;
            }

            if (!breaking && clock >= formEnd + holdSeconds)
                Break();
            Draw();
        }

        void SetWorldFrozen(bool frozen)
        {
            if (worldFrozen == frozen)
                return;
            worldFrozen = frozen;
            TimeWarp.Freeze(frozen);
        }

        void OnDestroy() => SetWorldFrozen(false);

        void Draw()
        {
            if (breaking)
            {
                DrawBreak();
                return;
            }

            if (clock < 0f)
            {
                SetAll(null);
                return;
            }
            if (solid != null && !solid.activeSelf)
                solid.SetActive(true);

            if (clock < formEnd)
            {
                int i = FrameAt(clock, art.formDurations, Mathf.Max(Count(art.formBack), Count(art.formFront)), 0.07f);
                back.sprite = At(art.formBack, i);
                front.sprite = At(art.formFront, i);
                glow.sprite = At(art.formGlow, i);
                veins.sprite = At(art.veinsForm, i);
                crackLayer.sprite = null;
                SetHeal(-1);
                SetStars(null);
                return;
            }

            float held = clock - formEnd;
            int frame = Mathf.FloorToInt(held / Mathf.Max(0.02f, art.holdFrame));
            back.sprite = Loop(art.holdBack, frame);
            front.sprite = Loop(art.holdFront, frame);
            veins.sprite = Loop(art.veinsHold, frame);
            glow.sprite = null;
            crackLayer.sprite = CrackSprite();
            SetHeal(heal == null ? -1 : Mathf.FloorToInt(held / Mathf.Max(0.02f, heal.frameSeconds)));
            SetStars(art.stars);
        }

        Sprite CrackSprite()
        {
            if (art.crack == null || art.crack.Length == 0)
                return null;
            int phase = crackPhase;
            if (CrackByTime)
            {
                float remaining = formEnd + holdSeconds - clock;
                phase = remaining <= 0.5f ? 3 : remaining <= 1f ? 2 : remaining <= 1.5f ? 1 : 0;
            }
            return phase <= 0 ? null : art.crack[Mathf.Min(art.crack.Length, phase) - 1];
        }

        void SetHeal(int frame)
        {
            if (heal == null)
                return;
            healBack.sprite = frame >= 0 ? At(heal.back, frame, false) : null;
            healFront.sprite = frame >= 0 ? At(heal.front, frame, false) : null;
            hearts.sprite = frame >= 0 ? At(heal.hearts, frame, false) : null;
            healGlow.sprite = frame >= 0 ? At(heal.glow, frame, false) : null;
        }

        void SetStars(Sprite[] frames)
        {
            if (stars == null)
                return;
            stars.sprite = frames == null || frames.Length == 0 ? null : frames[Mathf.FloorToInt(Mathf.Max(0f, clock) / StarFrame) % frames.Length];
        }

        void DrawBreak()
        {
            int count = art.BreakFrames;
            int i = count == 0 ? 0 : FrameAt(clock - breakStart, art.breakDurations, count, 0.06f);
            if (!released && i >= Mathf.Min(art.breakCountFrame, count))
            {
                released = true;
                if (solid != null)
                    solid.SetActive(false);
                Broken?.Invoke(Center);
            }
            if (i >= count)
            {
                Destroy(gameObject);
                return;
            }
            back.sprite = At(art.breakBack, i);
            front.sprite = At(art.breakFront, i);
            glow.sprite = At(art.breakGlow, i);
            veins.sprite = At(art.veinsBreak, i);
            crackLayer.sprite = null;
            SetHeal(-1);
            SetStars(null);
        }

        void SetAll(Sprite sprite)
        {
            back.sprite = front.sprite = glow.sprite = veins.sprite = crackLayer.sprite = sprite;
            SetHeal(-1);
            SetStars(null);
        }

        static int Count(Sprite[] sprites) => sprites?.Length ?? 0;

        static Sprite At(Sprite[] sprites, int index, bool clamp = true)
        {
            if (sprites == null || sprites.Length == 0 || index < 0)
                return null;
            if (index >= sprites.Length)
                return clamp ? sprites[^1] : null;
            return sprites[index];
        }

        static Sprite Loop(Sprite[] sprites, int frame) => sprites == null || sprites.Length == 0 ? null : sprites[frame % sprites.Length];

        /// <summary>Cuadro que toca a esos segundos, con una duración por cuadro; <paramref name="count"/> si ya pasó el último.</summary>
        static int FrameAt(float seconds, float[] durations, int count, float fallback)
        {
            float elapsed = 0f;
            for (int i = 0; i < count; i++)
            {
                elapsed += durations != null && i < durations.Length ? durations[i] : fallback;
                if (seconds < elapsed)
                    return i;
            }
            return count;
        }

        /// <summary>Lo revienta ya: empieza la rotura y avisa al llegar su cuadro de liberación.</summary>
        public void Break()
        {
            if (breaking)
                return;
            breaking = true;
            clock = Mathf.Max(clock, 0f);
            breakStart = clock;
            SetWorldFrozen(false);
        }
    }

    /// <summary>El estallido de un bloque de ámbar: esquirlas que dañan y empujan (y a veces aturden) a los enemigos de alrededor.</summary>
    public static class AmberShards
    {
        public static int Burst(Vector2 center, float radius, int damage, float push, float stunSeconds, Health ignore = null)
        {
            int hits = 0;
            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health == ignore || health.IsDead || !seen.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;

                hits++;
                if (damage > 0)
                    health.TakeDamage(damage);
                if (health.IsDead || !health.TryGetComponent<EnemyAI>(out var ai))
                    continue;
                Vector2 away = (Vector2)health.transform.position - center;
                if (away.sqrMagnitude < 0.01f)
                    away = Vector2.up;
                // Los jefes ni se empujan ni se aturden.
                bool boss = health.GetComponentInParent<IBoss>() != null;
                if (!boss)
                    ai.Knockback(away.normalized * push, Mathf.Max(0.2f, stunSeconds));
            }
            return hits;
        }
    }
}
