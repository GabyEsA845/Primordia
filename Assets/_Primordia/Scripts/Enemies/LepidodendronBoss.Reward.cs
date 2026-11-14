using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using Primordia.UI;
using UnityEngine;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        // Provisionales: tiempos de la recompensa y de la sala del tocón (posiciones del README «Recompensa»).
        public const float RewardStartDelay = 0.4f, RewardFallSeconds = 1.8f, FadeSeconds = 0.45f, FadeHoldSeconds = 0.15f;
        const float RewardFallHeight = 7f, RewardSway = 0.6f, RewardPickupRadius = 1.1f, StumpRoomYOffset = 400f;
        const float StumpFloorRadius = 92f / Ppu, PromptRadius = 1.8f;
        static readonly Vector2 HeartRest = new(-22f / Ppu, -26f / Ppu), SporeRest = new(22f / Ppu, -26f / Ppu);
        static readonly Vector2 RoomPlayerStart = new(240f, 244f), RoomExit = new(240f, 252f), RoomPedestal = new(240f, 164f);
        static readonly Color GoldFade = new(0xe0 / 255f, 0xa2 / 255f, 0x34 / 255f);
        static readonly (string sheet, float x, float y)[] StumpShrooms =
        {
            ("Lepido_ShroomA_Idle", 178, 95), ("Lepido_ShroomB_Idle", 209, 75), ("Lepido_ShroomC_Idle", 266, 74),
            ("Lepido_ShroomA_Idle", 294, 88), ("Lepido_ShroomB_Idle", 315, 111), ("Lepido_ShroomC_Idle", 162, 116)
        };

        [SerializeField] Sprite[] heartFrames = Array.Empty<Sprite>();
        [SerializeField] Sprite[] promptFrames = Array.Empty<Sprite>();
        Transform rewardRoot;
        LepidodendronPrompt entrancePrompt;
        SpriteRenderer pedestal, relicBody, stumpLight;
        readonly List<Texture2D> rewardTextures = new();
        readonly List<Sprite> rewardSprites = new();
        bool rewardStarted, rewardBusy;

        internal void ConfigureReward(Sprite[] heart, Sprite[] prompt)
        {
            heartFrames = heart ?? Array.Empty<Sprite>();
            promptFrames = prompt ?? Array.Empty<Sprite>();
        }

        /// <summary>El jugador está dentro de la sala del tocón: la cámara se queda en ella, sin zoom.</summary>
        public bool InStumpRoom { get; private set; }
        Vector2 StumpRoomOrigin => entrance + Vector2.up * StumpRoomYOffset;
        Vector2 StumpRoomCenter => RoomPoint(240f, 135f);
        public bool RewardBusy => rewardBusy;
        internal bool HeartDropped { get; private set; }
        internal bool SporeDropped { get; private set; }
        internal Vector2 StumpRoomPlayerStart => RoomPoint(RoomPlayerStart.x, RoomPlayerStart.y);
        internal Transform StumpRoomRoot => rewardRoot != null ? rewardRoot.Find("StumpRoom") : null;
        internal bool RelicOnPedestal => relicBody != null;

        Vector2 RoomPoint(float px, float py) => StumpRoomOrigin + new Vector2(px / Ppu, -py / Ppu);

        // Al terminar el desplome: la grieta se puede abrir y baja la recompensa que aún no se haya cogido.
        void BeginReward()
        {
            if (rewardStarted || !built) return;
            rewardStarted = true;
            rewardRoot = new GameObject("Reward").transform;
            rewardRoot.SetParent(scenery, true);
            entrancePrompt = LepidodendronPrompt.Create(rewardRoot, StumpCrack.position, promptFrames, "Grieta del tocón",
                "E para entrar", new Color(1f, 0.82f, 0.4f), PromptRadius, () => StartCoroutine(EnterStumpRoom()));
            StartCoroutine(DropRewards());
        }

        IEnumerator DropRewards()
        {
            yield return new WaitForSeconds(RewardStartDelay);
            Vector2 foot = Foot;
            if (!RunInventory.HasFlag(RunInventory.LepidoHeartTaken))
            {
                HeartDropped = true;
                var heart = LepidodendronDrop.Create(rewardRoot, "Heart", heartFrames, heartFrames, art.Find("Fx_HeartGlow").sprites,
                    art.Find("Fx_RewardShadow").sprites, foot + HeartRest, RewardFallHeight, RewardFallSeconds, RewardSway, 0.13f);
                heart.Taken += () => StartCoroutine(TakeHeart());
                heart.Player = player;
            }
            if (!RunInventory.Has(RunInventory.PurpleSpore))
            {
                SporeDropped = true;
                var spore = LepidodendronDrop.Create(rewardRoot, "PurpleSpore", art.Find("Item_PurpleSpore_Fall").sprites,
                    art.Find("Item_PurpleSpore_Idle").sprites, null, art.Find("Fx_RewardShadow").sprites, foot + SporeRest, RewardFallHeight,
                    RewardFallSeconds, -RewardSway, 0.14f);
                spore.Taken += () => StartCoroutine(TakeSpore());
                spore.Player = player;
            }
        }

        IEnumerator TakeHeart()
        {
            RunInventory.SetFlag(RunInventory.LepidoHeartTaken);
            if (player != null && player.TryGetComponent<Health>(out var health))
                health.SetMax(health.Max + 2); // Un corazón entero son dos medios; SetMax cura la diferencia.
            Sfx.Play(Sounds.HeartCoral);
            yield break;
        }

        RunInventory.Entry SporeEntry() => new()
        {
            Id = RunInventory.PurpleSpore, Name = "Espora morada", Effect = "Abre un camino secreto en el piso 4",
            Icon16 = Sheet("Icon_PurpleSpore_16"), Icon32 = Sheet("Icon_PurpleSpore_32")
        };

        RunInventory.Entry RelicEntry() => new()
        {
            Id = RunInventory.RootAncestral, Name = "Raíz ancestral", Effect = "Al llegar a un piso nuevo recuperas toda la vida",
            Icon16 = Sheet("Icon_RootAncestral_16"), Icon32 = Sheet("Icon_RootAncestral_32")
        };

        Sprite Sheet(string name) => art.Find(name)?.sprites is { Length: > 0 } sprites ? sprites[0] : null;

        IEnumerator TakeSpore()
        {
            RunInventory.AddKeyItem(SporeEntry());
            Sfx.Play(Sounds.HeartCoral);
            yield return Present("O B J E T O   C L A V E", "Item_PurpleSpore_Big", "Title_Key_PurpleSpore", "Abre un camino secreto en el piso 4",
                player != null ? (Vector2)player.position + Vector2.up : Foot);
        }

        IEnumerator Present(string label, string itemSheet, string titleSheet, string effect, Vector2 fromWorld)
        {
            var presenter = RewardPresenter.Create();
            presenter.StartCoroutine(presenter.Present(new RewardPresenter.Content
            {
                halo = art.Find("UI_RelicHalo").sprites, item = art.Find(itemSheet).sprites, title = Sheet(titleSheet),
                filletTop = Sheet("UI_RelicFillet_Top"), filletBottom = Sheet("UI_RelicFillet_Bottom"), label = label, effect = effect,
                from = StagePoint(fromWorld)
            }));
            while (presenter != null) yield return null;
        }

        static Vector2 StagePoint(Vector2 world)
        {
            var camera = Camera.main;
            if (camera == null) return new Vector2(240f, 135f);
            Vector3 screen = camera.WorldToScreenPoint(world);
            return new Vector2(screen.x / Mathf.Max(1, Screen.width) * 480f, (1f - screen.y / Mathf.Max(1, Screen.height)) * 270f);
        }

        // ---- Sala del tocón ----

        IEnumerator EnterStumpRoom()
        {
            if (rewardBusy || InStumpRoom) yield break;
            rewardBusy = true;
            yield return Transition(() =>
            {
                BuildStumpRoom();
                InStumpRoom = true;
                Teleport(StumpRoomPlayerStart);
            });
            rewardBusy = false;
        }

        IEnumerator LeaveStumpRoom()
        {
            if (rewardBusy || !InStumpRoom) yield break;
            rewardBusy = true;
            yield return Transition(() =>
            {
                InStumpRoom = false;
                Teleport((Vector2)StumpCrack.position + Vector2.down * 1.5f);
                lookAhead?.ClearFocus();
            });
            rewardBusy = false;
        }

        // Fundido dorado a opaco, acción en negro y vuelta a transparente; el jugador queda quieto mientras tanto.
        IEnumerator Transition(Action midpoint)
        {
            var presenter = RewardPresenter.Create();
            var controller = player != null ? player.GetComponent<PlayerController>() : null;
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            if (player != null && player.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
            yield return presenter.Fade(GoldFade, FadeSeconds, 0f, 1f);
            midpoint();
            yield return new WaitForSeconds(FadeHoldSeconds);
            yield return presenter.Fade(GoldFade, FadeSeconds, 1f, 0f);
            if (controller != null) controller.enabled = wasEnabled;
            if (presenter != null) Destroy(presenter.gameObject);
        }

        void Teleport(Vector2 point)
        {
            if (player == null) return;
            player.position = point;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
            {
                body.position = point;
                body.linearVelocity = Vector2.zero;
            }
        }

        void BuildStumpRoom()
        {
            if (rewardRoot.Find("StumpRoom") != null) return;
            var root = new GameObject("StumpRoom").transform;
            root.SetParent(rewardRoot, true);
            // Telón negro: la vista puede ser más ancha que los 480×270 de la sala.
            var black = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            black.SetPixel(0, 0, Color.black);
            black.Apply();
            var blackSprite = Sprite.Create(black, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            rewardTextures.Add(black);
            rewardSprites.Add(blackSprite);
            var backdrop = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            backdrop.transform.SetParent(root, true);
            backdrop.transform.position = StumpRoomCenter;
            backdrop.transform.localScale = new Vector3(400f, 300f, 1f);
            backdrop.sprite = blackSprite;
            backdrop.sortingOrder = -100;

            Vector2 origin = StumpRoomOrigin;
            Piece("Room", "Lepido_StumpRoom", origin, 0, 0f, 0f, root);
            stumpLight = Piece("Light", "Fx_StumpRoom_Light", origin, 1, 0f, 0f, root);
            Piece("Rays", "Fx_StumpRoom_Rays", origin, 50, 0.15f, 0f, root);
            for (int i = 0; i < StumpShrooms.Length; i++)
                Piece($"Shroom{i}", StumpShrooms[i].sheet, RoomPoint(StumpShrooms[i].x, StumpShrooms[i].y), 2, 0.16f, i, root);
            Piece("Exit", "Lepido_StumpRoom_Exit", RoomPoint(RoomExit.x, RoomExit.y), 2, 0.14f, 0f, root);
            LepidodendronPrompt.Create(root, RoomPoint(RoomExit.x, RoomExit.y - 8f), promptFrames, "Salir", "E para salir",
                new Color(1f, 0.82f, 0.4f), 1.2f, () => StartCoroutine(LeaveStumpRoom()));

            // Suelo redondo: la pared es todo lo de fuera del círculo.
            var wall = new GameObject("Wall").AddComponent<EdgeCollider2D>();
            wall.transform.SetParent(root, true);
            var points = new Vector2[41];
            Vector2 center = RoomPoint(240f, 158f);
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i / 40f * Mathf.PI * 2f;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * StumpFloorRadius;
            }
            wall.points = points;

            bool relicTaken = RunInventory.Has(RunInventory.RootAncestral);
            pedestal = Piece("Pedestal", relicTaken ? "Lepido_Pedestal_Empty" : "Lepido_Pedestal_Lit", RoomPoint(RoomPedestal.x, RoomPedestal.y),
                2, relicTaken ? 0f : 0.16f, 0f, root);
            var cup = new GameObject("PedestalBody").AddComponent<CircleCollider2D>();
            cup.transform.SetParent(root, true);
            cup.transform.position = RoomPoint(RoomPedestal.x, RoomPedestal.y - 6f);
            cup.radius = 0.7f;
            if (relicTaken)
            {
                if (stumpLight != null) { Destroy(stumpLight.gameObject); stumpLight = null; }
                return;
            }
            relicBody = Piece("Relic", "Relic_RootAncestral_Idle", RoomPoint(228f, 103f), 3, 0.12f, 0f, root);
            LepidodendronPrompt.Create(root, RoomPoint(RoomPedestal.x, RoomPedestal.y - 20f), promptFrames, "Raíz ancestral",
                "Reliquia · E para coger", new Color(1f, 0.82f, 0.4f), 2f, () => StartCoroutine(TakeRelic()));
        }

        IEnumerator TakeRelic()
        {
            if (rewardBusy || relicBody == null) yield break;
            rewardBusy = true;
            Vector2 from = relicBody.transform.position;
            Destroy(relicBody.gameObject);
            relicBody = null;
            if (stumpLight != null) { Destroy(stumpLight.gameObject); stumpLight = null; }
            RunInventory.AddRelic(RelicEntry());
            foreach (var prompt in rewardRoot.GetComponentsInChildren<LepidodendronPrompt>())
                if (prompt.Title == "Raíz ancestral") prompt.Disable();
            Sfx.Play(Sounds.HeartCoral);
            yield return Present("R E L I Q U I A", "Relic_RootAncestral_Big", "Title_Relic_RootAncestral", "Al llegar a un piso nuevo recuperas toda la vida", from);
            if (pedestal != null)
            {
                visuals.RemoveAll(v => v.renderer == pedestal);
                var fade = art.Find("Lepido_Pedestal_Fade").sprites;
                foreach (var frame in fade)
                {
                    if (pedestal == null) break;
                    pedestal.sprite = frame;
                    yield return new WaitForSeconds(0.1f);
                }
                if (pedestal != null) pedestal.sprite = art.Find("Lepido_Pedestal_Empty").sprites[0];
            }
            rewardBusy = false;
        }

        void ReleaseRewardAssets()
        {
            foreach (var sprite in rewardSprites) if (sprite != null) Destroy(sprite);
            foreach (var texture in rewardTextures) if (texture != null) Destroy(texture);
            rewardSprites.Clear();
            rewardTextures.Clear();
            InStumpRoom = false;
        }

        internal string TestRewardAction(int action)
        {
            if (!HasCollapsed) return "Primero fuerza el desplome (Alt+F1)";
            if (action == 0)
            {
                if (!rewardStarted) { BeginReward(); return "Recompensa lanzada"; }
                StartCoroutine(DropRewards());
                return "Recompensa vuelta a bajar (lo ya cogido no reaparece)";
            }
            if (action == 1) { Teleport((Vector2)StumpCrack.position + Vector2.down * 1.5f); return "Junto a la grieta del tocón"; }
            if (action == 2) { BeginReward(); BuildStumpRoom(); InStumpRoom = true; Teleport(StumpRoomPlayerStart); return "Dentro de la sala del tocón"; }
            if (action == 3)
            {
                RunInventory.AddKeyItem(SporeEntry());
                RunInventory.AddRelic(RelicEntry());
                return "Reliquia y Espora morada dadas (sin presentación)";
            }
            RunInventory.Reset();
            return "Estado de recompensa reiniciado";
        }
    }
}
