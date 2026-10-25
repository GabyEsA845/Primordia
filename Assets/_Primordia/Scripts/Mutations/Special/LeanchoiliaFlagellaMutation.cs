using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Flagelos de Leanchoilia (Extremidades, especial de las almejas): cuando el ataque principal alcanza a un enemigo, unos
    /// flagelos aparecen junto a él y lo azotan con el daño de 1 s de la Boca y sus efectos (uno cada 3 s). Nivel II: uno
    /// cada 2,5 s y +25 % de daño. Nivel III, Latigazo en cadena: salta a los 2 enemigos más cercanos con el mismo daño.
    /// No se dibujan en el cuerpo: solo el latigazo junto al enemigo y un destello sobre el jugador al volver a estar listo.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Leanchoilia Flagella", fileName = "Mutation_LeanchoiliaFlagella")]
    public class LeanchoiliaFlagellaMutation : MutationData
    {
        [Serializable]
        public class FlagellaLevel
        {
            [Tooltip("Segundos entre dos latigazos.")]
            [Min(0.1f)] public float cooldown = 3f;
            [Tooltip("Multiplica el daño de 1 s de la Boca.")]
            [Min(0f)] public float damageMultiplier = 1f;
            [Tooltip("Enemigos más cercanos a los que salta el latigazo (evolución). 0 = sin cadena.")]
            [Min(0)] public int chainTargets;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] FlagellaLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] FlagellaLevel[] upgrades =
        {
            new() { cooldown = 2.5f, damageMultiplier = 1.25f },
            new() { cooldown = 2.5f, damageMultiplier = 1.25f, chainTargets = 2 },
        };

        [Header("Latigazo")]
        [Tooltip("Segundos desde que aparecen los flagelos hasta que entra el daño (cuadro 4 de la animación).")]
        [SerializeField, Min(0f)] float strikeDelay = 0.4f;
        [Tooltip("Si el enemigo muere antes del latigazo, va al más cercano a esta distancia.")]
        [SerializeField, Min(0f)] float retargetRadius = 2f;
        [Tooltip("Evolución: distancia máxima a la que salta la cadena.")]
        [SerializeField, Min(0f)] float chainRadius = 4f;

        [Header("Arte")]
        [Tooltip("Latigazo junto al enemigo, centrado en él (48×48, 0,1 s por cuadro).")]
        [SerializeField] Sprite[] lashFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float lashFrameDuration = 0.1f;
        [Tooltip("Destello sobre el jugador cuando el latigazo vuelve a estar listo.")]
        [SerializeField] Sprite[] readyFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float readyFrameDuration = 0.07f;

        public FlagellaLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new FlagellaLevel();
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var current = ValuesAt(level);
            player.AddBehaviour<FlagellaLash>(Slot).Configure(player.Weapon, new FlagellaLash.Settings
            {
                cooldown = current.cooldown,
                damageMultiplier = current.damageMultiplier,
                chainTargets = current.chainTargets,
                strikeDelay = strikeDelay,
                retargetRadius = retargetRadius,
                chainRadius = chainRadius,
                lashFrames = lashFrames,
                lashFrameDuration = lashFrameDuration,
                readyFrames = readyFrames,
                readyFrameDuration = readyFrameDuration,
            });
        }

        internal void ConfigureLevels(FlagellaLevel levelOne, params FlagellaLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(Sprite[] lash, Sprite[] ready)
        {
            lashFrames = lash ?? Array.Empty<Sprite>();
            readyFrames = ready ?? Array.Empty<Sprite>();
        }
    }
}
