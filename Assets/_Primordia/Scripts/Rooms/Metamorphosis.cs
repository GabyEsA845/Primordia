using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Rooms
{
    /// <summary>
    /// Cinemática de la metamorfosis al saltar a la fosa, tras elegir la Boca: fundido a un abismo por el que la criatura
    /// cae (burbujas y motas que suben, rayos de luz), un brillo del color de la familia de la Boca que la cubre, la
    /// silueta blanca que cambia de la célula a la criatura nueva, un estallido de luz que la revela con sus colores y
    /// una pose de ataque, y un fundido a negro. Desde la segunda vez que se ve entera se puede saltar con Interactuar (E),
    /// con un aviso abajo a la derecha.
    /// </summary>
    public class Metamorphosis : MonoBehaviour
    {
        /// <summary>Clave de PlayerPrefs: la metamorfosis ya se vio entera una vez (desde entonces se puede saltar).</summary>
        public const string SeenKey = "Primordia.MetamorphosisSeen";

        [Header("Arte")]
        [SerializeField] Sprite background;
        [SerializeField] Sprite rays;
        [SerializeField] Sprite[] moteFrames = Array.Empty<Sprite>();
        [SerializeField] Sprite[] bubbleFrames = Array.Empty<Sprite>();
        [Tooltip("Halo en blanco, se tiñe del color de la familia.")]
        [SerializeField] Sprite[] auraFrames = Array.Empty<Sprite>();
        [Tooltip("Estallido en blanco, se tiñe del color de la familia.")]
        [SerializeField] Sprite[] burstFrames = Array.Empty<Sprite>();

        [Header("Colores por familia (Proyectil, Cuerpo a cuerpo, Área)")]
        [SerializeField] Color[] familyColors =
        {
            new(1f, 0.55f, 0.86f), new(1f, 0.62f, 0.3f), new(0.7f, 0.95f, 0.4f),
        };

        [Header("Ritmo (segundos)")]
        [SerializeField, Min(0f)] float fadeOut = 0.35f;
        [SerializeField, Min(0f)] float fadeIn = 0.45f;
        [SerializeField, Min(0f)] float fall = 1f;
        [SerializeField, Min(0f)] float glow = 1.5f;
        [SerializeField, Min(0f)] float morph = 1.5f;
        [SerializeField, Min(0f)] float reveal = 0.7f;
        [SerializeField, Min(0f)] float pose = 1.3f;
        [SerializeField, Min(0f)] float endFade = 0.6f;

        [Header("Escena")]
        [Tooltip("Dónde se monta el abismo, respecto a la fosa: lejos del piso, fuera de la vista.")]
        [SerializeField] Vector2 stageOffset = new(0f, -400f);
        [Tooltip("Tamaño de la cámara durante la escena (0,75 = se acerca un 25 %).")]
        [SerializeField, Range(0.3f, 1f)] float zoom = 0.7f;

        /// <summary>Se ha visto entera alguna vez: desde entonces se puede saltar.</summary>
        public static bool Seen
        {
            get => PlayerPrefs.GetInt(SeenKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SeenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool IsPlaying { get; private set; }

        /// <summary>True si la última se saltó con Interactuar.</summary>
        public bool Skipped { get; private set; }

        // Las pruebas lo ponen a mano en lugar de leer la tecla.
        internal bool readInput = true;
        internal bool skipPressed;

        readonly List<GameObject> spawned = new();
        InputAction interactAction;
        bool canSkip;

        internal void Configure(Sprite newBackground, Sprite newRays, Sprite[] newMotes, Sprite[] newBubbles, Sprite[] newAura,
            Sprite[] newBurst)
        {
            background = newBackground;
            rays = newRays;
            moteFrames = newMotes ?? Array.Empty<Sprite>();
            bubbleFrames = newBubbles ?? Array.Empty<Sprite>();
            auraFrames = newAura ?? Array.Empty<Sprite>();
            burstFrames = newBurst ?? Array.Empty<Sprite>();
        }

        internal void SetTiming(float newFadeOut, float newFadeIn, float newFall, float newGlow, float newMorph, float newReveal,
            float newPose, float newEndFade)
        {
            fadeOut = newFadeOut;
            fadeIn = newFadeIn;
            fall = newFall;
            glow = newGlow;
            morph = newMorph;
            reveal = newReveal;
            pose = newPose;
            endFade = newEndFade;
        }

        public Color ColorOf(WeaponFamily family)
        {
            int index = (int)family;
            return familyColors != null && index < familyColors.Length ? familyColors[index] : Color.white;
        }

        /// <summary>La escena entera. Al acabar, la pantalla queda en negro y la criatura con su forma nueva.</summary>
        public IEnumerator Play(GameObject player, MutationData mouth)
        {
            IsPlaying = true;
            Skipped = false;
            skipPressed = false;
            canSkip = Seen;
            interactAction ??= InputSystem.actions?.FindAction("Player/Interact");
            var overlay = FindAnyObjectByType<CinematicOverlay>();
            if (overlay != null)
                overlay.SetSkipHint(canSkip);

            var look = player.GetComponent<MutationLook>();
            var animator = player.GetComponent<PlayerAnimator>();
            var color = mouth != null ? ColorOf(mouth.Family) : Color.white;
            var fader = CreateFader();

            yield return Fade(fader, 0f, 1f, fadeOut);

            // El abismo, lejos del piso; la criatura y la cámara se van allí.
            Vector2 stage = (Vector2)transform.position + stageOffset;
            var scene = BuildStage(stage);
            var visual = player.transform;
            var baseScale = visual.localScale.sqrMagnitude > 0.0001f ? visual.localScale : Vector3.one;
            visual.localScale = baseScale;
            visual.position = stage;
            if (animator != null)
                animator.FacingOverride = PlayerAnimator.Facing.Side;
            var lookAhead = FindAnyObjectByType<CameraLookAhead>();
            SnapCamera(lookAhead, stage);
            var cameraZoom = new CameraZoom(zoom);
            cameraZoom.Set(1f);
            Sfx.Play(Sounds.TransitionFall);

            yield return Fade(fader, 1f, 0f, fadeIn, () => Drift(scene, visual, stage));

            // Brillo y silueta, por encima del cuerpo; las piezas se funden en la luz.
            int order = animator != null && animator.Body != null ? animator.Body.sortingOrder : 5;
            var renderers = new List<(SpriteRenderer renderer, Color color)>();
            foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
                renderers.Add((renderer, renderer.color));
            var aura = Child(visual, "MetamorphosisAura", order - 1, auraFrames.Length > 0 ? auraFrames[0] : null);
            var silhouette = Child(visual, "MetamorphosisSilhouette", order + 6, Frame(mouth?.MorphFrames, 0));
            var burst = Child(visual, "MetamorphosisBurst", order + 7, null);
            SetAlpha(aura, color, 0f);
            SetAlpha(silhouette, Color.white, 0f);

            bool skipped = false;
            yield return Phase(fall, _ => Drift(scene, visual, stage), () => skipped = true);

            if (!skipped)
                yield return Phase(glow, k =>
                {
                    Drift(scene, visual, stage);
                    Animate(aura, auraFrames, 8f);
                    aura.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.3f, k);
                    SetAlpha(aura, color, Mathf.SmoothStep(0f, 1f, k));
                    SetAlpha(silhouette, Color.white, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.9f, k)));
                    float pieces = 1f - Mathf.InverseLerp(0.4f, 1f, k);
                    foreach (var (renderer, original) in renderers)
                        renderer.color = new Color(original.r, original.g, original.b, original.a * pieces);
                }, () => skipped = true);

            if (!skipped)
            {
                if (look != null)
                    look.Hidden = true;
                yield return Phase(morph, k =>
                {
                    Drift(scene, visual, stage);
                    Animate(aura, auraFrames, 10f);
                    aura.transform.localScale = Vector3.one * (1.3f + Mathf.Sin(Time.time * 9f) * 0.08f);
                    var frames = mouth != null ? mouth.MorphFrames : Array.Empty<Sprite>();
                    if (frames.Length > 0)
                        silhouette.sprite = frames[Mathf.Min(frames.Length - 1, (int)(k * frames.Length))];
                }, () => skipped = true);
            }

            // Revelación: estallido, la silueta toma los colores y aparece la criatura nueva.
            if (look != null)
                look.ReleaseForm();
            foreach (var (renderer, original) in renderers)
            {
                if (renderer != null)
                    renderer.color = original;
            }

            if (!skipped)
            {
                Sfx.Play(Sounds.RoomExit);
                var revealFrames = mouth != null ? mouth.RevealFrames : Array.Empty<Sprite>();
                yield return Phase(reveal, k =>
                {
                    Drift(scene, visual, stage);
                    burst.sprite = Frame(burstFrames, Mathf.Min(burstFrames.Length - 1, (int)(k * burstFrames.Length * 1.4f)));
                    SetAlpha(burst, color, 1f - Mathf.InverseLerp(0.6f, 1f, k));
                    SetAlpha(aura, color, 1f - k);
                    if (revealFrames.Length > 0)
                        silhouette.sprite = revealFrames[Mathf.Min(revealFrames.Length - 1, (int)(k * revealFrames.Length))];
                    SetAlpha(silhouette, Color.white, 1f - Mathf.InverseLerp(0.7f, 1f, k));
                }, () => skipped = true);
            }

            Destroy(aura.gameObject);
            Destroy(silhouette.gameObject);
            Destroy(burst.gameObject);
            if (look != null)
                look.Hidden = false;

            if (!skipped)
            {
                // Se ha visto entera: desde la próxima vez se puede saltar.
                Seen = true;
                if (animator != null)
                    animator.PlayAttack(PlayerAnimator.Facing.Side);
                yield return Phase(pose, _ => Drift(scene, visual, stage), () => skipped = true);
            }

            Skipped = skipped;
            if (overlay != null)
                overlay.SetSkipHint(false);
            yield return Fade(fader, 0f, 1f, endFade, () => Drift(scene, visual, stage));

            if (animator != null)
                animator.FacingOverride = null;
            cameraZoom.Restore();
            if (lookAhead != null)
                lookAhead.enabled = true;
            foreach (var go in spawned)
            {
                if (go != null && go != fader.gameObject)
                    Destroy(go);
            }
            spawned.Clear();
            spawned.Add(fader.gameObject);
            IsPlaying = false;
        }

        /// <summary>Quita el negro del final (lo deja puesto <see cref="Play"/> para que la victoria salga sobre él).</summary>
        public void ClearFade()
        {
            foreach (var go in spawned)
            {
                if (go != null)
                    Destroy(go);
            }
            spawned.Clear();
        }

        void OnDestroy() => ClearFade();

        bool SkipRequested()
        {
            if (!canSkip)
                return false;
            if (readInput && interactAction != null && interactAction.WasPressedThisFrame())
                skipPressed = true;
            return skipPressed;
        }

        IEnumerator Phase(float duration, Action<float> update, Action onSkip)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (SkipRequested())
                {
                    onSkip();
                    yield break;
                }
                update(t / duration);
                yield return null;
            }
            update(1f);
        }

        IEnumerator Fade(SpriteRenderer fader, float from, float to, float duration, Action update = null)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                update?.Invoke();
                SetAlpha(fader, Color.black, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            SetAlpha(fader, Color.black, to);
        }

        // ---------- El abismo ----------

        sealed class Stage
        {
            public Transform Root;
            public readonly List<(Transform particle, float speed)> Particles = new();
            public readonly List<SpriteRenderer> Animated = new();
            public Transform[] Rays = Array.Empty<Transform>();
        }

        Stage BuildStage(Vector2 center)
        {
            var root = new GameObject("MetamorphosisStage").transform;
            root.position = center;
            spawned.Add(root.gameObject);
            var stage = new Stage { Root = root };

            if (background != null)
            {
                var bg = Child(root, "Background", -60, background);
                bg.transform.localScale = Vector3.one * 1.3f;
            }

            if (rays != null)
            {
                stage.Rays = new Transform[2];
                for (int i = 0; i < 2; i++)
                {
                    var ray = Child(root, $"Rays_{i}", -58, rays);
                    ray.transform.localPosition = new Vector3(i == 0 ? -6f : 7f, 1f, 0f);
                    ray.color = new Color(1f, 1f, 1f, i == 0 ? 0.55f : 0.35f);
                    stage.Rays[i] = ray.transform;
                }
            }

            var random = new System.Random(17);
            float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
            for (int i = 0; i < 46 && moteFrames.Length > 0; i++)
            {
                var mote = Child(root, "Mote", -55, moteFrames[i % moteFrames.Length]);
                mote.transform.localPosition = new Vector3(Range(-15f, 15f), Range(-10f, 10f), 0f);
                stage.Particles.Add((mote.transform, Range(5f, 11f)));
            }
            for (int i = 0; i < 14 && bubbleFrames.Length > 0; i++)
            {
                // Unas pocas por delante de la criatura, más rápidas: dan profundidad.
                bool front = i % 4 == 0;
                var bubble = Child(root, "Bubble", front ? 30 : -54, bubbleFrames[0]);
                bubble.transform.localPosition = new Vector3(Range(-14f, 14f), Range(-10f, 10f), 0f);
                bubble.transform.localScale = Vector3.one * (front ? 1.6f : 1f);
                stage.Particles.Add((bubble.transform, front ? Range(12f, 16f) : Range(4f, 8f)));
                stage.Animated.Add(bubble);
            }
            return stage;
        }

        /// <summary>Las partículas suben deprisa (se cae) y la criatura se mece un poco.</summary>
        void Drift(Stage stage, Transform creature, Vector2 center)
        {
            float dt = Time.deltaTime;
            foreach (var (particle, speed) in stage.Particles)
            {
                var p = particle.localPosition;
                p.y += speed * dt;
                if (p.y > 10f)
                    p.y -= 20f;
                particle.localPosition = p;
            }
            foreach (var bubble in stage.Animated)
                Animate(bubble, bubbleFrames, 4f);
            for (int i = 0; i < stage.Rays.Length; i++)
                stage.Rays[i].localPosition = new Vector3(stage.Rays[i].localPosition.x, 1f + Mathf.Sin(Time.time * 0.7f + i) * 0.3f, 0f);

            creature.position = center + new Vector2(Mathf.Sin(Time.time * 1.6f) * 0.35f, Mathf.Sin(Time.time * 2.3f) * 0.12f);
        }

        static void SnapCamera(CameraLookAhead lookAhead, Vector2 position)
        {
            if (lookAhead != null)
            {
                lookAhead.enabled = false;
                lookAhead.transform.position = position;
            }
            var virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            if (virtualCamera != null)
            {
                virtualCamera.ForceCameraPosition((Vector3)position + Vector3.back * 10f, Quaternion.identity);
                virtualCamera.PreviousStateIsValid = false;
            }
        }

        // ---------- Utilidades ----------

        SpriteRenderer CreateFader()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            var fader = new GameObject("MetamorphosisFade").AddComponent<SpriteRenderer>();
            fader.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            fader.sortingOrder = 1000;
            var camera = Camera.main;
            if (camera != null)
            {
                fader.transform.SetParent(camera.transform, false);
                fader.transform.localPosition = new Vector3(0f, 0f, 1f);
            }
            fader.transform.localScale = new Vector3(200f, 200f, 1f);
            SetAlpha(fader, Color.black, 0f);
            spawned.Add(fader.gameObject);
            return fader;
        }

        static SpriteRenderer Child(Transform parent, string name, int sortingOrder, Sprite sprite)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = sprite;
            return renderer;
        }

        static Sprite Frame(Sprite[] frames, int index) =>
            frames != null && frames.Length > 0 ? frames[Mathf.Clamp(index, 0, frames.Length - 1)] : null;

        static void Animate(SpriteRenderer renderer, Sprite[] frames, float fps)
        {
            if (renderer != null && frames != null && frames.Length > 0)
                renderer.sprite = frames[(int)(Time.time * fps) % frames.Length];
        }

        static void SetAlpha(SpriteRenderer renderer, Color color, float alpha)
        {
            if (renderer != null)
                renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }
    }
}
