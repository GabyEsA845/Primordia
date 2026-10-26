using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Toques del Carroñero: Coraza de carroña (Trilobite: tras comer unos restos, el siguiente proyectil que te alcanza
    /// se deshace contra tus placas, <see cref="PlatesBounce.BounceNext"/>) y Doble ración (Hallucigenia: los enemigos que
    /// mata una púa dejan restos dobles, que cuentan como dos). Trabajan sobre el <see cref="CarrionBenefit"/>.
    /// </summary>
    public class CarrionTouch : MonoBehaviour
    {
        public enum Kind
        {
            Armor,
            DoubleServing
        }

        CarrionBenefit carrion;
        Kind kind;
        CarrionRemains doublePrefab;

        /// <summary>Veces que ha preparado las placas (para las pruebas).</summary>
        public int Armored { get; private set; }

        public void Configure(CarrionBenefit newCarrion, Kind newKind, CarrionRemains newDoublePrefab)
        {
            Unsubscribe();
            carrion = newCarrion;
            kind = newKind;
            doublePrefab = newDoublePrefab;
            if (carrion == null)
                return;
            if (kind == Kind.Armor)
                carrion.Eaten += OnEaten;
            else
                carrion.RemainsOverride = DoubleWhenSpineKills;
        }

        void OnEaten(CarrionRemains remains)
        {
            // Las placas se rehacen al subir de nivel: se buscan cada vez.
            if (TryGetComponent<PlatesBounce>(out var plates))
            {
                plates.BounceNext();
                Armored++;
            }
        }

        // La muerte se anuncia mientras la púa quita la vida.
        CarrionRemains DoubleWhenSpineKills(Enemy enemy) => GroundSpine.Striking != null ? doublePrefab : null;

        void Unsubscribe()
        {
            if (carrion == null)
                return;
            carrion.Eaten -= OnEaten;
            if (carrion.RemainsOverride == DoubleWhenSpineKills)
                carrion.RemainsOverride = null;
        }

        void OnDestroy() => Unsubscribe();
    }
}
