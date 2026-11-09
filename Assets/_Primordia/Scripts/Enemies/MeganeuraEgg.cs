using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Racimo de huevos de la Meganeura (fase 2): aparece en la orilla de una charca, late en reposo y, si nadie lo rompe a tiempo,
    /// eclosiona y suelta una ninfa (en el cuadro 3). El jugador lo rompe a disparos o golpes (tiene unos pocos puntos de vida).
    /// Cuando no queda ninguno, la madre baja furiosa.
    /// </summary>
    public class MeganeuraEgg : MonoBehaviour
    {
        public const int MaxHealth = 4;
        const float AppearFrame = 0.1f, IdleFrame = 0.2f, HatchFrame = 0.09f, BreakFrame = 0.07f;
        const int NymphFrame = 3;

        static readonly List<MeganeuraEgg> alive = new();

        /// <summary>Racimos que aún no han eclosionado ni se han roto.</summary>
        public static IReadOnlyList<MeganeuraEgg> Alive => alive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => alive.Clear();

        /// <summary>Se lanza al resolverse el racimo: true si lo rompió el jugador, false si eclosionó.</summary>
        public event Action<MeganeuraEgg, bool> Resolved;

        MeganeuraBoss.Effects art;
        Enemy nymph;
        float hatchSeconds;
        SpriteRenderer body;
        Health health;
        bool done;

        /// <summary>Pone un racimo en ese punto (el pie del dibujo). <paramref name="hatchIn"/> = segundos hasta que eclosiona.</summary>
        internal static MeganeuraEgg Spawn(Transform parent, Vector2 position, MeganeuraBoss.Effects effects, Enemy nymphPrefab, float hatchIn)
        {
            var go = new GameObject("MeganeuraEggs") { layer = LayerMask.NameToLayer(CombatLayers.Enemy) };
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var egg = go.AddComponent<MeganeuraEgg>();
            egg.art = effects;
            egg.nymph = nymphPrefab;
            egg.hatchSeconds = hatchIn;
            egg.body = go.AddComponent<SpriteRenderer>();
            // En el suelo, como las ninfas: bajo la Meganeura.
            egg.body.sortingOrder = 5;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.6f;
            collider.offset = new Vector2(0f, 0.4f);
            egg.health = go.AddComponent<Health>();
            egg.health.SetMax(MaxHealth, healDifference: false);
            egg.health.ResetHealth();
            egg.StartCoroutine(egg.Run());
            return egg;
        }

        void OnEnable()
        {
            if (!alive.Contains(this))
                alive.Add(this);
            // Spawn rellena el arte justo después de AddComponent: con el arte ya puesto arranca la vida del racimo.
            if (art != null)
                StartCoroutine(Run());
        }

        void OnDisable() => alive.Remove(this);

        IEnumerator Run()
        {
            yield return Play(art.eggAppear, AppearFrame);
            float born = Time.time;
            while (!done && Time.time - born < hatchSeconds && !health.IsDead)
            {
                if (art.eggIdle.Length > 0)
                    body.sprite = art.eggIdle[Mathf.FloorToInt((Time.time - born) / IdleFrame) % art.eggIdle.Length];
                yield return null;
            }

            if (health.IsDead)
            {
                Finish(broken: true);
                foreach (var collider in GetComponents<Collider2D>())
                    collider.enabled = false;
                yield return Play(art.eggBreak, BreakFrame);
                Destroy(gameObject);
                yield break;
            }

            // Eclosiona: la ninfa sale en el cuadro 3 y el racimo deja de contar.
            for (int i = 0; i < art.eggHatch.Length; i++)
            {
                body.sprite = art.eggHatch[i];
                if (i == NymphFrame - 1)
                {
                    Finish(broken: false);
                    if (nymph != null)
                        Instantiate(nymph, transform.position + Vector3.up * 0.3f, Quaternion.identity, transform.parent);
                }
                yield return new WaitForSeconds(HatchFrame);
            }
            Destroy(gameObject);
        }

        IEnumerator Play(Sprite[] frames, float seconds)
        {
            foreach (var frame in frames)
            {
                body.sprite = frame;
                yield return new WaitForSeconds(seconds);
            }
        }

        void Finish(bool broken)
        {
            if (done)
                return;
            done = true;
            alive.Remove(this);
            foreach (var collider in GetComponents<Collider2D>())
                collider.enabled = broken ? collider.enabled : false;
            Resolved?.Invoke(this, broken);
        }

        /// <summary>Lo quita sin más (el jefe murió).</summary>
        internal void Vanish()
        {
            done = true;
            alive.Remove(this);
            Destroy(gameObject);
        }
    }
}
