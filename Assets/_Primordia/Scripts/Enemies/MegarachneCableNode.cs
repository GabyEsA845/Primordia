using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    internal sealed class MegarachneCableNode : MonoBehaviour, IFlammable
    {
        MegarachneBoss boss;
        int cable;
        float distance;

        public Vector2 Position => transform.position;
        public bool CanIgnite => isActiveAndEnabled && boss != null && boss.CanIgniteCable(cable);

        internal void Configure(MegarachneBoss owner, int index, float alongCable)
        {
            boss = owner;
            cable = index;
            distance = alongCable;
        }

        void OnEnable() => Flammables.Register(this);
        void OnDisable() => Flammables.Unregister(this);

        public void Ignite()
        {
            if (CanIgnite) boss.IgniteCable(cable, distance);
        }
    }
}
