using System;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Un latigazo de <see cref="FlagellaLash"/>: reproduce una vez sus cuadros siguiendo al enemigo y, a los
    /// <c>strikeDelay</c> segundos, llama a quien lo lanzó para que haga el daño (que puede cambiar de enemigo si murió).
    /// Sin cuadros (pruebas o arte sin importar) el daño entra igual. También sirve para el destello de listo, sin daño.
    /// </summary>
    public class FlagellumStrike : MonoBehaviour
    {
        Health target;
        Vector2 lastPosition;
        Sprite[] frames;
        float frameDuration;
        float strikeDelay;
        Func<Health, Vector2, Health> onStrike;
        SpriteRenderer spriteRenderer;
        float elapsed;
        bool struck;

        /// <summary>Latigazo junto a ese enemigo; <paramref name="onStrike"/> recibe el enemigo y dónde estaba, y devuelve a quién azotó.</summary>
        internal static FlagellumStrike Spawn(Health target, Sprite[] frames, float frameDuration, float strikeDelay, int sortingOrder,
            Func<Health, Vector2, Health> onStrike)
        {
            var strike = Create("FlagellaLash", frames, frameDuration, sortingOrder, null);
            strike.target = target;
            strike.lastPosition = CenterOf(target);
            strike.transform.position = strike.lastPosition;
            strike.strikeDelay = strikeDelay;
            strike.onStrike = onStrike;
            return strike;
        }

        /// <summary>Cuadros una vez, colgando de ese padre (el destello sobre el jugador). Sin cuadros no crea nada.</summary>
        internal static void PlayOnce(string name, Sprite[] frames, float frameDuration, Transform parent, int sortingOrder)
        {
            if (frames == null || frames.Length == 0)
                return;
            var strike = Create(name, frames, frameDuration, sortingOrder, parent);
            strike.struck = true;
        }

        static FlagellumStrike Create(string name, Sprite[] frames, float frameDuration, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
            }
            var strike = go.AddComponent<FlagellumStrike>();
            strike.frames = frames ?? Array.Empty<Sprite>();
            strike.frameDuration = Mathf.Max(0.01f, frameDuration);
            if (strike.frames.Length > 0)
            {
                strike.spriteRenderer = go.AddComponent<SpriteRenderer>();
                strike.spriteRenderer.sortingOrder = sortingOrder;
                strike.spriteRenderer.sprite = strike.frames[0];
            }
            return strike;
        }

        static Vector2 CenterOf(Health health)
        {
            if (health == null)
                return Vector2.zero;
            var collider = health.GetComponentInChildren<Collider2D>();
            return collider != null ? (Vector2)collider.bounds.center : (Vector2)health.transform.position;
        }

        float Length => Mathf.Max(frames.Length * frameDuration, struck ? 0f : strikeDelay);

        void Update()
        {
            elapsed += Time.deltaTime;

            if (target != null && !target.IsDead)
                lastPosition = CenterOf(target);
            if (onStrike != null)
                transform.position = lastPosition;

            if (!struck && elapsed >= strikeDelay)
            {
                struck = true;
                var hit = onStrike?.Invoke(target, lastPosition);
                // Si fue a otro enemigo, los flagelos lo siguen a él.
                if (hit != null && hit != target)
                {
                    target = hit;
                    lastPosition = CenterOf(hit);
                }
            }

            if (spriteRenderer != null)
                spriteRenderer.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed / frameDuration))];

            if (elapsed >= Length)
                Destroy(gameObject);
        }
    }
}
