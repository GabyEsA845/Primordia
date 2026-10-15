using System;
using System.Collections.Generic;
using Primordia.Biomass;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Vigila las ranuras del jugador y activa el linaje cuyas 4 mutaciones lleva a la vez. Al cambiar una de ellas
    /// el linaje se pierde (se deshace su beneficio). Los linajes posibles salen de las mutaciones equipadas, así
    /// que no hace falta un catálogo. El beneficio son componentes en el jugador: uno por la clase del linaje y
    /// otro por su toque propio, que se quitan al perderlo.
    /// </summary>
    [RequireComponent(typeof(PlayerMutations))]
    public class PlayerLineage : MonoBehaviour
    {
        [Tooltip("Efecto sobre el jugador al completar un linaje.")]
        [SerializeField] GameObject activateEffect;
        [SerializeField] LineageBenefitSettings benefits = new();

        PlayerMutations mutations;
        readonly List<Component> benefitComponents = new();

        /// <summary>Linaje completo ahora mismo, o null.</summary>
        public LineageData Active { get; private set; }

        /// <summary>Se lanza al cambiar el linaje activo: (anterior, nuevo); cualquiera puede ser null.</summary>
        public event Action<LineageData, LineageData> ActiveChanged;

        void Awake() => mutations = GetComponent<PlayerMutations>();

        void OnEnable()
        {
            mutations.Changed += OnChanged;
            Refresh();
        }

        void OnDisable()
        {
            mutations.Changed -= OnChanged;
            SetActive(null);
        }

        internal void Configure(GameObject newActivateEffect) => activateEffect = newActivateEffect;

        internal LineageBenefitSettings Benefits => benefits;

        internal void ConfigureBenefits(LineageBenefitSettings newBenefits) => benefits = newBenefits ?? new LineageBenefitSettings();

        /// <summary>Componente de beneficio de ese tipo que lleva ahora (o null).</summary>
        public T BenefitOf<T>() where T : Component
        {
            foreach (var component in benefitComponents)
            {
                if (component != null && component is T found)
                    return found;
            }
            return null;
        }

        void OnChanged(MutationSlot slot, MutationData mutation) => Refresh();

        /// <summary>Busca el primer linaje completo entre los de las mutaciones equipadas.</summary>
        public void Refresh()
        {
            LineageData found = null;
            foreach (var mutation in mutations.Equipped)
            {
                foreach (var lineage in mutation.Lineages)
                {
                    if (lineage != null && lineage.IsComplete(mutations))
                    {
                        found = lineage;
                        break;
                    }
                }
                if (found != null)
                    break;
            }
            SetActive(found);
        }

        void SetActive(LineageData lineage)
        {
            if (lineage == Active)
                return;

            var old = Active;
            RemoveBenefits();

            Active = lineage;
            if (lineage != null)
            {
                ApplyBenefits(lineage);
                if (activateEffect != null && isActiveAndEnabled)
                    OneShotEffect.Spawn(activateEffect, transform.position, parent: transform);
            }

            ActiveChanged?.Invoke(old, lineage);
        }

        void ApplyBenefits(LineageData lineage)
        {
            var b = benefits;
            var touch = lineage.Touch;
            switch (lineage.Class)
            {
                case LineageClass.Carnivore:
                    Add<FinisherBenefit>().Configure(mutations.Controller, b.woundedThreshold, b.finisherMultiplier,
                        b.bossFinisherMultiplier, touch == LineageTouch.BlindPrey, touch == LineageTouch.BloodInTheWater, b.finishEffect);
                    break;

                case LineageClass.Herbivore:
                    Add<GrazingBenefit>().Configure(GetComponent<PlayerBiomass>(), mutations.Health, b.biomassPerHeal, b.healEffect);
                    break;

                case LineageClass.Venomous:
                    Add<ToxinBenefit>().Configure(b.toxinMultiplier);
                    break;

                case LineageClass.Fungal:
                    bool big = touch == LineageTouch.BigColony;
                    var mat = new MyceliumPatch.Settings
                    {
                        // Duran hasta que se limpia la sala, que las quita.
                        duration = float.MaxValue,
                        damagePerSecond = b.matDamagePerSecond,
                        slowFactor = b.matSlowFactor,
                        radius = b.matRadius * (big ? b.largeMatScale : 1f),
                    };
                    Add<ColonyBenefit>().Configure(big ? b.largeMatPrefab : b.matPrefab, mat, big ? b.largeMatCount : b.matCount);
                    break;
            }

            switch (touch)
            {
                case LineageTouch.Retract:
                    Add<RetractTouch>().Configure(mutations.Ability, mutations.Health, b.retractDuration, b.retractEffect);
                    break;

                case LineageTouch.StingingTrail:
                    var trail = new MyceliumPatch.Settings
                    {
                        duration = b.trailDuration,
                        slowFactor = 1f,
                        radius = b.trailRadius,
                        touchPoisonDamage = b.trailPoisonDamage,
                        touchPoisonDuration = b.trailPoisonDuration,
                        poisonEffect = b.poisonEffect,
                    };
                    Add<StingingTrailTouch>().Configure(mutations.Controller, b.trailPrefab, trail, b.trailSpacing);
                    break;

                case LineageTouch.HiddenSting:
                    Add<HiddenStingTouch>().Configure(mutations.Weapon, b.hiddenStingMultiplier);
                    break;

                case LineageTouch.PuffOnHit:
                    Add<PuffOnHitTouch>().Configure(mutations, b.puffRadius, b.puffCooldown);
                    break;
            }
        }

        T Add<T>() where T : Component
        {
            var component = gameObject.AddComponent<T>();
            benefitComponents.Add(component);
            return component;
        }

        void RemoveBenefits()
        {
            foreach (var component in benefitComponents)
            {
                if (component != null)
                    Destroy(component);
            }
            benefitComponents.Clear();
        }
    }
}
