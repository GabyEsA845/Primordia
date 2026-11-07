using Primordia.Biomass;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Cofre de cono gigante de Lepidodendron (los cofres del piso 3): una <see cref="Clam"/> con otro nombre y otro dibujo. Se abre con
    /// E y suelta la biomasa grande del piso. El dibujo es PROVISIONAL (<see cref="ConeChestPlaceholder"/>) hasta tener el arte.
    /// </summary>
    public static class ConeChest
    {
        public const string Title = "Cono de Lepidodendron";

        /// <summary>Crea el cofre en esa posición (el centro de sus dos casillas) colgando de <paramref name="parent"/>.</summary>
        internal static Clam Spawn(Transform parent, Vector2 position, FloorConfig config, BiomassPickup small, BiomassPickup big)
        {
            var go = new GameObject("ConeChest");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var body = go.AddComponent<SpriteRenderer>();
            body.sortingOrder = 5;
            body.sprite = ConeChestPlaceholder.Idle[0];
            go.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
            var clam = go.AddComponent<Clam>();
            var biomass = config != null ? config.Clams.bigBiomass : new Vector2Int(15, 25);
            clam.Configure(body, ConeChestPlaceholder.Idle, ConeChestPlaceholder.Open, ConeChestPlaceholder.Opened, true, biomass, null, null, small, big);
            if (config != null)
                clam.ConfigurePrompt(config.Pieces.keyPrompt, 1.4f);
            clam.SetRadius(1.8f);
            clam.SetTitle(Title);
            return clam;
        }
    }
}
