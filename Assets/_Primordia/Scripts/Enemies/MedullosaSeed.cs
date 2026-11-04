using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Vaina de la Medullosa (piso 3): sale en arco hacia la marca del suelo, dando vueltas, y al caer revienta: salpica, hace daño
    /// a quien esté debajo y deja un charco de veneno (<see cref="PoisonPuddle"/>). La sombra va pegada al suelo; el cuerpo de la
    /// vaina sube y baja con el arco. Quita la marca del suelo al caer.
    /// </summary>
    public class MedullosaSeed : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform shadow;
        [Tooltip("Efecto de cuadros que se reproduce una vez al reventar (Fx_MedullosaSeed_Burst).")]
        [SerializeField] GameObject burst;

        Vector2 from;
        Vector2 to;
        Vector2 launchShift;
        float duration;
        float arcHeight;
        float elapsed;
        int damage;
        float hitRadius;
        float puddleSeconds;
        float puddleRadius;
        GameObject marker;
        bool flying;

        public bool Flying => flying;

        public void Configure(Transform newBody, Transform newShadow, GameObject newBurst)
        {
            body = newBody;
            shadow = newShadow;
            burst = newBurst;
        }

        /// <summary>Lanza la vaina de <paramref name="start"/> (los pies de la planta) a <paramref name="end"/>, saliendo de la fronda.</summary>
        public void Launch(Vector2 start, Vector2 end, float seconds, float arc, int damageOnHit, float radius, float puddleLife,
            float puddleSize, GameObject groundMarker, Vector2 launchFromFeet)
        {
            from = start;
            to = end;
            duration = Mathf.Max(0.05f, seconds);
            arcHeight = arc;
            damage = damageOnHit;
            hitRadius = radius;
            puddleSeconds = puddleLife;
            puddleRadius = puddleSize;
            marker = groundMarker;
            launchShift = launchFromFeet;
            elapsed = 0f;
            flying = true;
            transform.position = start;
        }

        void Update()
        {
            if (!flying)
                return;

            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector2.Lerp(from, to, k);
            float height = arcHeight * 4f * k * (1f - k);
            if (body != null)
                body.localPosition = (Vector3)(launchShift * (1f - k)) + new Vector3(0f, height, 0f);
            if (shadow != null)
                shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, arcHeight > 0f ? height / arcHeight : 0f);

            if (k >= 1f)
                Land();
        }

        void OnDestroy()
        {
            if (marker != null)
                Destroy(marker);
        }

        void Land()
        {
            flying = false;
            if (burst != null)
                Instantiate(burst, to, Quaternion.identity);

            // Quien esté debajo se lleva el golpe, salvo que lo esquive justo entonces.
            var seen = new System.Collections.Generic.HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(to, hitRadius, LayerMask.GetMask(CombatLayers.Player)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health != null && seen.Add(health) && !(hit.GetComponentInParent<Primordia.Player.PlayerController>() is { IsDodging: true }))
                    health.TakeDamage(damage);
            }

            PoisonPuddle.SpawnSplat(to, puddleSeconds, puddleRadius, harmsPlayer: true);
            Destroy(gameObject);
        }
    }
}
