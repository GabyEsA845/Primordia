using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// La gota de ámbar lanzada por la Gota de ámbar: vuela recta hasta el primer enemigo y lo encierra en un bloque
    /// (<see cref="AmberCocoon"/>); se deshace contra una pared o al acabar su alcance.
    /// </summary>
    public class AmberDropShot : MonoBehaviour
    {
        public struct Settings
        {
            public float speed;
            public float range;
            public float stunSeconds;
            public float bossSeconds;
            public bool kills;
            public int damagePerSecond;
            public float shardRadius;
            public int shardDamage;
            public float shardPush;
            public int corpseHealth;
            public float corpseSeconds;
            public AmberShellArt shell;
            public Sprite[] dropFrames;
            public Sprite[] hitFrames;
            public Sprite[] starFrames;
            public Sprite[] crystalFrames;
            public Sprite[] fossilFrames;
        }

        const float Radius = 0.22f;
        const int SortingOrder = 21;

        Settings settings;
        Vector2 direction;
        float travelled;
        SpriteRenderer renderer2D;

        public static AmberDropShot Fire(Vector2 origin, Vector2 direction, Settings settings)
        {
            var go = new GameObject("AmberDrop");
            go.transform.position = origin;
            var shot = go.AddComponent<AmberDropShot>();
            shot.settings = settings;
            shot.direction = direction.normalized;
            shot.renderer2D = go.AddComponent<SpriteRenderer>();
            shot.renderer2D.sortingOrder = SortingOrder;
            shot.renderer2D.sprite = settings.dropFrames != null && settings.dropFrames.Length > 0 ? settings.dropFrames[0] : null;
            if (settings.dropFrames != null && settings.dropFrames.Length > 1)
                go.AddComponent<SpriteFramePlayer>().Play(shot.renderer2D, settings.dropFrames, 0.08f, newLoop: true);
            go.transform.right = shot.direction;
            return shot;
        }

        void Update()
        {
            float step = settings.speed * Time.deltaTime;
            Vector2 position = transform.position;
            foreach (var hit in Physics2D.CircleCastAll(position, Radius, direction, step))
            {
                if (hit.collider.isTrigger || hit.collider.GetComponentInParent<Primordia.Player.PlayerController>() != null)
                    continue;

                var enemy = hit.collider.GetComponentInParent<Enemy>();
                var health = hit.collider.GetComponentInParent<Health>();
                if (enemy != null && health != null && !health.IsDead && health.GetComponentInParent<AmberCocoon>() == null)
                {
                    Impact(hit.point);
                    AmberCocoon.Encase(enemy, health, settings);
                    return;
                }
                // Pared, roca u otra cosa sólida (también un bloque de ámbar): se deshace.
                if (enemy == null)
                {
                    Impact(hit.point);
                    return;
                }
            }

            transform.position = position + direction * step;
            travelled += step;
            if (travelled >= settings.range)
                Impact(transform.position);
        }

        void Impact(Vector2 point)
        {
            FrameEffect.Play("AmberDropHit", settings.hitFrames, 0.06f, SortingOrder, null, point);
            Destroy(gameObject);
        }
    }
}
