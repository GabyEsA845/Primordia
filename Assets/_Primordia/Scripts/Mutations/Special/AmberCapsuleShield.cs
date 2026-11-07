using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="AmberCapsuleMutation"/>: si un golpe deja al jugador a medio corazón o menos (o iba a matarle),
    /// le encierra en una cápsula de ámbar: el tiempo se detiene mientras se forma, y dentro está invulnerable e inmóvil mientras se
    /// cura por pulsos de medio corazón. Al romperse suelta esquirlas y le da unos segundos más de invencibilidad. Las activaciones
    /// se reponen al entrar en otra sala. Antes de activarse, un halo con alas levita sobre su cabeza.
    /// </summary>
    public class AmberCapsuleShield : MonoBehaviour
    {
        internal struct Settings
        {
            public int heal;
            public int perRoom;
            public float shardStun;
            public float lockSeconds;
            public float exitInvulnerability;
            public float shardRadius;
            public int shardDamage;
            public float shardPush;
            public bool levelThree;
            public AmberShellArt shell;
            public AmberHealArt healArt;
            public AmberHaloArt halo;
        }

        // Dorado hacia el que se tiñe la criatura al curarse.
        static readonly Color GoldTint = new(1f, 0.94f, 0.69f, 1f);
        // Pulsos de curación sin dibujo (las pruebas): cada tanto desde que está dentro.
        const float FallbackPulseSeconds = 0.4f;

        Settings settings;
        Health health;
        PlayerController controller;
        PlayerAnimator animator;
        int left;
        bool pending;
        AmberShell shell;
        AmberHalo halo;
        bool lockedMovement;
        bool tinted;
        int healedPulses;
        Room lastRoom;

        /// <summary>Activaciones que quedan en esta sala.</summary>
        public int ActivationsLeft => left;

        /// <summary>True mientras está dentro del bloque.</summary>
        public bool IsEncased => shell != null;

        /// <summary>Veces que se ha encerrado (para las pruebas).</summary>
        public int Encasements { get; private set; }

        /// <summary>El halo sobre la cabeza (null si no hay arte).</summary>
        public AmberHalo Halo => halo;

        internal void Configure(Settings newSettings)
        {
            settings = newSettings;
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            animator = GetComponent<PlayerAnimator>();
            left = settings.perRoom;
            health.Damaged += OnDamaged;
            Health.AddLethalInterceptor(Intercept);
            Room.PlayerEnteredAny += OnRoomEntered;
            halo = AmberHalo.Create(animator, settings.halo, settings.levelThree, left);
        }

        void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            Health.RemoveLethalInterceptor(Intercept);
            Room.PlayerEnteredAny -= OnRoomEntered;
            Unlock();
            ClearTint();
            if (shell != null)
                Destroy(shell.gameObject);
            if (halo != null)
                Destroy(halo.gameObject);
        }

        void OnRoomEntered(Room room)
        {
            if (room == lastRoom)
                return;
            lastRoom = room;
            left = settings.perRoom;
            if (halo != null)
                halo.SetCharges(left);
        }

        // Un golpe mortal con una activación a mano: no se muere; la cápsula lo recoge.
        bool Intercept(Health target) => target == health && left > 0 && shell == null;

        void OnDamaged(int amount)
        {
            if (left > 0 && shell == null && !health.IsDead && health.Current <= 1)
                pending = true;
        }

        void Update()
        {
            if (pending && shell == null && left > 0 && !health.IsDead)
            {
                pending = false;
                Encase();
            }
            pending = false;

            if (shell != null && !shell.IsBroken && !shell.IsForming)
                Heal();
            else if (shell != null && shell.IsBroken)
                ClearTint();
        }

        /// <summary>Encierra al jugador ya (también para las pruebas).</summary>
        public void Encase()
        {
            if (shell != null || left <= 0 || health == null || health.IsDead)
                return;
            left--;
            Encasements++;
            healedPulses = 0;
            controller.MovementLocked = true;
            lockedMovement = true;
            if (halo != null)
                halo.SetCharges(left);

            var art = settings.shell ?? new AmberShellArt();
            // Invulnerable mientras está dentro y los segundos de después. La formación no cuenta: el tiempo está detenido.
            health.MakeInvulnerable(settings.lockSeconds + art.BreakReleaseSeconds + settings.exitInvulnerability);

            shell = AmberShell.Create(transform, art, settings.lockSeconds, AmberShell.Anchor.Center,
                freezeWorld: art.FormSeconds > 0f, heal: settings.healArt);
            shell.Broken += OnBroken;
        }

        /// <summary>Cura medio corazón en cada pulso de la curación y tiñe de dorado a la criatura.</summary>
        void Heal()
        {
            float held = shell.HoldTime;
            while (healedPulses < settings.heal && held >= PulseTime(healedPulses))
            {
                health.Heal(1);
                healedPulses++;
            }

            var art = settings.healArt;
            if (art == null || art.tint == null || art.tint.Length == 0 || animator == null || animator.Body == null)
                return;
            int frame = Mathf.Min(art.tint.Length - 1, Mathf.FloorToInt(held / Mathf.Max(0.02f, art.frameSeconds)));
            Color color = Color.Lerp(Color.white, GoldTint, art.tint[frame]);
            // La transparencia es de quien la usa (el parpadeo tras un golpe, la sombra): solo se tiñe el color.
            var current = animator.Body.color;
            animator.Body.color = new Color(color.r, color.g, color.b, current.a);
            tinted = true;
        }

        float PulseTime(int index)
        {
            var art = settings.healArt;
            if (art != null && art.pulseFrames != null && art.pulseFrames.Length == settings.heal)
                return art.pulseFrames[index] * art.frameSeconds;
            return FallbackPulseSeconds * (index + 1);
        }

        void ClearTint()
        {
            if (!tinted)
                return;
            tinted = false;
            if (animator != null && animator.Body != null)
            {
                var current = animator.Body.color;
                animator.Body.color = new Color(1f, 1f, 1f, current.a);
            }
        }

        void OnBroken(Vector2 center)
        {
            // Si el bloque se rompe antes de acabar la curación, lo que falte se cura de golpe.
            if (healedPulses < settings.heal)
            {
                health.Heal(settings.heal - healedPulses);
                healedPulses = settings.heal;
            }
            ClearTint();
            Unlock();
            AmberShards.Burst(center, settings.shardRadius, settings.shardDamage, settings.shardPush, settings.shardStun);
        }

        void Unlock()
        {
            if (!lockedMovement || controller == null)
                return;
            lockedMovement = false;
            controller.MovementLocked = false;
        }
    }
}
