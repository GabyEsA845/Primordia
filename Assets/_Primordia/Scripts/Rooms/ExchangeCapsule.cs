using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// La cápsula del laboratorio de Darwin, en el centro de la sala de cambio (desde el piso 2, detrás de la tienda): con
    /// el jugador cerca enseña "E"; al usarla se abre su ventana (<see cref="ExchangeUI"/>) para entregar la mutación de una
    /// ranura y recibir otra del piso (<see cref="MutationExchange"/>). Gratis y una sola vez por piso: después el líquido
    /// baja y se apaga.
    /// Si la Boca nueva cambia el cuerpo (otra familia), metamorfosis corta dentro de la cápsula: el jugador entra, el
    /// líquido brilla del color de la familia nueva, la silueta del cuerpo viejo cambia al nuevo, se revela con sus colores
    /// y, con un destello, la criatura sale delante. Si no, solo un brillo corto.
    /// </summary>
    public class ExchangeCapsule : PromptInteractable
    {
        static readonly Color GlowColor = new(0.45f, 0.95f, 0.65f);

        /// <summary>Silueta de un cuerpo a otro (por familia de la Boca).</summary>
        [Serializable]
        public struct Morph
        {
            public WeaponFamily from;
            public WeaponFamily to;
            public Sprite[] frames;

            public Morph(WeaponFamily from, WeaponFamily to, Sprite[] frames)
            {
                this.from = from;
                this.to = to;
                this.frames = frames;
            }
        }

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle = Array.Empty<Sprite>();
        [SerializeField] Sprite[] used = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float idleFrameDuration = 0.14f;
        [SerializeField, Min(0.01f)] float usedFrameDuration = 0.26f;
        [Tooltip("Las mutaciones que ofrece: las del piso (el generador pone la reserva de su piso).")]
        [SerializeField] MutationPool pool;
        [SerializeField, Min(1)] int floor = 2;

        [Header("Metamorfosis")]
        [Tooltip("Brillo del líquido, encima de la cápsula; se tiñe del color de la familia.")]
        [SerializeField] SpriteRenderer glow;
        [Tooltip("Silueta y revelación de la criatura dentro del vidrio.")]
        [SerializeField] SpriteRenderer silhouette;
        [SerializeField] Sprite[] glowFrames = Array.Empty<Sprite>();
        [SerializeField] Sprite[] flashFrames = Array.Empty<Sprite>();
        [SerializeField] Morph[] morphs = Array.Empty<Morph>();
        [Tooltip("Colores por familia (Proyectil, Cuerpo a cuerpo, Área), los de la metamorfosis de la fosa.")]
        [SerializeField] Color[] familyColors = { new(1f, 0.55f, 0.86f), new(1f, 0.62f, 0.3f), new(0.7f, 0.95f, 0.4f) };
        [Tooltip("Dónde sale la criatura, respecto al centro de la cápsula.")]
        [SerializeField] Vector2 exitOffset = new(0f, -1.7f);

        [Header("Ritmo (segundos)")]
        [SerializeField, Min(0f)] float enter = 0.35f;
        [SerializeField, Min(0f)] float fill = 0.4f;
        [SerializeField, Min(0f)] float morph = 1.1f;
        [SerializeField, Min(0f)] float reveal = 0.4f;
        [SerializeField, Min(0f)] float flashFrame = 0.07f;
        [SerializeField, Min(0f)] float pose = 0.6f;
        [SerializeField, Min(0f)] float shine = 0.6f;

        public bool Used { get; private set; }
        public MutationPool Pool => pool;
        public int Floor => floor;

        /// <summary>True mientras dura la metamorfosis o el brillo tras un cambio.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Se lanza tras un cambio, con la mutación recibida (antes de la metamorfosis).</summary>
        public event Action<MutationData> Exchanged;

        public override bool IsAvailable => !Used;

        protected override string TooltipTitle => "Cápsula de Darwin";
        protected override string TooltipDetails => "E para cambiar una mutación (gratis, una vez por piso)";
        protected override Color TooltipColor => GlowColor;

        internal void Configure(SpriteRenderer newBody, Sprite[] newIdle, Sprite[] newUsed)
        {
            body = newBody;
            idle = newIdle ?? Array.Empty<Sprite>();
            used = newUsed ?? Array.Empty<Sprite>();
        }

        internal void ConfigureMetamorphosis(SpriteRenderer newGlow, SpriteRenderer newSilhouette, Sprite[] newGlowFrames,
            Sprite[] newFlashFrames, Morph[] newMorphs)
        {
            glow = newGlow;
            silhouette = newSilhouette;
            glowFrames = newGlowFrames ?? Array.Empty<Sprite>();
            flashFrames = newFlashFrames ?? Array.Empty<Sprite>();
            morphs = newMorphs ?? Array.Empty<Morph>();
        }

        /// <summary>Lo que ofrece: la reserva y el número de su piso.</summary>
        internal void SetFloor(MutationPool newPool, int newFloor)
        {
            if (newPool != null)
                pool = newPool;
            floor = Mathf.Max(1, newFloor);
        }

        void Awake()
        {
            if (glow != null)
                glow.enabled = false;
            if (silhouette != null)
                silhouette.enabled = false;
        }

        void Update()
        {
            // Mientras cambia, el líquido sigue lleno; se gasta al acabar.
            var frames = Used && !IsPlaying ? used : idle;
            if (body == null || frames.Length == 0)
                return;
            float duration = frames == used ? usedFrameDuration : idleFrameDuration;
            body.sprite = frames[Mathf.FloorToInt(Time.time / duration) % frames.Length];
        }

        public override void Interact(GameObject player)
        {
            if (Used || player == null)
                return;
            var mutations = player.GetComponent<PlayerMutations>();
            var window = FindAnyObjectByType<ExchangeUI>(FindObjectsInactive.Include);
            if (mutations == null || window == null)
                return;
            Sfx.Play(Sounds.ExchangeOpen);
            window.Show(mutations, pool, floor, received => Exchange(mutations, received), null);
        }

        /// <summary>
        /// Hace el cambio (sin ventana: también lo usan las pruebas), deja la cápsula gastada y lanza la metamorfosis (o el
        /// brillo). La mutación nueva ya está equipada al volver; el cuerpo nuevo se ve al acabar la metamorfosis.
        /// </summary>
        public void Exchange(PlayerMutations player, MutationData received)
        {
            if (Used || player == null || received == null)
                return;
            var look = player.GetComponent<MutationLook>();
            bool newBody = MutationExchange.ChangesBody(player, received);
            var oldMouth = player.Get(MutationSlot.Mouth);
            if (newBody && look != null)
                look.HoldForm();
            if (MutationExchange.Apply(player, received, floor) == 0)
            {
                if (newBody && look != null)
                    look.ReleaseForm();
                return;
            }
            Used = true;
            HidePrompt();
            Exchanged?.Invoke(received);

            var color = ColorOf(received.Family);
            if (newBody)
                StartCoroutine(Metamorphose(player, look, MorphFrames(oldMouth, received), received.RevealFrames, color));
            else
                StartCoroutine(Shine(color));
        }

        public Color ColorOf(WeaponFamily family)
        {
            int index = (int)family;
            return familyColors != null && index < familyColors.Length ? familyColors[index] : Color.white;
        }

        Sprite[] MorphFrames(MutationData from, MutationData to)
        {
            if (from == null || to == null)
                return to != null ? to.MorphFrames : Array.Empty<Sprite>();
            foreach (var entry in morphs)
            {
                if (entry.from == from.Family && entry.to == to.Family && entry.frames is { Length: > 0 })
                    return entry.frames;
            }
            return Array.Empty<Sprite>();
        }

        /// <summary>Sin cambio de cuerpo: el líquido brilla un momento del color de la familia.</summary>
        IEnumerator Shine(Color color)
        {
            IsPlaying = true;
            Sfx.Play(Sounds.ExchangeAccept);
            for (float t = 0f; t < shine; t += Time.deltaTime)
            {
                ShowGlow(color, Mathf.Sin(t / shine * Mathf.PI));
                yield return null;
            }
            HideGlow();
            IsPlaying = false;
        }

        IEnumerator Metamorphose(PlayerMutations player, MutationLook look, Sprite[] morphFrames, Sprite[] revealFrames, Color color)
        {
            IsPlaying = true;
            var manager = GameManager.Instance;
            if (manager != null)
                manager.SetState(GameState.Cinematic);
            var animator = player.GetComponent<PlayerAnimator>();
            var rigidbody = player.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector2.zero;
                rigidbody.simulated = false;
            }

            // El jugador entra: se desvanece mientras se acerca al vidrio y el líquido empieza a brillar.
            var renderers = new List<(SpriteRenderer renderer, Color color)>();
            foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
                renderers.Add((renderer, renderer.color));
            var visual = player.transform;
            Vector2 start = visual.position;
            Vector2 center = transform.position;
            Sfx.Play(Sounds.ExchangeAccept);
            yield return Phase(enter, k =>
            {
                visual.position = Vector2.Lerp(start, center, k * 0.6f);
                SetAlpha(renderers, 1f - k);
                ShowGlow(color, k * 0.6f);
            });
            if (look != null)
                look.Hidden = true;

            // Dentro: la silueta del cuerpo viejo aparece y cambia al nuevo.
            SetSilhouette(Frame(morphFrames, 0), 0f);
            yield return Phase(fill, k =>
            {
                ShowGlow(color, 0.6f + k * 0.4f);
                SetSilhouette(Frame(morphFrames, 0), k);
            });
            yield return Phase(morph, k =>
            {
                ShowGlow(color, 1f);
                SetSilhouette(Frame(morphFrames, (int)(k * morphFrames.Length)), 1f);
            });
            Sfx.Play(Sounds.RoomExit);
            yield return Phase(reveal, k =>
            {
                ShowGlow(color, 1f);
                SetSilhouette(Frame(revealFrames, (int)(k * revealFrames.Length)), 1f);
            });

            // Destello: la criatura nueva sale delante de la cápsula.
            if (look != null)
            {
                look.ReleaseForm();
                look.Hidden = false;
            }
            if (silhouette != null)
                silhouette.enabled = false;
            visual.position = center + exitOffset;
            SetAlpha(renderers, 1f);
            if (animator != null)
                animator.PlayAttack(PlayerAnimator.Facing.Down);
            if (glow != null && flashFrames.Length > 0)
            {
                foreach (var frame in flashFrames)
                {
                    glow.sprite = frame;
                    glow.color = color;
                    glow.enabled = true;
                    yield return new WaitForSeconds(flashFrame);
                }
            }
            HideGlow();
            yield return new WaitForSeconds(pose);

            if (rigidbody != null)
            {
                rigidbody.position = visual.position;
                rigidbody.simulated = true;
            }
            if (manager != null && manager.State == GameState.Cinematic)
                manager.SetState(GameState.Playing);
            IsPlaying = false;
        }

        static IEnumerator Phase(float duration, Action<float> update)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                update(Mathf.Clamp01(t / duration));
                yield return null;
            }
            update(1f);
        }

        void ShowGlow(Color color, float alpha)
        {
            if (glow == null || glowFrames.Length == 0)
                return;
            glow.enabled = true;
            glow.sprite = glowFrames[Mathf.FloorToInt(Time.time / 0.1f) % glowFrames.Length];
            glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        void HideGlow()
        {
            if (glow != null)
                glow.enabled = false;
        }

        void SetSilhouette(Sprite sprite, float alpha)
        {
            if (silhouette == null)
                return;
            silhouette.enabled = sprite != null;
            silhouette.sprite = sprite;
            silhouette.color = new Color(1f, 1f, 1f, alpha);
        }

        static Sprite Frame(Sprite[] frames, int index) =>
            frames == null || frames.Length == 0 ? null : frames[Mathf.Clamp(index, 0, frames.Length - 1)];

        static void SetAlpha(List<(SpriteRenderer renderer, Color color)> renderers, float alpha)
        {
            foreach (var (renderer, original) in renderers)
            {
                if (renderer != null)
                    renderer.color = new Color(original.r, original.g, original.b, original.a * alpha);
            }
        }
    }
}
