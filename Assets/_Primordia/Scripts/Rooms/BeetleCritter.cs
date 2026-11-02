using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Escarabajo decorativo del piso 3: se queda quieto y, cuando el jugador se acerca, corre en dirección contraria sin salirse
    /// del suelo libre. No hace daño ni bloquea nada (no tiene colisionador).
    /// </summary>
    public class BeetleCritter : MonoBehaviour
    {
        const float FleeDistance = 3.2f;
        const float Speed = 3f;
        const float IdleFrameSeconds = 0.4f;
        const float RunFrameSeconds = 0.07f;

        SpriteRenderer body;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] run = System.Array.Empty<Sprite>();
        Transform player;
        float phase;

        internal static BeetleCritter Create(Transform parent, Vector2 position, Sprite[] idle, Sprite[] run)
        {
            var obj = new GameObject("Beetle");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            renderer.sortingOrder = -2;
            var beetle = obj.AddComponent<BeetleCritter>();
            beetle.body = renderer;
            beetle.idle = idle;
            beetle.run = run;
            beetle.phase = Random.value * 5f;
            return beetle;
        }

        void Update()
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
            }

            Vector2 away = player != null ? (Vector2)transform.position - (Vector2)player.position : Vector2.zero;
            bool flee = player != null && away.sqrMagnitude < FleeDistance * FleeDistance;
            if (flee && TryMove(away.normalized))
            {
                if (run.Length > 0)
                    body.sprite = run[Mathf.FloorToInt(Time.time / RunFrameSeconds) % run.Length];
                body.flipX = away.x < 0f;
                return;
            }

            if (idle.Length > 0)
                body.sprite = idle[Mathf.FloorToInt((Time.time + phase) / IdleFrameSeconds) % idle.Length];
        }

        // Da un paso si hay suelo libre delante; si no, prueba solo en x o solo en y.
        bool TryMove(Vector2 direction)
        {
            foreach (var option in new[] { direction, new Vector2(direction.x, 0f), new Vector2(0f, direction.y) })
            {
                if (option.sqrMagnitude < 0.01f)
                    continue;
                var next = (Vector2)transform.position + option.normalized * Speed * Time.deltaTime;
                if (IsFree(next) && IsFree(next + option.normalized * 0.3f))
                {
                    transform.position = next;
                    return true;
                }
            }
            return false;
        }

        static bool IsFree(Vector2 position) =>
            Pits.Map == null || Pits.Map.Get(NavigationGrid.CellOf(position)) == FloorMap.Kind.Floor;
    }
}
