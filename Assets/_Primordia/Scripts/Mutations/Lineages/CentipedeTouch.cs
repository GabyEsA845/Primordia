using Primordia.Combat;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cien patas (Ciempiés): cada aterrizaje de las Patas de salto deja un charco de veneno que no te daña, y eres inmune a todo
    /// veneno y gas tóxico (<see cref="IFullPoisonImmune"/>: charcos y nube de las plantas, el veneno de los ataques y tu propio metano).
    /// </summary>
    public class CentipedeTouch : MonoBehaviour, IFullPoisonImmune
    {
        float puddleSeconds = 4f;
        JumpingLegs legs;

        public bool ImmuneToPlantPoison => true;

        /// <summary>Charcos que ha dejado (para las pruebas, que no cargan el arte del veneno).</summary>
        public int PuddlesLeft { get; private set; }

        public void Configure(float newPuddleSeconds) => puddleSeconds = Mathf.Max(0.5f, newPuddleSeconds);

        // Las Patas de salto pueden ponerse después que el linaje: se buscan hasta encontrarlas.
        void Update()
        {
            if (legs != null)
                return;
            legs = GetComponent<JumpingLegs>();
            if (legs != null)
                legs.Landed += OnLanded;
        }

        void OnLanded(Vector2 point)
        {
            PuddlesLeft++;
            PoisonPuddle.Spawn(point, puddleSeconds, harmsPlayer: false);
        }

        void OnDestroy()
        {
            if (legs != null)
                legs.Landed -= OnLanded;
        }
    }
}
