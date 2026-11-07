using System;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Hojas del halo con alas de la Cápsula de ámbar (docs/multimedia-temp/floor3-capsule/halo).</summary>
    [Serializable]
    public class AmberHaloArt
    {
        [Tooltip("Nivel I-II con la cápsula lista: 1 gema (bucle de 8 cuadros).")]
        public Sprite[] ready = Array.Empty<Sprite>();
        [Tooltip("Nivel III con 2 activaciones: las 2 gemas encendidas.")]
        public Sprite[] readyFull = Array.Empty<Sprite>();
        [Tooltip("Nivel III con 1 activación: 1 gema encendida y 1 apagada.")]
        public Sprite[] readyHalf = Array.Empty<Sprite>();
        [Tooltip("Apagado (alas caídas, gemas oscuras), nivel I-II y III.")]
        public Sprite off;
        public Sprite offThree;
        [Tooltip("Nivel I-II: se apaga al activarse (4 cuadros); al revés, se repone.")]
        public Sprite[] fade = Array.Empty<Sprite>();
        [Tooltip("Resplandor tenue que respira detrás del halo.")]
        public Sprite[] glow = Array.Empty<Sprite>();

        public bool IsEmpty => ready == null || ready.Length == 0;
    }

    /// <summary>
    /// Halo con alas que levita sobre la cabeza de la criatura mientras la Cápsula de ámbar está lista (una gema, dos en el nivel III).
    /// Se apaga al gastarse una activación y se repone al revés al entrar en otra sala.
    /// </summary>
    public class AmberHalo : MonoBehaviour
    {
        const float LoopFrame = 0.12f;
        const float FadeFrame = 0.09f;

        AmberHaloArt art;
        bool three;
        SpriteRenderer body;
        SpriteRenderer halo;
        SpriteRenderer glow;
        int charges;
        // Para el nivel I-II: transición entre encendido y apagado (hacia apagado si fadingOff).
        float fadeStart = -1f;
        bool fadingOff;

        /// <summary>Estado que se muestra: 0 = apagado.</summary>
        public int Charges => charges;

        /// <summary>Distancia (en píxeles) del centro del cuerpo al centro del halo: unos 9 px sobre el cráneo según el cuerpo.</summary>
        static float RiseFor(PlayerAnimator animator)
        {
            string name = animator != null && animator.CreatureController != null ? animator.CreatureController.name : string.Empty;
            if (name.Contains("Diplocaulus"))
                return 24f;
            if (name.Contains("Whipscorpion"))
                return 18f;
            return 16f;
        }

        public static AmberHalo Create(PlayerAnimator animator, AmberHaloArt art, bool three, int charges)
        {
            if (animator == null || animator.Body == null || art == null || art.IsEmpty)
                return null;
            var go = new GameObject("AmberHalo");
            go.transform.SetParent(animator.Body.transform, false);
            go.transform.localPosition = Vector3.up * (RiseFor(animator) / 16f);
            var component = go.AddComponent<AmberHalo>();
            component.art = art;
            component.three = three;
            component.body = animator.Body;
            component.halo = go.AddComponent<SpriteRenderer>();
            var glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(go.transform, false);
            component.glow = glowObject.AddComponent<SpriteRenderer>();
            component.charges = charges;
            return component;
        }

        /// <summary>Activaciones que quedan; el cambio hacia o desde cero hace la transición de apagado (nivel I-II).</summary>
        public void SetCharges(int value)
        {
            if (value == charges)
                return;
            bool wasLit = charges > 0;
            charges = value;
            if (!three && art.fade != null && art.fade.Length > 0 && wasLit != (value > 0))
            {
                fadeStart = Time.time;
                fadingOff = !(value > 0);
            }
        }

        void LateUpdate()
        {
            bool shown = body != null && body.enabled;
            halo.enabled = shown;
            glow.enabled = shown;
            if (!shown)
                return;

            halo.sortingLayerID = body.sortingLayerID;
            halo.sortingOrder = body.sortingOrder + 3;
            glow.sortingLayerID = body.sortingLayerID;
            glow.sortingOrder = body.sortingOrder + 2;

            float time = Time.time;
            bool lit = charges > 0;
            halo.sprite = PickHalo(time, lit);
            // El resplandor solo con el halo encendido.
            glow.sprite = lit && art.glow != null && art.glow.Length > 0 ? art.glow[Mathf.FloorToInt(time / LoopFrame) % art.glow.Length] : null;
        }

        Sprite PickHalo(float time, bool lit)
        {
            if (fadeStart >= 0f)
            {
                int step = Mathf.FloorToInt((time - fadeStart) / FadeFrame);
                if (step < art.fade.Length)
                    return art.fade[fadingOff ? step : art.fade.Length - 1 - step];
                fadeStart = -1f;
            }

            if (!lit)
                return three && art.offThree != null ? art.offThree : art.off;
            var frames = three ? (charges >= 2 ? art.readyFull : art.readyHalf) : art.ready;
            if (frames == null || frames.Length == 0)
                frames = art.ready;
            return frames[Mathf.FloorToInt(time / LoopFrame) % frames.Length];
        }
    }
}
