using Primordia.Audio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        public const float IntroPanSeconds = 1.5f;
        const float IntroHoldSeconds = 2.2f, IntroReturnSeconds = 0.9f; // Provisionales, presentación.
        [SerializeField] BossIntroData introData;
        static readonly HashSet<UnityEngine.SceneManagement.Scene> presentedScenes = new();
        readonly Dictionary<Health, bool> protectedHealth = new();
        readonly Dictionary<Behaviour, bool> pausedBehaviours = new();
        Rigidbody2D frozenBody;
        RigidbodyConstraints2D savedConstraints;
        GameState previousState;
        bool ownsCinematic;
        BossIntroBanner introBanner;
        Coroutine presentation;
        public bool IntroPlaying { get; private set; }
        public bool IntroPlayed { get; private set; }
        public bool FightReady => !IntroPlaying && !Collapsing && !HasCollapsed;
        public float BarHitAt { get; private set; } = float.NegativeInfinity;
        public int PhaseIndex => walls.Count(w => w.Fallen);
        public float[] PhaseThresholds => new[] { 0.75f, 0.5f, 0.25f };
        internal void ConfigureIntro(BossIntroData data) => introData = data;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPresentations() => presentedScenes.Clear();
        public bool BarKnotHit(int index) => walls[index].GetComponentsInChildren<SpriteRenderer>()
            .Any(r => r.sprite != null && r.sprite.name.Contains("_Knot") && r.sprite.name.Contains("_Hit"));
        public int BarConeState(int index) => cones[index].Broken ? 3 : cones[index].ArmorRemaining == 0 ? 2 : cones[index].Charred ? 1 : 0;
        void BarHealthChanged(int current, int max) => BarHitAt = Time.unscaledTime;

        IEnumerator AutoPresentation()
        {
            yield return null;
            if (built && wallRoom.GetComponent<Room>() != null && introData != null && !HasCollapsed)
            {
                if (presentedScenes.Contains(gameObject.scene)) IntroPlayed = true;
                else yield return IntroRoutine();
            }
        }

        internal void ReplayPresentation()
        {
            if (!built || IntroPlaying || Collapsing || HasCollapsed) return;
            presentation = StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            IntroPlaying = true;
            IntroPlayed = true;
            Sfx.Play(Sounds.BossLepidodendronIntro);
            presentedScenes.Add(gameObject.scene);
            AcquireCinematic();
            Vector2 start = player != null ? (Vector2)player.position : entrance;
            for (float t = 0; t < IntroPanSeconds; t += Time.deltaTime)
            {
                lookAhead?.SetFocus(Vector2.Lerp(start, ArenaCenter, Mathf.SmoothStep(0, 1, t / IntroPanSeconds)), 1f);
                yield return null;
            }
            PlayIntroPose(IntroHoldSeconds);
            var overlay = FindAnyObjectByType<CinematicOverlay>();
            if (overlay != null && introData != null)
            {
                introBanner = BossIntroBanner.For(overlay.transform);
                introBanner.Show(introData, secret: true);
            }
            for (float t = 0; t < IntroHoldSeconds; t += Time.deltaTime)
            {
                lookAhead?.SetFocus(ArenaCenter, 1f);
                if (t < 0.3f)
                    foreach (var wall in walls.Where(w => !w.Fallen))
                    {
                        wall.transform.position += Vector3.right * (Mathf.Sin(t * 60f) / Ppu - introWallShake);
                        for (int k = 0; k < wall.KnotCount; k++)
                            wall.transform.Find($"Knot{k + 1}").GetComponent<SpriteRenderer>().sprite = art.Find($"Lepido_Wall{wall.Index + 1}_Knot{k}_Hit").sprites[Mathf.FloorToInt(t / 0.06f) % 2];
                    }
                introWallShake = t < 0.3f ? Mathf.Sin(t * 60f) / Ppu : introWallShake;
                yield return null;
            }
            RestoreIntroWalls();
            introBanner?.Hide();
            for (float t = 0; t < IntroReturnSeconds; t += Time.deltaTime)
            {
                lookAhead?.SetFocus(ArenaCenter, 1f - Mathf.SmoothStep(0, 1, t / IntroReturnSeconds));
                yield return null;
            }
            IntroPlaying = false;
            presentation = null;
            ReleaseCinematic();
        }
        float introWallShake;
        void RestoreIntroWalls()
        {
            foreach (var wall in walls.Where(w => w != null && !w.Fallen)) wall.transform.position -= Vector3.right * introWallShake;
            introWallShake = 0;
        }

        void AcquireCinematic()
        {
            ownsCinematic = true;
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                player = controller.transform;
                Protect(controller.GetComponent<Health>());
                frozenBody = controller.GetComponent<Rigidbody2D>();
                if (frozenBody != null)
                {
                    savedConstraints = frozenBody.constraints;
                    frozenBody.linearVelocity = Vector2.zero;
                    frozenBody.constraints = RigidbodyConstraints2D.FreezeAll;
                }
            }
            foreach (var wall in walls) { Protect(wall.Health); Pause(wall); }
            foreach (var cone in cones) { Protect(cone.Health); Pause(cone); }
            var manager = GameManager.Instance;
            if (manager != null) { previousState = manager.State; manager.SetState(GameState.Cinematic); }
            lookAhead = FindAnyObjectByType<CameraLookAhead>();
        }
        void Protect(Health target)
        {
            if (target == null || protectedHealth.ContainsKey(target)) return;
            protectedHealth.Add(target, target.IsInvulnerable);
            target.IsInvulnerable = true;
        }
        void Pause(Behaviour target) { pausedBehaviours[target] = target.enabled; target.enabled = false; }
        void ReleaseCinematic()
        {
            if (!ownsCinematic) return;
            ownsCinematic = false;
            RestoreIntroWalls();
            introBanner?.HideImmediate();
            foreach (var pair in protectedHealth) if (pair.Key != null) pair.Key.IsInvulnerable = pair.Value;
            protectedHealth.Clear();
            foreach (var pair in pausedBehaviours) if (pair.Key != null) pair.Key.enabled = pair.Value;
            pausedBehaviours.Clear();
            if (frozenBody != null) { frozenBody.linearVelocity = Vector2.zero; frozenBody.constraints = savedConstraints; }
            frozenBody = null;
            lookAhead?.ClearFocus();
            var manager = GameManager.Instance;
            if (manager != null && manager.State == GameState.Cinematic) manager.SetState(previousState);
        }
    }
}
