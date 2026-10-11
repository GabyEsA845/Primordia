using System.Collections;
using UnityEngine;

namespace Primordia.Core
{
    /// <summary>
    /// Tiñe los sprites un instante al recibir daño, para que cada golpe se lea de un vistazo.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamageFlash : MonoBehaviour
    {
        [Tooltip("Sprites a teñir. Si está vacío, usa todos los del objeto y sus hijos.")]
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] Color flashColor = Color.white;
        [SerializeField, Min(0.01f)] float duration = 0.08f;

        Health health;
        Color[] originalColors;
        Coroutine flashRoutine;

        void Awake()
        {
            health = GetComponent<Health>();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>();

            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        void OnEnable() => health.Damaged += OnDamaged;

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                SetColors(restore: true);
            }
        }

        void OnDamaged(int _)
        {
            if (!isActiveAndEnabled)
                return;

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            SetColors(restore: false);
            yield return new WaitForSeconds(duration);
            SetColors(restore: true);
            flashRoutine = null;
        }

        void SetColors(bool restore)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = restore ? originalColors[i] : flashColor;
        }
    }
}
