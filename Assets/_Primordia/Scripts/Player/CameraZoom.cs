using Unity.Cinemachine;
using UnityEngine;

namespace Primordia.Player
{
    /// <summary>
    /// Acerca o aleja la cámara poco a poco, también con el juego congelado (evolución de un linaje, escenas de los
    /// jefes). La Pixel Perfect Camera solo admite tamaños enteros, así que mientras dura se desactiva su ajuste y se
    /// cambia el tamaño de la lente; al acabar (<see cref="Restore"/>) todo vuelve a como estaba.
    /// </summary>
    public sealed class CameraZoom
    {
        readonly CinemachineCamera virtualCamera;
        readonly CinemachineBrain brain;
        readonly Behaviour pixelPerfect;
        readonly Behaviour cinemachinePixelPerfect;
        readonly float baseSize;
        readonly float lensSize;
        readonly bool brainIgnoredTimeScale;
        readonly bool pixelPerfectWasOn;
        readonly bool cinemachinePixelPerfectWasOn;
        float zoom;
        bool restored;

        /// <summary>Tamaño ortográfico real al empezar (el que fija la Pixel Perfect Camera).</summary>
        public float BaseSize => baseSize;

        /// <param name="zoom">Tamaño final respecto al de ahora: 0,8 se acerca un 20 %, 2 se aleja al doble.</param>
        public CameraZoom(float zoom)
        {
            this.zoom = zoom;
            virtualCamera = Object.FindAnyObjectByType<CinemachineCamera>();
            brain = Object.FindAnyObjectByType<CinemachineBrain>();
            if (virtualCamera == null)
                return;

            var camera = brain != null ? brain.GetComponent<Camera>() : Camera.main;
            // Por nombre: así no hace falta que el código dependa del paquete de 2D de URP.
            pixelPerfect = camera != null ? camera.GetComponent("PixelPerfectCamera") as Behaviour : null;
            cinemachinePixelPerfect = virtualCamera.GetComponent<CinemachinePixelPerfect>();

            lensSize = virtualCamera.Lens.OrthographicSize;
            baseSize = camera != null ? camera.orthographicSize : lensSize;
            if (brain != null)
            {
                brainIgnoredTimeScale = brain.IgnoreTimeScale;
                brain.IgnoreTimeScale = true;
            }
            pixelPerfectWasOn = pixelPerfect != null && pixelPerfect.enabled;
            cinemachinePixelPerfectWasOn = cinemachinePixelPerfect != null && cinemachinePixelPerfect.enabled;
            if (pixelPerfect != null)
                pixelPerfect.enabled = false;
            if (cinemachinePixelPerfect != null)
                cinemachinePixelPerfect.enabled = false;
        }

        /// <summary>Cambia el tamaño final (por ejemplo, para que quepa una sala entera).</summary>
        public float Zoom
        {
            get => zoom;
            set => zoom = Mathf.Max(0.01f, value);
        }

        /// <summary>0 = tamaño normal, 1 = con el zoom entero.</summary>
        public void Set(float amount)
        {
            if (virtualCamera == null || restored)
                return;
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = baseSize * Mathf.LerpUnclamped(1f, zoom, amount);
            virtualCamera.Lens = lens;
        }

        public void Restore()
        {
            if (virtualCamera == null || restored)
                return;
            restored = true;
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = lensSize;
            virtualCamera.Lens = lens;
            if (pixelPerfect != null)
                pixelPerfect.enabled = pixelPerfectWasOn;
            if (cinemachinePixelPerfect != null)
                cinemachinePixelPerfect.enabled = cinemachinePixelPerfectWasOn;
            if (brain != null)
                brain.IgnoreTimeScale = brainIgnoredTimeScale;
        }
    }
}
