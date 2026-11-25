using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Biomass;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Lo que queda de Megarachne en su sala (M6): el cuerpo panza arriba, la red del Nido que se deshace y la recompensa. Del embudo cae
    /// un capullo de seda que se abre con un contenedor de corazón (solo una vez por partida), biomasa y, pasado un momento, las
    /// tarjetas para subir un nivel una mutación equipada que no esté al máximo.
    /// </summary>
    public sealed class MegarachneCorpse : MonoBehaviour
    {
        public const string HeartFlag = "Megarachne.Heart";
        // PROVISIONALES: tiempos y cantidades de la muerte y la recompensa.
        public const int BiomassReward = 15;
        const float DeathMs = 100f, DeadHoldSeconds = 1.2f, WebFrameSeconds = 0.12f, CocoonFallSeconds = 0.9f, OpenFrameSeconds = 0.1f;
        const float CardsDelay = 1.2f, CocoonGroundOffset = 0.2f, HeartFallHeight = 1.2f, HeartFallSeconds = 0.5f;
        const int CocoonOrder = 31, DustOrder = 32;

        public sealed class Setup
        {
            public Transform arena, player;
            public MegarachneBossCanopy canopy;
            public DirectionalSprite body, shadow;
            public SpriteRenderer bodyRenderer, shadowRenderer;
            public Vector2 facing, nest, center;
            public Sprite[] webDecay = System.Array.Empty<Sprite>(), cocoonFall = System.Array.Empty<Sprite>(),
                cocoonOpen = System.Array.Empty<Sprite>(), dust = System.Array.Empty<Sprite>(), heart = System.Array.Empty<Sprite>();
        }

        Setup setup;
        SpriteRenderer cocoon;
        LepidodendronDrop heartDrop;

        public bool Dead { get; private set; }
        public bool CocoonLanded { get; private set; }
        public bool CocoonOpened { get; private set; }
        public bool CardsShown { get; private set; }
        public bool HeartTaken { get; private set; }
        public int BiomassDropped { get; private set; }
        public Vector2 CocoonPoint { get; private set; }
        public int CardOptions { get; private set; }

        internal void Configure(Setup newSetup)
        {
            setup = newSetup;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            float angle = Mathf.Atan2(setup.facing.y, setup.facing.x) * Mathf.Rad2Deg;
            if (setup.body != null)
            {
                setup.body.Angle = angle;
                setup.body.Play("Megarachne_Death", DeathMs, false, restart: true);
            }
            if (setup.shadow != null)
            {
                setup.shadow.Angle = angle;
                setup.shadow.Play("Megarachne_Shadow_Death", DeathMs, false, restart: true);
            }
            yield return new WaitForSeconds(8f * DeathMs / 1000f);
            if (setup.body != null) setup.body.Play("Megarachne_Dead", DeathMs, true, restart: true);
            if (setup.shadow != null) setup.shadow.Play("Megarachne_Shadow_Dead", DeathMs, true, restart: true);
            Dead = true;
            Sfx.Play(Sounds.BossMegarachneWebCollapse);

            // La seda del Nido se deshace mientras el cuerpo se queda quieto.
            for (float t = 0f; t < setup.webDecay.Length * WebFrameSeconds && setup.canopy != null; t += Time.deltaTime)
            {
                setup.canopy.ShowWeb(setup.webDecay, t / WebFrameSeconds);
                yield return null;
            }
            if (setup.canopy != null) setup.canopy.ShowWeb(setup.webDecay, setup.webDecay.Length);
            yield return new WaitForSeconds(DeadHoldSeconds);
            yield return DropCocoon();
        }

        IEnumerator DropCocoon()
        {
            if (setup.cocoonFall.Length == 0) yield break;
            CocoonPoint = LandingPoint();
            cocoon = new GameObject("Capullo").AddComponent<SpriteRenderer>();
            cocoon.transform.SetParent(setup.arena, true);
            cocoon.sortingOrder = CocoonOrder;
            Vector2 from = setup.nest;
            Sfx.Play(Sounds.BossMegarachneCocoonFall);
            for (float t = 0f; t < CocoonFallSeconds; t += Time.deltaTime)
            {
                float k = t / CocoonFallSeconds;
                cocoon.transform.position = Vector2.Lerp(from, CocoonPoint, k * k);
                cocoon.sprite = setup.cocoonFall[Mathf.FloorToInt(t / 0.08f) % setup.cocoonFall.Length];
                yield return null;
            }
            cocoon.transform.position = CocoonPoint;
            CocoonLanded = true;
            ScreenShake.Pulse(2f, 0.2f);
            if (setup.dust.Length > 0) SpriteFramePlayer.PlayOnce(setup.dust, 0.08f, CocoonPoint + Vector2.down * 0.3f, DustOrder);
            yield return new WaitForSeconds(0.4f);
            // Se abre con el contenedor de corazón: este sonido sustituye al del coral de siempre.
            Sfx.Play(Sounds.BossMegarachneCocoonOpen);
            for (int i = 0; i < setup.cocoonOpen.Length; i++)
            {
                cocoon.sprite = setup.cocoonOpen[i];
                yield return new WaitForSeconds(OpenFrameSeconds);
            }
            CocoonOpened = true;
            Reward();
            yield return new WaitForSeconds(CardsDelay);
            yield return OfferCards();
        }

        // Punto del suelo para el capullo, lo más lejos del cuerpo entre tres sitios libres del Nido.
        Vector2 LandingPoint()
        {
            Vector2 body = transform.position;
            Vector2[] options =
            {
                setup.center + Vector2.down * 3f, setup.center + new Vector2(-4f, -2f), setup.center + new Vector2(4f, -2f),
            };
            Vector2 best = options[0];
            float far = -1f;
            foreach (var option in options)
            {
                float distance = Vector2.Distance(option, body);
                if (distance > far) { far = distance; best = option; }
            }
            return best;
        }

        void Reward()
        {
            Vector2 at = CocoonPoint + Vector2.down * CocoonGroundOffset;
            var biomass = setup.arena != null ? setup.arena.GetComponentInParent<RoomBiomass>() : null;
            if (biomass == null) biomass = FindAnyObjectByType<RoomBiomass>();
            if (biomass != null)
            {
                biomass.Drop(BiomassReward, at);
                BiomassDropped = BiomassReward;
            }
            if (RunInventory.HasFlag(HeartFlag) || setup.heart.Length == 0) return;
            heartDrop = LepidodendronDrop.Create(setup.arena, "Heart", setup.heart, setup.heart, null, null, at + Vector2.up * 0.4f,
                HeartFallHeight, HeartFallSeconds, 0.1f, 0.13f);
            heartDrop.Player = setup.player;
            heartDrop.Taken += TakeHeart;
        }

        void TakeHeart()
        {
            RunInventory.SetFlag(HeartFlag);
            HeartTaken = true;
            if (setup.player != null && setup.player.TryGetComponent<Health>(out var health))
                health.SetMax(health.Max + 2); // Un corazón entero son dos medios; SetMax cura la diferencia.
        }

        // Las mutaciones equipadas que aún pueden subir de nivel, para que elija una (hasta las tarjetas que caben).
        internal static List<MutationData> UpgradableMutations(PlayerMutations player, int maxCards)
        {
            var result = new List<MutationData>();
            foreach (MutationSlot slot in System.Enum.GetValues(typeof(MutationSlot)))
            {
                var mutation = player.Get(slot);
                if (mutation != null && player.CanLevelUp(mutation)) result.Add(mutation);
            }
            while (result.Count > maxCards) result.RemoveAt(Random.Range(0, result.Count));
            return result;
        }

        IEnumerator OfferCards()
        {
            // Con la pausa, el mapa o la evolución abiertos, espera a volver al juego.
            while (GameManager.Instance != null && GameManager.Instance.State is GameState.Paused or GameState.Map or GameState.Evolution)
                yield return null;
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) yield break;
            var player = setup.player != null ? setup.player.GetComponent<PlayerMutations>() : null;
            var choice = FindFirstObjectByType<MutationChoiceUI>(FindObjectsInactive.Include);
            if (player == null || choice == null || player.Health.IsDead) yield break;
            var options = UpgradableMutations(player, 3);
            CardOptions = options.Count;
            if (options.Count == 0) yield break;
            choice.Show(options, player);
            CardsShown = true;
        }
    }
}
