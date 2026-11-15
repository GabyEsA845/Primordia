using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>Arthropleura, subpaso F: las flechas del borde de pantalla (cabeza, cola y punto débil).</summary>
    public partial class ArthropleuraBoss
    {
        [SerializeField] EdgeArrowArt edgeArrowArt = new();
        ArthropleuraEdgeArrows edgeArrows;

        internal void ConfigureEdgeArrows(EdgeArrowArt art) => edgeArrowArt = art;
        internal ArthropleuraEdgeArrows EdgeArrows => edgeArrows;

        internal Vector2 HeadWorld => wheelActive ? wheelPos + Vector2.up * 1.5f : GroundToWorld(NodeGround(0));
        internal Vector2 TailWorld => GroundToWorld(NodeGround(pieces.Count - 1));

        /// <summary>La cabeza está a punto de embestir (aviso de embestida o de la rueda): su insignia parpadea.</summary>
        internal bool IsAboutToAttack => CurrentMode == Mode.Warn || CurrentMode == Mode.WheelWarn;

        /// <summary>Un punto débil abierto (placas abiertas por fuego o por tramo): el centro de las piezas abiertas.</summary>
        internal bool TryGetWeakPoint(out Vector2 world)
        {
            world = default;
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var piece in firePieces)
                if (piece.Open)
                {
                    sum += piece.Position;
                    count++;
                }
            if (count == 0)
                return false;
            world = sum / count;
            return true;
        }

        void UpdateEdgeArrows()
        {
            bool wanted = IntroStage == EntryStage.Complete && !Health.IsDead && CurrentMode != Mode.Dead && edgeArrowArt != null
                && edgeArrowArt.badgeHead.Length > 0;
            if (wanted && edgeArrows == null)
                edgeArrows = ArthropleuraEdgeArrows.Create(this, edgeArrowArt);
        }
    }
}
