using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Mutación de Órgano especial: una habilidad con recarga que se usa con el clic derecho.
    /// Cada habilidad concreta hereda de aquí e implementa <see cref="Activate"/>; los niveles pueden cambiar la
    /// recarga (<see cref="CooldownAt"/>) y el efecto.
    /// </summary>
    public abstract class AbilityMutation : MutationData
    {
        [Tooltip("Nivel I: segundos de recarga después de usarla.")]
        [SerializeField, Min(0.1f)] float cooldown = 6f;
        [Tooltip("Nivel I: efecto visual que aparece al usarla. Se escala con el radio.")]
        [SerializeField] GameObject effectPrefab;
        [Tooltip("Radio, en unidades, con el que está dibujado el efecto. Se escala por radio / este valor, " +
                 "así un sprite dibujado a su tamaño real queda a escala 1. El círculo provisional mide 1.")]
        [SerializeField, Min(0.01f)] float effectRadius = 1f;

        public float Cooldown => cooldown;
        public GameObject EffectPrefab => effectPrefab;

        /// <summary>Recarga en ese nivel; por defecto, la del nivel I.</summary>
        public virtual float CooldownAt(int level) => cooldown;

        /// <summary>
        /// Enemigos que hay que matar para recargarla en ese nivel (la Vacuola digestiva). 0 = se recarga con el
        /// tiempo (<see cref="CooldownAt"/>).
        /// </summary>
        public virtual int KillsToChargeAt(int level) => 0;

        /// <summary>False si ahora no serviría de nada (curar con la vida llena): no se gasta.</summary>
        public virtual bool CanActivate(PlayerMutations player) => true;

        public override void Apply(PlayerMutations player, int level)
        {
            if (player.Ability != null)
                player.Ability.SetAbility(this);
        }

        public override void Remove(PlayerMutations player, int level)
        {
            if (player.Ability != null && player.Ability.Ability == this)
                player.Ability.SetAbility(null);
        }

        public abstract void Activate(PlayerMutations player);

        /// <summary>Crea el efecto visual del nivel I en esa posición, del tamaño del área.</summary>
        protected GameObject SpawnEffect(Vector2 position, float radius) => SpawnEffect(effectPrefab, effectRadius, position, radius);

        /// <summary>Crea ese efecto (dibujado a <paramref name="drawnRadius"/>) escalado al radio del área.</summary>
        protected static GameObject SpawnEffect(GameObject prefab, float drawnRadius, Vector2 position, float radius)
        {
            if (prefab == null)
                return null;

            var effect = Instantiate(prefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * (radius / Mathf.Max(0.01f, drawnRadius));
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
