using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="CerciMutation"/>: cuando un proyectil enemigo va a alcanzar al jugador y hay un quiebro listo,
    /// el jugador se hace invulnerable lo que tarda el proyectil en atravesarle (no se mueve; el proyectil sigue de largo) y se
    /// gasta una carga, que se recarga por su cuenta. Con la capa de los cercos detrás del cuerpo, la oscurece mientras no hay
    /// ninguna lista y destella al volver. Es un <see cref="IHitReflector"/> que nunca devuelve el golpe: solo avisa de que llega.
    /// </summary>
    public class CerciDodge : MonoBehaviour, IHitReflector
    {
        internal struct Settings
        {
            public float cooldown;
            public int charges;
            public float speedBonus;
            public float speedSeconds;
            public float dodgeSeconds;
            public float spentBrightness;
            public Sprite[] flashFrames;
            public Sprite[] readyFrames;
            public Sprite[] burstFrames;
            public float frameDuration;
        }

        const int SortingOrder = 22;

        Settings settings;
        Health health;
        PlayerController controller;
        MutationLook look;
        float[] readyAt = System.Array.Empty<float>();
        bool[] wasReady = System.Array.Empty<bool>();
        float boostUntil;
        bool boosting;
        float appliedBoost;

        /// <summary>Quiebros hechos (para las pruebas).</summary>
        public int DodgesMade { get; private set; }

        /// <summary>Quiebros listos ahora.</summary>
        public int ReadyCharges
        {
            get
            {
                int ready = 0;
                foreach (float time in readyAt)
                    if (Time.time >= time)
                        ready++;
                return ready;
            }
        }

        public int Charges => readyAt.Length;

        internal void Configure(Settings newSettings)
        {
            settings = newSettings;
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            look = GetComponent<MutationLook>();
            readyAt = new float[Mathf.Max(1, settings.charges)];
            wasReady = new bool[readyAt.Length];
            for (int i = 0; i < wasReady.Length; i++)
                wasReady[i] = true;
        }

        void OnDestroy()
        {
            EndBoost();
            if (look != null && look.LimbsMirror != null)
                look.LimbsMirror.Tint = Color.white;
        }

        void Update()
        {
            int ready = ReadyCharges;
            for (int i = 0; i < readyAt.Length; i++)
            {
                bool now = Time.time >= readyAt[i];
                // Una carga que vuelve: destello.
                if (now && !wasReady[i])
                    FrameEffect.Play("CerciReady", settings.readyFrames, settings.frameDuration, SortingOrder, null, (Vector2)transform.position + Vector2.up * 0.7f);
                wasReady[i] = now;
            }

            if (look != null && look.LimbsMirror != null)
            {
                float b = ready > 0 ? 1f : settings.spentBrightness;
                look.LimbsMirror.Tint = new Color(b, b, b, 1f);
            }

            if (boosting && Time.time >= boostUntil)
                EndBoost();
        }

        public bool TryReflect(Projectile projectile)
        {
            if (projectile == null || health == null || !health.CanTakeDamage)
                return false;
            // Una carga lista, la que antes vuelva a estar disponible.
            int use = -1;
            for (int i = 0; i < readyAt.Length; i++)
            {
                if (Time.time >= readyAt[i])
                {
                    use = i;
                    break;
                }
            }
            if (use < 0)
                return false;

            readyAt[use] = Time.time + settings.cooldown;
            wasReady[use] = false;
            DodgesMade++;
            health.MakeInvulnerable(settings.dodgeSeconds);

            Vector2 center = transform.position;
            FrameEffect.Play("CerciFlash", settings.flashFrames, settings.frameDuration, SortingOrder, transform, center);
            if (settings.burstFrames != null && settings.burstFrames.Length > 0)
                FrameEffect.Play("CerciBurst", settings.burstFrames, settings.frameDuration, SortingOrder + 1, transform, center);
            if (settings.speedBonus > 0f)
                StartBoost();

            // El proyectil sigue de largo: no se devuelve ni se detiene.
            return false;
        }

        public bool TryReflect(Health attacker) => false;

        void StartBoost()
        {
            boostUntil = Time.time + settings.speedSeconds;
            if (boosting || controller == null)
                return;
            boosting = true;
            appliedBoost = 1f + settings.speedBonus;
            controller.BoostSpeedFactor *= appliedBoost;
        }

        void EndBoost()
        {
            if (!boosting)
                return;
            boosting = false;
            if (controller != null)
                controller.BoostSpeedFactor /= appliedBoost;
        }
    }
}
