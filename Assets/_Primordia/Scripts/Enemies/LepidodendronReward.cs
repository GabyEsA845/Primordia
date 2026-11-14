using System;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Punto de interacción con E del Lepidodendron (grieta del tocón, salida de la sala, reliquia).</summary>
    public sealed class LepidodendronPrompt : PromptInteractable
    {
        string title, details;
        Color color;
        Action action;
        bool disabled;

        public string Title => title;
        public override bool IsAvailable => !disabled;
        protected override string TooltipTitle => title;
        protected override string TooltipDetails => details;
        protected override Color TooltipColor => color;

        internal static LepidodendronPrompt Create(Transform parent, Vector2 position, Sprite[] keyFrames, string title, string details,
            Color color, float radius, Action action)
        {
            var go = new GameObject("Prompt_" + title);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var prompt = go.AddComponent<LepidodendronPrompt>();
            prompt.title = title;
            prompt.details = details;
            prompt.color = color;
            prompt.action = action;
            prompt.ConfigurePrompt(keyFrames, 1.4f);
            prompt.SetRadius(radius);
            return prompt;
        }

        internal void Disable()
        {
            disabled = true;
            HidePrompt();
        }

        public override void Interact(GameObject player)
        {
            if (disabled) return;
            action?.Invoke();
        }
    }

    /// <summary>Objeto que baja por la columna de luz, flota con su sombra y se coge al tocarlo.</summary>
    public sealed class LepidodendronDrop : MonoBehaviour
    {
        const float BobPixels = 1f / 16f * 2f, BobPeriod = 1.6f;

        Sprite[] fallFrames, idleFrames, glowFrames, shadowFrames;
        SpriteRenderer body, glow, shadow;
        Vector2 rest;
        float height, seconds, sway, step, clock;
        bool landed, taken;

        public Transform Player;
        public bool Landed => landed;
        public event Action Taken;

        internal static LepidodendronDrop Create(Transform parent, string name, Sprite[] fall, Sprite[] idle, Sprite[] glow, Sprite[] shadow,
            Vector2 rest, float height, float seconds, float sway, float step)
        {
            var go = new GameObject("Drop_" + name);
            go.transform.SetParent(parent, true);
            var drop = go.AddComponent<LepidodendronDrop>();
            drop.fallFrames = fall ?? Array.Empty<Sprite>();
            drop.idleFrames = idle ?? Array.Empty<Sprite>();
            drop.glowFrames = glow ?? Array.Empty<Sprite>();
            drop.shadowFrames = shadow ?? Array.Empty<Sprite>();
            drop.rest = rest;
            drop.height = height;
            drop.seconds = Mathf.Max(0.1f, seconds);
            drop.sway = sway;
            drop.step = step;
            drop.body = go.AddComponent<SpriteRenderer>();
            drop.body.sortingOrder = 6;
            drop.shadow = drop.Child("Shadow", -1, rest + Vector2.down * 0.2f);
            if (drop.glowFrames.Length > 0) drop.glow = drop.Child("Glow", 5, rest);
            drop.Apply(0f);
            return drop;
        }

        SpriteRenderer Child(string name, int order, Vector2 position)
        {
            var r = new GameObject(name).AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, true);
            r.transform.position = position;
            r.sortingOrder = order;
            return r;
        }

        void Update()
        {
            clock += Time.deltaTime;
            Apply(clock);
            if (!landed || taken || Player == null) return;
            if (((Vector2)Player.position - (Vector2)transform.position).sqrMagnitude <= 1.2f * 1.2f)
            {
                taken = true;
                Taken?.Invoke();
                Destroy(gameObject);
            }
        }

        void Apply(float t)
        {
            float k = Mathf.Clamp01(t / seconds);
            landed = k >= 1f;
            float eased = 1f - Mathf.Pow(1f - k, 2f);
            Vector2 position = rest + Vector2.up * (height * (1f - eased)) + Vector2.right * (Mathf.Sin(k * Mathf.PI * 3f) * sway * (1f - k));
            if (landed) position.y += Mathf.Sin((t - seconds) / BobPeriod * Mathf.PI * 2f) * BobPixels;
            transform.position = position;
            var frames = landed ? idleFrames : fallFrames;
            if (frames.Length > 0) body.sprite = frames[Mathf.FloorToInt(t / Mathf.Max(0.01f, step)) % frames.Length];
            if (glow != null)
            {
                glow.transform.position = position;
                glow.sprite = glowFrames[Mathf.FloorToInt(t / 0.15f) % glowFrames.Length];
                glow.enabled = landed;
            }
            if (shadowFrames.Length > 0)
            {
                shadow.sprite = shadowFrames[Mathf.FloorToInt(t / 0.14f) % shadowFrames.Length];
                shadow.transform.position = rest + Vector2.down * 0.2f;
                shadow.enabled = k > 0.3f;
            }
        }
    }
}
