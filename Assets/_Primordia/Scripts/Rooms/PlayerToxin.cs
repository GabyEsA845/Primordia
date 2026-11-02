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

        static readonly Color DropColor = new(0.66f, 0.88f, 0.25f, 1f);

        PlayerController controller;
        Health health;
        BurnIcon icon;
        float fed;
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
            controller.Slow(Toxins.PuddleSlow, SlowHold);
            if (Time.time < puddleWindowEnd)
                return;
            puddleWindowEnd = Time.time + PuddleWindowSeconds;
            Hurt();
            ShowMarker(PuddleWindowSeconds);
        }

        /// <summary>Lo llama la nube tóxica mientras el jugador está dentro: llena la barra.</summary>
        public void Feed(float seconds) => fed += seconds;

        void Update()
        {
            if (health != null && health.IsDead)
                return;

            if (fed > 0f)
            {
                Bar += fed / FillSeconds;
                fed = 0f;
            }
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

        void UpdateIcon()
        {
            bool visible = Bar > 0.001f;
            if (!visible && icon == null)
                return;
            if (icon == null)
                icon = BurnIcon.Create(transform, BurnIcon.Shape.Drop, DropColor, new Vector3(0.4f, 1.05f, 0f));
            icon.Set(Bar, visible, IsIntoxicated);
        }

        void Clear()
        {
            Bar = 0f;
            fed = 0f;
            intoxicatedUntil = 0f;
            puddleWindowEnd = 0f;
            HideMarker();
            if (icon != null)
                icon.Set(0f, false, false);
        }
    }
}
