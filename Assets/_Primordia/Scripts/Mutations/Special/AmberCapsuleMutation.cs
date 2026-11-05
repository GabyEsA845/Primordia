using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cápsula de ámbar (Piel, especial del ámbar): un golpe que te deja a medio corazón o menos te encierra en ámbar 2 s, invulnerable
    /// e inmóvil; al encerrarte te cura medio corazón, y al romperse el bloque suelta esquirlas que dañan y empujan, y tienes 2 s de
    /// invencibilidad al salir. Una vez por sala. Nivel II: en total cura 1 corazón y las esquirlas aturden 1 s (no a jefes). Nivel
    /// III: 2 activaciones por sala y cura 1 corazón y medio, con el bloque de doble cáscara y corazoncitos que suben.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Amber Capsule", fileName = "Mutation_AmberCapsule")]
    public class AmberCapsuleMutation : MutationData
    {
        [Serializable]
        public class CapsuleLevel
        {
            [Tooltip("Vida que cura al encerrarte (1 = medio corazón).")]
            [Min(0)] public int heal = 1;
            [Tooltip("Activaciones por sala.")]
            [Min(1)] public int perRoom = 1;
            [Tooltip("Segundos que aturden las esquirlas a los enemigos (no a jefes). 0 = no aturden.")]
            [Min(0f)] public float shardStun;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] CapsuleLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] CapsuleLevel[] upgrades =
        {
            new() { heal = 2, shardStun = 1f },
            new() { heal = 3, perRoom = 2, shardStun = 1f },
        };

        [Header("Bloque")]
        [SerializeField, Min(0.2f)] float lockSeconds = 2f;
        [Tooltip("Segundos de invencibilidad después de salir del bloque.")]
        [SerializeField, Min(0f)] float exitInvulnerability = 2f;
        [Header("Esquirlas")]
        [SerializeField, Min(0f)] float shardRadius = 2.4f;
        [SerializeField, Min(0)] int shardDamage = 4;
        [SerializeField, Min(0f)] float shardPush = 7f;

        [Header("Arte")]
        [SerializeField] AmberShellArt shell = new();
        [Tooltip("Nivel III: bloque de doble cáscara.")]
        [SerializeField] AmberShellArt shellIII = new();
        [Tooltip("Nivel III: pulsos cálidos y medios corazones que suben (48×56).")]
        [SerializeField] Sprite[] healFrames = Array.Empty<Sprite>();

        public CapsuleLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new CapsuleLevel();
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var current = ValuesAt(level);
            player.AddBehaviour<AmberCapsuleShield>(Slot).Configure(new AmberCapsuleShield.Settings
            {
                heal = current.heal,
                perRoom = current.perRoom,
                shardStun = current.shardStun,
                lockSeconds = lockSeconds,
                exitInvulnerability = exitInvulnerability,
                shardRadius = shardRadius,
                shardDamage = shardDamage,
                shardPush = shardPush,
                shell = level >= 3 && !shellIII.IsEmpty ? shellIII : shell,
                healFrames = level >= 3 ? healFrames : Array.Empty<Sprite>(),
            });
        }

        internal void ConfigureLevels(CapsuleLevel levelOne, params CapsuleLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(AmberShellArt newShell, AmberShellArt newShellIII, Sprite[] newHeal)
        {
            shell = newShell ?? new AmberShellArt();
            shellIII = newShellIII ?? new AmberShellArt();
            healFrames = newHeal ?? Array.Empty<Sprite>();
        }
    }
}
