using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Cinco ojos (Opabinia): los Ojos compuestos duran un segundo más, y cada remate mientras duran los alarga medio
    /// segundo. El efecto de los ojos se crea la primera vez que se usan, así que se escucha su aviso estático.
    /// </summary>
    public class FiveEyesTouch : MonoBehaviour
    {
        FinisherBenefit finisher;
        float bonus = 1f;
        float perFinish = 0.5f;

        public void Configure(FinisherBenefit newFinisher, float newBonus, float newPerFinish)
        {
            Unsubscribe();
            finisher = newFinisher;
            bonus = Mathf.Max(0f, newBonus);
            perFinish = Mathf.Max(0f, newPerFinish);
            CompoundEyesEffect.AnyStarted += OnStarted;
            if (finisher != null)
                finisher.Finished += OnFinished;
        }

        void OnStarted(CompoundEyesEffect effect)
        {
            if (effect != null && effect.gameObject == gameObject)
                effect.Extend(bonus);
        }

        void OnFinished(Health target)
        {
            if (TryGetComponent<CompoundEyesEffect>(out var effect) && effect.IsActive)
                effect.Extend(perFinish);
        }

        void Unsubscribe()
        {
            CompoundEyesEffect.AnyStarted -= OnStarted;
            if (finisher != null)
                finisher.Finished -= OnFinished;
        }

        void OnDestroy() => Unsubscribe();
    }
}
