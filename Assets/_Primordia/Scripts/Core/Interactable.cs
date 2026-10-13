using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Algo con lo que el jugador interactúa pulsando Interactuar (E) cuando está cerca: un artículo de la tienda,
    /// y lo que venga después. El PlayerInteractor del jugador elige el más cercano a su alcance,
    /// lo marca con <see cref="SetFocused"/> y llama a <see cref="Interact"/> al pulsar.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Distancia al jugador desde la que se puede usar.")]
        [SerializeField, Min(0.1f)] float radius = 1.25f;

        static readonly List<Interactable> active = new();

        /// <summary>Los que están activos en la escena.</summary>
        public static IReadOnlyList<Interactable> Active => active;

        public float Radius => radius;
        public bool IsFocused { get; private set; }

        /// <summary>Punto desde el que se mide la distancia al jugador.</summary>
        public virtual Vector2 Position => transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        protected virtual void OnEnable() => active.Add(this);

        protected virtual void OnDisable()
        {
            active.Remove(this);
            if (IsFocused)
                SetFocused(false, null);
        }

        internal void SetRadius(float newRadius) => radius = Mathf.Max(0.1f, newRadius);

        /// <summary>False si ahora no se puede elegir (por ejemplo, un artículo ya vendido).</summary>
        public virtual bool IsAvailable => true;

        /// <summary>El jugador lo tiene delante (true) o se ha alejado (false).</summary>
        public void SetFocused(bool focused, GameObject player)
        {
            if (IsFocused == focused)
                return;

            IsFocused = focused;
            OnFocusChanged(focused, player);
        }

        protected virtual void OnFocusChanged(bool focused, GameObject player) { }

        /// <summary>El jugador ha pulsado Interactuar teniéndolo delante.</summary>
        public abstract void Interact(GameObject player);
    }
}
