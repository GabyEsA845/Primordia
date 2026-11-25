using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>La copa y la seda dejan ver a los actores del suelo, incluidos los de varios colliders.</summary>
    public class MegarachneBossCanopy : MonoBehaviour
    {
        public const float CoveredAlpha = 0.45f;
        SpriteRenderer crown, nest;
        SpriteRenderer[] cableRoots;
        readonly Collider2D[] occupants = new Collider2D[128];
        public float Alpha => crown != null ? crown.color.a : 1f;
        internal void Configure(SpriteRenderer tree, SpriteRenderer web, SpriteRenderer[] roots)
        {
            crown = tree;
            nest = web;
            cableRoots = roots;
        }
        void LateUpdate() => Refresh();
        internal void ShowTree(Sprite[] frames, float frame, bool loop)
        {
            if (frames.Length == 0 || crown == null) return;
            int index = Mathf.Max(0, Mathf.FloorToInt(frame));
            crown.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }
        internal void ShowTrunk(Sprite[] frames, float frame, bool loop)
        {
            if (frames.Length == 0) return;
            var trunk = transform.Find("BaseTronco").GetComponent<SpriteRenderer>();
            int index = Mathf.Max(0, Mathf.FloorToInt(frame));
            trunk.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        }
        internal void ShowWeb(Sprite[] frames, float frame)
        {
            if (nest == null || frames.Length == 0) return;
            int index = Mathf.Max(0, Mathf.FloorToInt(frame));
            nest.enabled = index < frames.Length;
            nest.sprite = frames[Mathf.Min(index, frames.Length - 1)];
        }
        public void Refresh()
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * 4f;
            int count = Physics2D.OverlapCircle(center, 4f, new ContactFilter2D().NoFilter(), occupants);
            bool covered = false;
            for (int i = 0; i < count; i++)
            {
                var collider = occupants[i];
                var enemy = collider.GetComponentInParent<Enemy>();
                var player = collider.GetComponentInParent<PlayerController>();
                if (enemy == null && player == null && collider.GetComponentInParent<MegarachneBrood>() == null) continue;
                var mega = collider.GetComponentInParent<MegarachneBoss>();
                if (mega != null && (mega.CurrentMode == MegarachneBoss.Mode.OnCable || (mega.IsEnraging && mega.VisualHeight > 0f))) continue;
                Vector2 delta = (Vector2)collider.bounds.center - center;
                if (delta.x * delta.x / 16f + delta.y * delta.y / (2.75f * 2.75f) <= 1f)
                { covered = true; break; }
            }
            float alpha = covered ? CoveredAlpha : 1f;
            SetAlpha(crown, alpha);
            SetAlpha(nest, alpha);
            if (cableRoots != null)
                foreach (var root in cableRoots) SetAlpha(root, alpha);
        }
        static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null) return;
            var color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
