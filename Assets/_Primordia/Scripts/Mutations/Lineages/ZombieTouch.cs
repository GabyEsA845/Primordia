using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Zombi (Cordyceps): un enemigo confundido por las Feromonas al que le toca morir no muere: se levanta como tu aliado unos segundos
    /// (<see cref="ZombieAlly"/>) y cae de verdad al acabar. Se engancha a la muerte (<see cref="Health.AddLethalInterceptor"/>) en lugar de
    /// a un aviso posterior, porque el enemigo no debe llegar a morir (la sala no lo cuenta hasta que cae). Los jefes y lo que no se mueve no
    /// se levantan.
    /// </summary>
    public class ZombieTouch : MonoBehaviour
    {
        float seconds = 4f;
        ZombieAlly.Art art = new();
        System.Func<Health, bool> interceptor;

        /// <summary>Enemigos levantados (para las pruebas).</summary>
        public int Raised { get; private set; }

        public void Configure(float allySeconds, ZombieAlly.Art newArt)
        {
            seconds = Mathf.Max(0.5f, allySeconds);
            art = newArt ?? new ZombieAlly.Art();
            if (interceptor == null)
            {
                interceptor = Intercept;
                Health.AddLethalInterceptor(interceptor);
            }
        }

        bool Intercept(Health target)
        {
            if (target == null || !Confusion.IsConfused(target) || target.GetComponent<ZombieAlly>() != null)
                return false;
            if (ZombieAlly.Raise(target, seconds, art) == null)
                return false;
            Raised++;
            return true;
        }

        void OnDestroy()
        {
            if (interceptor != null)
                Health.RemoveLethalInterceptor(interceptor);
        }
    }
}
