using System;
using Primordia.Core;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Muerte y recompensa del minijefe (M6): rueda panza arriba (Death/Dead), el Nido se deshace y los cables se descuelgan, y del embudo
    /// cae un capullo de seda que se abre con un contenedor de corazón (una vez por partida), biomasa y tarjetas para subir un nivel una
    /// mutación equipada. Lo que sobrevive al jefe (el cuerpo y la recompensa) vive en un <see cref="MegarachneCorpse"/> de la sala.
    /// </summary>
    public partial class MegarachneBoss
    {
        [SerializeField] EffectSheet[] finalEffects = Array.Empty<EffectSheet>();
        [SerializeField] Sprite[] rewardHeart = Array.Empty<Sprite>();
        bool deathStarted;

        internal void ConfigureFinal(EffectSheet[] sheets, Sprite[] heart)
        {
            finalEffects = sheets;
            rewardHeart = heart;
        }

        public bool DeathStarted => deathStarted;

        void StartDeath()
        {
            if (deathStarted) return;
            deathStarted = true;
            entryStage = EntryStage.None;
            ActiveBoss.Clear(this);
            ClearAttacks();
            KillAllBrood();
            travel = Travel.None;
            if (thread != null) thread.enabled = false;
            HideEyes();
            SetHeight(0f);

            var parent = arena != null ? arena : transform.parent;
            var corpse = new GameObject("MegarachneCorpse");
            corpse.transform.SetParent(parent, true);
            corpse.transform.position = transform.position;
            // Enemy sigue emitiendo Died y destruyéndose: el cuerpo y su sombra pasan a la sala para seguir ahí.
            if (bodyRenderer != null) bodyRenderer.transform.SetParent(corpse.transform, true);
            if (shadowRenderer != null) shadowRenderer.transform.SetParent(corpse.transform, true);
            DropCables();
            var player = target != null ? target : FindAnyObjectByType<PlayerController>()?.transform;
            corpse.AddComponent<MegarachneCorpse>().Configure(new MegarachneCorpse.Setup
            {
                arena = parent, canopy = Canopy, body = bodySprite, shadow = shadowSprite, bodyRenderer = bodyRenderer, shadowRenderer = shadowRenderer,
                facing = facing, nest = NestPosition, center = ArenaCenter, player = player,
                webDecay = EffectFrames("Nido_WebDecay"), cocoonFall = EffectFrames("Megarachne_Cocoon_Fall"),
                cocoonOpen = EffectFrames("Megarachne_Cocoon_Open"), dust = EffectFrames("Fx_MegarachneDust"), heart = rewardHeart,
            });
        }

        // Los cables se descuelgan: se quitan sus dibujos (el jefe se destruye con sus materiales) y la seda se deshace en el Nido.
        void DropCables()
        {
            if (scenery == null) return;
            foreach (var line in scenery.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
            foreach (var renderer in scenery.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string name = renderer.gameObject.name;
                if (name.StartsWith("Fx_") || name.StartsWith("Cable_") || name.Contains("CableRoot")) renderer.enabled = false;
            }
            foreach (var node in scenery.GetComponentsInChildren<MegarachneBossNest>(true)) node.enabled = false;
        }
    }
}
