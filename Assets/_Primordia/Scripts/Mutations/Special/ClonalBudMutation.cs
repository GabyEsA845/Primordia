using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Yema clonal (Órgano especial, especial de las almejas): clic derecho, brota una copia translúcida de ti durante 6 s que
    /// va sola a por los enemigos con tu Boca y la mitad de tu daño (con los efectos de la Boca, no los de tus otras
    /// mutaciones). No recibe daño, pero los enemigos la atacan como a ti (señuelo); flota sobre las fosas. La recarga de
    /// 15 s empieza cuando desaparece (también si se limpia la sala). Nivel II: 7 s y el 65 %. Nivel III, Gemación: dos copias.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Clonal Bud", fileName = "Mutation_ClonalBud")]
    public class ClonalBudMutation : AbilityMutation
    {
        [Serializable]
        public class BudLevel
        {
            [Tooltip("Segundos que dura la copia.")]
            [Min(0.5f)] public float duration = 6f;
            [Tooltip("Parte de tu daño que hace la copia.")]
            [Range(0f, 1f)] public float damageFraction = 0.5f;
            [Tooltip("Copias que brotan.")]
            [Min(1)] public int copies = 1;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] BudLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] BudLevel[] upgrades =
        {
            new() { duration = 7f, damageFraction = 0.65f },
            new() { duration = 7f, damageFraction = 0.65f, copies = 2 },
        };

        [Header("Copia")]
        [Tooltip("Distancia del jugador a la que brota.")]
        [SerializeField, Min(0f)] float sproutDistance = 0.9f;
        [Tooltip("Material de la copia: el sprite del jugador pasado a verde agua y translúcido. Sin él, se tiñe.")]
        [SerializeField] Material copyMaterial;
        [Tooltip("Brote de la copia; la copia aparece en el cuadro Appear Frame (desde 0).")]
        [SerializeField] Sprite[] sproutFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0)] int appearFrame = 5;
        [Tooltip("La copia se deshace en burbujas.")]
        [SerializeField] Sprite[] dissolveFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.06f;

        public BudLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new BudLevel();
        }

        public override bool IsRunning(PlayerMutations player) => ClonalBudCopy.CountOf(player) > 0;

        public override void Activate(PlayerMutations player)
        {
            var current = ValuesAt(Mathf.Max(1, player.LevelOf(Slot)));
            var settings = new ClonalBudCopy.Settings
            {
                duration = current.duration,
                damageFraction = current.damageFraction,
                material = copyMaterial,
                sproutFrames = sproutFrames,
                appearDelay = sproutFrames.Length > 0 ? Mathf.Min(appearFrame, sproutFrames.Length - 1) * frameDuration : 0f,
                dissolveFrames = dissolveFrames,
                frameDuration = frameDuration,
            };

            // Una a cada lado del jugador, perpendicular a donde apunta (con una sola, a un lado al azar).
            Vector2 aim = player.Controller != null ? player.Controller.AimDirection : Vector2.right;
            if (aim.sqrMagnitude < 0.0001f)
                aim = Vector2.right;
            Vector2 side = new Vector2(-aim.y, aim.x).normalized;
            if (current.copies == 1 && UnityEngine.Random.value < 0.5f)
                side = -side;
            for (int i = 0; i < current.copies; i++)
            {
                Vector2 offset = (i % 2 == 0 ? side : -side) * sproutDistance;
                ClonalBudCopy.Sprout(player, (Vector2)player.transform.position + offset, settings);
            }
        }

        internal void ConfigureLevels(BudLevel levelOne, params BudLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(Material material, Sprite[] sprout, Sprite[] dissolve)
        {
            copyMaterial = material;
            sproutFrames = sprout ?? Array.Empty<Sprite>();
            dissolveFrames = dissolve ?? Array.Empty<Sprite>();
        }
    }
}
