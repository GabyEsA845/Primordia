using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Reproduce estados de un Animator por su nombre y conoce cuánto dura cada uno.
    /// Pensado para los controladores que crea el menú Primordia > Art: un estado por clip,
    /// con el mismo nombre que el clip y sin transiciones, de modo que el código decide qué se ve.
    /// </summary>
    public class AnimatorStates
    {
        readonly Animator animator;
        readonly Dictionary<string, float> lengths = new();

        /// <summary>Último estado pedido. Se actualiza aunque no haya Animator (útil en pruebas).</summary>
        public string Current { get; private set; }

        public AnimatorStates(Animator animator)
        {
            this.animator = animator;
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                lengths[clip.name] = clip.length;
        }

        public bool Has(string state) => lengths.ContainsKey(state);

        /// <summary>Duración del clip en segundos; 0 si no existe.</summary>
        public float Length(string state) => lengths.TryGetValue(state, out var length) ? length : 0f;

        /// <summary>Cambia al estado. Si ya está en él, no hace nada salvo que se pida reiniciarlo.</summary>
        public void Play(string state, bool restart = false)
        {
            if (state == Current && !restart)
                return;

            if (animator != null)
            {
                // Con el objeto desactivado no se puede reproducir; se volverá a pedir en el siguiente cuadro.
                if (!animator.isActiveAndEnabled)
                    return;
                animator.Play(state, 0, 0f);
            }

            Current = state;
        }
    }
}
