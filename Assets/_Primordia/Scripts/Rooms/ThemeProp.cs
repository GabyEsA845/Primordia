using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Pieza de decorado de una sala con tema (nido, seda, matas, posaderos…): se mece si tiene varios cuadros en reposo y, si
    /// tiene dibujo de fuego, arde al prenderla (unos segundos, con un fuego en su base que salta a lo de al lado) y queda como
    /// restos. Se atraviesa y no tapa a nadie salvo que el jugador esté detrás de ella. Solo decorado: no esconde ni frena.
    /// </summary>
    public class ThemeProp : MonoBehaviour, IFlammable
    {
        const float BurnFrameSeconds = 0.11f;
        const float BurnSeconds = 3f;
        const float BaseFireSeconds = 3.5f;
        const int BehindOrder = 3;
        const int InFrontOrder = 12;

        enum State
        {
            Idle,
            Burning,
            Burnt
        }

        SpriteRenderer body;
        ThemeDecorArt.Look look;
        float idleSeconds;
        float phase;
        float halfWidth;
        State state;
        float stateStart;
        Transform player;

        public Vector2 Position => transform.position;

        public bool CanIgnite => look != null && look.burn != null && look.burn.Length > 0 && state == State.Idle;

        internal void Configure(SpriteRenderer newBody, ThemeDecorArt.Look newLook, float newIdleSeconds, float newPhase)
        {
            body = newBody;
            look = newLook;
            idleSeconds = newIdleSeconds;
            phase = newPhase;
            halfWidth = look.idle[0] != null ? look.idle[0].bounds.extents.x : 0.8f;
            Show();
        }

        void OnEnable() => Flammables.Register(this);

        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (!CanIgnite)
                return;
            state = State.Burning;
            stateStart = Time.time;
            GroundFire.Spawn(transform.position, BaseFireSeconds);
            Show();
        }

        void Update()
        {
            if (state == State.Burning && Time.time - stateStart >= BurnSeconds)
            {
                state = State.Burnt;
                stateStart = Time.time;
            }
            Show();
            Sort();
        }

        void Show()
        {
            if (body == null || look == null)
                return;
            switch (state)
            {
                case State.Burning:
                    body.sprite = Frame(look.burn, BurnFrameSeconds, Time.time - stateStart);
                    break;
                case State.Burnt:
                    if (look.burnt != null)
                        body.sprite = look.burnt;
                    break;
                default:
                    body.sprite = look.idle.Length > 1 ? Frame(look.idle, idleSeconds, Time.time + phase) : look.idle[0];
                    break;
            }
        }

        static Sprite Frame(Sprite[] frames, float seconds, float time)
        {
            int frame = Mathf.FloorToInt(time / seconds);
            return frames[((frame % frames.Length) + frames.Length) % frames.Length];
        }

        // Tapa al jugador cuando está detrás (más arriba que su base) y a la altura de la pieza.
        void Sort()
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
            }
            bool covers = player != null && player.position.y - 0.3f > transform.position.y + 0.05f
                && player.position.y - 0.3f < transform.position.y + body.sprite.bounds.size.y
                && Mathf.Abs(player.position.x - transform.position.x) < halfWidth * 0.8f;
            body.sortingOrder = covers ? InFrontOrder : BehindOrder;
        }
    }
}
