using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Niebla de metano a ras de suelo (piso 3): una mancha translúcida y quieta que se enciende con fuego o una explosión
    /// cerca. El fogonazo la recorre y salta a las manchas vecinas (la cadena corre por la sala): 3 de daño y llamas a los
    /// enemigos, ½ corazón al jugador con la barra de quemado a la mitad. Después la mancha desaparece.
    /// </summary>
    public class MethaneMist : MonoBehaviour, IFlammable, IToxifiable
    {
        // Lo que cubre la mancha, el retardo hasta que el fogonazo daña (cuadro 1 de 70 ms) y hasta que salta a las vecinas.
        static readonly Vector2 Area = new(3.6f, 1.4f);
        const float DamageDelay = 0.07f;
        const float SpreadDelay = 0.12f;
        const float SpreadRadius = 3.6f;
        const int EnemyDamage = 3;
        const int PlayerDamage = 1;
        const float PlayerBurnBar = 0.5f;
        const float EnemyBurnSeconds = 3f;

        SpriteRenderer body;
        bool lit;

        public Vector2 Position => transform.position;

        public bool CanIgnite => !lit;

        public bool CanToxify => !lit;

        public Bounds ToxifyBounds => new((Vector2)transform.position + Vector2.up * Area.y / 2f, Area);

        /// <summary>Aguijón de gas: la mancha de niebla se vuelve una nube tóxica en lugar de arder.</summary>
        public void Toxify()
        {
            if (lit)
                return;
            lit = true;
            Flammables.Unregister(this);
            ToxicCloud.Spawn(transform.position);
            Destroy(gameObject);
        }

        internal static MethaneMist Create(Transform parent, Vector2 position, Sprite[] frames, float phase)
        {
            var obj = new GameObject("MethaneMist");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            // Pegada al suelo: por debajo de todo lo que se mueve.
            renderer.sortingOrder = 2;
            var mist = obj.AddComponent<MethaneMist>();
            mist.body = renderer;
            obj.AddComponent<SpriteFramePlayer>().Play(renderer, frames, 0.14f, newLoop: true);
            return mist;
        }

        void OnEnable() => Flammables.Register(this);

        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (lit)
                return;
            lit = true;
            Flammables.Unregister(this);
            StartCoroutine(Flash());
        }

        System.Collections.IEnumerator Flash()
        {
            var kit = ReactionKit.Current;
            // La mancha se enciende de un lado a otro: un fogonazo a cada lado.
            if (body != null)
                body.enabled = false;
            if (kit != null && kit.MistIgnite.Length > 0)
            {
                foreach (float x in new[] { -1f, 1f })
                    SpriteFramePlayer.PlayOnce(kit.MistIgnite, kit.MistIgniteFrameSeconds, (Vector2)transform.position + Vector2.right * x, 6);
            }

            yield return new WaitForSeconds(DamageDelay);
            Hurt();

            yield return new WaitForSeconds(SpreadDelay - DamageDelay);
            Flammables.IgniteNear(transform.position, SpreadRadius);
            if (kit != null && kit.MistFlare.Length > 0)
                SpriteFramePlayer.PlayOnce(kit.MistFlare, kit.MistIgniteFrameSeconds, transform.position, 7);

            yield return new WaitForSeconds(0.4f);
            Destroy(gameObject);
        }

        void Hurt()
        {
            var seen = new HashSet<Health>();
            var center = (Vector2)transform.position + Vector2.up * Area.y / 2f;
            foreach (var hit in Physics2D.OverlapBoxAll(center, Area, 0f))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health) || FireImmune.Has(health))
                    continue;

                if (health.GetComponentInParent<PlayerController>() != null)
                {
                    // El jugador que cruza el fogonazo no esquivando se chamusca.
                    if (health.TryGetComponent<PlayerBurn>(out var burn) && burn.IsDodging)
                        continue;
                    health.TakeDamage(PlayerDamage);
                    if (burn != null)
                        burn.SetOnFire(PlayerBurnBar);
                }
                else if (health.GetComponentInParent<Enemy>() != null)
                {
                    health.TakeDamage(EnemyDamage);
                    Burn.Apply(health, 1, EnemyBurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
                }
            }
        }
    }
}
