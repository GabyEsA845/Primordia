using Primordia.Core;
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

        /// <summary>False mientras no debe verse la animación de daño (enterrado, en mitad de una embestida…).</summary>
        public virtual bool ShowsHurt => true;
    }
}
