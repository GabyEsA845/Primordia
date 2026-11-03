using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El jugador envenenado en el piso 3, con dos orígenes. <b>Charco</b> (<see cref="TouchPuddle"/>): lo frena un 30 % y le
    /// quita 1 de vida al empezar cada ventana de 3 s. <b>Nube tóxica</b> (<see cref="Feed"/>): una barra verde sobre la cabeza
    /// que se llena en 2 s dentro de la nube; al llenarse hace 1 de daño, queda intoxicado 4 s (1 de daño cada 2 s aunque salga) y
    /// la barra empieza otra vez. Fuera de la nube la barra se vacía sola en unos 2 s y esquivar la vacía de golpe. Lo añade
    /// <see cref="FloorGenerator"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerToxin : MonoBehaviour
    {
        const int Damage = 1;
        const float FillSeconds = 2f;
        const float EmptySeconds = 2f;
        const float IntoxicatedSeconds = 4f;
        const float TickEvery = 2f;
        const float PuddleWindowSeconds = 3f;
        // La ralentización del charco se renueva mientras se pisa; dura un instante más.
        const float SlowHold = 0.3f;
        // Cuánto dura el aviso de la nube (algo más que el intervalo entre avisos).
        const float InsideGrace = 0.25f;

        static readonly Color DropColor = new(0.66f, 0.88f, 0.25f, 1f);

        PlayerController controller;
        Health health;
        BurnIcon icon;
        // Hasta cuándo se considera que sigue dentro de la nube: la nube avisa cada 0,1 s y la barra se actualiza cada cuadro.
        float insideUntil;
        float intoxicatedUntil;
        float nextTick;
        float puddleWindowEnd;

        /// <summary>Barra de veneno de la nube, de 0 a 1.</summary>
        public float Bar { get; private set; }

        public bool IsIntoxicated => Time.time < intoxicatedUntil;

        public bool IsDodging => controller != null && (controller.IsDodging || controller.IsLunging);

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            controller.DodgeStarted += OnDodgeStarted;
            if (health != null)
                health.Died += Clear;
        }

        void OnDisable()
        {
            controller.DodgeStarted -= OnDodgeStarted;
            if (health != null)
                health.Died -= Clear;
            Clear();
        }

        void OnDestroy()
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }

        /// <summary>Lo llama un charco mientras el jugador lo pisa: frena y, una vez cada ventana de 3 s, quita 1 de vida.</summary>
        public void TouchPuddle()
        {
            if (PoisonImmune.Has(this))
                return;
            controller.Slow(Toxins.PuddleSlow, SlowHold);
            if (Time.time < puddleWindowEnd)
                return;
            puddleWindowEnd = Time.time + PuddleWindowSeconds;
            Hurt();
            ShowMarker(PuddleWindowSeconds);
        }

        /// <summary>
        /// Lo llama la nube tóxica cada poco mientras el jugador está dentro: mientras llegue, la barra se llena sin parar (no solo en
        /// los cuadros en que llega el aviso) y, al dejar de llegar, tras un instante empieza a vaciarse.
        /// </summary>
        public void Feed()
        {
            if (!PoisonImmune.Has(this))
                insideUntil = Time.time + InsideGrace;
        }

        void Update()
        {
            if (health != null && health.IsDead)
                return;

            if (Time.time < insideUntil)
                Bar += Time.deltaTime / FillSeconds;
            else if (Bar > 0f)
                Bar = Mathf.Max(0f, Bar - Time.deltaTime / EmptySeconds);

            if (Bar >= 1f)
            {
                Hurt();
                Bar = 0f;
                intoxicatedUntil = Time.time + IntoxicatedSeconds;
                nextTick = Time.time + TickEvery;
                ShowMarker(0f);
            }

            if (IsIntoxicated)
            {
                if (Time.time >= nextTick)
                {
                    nextTick += TickEvery;
                    Hurt();
                }
            }
            else if (intoxicatedUntil > 0f)
            {
                intoxicatedUntil = 0f;
                HideMarker();
            }

            UpdateIcon();
        }

        void OnDodgeStarted()
        {
            if (Bar <= 0f)
                return;
            Bar = 0f;
            UpdateIcon();
        }

        void Hurt()
        {
            if (health != null)
                health.TakeDamage(Damage, DamageKind.Tick);
        }

        // Las gotas del arte sobre el cuerpo mientras dura el veneno (0 = hasta que se quite).
        void ShowMarker(float seconds)
        {
            var kit = ReactionKit.Current;
            if (health != null && kit != null)
                StatusMarkers.Show(health, Poison.Key, kit.PoisonedMarker, seconds, overhead: false);
        }

        void HideMarker()
        {
            if (health != null)
                StatusMarkers.Hide(health, Poison.Key);
        }

        // Con la barra vacía pero intoxicado, la gota sigue a la vista y se vacía con el tiempo que queda de veneno.
        void UpdateIcon()
        {
            float shown = Bar;
            if (shown <= 0.001f && IsIntoxicated)
                shown = (intoxicatedUntil - Time.time) / IntoxicatedSeconds;
            bool visible = shown > 0.001f;
            if (!visible && icon == null)
                return;
            if (icon == null)
                icon = BurnIcon.Create(transform, BurnIcon.Shape.Cloud, DropColor, new Vector3(0.55f, 1.05f, 0f));
            icon.Set(shown, visible, IsIntoxicated);
        }

        void Clear()
        {
            Bar = 0f;
            insideUntil = 0f;
            intoxicatedUntil = 0f;
            puddleWindowEnd = 0f;
            HideMarker();
            if (icon != null)
                icon.Set(0f, false, false);
        }
    }
}
