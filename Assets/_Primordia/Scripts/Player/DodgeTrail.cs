using Primordia.Core;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Mientras el jugador esquiva, deja siluetas translúcidas detrás cada pocos centésimos de segundo.
    /// Cada silueta es un <see cref="OneShotEffect"/> que se desvanece sola.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class DodgeTrail : MonoBehaviour
    {
        [SerializeField] GameObject trailPrefab;
        [Tooltip("Segundos entre una silueta y la siguiente.")]
        [SerializeField, Min(0.01f)] float interval = 0.05f;

        PlayerController controller;
        float nextTrail;

        void Awake() => controller = GetComponent<PlayerController>();

        void OnEnable() => controller.DodgeStarted += OnDodgeStarted;

        void OnDisable() => controller.DodgeStarted -= OnDodgeStarted;

        internal void Configure(GameObject newTrailPrefab, float newInterval = 0.05f)
        {
            trailPrefab = newTrailPrefab;
            interval = Mathf.Max(0.01f, newInterval);
        }

        // La primera silueta sale en el mismo instante en que empieza la esquiva.
        void OnDodgeStarted() => nextTrail = Time.time;

        void Update()
        {
            if (!controller.IsDodging || Time.time < nextTrail)
                return;

            OneShotEffect.Spawn(trailPrefab, transform.position);
            nextTrail = Time.time + interval;
        }
    }
}
