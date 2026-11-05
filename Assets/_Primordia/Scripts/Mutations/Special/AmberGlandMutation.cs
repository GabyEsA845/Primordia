using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Gota de ámbar (Órgano especial del ámbar): mantener el clic derecho carga y, al soltar, lanzas una gota que encierra a un
    /// enemigo. Dentro del bloque está aturdido, hace de escudo y recibe daño; al romperse suelta esquirlas que dañan alrededor.
    /// Nivel I: 5 s (jefes 1,5 s), recarga de 20 s. Nivel II: 8 s (jefes 2 s), recarga de 18 s. Nivel III: recarga de 15 s y el enemigo
    /// muere al instante, quedando su cadáver en ámbar como escudo que se puede romper; a los jefes solo 2 s de aturdimiento.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Special/Amber Gland", fileName = "Mutation_AmberGland")]
    public class AmberGlandMutation : AbilityMutation
    {
        [Serializable]
        public class GlandLevel
        {
            [Tooltip("Recarga en segundos, desde que lanzas la gota.")]
            [Min(0.1f)] public float cooldown = 20f;
            [Tooltip("Segundos encerrado un enemigo normal.")]
            [Min(0.1f)] public float stunSeconds = 5f;
            [Tooltip("Segundos encerrado un jefe.")]
            [Min(0.1f)] public float bossSeconds = 1.5f;
            [Tooltip("Evolución: el enemigo muere al instante y su cadáver queda en ámbar de escudo.")]
            public bool kills;
        }

        [Tooltip("Nivel I.")]
        [SerializeField] GlandLevel values = new();
        [Tooltip("Niveles II y III.")]
        [SerializeField] GlandLevel[] upgrades =
        {
            new() { cooldown = 18f, stunSeconds = 8f, bossSeconds = 2f },
            new() { cooldown = 15f, stunSeconds = 8f, bossSeconds = 2f, kills = true },
        };

        [Header("Carga y gota")]
        [Tooltip("Segundos que hay que mantener el clic para que la gota salga; antes de eso, soltar no hace nada.")]
        [SerializeField, Min(0.05f)] float chargeSeconds = 0.5f;
        [SerializeField, Min(1f)] float dropSpeed = 15f;
        [SerializeField, Min(1f)] float dropRange = 12f;
        [Header("Dentro del bloque")]
        [Tooltip("Daño por segundo al enemigo encerrado (1 = medio corazón de los suyos... un punto de vida).")]
        [SerializeField, Min(0)] int damagePerSecond = 2;
        [Header("Esquirlas")]
        [SerializeField, Min(0f)] float shardRadius = 2.2f;
        [SerializeField, Min(0)] int shardDamage = 4;
        [SerializeField, Min(0f)] float shardPush = 5f;
        [Tooltip("Vida del cadáver en ámbar del nivel III (lo que aguanta antes de romperse) y segundos que dura.")]
        [SerializeField, Min(1)] int corpseHealth = 6;
        [SerializeField, Min(1f)] float corpseSeconds = 10f;

        [Header("Arte")]
        [SerializeField] AmberShellArt shell = new();
        [Tooltip("Nivel III: bloque más cristalino que se cierra con cristales del suelo.")]
        [SerializeField] AmberShellArt shellIII = new();
        [SerializeField] Sprite[] dropFrames = Array.Empty<Sprite>();
        [SerializeField] Sprite[] hitFrames = Array.Empty<Sprite>();
        [Tooltip("Estrellas de aturdido sobre el enemigo (niveles I y II, 32×16).")]
        [SerializeField] Sprite[] starFrames = Array.Empty<Sprite>();
        [Tooltip("Nivel III: cristales que brotan del suelo y cierran el bloque.")]
        [SerializeField] Sprite[] crystalFrames = Array.Empty<Sprite>();
        [Tooltip("Nivel III: silueta fósil del cadáver por delante.")]
        [SerializeField] Sprite[] fossilFrames = Array.Empty<Sprite>();
        [Tooltip("Capas del cuerpo con la glándula llena, por cuerpo de criatura (la vacía va en la ranura normal).")]
        [SerializeField] CreatureLayer[] fullLayers = Array.Empty<CreatureLayer>();

        public GlandLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new GlandLevel();
        }

        public override float CooldownAt(int level) => ValuesAt(level).cooldown;

        public float ChargeSeconds => chargeSeconds;

        public RuntimeAnimatorController FullLayerFor(RuntimeAnimatorController body)
        {
            if (body == null || fullLayers == null)
                return null;
            foreach (var layer in fullLayers)
            {
                if (layer.body == body)
                    return layer.layer;
            }
            return null;
        }

        public override bool HoldToCharge => true;

        public override void Apply(PlayerMutations player, int level)
        {
            base.Apply(player, level);
            player.AddBehaviour<AmberGlandGlow>(Slot).Configure(this, player.Ability);
        }

        public override void Activate(PlayerMutations player)
        {
            var glow = player.GetComponent<AmberGlandGlow>();
            if (glow != null)
                glow.BeginCharge();
        }

        public override bool Release(PlayerMutations player)
        {
            var glow = player.GetComponent<AmberGlandGlow>();
            if (glow == null || !glow.EndCharge(chargeSeconds))
                return false;

            int level = Mathf.Max(1, player.LevelOf(Slot));
            var current = ValuesAt(level);
            Vector2 aim = player.Controller != null ? player.Controller.AimDirection : Vector2.right;
            if (aim.sqrMagnitude < 0.0001f)
                aim = Vector2.right;
            aim.Normalize();

            var settings = new AmberDropShot.Settings
            {
                speed = dropSpeed,
                range = dropRange,
                stunSeconds = current.stunSeconds,
                bossSeconds = current.bossSeconds,
                kills = current.kills,
                damagePerSecond = damagePerSecond,
                shardRadius = shardRadius,
                shardDamage = shardDamage,
                shardPush = shardPush,
                corpseHealth = corpseHealth,
                corpseSeconds = corpseSeconds,
                shell = level >= 3 && !shellIII.IsEmpty ? shellIII : shell,
                dropFrames = dropFrames,
                hitFrames = hitFrames,
                starFrames = level >= 3 ? Array.Empty<Sprite>() : starFrames,
                crystalFrames = level >= 3 ? crystalFrames : Array.Empty<Sprite>(),
                fossilFrames = level >= 3 ? fossilFrames : Array.Empty<Sprite>(),
            };
            AmberDropShot.Fire((Vector2)player.transform.position + aim * 0.7f, aim, settings);
            return true;
        }

        internal void ConfigureLevels(GlandLevel levelOne, params GlandLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(AmberShellArt newShell, AmberShellArt newShellIII, Sprite[] drop, Sprite[] hit, Sprite[] stars, Sprite[] crystals,
            Sprite[] fossil)
        {
            shell = newShell ?? new AmberShellArt();
            shellIII = newShellIII ?? new AmberShellArt();
            dropFrames = drop ?? Array.Empty<Sprite>();
            hitFrames = hit ?? Array.Empty<Sprite>();
            starFrames = stars ?? Array.Empty<Sprite>();
            crystalFrames = crystals ?? Array.Empty<Sprite>();
            fossilFrames = fossil ?? Array.Empty<Sprite>();
        }

        internal void SetFullLayers(params CreatureLayer[] layers) => fullLayers = layers ?? Array.Empty<CreatureLayer>();
    }
}
