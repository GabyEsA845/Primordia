using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Comportamiento propio de un enemigo del piso 2 en adelante (el trilobite que embiste, la medusa que suelta anillos…).
    /// <see cref="EnemyAI"/> sigue llevando lo común (objetivo y señuelos, empujones y aturdimiento, ceguera, corrientes,
    /// fosas, separación entre enemigos) y, cuando el enemigo puede actuar, le pide a su cerebro la velocidad deseada.
    /// El cerebro también dice qué animación en bucle se ve (<see cref="AnimationState"/>) en lugar de Move.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public abstract class EnemyBrain : MonoBehaviour
    {
        protected EnemyAI AI { get; private set; }
        protected Enemy Enemy { get; private set; }
        protected Health Health { get; private set; }
        protected Rigidbody2D Body { get; private set; }
        protected EnemyAnimator Animator { get; private set; }

        protected virtual void Awake()
        {
            AI = GetComponent<EnemyAI>();
            Enemy = GetComponent<Enemy>();
            Health = GetComponent<Health>();
            Body = GetComponent<Rigidbody2D>();
            Animator = GetComponent<EnemyAnimator>();
        }

        /// <summary>
        /// Cada cuadro en que puede actuar (no está aturdido, cegado ni cayendo): devuelve la velocidad deseada.
        /// <paramref name="target"/> es a quién va (el jugador o un señuelo), o null si no hay nadie.
        /// </summary>
        public abstract Vector2 Think(Transform target, EnemyData data);

        /// <summary>Animación en bucle que se ve ahora en lugar de Move (null = Move).</summary>
        public virtual string AnimationState => null;

        /// <summary>Hacia dónde mira (para voltear el sprite). Null = lo de siempre (al objetivo o hacia donde se mueve).</summary>
        public virtual Vector2? Facing => null;

        /// <summary>False si ahora nada lo empuja ni lo aturde (la libélula en el aire: un golpe sin daño no debe frenarla).</summary>
        public virtual bool CanBePushed => true;

        /// <summary>Animación de daño que se ve ahora (null = Hurt): el sapo en el lodo la tiene propia.</summary>
        public virtual string HurtState => null;

        /// <summary>Animación de muerte que se ve ahora (null = Death).</summary>
        public virtual string DeathState => null;

        /// <summary>False mientras no debe verse la animación de daño (enterrado, en mitad de una embestida…).</summary>
        public virtual bool ShowsHurt => true;

        /// <summary>
        /// True mientras su animación se queda en el último cuadro (la Ottoia asomada tras morder): si la corta el daño,
        /// vuelve a ese cuadro en lugar de repetirla desde el principio.
        /// </summary>
        public virtual bool HoldsAnimationEnd => false;

        Vector2 wanderPoint;
        float wanderUntil;

        /// <summary>Pasea sin rumbo: va a un punto cercano a la vista y, al llegar o pasado un rato, elige otro.</summary>
        protected Vector2 Wander(Vector2 position, float speed)
        {
            if (Time.time >= wanderUntil || (wanderPoint - position).sqrMagnitude < 0.2f)
            {
                wanderPoint = position + Random.insideUnitCircle * 3f;
                var grid = NavigationGrid.Current;
                if (grid != null && !grid.HasLineOfSight(position, wanderPoint, AI.BodyRadius))
                    wanderPoint = position;
                wanderUntil = Time.time + Random.Range(1.5f, 3f);
            }
            Vector2 to = wanderPoint - position;
            return to.sqrMagnitude > 0.04f ? AI.Separated(to.normalized * speed, speed) : Vector2.zero;
        }
    }
}
