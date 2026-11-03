using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Ventosa giratoria (Lamprea): mientras giras con la Peonza, cada parásito que pegas pega otro al enemigo más cercano a ese,
    /// y el máximo de parásitos vivos sube a 6 (3 de base). Al dejar de girar el máximo vuelve a 3: los que ya hay se quedan, pero
    /// no entran más hasta que bajen. Usa el parásito grande de la Lamprea (<see cref="ParasiteBenefit.SetArt"/>).
    /// </summary>
    public class LampreyTouch : MonoBehaviour
    {
        // Segundos que se mantiene el máximo alto tras soltar el giro, para que el último parásito quepa.
        const float LimitGrace = 1f;

        ParasiteBenefit benefit;
        SpinAttack spin;
        int baseLimit = Parasite.DefaultLimit;
        int spinLimit = 6;
        float highUntil;
        bool duplicating;

        /// <summary>Parásitos duplicados (para las pruebas).</summary>
        public int Duplicated { get; private set; }

        public void Configure(ParasiteBenefit newBenefit, int newBaseLimit, int newSpinLimit)
        {
            Unsubscribe();
            benefit = newBenefit;
            baseLimit = Mathf.Max(1, newBaseLimit);
            spinLimit = Mathf.Max(baseLimit, newSpinLimit);
            Parasite.Attached += OnAttached;
        }

        /// <summary>True mientras gira (la Peonza en marcha, no la preparación ni el frenado).</summary>
        public bool Spinning => forceSpinning || (spin != null && spin.State == SpinAttack.Phase.Spinning);

        // Para las pruebas, que no montan la Peonza entera.
        internal bool forceSpinning;

        void Update()
        {
            if (spin == null)
                spin = GetComponent<SpinAttack>();
            if (Spinning)
                highUntil = Time.time + LimitGrace;
            if (benefit != null)
                benefit.SetLimit(Time.time < highUntil ? spinLimit : baseLimit);
        }

        void OnAttached(Parasite parasite)
        {
            if (duplicating || benefit == null || !Spinning || parasite == null)
                return;

            // El segundo parásito no se duplica a su vez (solo un paso).
            duplicating = true;
            try
            {
                // El máximo ya es el alto mientras gira; si el giro acaba de empezar, se adelanta.
                benefit.SetLimit(spinLimit);
                highUntil = Time.time + LimitGrace;
                var next = Parasite.FindNextHost(parasite.Host);
                if (next != null && benefit.AttachTo(next) != null)
                    Duplicated++;
            }
            finally
            {
                duplicating = false;
            }
        }

        void Unsubscribe() => Parasite.Attached -= OnAttached;

        void OnDestroy()
        {
            Unsubscribe();
            // Si se pierde el linaje, ParasiteBenefit (clase) deja el máximo como estaba.
            if (benefit != null)
                benefit.SetLimit(baseLimit);
        }
    }
}
