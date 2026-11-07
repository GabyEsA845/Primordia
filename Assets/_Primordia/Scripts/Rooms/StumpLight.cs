using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Rayo de luz del Tocón hueco (piso 3): entra por arriba del tocón y cae sobre el cofre de cono con su charco en el suelo y motas de
    /// polvo (8 cuadros, 140 ms). Es translúcido y lleva una luz cálida para que el cofre se vea iluminado.
    /// </summary>
    public class StumpLight : MonoBehaviour
    {
        const float FrameSeconds = 0.14f;
        static readonly Color Warm = new(1f, 0.86f, 0.5f);

        SpriteRenderer body;
        Sprite[] frames = System.Array.Empty<Sprite>();
        float phase;

        /// <summary><paramref name="puddle"/> es el centro del charco, el pivote del dibujo.</summary>
        internal static StumpLight Create(Transform parent, Vector2 puddle, Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
                return null;
            var go = new GameObject("StumpLight");
            go.transform.SetParent(parent, false);
            go.transform.position = puddle;
            var light = go.AddComponent<StumpLight>();
            light.frames = frames;
            light.phase = Random.value * 5f;
            light.body = go.AddComponent<SpriteRenderer>();
            light.body.sprite = frames[0];
            light.body.sortingOrder = 7;
            light.body.color = new Color(1f, 1f, 1f, 0.9f);
            Ambience.CreateGlow(go.transform, puddle + Vector2.up * 0.6f, new Glow(null, Warm, 3.2f, new Vector2(0.9f, 1.2f), 4f), 0f);
            return light;
        }

        void Update() => body.sprite = frames[Mathf.FloorToInt((Time.time + phase) / FrameSeconds) % frames.Length];
    }
}
