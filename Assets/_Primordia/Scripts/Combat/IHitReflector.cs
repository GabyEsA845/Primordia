using Primordia.Core;

namespace Primordia.Combat
{
    /// <summary>
    /// Algo en el jugador que puede devolver un ataque antes de que le haga daño (la esquiva justa de la Cutícula de
    /// Marrella). Lo consultan los proyectiles enemigos, el daño por contacto, los golpes cuerpo a cuerpo y la ameba gigante.
    /// </summary>
    public interface IHitReflector
    {
        /// <summary>Un proyectil enemigo lo alcanza. True si lo devolvió: el proyectil desaparece sin hacer daño.</summary>
        bool TryReflect(Projectile projectile);

        /// <summary>Un golpe cuerpo a cuerpo o de contacto de <paramref name="attacker"/>. True si lo devolvió (no hace daño).</summary>
        bool TryReflect(Health attacker);
    }
}
