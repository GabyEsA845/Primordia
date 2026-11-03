using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Marca de quien no sufre las explosiones de gas (el odre de metano y la nube del Saco de metano): el jugador mientras lleva el
    /// Saco de metano. A diferencia de <see cref="FireImmune"/>, no protege del fuego en el suelo ni del quemado.
    /// </summary>
    public class ExplosionImmune : MonoBehaviour
    {
        public static bool Has(Component target) => target != null && target.GetComponentInParent<ExplosionImmune>() != null;
    }
}
