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

        /// <summary>Crea el efecto visual en esa posición, con el diámetro del área.</summary>
        protected void SpawnEffect(Vector2 position, float radius)
        {
            if (effectPrefab == null)
                return;

            var effect = Instantiate(effectPrefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * radius;
        }

        internal void ConfigureAbility(float newCooldown, GameObject newEffect)
        {
            cooldown = Mathf.Max(0.1f, newCooldown);
            effectPrefab = newEffect;
        }
    }
}
