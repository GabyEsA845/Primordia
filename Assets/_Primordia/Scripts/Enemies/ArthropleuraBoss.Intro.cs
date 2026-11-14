using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class ArthropleuraBoss
    {
        // Provisionales: recorrido y temblores de la presentación, pendientes de revisión visual.
        const float IntroSpeed = 12f, IntroStartX = 24f, IntroCenterRadius = 3f, PeekDepth = 1.5f;
        const float IntroShakeSeconds = 1.5f, IntroShakePixels = 3f, IntroRumblePixels = 0.5f;
        const float BreachShakeSeconds = 0.35f, BreachShakePixels = 4f, IntroTremorSeconds = 0.3f;
        const float IntroRingStep = 0.005f;
        public enum EntryStage { Sleeping, Arriving, Peeking, Roaring, Complete }
        public EntryStage IntroStage { get; private set; }
        readonly List<Vector2> introRoute = new();
        int introTarget, entrySide = 1;
        float nextIntroTremor, roarAt;
        bool firstCharge;
        // Las pruebas aisladas sin sala conservan el acceso directo al fuego de B2.
        bool FireAllowed => thicketBuilt ? entryCells.Count == 0 || IntroStage == EntryStage.Complete : wallRoom != null && wallRoom.parent == null;

        void SetSacsAvailable(bool available)
        {
            foreach (var slot in sacs)
            {
                if (slot.sac == null) continue;
                slot.sac.enabled = available;
                slot.sac.GetComponent<Health>().IsInvulnerable = !available;
            }
        }

        void WakeUp()
        {
            awake = true;
            modeTimer = 0f;
            if (entryCells.Count == 0)
            {
                IntroStage = EntryStage.Complete;
                ActiveBoss.Set(this);
                return;
            }
            IntroStage = EntryStage.Arriving;
            int side = -entrySide;
            headPos = new Vector2(side * IntroStartX, 0f);
            // Toda la cadena empieza en fila fuera de la vista, sin nudos superpuestos.
            for (int i = 0; i < trail.Count; i++)
                trail[i] = headPos + Vector2.right * (side * i * TrailSpacing);
            heading = side > 0 ? 180f : 0f;
            introRoute.Clear();
            introTarget = 0;
            float start = side > 0 ? 0f : Mathf.PI;
            float turn = side > 0 ? -1f : 1f;
            introRoute.Add(RingPoint(start));
            for (float angle = IntroRingStep; angle < Mathf.PI * 1.5f; angle += IntroRingStep)
                introRoute.Add(RingPoint(start + turn * angle));
            introRoute.Add(new Vector2(0f, RingHalf.y));
            ScreenShake.Pulse(IntroShakePixels, IntroShakeSeconds);
            ScreenShake.SetRumble(this, IntroRumblePixels);
            nextIntroTremor = 0f;
            Place();
        }

        void StepIntro(float dt)
        {
            if (IntroStage == EntryStage.Roaring) return;
            float budget = IntroSpeed * dt;
            while (IntroStage == EntryStage.Arriving && budget > 0f && introTarget < introRoute.Count)
            {
                Vector2 target = introRoute[introTarget];
                float distance = Vector2.Distance(headPos, target);
                if (distance > 0.00001f)
                {
                    float moved = Mathf.Min(distance, budget);
                    Advance(target, IntroSpeed, float.MaxValue, moved / IntroSpeed);
                    budget -= moved;
                }
                if (Vector2.Distance(headPos, target) < 0.001f) introTarget++;
                if (budget <= 0f) break;
            }
            if (IntroStage == EntryStage.Arriving && introTarget == introRoute.Count)
            {
                IntroStage = EntryStage.Peeking;
                FaceHead(Vector2.down);
                BreakLine(headPos, Vector2.down, -1f, RingHalf.y * GroundSquash - ClearingHalf.y + PeekDepth);
                SpawnEffect(effects.edgeBreak, center + Vector2.up * ClearingHalf.y, 0f, 0.08f);
                ScreenShake.ClearRumble(this);
                ScreenShake.Pulse(BreachShakePixels, BreachShakeSeconds);
                return; // El borde revienta antes de que avance la cabeza.
            }
            if (IntroStage == EntryStage.Peeking)
            {
                Vector2 target = new(0f, (ClearingHalf.y - PeekDepth) / GroundSquash);
                float distance = Vector2.Distance(headPos, target);
                Advance(target, IntroSpeed, float.MaxValue, Mathf.Min(dt, distance / IntroSpeed));
                if (Vector2.Distance(headPos, target) < 0.001f)
                {
                    IntroStage = EntryStage.Roaring;
                    roarAt = Time.time;
                    ActiveBoss.Set(this);
                    var presentation = GetComponent<BossIntro>();
                    if (presentation != null) presentation.enabled = true;
                }
            }
            if (IntroStage == EntryStage.Arriving && Time.time >= nextIntroTremor)
            {
                nextIntroTremor = Time.time + IntroTremorSeconds;
                SpawnEffect(effects.tremor, GroundToWorld(headPos), 0f, 0.09f);
                var spot = new Vector2(Random.Range(-ClearingHalf.x + 2f, ClearingHalf.x - 2f), Random.Range(-ClearingHalf.y + 2f, ClearingHalf.y - 2f));
                SpawnEffect(effects.tremor, center + spot, 0f, 0.09f);
            }
        }

        internal void ForcePresentation()
        {
            if (!thicketBuilt || awake || entryCells.Count == 0) return;
            if (!entriesSealed)
            {
                entrySide = player != null && player.position.x < center.x ? -1 : 1;
                SealEntries();
            }
            WakeUp();
        }

        public void PlayIntroPose(float hold)
        {
            // El cuadro 3 del rugido se mantiene hasta BeginFight, incluso si se salta el rótulo.
        }

        public void BeginFight()
        {
            if (IntroStage != EntryStage.Roaring || !isActiveAndEnabled) return;
            IntroStage = EntryStage.Complete;
            SetSacsAvailable(true);
            firstCharge = true;
            chargeDirection = Vector2.down;
            FaceHead(chargeDirection);
            SetMode(Mode.Warn);
            UpdateAim(headPos);
        }
    }
}
