using System;
using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cutícula de Marrella (Piel, especial de las almejas). Esquiva justa: si un ataque te alcanza mientras esquivas, tu piel
    /// lo refleja con un destello iridiscente. Un proyectil vuelve buscando a quien lo lanzó y le hace 6 de daño; un golpe
    /// cuerpo a cuerpo o de contacto aturde 1 s al atacante y le hace 3. Nivel II: 8 y 4, aturde 1,5 s. Nivel III, Prisma:
    /// cada proyectil reflejado se divide en 3 en abanico. La capa del cuerpo llega con los cuerpos del piso 2.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Marrella Cuticle", fileName = "Mutation_MarrellaCuticle")]
    public class MarrellaCuticleMutation : MutationData
    {
        [Serializable]
        public class CuticleLevel
        {
            [Tooltip("Daño del proyectil reflejado.")]
            [Min(0)] public int projectileDamage = 6;
            [Tooltip("Daño del destello al que te golpea cuerpo a cuerpo o por contacto.")]
            [Min(0)] public int contactDamage = 3;
            [Tooltip("Segundos que queda aturdido.")]
            [Min(0f)] public float stunDuration = 1f;
            [Tooltip("Proyectiles en que se divide el reflejado (Prisma). 1 = sin dividir.")]
            [Min(1)] public int prismCount = 1;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] CuticleLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] CuticleLevel[] upgrades =
        {
            new() { projectileDamage = 8, contactDamage = 4, stunDuration = 1.5f },
            new() { projectileDamage = 8, contactDamage = 4, stunDuration = 1.5f, prismCount = 3 },
        };
        [Tooltip("Apertura del abanico del Prisma, en grados.")]
        [SerializeField, Range(0f, 180f)] float prismSpread = 40f;

        [Header("Proyectil reflejado")]
        [Tooltip("Ataque del proyectil reflejado (prefab, velocidad, alcance y giro hacia quien lo lanzó). El daño es el del nivel.")]
        [SerializeField] AttackData reflectedAttack;

        [Header("Arte")]
        [Tooltip("Anillo alrededor del jugador al reflejar.")]
        [SerializeField] Sprite[] reflectFrames = Array.Empty<Sprite>();
        [Tooltip("Destello que aturde, sobre el atacante.")]
        [SerializeField] Sprite[] stunFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float frameDuration = 0.06f;
        [Tooltip("Marca de aturdido sobre la cabeza del enemigo.")]
        [SerializeField] GameObject stunMarker;

        public CuticleLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new CuticleLevel();
        }

        public override void Apply(PlayerMutations player, int level)
        {
            var current = ValuesAt(level);
            player.AddBehaviour<CuticleReflector>(Slot).Configure(player.Controller, new CuticleReflector.Settings
            {
                projectileDamage = current.projectileDamage,
                contactDamage = current.contactDamage,
                stunDuration = current.stunDuration,
                prismCount = current.prismCount,
                prismSpread = prismSpread,
                reflectedAttack = reflectedAttack,
                reflectFrames = reflectFrames,
                stunFrames = stunFrames,
                frameDuration = frameDuration,
                stunMarker = stunMarker,
            });
        }

        internal void ConfigureLevels(CuticleLevel levelOne, params CuticleLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetReflectedAttack(AttackData attack) => reflectedAttack = attack;

        internal void SetArt(Sprite[] reflect, Sprite[] stun, GameObject marker)
        {
            reflectFrames = reflect ?? Array.Empty<Sprite>();
            stunFrames = stun ?? Array.Empty<Sprite>();
            stunMarker = marker;
        }
    }
}
