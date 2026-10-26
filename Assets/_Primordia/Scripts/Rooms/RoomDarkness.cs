using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Penumbra de la sala del Anomalocaris (la que tiene plantas luminosas): al entrar el jugador, mientras se presenta el
    /// jefe, la luz global baja y el jugador lleva un halo tenue; las plantas dan casi todo el brillo. Al despejar la sala,
    /// la luz vuelve. También apaga y enciende las plantas (escena de los ojos).
    /// </summary>
    public class RoomDarkness : MonoBehaviour
    {
        [Tooltip("Intensidad de la luz global en penumbra (1 = normal): se ve, pero poco.")]
        [SerializeField, Range(0f, 1f)] float darkIntensity = 0.12f;
        [Tooltip("Color de la luz global en penumbra: azul de fondo marino.")]
        [SerializeField] Color darkColor = new(0.55f, 0.68f, 1f);
        [Tooltip("Segundos que tarda en bajar la luz al entrar (lo que dura más o menos la presentación del jefe).")]
        [SerializeField, Min(0.01f)] float dimTime = 1.8f;
        [Tooltip("Segundos que tarda en volver la luz al despejar la sala.")]
        [SerializeField, Min(0.01f)] float restoreTime = 2.5f;
        [Tooltip("Halo del jugador: radio (unidades) e intensidad.")]
        [SerializeField, Min(0.1f)] float haloRadius = 2.5f;
        [SerializeField, Min(0f)] float haloIntensity = 0.7f;
        [SerializeField] Color haloColor = new(1f, 0.93f, 0.8f);

        Room room;
        readonly List<LightPlant> plants = new();
        Light2D globalLight;
        Light2D halo;
        float normalIntensity = 1f;
        Color normalColor = Color.white;
        Coroutine fading;

        /// <summary>0 con la luz normal, 1 en penumbra.</summary>
        public float Darkness { get; private set; }

        public IReadOnlyList<LightPlant> Plants => plants;

        internal void Configure(Room newRoom, IEnumerable<LightPlant> newPlants)
        {
            if (room != null)
            {
                room.Entered -= OnEntered;
                room.Cleared -= OnCleared;
            }
            room = newRoom;
            plants.Clear();
            plants.AddRange(newPlants);
            if (isActiveAndEnabled)
                Subscribe();
        }

        void OnEnable() => Subscribe();

        void Subscribe()
        {
            if (room == null)
                return;
            room.Entered -= OnEntered;
            room.Cleared -= OnCleared;
            room.Entered += OnEntered;
            room.Cleared += OnCleared;
        }

        void OnDisable()
        {
            if (room != null)
            {
                room.Entered -= OnEntered;
                room.Cleared -= OnCleared;
            }
            // Si la sala desaparece en penumbra (cambio de piso, reinicio), la luz del resto vuelve a la normalidad.
            if (fading != null)
                StopCoroutine(fading);
            fading = null;
            if (Darkness > 0f)
                Apply(0f);
            if (halo != null)
                Destroy(halo.gameObject);
        }

        void OnEntered(Room _) => Dim();

        void OnCleared(Room _) => Restore();

        /// <summary>Baja la luz y enciende el halo del jugador (al entrar en la sala).</summary>
        internal void Dim() => FadeTo(1f, dimTime);

        /// <summary>La luz vuelve a la normalidad y el halo se apaga (al despejar la sala).</summary>
        internal void Restore() => FadeTo(0f, restoreTime);

        /// <summary>Apaga (false) o enciende todas las plantas en <paramref name="duration"/> segundos.</summary>
        public void SetPlantsLit(bool lit, float duration)
        {
            foreach (var plant in plants)
            {
                if (plant != null)
                    plant.SetLit(lit, duration);
            }
        }

        void FadeTo(float goal, float time)
        {
            if (globalLight == null)
            {
                globalLight = Ambience.CreateGlobalLight(transform.parent != null ? transform.parent : transform);
                normalIntensity = globalLight.intensity;
                normalColor = globalLight.color;
            }
            if (halo == null && goal > 0f)
                halo = CreateHalo();
            if (fading != null)
                StopCoroutine(fading);
            fading = StartCoroutine(Fade(goal, time));
        }

        IEnumerator Fade(float goal, float time)
        {
            float start = Darkness;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                Apply(Mathf.Lerp(start, goal, Mathf.SmoothStep(0f, 1f, t / time)));
                yield return null;
            }
            Apply(goal);
            fading = null;
            if (goal <= 0f && halo != null)
                Destroy(halo.gameObject);
        }

        void Apply(float darkness)
        {
            Darkness = darkness;
            if (globalLight != null)
            {
                globalLight.intensity = Mathf.Lerp(normalIntensity, darkIntensity, darkness);
                globalLight.color = Color.Lerp(normalColor, darkColor, darkness);
            }
            if (halo != null)
                halo.intensity = haloIntensity * darkness;
        }

        Light2D CreateHalo()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                return null;
            var light = Ambience.CreateGlow(player.transform, player.transform.position,
                new Glow(null, haloColor, haloRadius, Vector2.one * haloIntensity, 0f), 0f);
            light.name = "DarkRoomHalo";
            light.intensity = 0f;
            return light;
        }
    }
}
