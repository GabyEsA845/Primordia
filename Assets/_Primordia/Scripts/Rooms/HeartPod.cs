using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Vaina de semillas de la tienda (piso 3 en adelante): guarda los medios corazones que el jugador envía desde las salas. Su cuadro
    /// muestra cuántos hay; con E (y vida que curar) saca uno y cura medio corazón. Late al recibir un corazón.
    /// </summary>
    public class HeartPod : PromptInteractable
    {
        const float ReceiveFrameSeconds = 0.11f;
        static readonly Color PodColor = new(0.9f, 0.45f, 0.5f);

        SpriteRenderer body;
        float receiveStart = -1f;

        protected override string TooltipTitle => "Vaina de semillas";

        protected override string TooltipDetails =>
            HeartStorage.Stored == 0 ? $"Vacía (0/{HeartStorage.Capacity}). Envía aquí medios corazones con E desde las salas"
            : $"{HeartStorage.Stored}/{HeartStorage.Capacity} · E para curarte medio corazón";

        protected override Color TooltipColor => PodColor;

        /// <summary>Crea la vaina en esa posición (los pies); null si no hay arte.</summary>
        public static HeartPod Create(Transform parent, Vector2 position, Sprite[] keyPrompt)
        {
            var art = HeartStorageArt.Current;
            if (art == null || art.podStored == null || art.podStored.Length == 0)
                return null;

            var go = new GameObject("HeartPod");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -3;
            // Sólida como un pedestal: no se atraviesa.
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.4f, 0.7f);
            collider.offset = new Vector2(0f, 0.35f);
            var pod = go.AddComponent<HeartPod>();
            pod.body = renderer;
            pod.ConfigurePrompt(keyPrompt, 3.4f);
            pod.SetRadius(1.8f);
            pod.Refresh();
            return pod;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            HeartStorage.Changed += OnChanged;
        }

        protected override void OnDisable()
        {
            HeartStorage.Changed -= OnChanged;
            base.OnDisable();
        }

        void OnChanged()
        {
            receiveStart = Time.time;
            Refresh();
        }

        void Update()
        {
            var art = HeartStorageArt.Current;
            if (art == null || receiveStart < 0f || art.podReceive.Length == 0)
                return;
            int frame = Mathf.FloorToInt((Time.time - receiveStart) / ReceiveFrameSeconds);
            if (frame >= art.podReceive.Length)
            {
                receiveStart = -1f;
                Refresh();
                return;
            }
            body.sprite = art.podReceive[frame];
        }

        void Refresh()
        {
            var art = HeartStorageArt.Current;
            if (art == null || body == null)
                return;
            body.sprite = art.podStored[Mathf.Clamp(HeartStorage.Stored, 0, art.podStored.Length - 1)];
        }

        public override void Interact(GameObject player)
        {
            if (HeartStorage.Stored <= 0 || player == null || !player.TryGetComponent<Health>(out var health))
                return;
            // Con la vida llena no se gasta.
            if (health.IsDead || health.Current >= health.Max)
                return;
            if (!HeartStorage.Take())
                return;
            health.Heal(1);
            Sfx.Play(Sounds.PlayerHeal);
            RefreshTooltip(player);
        }

        // Vuelve a escribir el cartel con lo que queda.
        void RefreshTooltip(GameObject player) => OnFocusChanged(true, player);
    }
}
