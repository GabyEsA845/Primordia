using Primordia.Biomass;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Cofre de cono gigante de Lepidodendron (los cofres del piso 3): una <see cref="Clam"/> con otro nombre y otro dibujo. Se abre con
    /// E y suelta la biomasa grande del piso. Lleva el arte de <see cref="SpecialRoomArt"/> (estróbilo grande de 2 casillas o pequeño de 1);
    /// sin él, el dibujo provisional <see cref="ConeChestPlaceholder"/>.
    /// </summary>
    public static class ConeChest
    {
        public const string Title = "Cono de Lepidodendron";

        /// <summary>Crea el cofre en esa posición (el centro de sus dos casillas) colgando de <paramref name="parent"/>.</summary>
        internal static Clam Spawn(Transform parent, Vector2 position, FloorConfig config, BiomassPickup small, BiomassPickup big, bool large = true)
        {
            // La suelta de la biomasa va en el cuadro 4 de la apertura (el que cuenta, según el README del arte).
            const int ReleaseFrame = 3;
            var art = SpecialRoomArt.Current;
            var cone = art != null ? (large ? art.coneBig : art.coneSmall) : null;
            bool real = cone != null && cone.IsValid;
            var idle = real ? cone.idle : ConeChestPlaceholder.Idle;
            var open = real ? cone.open : ConeChestPlaceholder.Open;
            var opened = real ? cone.opened : ConeChestPlaceholder.Opened;

            var go = new GameObject("ConeChest");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var body = go.AddComponent<SpriteRenderer>();
            body.sortingOrder = 5;
            body.sprite = idle[0];
            go.AddComponent<BoxCollider2D>().size = large ? new Vector2(2f, 1f) : Vector2.one;
            var clam = go.AddComponent<Clam>();
            var biomass = config != null ? config.Clams.bigBiomass : new Vector2Int(15, 25);
            clam.Configure(body, idle, open, opened, true, biomass, null, null, small, big);
            if (real)
                clam.SetReleaseFrame(ReleaseFrame);
            if (config != null)
                clam.ConfigurePrompt(config.Pieces.keyPrompt, large ? 1.4f : 1f);
            clam.SetRadius(large ? 1.8f : 1.4f);
            clam.SetTitle(Title);
            return clam;
        }
    }
}
