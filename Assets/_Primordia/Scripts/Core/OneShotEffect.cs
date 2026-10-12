using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Efecto visual de un solo uso (impacto, burbujas, estela…): su Animator reproduce la animación
    /// una vez y el objeto se destruye al terminar.
    /// </summary>
    public class OneShotEffect : MonoBehaviour
    {
        [Tooltip("Segundos hasta destruirse; lo que dura su animación.")]
        [SerializeField, Min(0.01f)] float duration = 0.5f;

        public float Duration => duration;

        void Start() => Destroy(gameObject, duration);

        /// <summary>Crea el efecto en esa posición. No hace nada si no hay prefab.</summary>
        public static GameObject Spawn(GameObject prefab, Vector2 position, float angle = 0f, Transform parent = null)
        {
            if (prefab == null)
                return null;

            var effect = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, angle));
            if (parent != null)
                effect.transform.SetParent(parent, worldPositionStays: true);
            return effect;
        }

        internal void SetDuration(float newDuration) => duration = Mathf.Max(0.01f, newDuration);
    }
}
