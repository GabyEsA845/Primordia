using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>China decorativa del derrumbe de la cueva: cae por la pantalla hasta un punto del suelo y suelta un polvillo. Sin daño.</summary>
    public class FallingPebble : MonoBehaviour
    {
        public const int SortingOrder = 20;
        const float Speed = 10f;

        float landY;
        Sprite[] puffFrames;

        public static FallingPebble Spawn(Transform parent, Vector2 start, float landY, Sprite sprite, Sprite[] puffFrames)
        {
            var go = new GameObject("Pebble");
            go.transform.SetParent(parent, true);
            go.transform.position = start;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingOrder;
            var pebble = go.AddComponent<FallingPebble>();
            pebble.landY = landY;
            pebble.puffFrames = puffFrames;
            return pebble;
        }

        void Update()
        {
            var position = transform.position;
            position.y -= Speed * Time.deltaTime;
            if (position.y <= landY)
            {
                Sfx.Play(Sounds.CavePebble);
                FrameEffect.Play("PebblePuff", puffFrames, 0.07f, FallingRock.ShadowOrder, null, new Vector2(position.x, landY));
                Destroy(gameObject);
                return;
            }
            transform.position = position;
        }
    }
}
