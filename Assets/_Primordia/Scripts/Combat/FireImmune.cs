using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>Marca de lo que no se quema: los enemigos de fuego y, con las Escamas carbonizadas, el jugador. El fuego lo ignora.</summary>
    public class FireImmune : MonoBehaviour
    {
        /// <summary>False si ahora no protege (el moco de la Piel anfibia sin cargar).</summary>
        public virtual bool IsImmune => true;

        public static bool Has(Component target)
        {
            if (target == null)
                return false;
            foreach (var immune in target.GetComponentsInParent<FireImmune>())
            {
                if (immune.IsImmune)
                    return true;
            }
            return false;
        }
    }
}
