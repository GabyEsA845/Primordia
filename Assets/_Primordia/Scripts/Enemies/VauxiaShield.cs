using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Escudo leve que la Esponja Vauxia pone a los enemigos que cura: absorbe unos pocos puntos de daño mientras dura y
    /// se ve sobre su cuerpo. Un pulso nuevo lo recarga y alarga.
    /// </summary>
    public class VauxiaShield : MonoBehaviour
    {
        const string MarkerKey = "VauxiaShield";

        Health health;
        int points;
        float until;

        /// <summary>Daño que aún absorbe (0 si se rompió o se acabó).</summary>
        public int Points => Time.time < until ? points : 0;

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => Health.AddDamageModifier(Absorb);

        void OnDisable() => StatusMarkers.Hide(health, MarkerKey);

        /// <summary>Pone (o recarga) el escudo en ese objetivo: absorbe esos puntos durante esos segundos.</summary>
        public static VauxiaShield Give(Health target, int points, float duration, GameObject marker)
        {
            if (target == null || target.IsDead)
                return null;
            if (!target.TryGetComponent<VauxiaShield>(out var shield))
                shield = target.gameObject.AddComponent<VauxiaShield>();
            shield.points = Mathf.Max(shield.Points, points);
            shield.until = Time.time + duration;
            StatusMarkers.Show(target, MarkerKey, marker, duration, overhead: false);
            return shield;
        }

        /// <summary>El escudo se come el daño hasta donde le alcanza; al romperse, se quita su marca.</summary>
        static int Absorb(Health target, int amount)
        {
            if (amount <= 0 || !target.TryGetComponent<VauxiaShield>(out var shield) || !shield.isActiveAndEnabled)
                return amount;
            int available = shield.Points;
            if (available <= 0)
                return amount;
            int absorbed = Mathf.Min(available, amount);
            shield.points = available - absorbed;
            if (shield.points == 0)
                StatusMarkers.Hide(target, MarkerKey);
            return amount - absorbed;
        }
    }
}
