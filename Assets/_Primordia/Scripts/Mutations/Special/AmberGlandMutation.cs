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
        [Tooltip("Bloque de los niveles I y II (y de los jefes): talla normal y grande (plantas).")]
        [SerializeField] AmberShellArt shell = new();
        [SerializeField] AmberShellArt shellLarge = new();
        [Tooltip("Nivel III: bloque-cadáver en ámbar antiguo, talla normal y grande.")]
        [SerializeField] AmberShellArt shellIII = new();
        [SerializeField] AmberShellArt shellIIILarge = new();
        [SerializeField] Sprite[] dropFrames = Array.Empty<Sprite>();
        [SerializeField] Sprite[] hitFrames = Array.Empty<Sprite>();
        [Tooltip("Cristal de ámbar del lomo en sus cuatro estados, por cuerpo de criatura.")]
        [SerializeField] CreatureLayer[] emptyLayers = Array.Empty<CreatureLayer>();
        [SerializeField] CreatureLayer[] halfLayers = Array.Empty<CreatureLayer>();
        [SerializeField] CreatureLayer[] fullLayers = Array.Empty<CreatureLayer>();
        [SerializeField] CreatureLayer[] chargeLayers = Array.Empty<CreatureLayer>();

        /// <summary>Estado del cristal del lomo.</summary>
        public enum CrystalState { Empty, Half, Full, Charge }

        public GlandLevel ValuesAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index] : values ?? new GlandLevel();
        }

        public override float CooldownAt(int level) => ValuesAt(level).cooldown;

        public float ChargeSeconds => chargeSeconds;

        public RuntimeAnimatorController CrystalLayerFor(CrystalState state, RuntimeAnimatorController body)
        {
            var layers = state switch
            {
                CrystalState.Empty => emptyLayers,
                CrystalState.Half => halfLayers,
                CrystalState.Charge => chargeLayers,
                _ => fullLayers,
            };
            if (body == null || layers == null)
                return null;
            foreach (var layer in layers)
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
                shell = shell,
                shellLarge = shellLarge.IsEmpty ? shell : shellLarge,
                shellIII = shellIII.IsEmpty ? shell : shellIII,
                shellIIILarge = shellIIILarge.IsEmpty ? shellIII : shellIIILarge,
                dropFrames = dropFrames,
                hitFrames = hitFrames,
            };
            AmberDropShot.Fire((Vector2)player.transform.position + aim * 0.7f, aim, settings);
            return true;
        }

        internal void ConfigureLevels(GlandLevel levelOne, params GlandLevel[] newUpgrades)
        {
            values = levelOne;
            upgrades = newUpgrades;
        }

        internal void SetArt(AmberShellArt normal, AmberShellArt large, AmberShellArt normalIII, AmberShellArt largeIII, Sprite[] drop, Sprite[] hit)
        {
            shell = normal ?? new AmberShellArt();
            shellLarge = large ?? new AmberShellArt();
            shellIII = normalIII ?? new AmberShellArt();
            shellIIILarge = largeIII ?? new AmberShellArt();
            dropFrames = drop ?? Array.Empty<Sprite>();
            hitFrames = hit ?? Array.Empty<Sprite>();
        }

        internal void SetCrystalLayers(CreatureLayer[] empty, CreatureLayer[] half, CreatureLayer[] full, CreatureLayer[] charge)
        {
            emptyLayers = empty ?? Array.Empty<CreatureLayer>();
            halfLayers = half ?? Array.Empty<CreatureLayer>();
            fullLayers = full ?? Array.Empty<CreatureLayer>();
            chargeLayers = charge ?? Array.Empty<CreatureLayer>();
        }
    }
}
