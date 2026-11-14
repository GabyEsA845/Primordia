using System;
using System.Collections.Generic;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso C2b-1 (F2): los lagos de lodo profundo de la sala (marca <c>m</c> de la plantilla, uno a cada lado).
    /// Una pieza ardiendo que pisa lodo se apaga (queda chamuscada y cierra las placas). Con muchas piezas ardiendo la cabeza deja
    /// de perseguir y corre al lago más cercano para dar vueltas encima hasta que el cuerpo entero se moja.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        // Provisionales (a ojo, pendientes de la prueba de la usuaria).
        const int DouseMinBurning = 5;
        const float DouseSpeed = 14f, DouseTurn = 300f, DouseSeconds = 4.5f, DouseGiveUpSeconds = 9f;
        const float DouseRadius = 3f, DouseCooldownSeconds = 12f, LakeSearchStep = 1f, LakeClusterRadius = 7f;
        // Cuánto aguanta ardiendo (con 5 o más piezas) antes de ir al lodo: ese tiempo le hace daño.
        const float DouseDelaySeconds = 4f;

        /// <summary>Si hay lodo profundo en ese punto del mundo (las pruebas la sustituyen).</summary>
        internal Func<Vector2, bool> MudProbe = position => Pits.IsMud(position);

        Vector2 douseCenter; // Suelo.
        float douseUntil, douseAngle, douseCooldownAt, burnSince = float.PositiveInfinity;
        bool douseReached;

        public int BurningPieces
        {
            get
            {
                int count = 0;
                foreach (var piece in firePieces)
                    if (piece.state == FireState.Burning) count++;
                return count;
            }
        }

        /// <summary>Apaga las piezas ardiendo que pisan lodo (solo a partir de la F2: en la F1 el lodo está bajo la espesura).</summary>
        void ExtinguishInMud()
        {
            if (PhaseIndex < 1)
                return;
            foreach (var piece in firePieces)
            {
                if (piece.state != FireState.Burning || !MudProbe(piece.Position))
                    continue;
                piece.state = FireState.Charred;
                piece.charred = true;
                piece.spreadAt = float.PositiveInfinity;
                piece.cooledAt = fireClock + CharredCoolSeconds;
            }
        }

        /// <summary>Centro (en suelo) del lago de lodo más cercano a la cabeza, buscando por toda la sala.</summary>
        bool TryFindLake(out Vector2 lakeGround)
        {
            lakeGround = Vector2.zero;
            var mud = new List<Vector2>();
            float halfX = ArenaWidth / 2f, halfY = ArenaHeight / 2f;
            for (float x = -halfX + 1f; x < halfX; x += LakeSearchStep)
                for (float y = -halfY + 1f; y < halfY; y += LakeSearchStep)
                    if (MudProbe(center + new Vector2(x, y)))
                        mud.Add(new Vector2(x, y / GroundSquash));
            if (mud.Count == 0)
                return false;
            Vector2 nearest = mud[0];
            foreach (var cell in mud)
                if ((cell - headPos).sqrMagnitude < (nearest - headPos).sqrMagnitude)
                    nearest = cell;
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var cell in mud)
                if (Vector2.Distance(cell, nearest) <= LakeClusterRadius)
                {
                    sum += cell;
                    count++;
                }
            lakeGround = sum / count;
            return true;
        }

        bool TryStartDouse()
        {
            if (BurningPieces < DouseMinBurning)
            {
                burnSince = float.PositiveInfinity;
                return false;
            }
            if (float.IsPositiveInfinity(burnSince))
                burnSince = fireClock;
            if (fireClock < douseCooldownAt || fireClock - burnSince < DouseDelaySeconds || !TryFindLake(out var lake))
                return false;
            douseCenter = lake;
            douseReached = false;
            SetMode(Mode.Douse);
            return true;
        }

        void StepDouse(float dt)
        {
            if (!douseReached)
            {
                Advance(AvoidTrees(douseCenter), DouseSpeed, DouseTurn, dt);
                if (Vector2.Distance(headPos, douseCenter) < DouseRadius)
                {
                    douseReached = true;
                    douseUntil = modeTimer + DouseSeconds;
                    Vector2 from = headPos - douseCenter;
                    douseAngle = Mathf.Atan2(from.y, from.x);
                }
            }
            else
            {
                // Da vueltas sobre el lago: el cuerpo entero acaba pasando por el lodo.
                douseAngle += DouseSpeed / DouseRadius * dt;
                Advance(douseCenter + new Vector2(Mathf.Cos(douseAngle), Mathf.Sin(douseAngle)) * DouseRadius, DouseSpeed, DouseTurn, dt);
            }
            bool done = douseReached && (modeTimer >= douseUntil || BurningPieces == 0);
            if (done || modeTimer >= DouseGiveUpSeconds)
            {
                douseCooldownAt = fireClock + DouseCooldownSeconds;
                burnSince = float.PositiveInfinity;
                StartChase();
            }
        }
    }
}
