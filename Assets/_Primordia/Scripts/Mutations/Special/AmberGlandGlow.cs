using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Lleva la carga de la Gota de ámbar: cuánto tiempo se mantiene el clic y, sobre la glándula vacía del cuerpo (la capa normal del
    /// Órgano), funde la capa de la glándula llena a medida que carga y mientras queda gota lista (la recarga la vacía).
    /// </summary>
    public class AmberGlandGlow : MonoBehaviour
    {
        AmberGlandMutation mutation;
        PlayerAbility ability;
        PlayerAnimator animator;
        CreatureLayerMirror full;
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

            // Llena mientras carga y mientras la habilidad está lista; vacía mientras se recarga.
            float fill = IsCharging ? Charge : ability != null && ability.IsReady ? 0.6f : 0f;
            UpdateFull(fill);
        }

        void UpdateFull(float fill)
        {
            if (animator == null || animator.Body == null || mutation == null)
                return;
            var layer = mutation.FullLayerFor(animator.CreatureController);
            if (layer == null && full == null)
                return;
            if (full == null)
                full = CreatureLayerMirror.Create(animator.BodyAnimator, animator.Body, "CreatureGlandFull", 2);
            full.Controller = layer;
            full.Tint = new Color(1f, 1f, 1f, fill);
        }

        void OnDestroy()
        {
            if (full != null)
                Destroy(full.gameObject);
        }
    }
}
