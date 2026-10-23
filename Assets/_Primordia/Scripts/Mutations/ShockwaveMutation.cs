using System;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Glándula de choque: onda que destruye los proyectiles enemigos cercanos y empuja a los enemigos.
    /// Evolución (nivel III): Paralizante, que deja paralizados unos segundos a los enemigos que alcanza.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Shockwave Mutation", fileName = "Mutation_")]
    public class ShockwaveMutation : AbilityMutation
    {
        [Serializable]
        public class ShockLevel
        {
            [Min(0.1f)] public float radius = 4f;
            [Min(0.1f)] public float cooldown = 6f;
            [Tooltip("Onda de este nivel. Vacío = la del nivel I.")]
            public GameObject effect;
            [Tooltip("Radio al que está dibujada la onda de este nivel.")]
            [Min(0.01f)] public float effectRadius = 4f;
        }

        [Tooltip("Nivel I: radio de la onda.")]
        [SerializeField, Min(0.1f)] float radius = 4f;
        [Tooltip("Velocidad inicial del empujón a los enemigos.")]
        [SerializeField, Min(0f)] float pushSpeed = 14f;
        [SerializeField, Min(0f)] float stunDuration = 0.4f;
        [Tooltip("Niveles II y III.")]
        [SerializeField] ShockLevel[] upgrades = Array.Empty<ShockLevel>();

        [Header("Evolución (nivel III): Paralizante")]
        [Tooltip("Segundos de parálisis. 0 = sin parálisis.")]
        [SerializeField, Min(0f)] float paralyzeDuration;
        [Tooltip("Marca de paralizado sobre los enemigos.")]
        [SerializeField] GameObject paralyzedMarker;
        [Tooltip("Rayos que recorren el cuerpo del enemigo paralizado.")]
        [SerializeField] GameObject paralyzedBodyEffect;

        public float Radius => radius;

        public float RadiusAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index].radius : radius;
        }

        public override float CooldownAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index].cooldown : Cooldown;
        }

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            int index = UpgradeIndex(upgrades, level);
            var upgrade = index >= 0 ? upgrades[index] : null;
            float levelRadius = RadiusAt(level);
            bool paralyzes = level >= MaxLevel && paralyzeDuration > 0f;
            Vector2 center = player.transform.position;

            if (upgrade != null && upgrade.effect != null)
                SpawnEffect(upgrade.effect, upgrade.effectRadius, center, levelRadius);
            else
                SpawnEffect(center, levelRadius);

            Sfx.Play(Sounds.Shockwave);
            int mask = LayerMask.GetMask(CombatLayers.Enemy, CombatLayers.EnemyProjectile);
            foreach (var hit in Physics2D.OverlapCircleAll(center, levelRadius, mask))
            {
                if (hit.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Despawn();
                    Sfx.Play(Sounds.ShockwaveProjectileBreak);
                    continue;
                }

                var body = hit.attachedRigidbody;
                if (body == null || !body.TryGetComponent<EnemyAI>(out var enemy))
                    continue;

                Vector2 away = (body.position - center).normalized;
                enemy.Knockback(away * pushSpeed, stunDuration);
                if (paralyzes)
                    enemy.Paralyze(paralyzeDuration, paralyzedMarker, paralyzedBodyEffect);
            }
        }

        internal void Configure(float newRadius, float newPushSpeed, float newStunDuration)
        {
            radius = newRadius;
            pushSpeed = newPushSpeed;
            stunDuration = newStunDuration;
        }

        internal void ConfigureUpgrades(params ShockLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void ConfigureParalysis(float duration, GameObject marker, GameObject bodyEffect)
        {
            paralyzeDuration = Mathf.Max(0f, duration);
            paralyzedMarker = marker;
            paralyzedBodyEffect = bodyEffect;
        }
    }
}
