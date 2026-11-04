using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Aspecto de los enemigos que nadan (medusa, Pikaia, Anomalocaris): el cuerpo ("Body") flota un poco por encima con
    /// vaivén y su sombra se queda en el suelo, separada; así se lee que pasan por encima de las fosas.
    /// </summary>
    public class SwimmerLook : MonoBehaviour
    {
        const float Height = 0.35f;
        const float BobAmplitude = 0.08f;
        const float BobPeriod = 1.4f;
        const int ShadowOrder = -12;

        [SerializeField] Sprite shadowSprite;

        Transform body;
        Vector3 bodyRest;
        float phase;
        static Sprite defaultShadow;

        public Transform Shadow { get; private set; }

        /// <summary>True si está posado: el cuerpo baja al suelo (la libélula posada) y vuelve a subir al despegar.</summary>
        public bool Grounded { get; set; }

        // Cuánto de la altura y el vaivén se ve, de 0 (en el suelo) a 1 (en el aire); cambia con el tiempo.
        float level = 1f;
        const float LevelSpeed = 5f;

        internal void Configure(Sprite newShadow) => shadowSprite = newShadow;

        void Start()
        {
            body = transform.Find("Body");
            if (body != null)
                bodyRest = body.localPosition;
            phase = Random.value * BobPeriod;

            var shadow = new GameObject("SwimShadow");
            shadow.transform.SetParent(transform, false);
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = shadowSprite != null ? shadowSprite : DefaultShadow();
            renderer.sortingOrder = ShadowOrder;
            renderer.color = shadowSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.35f);
            Shadow = shadow.transform;
        }

        void LateUpdate()
        {
            if (body == null)
                return;
            level = Mathf.MoveTowards(level, Grounded ? 0f : 1f, LevelSpeed * Time.deltaTime);
            float bob = Mathf.Sin((Time.time + phase) / BobPeriod * Mathf.PI * 2f) * BobAmplitude;
            body.localPosition = bodyRest + Vector3.up * ((Height + bob) * level);
        }

        // Elipse de 12×4 px a 16 px por unidad.
        static Sprite DefaultShadow()
        {
            if (defaultShadow != null)
                return defaultShadow;
            var texture = new Texture2D(12, 4) { filterMode = FilterMode.Point };
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 12; x++)
                {
                    float nx = (x + 0.5f - 6f) / 6f, ny = (y + 0.5f - 2f) / 2f;
                    texture.SetPixel(x, y, nx * nx + ny * ny <= 1f ? Color.white : Color.clear);
                }
            texture.Apply();
            defaultShadow = Sprite.Create(texture, new Rect(0, 0, 12, 4), new Vector2(0.5f, 0.5f), 16f);
            return defaultShadow;
        }
    }
}
