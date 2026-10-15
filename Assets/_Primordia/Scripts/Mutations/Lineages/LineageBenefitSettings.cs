using System;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Valores y efectos de los beneficios de los linajes (los usa <see cref="PlayerLineage"/>).</summary>
    [Serializable]
    public class LineageBenefitSettings
    {
        [Header("Carnívoro: Rematar")]
        [Tooltip("Un enemigo con esta fracción de su vida o menos cuenta como herido.")]
        [Range(0.05f, 1f)] public float woundedThreshold = 0.5f;
        [Min(1f)] public float finisherMultiplier = 2f;
        [Tooltip("Contra el jefe.")]
        [Min(1f)] public float bossFinisherMultiplier = 1.5f;
        [Tooltip("Sangre en el agua (Tiburón): efecto al recargar la esquiva.")]
        public GameObject finishEffect;

        [Header("Herbívoro: Pastar biomasa")]
        [Min(1)] public int biomassPerHeal = 10;
        public GameObject healEffect;

        [Header("Venenoso: Toxina débil")]
        [Min(1f)] public float toxinMultiplier = 1.5f;

        [Header("Hongo: Colonizar la sala")]
        public MyceliumPatch matPrefab;
        [Tooltip("Colonia grande (Moho): la mata grande.")]
        public MyceliumPatch largeMatPrefab;
        [Min(0)] public int matCount = 3;
        [Min(0)] public int largeMatCount = 5;
        [Min(0.1f)] public float matRadius = 1.2f;
        [Tooltip("Colonia grande: cuánto más grandes (el radio).")]
        [Min(1f)] public float largeMatScale = 1.3f;
        [Tooltip("Velocidad de los enemigos encima (0,6 = un 40 % más lentos).")]
        [Range(0.05f, 1f)] public float matSlowFactor = 0.6f;
        [Min(0)] public int matDamagePerSecond = 1;

        [Header("Tortuga: Retraerse")]
        [Min(0f)] public float retractDuration = 1.5f;
        public GameObject retractEffect;

        [Header("Medusa: Estela urticante")]
        public MyceliumPatch trailPrefab;
        [Min(0.1f)] public float trailRadius = 0.45f;
        [Min(0.1f)] public float trailDuration = 3f;
        [Min(0.1f)] public float trailSpacing = 0.5f;
        [Min(1)] public int trailPoisonDamage = 1;
        [Min(0.1f)] public float trailPoisonDuration = 3f;
        public GameObject poisonEffect;

        [Header("Escorpión: Picadura oculta")]
        [Min(1f)] public float hiddenStingMultiplier = 3f;

        [Header("Cuesco de lobo: Bocanada al golpe")]
        [Min(0.1f)] public float puffRadius = 2f;
        [Min(0f)] public float puffCooldown = 6f;
    }
}
