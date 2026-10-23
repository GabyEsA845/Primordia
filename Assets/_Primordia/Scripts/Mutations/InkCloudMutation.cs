using System;
using Primordia.Audio;
using Primordia.Combat;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Vejiga de tinta: nube alrededor del jugador que ciega a los enemigos que alcanza. Con el efecto de
    /// <see cref="InkCloudArea"/>, la nube se queda unos segundos y ciega también a los que entran en ella.
    /// Evolución (nivel III): Tinta corrosiva, que daña cada segundo a los enemigos que están dentro.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Ink Cloud Mutation", fileName = "Mutation_")]
    public class InkCloudMutation : AbilityMutation
    {
        [Serializable]
        public class InkLevel
        {
            [Min(0.1f)] public float radius = 3f;
            [Min(0.1f)] public float cooldown = 8f;
            [Tooltip("Nube de este nivel. Vacío = la del nivel I.")]
            public GameObject effect;
            [Tooltip("Radio al que está dibujada la nube de este nivel.")]
            [Min(0.01f)] public float effectRadius = 3f;
            [Tooltip("Daño por segundo a los enemigos dentro de la nube. 0 = sin daño.")]
            [Min(0)] public int damagePerSecond;
        }

        [Tooltip("Nivel I: radio de la nube.")]
        [SerializeField, Min(0.1f)] float radius = 3f;
        [Tooltip("Segundos de ceguera; también lo que dura la nube.")]
        [SerializeField, Min(0.1f)] float blindDuration = 3f;
        [Tooltip("Niveles II y III.")]
        [SerializeField] InkLevel[] upgrades = Array.Empty<InkLevel>();
        [Tooltip("Marca de cegado sobre los enemigos.")]
        [SerializeField] GameObject blindMarker;

        public float Radius => radius;
        public float BlindDuration => blindDuration;

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

        public int DamagePerSecondAt(int level)
        {
            int index = UpgradeIndex(upgrades, level);
            return index >= 0 && upgrades[index] != null ? upgrades[index].damagePerSecond : 0;
        }

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            int index = UpgradeIndex(upgrades, level);
            var upgrade = index >= 0 ? upgrades[index] : null;
            float levelRadius = RadiusAt(level);
            Vector2 center = player.transform.position;

            var cloud = upgrade != null && upgrade.effect != null
                ? SpawnEffect(upgrade.effect, upgrade.effectRadius, center, levelRadius)
                : SpawnEffect(center, levelRadius);

            Blind(cloud, center, levelRadius, DamagePerSecondAt(level));
            Sfx.Play(Sounds.Ink);
        }

        /// <summary>
        /// Una nube de tinta del nivel I de ese radio, que ciega igual que la de la habilidad (la Bocanada al golpe
        /// del Cuesco de lobo).
        /// </summary>
        public void SpawnCloud(Vector2 center, float cloudRadius)
        {
            Blind(SpawnEffect(center, cloudRadius), center, cloudRadius, damagePerSecond: 0);
            Sfx.Play(Sounds.LineagePuffOnHit);
        }

        void Blind(GameObject cloud, Vector2 center, float cloudRadius, int damagePerSecond)
        {
            if (cloud != null && cloud.TryGetComponent<InkCloudArea>(out var area))
            {
                area.Configure(cloudRadius, blindDuration, damagePerSecond, blindMarker);
                return;
            }

            // Sin nube que dure (efecto provisional): ciega al momento a los que alcanza.
            foreach (var hit in Physics2D.OverlapCircleAll(center, cloudRadius, LayerMask.GetMask(CombatLayers.Enemy)))
            {
                var body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent<EnemyAI>(out var enemy))
                    enemy.Blind(blindDuration, blindMarker);
            }
        }

        internal void Configure(float newRadius, float newBlindDuration)
        {
            radius = newRadius;
            blindDuration = newBlindDuration;
        }

        internal void ConfigureUpgrades(params InkLevel[] newUpgrades) => upgrades = newUpgrades;

        internal void SetBlindMarker(GameObject marker) => blindMarker = marker;
    }
}
