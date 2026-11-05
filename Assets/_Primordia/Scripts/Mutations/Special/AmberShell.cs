using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Hojas del bloque de ámbar (docs/multimedia-temp/floor3-specials-look/shell y level3), compartidas por la Cápsula y la Gota.</summary>
    [Serializable]
    public class AmberShellArt
    {
        [Tooltip("Cáscara de detrás de la criatura: se forma, se mantiene.")]
        public Sprite[] backForm = Array.Empty<Sprite>();
        public Sprite[] backHold = Array.Empty<Sprite>();
        [Tooltip("Cáscara de delante (translúcida): se forma, se mantiene y se agrieta (3 fases).")]
        public Sprite[] frontForm = Array.Empty<Sprite>();
        public Sprite[] frontHold = Array.Empty<Sprite>();
        public Sprite[] crack = Array.Empty<Sprite>();
        [Tooltip("Estallido en esquirlas.")]
        public Sprite[] shatter = Array.Empty<Sprite>();

        public bool IsEmpty => (frontHold == null || frontHold.Length == 0) && (backHold == null || backHold.Length == 0);
    }

    /// <summary>
    /// Bloque de ámbar de dos capas alrededor de algo (el jugador con la Cápsula, un enemigo con la Gota): la capa de atrás y la de
    /// delante con la criatura en medio; se forma, se mantiene, se agrieta al final y estalla. Sigue a lo que encierra. Se destruye
    /// solo al estallar; <see cref="Break"/> lo revienta antes.
    /// </summary>
    public class AmberShell : MonoBehaviour
    {
        const float FormFrame = 0.07f;
        const float HoldFrame = 0.12f;
        const float ShatterFrame = 0.06f;
        // La hoja está dibujada para un cuerpo de unas 2,3 × 2,9 unidades; se escala al de lo encerrado.
        static readonly Vector2 DrawnBody = new(2.3f, 2.9f);
        const float CrackSeconds = 0.5f;

        Transform target;
        Vector2 lastCenter;
        Vector2 lastSize;
        AmberShellArt art;
        float duration;
        float start;
        SpriteRenderer back;
        SpriteRenderer front;
        bool breaking;
        int order;

        /// <summary>Al estallar (por tiempo o a la fuerza), con el centro del bloque.</summary>
        public event Action<Vector2> Broken;

        public bool IsBroken { get; private set; }

        /// <summary>Segundos que lleva puesto.</summary>
        public float Age => Time.time - start;

        Vector2 sizeHint = new(1f, 1f);
        Sprite[] overlay;
        SpriteRenderer overlayRenderer;

        /// <param name="sizeHint">Tamaño del bloque si lo encerrado no tiene dibujo (un cadáver ya sin cuerpo).</param>
        /// <param name="overlay">Dibujo en bucle delante de todo (la silueta fósil del cadáver).</param>
        public static AmberShell Create(Transform target, AmberShellArt art, float duration, Vector2? sizeHint = null, Sprite[] overlay = null)
        {
            var go = new GameObject("AmberShell");
            var shell = go.AddComponent<AmberShell>();
            shell.target = target;
            shell.art = art ?? new AmberShellArt();
            shell.duration = Mathf.Max(0.2f, duration);
            shell.start = Time.time;
            shell.sizeHint = sizeHint ?? new Vector2(1f, 1f);
            shell.overlay = overlay;
            shell.Measure();
            shell.Build();
            return shell;
        }

        void Measure()
        {
            if (target == null)
                return;
            bool any = false;
            var bounds = new Bounds(target.position, Vector3.zero);
            order = int.MinValue;
            foreach (var renderer in target.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null || renderer.gameObject.name.Contains("Shadow"))
                    continue;
                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
                order = Mathf.Max(order, renderer.sortingOrder);
            }
            if (!any)
            {
                bounds = new Bounds(target.position, new Vector3(sizeHint.x, sizeHint.y, 0f));
                order = 10;
            }
            lastCenter = bounds.center;
            lastSize = bounds.size;
        }

        void Build()
        {
            back = NewLayer("Back", order - 2);
            front = NewLayer("Front", order + 2);
            if (overlay != null && overlay.Length > 0)
                overlayRenderer = NewLayer("Overlay", order + 3);
            float scale = Mathf.Clamp(Mathf.Max(lastSize.x / DrawnBody.x, lastSize.y / DrawnBody.y) * 1.15f, 0.7f, 3f);
            transform.localScale = Vector3.one * scale;
            Place();
        }

        SpriteRenderer NewLayer(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        void Place()
        {
            Measure();
            transform.position = lastCenter;
        }

        void Update()
        {
            if (target != null)
                Place();

            float age = Age;
            if (breaking)
            {
                var shatter = art.shatter;
                int frame = Mathf.FloorToInt((age - breakStart) / ShatterFrame);
                if (shatter == null || shatter.Length == 0 || frame >= shatter.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                front.sprite = shatter[frame];
                back.sprite = null;
                if (overlayRenderer != null)
                    overlayRenderer.sprite = null;
                return;
            }

            if (overlayRenderer != null)
                overlayRenderer.sprite = overlay[Mathf.FloorToInt(age / HoldFrame) % overlay.Length];

            bool formed = false;
            front.sprite = Pick(art.frontForm, art.frontHold, art.crack, age, ref formed);
            back.sprite = Pick(art.backForm, art.backHold, null, age, ref formed);
            if (age >= duration)
                Break();
        }

        float breakStart;

        Sprite Pick(Sprite[] form, Sprite[] hold, Sprite[] crack, float age, ref bool formed)
        {
            float formTime = (form?.Length ?? 0) * FormFrame;
            if (age < formTime)
                return form[Mathf.Min(form.Length - 1, Mathf.FloorToInt(age / FormFrame))];
            if (crack != null && crack.Length > 0 && age >= duration - CrackSeconds)
            {
                float into = (age - (duration - CrackSeconds)) / CrackSeconds;
                return crack[Mathf.Min(crack.Length - 1, Mathf.FloorToInt(into * crack.Length))];
            }
            if (hold == null || hold.Length == 0)
                return null;
            return hold[Mathf.FloorToInt(age / HoldFrame) % hold.Length];
        }

        /// <summary>Lo revienta ya: estalla en esquirlas y avisa.</summary>
        public void Break()
        {
            if (breaking)
                return;
            breaking = true;
            IsBroken = true;
            breakStart = Time.time - start;
            Broken?.Invoke(lastCenter);
        }
    }

    /// <summary>El estallido de un bloque de ámbar: esquirlas que dañan y empujan (y a veces aturden) a los enemigos de alrededor.</summary>
    public static class AmberShards
    {
        public static int Burst(Vector2 center, float radius, int damage, float push, float stunSeconds, Health ignore = null)
        {
            int hits = 0;
            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health == ignore || health.IsDead || !seen.Add(health) || health.GetComponentInParent<Enemy>() == null)
                    continue;

                hits++;
                if (damage > 0)
                    health.TakeDamage(damage);
                if (health.IsDead || !health.TryGetComponent<EnemyAI>(out var ai))
                    continue;
                Vector2 away = (Vector2)health.transform.position - center;
                if (away.sqrMagnitude < 0.01f)
                    away = Vector2.up;
                // Los jefes ni se empujan ni se aturden.
                bool boss = health.GetComponentInParent<IBoss>() != null;
                if (!boss)
                    ai.Knockback(away.normalized * push, Mathf.Max(0.2f, stunSeconds));
            }
            return hits;
        }
    }
}
