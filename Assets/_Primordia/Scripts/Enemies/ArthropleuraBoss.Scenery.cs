using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso C2a (F2): el escenario de la sala abierta que da fuego al jugador y que el jefe aprovecha. Helechos
    /// repartidos (la cabeza los come al pasar; si ardían, se enciende) que rebrotan, y odres de metano más por toda la sala.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria). Posiciones relativas al centro de la sala.
        const float EatSeekRange = 9f, EatReach = 2.2f, EatSeconds = 0.8f, EatBiteAt = 0.1f, EatCooldownSeconds = 6f, FernRegrowSeconds = 25f;
        static readonly Vector2[] FernSpots =
        {
            new(-13f, -8f), new(-13f, 8f), new(13f, -8f), new(13f, 8f), new(-5f, 0f), new(5f, 0f),
        };
        // Más odres en la parte que antes era espesura (los cuatro del claro siguen).
        static readonly Vector2[] Phase2SacSpots = { new(0f, -10f), new(0f, 10f) };

        sealed class FernSlot
        {
            public Vector2 position;
            public Fern fern;
            public float regrowAt = float.PositiveInfinity;
        }

        [SerializeField] FernSpec fernSpec;
        readonly List<FernSlot> ferns = new();
        bool sceneryBuilt;
        float eatCooldownAt;
        FernSlot eating;
        bool bitten;

        internal void ConfigureFern(FernSpec spec) => fernSpec = spec;

        /// <summary>Helechos vivos (comestibles) ahora mismo.</summary>
        public int FernCount
        {
            get
            {
                int count = 0;
                foreach (var slot in ferns)
                    if (slot.fern != null && slot.fern.IsFood) count++;
                return count;
            }
        }

        /// <summary>Para pruebas.</summary>
        internal Fern FernAt(int index) => ferns[index].fern;

        /// <summary>Para pruebas: la cabeza empieza a comerse el helecho indicado.</summary>
        internal void TestEat(int index)
        {
            eating = ferns[index];
            bitten = false;
            SetMode(Mode.Eat);
        }

        void BuildPhase2Scenery()
        {
            if (sceneryBuilt)
                return;
            sceneryBuilt = true;
            if (fernSpec != null)
                foreach (var spot in FernSpots)
                {
                    var slot = new FernSlot { position = center + spot };
                    ferns.Add(slot);
                    GrowFern(slot);
                }
            foreach (var spot in Phase2SacSpots)
            {
                var slot = new SacSlot { position = center + spot };
                sacs.Add(slot);
                GrowSac(slot);
            }
        }

        void GrowFern(FernSlot slot)
        {
            if (fernSpec == null)
                return;
            if (slot.fern != null)
                Destroy(slot.fern.gameObject);
            var obj = new GameObject("FernF2");
            obj.transform.SetParent(transform, true);
            obj.transform.position = slot.position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            var trigger = obj.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = fernSpec.footRadius;
            trigger.offset = new Vector2(0f, fernSpec.footRadius * 0.6f);
            slot.fern = obj.AddComponent<Fern>();
            slot.fern.Configure(renderer, fernSpec, Random.value * 10f);
            slot.regrowAt = float.PositiveInfinity;
        }

        /// <summary>Los helechos comidos o quemados rebrotan a los 25 s.</summary>
        void TickScenery()
        {
            foreach (var slot in ferns)
            {
                bool gone = slot.fern == null || slot.fern.IsBurnt;
                if (gone && float.IsPositiveInfinity(slot.regrowAt))
                    slot.regrowAt = fireClock + FernRegrowSeconds;
                if (!float.IsPositiveInfinity(slot.regrowAt) && fireClock >= slot.regrowAt)
                    GrowFern(slot);
            }
        }

        FernSlot NearestFern(Vector2 headGround, float range)
        {
            FernSlot best = null;
            float bestDistance = range;
            foreach (var slot in ferns)
            {
                if (slot.fern == null || !slot.fern.IsFood)
                    continue;
                float distance = Vector2.Distance(WorldToGround(slot.position), headGround);
                if (distance <= bestDistance)
                {
                    best = slot;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Tiene hambre: si hay un helecho a mano, va a por él en vez de seguir al jugador.</summary>
        bool TryEatAt(ref Vector2 target)
        {
            if (fireClock < eatCooldownAt)
                return false;
            var snack = NearestFern(headPos, EatSeekRange);
            if (snack == null)
                return false;
            target = WorldToGround(snack.position);
            if (Vector2.Distance(headPos, target) > EatReach)
                return true;
            eating = snack;
            bitten = false;
            SetMode(Mode.Eat);
            return true;
        }

        void StepEat()
        {
            if (!bitten && modeTimer >= EatBiteAt)
            {
                bitten = true;
                // El bocado es el cuadro 1 de Head_Eat: el helecho desaparece y, si ardía, la cabeza se enciende.
                bool burning = eating.fern != null && eating.fern.IsBurning;
                if (eating.fern != null)
                    Destroy(eating.fern.gameObject);
                eating.fern = null;
                eating.regrowAt = fireClock + FernRegrowSeconds;
                if (burning)
                    IgnitePiece(0);
            }
            if (modeTimer >= EatSeconds)
            {
                eating = null;
                eatCooldownAt = fireClock + EatCooldownSeconds;
                StartChase();
            }
        }
    }
}
