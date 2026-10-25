using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Algo que una corriente puede arrastrar sin dejar de actuar (los enemigos dentro del remolino de la Corona de brazos):
    /// la velocidad se suma a la suya mientras se siga llamando.
    /// </summary>
    public interface IPullable
    {
        void Pull(Vector2 velocity);
    }
}
