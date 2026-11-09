using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Combate de la Meganeura, fase 1 (100–65 %; paso 7b): sube y solo se ve su sombra (invulnerable), la sombra persigue al jugador,
    /// una marca ámbar lo sigue y se fija en rojo, y cae en picado (triple picado). Tras cada picado queda un momento a ras de suelo
    /// (vulnerable). Si se clava en el Lepidodendron partido queda atrapada 1,5 s (vulnerable) y se arranca. Al final, un aleteo
    /// fuerte que empuja la niebla, las nubes tóxicas y los proyectiles; si el metano que tiene debajo se enciende mientras está baja,
    /// cae ardiendo (las alas quemadas se quedan así) y queda en el suelo. Las fases 2 y 3 (huevos, tormenta) vendrán en el 7c y el 7d;
    /// mientras, todas las fases repiten este ciclo.
    /// </summary>
    public partial class MeganeuraBoss
    {
        /// <summary>Lo que está haciendo ahora.</summary>
        public enum Mode
        {
            /// <summary>Antes de empezar o posada en el tronco.</summary>
            Perched,
            /// <summary>Remonta hasta salir de pantalla.</summary>
            Climbing,
            /// <summary>Alta: solo se ve su sombra, que persigue al jugador.</summary>
            High,
            /// <summary>Alta, con la marca siguiendo al jugador y luego fija.</summary>
            Marking,
            /// <summary>Cayendo en picado.</summary>
            Diving,
            /// <summary>Clavada en el Lepidodendron partido.</summary>
            Stuck,
            /// <summary>A ras de suelo, vulnerable.</summary>
            Hover,
            /// <summary>Aleteo fuerte.</summary>
            Gust,
            /// <summary>Cae con las alas ardiendo.</summary>
            BurnFall,
            /// <summary>Boca arriba en el suelo (tras arder), vulnerable.</summary>
            Grounded,
            /// <summary>Furiosa junto a la charca (fase 2): chillido y ventana de daño.</summary>
            Rage,
            Dead
        }

        /// <summary>Imágenes sueltas del combate (marcas, impactos, aleteo, astillas, tronco). Las corta el montaje.</summary>
        [Serializable]
        public class Effects
        {
            public Sprite[] diveMarkFollow = Array.Empty<Sprite>();
            public Sprite[] diveMarkLocked = Array.Empty<Sprite>();
            public Sprite[] diveImpact = Array.Empty<Sprite>();
            public Sprite[] diveCrack = Array.Empty<Sprite>();
            [Tooltip("Ráfaga del aleteo (pivote a la izquierda, mira a la derecha).")] public Sprite[] gust = Array.Empty<Sprite>();
            public Sprite[] crash = Array.Empty<Sprite>();
            public Sprite[] splinters = Array.Empty<Sprite>();
            public Sprite[] stumpHit = Array.Empty<Sprite>();
            public Sprite[] stumpGash = Array.Empty<Sprite>();
            [Tooltip("Racimo de huevos (40×32, pie en y = 26): aparece, reposo, eclosiona (ninfa en el cuadro 3) y se rompe.")]
            public Sprite[] eggAppear = Array.Empty<Sprite>();
            public Sprite[] eggIdle = Array.Empty<Sprite>();
            public Sprite[] eggHatch = Array.Empty<Sprite>();
            public Sprite[] eggBreak = Array.Empty<Sprite>();
            [Tooltip("Ondas del chillido (160×112, centro = la cabeza).")] public Sprite[] screech = Array.Empty<Sprite>();
            [Tooltip("Charcas de agua donde pone los huevos (una imagen cada una).")] public Sprite[] puddles = Array.Empty<Sprite>();
        }

        // Alturas (unidades de suelo), tiempos (s) y números del ciclo.
        const float HighAltitude = 16f;
        const float HoverAltitude = 1f;
        const float GustAltitude = 2f;
        const float ClimbSeconds = 0.8f;
        const float ChaseSpeed = 7f;
        const float MarkFollowSeconds = 1.1f;
        const float MarkLockSeconds = 0.7f;
        const float MarkFollowSpeed = 14f;
        const float DiveSeconds = 0.4f;
        const int DivesPerCycle = 3;
        const float DiveRadius = 1.8f;
        const int DiveDamage = 2;
        const float StuckRadius = 2.4f;
        const float StuckSeconds = 1.5f;
        const float GustBlowSeconds = 0.7f;
        const float GustRange = 9f;
        const float GustHalfAngle = 35f;
        const float GustDragSpeed = 5f;
        const float GustProjectileAcceleration = 40f;
        const float GroundedSeconds = 4f;
        const float WoundHeight = 2.5f;

        Coroutine fight;
        Transform playerTarget;
        readonly List<GameObject> temporary = new();

        public Mode CurrentMode { get; private set; } = Mode.Perched;

        /// <summary>True mientras vuela alta o cae en picado: invulnerable (la barra se ve con candado).</summary>
        public bool IsHigh => CurrentMode is Mode.Climbing or Mode.High or Mode.Marking or Mode.Diving;

        static bool Playing => GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        void StartFight()
        {
            StopFight();
            if (shadowRenderer != null)
                shadowRenderer.transform.localPosition = Vector3.zero;
            fight = StartCoroutine(FightLoop());
            MethaneMist.Flashed += OnMistFlash;
        }

        void StopFight()
        {
            MethaneMist.Flashed -= OnMistFlash;
            StopFightRoutine();
        }

        void StopFightRoutine()
        {
            if (fight != null)
                StopCoroutine(fight);
            fight = null;
            foreach (var item in temporary)
            {
                if (item != null)
                    Destroy(item);
            }
            temporary.Clear();
        }

        // ---------- Ciclo ----------

        IEnumerator FightLoop()
        {
            yield return Wait(1.2f);
            while (true)
            {
                // Desde el 65 % de vida: huevos, ninfas y furia (MeganeuraBoss.Phase2.cs).
                if (PhaseIndex >= 1)
                {
                    yield return PhaseTwoCycle();
                    continue;
                }
                for (int i = 0; i < DivesPerCycle; i++)
                {
                    if (PhaseIndex >= 1)
                        break;
                    yield return TakeOff();
                    yield return Chase(i == 0 ? 1.4f : 0.7f);
                    yield return Dive();
                    if (i < DivesPerCycle - 1)
                        yield return LowWindow(0.9f);
                }
                yield return LowWindow(2.5f);
                yield return GustRoutine();
            }
        }

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds;)
            {
                if (Playing)
                    t += Time.deltaTime;
                yield return null;
            }
        }

        // ---------- Sube y persigue ----------

        IEnumerator TakeOff()
        {
            CurrentMode = Mode.Climbing;
            SetVulnerable(false);
            ShowBody(true);
            PlayBody("Climb", 60f, loop: true, restart: true);
            SetAngle(90f);
            float from = Altitude;
            for (float t = 0f; t < ClimbSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(from, HighAltitude, Mathf.SmoothStep(0f, 1f, t / ClimbSeconds));
                yield return null;
            }
            // Fuera de pantalla: solo se ve su sombra.
            bodyRenderer.enabled = false;
            SetShadowAlpha(ShadowAlphaHigh);
            CurrentMode = Mode.High;
        }

        IEnumerator Chase(float seconds)
        {
            CurrentMode = Mode.High;
            if (shadowSprite != null)
                shadowSprite.Play("Meganeura_Shadow_Fly", FlyMs, true);
            for (float t = 0f; t < seconds; t += Playing ? Time.deltaTime : 0f)
            {
                FindPlayer();
                if (playerTarget != null && Playing)
                    MoveTowards(playerTarget.position, ChaseSpeed);
                yield return null;
            }
        }

        // ---------- Picado ----------

        IEnumerator Dive()
        {
            CurrentMode = Mode.Marking;
            FindPlayer();
            Vector2 markPosition = transform.position;
            var mark = NewMark(effects.diveMarkFollow, 0.16f, markPosition);
            for (float t = 0f; t < MarkFollowSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                if (playerTarget != null && Playing)
                {
                    markPosition = Vector2.MoveTowards(markPosition, playerTarget.position, MarkFollowSpeed * Time.deltaTime);
                    MoveTo(markPosition);
                    mark.transform.position = transform.position;
                }
                yield return null;
            }

            // La marca se fija en rojo y las muescas se cierran.
            SetMarkFrames(mark, effects.diveMarkLocked, 0.09f);
            yield return Wait(MarkLockSeconds);

            // Cae: el cuerpo aparece en lo alto, con el morro al blanco.
            CurrentMode = Mode.Diving;
            markPosition = transform.position;
            ShowBody(true);
            SetShadowAlpha(ShadowAlphaGround);
            SetAngle(-90f);
            PlayBody("Dive", 50f, loop: true, restart: true);
            for (float t = 0f; t < DiveSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                float k = t / DiveSeconds;
                Altitude = Mathf.Lerp(HighAltitude, 0f, k * k);
                yield return null;
            }
            Altitude = 0f;
            temporary.Remove(mark);
            Destroy(mark);
            Impact(markPosition);

            if (stumpBuilt && stumpFrames.Length > 0 && Vector2.Distance(markPosition, stumpGround) <= StuckRadius)
                yield return StuckRoutine();
            else
                EnterHover(markPosition);
        }

        void Impact(Vector2 position)
        {
            SpriteFramePlayer.PlayOnce(effects.diveImpact, 0.07f, position, 7);
            SpriteFramePlayer.PlayOnce(effects.diveCrack, 0.4f, position, 1);
            FindPlayer();
            if (playerTarget != null && Vector2.Distance(playerTarget.position, position) <= DiveRadius
                && playerTarget.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(DiveDamage);
            }
        }

        // ---------- Clavada en el tronco ----------

        IEnumerator StuckRoutine()
        {
            CurrentMode = Mode.Stuck;
            MoveTo(stumpGround);
            Altitude = PerchHeight;
            SetAngle(-90f);
            SetVulnerable(true);
            PlayBody("Stuck", 70f, loop: true, restart: true);
            SetStump(effects.stumpHit, 0.07f);
            yield return Wait(0.3f);
            SetStump(stumpGashed ? effects.stumpGash : stumpFrames, 0.2f);
            yield return Wait(StuckSeconds - 0.3f);

            // Se arranca: astillas en el cuadro 2 y el tronco queda con la herida abierta.
            PlayBody("TearFree", 80f, loop: false, restart: true);
            yield return Wait(0.16f);
            SpriteFramePlayer.PlayOnce(effects.splinters, 0.07f, stumpGround + Vector2.up * WoundHeight, 8);
            SetStump(effects.stumpGash, 0.2f);
            stumpGashed = true;
            yield return Wait(0.24f);
            EnterHover(stumpGround);
        }

        bool stumpGashed;

        void SetStump(Sprite[] frames, float seconds)
        {
            if (stumpLoop != null && frames != null && frames.Length > 0)
                stumpLoop.Configure(stumpRenderer, frames, seconds, 0f);
        }

        // ---------- A ras de suelo ----------

        void EnterHover(Vector2 position)
        {
            CurrentMode = Mode.Hover;
            MoveTo(position);
            Altitude = HoverAltitude;
            SetVulnerable(true);
            PlayBody("Hover", 40f, loop: true, restart: true);
        }

        /// <summary>Se queda cernida a ras de suelo (vulnerable), de cara al jugador.</summary>
        IEnumerator LowWindow(float seconds)
        {
            if (CurrentMode != Mode.Hover)
                EnterHover(transform.position);
            for (float t = 0f; t < seconds; t += Playing ? Time.deltaTime : 0f)
            {
                FaceTarget();
                yield return null;
            }
        }

        // ---------- Aleteo fuerte ----------

        IEnumerator GustRoutine()
        {
            CurrentMode = Mode.Gust;
            SetVulnerable(true);
            FaceTarget();
            PlayBody("Gust", 80f, loop: false, restart: true);
            float from = Altitude;
            for (float t = 0f; t < 0.16f; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(from, GustAltitude, t / 0.16f);
                yield return null;
            }

            // El empujón sale en el cuadro 3 de la hoja.
            FindPlayer();
            Vector2 origin = transform.position;
            Vector2 direction = playerTarget != null ? ((Vector2)playerTarget.position - origin).normalized : Vector2.down;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.down;
            var blast = NewGust(origin, direction);
            if (playerTarget != null && Wind.InCone(origin, direction, GustRange, GustHalfAngle, playerTarget.position)
                && playerTarget.TryGetComponent<Player.PlayerController>(out var controller))
            {
                controller.Shove(direction * 6f, 0.3f);
            }
            for (float t = 0f; t < GustBlowSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                if (Playing)
                    Wind.Blow(origin, direction, GustRange, GustHalfAngle, GustDragSpeed, GustProjectileAcceleration, Time.deltaTime);
                yield return null;
            }
            temporary.Remove(blast);
            Destroy(blast);
        }

        // ---------- Prender el metano bajo ella ----------

        void OnMistFlash(Vector2 center, Vector2 size)
        {
            if (CurrentMode is not (Mode.Hover or Mode.Gust))
                return;
            var area = new Bounds(center, size);
            area.Expand(new Vector3(1.6f, 1.6f, 0f));
            if (area.Contains(transform.position))
            {
                StopFightRoutine();
                fight = StartCoroutine(BurnRoutine());
            }
        }

        /// <summary>
        /// Cae con las alas ardiendo (los agujeros quemados se quedan), se estrella, queda boca arriba unos segundos (vulnerable) y
        /// se endereza para volver a subir.
        /// </summary>
        IEnumerator BurnRoutine()
        {
            Burnt = true;
            CurrentMode = Mode.BurnFall;
            SetVulnerable(false);
            ShowBody(true);
            PlayBody("Fall", 70f, loop: true, restart: true);
            PlayFire("Meganeura_Fall_Burning", 70f);
            float from = Altitude;
            const float fallSeconds = 0.6f;
            for (float t = 0f; t < fallSeconds; t += Playing ? Time.deltaTime : 0f)
            {
                Altitude = Mathf.Lerp(from, 0f, t / fallSeconds);
                yield return null;
            }
            Altitude = 0f;
            SpriteFramePlayer.PlayOnce(effects.crash, 0.07f, transform.position, 7);

            CurrentMode = Mode.Grounded;
            SetVulnerable(true);
            PlayBody("Grounded", 150f, loop: true, restart: true);
            PlayFire("Meganeura_Grounded_Burning", 150f);
            yield return Wait(GroundedSeconds);

            PlayFire(null, 0f);
            PlayBody("Recover", 80f, loop: false, restart: true);
            yield return Wait(0.48f);
            fight = StartCoroutine(FightLoop());
        }

        void PlayFire(string sheet, float milliseconds)
        {
            if (fireRenderer == null || fireSprite == null)
                return;
            fireRenderer.enabled = sheet != null;
            if (sheet != null)
            {
                if (bodySprite != null)
                    fireSprite.Angle = bodySprite.Angle;
                fireSprite.Play(sheet, milliseconds, true, restart: true);
            }
        }

        // ---------- Utilidades ----------

        void FindPlayer()
        {
            if (playerTarget != null)
                return;
            var found = GameObject.FindWithTag("Player");
            if (found != null)
                playerTarget = found.transform;
        }

        void FaceTarget()
        {
            FindPlayer();
            if (playerTarget == null)
                return;
            var toPlayer = (Vector2)playerTarget.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.01f)
                SetAngle(Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg);
        }

        void SetVulnerable(bool vulnerable)
        {
            if (TryGetComponent<Collider2D>(out var hit))
                hit.enabled = vulnerable;
        }

        void SetShadowAlpha(float alpha)
        {
            if (shadowRenderer == null)
                return;
            shadowRenderer.enabled = true;
            var color = shadowRenderer.color;
            color.a = alpha;
            shadowRenderer.color = color;
        }

        void MoveTo(Vector2 position)
        {
            // Dentro del suelo de la sala (el arenaSize de la plantilla suma las paredes).
            Vector2 half = new(arenaSize.x * 0.5f - 2.5f, arenaSize.y * 0.5f - 3.5f);
            Vector2 center = ArenaCenter;
            position.x = Mathf.Clamp(position.x, center.x - half.x, center.x + half.x);
            position.y = Mathf.Clamp(position.y, center.y - half.y, center.y + half.y);
            transform.position = position;
            if (TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
        }

        void MoveTowards(Vector2 target, float speed)
        {
            Vector2 current = transform.position;
            var delta = target - current;
            if (delta.sqrMagnitude > 0.04f)
                SetAngle(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            MoveTo(Vector2.MoveTowards(current, target, speed * Time.deltaTime));
        }

        GameObject NewMark(Sprite[] frames, float frameSeconds, Vector2 position)
        {
            var mark = new GameObject("DiveMark");
            mark.transform.position = position;
            var renderer = mark.AddComponent<SpriteRenderer>();
            // En el suelo, por encima de las charcas y por debajo de los personajes.
            renderer.sortingOrder = 3;
            mark.AddComponent<SpriteFramePlayer>().Play(renderer, frames, frameSeconds, newLoop: true);
            temporary.Add(mark);
            return mark;
        }

        static void SetMarkFrames(GameObject mark, Sprite[] frames, float frameSeconds)
        {
            if (mark != null && frames.Length > 0)
                mark.GetComponent<SpriteFramePlayer>().Play(mark.GetComponent<SpriteRenderer>(), frames, frameSeconds, newLoop: true);
        }

        GameObject NewGust(Vector2 origin, Vector2 direction)
        {
            var blast = new GameObject("MeganeuraGust");
            // La ráfaga del dibujo sale del borde izquierdo hacia la derecha: se gira hacia el jugador.
            blast.transform.position = origin + direction * 0.8f;
            blast.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var renderer = blast.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 25;
            blast.AddComponent<SpriteFramePlayer>().Play(renderer, effects.gust, 0.06f, newLoop: true);
            temporary.Add(blast);
            return blast;
        }
    }
}
