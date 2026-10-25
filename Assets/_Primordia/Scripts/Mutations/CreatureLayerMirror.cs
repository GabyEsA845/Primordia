using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Capa de una Piel sobre el cuerpo de una criatura (la Cutícula de Marrella sobre la medusa, el pez o el lirio): un
    /// sprite delante del cuerpo con sus propias animaciones, que en cada cuadro va al mismo estado y al mismo momento que
    /// el cuerpo y se voltea con él. En los estados que la capa no tiene (esquiva, daño, muerte) no se ve.
    /// Va después de <c>PlayerAnimator</c> para copiar el estado de este cuadro.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CreatureLayerMirror : MonoBehaviour
    {
        Animator source;
        SpriteRenderer sourceRenderer;
        Animator animator;
        SpriteRenderer spriteRenderer;
        RuntimeAnimatorController controller;

        /// <summary>Animaciones de la capa; null la oculta.</summary>
        public RuntimeAnimatorController Controller
        {
            get => controller;
            set
            {
                if (controller == value)
                    return;
                controller = value;
                animator.runtimeAnimatorController = value;
            }
        }

        /// <summary>Oculta la capa (la evolución del linaje esconde las piezas).</summary>
        public bool Hidden { get; set; }

        public bool IsShown => spriteRenderer.enabled;

        /// <summary>Crea la capa como hija del cuerpo, delante de él.</summary>
        internal static CreatureLayerMirror Create(Animator bodyAnimator, SpriteRenderer body)
        {
            var go = new GameObject("CreatureSkinLayer");
            go.transform.SetParent(body.transform, false);
            var mirror = go.AddComponent<CreatureLayerMirror>();
            mirror.source = bodyAnimator;
            mirror.sourceRenderer = body;
            mirror.spriteRenderer = go.AddComponent<SpriteRenderer>();
            mirror.spriteRenderer.enabled = false;
            mirror.animator = go.AddComponent<Animator>();
            return mirror;
        }

        void LateUpdate()
        {
            bool shown = false;
            if (controller != null && !Hidden && source != null && sourceRenderer != null && sourceRenderer.enabled
                && source.runtimeAnimatorController != null)
            {
                var state = source.GetCurrentAnimatorStateInfo(0);
                if (animator.HasState(0, state.shortNameHash))
                {
                    animator.Play(state.shortNameHash, 0, state.normalizedTime);
                    animator.Update(0f);
                    shown = true;
                }
            }

            spriteRenderer.enabled = shown;
            if (!shown)
                return;
            spriteRenderer.flipX = sourceRenderer.flipX;
            spriteRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            spriteRenderer.color = sourceRenderer.color;
        }
    }
}
