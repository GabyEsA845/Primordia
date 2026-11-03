using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Planta venenosa del piso 3: un bulbo lima sólido que respira y gotea. Reventarlo (un golpe, un disparo, una explosión o
    /// fuego) suelta un charco de veneno grande y salpica a los de al lado (1 de daño y veneno); queda la cáscara abierta y,
    /// 25 s después, rebrota y vuelve a estar lista. Tiene un colisionador de bloqueo y otro por equipo para los golpes cuerpo
    /// a cuerpo, como el odre de metano.
    /// </summary>
    public class PoisonBulb : MonoBehaviour, IFlammable
    {
        const float IdleFrameSeconds = 0.26f;
        const float BurstFrameSeconds = 0.07f;
        const float RegrowFrameSeconds = 0.14f;
        public const float RegrowAfterSeconds = 25f;
        // Cuadro de la hinchazón en el que sale el charco.
        const int PuddleFrame = 2;
        // Salpicadura al reventar.
        const float SplashRadius = 1.6f;
        static readonly Vector2 BodySize = new(0.75f, 0.6f);
        // Lo que recibe los golpes cubre todo el dibujo (el bloqueo, solo la base): si no, los ataques atraviesan la mitad de arriba.
        static readonly Vector2 HitSize = new(1.2f, 1.3f);
        const float HitCenterY = 0.8f;

        enum State
        {
            Idle,
            Bursting,
            Stump,
            Regrowing
        }

        SpriteRenderer body;
        PoisonSpec art;
        Health health;
        Collider2D[] colliders = System.Array.Empty<Collider2D>();
        State state;
        float phase;

        /// <summary>Centro del bulbo (un poco por encima de su base).</summary>
        public Vector2 Position => (Vector2)transform.position + Vector2.up * 0.45f;

        public bool CanIgnite => state == State.Idle;

        internal static PoisonBulb Create(Transform parent, Vector2 position, PoisonSpec art)
        {
            var obj = new GameObject("PoisonBulb");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = art.bulbIdle.Length > 0 ? art.bulbIdle[0] : null;
            renderer.sortingOrder = 4;

            var blocker = obj.AddComponent<BoxCollider2D>();
            blocker.size = BodySize;
            blocker.offset = new Vector2(0f, BodySize.y / 2f);

            var health = obj.AddComponent<Health>();
            health.SetMax(1, healDifference: false);

            var bulb = obj.AddComponent<PoisonBulb>();
            bulb.body = renderer;
            bulb.art = art;
            bulb.health = health;
            bulb.Subscribe();
            bulb.colliders = new Collider2D[]
            {
                blocker,
                AddMeleeTarget(obj.transform, CombatLayers.Enemy),
                AddMeleeTarget(obj.transform, CombatLayers.Player),
            };
            return bulb;
        }

        static Collider2D AddMeleeTarget(Transform parent, string layerName)
        {
            var obj = new GameObject($"Hit_{layerName}");
            obj.transform.SetParent(parent, false);
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                obj.layer = layer;
            var box = obj.AddComponent<BoxCollider2D>();
            box.size = HitSize;
            box.offset = new Vector2(0f, HitCenterY);
            // Solo recibe golpes por capa: no empuja ni estorba a nadie (el bloqueo ya lo hace el colisionador de la base).
            box.excludeLayers = LayerMask.GetMask(CombatLayers.Player, CombatLayers.Enemy);
            return box;
        }

        void Awake() => phase = Random.value * 10f;

        void OnEnable()
        {
            Flammables.Register(this);
            Subscribe();
        }

        void OnDisable()
        {
            Flammables.Unregister(this);
            if (health != null)
                health.Died -= Burst;
        }

        void Subscribe()
        {
            if (health == null)
                return;
            health.Died -= Burst;
            health.Died += Burst;
        }

        void Update()
        {
            if (state != State.Idle || body == null || art.bulbIdle.Length == 0)
                return;
            body.sprite = art.bulbIdle[Mathf.FloorToInt((Time.time + phase) / IdleFrameSeconds) % art.bulbIdle.Length];
        }

        /// <summary>El fuego lo revienta.</summary>
        public void Ignite() => Burst();

        void Burst()
        {
            if (state != State.Idle)
                return;
            state = State.Bursting;
            Flammables.Unregister(this);
            SetColliders(false);
            StartCoroutine(Run());
        }

        void SetColliders(bool enabled)
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                    collider.enabled = enabled;
            }
        }

        IEnumerator Run()
        {
            for (int i = 0; i < art.bulbBurst.Length; i++)
            {
                body.sprite = art.bulbBurst[i];
                if (i == PuddleFrame)
                    Release();
                yield return new WaitForSeconds(BurstFrameSeconds);
            }
            if (art.bulbBurst.Length <= PuddleFrame)
                Release();

            // La cáscara abierta mientras espera el rebrote.
            state = State.Stump;
            body.sprite = art.bulbStump;
            yield return new WaitForSeconds(RegrowAfterSeconds);

            state = State.Regrowing;
            foreach (var frame in art.bulbRegrow)
            {
                body.sprite = frame;
                yield return new WaitForSeconds(RegrowFrameSeconds);
            }

            // Vuelve a estar lista: vida entera, sólida y otra vez inflamable.
            health.ResetHealth();
            SetColliders(true);
            state = State.Idle;
            Flammables.Register(this);
        }

        // El charco grande en la base y la salpicadura a los de al lado.
        void Release()
        {
            PoisonPuddle.Spawn((Vector2)transform.position + Vector2.up * 0.2f);

            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(Position, SplashRadius))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerToxin>() is { } player)
                {
                    if (!player.IsDodging)
                        player.TouchPuddle();
                    continue;
                }

                var other = hit.GetComponentInParent<Health>();
                if (other == null || other == health || other.IsDead || !seen.Add(other) || other.GetComponentInParent<Enemy>() == null)
                    continue;
                other.TakeDamage(1);
                Toxins.PoisonEnemy(other, 1, 3f);
            }
        }
    }
}
