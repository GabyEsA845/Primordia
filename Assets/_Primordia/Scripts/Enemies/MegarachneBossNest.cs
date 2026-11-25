using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    public sealed class MegarachneBossNest : MonoBehaviour, IFlammable, IFlammableRadius
    {
        // PROVISIONAL: radio de ignición de la copa seca, aprobado para M5.
        public const float Radius = 2.5f;
        MegarachneBoss boss;
        public Vector2 Position => transform.position;
        public float IgnitionRadius => Radius;
        public bool CanIgnite => isActiveAndEnabled && boss != null && boss.isActiveAndEnabled
            && !boss.Health.IsDead && boss.NestCanBurn;
        internal void Configure(MegarachneBoss owner) => boss = owner;
        void OnEnable() => Flammables.Register(this);
        void OnDisable() => Flammables.Unregister(this);
        public void Ignite() { if (CanIgnite) boss.IgniteNest(); }
    }
}
