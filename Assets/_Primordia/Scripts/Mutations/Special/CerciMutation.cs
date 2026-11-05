using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cercos de cucaracha (Extremidades, especial del ámbar): si un proyectil está a punto de alcanzarte, haces un quiebro en el sitio,
    /// sin moverte, y te atraviesa sin daño. Solo proyectiles. Nivel I: recarga de 8 s, con un brillo cuando vuelve a estar listo. Nivel
    /// II: 6 s. Nivel III: 2 cargas con enfriamiento independiente y un 30 % más de velocidad durante 2 s tras cada quiebro.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Cerci", fileName = "Mutation_Cerci")]
    public class CerciMutation : MutationData
    {
        [Serializable]
        public class CerciLevel
        {
            [Tooltip("Segundos de recarga de cada carga.")]
            [Min(0.1f)] public float cooldown = 8f;
            [Tooltip("Quiebros guardados a la vez; cada uno se recarga por su cuenta.")]
            [Min(1)] public int charges = 1;
            [Tooltip("Velocidad extra (0,3 = +30 %) justo después de cada quiebro. 0 = sin ella.")]
            [Min(0f)] public float speedBonus;
            [Min(0f)] public float speedSeconds = 2f;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] CerciLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] CerciLevel[] upgrades =
        {
            new() { cooldown = 6f },
            new() { cooldown = 6f, charges = 2, speedBonus = 0.3f, speedSeconds = 2f },
        };

        [Header("Quiebro")]
        [Tooltip("Segundos de invulnerabilidad del quiebro (lo que tarda el proyectil en atravesarte).")]
        [SerializeField, Min(0.05f)] float dodgeSeconds = 0.45f;
        [Tooltip("Cuánto se oscurece la capa de los cercos mientras está gastada (1 = nada).")]
        [SerializeField, Range(0.2f, 1f)] float spentBrightness = 0.55f;

        [Header("Arte")]
        [Tooltip("Silueta fantasma del quiebro (32×32).")]
        [SerializeField] Sprite[] flashFrames = Array.Empty<Sprite>();
        [Tooltip("Brillo de recarga lista (16×16).")]
        [SerializeField] Sprite[] readyFrames = Array.Empty<Sprite>();
        [Tooltip("Nivel III: estallido de chispas en estrella en el quiebro (56×56).")]
        [SerializeField] Sprite[] burstFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.07f;

        public CerciLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new CerciLevel();
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var current = ValuesAt(level);
            player.AddBehaviour<CerciDodge>(Slot).Configure(new CerciDodge.Settings
            {
                cooldown = current.cooldown,
                charges = current.charges,
                speedBonus = current.speedBonus,
                speedSeconds = current.speedSeconds,
                dodgeSeconds = dodgeSeconds,
                spentBrightness = spentBrightness,
                flashFrames = flashFrames,
                readyFrames = readyFrames,
                burstFrames = level >= 3 ? burstFrames : Array.Empty<Sprite>(),
                frameDuration = frameDuration,
            });
        }

        internal void ConfigureLevels(CerciLevel levelOne, params CerciLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(Sprite[] flash, Sprite[] ready, Sprite[] burst)
        {
            flashFrames = flash ?? Array.Empty<Sprite>();
            readyFrames = ready ?? Array.Empty<Sprite>();
            burstFrames = burst ?? Array.Empty<Sprite>();
        }
    }
}
