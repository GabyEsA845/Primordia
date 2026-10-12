using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Tiñe los sprites un instante al recibir daño, para que cada golpe se lea de un vistazo.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [Tooltip("Sprites a teñir. Si está vacío, usa todos los del objeto y sus hijos en el momento del golpe.")]
        [SerializeField] SpriteRenderer[] renderers;
        [Tooltip("Sprites que nunca se tiñen; por ejemplo, el cuerpo del jugador, que tiene su propia animación de daño.")]
        [SerializeField] SpriteRenderer[] excluded;
        [SerializeField] Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] float duration = 0.08f;

        Health health;
        bool collectOnHit;
        Color[] originalColors = Array.Empty<Color>();
        Coroutine flashRoutine;
        readonly List<SpriteRenderer> buffer = new();

        void Awake()
        {
            health = GetComponent<Health>();
            collectOnHit = renderers == null || renderers.Length == 0;
            if (!collectOnHit)
                CacheColors();
        }

        void OnEnable() => health.Damaged += OnDamaged;

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            StopFlash();
        }

        void OnDamaged(int _)
        {
            if (!isActiveAndEnabled)
                return;

            StopFlash();

            // Se recogen en cada golpe para incluir las piezas de mutación equipadas durante la partida.
            if (collectOnHit)
            {
                GetComponentsInChildren(buffer);
                if (excluded != null)
                    buffer.RemoveAll(r => Array.IndexOf(excluded, r) >= 0);
                renderers = buffer.ToArray();
                CacheColors();
            }

            flashRoutine = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            SetColors(restore: false);
            yield return new WaitForSeconds(duration);
            SetColors(restore: true);
            flashRoutine = null;
        }

        void StopFlash()
        {
            if (flashRoutine == null)
                return;

            StopCoroutine(flashRoutine);
            flashRoutine = null;
            SetColors(restore: true);
        }

        void CacheColors()
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        void SetColors(bool restore)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = restore ? originalColors[i] : flashColor;
            }
        }
    }
}
