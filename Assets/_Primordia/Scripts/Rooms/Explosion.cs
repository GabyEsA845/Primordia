using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Explosión de gas del piso 3 (odre de metano): daña a todo lo que tiene vida en su radio (6 a los enemigos y a lo demás, 3 al
    /// jugador), prende lo inflamable que haya cerca (helechos, maleza, niebla) y deja un fuego corto en el centro. Lo que no se
    /// quema (<see cref="FireImmune"/>) no la sufre. Un odre dañado por otra explosión revienta a su vez, así que se encadenan.
    /// </summary>
    public static class Explosion
    {
        public const float Radius = 2.5f;
        public const int EnemyDamage = 6;
        public const int PlayerDamage = 3;
        public const float GroundFireSeconds = 2f;

        public static void Detonate(Vector2 center) => Detonate(center, Radius, EnemyDamage, PlayerDamage, true);

        /// <summary>
        /// Explosión con otro radio y otros daños (las burbujas del Burbujeo, más pequeñas que un odre). Con <paramref name="leaveFire"/>
        /// deja un fuego en el suelo; prende siempre lo inflamable que alcance.
        /// </summary>
        public static void Detonate(Vector2 center, float radius, int enemyDamage, int playerDamage, bool leaveFire)
        {
            var kit = ReactionKit.Current;
            if (kit != null && kit.Explosion.Length > 0)
                SpriteFramePlayer.PlayOnce(kit.Explosion, kit.ExplosionFrameSeconds, center, 15);

            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health) || FireImmune.Has(health) || ExplosionImmune.Has(health))
                    continue;
                bool isPlayer = health.GetComponentInParent<PlayerController>() != null;
                health.TakeDamage(isPlayer ? playerDamage : enemyDamage);
            }

            Flammables.IgniteNear(center, radius);
            if (leaveFire)
                GroundFire.Spawn(center + Vector2.down * 0.3f, GroundFireSeconds);
        }
    }
}
