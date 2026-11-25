using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Entrada del minijefe (M6): con el cuadro de presentación del jefe en marcha, los ojos se encienden en el embudo del Nido, asoma
    /// (IntroEmerge), baja colgada hasta el suelo, alza las delanteras (Threat) y espera a que acabe el rótulo (BeginFight). Si el
    /// jugador salta el rótulo, la entrada acaba al momento. Sin presentación (pruebas, sin GameManager) no hay entrada.
    /// </summary>
    public partial class MegarachneBoss
    {
        // PROVISIONALES: tiempos de la entrada; las hojas de IntroEmerge (8 × 110 ms), Drop (0,48 s) y Threat (6 × 110 ms) son las aprobadas.
        const float EyesSeconds = 0.7f, EyesFrameSeconds = 0.1f, ThreatMs = 110f;
        const float EntryShakePixels = 2f, EntryShakeSeconds = 0.5f;
        const int EyesOrder = 22;

        enum EntryStage { None, Waiting, Eyes, Emerge, Descend, Threat }
        EntryStage entryStage;
        float entryTime;
        SpriteRenderer entryEyes;
        bool entryFight;

        /// <summary>True desde que la sala se activa con presentación hasta que acaba la entrada.</summary>
        public bool IsEntering => entryStage != EntryStage.None;

        /// <summary>True cuando ya bajó y amenaza, esperando a que acabe el rótulo.</summary>
        public bool EntryAtThreat => entryStage == EntryStage.Threat;

        /// <summary>Punto al que mira la cámara en la presentación: el embudo del Nido, de donde sale.</summary>
        public Vector2 IntroFocus => NestPosition;

        void BeginIntroIfAny()
        {
            var intro = GetComponent<BossIntro>();
            if (intro == null || intro.Data == null || intro.Played || GameManager.Instance == null || Canopy == null) return;
            StartEntry();
        }

        // Escondida en el embudo hasta que empiece la pose de la presentación.
        internal void StartEntry()
        {
            if (Health.IsDead) return;
            entryStage = EntryStage.Waiting;
            entryFight = false;
            Health.IsInvulnerable = true;
            hit.enabled = false;
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            if (thread != null) thread.enabled = false;
            body.position = ArenaCenter + Vector2.down * GroundApproach;
            SetHeight(0f);
        }

        public void PlayIntroPose(float hold)
        {
            if (entryStage != EntryStage.Waiting) return;
            entryStage = EntryStage.Eyes;
            entryTime = 0f;
            Sfx.Play(Sounds.BossMegarachneIntro);
            ScreenShake.Pulse(EntryShakePixels, EntryShakeSeconds);
        }

        public void BeginFight()
        {
            entryFight = true;
            if (IsEntering) FinishEntry();
        }

        bool TickEntry(float dt)
        {
            if (!IsEntering) return false;
            entryTime += dt;
            switch (entryStage)
            {
                case EntryStage.Eyes:
                    ShowEyes(entryTime);
                    if (entryTime < EyesSeconds) break;
                    SetHeight(FunnelHeight + GroundApproach);
                    body.position = NestPosition - Vector2.up * VisualHeight;
                    facing = Vector2.down;
                    if (bodyRenderer != null) bodyRenderer.enabled = true;
                    if (bodySprite != null) bodySprite.Play("Megarachne_IntroEmerge", EmergeMs, false, restart: true);
                    entryStage = EntryStage.Emerge;
                    entryTime = 0f;
                    break;
                case EntryStage.Emerge:
                    ShowEyes(EyesSeconds + entryTime);
                    if (entryTime < EmergeSeconds) break;
                    HideEyes();
                    dropHeight = VisualHeight;
                    body.position = ArenaCenter + Vector2.down * GroundApproach;
                    routeStart = body.position + Vector2.up * (VisualHeight + SpinneretOffset);
                    if (shadowRenderer != null) shadowRenderer.enabled = true;
                    entryStage = EntryStage.Descend;
                    entryTime = 0f;
                    Sfx.Play(Sounds.BossMegarachneDrop);
                    break;
                case EntryStage.Descend:
                    SetHeight(Mathf.Lerp(dropHeight, 0f, entryTime / DropSeconds));
                    Play("Drop", DropMs);
                    DrawThread(routeStart);
                    if (entryTime < DropSeconds) break;
                    SetHeight(0f);
                    if (thread != null) thread.enabled = false;
                    FaceTarget();
                    entryStage = EntryStage.Threat;
                    entryTime = 0f;
                    break;
                case EntryStage.Threat:
                    // Amenaza hasta que acaba el rótulo (BeginFight); sin él, acaba cuando termina la animación.
                    Play("Threat", ThreatMs);
                    if (entryFight) FinishEntry();
                    break;
            }
            return true;
        }

        void FaceTarget()
        {
            if (target == null) return;
            Vector2 toTarget = (Vector2)target.position - body.position;
            if (toTarget.sqrMagnitude > 0.01f) facing = toTarget.normalized;
        }

        void ShowEyes(float time)
        {
            var frames = EffectFrames("Nido_Eyes");
            if (frames.Length == 0 || Canopy == null) return;
            if (entryEyes == null)
            {
                var go = new GameObject("Ojos");
                go.transform.SetParent(Canopy.transform, false);
                entryEyes = go.AddComponent<SpriteRenderer>();
                entryEyes.sortingOrder = EyesOrder;
            }
            entryEyes.enabled = true;
            entryEyes.sprite = frames[Mathf.FloorToInt(time / EyesFrameSeconds) % frames.Length];
        }

        void HideEyes()
        {
            if (entryEyes != null) entryEyes.enabled = false;
        }

        // Acaba la entrada (también si se salta el rótulo): en el suelo, delante del jugador, y empieza el combate.
        void FinishEntry()
        {
            entryStage = EntryStage.None;
            HideEyes();
            hit.enabled = true;
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            if (shadowRenderer != null) shadowRenderer.enabled = true;
            body.position = ArenaCenter + Vector2.down * GroundApproach;
            FaceTarget();
            Land();
            attackClock = 0f;
            ActiveBoss.Set(this);
        }
    }
}
