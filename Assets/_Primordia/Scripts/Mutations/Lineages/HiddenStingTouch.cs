using Primordia.Combat;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Picadura oculta (Escorpión): mientras el jugador está camuflado, sus golpes envenenan el triple. Como el
    /// daño, el proyectil se lleva el multiplicador al salir, aunque el disparo deje al jugador a la vista.
    /// </summary>
    public class HiddenStingTouch : MonoBehaviour
    {
        Weapon weapon;
        float multiplier = 3f;
        Camouflage camouflage;

        public void Configure(Weapon newWeapon, float newMultiplier)
        {
            weapon = newWeapon;
            multiplier = newMultiplier;
        }

        void Update()
        {
            if (weapon == null)
                return;

            // El Camuflaje es un componente de la Piel: puede llegar o irse con ella.
            if (camouflage == null)
                camouflage = GetComponent<Camouflage>();

            if (camouflage != null && camouflage.IsConcealed)
                weapon.SetPoisonBonus(this, multiplier);
            else
                weapon.ClearPoisonBonus(this);
        }

        void OnDestroy()
        {
            if (weapon != null)
                weapon.ClearPoisonBonus(this);
        }
    }
}
