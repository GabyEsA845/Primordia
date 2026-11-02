using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>Marca de lo que no se quema: los enemigos de fuego y, con las Escamas carbonizadas, el jugador. El fuego lo ignora.</summary>
    public class FireImmune : MonoBehaviour
    {
        public static bool Has(Component target) => target != null && target.GetComponentInParent<FireImmune>() != null;
    }
}
