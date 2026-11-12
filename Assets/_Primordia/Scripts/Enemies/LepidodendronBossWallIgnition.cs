using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Un punto de la superficie de la pared en el registro común de inflamables.</summary>
    public class LepidodendronBossWallIgnition : MonoBehaviour, IFlammable
    {
        LepidodendronWall wall;
        float height;
        public Vector2 Position => transform.position;
        public bool CanIgnite => wall != null && !wall.Fallen && !wall.IsBurning;
        internal void Configure(LepidodendronWall target, float y) { wall = target; height = y; }
        void OnEnable() => Flammables.Register(this);
        void OnDisable() => Flammables.Unregister(this);
        public void Ignite() { if (CanIgnite) wall.IgniteAt(height); }
    }
}
