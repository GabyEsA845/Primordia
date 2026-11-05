using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Capa de una mutación sobre el cuerpo de una criatura (la Cutícula de Marrella sobre la medusa, el pez o el lirio; la
    /// Cola propulsora detrás de ellos): un sprite delante (Piel) o detrás (Extremidades) del cuerpo con sus propias animaciones, que en cada cuadro va al mismo estado y al mismo momento que
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
        int orderOffset = 1;

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

        /// <summary>Tinte que se multiplica al color del cuerpo (los Cercos gastados se oscurecen, la glándula llena se funde). Blanco = nada.</summary>
        public Color Tint { get; set; } = Color.white;

        /// <summary>Crea la capa como hija del cuerpo: delante (+1, la Piel) o detrás (-1, las Extremidades).</summary>
        internal static CreatureLayerMirror Create(Animator bodyAnimator, SpriteRenderer body, string name = "CreatureSkinLayer",
            int orderOffset = 1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, false);
            var mirror = go.AddComponent<CreatureLayerMirror>();
            mirror.orderOffset = orderOffset;
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
            spriteRenderer.sortingOrder = sourceRenderer.sortingOrder + orderOffset;
            spriteRenderer.color = sourceRenderer.color * Tint;
        }
    }
}
