using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Sigillaria quemada de la sala de la Meganeura: tronco sólido y pararrayos de un solo uso (fase 3). En pie, atrae el rayo grande
    /// que cae cerca (<see cref="Strike"/>) y ningún rayo suelto cae a menos de <see cref="SafeRadius"/>. Tras el rayo: se raja y
    /// prende (Struck → Burning), la lluvia la va apagando (Smolder) y queda partida para siempre (Split).
    /// </summary>
    public class BossSigillaria : MonoBehaviour
    {
        /// <summary>Distancia mínima a la que puede caer un rayo suelto (u) para no romperla antes de que el jugador lleve al jefe.</summary>
        public const float SafeRadius = 3.5f;
        /// <summary>A qué distancia del rayo grande lo atrae hacia sí (u).</summary>
        public const float AttractRadius = 3f;

        const float StruckFrameSeconds = 0.08f, BurningSeconds = 3f, BurningFrameSeconds = 0.11f, SmolderSeconds = 3f, SmolderFrameSeconds = 0.14f;

        static readonly List<BossSigillaria> All = new();

        /// <summary>Sigillarias de la sala que aún no han atraído un rayo.</summary>
        public static IReadOnlyList<BossSigillaria> Standing => All;

        public Vector2 Position => transform.position;

        /// <summary>True una vez que atrajo su rayo (ya no vuelve a atraer ninguno).</summary>
        public bool Struck { get; private set; }

        void OnEnable()
        {
            if (!Struck)
                All.Add(this);
        }

        void OnDisable() => All.Remove(this);

        /// <summary>True si un rayo suelto puede caer en <paramref name="point"/> sin dañar ninguna sigillaria en pie.</summary>
        public static bool IsSafeSpot(Vector2 point)
        {
            foreach (var sigillaria in All)
            {
                if (Vector2.Distance(sigillaria.Position, point) < SafeRadius)
                    return false;
            }
            return true;
        }

        /// <summary>La sigillaria en pie más cercana a <paramref name="point"/> dentro de <paramref name="radius"/>, o null.</summary>
        public static BossSigillaria Nearest(Vector2 point, float radius)
        {
            BossSigillaria best = null;
            float bestDistance = radius;
            foreach (var sigillaria in All)
            {
                float distance = Vector2.Distance(sigillaria.Position, point);
                if (distance <= bestDistance)
                {
                    best = sigillaria;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Recibe el rayo: de un solo uso (sale de la lista de pararrayos en pie).</summary>
        public void Strike(MeganeuraBoss.Effects effects)
        {
            if (Struck)
                return;
            Struck = true;
            All.Remove(this);
            StartCoroutine(Burn(effects));
        }

        IEnumerator Burn(MeganeuraBoss.Effects effects)
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
                yield break;
            foreach (var frame in effects.sigillariaStruck)
            {
                renderer.sprite = frame;
                yield return new WaitForSeconds(StruckFrameSeconds);
            }
            yield return Loop(renderer, effects.sigillariaBurning, BurningSeconds, BurningFrameSeconds);
            yield return Loop(renderer, effects.sigillariaSmolder, SmolderSeconds, SmolderFrameSeconds);
            if (effects.sigillariaSplit.Length > 0)
                renderer.sprite = effects.sigillariaSplit[0];
        }

        static IEnumerator Loop(SpriteRenderer renderer, Sprite[] frames, float seconds, float frameSeconds)
        {
            if (frames.Length == 0)
                yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                renderer.sprite = frames[Mathf.FloorToInt(t / frameSeconds) % frames.Length];
                yield return null;
            }
        }
    }
}
