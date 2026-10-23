using System;
using System.Collections;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Evolución al completar un linaje: el juego se congela (<see cref="GameState.Evolution"/>), la cámara se acerca
    /// un poco, el jugador se oculta y en su lugar se ve la animación del linaje (la célula se transforma en el
    /// animal y vuelve). Al acabar, el jugador lleva las piezas del linaje (<see cref="MutationLook.SetLineageLook"/>).
    /// Si el linaje se completa con un menú abierto (la elección de mutación), espera a volver al juego. Al perder el
    /// linaje, un pof del color de su clase y vuelven las piezas de las mutaciones.
    /// </summary>
    [RequireComponent(typeof(PlayerLineage))]
    public class LineageEvolution : MonoBehaviour
    {
        [Tooltip("Pof blanco al perder el linaje; se tiñe del color de su clase.")]
        [SerializeField] GameObject lostEffect;
        [Tooltip("Tamaño de la cámara durante la evolución (0,8 = se acerca un 20 %).")]
        [SerializeField, Range(0.3f, 1f)] float zoom = 0.8f;
        [Tooltip("Segundos que tarda la cámara en acercarse y en volver.")]
        [SerializeField, Min(0.01f)] float zoomTime = 0.35f;
        [Tooltip("Duración si el linaje no tiene animación.")]
        [SerializeField, Min(0f)] float fallbackDuration = 1f;
        [Tooltip("Colores por clase, en el orden Carnívoro, Herbívoro, Venenoso, Hongo (los del rótulo del HUD).")]
        [SerializeField] Color[] classColors =
        {
            new(0.95f, 0.45f, 0.4f), new(0.55f, 0.9f, 0.45f), new(0.8f, 0.6f, 0.95f), new(0.95f, 0.75f, 0.4f),
        };

        PlayerLineage lineage;
        MutationLook look;
        PlayerAnimator playerAnimator;
        Health health;
        LineageData pending;
        CameraZoom zoomer;

        /// <summary>True mientras se ve la animación.</summary>
        public bool IsEvolving { get; private set; }

        /// <summary>Linaje que espera a que se vuelva al juego para evolucionar.</summary>
        public LineageData Pending => pending;

        /// <summary>Se lanza al empezar la animación (el cartel del linaje sale entonces).</summary>
        public event Action<LineageData> Evolving;

        /// <summary>Se lanza al acabar, ya con las piezas del linaje puestas.</summary>
        public event Action<LineageData> Evolved;

        void Awake()
        {
            lineage = GetComponent<PlayerLineage>();
            look = GetComponent<MutationLook>();
            playerAnimator = GetComponent<PlayerAnimator>();
            health = GetComponent<Health>();
        }

        void OnEnable() => lineage.ActiveChanged += OnActiveChanged;

        void OnDisable()
        {
            lineage.ActiveChanged -= OnActiveChanged;
            if (IsEvolving)
            {
                // La corrutina se para con el componente: se deja todo como estaba.
                zoomer?.Restore();
                Finish(null);
            }
        }

        void Start()
        {
            // Un linaje completado antes de que este componente escuchara.
            if (lineage.Active != null && (look == null || look.LineageLook != lineage.Active))
                pending = lineage.Active;
        }

        internal void Configure(GameObject newLostEffect) => lostEffect = newLostEffect;

        void OnActiveChanged(LineageData old, LineageData active)
        {
            if (old != null)
            {
                if (pending == old)
                    pending = null;
                else if (look != null && look.LineageLook == old)
                {
                    // Se pierde: vuelven las piezas de las mutaciones, con un pof. Sin pof si es que el jugador se
                    // desactiva (al cerrar la escena): crear objetos entonces deja restos en la escena siguiente.
                    look.SetLineageLook(null);
                    if (!lineage.Disabling)
                        Puff(old);
                }
            }

            if (active != null)
                pending = active;
        }

        void Update()
        {
            if (pending == null || IsEvolving || health == null || health.IsDead)
                return;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                return;

            var target = pending;
            pending = null;
            StartCoroutine(Evolve(target));
        }

        IEnumerator Evolve(LineageData target)
        {
            IsEvolving = true;
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Evolution);

            SetPlayerHidden(true);
            GameObject effect = null;
            float duration = fallbackDuration;
            if (target.EvolutionEffect != null)
            {
                effect = Instantiate(target.EvolutionEffect, transform.position, Quaternion.identity, transform);
                if (effect.TryGetComponent<OneShotEffect>(out var oneShot))
                    Destroy(oneShot);
                if (effect.TryGetComponent<Animator>(out var animator))
                {
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    if (animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0)
                        duration = animator.runtimeAnimatorController.animationClips[0].length;
                }
            }

            Evolving?.Invoke(target);

            zoomer = new CameraZoom(zoom);
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // Se acerca al empezar y vuelve al acabar.
                float into = Mathf.Clamp01(t / zoomTime);
                float back = Mathf.Clamp01((duration - t) / zoomTime);
                zoomer.Set(Mathf.SmoothStep(0f, 1f, Mathf.Min(into, back)));
                yield return null;
            }
            zoomer.Restore();
            zoomer = null;

            if (effect != null)
                Destroy(effect);
            Finish(target);
        }

        void Finish(LineageData target)
        {
            IsEvolving = false;
            SetPlayerHidden(false);

            // Solo si el linaje sigue completo (no se perdió durante la animación).
            if (target != null && lineage.Active == target && look != null)
                look.SetLineageLook(target);

            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Evolution)
                GameManager.Instance.SetState(GameState.Playing);

            if (target != null)
                Evolved?.Invoke(target);
        }

        void SetPlayerHidden(bool hidden)
        {
            if (look != null)
                look.Hidden = hidden;
            if (playerAnimator != null && playerAnimator.Body != null)
                playerAnimator.Body.enabled = !hidden;
        }

        void Puff(LineageData old)
        {
            if (lostEffect == null)
                return;

            var puff = OneShotEffect.Spawn(lostEffect, transform.position, parent: transform);
            int index = (int)old.Class;
            if (puff != null && classColors != null && index < classColors.Length)
            {
                foreach (var sprite in puff.GetComponentsInChildren<SpriteRenderer>())
                    sprite.color = classColors[index];
            }
        }
    }
}
