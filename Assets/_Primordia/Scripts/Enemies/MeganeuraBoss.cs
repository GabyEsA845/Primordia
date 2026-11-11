using System.Collections;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Meganeura, jefe 2 del piso 3 (libélula gigante; diseño en primordia-boss-meganeura). Vista en picada: la raíz es el punto del
    /// suelo bajo el jefe, con su sombra en el suelo; el cuerpo ("Body") va elevado sobre ella (<see cref="AltitudeToY"/> por unidad
    /// de altura) y se dibuja por encima de los enemigos del suelo. Sus hojas tienen una fila por dirección (<see cref="DirectionalArt"/>).
    /// Paso 7a: la sala (el Lepidodendron partido), la presentación (su sombra cruza la sala, se posa en el tronco y abre las alas) y
    /// el cuerpo que se ve dañado según las fases (alas sanas, rasgadas y muy rasgadas, y quemadas). El combate (picado, huevos,
    /// tormenta) viene en los pasos 7b-7d.
    /// </summary>
    [RequireComponent(typeof(Enemy), typeof(Rigidbody2D))]
    public partial class MeganeuraBoss : MonoBehaviour, IBoss, IPhasedBoss
    {
        /// <summary>Unidades que sube el cuerpo en pantalla por cada unidad de altura (cos 55°).</summary>
        public const float AltitudeToY = 0.57f;

        // Dónde está el Lepidodendron partido respecto al centro de la sala, y a qué altura queda el tórax posada encima.
        static readonly Vector2 StumpOffset = new(8f, 3f);
        const float PerchHeight = 3f;
        const float IntroFlightSeconds = 1.4f;
        const float ShadowAlphaGround = 0.45f;
        const float ShadowAlphaHigh = 0.3f;
        const float FlashTime = 0.12f;

        // Milisegundos por cuadro de cada hoja (README de la Meganeura).
        const float FlyMs = 50f, LandMs = 90f, PerchMs = 200f, IntroMs = 110f, DeathFallMs = 90f;

        [SerializeField] string displayName = "Meganeura";
        [SerializeField] DirectionalArt art;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] DirectionalSprite bodySprite;
        [SerializeField] SpriteRenderer shadowRenderer;
        [SerializeField] DirectionalSprite shadowSprite;
        [Tooltip("Lepidodendron partido (4 cuadros, pivote abajo): donde se posa en la presentación.")]
        [SerializeField] Sprite[] stumpFrames = System.Array.Empty<Sprite>();
        [Tooltip("Capa de fuego encima del cuerpo (Fall_Burning y Grounded_Burning).")]
        [SerializeField] SpriteRenderer fireRenderer;
        [SerializeField] DirectionalSprite fireSprite;
        [SerializeField] Effects effects = new();
        [Tooltip("Ninfa que sale de los huevos de la fase 2.")]
        [SerializeField] Enemy nymphPrefab;
        [SerializeField, Range(0f, 1f)] float torn1Threshold = 0.65f;
        [SerializeField, Range(0f, 1f)] float torn2Threshold = 0.30f;

        Enemy enemy;
        Rigidbody2D rb;
        Transform arenaCenter;
        Vector2 arenaSize = new(40f, 24f);
        Vector2 stumpGround;
        bool stumpBuilt;
        bool waitingIntro;
        bool fighting;
        float flashUntil;
        Coroutine intro;
        SpriteRenderer stumpRenderer;
        SpriteLoop stumpLoop;

        public string DisplayName => displayName;
        public Health Health { get; private set; }
        public float[] PhaseThresholds => new[] { torn1Threshold, torn2Threshold };

        public BossBarStyle BarStyle => IsHigh || (Health != null && Health.IsInvulnerable) ? BossBarStyle.Immune : BossBarStyle.Normal;
        public Vector2 ArenaCenter => arenaCenter != null ? arenaCenter.position : transform.position;

        /// <summary>True si ya se le quemaron las alas en el metano (los agujeros son permanentes).</summary>
        public bool Burnt { get; set; }

        /// <summary>Altura a la que vuela, en unidades de suelo: el cuerpo sube <see cref="AltitudeToY"/> por cada una.</summary>
        public float Altitude
        {
            get => bodyRenderer != null ? bodyRenderer.transform.localPosition.y / AltitudeToY : 0f;
            set
            {
                if (bodyRenderer != null)
                    bodyRenderer.transform.localPosition = new Vector3(0f, value * AltitudeToY, 0f);
                // La colisión sigue al tórax: donde se ve es donde se le pega.
                if (TryGetComponent<CircleCollider2D>(out var hit))
                    hit.offset = new Vector2(0f, value * AltitudeToY);
            }
        }

        /// <summary>Sufijo de las hojas según el daño de las alas: nada, Torn1 (≤ 65 %) o Torn2 (≤ 30 %), más Burnt si ardió.</summary>
        public string WingVariant
        {
            get
            {
                float fraction = Health != null && Health.Max > 0 ? (float)Health.Current / Health.Max : 1f;
                string torn = fraction <= torn2Threshold ? "Torn2" : fraction <= torn1Threshold ? "Torn1" : "";
                return torn + (Burnt ? "Burnt" : "");
            }
        }

        internal void Configure(DirectionalArt newArt, SpriteRenderer newBody, DirectionalSprite newBodySprite, SpriteRenderer newShadow,
            DirectionalSprite newShadowSprite, Sprite[] newStump)
        {
            art = newArt;
            bodyRenderer = newBody;
            bodySprite = newBodySprite;
            shadowRenderer = newShadow;
            shadowSprite = newShadowSprite;
            stumpFrames = newStump ?? System.Array.Empty<Sprite>();
        }

        internal void ConfigureFight(SpriteRenderer newFireRenderer, DirectionalSprite newFireSprite, Effects newEffects, Enemy newNymph = null)
        {
            nymphPrefab = newNymph;
            fireRenderer = newFireRenderer;
            fireSprite = newFireSprite;
            effects = newEffects ?? new Effects();
        }

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            Health = GetComponent<Health>();
            rb = GetComponent<Rigidbody2D>();
            // Cinemático: no lo empujan, pero el jugador choca con él y los disparos le dan.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        void OnEnable()
        {
            // Se activa cuando el jugador entra en su sala (la sala oculta a sus enemigos hasta entonces).
            ActiveBoss.Set(this);
            Health.Damaged += OnDamaged;
            enemy.Died += OnDied;
            waitingIntro = TryGetComponent<BossIntro>(out var introData) && introData.Data != null && !introData.Played;
            if (waitingIntro)
                HideForIntro();
            else
                Perch();
        }

        void OnDisable()
        {
            ActiveBoss.Clear(this);
            Health.Damaged -= OnDamaged;
            enemy.Died -= OnDied;
            StopFight();
            if (intro != null)
                StopCoroutine(intro);
            intro = null;
        }

        // ---------- IBoss ----------

        public void SetArena(Transform center, Vector2 size)
        {
            arenaCenter = center;
            arenaSize = size;
            stumpGround = (Vector2)center.position + StumpOffset;
            if (!stumpBuilt)
                BuildStump(center);
            BuildScenery(center);
            // Empieza donde se posa: la cámara de la presentación mira a la raíz. La sala lo mantiene desactivado hasta que entras
            // (su Awake aún no ha corrido): se pide el cuerpo físico aquí.
            if (TryGetComponent<Rigidbody2D>(out var body))
                body.position = stumpGround;
            transform.position = stumpGround;
        }

        public void PlayIntroPose(float hold)
        {
            if (intro != null)
                StopCoroutine(intro);
            intro = StartCoroutine(IntroRoutine());
        }

        public void BeginFight()
        {
            waitingIntro = false;
            if (intro != null)
                StopCoroutine(intro);
            intro = null;
            fighting = true;
            Perch();
            StartFight();
        }

        // ---------- Escenario ----------

        /// <summary>El Lepidodendron partido de la sala: tronco sólido (≈ 2×1 u al pie) con ascuas en la rotura.</summary>
        void BuildStump(Transform parent)
        {
            stumpBuilt = true;
            if (stumpFrames.Length == 0)
                return;
            var stump = new GameObject("LepidoSplit");
            stump.transform.SetParent(parent, true);
            stump.transform.position = stumpGround;
            var renderer = stump.AddComponent<SpriteRenderer>();
            renderer.sprite = stumpFrames[0];
            renderer.sortingOrder = 4;
            stumpRenderer = renderer;
            stumpLoop = stump.AddComponent<SpriteLoop>();
            stumpLoop.Configure(renderer, stumpFrames, 0.2f, 0f);
            var collider = stump.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 1f);
            collider.offset = new Vector2(0f, 0.5f);
        }

        // ---------- Estados visibles ----------

        void HideForIntro()
        {
            if (bodyRenderer != null)
                bodyRenderer.enabled = false;
            if (shadowRenderer != null)
                shadowRenderer.enabled = false;
        }

        /// <summary>Posada en el tronco, de cara a la sala.</summary>
        void Perch()
        {
            CurrentMode = Mode.Perched;
            ShowBody(true);
            Altitude = PerchHeight;
            SetShadow(transform.position, ShadowAlphaGround);
            PlayBody("Perch", PerchMs, loop: true);
            SetAngle(-90f);
        }

        void ShowBody(bool visible)
        {
            if (bodyRenderer != null)
                bodyRenderer.enabled = visible;
            if (shadowRenderer != null)
                shadowRenderer.enabled = visible;
        }

        void SetShadow(Vector2 position, float alpha)
        {
            if (shadowRenderer == null)
                return;
            shadowRenderer.enabled = true;
            shadowRenderer.transform.position = position;
            var color = shadowRenderer.color;
            color.a = alpha;
            shadowRenderer.color = color;
        }

        void SetAngle(float angle)
        {
            if (bodySprite != null)
                bodySprite.Angle = angle;
            if (shadowSprite != null)
                shadowSprite.Angle = angle;
            // Las llamas son una capa del cuerpo: giran con él (con otra fila parecían una segunda silueta quieta).
            if (fireSprite != null)
                fireSprite.Angle = angle;
        }

        /// <summary>Hoja del cuerpo (con el sufijo de las alas dañadas; si no existe esa variante, la sana) y la de su sombra.</summary>
        void PlayBody(string animation, float milliseconds, bool loop, bool restart = false)
        {
            if (bodySprite != null)
            {
                string variant = WingVariant;
                bodySprite.Play(variant.Length > 0 ? $"Meganeura_{animation}_{variant}" : $"Meganeura_{animation}", milliseconds, loop,
                    $"Meganeura_{animation}", restart);
            }
            if (shadowSprite != null)
                shadowSprite.Play($"Meganeura_Shadow_{animation}", milliseconds, loop, null, restart);
        }

        // ---------- Presentación ----------

        /// <summary>
        /// Su sombra cruza la sala de un lado al tronco (aún no se la ve), se posa (Land) y abre las alas con el reflejo azulado
        /// (Intro, siempre con alas sanas). La última pose se queda hasta que acaba el rótulo.
        /// </summary>
        IEnumerator IntroRoutine()
        {
            Burnt = false;
            HideForIntro();
            Vector2 end = stumpGround;
            Vector2 start = end + new Vector2(-(arenaSize.x * 0.5f + 6f), 6f);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

            if (shadowSprite != null)
            {
                shadowSprite.Play("Meganeura_Shadow_Fly", FlyMs, true);
                shadowSprite.Angle = angle;
            }
            for (float t = 0f; t < IntroFlightSeconds; t += Time.deltaTime)
            {
                SetShadow(Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / IntroFlightSeconds)), ShadowAlphaHigh);
                yield return null;
            }

            // Se posa: el cuerpo aparece sobre el tronco, mirando a la sala.
            ShowBody(true);
            Altitude = PerchHeight;
            SetShadow(end, ShadowAlphaGround);
            SetAngle(-90f);
            PlayBody("Land", LandMs, loop: false, restart: true);
            yield return new WaitForSeconds(bodySprite != null ? bodySprite.Length : 0.27f);

            PlayBody("Intro", IntroMs, loop: false, restart: true);
            intro = null;
        }

        // ---------- Daño y muerte ----------

        void OnDamaged(int _)
        {
            flashUntil = Time.time + FlashTime;
            // Cruzar el 30 % cierra la ventana en el acto; y un aturdimiento solo admite su tope de daño: al llegar a él se levanta.
            if (stunWindow && stunStartHealth - Health.Current >= StunBudget)
            {
                stunWindow = false;
                stunCut = true;
            }
            RefreshInvulnerability();
        }

        void OnDied(Enemy _)
        {
            fighting = false;
            CurrentMode = Mode.Dead;
            StopFight();
            ClearSummons();
            StopAllCoroutines();
            PlayFire(null, 0f);
            if (intro != null)
                StopCoroutine(intro);
            intro = null;
            foreach (var collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            ShowBody(true);
            CreateDeathPresentation();
        }

        void LateUpdate()
        {
            if (bodyRenderer != null)
                bodyRenderer.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        }
    }
}
