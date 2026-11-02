using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// El jugador en el fuego (piso 3), con una barra de quemado sobre la cabeza como la del fuego de Enter the Gungeon: pisar
    /// fuego la llena (1 s de fuego seguido); llena, hace medio corazón. La barra no baja sola: con la barra a medias fuera del
    /// fuego sigue subiendo despacio. Tras el daño el jugador sigue ardiendo: la barra se vacía y vuelve a llenarse sola cada
    /// 1,5 s (otro medio corazón cada vez). Solo esquivar lo apaga: cada esquiva quita el 75 % de la barra, y se acaba el
    /// quemado cuando casi no queda barra (a veces hacen falta dos esquivas). Lo añade <see cref="FloorGenerator"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerBurn : MonoBehaviour
    {
        /// <summary>Medio corazón.</summary>
        public const int BurnDamage = 1;

        const float FillSeconds = 1f;
        const float RefillSeconds = 1.5f;
        // Con la barra a medias y fuera del fuego, lo que sube sola por segundo (de 0 a 1).
        const float CreepPerSecond = 0.15f;
        const float DodgeRelief = 0.75f;
        const float ExtinguishBelow = 0.05f;

        PlayerController controller;
        Health health;
        float fed;
        BurnIcon icon;

        /// <summary>Barra de quemado, de 0 a 1.</summary>
        public float Bar { get; private set; }

        /// <summary>True mientras sigue ardiendo (ya se ha llenado la barra al menos una vez desde que se apagó).</summary>
        public bool Burning { get; private set; }

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

        /// <summary>Lo llama el fuego del suelo mientras el jugador lo pisa: llena la barra.</summary>
        public void Feed(float seconds)
        {
            if (FireImmune.Has(this))
                return;
            fed += seconds;
        }

        void Update()
        {
            if (health != null && health.IsDead)
                return;
            if (FireImmune.Has(this))
            {
                Clear();
                return;
            }

            float before = Bar;
            if (fed > 0f)
            {
                Bar += fed / FillSeconds;
                fed = 0f;
            }
            else if (Burning)
                Bar += Time.deltaTime / RefillSeconds;
            else if (Bar > 0f)
                Bar += CreepPerSecond * Time.deltaTime;

            if (Bar >= 1f)
            {
                // Medio corazón; la barra empieza otra vez y, mientras siga ardiendo, vuelve a llenarse sola.
                if (health != null)
                    health.TakeDamage(BurnDamage, DamageKind.Tick);
                Bar = 0f;
                Burning = true;
            }

            Show(before);
        }

        void OnDodgeStarted()
        {
            if (Bar <= 0f && !Burning)
                return;
            Bar = Mathf.Max(0f, Bar - DodgeRelief);
            if (Bar <= ExtinguishBelow)
            {
                Bar = 0f;
                Burning = false;
            }
            Show(1f);
        }

        void Show(float before)
        {
            bool visible = Bar > 0.001f || Burning;
            if (!visible && before <= 0.001f && icon == null)
                return;
            if (icon == null)
                icon = BurnIcon.Create(transform);
            icon.Set(Bar, visible, Burning);
        }

        void Clear()
        {
            Bar = 0f;
            Burning = false;
            fed = 0f;
            if (icon != null)
                icon.Set(0f, false, false);
        }
    }
}
