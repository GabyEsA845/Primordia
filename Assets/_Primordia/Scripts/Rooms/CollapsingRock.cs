using System.Collections;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Roca de cobertura que se derrumba (marca R del plano, en la sala del Euriptérido): bloquea como las demás hasta que
    /// el jefe muda; entonces tiembla, se parte y quedan unos escombros por los que se pasa y se dispara. Solo visual: no
    /// hace daño.
    /// </summary>
    public class CollapsingRock : MonoBehaviour
    {
        public enum RockState { Standing, Collapsing, Collapsed }

        [SerializeField] SpriteRenderer body;
        [Tooltip("Contorno de color de la cobertura: se apaga al derrumbarse.")]
        [SerializeField] GameObject outline;
        [SerializeField] Collider2D blocker;
        [Tooltip("Cuadros del derrumbe; el último ya son los escombros.")]
        [SerializeField] Sprite[] collapseFrames = System.Array.Empty<Sprite>();
        [Tooltip("Escombros que se quedan en el suelo (sin colisión).")]
        [SerializeField] Sprite rubble;
        [SerializeField, Min(0.01f)] float frameDuration = 0.09f;
        [Tooltip("Cuadro en el que la roca se parte y deja de bloquear.")]
        [SerializeField, Min(0)] int breakFrame = 3;

        public RockState State { get; private set; } = RockState.Standing;
        public bool IsCollapsed => State == RockState.Collapsed;
        /// <summary>True mientras para al jugador y a los proyectiles.</summary>
        public bool Blocks => blocker != null && blocker.enabled;
        /// <summary>Lo que tarda el derrumbe entero (segundos).</summary>
        public float Duration => collapseFrames.Length * frameDuration;

        internal void Configure(SpriteRenderer newBody, GameObject newOutline, Collider2D newBlocker, Sprite[] frames, Sprite newRubble)
        {
            body = newBody;
            outline = newOutline;
            blocker = newBlocker;
            collapseFrames = frames ?? System.Array.Empty<Sprite>();
            rubble = newRubble;
        }

        /// <summary>Empieza el derrumbe. No hace nada si ya se derrumbó o se está derrumbando.</summary>
        public void Collapse()
        {
            if (State != RockState.Standing)
                return;
            State = RockState.Collapsing;
            if (outline != null)
                outline.SetActive(false);
            if (collapseFrames.Length == 0 || !isActiveAndEnabled)
            {
                Finish();
                return;
            }
            StartCoroutine(Play());
        }

        /// <summary>Derrumba todas las rocas que cuelgan de <paramref name="room"/>; devuelve cuántas empezaron a caer.</summary>
        public static int CollapseAll(Component room)
        {
            if (room == null)
                return 0;
            int count = 0;
            foreach (var rock in room.GetComponentsInChildren<CollapsingRock>())
            {
                if (rock.State != RockState.Standing)
                    continue;
                rock.Collapse();
                count++;
            }
            return count;
        }

        IEnumerator Play()
        {
            for (int i = 0; i < collapseFrames.Length; i++)
            {
                if (body != null)
                    body.sprite = collapseFrames[i];
                if (i >= breakFrame && blocker != null)
                    blocker.enabled = false;
                yield return new WaitForSeconds(frameDuration);
            }
            Finish();
        }

        void Finish()
        {
            if (blocker != null)
                blocker.enabled = false;
            if (body != null && rubble != null)
                body.sprite = rubble;
            State = RockState.Collapsed;
        }
    }
}
