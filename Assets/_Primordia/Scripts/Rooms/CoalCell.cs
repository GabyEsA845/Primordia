using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Una casilla de la veta de carbón (sala especial del piso 3): carbón apagado hasta que le llega fuego (el del suelo, el de
    /// un enemigo incendiario, un helecho que arde…). Entonces prende, arde con un fuego en el suelo que quema a quien lo pisa y
    /// salta a las casillas de al lado (la veta entera forma un muro de fuego que parte la sala), luego queda en brasas que se
    /// cruzan sin daño y al final se apaga para siempre. Mientras arde daña de verdad: al jugador le quita medio corazón cada vez que
    /// no está en la esquiva y a los enemigos los prende. Una casilla «encendida» (<see cref="CreateLit"/>) ya nace en brasas y no se
    /// apaga nunca: alumbra, dispersa el humo del Bosque en humo y, si se le pide, suelta chispas de vez en cuando que prenden lo
    /// inflamable cercano (así la veta de la Veta de carbón acaba ardiendo).
    /// </summary>
    public class CoalCell : MonoBehaviour, IFlammable
    {
        enum State
        {
            Idle,
            Igniting,
            Burning,
            Embers,
            Out
        }

        const float IgniteFrameSeconds = 0.09f;
        const float BurnFrameSeconds = 0.09f;
        const float EmberFrameSeconds = 0.14f;
        const float BurnSeconds = 8f;
        const float EmberSeconds = 12f;
        const int TileOrder = -7;
        const int CornerOrder = -6;
        const int FireOrder = 7;
        // Daño de las llamas: se mira cada 0,2 s; el golpe es de medio corazón y el jugador tiene sus segundos de gracia tras él.
        const float HurtEvery = 0.2f;
        const int PlayerDamage = 1;
        const float EnemyBurnSeconds = 3f;
        const float SparkFrameSeconds = 0.07f;
        // El fuego salta a las casillas de al lado cada 0,6 s (2 tiles): la veta entera arde aunque no haya fuego del suelo.
        const float SpreadEvery = 0.6f;
        const float SpreadRadius = 1.6f;

        SpecialRoomArt art;
        SpriteRenderer tile;
        SpriteRenderer overlay;
        Sprite[] burn = System.Array.Empty<Sprite>();
        State state;
        float stateStart;
        float nextHurt;
        float nextSpread;
        // Casilla encendida fija (brasas para siempre) y sus chispas.
        bool lit;
        Vector2 sparkEvery;
        float sparkRadius;
        float nextSpark;
        System.Func<bool> sparkGate;

        public Vector2 Position => transform.position;

        public bool CanIgnite => state == State.Idle;

        /// <summary>True mientras arde con llamas (daña a quien lo pisa).</summary>
        public bool IsBurning => state is State.Igniting or State.Burning;

        /// <summary>True en brasas: se cruza sin daño.</summary>
        public bool IsEmbers => state == State.Embers;

        /// <summary>True cuando ya está apagado del todo.</summary>
        public bool IsOut => state == State.Out;

        /// <summary>True si es una casilla encendida fija (brasas para siempre).</summary>
        public bool IsLit => lit;

        /// <summary>Chispas de una casilla encendida: cada tanto (entre esos segundos) prende lo inflamable a ese radio, si la puerta lo permite.</summary>
        internal void ConfigureSparks(Vector2 everySeconds, float radius, System.Func<bool> gate)
        {
            sparkEvery = everySeconds;
            sparkRadius = radius;
            sparkGate = gate;
            nextSpark = Time.time + Random.Range(everySeconds.x, everySeconds.y);
        }

        /// <summary>Una casilla de veta encendida desde el principio: brasas fijas, que no se apagan ni se pueden prender.</summary>
        internal static CoalCell CreateLit(Transform parent, Vector2 center, Sprite tileSprite, Sprite[] corners, SpecialRoomArt art, int variant)
        {
            var cell = Create(parent, center, tileSprite, corners, art, variant);
            cell.lit = true;
            cell.SetState(State.Embers);
            return cell;
        }

        /// <param name="center">Centro de la casilla.</param>
        /// <param name="corners">Esquinas de dentro que lleva (sprites ya elegidos).</param>
        /// <param name="variant">0-2: qué variante de llamas usa (para que las vecinas no repitan).</param>
        internal static CoalCell Create(Transform parent, Vector2 center, Sprite tileSprite, Sprite[] corners, SpecialRoomArt art, int variant)
        {
            var go = new GameObject("CoalCell");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var cell = go.AddComponent<CoalCell>();
            cell.art = art;
            cell.burn = art.BurnVariant(variant);
            cell.tile = go.AddComponent<SpriteRenderer>();
            cell.tile.sprite = tileSprite;
            cell.tile.sortingOrder = TileOrder;

            foreach (var corner in corners)
            {
                var piece = new GameObject("Corner");
                piece.transform.SetParent(go.transform, false);
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = corner;
                renderer.sortingOrder = CornerOrder;
                cell.cornerRenderers.Add(renderer);
            }

            // Las capas de fuego (16×32) apoyan en el borde de abajo de la casilla.
            var fire = new GameObject("Fire");
            fire.transform.SetParent(go.transform, false);
            fire.transform.localPosition = Vector3.down * 0.5f;
            cell.overlay = fire.AddComponent<SpriteRenderer>();
            cell.overlay.sortingOrder = FireOrder;
            return cell;
        }

        readonly System.Collections.Generic.List<SpriteRenderer> cornerRenderers = new();

        void OnEnable() => Flammables.Register(this);

        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (state != State.Idle)
                return;
            SetState(State.Igniting);
        }

        void SetState(State next)
        {
            state = next;
            stateStart = Time.time;
            if (next == State.Burning)
            {
                // El fuego del suelo es quien quema y quien salta a las casillas de al lado; el dibujo del fuego es el de la veta.
                var fire = GroundFire.Spawn(transform.position, BurnSeconds);
                if (fire != null && fire.TryGetComponent<SpriteRenderer>(out var fireRenderer))
                    fireRenderer.enabled = false;
            }
            else if (next == State.Out)
            {
                tile.sprite = art.coalOut != null ? art.coalOut : tile.sprite;
                foreach (var corner in cornerRenderers)
                    corner.enabled = false;
                overlay.sprite = null;
            }
        }

        void Update()
        {
            float age = Time.time - stateStart;
            switch (state)
            {
                case State.Igniting:
                    if (art.ignite.Length == 0 || age >= art.ignite.Length * IgniteFrameSeconds)
                        SetState(State.Burning);
                    else
                        overlay.sprite = art.ignite[Mathf.FloorToInt(age / IgniteFrameSeconds)];
                    break;
                case State.Burning:
                    if (age >= BurnSeconds)
                    {
                        SetState(State.Embers);
                        break;
                    }
                    if (burn.Length > 0)
                        overlay.sprite = burn[Mathf.FloorToInt(age / BurnFrameSeconds) % burn.Length];
                    if (Time.time >= nextHurt)
                    {
                        nextHurt = Time.time + HurtEvery;
                        HurtWhoStands();
                    }
                    if (Time.time >= nextSpread)
                    {
                        nextSpread = Time.time + SpreadEvery;
                        Flammables.IgniteNear(transform.position, SpreadRadius);
                    }
                    break;
                case State.Embers:
                    if (lit)
                        UpdateSparks();
                    if (!lit && age >= EmberSeconds)
                        SetState(State.Out);
                    else if (art.embers.Length > 0)
                    {
                        overlay.transform.localPosition = Vector3.zero;
                        overlay.sprite = art.embers[Mathf.FloorToInt(age / EmberFrameSeconds) % art.embers.Length];
                    }
                    break;
            }
        }

        // Quien pisa las llamas: el jugador se quema (un golpe de medio corazón y la barra de fuego; la esquiva pasa por encima) y los enemigos arden.
        void HurtWhoStands()
        {
            foreach (var hit in Physics2D.OverlapBoxAll(transform.position, Vector2.one * 0.9f, 0f))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerBurn>() is { } burning)
                {
                    if (burning.IsDodging)
                        continue;
                    burning.Feed(HurtEvery);
                    var health = burning.GetComponentInParent<Health>();
                    if (health != null && !health.IsDead)
                        health.TakeDamage(PlayerDamage);
                    continue;
                }

                var enemy = hit.GetComponentInParent<Health>();
                if (enemy != null && !enemy.IsDead && enemy.GetComponentInParent<Enemy>() != null)
                    Burn.Apply(enemy, 1, EnemyBurnSeconds);
            }
        }

        // Chispa de una casilla encendida: un destello y fuego en lo inflamable que tiene cerca.
        void UpdateSparks()
        {
            if (sparkRadius <= 0f || Time.time < nextSpark)
                return;
            nextSpark = Time.time + Random.Range(sparkEvery.x, sparkEvery.y);
            if (sparkGate != null && !sparkGate())
                return;
            FrameEffect.Play("CoalSpark", art.floatingEmbers, SparkFrameSeconds, FireOrder + 1, null, (Vector2)transform.position + Vector2.up * 0.6f);
            Flammables.IgniteNear(transform.position, sparkRadius);
        }
    }
}
