using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación de Órgano especial: una habilidad con recarga que se usa con el clic derecho.
    /// Cada habilidad concreta hereda de aquí e implementa <see cref="Activate"/>.
    /// </summary>
    public abstract class AbilityMutation : MutationData
    {
        [Tooltip("Segundos de recarga después de usarla.")]
        [SerializeField, Min(0.1f)] float cooldown = 6f;
        [Tooltip("Efecto visual que aparece al usarla. Se escala con el radio.")]
        [SerializeField] GameObject effectPrefab;
        [Tooltip("Radio, en unidades, con el que está dibujado el efecto. Se escala por radio / este valor, " +
                 "así un sprite dibujado a su tamaño real queda a escala 1. El círculo provisional mide 1.")]
        [SerializeField, Min(0.01f)] float effectRadius = 1f;

        public float Cooldown => cooldown;
        public GameObject EffectPrefab => effectPrefab;

        public override void Apply(PlayerMutations player)
        {
            if (player.Ability != null)
                player.Ability.SetAbility(this);
        }

        public override void Remove(PlayerMutations player)
        {
            if (player.Ability != null && player.Ability.Ability == this)
                player.Ability.SetAbility(null);
        }

        public abstract void Activate(PlayerMutations player);

        /// <summary>Crea el efecto visual en esa posición, del tamaño del área.</summary>
        protected GameObject SpawnEffect(Vector2 position, float radius)
        {
            if (effectPrefab == null)
                return null;

            var effect = Instantiate(effectPrefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * (radius / effectRadius);
            return effect;
        }

        internal void SetEffectRadius(float newEffectRadius) => effectRadius = Mathf.Max(0.01f, newEffectRadius);

        internal void ConfigureAbility(float newCooldown, GameObject newEffect)
        {
            cooldown = Mathf.Max(0.1f, newCooldown);
            effectPrefab = newEffect;
        }
    }
}
