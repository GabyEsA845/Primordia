using Primordia.Combat;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Bola de lodo del sapo (piso 3): sale en arco hacia donde estaba el jugador al escupir y, al caer, salpica, hace daño al que
    /// esté debajo y deja un charco de lodo que ralentiza unos segundos. El proyectil lleva su sombra pegada al suelo; el cuerpo de
    /// la bola sube y baja con el arco.
    /// </summary>
    public class ToadMudBall : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform shadow;
        [Tooltip("Efecto de cuadros que se reproduce una vez al caer (Fx_ToadMud_Splat).")]
        [SerializeField] GameObject splat;
        [Tooltip("Cuadros del charco de lodo que deja (Puddle_Sludge_*).")]
        [SerializeField] Sprite[] puddleFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float puddleFrameSeconds = 0.26f;

        Vector2 from;
        Vector2 to;
        float duration;
        float arcHeight;
        float elapsed;
        int damage;
        float hitRadius;
        float puddleSeconds;
        Vector2 mouthShift;
        bool flying;

        public bool Flying => flying;

        public void Configure(Transform newBody, Transform newShadow, GameObject newSplat, Sprite[] newPuddleFrames, float frameSeconds)
        {
            body = newBody;
            shadow = newShadow;
            splat = newSplat;
            puddleFrames = newPuddleFrames;
            puddleFrameSeconds = frameSeconds;
        }

        /// <summary>Lanza la bola de <paramref name="start"/> a <paramref name="end"/>.</summary>
        public void Launch(Vector2 start, Vector2 end, float seconds, float arc, int damageOnHit, float radius, float puddleLife, Vector2 mouthFromFeet = default)
        {
            from = start;
            to = end;
            duration = Mathf.Max(0.05f, seconds);
            arcHeight = arc;
            damage = damageOnHit;
            hitRadius = radius;
            puddleSeconds = puddleLife;
            mouthShift = mouthFromFeet;
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
                body.localPosition = (Vector3)(mouthShift * (1f - k)) + new Vector3(0f, height, 0f);
            if (shadow != null)
                shadow.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, arcHeight > 0f ? height / arcHeight : 0f);

            if (k >= 1f)
                Land();
        }

        void Land()
        {
            flying = false;
            if (splat != null)
                Instantiate(splat, to, Quaternion.identity);

            // Quien esté debajo se lleva el golpe, salvo que lo esquive justo entonces.
            var seen = new System.Collections.Generic.HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(to, hitRadius, LayerMask.GetMask(CombatLayers.Player)))
            {
                var health = hit.GetComponentInParent<Health>();
                if (health != null && seen.Add(health) && !(hit.GetComponentInParent<Primordia.Player.PlayerController>() is { IsDodging: true }))
                    health.TakeDamage(damage);
            }

            MakePuddle(to, puddleFrames, puddleFrameSeconds, puddleSeconds);
            Destroy(gameObject);
        }

        /// <summary>Charco de lodo que ralentiza y desaparece pasado un tiempo (null si no hay dibujo).</summary>
        public static GroundPuddle MakePuddle(Vector2 position, Sprite[] frames, float frameSeconds, float seconds)
        {
            if (frames == null || frames.Length == 0)
                return null;
            var puddle = GroundPuddle.Create(null, position, frames, frameSeconds, sludge: true);
            Destroy(puddle.gameObject, seconds);
            return puddle;
        }
    }
}
