using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    [RequireComponent(typeof(Health), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public class MegarachneBossSac : MonoBehaviour
    {
        // Provisionales.
        public const int MaxHealth = 3;
        public const float IdleSeconds = 6f, TrembleSeconds = 1f;
        internal static readonly Vector2 Size = new(1.5f, 2f);
        const int BurstReleaseFrame = 2, EndFrames = 5;

        enum Stage { Idle, Tremble, Burst, Break }
        Stage stage;
        float age;
        bool released;
        MegarachneBoss boss;
        SpriteRenderer sprite;
        BoxCollider2D hit;
        internal Health Health { get; private set; }

        internal void Configure(MegarachneBoss owner)
        {
            boss = owner;
            sprite = GetComponent<SpriteRenderer>();
            hit = GetComponent<BoxCollider2D>();
            hit.size = Size;
            hit.offset = Vector2.down * (Size.y * 0.5f);
            hit.excludeLayers = LayerMask.GetMask(Primordia.Combat.CombatLayers.Player, Primordia.Combat.CombatLayers.Enemy);
            Health = GetComponent<Health>();
            Health.SetMax(MaxHealth, false);
            Health.ResetHealth();
            Health.InvulnerabilityAfterHit = 0f;
            Health.Died += Break;
            Health.Damaged += _ =>
            {
                if (Health.Current > 0)
                    Sfx.Play(Sounds.BossMegarachneSacHit);
            };
            Show();
        }

        void OnDestroy()
        {
            if (Health != null) Health.Died -= Break;
        }

        internal void Burst()
        {
            if (stage == Stage.Idle || stage == Stage.Tremble) Finish(Stage.Burst);
        }

        void Break() => Finish(Stage.Break);

        void Finish(Stage ending)
        {
            // Si estaba temblando, el temblor se corta; rotura por el jugador (sin crías) o reventón (salen las crías).
            Sfx.Stop(Sounds.BossMegarachneSacTremble);
            Sfx.Play(ending == Stage.Break ? Sounds.BossMegarachneSacBreak : Sounds.BossMegarachneSacBurst);
            stage = ending;
            age = 0f;
            hit.enabled = false;
            Health.IsInvulnerable = true;
            Show();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (stage == Stage.Idle && age >= IdleSeconds)
            {
                stage = Stage.Tremble;
                Sfx.Play(Sounds.BossMegarachneSacTremble);
                age -= IdleSeconds;
            }
            if (stage == Stage.Tremble && age >= TrembleSeconds) Burst();
            if (stage == Stage.Burst && !released && age >= BurstReleaseFrame * MegarachneBoss.FxFrameSeconds)
            {
                released = true;
                boss.ReleaseBrood((Vector2)transform.position + hit.offset);
            }
            if ((stage == Stage.Burst || stage == Stage.Break) && age >= EndFrames * MegarachneBoss.FxFrameSeconds)
                Destroy(gameObject);
            else Show();
        }

        void Show() => boss.Frame(sprite, "Megarachne_Sac_" + stage, age, stage == Stage.Idle || stage == Stage.Tremble);
    }
}
