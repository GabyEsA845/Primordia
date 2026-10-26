using System;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Lo que dura el efecto de los Ojos compuestos: pone la cámara lenta (<see cref="TimeWarp"/>), hace que las animaciones
    /// del jugador vayan a su ritmo, enseña el marco de facetas de la pantalla y, con Visión total, sube el daño. Cuenta su
    /// duración en tiempo real (sin contar la pausa), porque el del juego va más lento.
    /// </summary>
    public class CompoundEyesEffect : MonoBehaviour
    {
        const int FlashOrder = 22;

        PlayerMutations player;
        float remaining;
        float damageBonus = 1f;
        readonly List<Animator> animators = new();

        public bool IsActive { get; private set; }
        public float Remaining => remaining;

        /// <summary>Se lanza al empezar y al acabar (los toques de los linajes, como los Cinco ojos de la Opabinia).</summary>
        public event Action Started;
        public event Action Ended;

        /// <summary>Al empezar cualquiera (el componente se crea la primera vez que se activan los ojos).</summary>
        public static event Action<CompoundEyesEffect> AnyStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyStarted = null;

        public void Begin(PlayerMutations newPlayer, float duration, float worldSpeed, float newDamageBonus, Sprite[] flashFrames,
            Color vignetteColor)
        {
            if (player != null && player.Health != null)
                player.Health.Died -= End;
            player = newPlayer;
            if (player.Health != null)
                player.Health.Died += End;
            remaining = duration;
            damageBonus = newDamageBonus;
            IsActive = true;
            TimeWarp.Set(worldSpeed);
            SetAnimatorSpeed(TimeWarp.PlayerSpeed);
            if (player.Weapon != null && damageBonus > 1f)
                player.Weapon.SetDamageBonus(this, damageBonus);
            FrameEffect.Play("CompoundEyesFlash", flashFrames, 0.07f, FlashOrder, transform);
            var vignette = FindAnyObjectByType<ScreenVignette>(FindObjectsInactive.Include);
            if (vignette != null)
                vignette.Show(vignetteColor);
            Sfx.Play(Sounds.PlayerAbilityReady);
            Started?.Invoke();
            AnyStarted?.Invoke(this);
        }

        /// <summary>Alarga lo que queda (los Cinco ojos de la Opabinia).</summary>
        public void Extend(float seconds)
        {
            if (IsActive)
                remaining += seconds;
        }

        void Update()
        {
            if (!IsActive)
                return;
            // En tiempo real, pero parado mientras el juego lo está (pausa, menús).
            if (Time.timeScale > 0f)
                remaining -= Time.unscaledDeltaTime;
            // Las piezas nuevas (una capa que aparece) también van a su ritmo.
            SetAnimatorSpeed(TimeWarp.PlayerSpeed);
            if (remaining <= 0f)
                End();
        }

        public void End()
        {
            if (!IsActive)
                return;
            IsActive = false;
            if (player != null && player.Health != null)
                player.Health.Died -= End;
            TimeWarp.Clear();
            SetAnimatorSpeed(1f);
            if (player != null && player.Weapon != null)
                player.Weapon.ClearDamageBonus(this);
            var vignette = FindAnyObjectByType<ScreenVignette>(FindObjectsInactive.Include);
            if (vignette != null)
                vignette.Hide();
            Ended?.Invoke();
        }

        void OnDisable() => End();

        void SetAnimatorSpeed(float speed)
        {
            animators.Clear();
            GetComponentsInChildren(true, animators);
            foreach (var animator in animators)
            {
                if (animator != null)
                    animator.speed = speed;
            }
        }
    }
}
