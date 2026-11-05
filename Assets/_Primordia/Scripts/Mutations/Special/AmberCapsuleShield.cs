using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="AmberCapsuleMutation"/>: si un golpe deja al jugador a medio corazón o menos (o iba a matarle),
    /// le encierra en un bloque de ámbar: invulnerable e inmóvil, se cura al empezar y, al romperse, suelta esquirlas y le da
    /// unos segundos más de invencibilidad. Las activaciones se reponen al entrar en otra sala.
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
            public AmberShellArt shell;
            public Sprite[] healFrames;
        }

        const int SortingOrder = 24;
        // A medio corazón o menos.
        const int TriggerHealth = 1;

        Settings settings;
        Health health;
        PlayerController controller;
        int left;
        bool pending;
        AmberShell shell;
        bool lockedMovement;
        Room lastRoom;

        /// <summary>Activaciones que quedan en esta sala.</summary>
        public int ActivationsLeft => left;

        /// <summary>True mientras está dentro del bloque.</summary>
        public bool IsEncased => shell != null;

        /// <summary>Veces que se ha encerrado (para las pruebas).</summary>
        public int Encasements { get; private set; }

        internal void Configure(Settings newSettings)
        {
            settings = newSettings;
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            left = settings.perRoom;
            health.Damaged += OnDamaged;
            Health.AddLethalInterceptor(Intercept);
            Room.PlayerEnteredAny += OnRoomEntered;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            Health.RemoveLethalInterceptor(Intercept);
            Room.PlayerEnteredAny -= OnRoomEntered;
            Unlock();
            if (shell != null)
                Destroy(shell.gameObject);
        }

        void OnRoomEntered(Room room)
        {
            if (room == lastRoom)
                return;
            lastRoom = room;
            left = settings.perRoom;
        }

        // Un golpe mortal con una activación a mano: no se muere; el bloque lo recoge.
        bool Intercept(Health target) => target == health && left > 0 && shell == null;

        void OnDamaged(int amount)
        {
            if (left > 0 && shell == null && !health.IsDead && health.Current <= TriggerHealth)
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
        }

        /// <summary>Encierra al jugador ya (también para las pruebas).</summary>
        public void Encase()
        {
            if (shell != null || left <= 0 || health == null || health.IsDead)
                return;
            left--;
            Encasements++;
            controller.MovementLocked = true;
            lockedMovement = true;
            // Invulnerable mientras dura el bloque y los segundos de después.
            health.MakeInvulnerable(settings.lockSeconds + settings.exitInvulnerability);
            health.Heal(settings.heal);
            if (settings.healFrames != null && settings.healFrames.Length > 0)
                FrameEffect.Play("CapsuleHeal", settings.healFrames, 0.08f, SortingOrder, transform);

            shell = AmberShell.Create(transform, settings.shell, settings.lockSeconds);
            shell.Broken += OnBroken;
        }

        void OnBroken(Vector2 center)
        {
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
