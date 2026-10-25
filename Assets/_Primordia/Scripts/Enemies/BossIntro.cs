using System;
using System.Collections;
using Primordia.Core;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Enemies
{
    /// <summary>
    /// Presentación del jefe al entrar en su sala, como en Enter the Gungeon (unos 3 s, siempre, se salta con E):
    /// el jugador se queda quieto e invulnerable, la cámara va al jefe, el jefe hace su pose, entra el rótulo con
    /// su nombre, su título y su retrato, y la cámara vuelve. Va en el mismo objeto que el jefe (<see cref="IBoss"/>)
    /// y empieza sola cuando la sala lo activa.
    /// </summary>
    public class BossIntro : MonoBehaviour
    {
        [SerializeField] BossIntroData data;
        [Tooltip("Segundos que tarda la cámara en ir al jefe (y en volver).")]
        [SerializeField, Min(0.05f)] float panTime = 0.6f;
        [Tooltip("Segundo en que entra el rótulo, contando desde que empieza la presentación.")]
        [SerializeField, Min(0f)] float bannerAt = 1f;
        [Tooltip("Segundo en que se va el rótulo.")]
        [SerializeField, Min(0f)] float bannerUntil = 3.2f;
        [Tooltip("Texto del aviso de abajo a la derecha.")]
        [SerializeField] string skipText = "[E] Saltar";

        /// <summary>Se lanza al acabar la presentación (vista entera o saltada).</summary>
        public event Action Finished;

        public BossIntroData Data => data;

        /// <summary>True mientras dura la presentación.</summary>
        public bool Playing { get; private set; }

        /// <summary>True si ya se ha visto (o saltado) en esta sala.</summary>
        public bool Played { get; private set; }

        /// <summary>True si la última se saltó con Interactuar.</summary>
        public bool Skipped { get; private set; }

        /// <summary>Duración entera, si no se salta.</summary>
        public float Length => bannerUntil + BossIntroBanner.OutTime + panTime;

        IBoss boss;
        CameraLookAhead cameraTarget;
        CinematicOverlay overlay;
        BossIntroBanner banner;
        InputAction interactAction;
        Coroutine running;
        internal bool skipPressed;

        internal void Configure(BossIntroData newData, float newPanTime = 0.6f, float newBannerAt = 1f, float newBannerUntil = 3.2f)
        {
            data = newData;
            panTime = newPanTime;
            bannerAt = newBannerAt;
            bannerUntil = newBannerUntil;
        }

        void Awake() => boss = GetComponent<IBoss>();

        void OnEnable()
        {
            if (!Played && data != null && boss != null)
                running = StartCoroutine(Run());
        }

        void OnDisable()
        {
            // Si se va a mitad (otra escena, reinicio, el jefe muere de alguna forma), todo vuelve a la normalidad.
            if (running != null)
                StopCoroutine(running);
            running = null;
            if (Playing)
                Release(instant: true);
        }

        IEnumerator Run()
        {
            // Un cuadro de margen: así el jefe ya ha hecho su OnEnable (la sala activa todo a la vez).
            yield return null;
            var manager = GameManager.Instance;
            if (manager == null)
                yield break;
            while (manager.State != GameState.Playing)
                yield return null;

            Played = true;
            Playing = true;
            Skipped = false;
            skipPressed = false;
            manager.SetState(GameState.Cinematic);

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && player.TryGetComponent<Health>(out var playerHealth))
                playerHealth.MakeInvulnerable(Length + 0.5f);

            cameraTarget = FindAnyObjectByType<CameraLookAhead>();
            overlay = FindAnyObjectByType<CinematicOverlay>();
            if (overlay != null)
            {
                overlay.SetSkipHint(true, skipText);
                banner = BossIntroBanner.For(overlay.transform);
            }
            interactAction ??= InputSystem.actions?.FindAction("Player/Interact");

            Vector2 focus = FocusPoint();
            bool posed = false, shown = false, hidden = false;
            float poseAt = panTime * 0.7f;
            for (float t = 0f; t < bannerUntil + BossIntroBanner.OutTime; t += Time.deltaTime)
            {
                if (t > 0.2f && SkipRequested())
                {
                    Skipped = true;
                    break;
                }
                if (cameraTarget != null)
                    cameraTarget.SetFocus(focus, Mathf.SmoothStep(0f, 1f, t / panTime));
                if (!posed && t >= poseAt)
                {
                    posed = true;
                    boss.PlayIntroPose(bannerUntil - poseAt);
                }
                if (!shown && t >= bannerAt)
                {
                    shown = true;
                    if (banner != null)
                        banner.Show(data);
                }
                if (!hidden && t >= bannerUntil)
                {
                    hidden = true;
                    if (banner != null)
                        banner.Hide();
                }
                yield return null;
            }

            if (Skipped && banner != null)
                banner.Hide(fast: true);
            if (overlay != null)
                overlay.SetSkipHint(false);

            // La cámara vuelve al jugador (más deprisa si se ha saltado).
            float back = Skipped ? panTime * 0.4f : panTime;
            for (float t = 0f; t < back; t += Time.deltaTime)
            {
                if (cameraTarget != null)
                    cameraTarget.SetFocus(focus, 1f - Mathf.SmoothStep(0f, 1f, t / back));
                yield return null;
            }

            running = null;
            Release(instant: false);
        }

        bool SkipRequested() =>
            skipPressed || (interactAction != null && interactAction.WasPressedThisFrame());

        /// <summary>Donde mira la cámara: el cuerpo del jefe (el de la ameba no está en su raíz).</summary>
        Vector2 FocusPoint() =>
            boss is AmoebaBoss amoeba && amoeba.MainBody != null ? amoeba.MainBody.transform.position : transform.position;

        void Release(bool instant)
        {
            Playing = false;
            if (cameraTarget != null)
                cameraTarget.ClearFocus();
            if (overlay != null)
                overlay.SetSkipHint(false);
            if (banner != null)
            {
                if (instant)
                    banner.HideImmediate();
                else
                    banner.Hide(fast: true);
            }
            var manager = GameManager.Instance;
            if (manager != null && manager.State == GameState.Cinematic)
                manager.SetState(GameState.Playing);
            boss?.BeginFight();
            Finished?.Invoke();
        }
    }
}
