using System.Collections;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Grieta de burbujas de metano del Burbujeo (sala especial del piso 3): en reposo es un agujero del suelo; cada pocos segundos (cada
    /// una a su ritmo) avisa 1 s (brilla y burbujea), suelta una burbuja grande que sube y, al llegar arriba, revienta en una explosión
    /// pequeña que daña a quien esté cerca (medio corazón al jugador, 3 a los enemigos) y prende lo inflamable que alcance. El dibujo del
    /// agujero es el de la fumarola del tramo.
    /// </summary>
    public class MethaneBubbler : MonoBehaviour
    {
        const float IdleFrameSeconds = 0.26f;
        const float WarnFrameSeconds = 0.12f;
        const float BubbleFrameSeconds = 0.13f;
        const float WarnSeconds = 1f;
        // Cada cuánto suelta una burbuja (entre esos segundos, desde el final de la anterior).
        static readonly Vector2 RestSeconds = new(4f, 7f);
        // La explosión: centro sobre la grieta, radio, daños.
        const float BlastHeight = 1.1f;
        const float BlastRadius = 1.7f;
        const int EnemyDamage = 3;
        const int PlayerDamage = 1;

        SpriteRenderer hole;
        SpriteRenderer bubble;
        Sprite[] idle = System.Array.Empty<Sprite>();
        Sprite[] warn = System.Array.Empty<Sprite>();
        Sprite[] bubbleFrames = System.Array.Empty<Sprite>();
        float startOffset;

        /// <summary>True durante el aviso y la subida de la burbuja.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Explosiones que ha hecho (para las pruebas).</summary>
        public int Blasts { get; private set; }

        /// <summary>Segundos desde el aviso hasta que revienta.</summary>
        public float FuseSeconds => WarnSeconds + bubbleFrames.Length * BubbleFrameSeconds;

        internal static MethaneBubbler Create(Transform parent, Vector2 position, Sprite[] idle, Sprite[] warn, Sprite[] bubbleFrames, float startOffset)
        {
            var go = new GameObject("MethaneBubbler");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var bubbler = go.AddComponent<MethaneBubbler>();
            bubbler.idle = idle;
            bubbler.warn = warn;
            bubbler.bubbleFrames = bubbleFrames;
            bubbler.startOffset = startOffset;

            bubbler.hole = go.AddComponent<SpriteRenderer>();
            bubbler.hole.sprite = idle.Length > 0 ? idle[0] : null;
            // Pegada al suelo, sobre los charcos y las rocas, como las fumarolas.
            bubbler.hole.sortingOrder = -3;

            var bubbleObject = new GameObject("Bubble");
            bubbleObject.transform.SetParent(go.transform, false);
            bubbler.bubble = bubbleObject.AddComponent<SpriteRenderer>();
            bubbler.bubble.sortingOrder = 6;
            return bubbler;
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(startOffset);
            while (true)
            {
                IsActive = false;
                bubble.sprite = null;
                yield return Loop(idle, IdleFrameSeconds, Random.Range(RestSeconds.x, RestSeconds.y));
                IsActive = true;
                yield return Loop(warn, WarnFrameSeconds, WarnSeconds);
                for (int i = 0; i < bubbleFrames.Length; i++)
                {
                    bubble.sprite = bubbleFrames[i];
                    yield return new WaitForSeconds(BubbleFrameSeconds);
                }
                bubble.sprite = null;
                Explosion.Detonate((Vector2)transform.position + Vector2.up * BlastHeight, BlastRadius, EnemyDamage, PlayerDamage, false);
                Blasts++;
            }
        }

        IEnumerator Loop(Sprite[] frames, float frameSeconds, float seconds)
        {
            if (frames.Length == 0)
            {
                yield return new WaitForSeconds(seconds);
                yield break;
            }
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                hole.sprite = frames[Mathf.FloorToInt(t / frameSeconds) % frames.Length];
                yield return null;
            }
        }
    }
}
