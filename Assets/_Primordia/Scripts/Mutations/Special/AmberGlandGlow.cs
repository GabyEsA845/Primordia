using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Lleva la carga de la Gota de ámbar: cuánto tiempo se mantiene el clic y el cristal de ámbar del lomo, que cambia de estado
    /// (vacío, a medias, lleno o cargando) con la recarga.
    /// </summary>
    public class AmberGlandGlow : MonoBehaviour
    {
        AmberGlandMutation mutation;
        PlayerAbility ability;
        PlayerAnimator animator;
        CreatureLayerMirror crystal;
        float chargeStart = -1f;

        /// <summary>De 0 a 1: lo cargada que está la gota que se está reteniendo.</summary>
        public float Charge { get; private set; }

        public bool IsCharging => chargeStart >= 0f;

        internal void Configure(AmberGlandMutation newMutation, PlayerAbility newAbility)
        {
            mutation = newMutation;
            ability = newAbility;
            animator = GetComponent<PlayerAnimator>();
        }

        public void BeginCharge() => chargeStart = Time.time;

        /// <summary>Suelta el clic. True si estaba cargada del todo (sale la gota).</summary>
        public bool EndCharge(float needed)
        {
            bool charged = IsCharging && Time.time - chargeStart >= needed;
            chargeStart = -1f;
            Charge = 0f;
            return charged;
        }

        void Update()
        {
            if (IsCharging)
                Charge = Mathf.Clamp01((Time.time - chargeStart) / Mathf.Max(0.05f, mutation.ChargeSeconds));
            UpdateCrystal();
        }

        /// <summary>El cristal del lomo: carga mientras se mantiene el clic, lleno si la gota está lista, y vacío o a medias mientras se recarga.</summary>
        void UpdateCrystal()
        {
            if (animator == null || animator.Body == null || mutation == null)
                return;
            var state = IsCharging ? AmberGlandMutation.CrystalState.Charge
                : ability == null || ability.IsReady ? AmberGlandMutation.CrystalState.Full
                : ability.CooldownProgress < 0.5f ? AmberGlandMutation.CrystalState.Empty
                : AmberGlandMutation.CrystalState.Half;
            var layer = mutation.CrystalLayerFor(state, animator.CreatureController);
            if (layer == null && crystal == null)
                return;
            if (crystal == null)
                crystal = CreatureLayerMirror.Create(animator.BodyAnimator, animator.Body, "CreatureGlandCrystal", 1);
            crystal.Controller = layer;
        }

        void OnDestroy()
        {
            if (crystal != null)
                Destroy(crystal.gameObject);
        }
    }
}
