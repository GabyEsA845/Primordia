using Primordia.Audio;
using System;
using System.Collections;
using System.Linq;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        public const float CollapseSeconds = 6.6f; // Provisional: duración total de puesta en escena.
        const float VeinsSeconds = 0.7f, CrackSeconds = 0.9f, ReturnSeconds = 0.9f;
        const int FallImpactFrame = 12, FollowFallFrame = 7;
        const float FallenWest = -40.2f, FallenEast = -6.2f, FallenY = -4.5f;
        const float FallenWestHalfHeight = 2.9f, FallenEastHalfHeight = 4.8f;
        static float[] FallSteps => new[] { .22f, .20f, .18f, .17f, .16f, .15f, .14f, .13f, .12f, .11f, .10f, .09f, .10f, .13f, .18f };
        public bool Collapsing { get; private set; }
        public bool HasCollapsed { get; private set; }
        public event Action Collapsed;
        internal int FallFrame { get; private set; } = -1;
        internal Transform StumpCrack => scenery != null ? scenery.Find("StumpCrack") : null;
        SpriteRenderer fallen, deathShadow;
        float collapseAt;

        void StartCollapse()
        {
            if (!built || Collapsing || HasCollapsed) return;
            if (IntroPlaying)
            {
                if (presentation != null) StopCoroutine(presentation);
                IntroPlaying = false;
                ReleaseCinematic();
            }
            Collapsing = true;
            collapseAt = Time.time;
            // El desplome (6,6 s) va en tres sonidos: grietas, caída y columna de luz. Se apagan los bucles del jefe.
            Sfx.Stop(Sounds.BossLepidodendronIntro);
            Sfx.Play(Sounds.BossLepidodendronDeathCrack);
            Sfx.StopLoop(Sounds.BossLepidodendronCreakLoop);
            Sfx.StopLoop(Sounds.BossLepidodendronRootWallBurnLoop);
            CancelAttacks();
            wallWaves.Clear();
            AcquireCinematic();
            foreach (var sac in wallRoom.GetComponentsInChildren<MethaneSac>())
            { sac.gameObject.SetActive(false); Destroy(sac.gameObject); }
            foreach (var fire in GroundFire.All.ToArray())
                if (fire != null && InEncounter(fire.transform.position))
                { fire.gameObject.SetActive(false); Destroy(fire.gameObject); }
            // Los helechos que arden pueden volver a generar fuego durante la escena.
            foreach (var fern in wallRoom.GetComponentsInChildren<Fern>())
            { fern.gameObject.SetActive(false); Destroy(fern.gameObject); }
            StartCoroutine(CollapseRoutine());
        }

        bool InEncounter(Vector2 point) => point.x >= entrance.x && point.x <= entrance.x + CorridorLength + ArenaWidth
            && Mathf.Abs(point.y - entrance.y) <= ArenaHeight / 2f;

        IEnumerator CollapseRoutine()
        {
            lookAhead?.SetFocus(ArenaCenter, 1f);
            var trunk = scenery.Find("Trunk").GetComponent<SpriteRenderer>();
            var veins = scenery.Find("Veins").GetComponent<SpriteRenderer>();
            for (int i = 0; i < 10; i++) { SetFrame(veins, "Lepido_Death_Veins", i); yield return new WaitForSeconds(VeinsSeconds / 10f); }
            veins.enabled = false;
            deathShadow = Piece("FallShadow", "Fx_FallShadow", Foot + new Vector2(-54f, -10.5f), MoundShadowOrder);
            for (int i = 0; i < 5; i++)
            {
                SetFrame(trunk, "Lepido_Death_Crack", i);
                SetFrame(arenaCanopy, "Canopy_Arena_Tear", i);
                SetFrame(deathShadow, "Fx_FallShadow", i);
                yield return new WaitForSeconds(CrackSeconds / 5f);
            }
            fallen = Piece("FallingTrunk", "Lepido_Death_Fall", Foot, TrunkOrder + 1);
            Sfx.Play(Sounds.BossLepidodendronDeathFall);
            StartCoroutine(CollapseEffect("TrunkSplinters", "Fx_TrunkSplinters", Foot + new Vector2(0, -4.5f), 0.06f));
            Vector2 impactFocus = Foot + new Vector2(-50f + (Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect : 20f), 0f);
            for (int i = 0; i < FallSteps.Length; i++)
            {
                FallFrame = i;
                SetFrame(fallen, "Lepido_Death_Fall", i);
                SetFrame(trunk, "Lepido_Stump_Break", i);
                SetFrame(arenaCanopy, "Canopy_Arena_Tear", Mathf.Min(16, 5 + i));
                if (deathShadow != null) SetFrame(deathShadow, "Fx_FallShadow", Mathf.Min(16, 5 + i));
                if (i == FollowFallFrame) StartCoroutine(CollapseEffect("CrownCrash", "Fx_CrownCrash", Foot + new Vector2(-43.2f, -5.5f), 0.1f));
                if (i == FallImpactFrame)
                {
                    Destroy(deathShadow.gameObject);
                    StartCoroutine(CollapseEffect("TrunkDust", "Fx_TrunkDust", Foot + new Vector2(-56f, -12f), 0.1f));
                    Piece("WallRubble", "Lepido_WallRubble", Foot + new Vector2(-43.2f, -7.5f), MoundOrder);
                }
                for (float t = 0; t < FallSteps[i]; t += Time.deltaTime)
                {
                    if (i >= FollowFallFrame)
                    {
                        float progress = Mathf.Clamp01((i - FollowFallFrame + t / FallSteps[i]) / (FallImpactFrame - FollowFallFrame));
                        Vector2 focus = Vector2.Lerp(ArenaCenter, impactFocus, progress);
                        if (i >= FallImpactFrame) focus.y += (i % 2 == 0 ? 5f : -3f) / Ppu;
                        lookAhead?.SetFocus(focus, 1f);
                    }
                    yield return null;
                }
            }
            BuildCollapsedScenery();
            float openAt = CollapseSeconds - ReturnSeconds;
            while (Time.time - collapseAt < openAt) yield return null;
            Piece("GoldColumn", "Fx_GoldColumn", Foot + Vector2.up * (4f / Ppu), LightOrder, 0.12f);
            Sfx.Play(Sounds.BossLepidodendronLightColumn);
            for (float t = 0; t < ReturnSeconds; t += Time.deltaTime)
            {
                SetFrame(trunk, "Lepido_Stump_Open", Mathf.Min(7, Mathf.FloorToInt(t / 0.11f)));
                lookAhead?.SetFocus(impactFocus, 1f - Mathf.SmoothStep(0, 1, t / ReturnSeconds));
                yield return null;
            }
            visuals.Add(new Visual { renderer = trunk, sheet = "Lepido_Stump_Idle", step = 0.16f });
            Collapsing = false;
            HasCollapsed = true;
            ReleaseCinematic();
            foreach (var cone in cones) cone.gameObject.SetActive(false);
            ActiveBoss.Clear(this);
            // No se emite Died: Enemy destruiría el escenario y MutationDrop daría la recompensa estándar.
            wallRoom.GetComponent<Room>()?.CompleteScriptedBoss(GetComponent<Enemy>());
            Collapsed?.Invoke();
            BeginReward();
        }

        void SetFrame(SpriteRenderer renderer, string sheet, int frame) => renderer.sprite = art.Find(sheet).sprites[frame];
        IEnumerator CollapseEffect(string name, string sheet, Vector2 position, float step)
        {
            var renderer = Piece(name, sheet, position, DecorOrder);
            foreach (var sprite in art.Find(sheet).sprites)
            { renderer.sprite = sprite; yield return new WaitForSeconds(step); }
            Destroy(renderer.gameObject);
        }

        void BuildCollapsedScenery()
        {
            fallen.name = "FallenTrunk";
            SetFrame(fallen, "Lepido_FallenTrunk", 0);
            var solid = fallen.gameObject.AddComponent<PolygonCollider2D>();
            solid.points = new[] { new Vector2(FallenWest, FallenY - FallenWestHalfHeight), new Vector2(FallenEast, FallenY - FallenEastHalfHeight),
                new Vector2(FallenEast, FallenY + FallenEastHalfHeight), new Vector2(FallenWest, FallenY + FallenWestHalfHeight) };
            // Conserva la base maciza; el blanco de combate desaparece.
            var target = scenery.Find("Trunk/TrunkTarget");
            if (target != null) target.gameObject.SetActive(false);
            var anchor = new GameObject("StumpCrack").transform;
            anchor.SetParent(scenery, true);
            anchor.position = Foot + Vector2.down * 2f;
            foreach (var visual in visuals)
                if (visual.sheet.StartsWith("Lepido_Shroom"))
                {
                    visual.sheet = visual.sheet.Replace("_Idle", "_Off");
                    visual.step = 1f;
                }
            // La iluminación ambiental deja ver el hueco de la copa.
            foreach (var quad in dappleQuads)
            {
                var colors = quad.mesh.colors;
                for (int i = 0; i < colors.Length; i++) colors[i].a = 0f;
                quad.mesh.colors = colors;
            }
            ReplaceNorthWall();
        }

        void ReplaceNorthWall()
        {
            // Tilemaps del piso: recorta solo el segmento norte sustituido, nunca la pared sur ni otras salas.
            var level = wallRoom.parent;
            if (level == null) return;
            foreach (var map in level.GetComponentsInChildren<Tilemap>())
            {
                if (map.name != "Walls") continue;
                for (float x = Foot.x + FallenWest; x <= Foot.x; x += 1f)
                {
                    float north = x < entrance.x + CorridorLength ? entrance.y + CorridorHeight / 2f : entrance.y + ArenaHeight / 2f;
                    for (float y = north; y <= north + 4f; y += 1f)
                        map.SetTile(map.WorldToCell(new Vector3(x, y)), null);
                }
                var collider = map.GetComponent<TilemapCollider2D>();
                if (collider != null) collider.ProcessTilemapChanges();
            }
        }

        internal string TestEndingAction(int action)
        {
            if (action == 1) { ReplayPresentation(); return "Presentación del Lepidodendron"; }
            if (Collapsing || HasCollapsed) return "El Lepidodendron ya se desploma o está tendido";
            TestFinalPhase();
            if (action == 2) return "Vida al 25 %: fase final";
            if (action == 3)
            {
                for (int i = 0; i < cones.Count - 1; i++) cones[i].Health.Kill();
                var last = cones[cones.Count - 1];
                int amount = Mathf.Max(0, last.Health.Current - MaxHealth / 100);
                last.DamageThroughTrunk(amount);
                return "Vida al 1 %";
            }
            foreach (var cone in cones) cone.Health.Kill();
            return "Desplome forzado";
        }
    }
}
