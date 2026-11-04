using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Acecho del fango (Eryops): cruza el lodo (el profundo y los charcos de lodo) sin hundirse, solo más despacio, medio sumergido como un
    /// cazador al acecho, y gana +30 % de velocidad mientras está en el lodo y 5 s después de salir (se renueva cada vez que entra). El barro
    /// se queda pegado: gotea, se seca por manchas y se desprende en escamas (<see cref="MudBodyVisual"/> en modo manual); mientras dura la
    /// mejora salen terrones de las patas a cada paso, y en el lodo, una estela. Lo pone <see cref="PlayerLineage"/> con el linaje Eryops
    /// (cuerpo de la Peonza). Los números son ajustes de <see cref="LineageBenefitSettings"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class EryopsTouch : MonoBehaviour
    {
        // Cuánto del alto del cuerpo cubre el barro al acecho y qué parte de eso queda hundida bajo la superficie.
        const float StalkCover = 0.55f;
        const float StalkSink = 0.8f;
        // Al salir del lodo el cuerpo se levanta en un momento.
        const float StandUpSeconds = 0.25f;
        // La línea de tiempo del barro fuera del lodo, pensada para 5 s (se escala si la mejora dura otra cosa).
        const float ReferenceSeconds = 5f;
        const float DripUntil = 1.4f;
        const float DryFrom = 0.8f;
        const float DryTo = 4f;
        const float DetachFrom = 4.4f;
        const float DetachClean = 0.2f;
        const float DripEvery = 0.35f;
        const float KickEvery = 0.25f;
        const float KickFrameSeconds = 0.05f;
        const float FlakeFrameSeconds = 0.1f;
        const float DripFrameSeconds = 0.08f;
        const float WakeFrameSeconds = 0.1f;
        const float MoveEpsilon = 0.002f;
        static readonly Vector2 FootOffset = new(0f, -0.3f);

        PlayerController controller;
        PlayerMudSink sink;
        float speedBonus = 0.3f;
        float mudSpeed = 0.6f;
        float buffSeconds = 5f;
        bool inMud;
        bool afterMud;
        bool flakesDropped;
        float buffUntil = -1f;
        float exitTime;
        float nextDrip;
        float nextKick;
        Vector2 lastPosition;
        SpriteRenderer wake;

        // Para las pruebas, que no montan el mapa del piso.
        internal bool? forceInMud;

        /// <summary>True mientras dura la mejora (en el lodo o hasta 5 s después).</summary>
        public bool Buffed => Time.time < buffUntil;

        public bool InMud => inMud;

        /// <summary>True mientras lleva el barro encima (en el lodo o secándose tras salir).</summary>
        public bool Muddy => inMud || afterMud;

        public void Configure(float newSpeedBonus, float newMudSpeed, float newBuffSeconds)
        {
            speedBonus = Mathf.Max(0f, newSpeedBonus);
            mudSpeed = Mathf.Clamp(newMudSpeed, 0.1f, 1f);
            buffSeconds = Mathf.Max(0.5f, newBuffSeconds);
            Apply();
        }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            lastPosition = transform.position;
        }

        // Cruzar el lodo sin hundirse lo hace el hundimiento del jugador, que puede ponerse después que el linaje.
        void Apply()
        {
            if (sink == null)
                sink = GetComponent<PlayerMudSink>();
            if (sink != null)
            {
                sink.SinkImmune = true;
                sink.MudWalkSpeed = mudSpeed;
            }
        }

        void Update()
        {
            Apply();
            float now = Time.time;
            bool was = inMud;
            inMud = forceInMud ?? DetectMud();
            if (inMud)
            {
                buffUntil = now + buffSeconds;
                afterMud = false;
            }
            else if (was)
            {
                exitTime = now;
                afterMud = true;
                flakesDropped = false;
            }
            controller.BoostSpeedFactor = Buffed ? 1f + speedBonus : 1f;

            Vector2 position = transform.position;
            Vector2 step = position - lastPosition;
            bool moving = step.magnitude > MoveEpsilon;
            lastPosition = position;

            UpdateMud(now);
            UpdateEffects(now, moving, step);
        }

        bool DetectMud()
        {
            // Saltando se pasa por encima.
            if (controller.IsDodging)
                return false;
            Vector2 feet = (Vector2)transform.position + FootOffset;
            return Pits.IsDeepInMud(feet) || GroundPuddle.IsSludgeOver(feet);
        }

        // ---------- El barro en el cuerpo ----------

        void UpdateMud(float now)
        {
            if (inMud)
            {
                MudBodyVisual.SetManual(transform, new MudBodyVisual.Manual { cover = StalkCover, sink = StalkSink });
                return;
            }
            if (!afterMud)
                return;

            float scale = buffSeconds / ReferenceSeconds;
            float t = (now - exitTime) / scale;
            if (t >= ReferenceSeconds)
            {
                afterMud = false;
                MudBodyVisual.ClearManual(transform);
                return;
            }

            if (t < DetachFrom)
            {
                // Se levanta del barro, que se va secando por manchas.
                float standUp = Mathf.Clamp01((now - exitTime) / StandUpSeconds);
                float dry = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DryFrom, DryTo, t));
                MudBodyVisual.SetManual(transform, new MudBodyVisual.Manual { cover = StalkCover, sink = StalkSink * (1f - standUp), dry = dry });
            }
            else
            {
                // Se desprende: quedan las manchas de siempre, que se limpian.
                float clean = Mathf.Lerp(DetachClean, 1f, Mathf.InverseLerp(DetachFrom, ReferenceSeconds, t));
                MudBodyVisual.SetManual(transform, new MudBodyVisual.Manual { cover = 0f, sink = 0f, stain = StalkCover, clean = clean });
            }
        }

        // ---------- Efectos ----------

        void UpdateEffects(float now, bool moving, Vector2 step)
        {
            var art = PolishArt.Current;
            if (art == null)
                return;

            int order = BodyOrder();
            var bounds = BodyBounds();
            float feetY = FeetY(bounds);
            float scale = buffSeconds / ReferenceSeconds;
            float t = afterMud ? (now - exitTime) / scale : 0f;

            // Goteo al salir del barro.
            if (afterMud && !inMud && t < DripUntil && now >= nextDrip)
            {
                nextDrip = now + DripEvery;
                var at = new Vector2(bounds.center.x + (Random.value - 0.5f) * bounds.size.x * 0.7f, bounds.min.y + bounds.size.y * 0.5f * Random.value);
                SpriteFramePlayer.PlayOnce(art.mudDrip, DripFrameSeconds, at, order + 3);
            }

            // Al desprenderse, caen escamas de barro seco.
            if (afterMud && !flakesDropped && t >= DetachFrom)
            {
                flakesDropped = true;
                int count = Random.Range(2, 4);
                for (int i = 0; i < count; i++)
                {
                    var at = new Vector2(bounds.center.x + (Random.value - 0.5f) * bounds.size.x * 0.8f, bounds.min.y + bounds.size.y * (0.3f + 0.4f * Random.value));
                    SpriteFramePlayer.PlayOnce(art.eryopsFlake, FlakeFrameSeconds, at, order + 3);
                }
            }

            // Marcador de la mejora: terrones que saltan de las patas a cada paso (dibujados yendo a la derecha).
            if (Buffed && moving && now >= nextKick)
            {
                nextKick = now + KickEvery;
                var kick = SpriteFramePlayer.PlayOnce(art.eryopsKick, KickFrameSeconds, new Vector2(bounds.center.x, feetY), order + 1);
                if (kick != null && kick.TryGetComponent<SpriteRenderer>(out var renderer))
                    renderer.flipX = step.x < 0f;
            }

            UpdateWake(art, moving, step, bounds, feetY, order);
        }

        // Dónde están los pies: la superficie del barro que pinta MudBodyVisual (el borde de la celda del sprite queda casi una unidad más
        // abajo). Sin él, los pies del cuerpo (como el hundimiento del jugador).
        float FeetY(Bounds bounds)
        {
            var visual = GetComponent<MudBodyVisual>();
            if (visual != null && visual.HasBody)
                return visual.FeetY;
            return transform.position.y + FootOffset.y;
        }

        // Estela en V detrás de quien avanza por el lodo (dibujada yendo a la derecha, con el pivote en el cuerpo).
        void UpdateWake(PolishArt art, bool moving, Vector2 step, Bounds bounds, float feetY, int order)
        {
            if (art.eryopsWake.Length == 0)
                return;
            bool show = inMud && moving;
            if (wake == null)
            {
                if (!show)
                    return;
                var obj = new GameObject("EryopsMudWake");
                wake = obj.AddComponent<SpriteRenderer>();
                wake.sprite = art.eryopsWake[0];
                obj.AddComponent<SpriteFramePlayer>().Play(wake, art.eryopsWake, WakeFrameSeconds, newLoop: true);
            }
            wake.enabled = show;
            if (!show)
                return;
            wake.sortingOrder = order - 1;
            wake.transform.position = new Vector3(bounds.center.x, feetY, 0f);
            // Gira con el movimiento (suavizado: un paso suelto no debe sacudirla).
            float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg;
            wake.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(wake.transform.eulerAngles.z, angle, 0.35f));
        }

        // Lo que ocupa el cuerpo del jugador ahora (los dibujos, no la celda).
        Bounds BodyBounds()
        {
            bool any = false;
            var bounds = new Bounds(transform.position, Vector3.one * 0.8f);
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow"))
                    continue;
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds;
        }

        int BodyOrder()
        {
            int order = 0;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                order = Mathf.Max(order, renderer.sortingOrder);
            return order;
        }

        void OnDestroy()
        {
            if (sink != null)
            {
                sink.SinkImmune = false;
                sink.MudWalkSpeed = 0.6f;
            }
            if (controller != null)
            {
                controller.BoostSpeedFactor = 1f;
                controller.TerrainSpeedFactor = 1f;
            }
            MudBodyVisual.ClearManual(transform);
            if (wake != null)
                Destroy(wake.gameObject);
        }
    }
}
