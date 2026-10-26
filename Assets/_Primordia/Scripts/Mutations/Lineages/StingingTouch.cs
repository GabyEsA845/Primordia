using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Pólipos urticantes (Burgessomedusa: las larvas envenenan al morder) y Lecho urticante (Anémona: quien pisa tus
    /// púas queda envenenado). Escucha los mordiscos de <see cref="Larva"/> o los pinchazos de <see cref="GroundSpine"/>.
    /// </summary>
    public class StingingTouch : MonoBehaviour
    {
        public enum Source
        {
            Larvae,
            Spines
        }

        Source source;
        int damagePerSecond = 1;
        float duration = 3f;
        GameObject poisonEffect;
        bool subscribed;

        /// <summary>Veces que ha envenenado (para las pruebas).</summary>
        public int Stings { get; private set; }

        public void Configure(Source newSource, int newDamagePerSecond, float newDuration, GameObject newPoisonEffect)
        {
            Unsubscribe();
            source = newSource;
            damagePerSecond = Mathf.Max(1, newDamagePerSecond);
            duration = Mathf.Max(0.1f, newDuration);
            poisonEffect = newPoisonEffect;
            if (source == Source.Larvae)
                Larva.AnyBit += OnLarvaBit;
            else
                GroundSpine.AnyStruck += OnSpineStruck;
            subscribed = true;
        }

        void OnLarvaBit(Larva larva, Health target) => Sting(target);

        void OnSpineStruck(GroundSpine spine, Health target) => Sting(target);

        void Sting(Health target)
        {
            if (target == null || target.IsDead)
                return;
            Poison.Apply(target, damagePerSecond, duration, poisonEffect);
            Stings++;
        }

        void Unsubscribe()
        {
            if (!subscribed)
                return;
            Larva.AnyBit -= OnLarvaBit;
            GroundSpine.AnyStruck -= OnSpineStruck;
            subscribed = false;
        }

        void OnDestroy() => Unsubscribe();
    }
}
