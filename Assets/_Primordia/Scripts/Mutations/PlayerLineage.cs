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
        readonly List<LineageData> candidates = new();
        LineageData preferred;

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
            Disabling = true;
            SetActive(null);
            Disabling = false;
        }

        /// <summary>
        /// True mientras quita el linaje porque el jugador se desactiva (al cerrar la escena): no es que se haya
        /// perdido jugando, así que no hay que crear efectos.
        /// </summary>
        public bool Disabling { get; private set; }

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

        /// <summary>
        /// Busca los linajes completos entre los de las mutaciones equipadas. Sin comodín solo puede haber uno; con una
        /// mutación especial puede haber dos (los linajes que solo se diferencian en esa ranura): entonces sigue el que ya
        /// estaba activo o el que eligió antes, y si no, el jugador elige (<see cref="ChoiceNeeded"/>).
        /// </summary>
        public void Refresh()
        {
            candidates.Clear();
            foreach (var mutation in mutations.Equipped)
            {
                foreach (var lineage in mutation.Lineages)
                {
                    if (lineage != null && !candidates.Contains(lineage) && lineage.IsComplete(mutations))
                        candidates.Add(lineage);
                }
            }

            AwaitingChoice = false;
            if (candidates.Count == 0)
                SetActive(null);
            else if (candidates.Count == 1)
                SetActive(candidates[0]);
            else if (Active != null && candidates.Contains(Active))
                SetActive(Active);
            else if (preferred != null && candidates.Contains(preferred))
                SetActive(preferred);
            else if (ChoiceNeeded == null)
                // Sin cartel que pregunte (pruebas, escenas sin interfaz): el primero.
                SetActive(candidates[0]);
            else
            {
                SetActive(null);
                AwaitingChoice = true;
                ChoiceNeeded.Invoke(candidates);
            }
        }

        /// <summary>Linajes completos ahora mismo (dos si el comodín completa dos a la vez).</summary>
        public IReadOnlyList<LineageData> Candidates => candidates;

        /// <summary>True si hay dos linajes completos y el jugador aún no ha elegido.</summary>
        public bool AwaitingChoice { get; private set; }

        /// <summary>El comodín completa dos linajes a la vez y hay que elegir uno: se lanza con los dos.</summary>
        public event Action<IReadOnlyList<LineageData>> ChoiceNeeded;

        /// <summary>Activa ese linaje de los completos (el cartel al completarlos o el cambio desde la pausa) y lo recuerda.</summary>
        public void Choose(LineageData lineage)
        {
            if (lineage == null || !candidates.Contains(lineage))
                return;
            preferred = lineage;
            AwaitingChoice = false;
            SetActive(lineage);
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
