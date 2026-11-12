using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Primordia.Enemies
{
    public partial class LepidodendronBoss
    {
        const float DappleTileSize = 16f, ArenaHalfViewHeight = 270f / (2f * Ppu * ArenaZoom);
        const float LightDrop = 15f, LightDrift = 4.2f, LightHoleY = 40f / Ppu;
        const float TrunkHalfWidth = 5.5f, BackRootsHalfWidth = 8f;

        static Vector3Int[] KnotCrops(int wall) => wall == 0
            ? new[] { new Vector3Int(44, 71, 38), new Vector3Int(39, 191, 38) }
            : wall == 1 ? new[] { new Vector3Int(41, 41, 41), new Vector3Int(47, 125, 41), new Vector3Int(44, 217, 41) }
            : new[] { new Vector3Int(42, 27, 47), new Vector3Int(50, 92, 47), new Vector3Int(41, 159, 47), new Vector3Int(49, 235, 47) };

        // DECOR_LAYOUT del generador aprobado: x hacia la derecha, y hacia abajo, respecto al pie.
        static (string sheet, float x, float y, int phase, bool flip, float step)[] DecorLayout() => new[]
        {
            ("Lepido_Fungus_L", -96f, -40f, 0, false, 0f), ("Lepido_Fungus_M", -70f, -18f, 0, false, 0f),
            ("Lepido_Fungus_S", 84f, -30f, 0, true, 0f), ("Lepido_Fungus_M", 104f, -10f, 0, true, 0f),
            ("Lepido_Fungus_S", 30f, -52f, 0, false, 0f),
            ("Lepido_ShroomA_Idle", -128f, 14f, 0, false, ShroomStep), ("Lepido_ShroomB_Idle", -40f, 30f, 2, false, ShroomStep),
            ("Lepido_ShroomC_Idle", 120f, 22f, 4, true, ShroomStep), ("Lepido_ShroomB_Idle", 60f, 36f, 1, true, ShroomStep),
            ("Lepido_ShroomA_Idle", 158f, 4f, 3, true, ShroomStep),
            ("Lepido_FlowerletsA", -150f, 30f, 0, false, FlowerStep), ("Lepido_FlowerletsB", -104f, 44f, 1, false, FlowerStep),
            ("Lepido_FlowerletsC", -16f, 48f, 2, false, FlowerStep), ("Lepido_FlowerletsA", 30f, 52f, 3, true, FlowerStep),
            ("Lepido_FlowerletsB", 88f, 44f, 0, true, FlowerStep), ("Lepido_FlowerletsC", 140f, 34f, 1, false, FlowerStep),
            ("Lepido_FlowerletsB", -182f, 10f, 2, true, FlowerStep), ("Lepido_FlowerletsA", 186f, 16f, 1, false, FlowerStep),
            ("Lepido_DarkPuddle", -64f, 46f, 0, false, PuddleStep),
            ("Remains_Pulmonoscorpius", 118f, 58f, 0, false, 0f), ("Remains_AmphibianSkull", -118f, 52f, 0, false, 0f),
            ("Remains_PaleoWings", 40f, 74f, 0, false, 0f), ("Remains_Trigonotarbid", -96f, 72f, 0, true, 0f)
        };

        sealed class DappleQuad
        {
            public SpriteRenderer source;
            public Mesh mesh;
            public Vector2 uvOffset, uvSize;
            public Sprite lastSprite;
        }

        public static IEnumerable<string> RequiredSheets
        {
            get
            {
                foreach (string name in new[] { "Lepido_Trunk", "Lepido_BackRoots", "Lepido_Mound", "Lepido_MoundShadow", "Lepido_Veins_Rest",
                    "Canopy_Arena_Sway", "Canopy_Arena_Shake", "Canopy_StripThin_Sway", "Canopy_StripMid_Sway", "Fx_Dapple",
                    "Fx_LightShaft", "Fx_LightPool", "Fx_FallingLeaf", "Fx_FallingSpore", "Critter_Roach_Idle", "Critter_Roach_Run",
                    "Critter_Millipede_Crawl", "Critter_Dragonfly_Perch", "Critter_Dragonfly_Fly", "Critter_Springtail_Hop",
                    "Lepido_LianaA_Sway", "Lepido_LianaB_Sway" })
                    yield return name;
                foreach (var decor in DecorLayout())
                    yield return decor.sheet;
                for (int wall = 1; wall <= 3; wall++)
                {
                    for (int state = 0; state < 4; state++)
                        foreach (string prefix in new[] { "D", "Charred_D", "Hurt_D" })
                            yield return $"Lepido_Wall{wall}_{prefix}{state}";
                    foreach (string suffix in new[] { "Retract", "Scar", "Crumble", "AshScar" })
                        yield return $"Lepido_Wall{wall}_{suffix}";
                    for (int knot = 0; knot <= wall; knot++)
                        foreach (string suffix in new[] { "Idle", "Hit", "Wound", "WoundCharred" })
                            yield return $"Lepido_Wall{wall}_Knot{knot}_{suffix}";
                }
                yield return "Fx_KnotBurst";
                yield return "Fx_RootSmolder";
            }
        }

        void BuildScenery()
        {
            if (art == null)
                throw new InvalidOperationException("Falta LepidodendronArt: ejecuta el menú de creación.");
            foreach (string name in RequiredSheets)
                if (art.Find(name) == null || art.Find(name).sprites.Length == 0)
                    throw new InvalidOperationException($"Falta la hoja aprobada {name}.");

            scenery = new GameObject("Scenery").transform;
            scenery.SetParent(transform, false);
            Piece("BackRoots", "Lepido_BackRoots", Foot, RootsOrder);
            Piece("MoundShadow", "Lepido_MoundShadow", Foot, MoundShadowOrder);
            Piece("Mound", "Lepido_Mound", Foot, MoundOrder);
            var trunk = Piece("Trunk", "Lepido_Trunk", Foot, TrunkOrder);
            var solid = trunk.gameObject.AddComponent<PolygonCollider2D>();
            // Cinco lóbulos del pie: 11 u de ancho; la colisión solo ocupa la base, no el tronco elevado.
            solid.points = new[] { new Vector2(-TrunkHalfWidth, 0f), new Vector2(-4f, -1.4f), new Vector2(-2f, -1.8f),
                new Vector2(0f, -2f), new Vector2(2f, -1.8f), new Vector2(4f, -1.4f), new Vector2(TrunkHalfWidth, 0f),
                new Vector2(4f, 1.5f), new Vector2(-4f, 1.5f) };
            var back = scenery.Find("BackRoots").gameObject.AddComponent<PolygonCollider2D>();
            // Se une al pie y llega a la pared norte: no existe paso por detrás.
            back.points = new[] { new Vector2(-TrunkHalfWidth, 0f), new Vector2(TrunkHalfWidth, 0f), new Vector2(BackRootsHalfWidth, FootFromNorth + 1f),
                new Vector2(-BackRootsHalfWidth, FootFromNorth + 1f) };
            Piece("Veins", "Lepido_Veins_Rest", Foot, VeinsOrder);
            BuildWalls();
            BuildDapple();
            BuildDecor();
            BuildCanopy();
        }

        SpriteRenderer Piece(string name, string sheet, Vector2 position, int order, float step = 0f, float phase = 0f, Transform parent = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent != null ? parent : scenery, true);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            var frames = art.Find(sheet).sprites;
            renderer.sprite = frames[Mathf.FloorToInt(phase) % frames.Length];
            if (step > 0f)
                visuals.Add(new Visual { renderer = renderer, sheet = sheet, step = step, phase = phase });
            return renderer;
        }

        void BuildWalls()
        {
            for (int i = 0; i < 3; i++)
            {
                var position = entrance + new Vector2(WallX(i), -CorridorHeight / 2f);
                var wall = Piece($"Wall{i + 1}", $"Lepido_Wall{i + 1}_D0", position, WallOrder);
                var collider = wall.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(WallThickness(i), CorridorHeight);
                collider.offset = Vector2.up * CorridorHeight / 2f;
                int pivotX = new[] { 62, 66, 70 }[i];
                int pivotY = new[] { 325, 332, 341 }[i];
                var crops = KnotCrops(i);
                for (int j = 0; j < crops.Length; j++)
                {
                    var crop = crops[j];
                    var offset = new Vector2(crop.x + crop.z / 2f - pivotX, pivotY - crop.y - crop.z / 2f) / Ppu;
                    Piece($"Knot{j + 1}", $"Lepido_Wall{i + 1}_Knot{j}_Idle", position + offset,
                        WallOrder + 1, 0f, j, wall.transform);
                }
                var component = wall.gameObject.AddComponent<LepidodendronWall>();
                component.Configure(this, i, wall, collider, art, encounterConfig != null ? encounterConfig.Reactions : null, crops);
                walls.Add(component);
                BuildWallSupplies(i);
            }
        }

        internal float DappleOpacity(float worldX) => Mathf.Clamp01((worldX - entrance.x - Wall1X) / (CorridorLength - Wall1X));

        void BuildDapple()
        {
            dappleMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "LepidodendronDapple",
                mainTexture = art.Find("Fx_Dapple").sprites[0].texture,
                hideFlags = HideFlags.HideAndDontSave,
            };
            FillDapple(Wall1X, CorridorLength, CorridorHeight);
            FillDapple(CorridorLength, CorridorLength + ArenaWidth, ArenaHeight);
        }

        void FillDapple(float from, float to, float height)
        {
            for (float x = from; x < to;)
            {
                float offsetX = Mathf.Repeat(x - Wall1X, DappleTileSize);
                float width = Mathf.Min(DappleTileSize - offsetX, to - x);
                for (float y = -height / 2f; y < height / 2f;)
                {
                    float offsetY = Mathf.Repeat(y + ArenaHeight / 2f, DappleTileSize);
                    float h = Mathf.Min(DappleTileSize - offsetY, height / 2f - y);
                    var r = Piece("Dapple", "Fx_Dapple", entrance + new Vector2(x + width / 2f, y + h / 2f), DappleOrder, DappleStep);
                    // Repite el arte aprobado sin costuras; el alfa por vértice interpola la x exacta.
                    // El SpriteRenderer oculto conserva la animación, sin duplicar el dibujo.
                    r.enabled = false;
                    var mesh = new Mesh { name = "LepidodendronDappleQuad", hideFlags = HideFlags.HideAndDontSave };
                    mesh.vertices = new[] { new Vector3(-width / 2f, -h / 2f), new Vector3(width / 2f, -h / 2f),
                        new Vector3(-width / 2f, h / 2f), new Vector3(width / 2f, h / 2f) };
                    mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                    var left = new Color(1f, 1f, 1f, DappleOpacity(entrance.x + x));
                    var right = new Color(1f, 1f, 1f, DappleOpacity(entrance.x + x + width));
                    mesh.colors = new[] { left, right, left, right };
                    var drawing = new GameObject("Gradient");
                    drawing.transform.SetParent(r.transform, false);
                    drawing.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = drawing.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = dappleMaterial;
                    renderer.sortingOrder = DappleOrder;
                    var quad = new DappleQuad { source = r, mesh = mesh, uvOffset = new Vector2(offsetX, offsetY) / DappleTileSize,
                        uvSize = new Vector2(width, h) / DappleTileSize };
                    dappleQuads.Add(quad);
                    UpdateDappleUV(quad);
                    y += h;
                }
                x += width;
            }
        }

        void BuildDecor()
        {
            foreach (var decor in DecorLayout())
            {
                bool shelf = decor.sheet.StartsWith("Lepido_Fungus");
                bool ground = decor.sheet.StartsWith("Remains") || decor.sheet == "Lepido_DarkPuddle";
                var r = Piece(decor.sheet, decor.sheet, Foot + new Vector2(decor.x, -decor.y) / Ppu,
                    ground ? MoundOrder + 1 : shelf ? DecorOrder : VeinsOrder + 1, decor.step, decor.phase);
                r.flipX = decor.flip;
            }
            foreach (var liana in new[] { ("A", -40f, 0f), ("B", 28f, 3f) })
                Piece("Liana" + liana.Item1, $"Lepido_Liana{liana.Item1}_Sway", Foot + new Vector2(liana.Item2 / Ppu, 8.625f), DecorOrder, CanopySwayStep, liana.Item3);
            AddCritter("Roach", "Critter_Roach_Idle", new Vector2(-160, -78), RoachIdleStep);
            AddCritter("Millipede", "Critter_Millipede_Crawl", new Vector2(84, -14), MillipedeStep);
            AddCritter("Dragonfly", "Critter_Dragonfly_Perch", new Vector2(-122, -46), 0f);
            var springtails = new[] { new Vector2(-14, -76), new Vector2(-6, -82), new Vector2(2, -74) };
            for (int i = 0; i < springtails.Length; i++)
                AddCritter("Springtail", "Critter_Springtail_Hop", springtails[i], SpringtailStep, i);
        }

        void AddCritter(string kind, string sheet, Vector2 pixels, float step, int phase = 0)
        {
            var r = Piece(kind, sheet, Foot + pixels / Ppu, DecorOrder, step, phase);
            critters.Add(new Critter { kind = kind, renderer = r, home = r.transform.position });
        }

        void UpdateCritters()
        {
            foreach (var c in critters)
            {
                bool near = player != null && Vector2.Distance(player.position, c.renderer.transform.position) < CritterNear;
                if (near && (c.kind == "Roach" || c.kind == "Dragonfly"))
                    c.fleeUntil = Time.time + CritterFleeSeconds;
                bool fleeing = Time.time < c.fleeUntil;
                if (c.kind == "Roach" || c.kind == "Dragonfly")
                {
                    string sheet = c.kind == "Roach" ? (fleeing ? "Critter_Roach_Run" : "Critter_Roach_Idle")
                        : fleeing ? "Critter_Dragonfly_Fly" : "Critter_Dragonfly_Perch";
                    var frames = art.Find(sheet).sprites;
                    c.renderer.sprite = frames[Mathf.FloorToInt(Time.time / (fleeing ? c.kind == "Roach" ? RoachRunStep : DragonflyStep : RoachIdleStep)) % frames.Length];
                    if (fleeing)
                    {
                        Vector2 direction = player != null ? ((Vector2)c.renderer.transform.position - (Vector2)player.position).normalized : Vector2.right;
                        c.renderer.flipX = direction.x < 0f;
                        var p = (Vector2)c.renderer.transform.position + direction * (c.kind == "Roach" ? RoachSpeed : DragonflySpeed) * Time.deltaTime;
                        p.x = Mathf.Clamp(p.x, ArenaCenter.x - ArenaWidth / 2f + 1f, ArenaCenter.x + ArenaWidth / 2f - 1f);
                        p.y = Mathf.Clamp(p.y, ArenaCenter.y - ArenaHeight / 2f + 1f, Foot.y - 1f);
                        c.renderer.transform.position = p;
                    }
                }
                else if (c.kind == "Millipede")
                    c.renderer.transform.position = c.home + Vector2.right * Mathf.Sin(Time.time * MillipedeSpeed);
            }
        }

        void BuildCanopy()
        {
            canopyRoot = new GameObject("Canopy").transform;
            canopyRoot.SetParent(scenery, true);
            canopyRoot.position = new Vector2(ArenaCenter.x, ArenaCenter.y + ArenaHalfViewHeight + CanopyAboveScreen);
            arenaCanopy = Piece("ArenaCanopy", "Canopy_Arena_Sway", canopyRoot.position, CanopyOrder, parent: canopyRoot);
            BuildStrip(Wall2X, Wall3X, "Canopy_StripThin_Sway");
            BuildStrip(Wall3X, CorridorLength - 2f, "Canopy_StripMid_Sway");
            for (int i = 0; i < 2; i++)
            {
                Piece($"LightShaft{i + 1}", "Fx_LightShaft", ArenaCenter, LightOrder, LightStep, i * 3);
                var hole = new Vector2(ArenaCenter.x + ((i == 0 ? 176f : 512f) - 352f) / Ppu, canopyRoot.position.y - LightHoleY);
                Piece($"LightPool{i + 1}", "Fx_LightPool", hole + new Vector2(LightDrift, -LightDrop), MoundOrder + 1, LightStep, i * 3);
            }
            UpdateLightPositions();
        }

        void BuildStrip(float start, float end, string sheet)
        {
            for (float x = start; x < end; x += 16f)
            {
                float width = Mathf.Min(16f, end - x);
                var r = Piece(sheet, sheet, new Vector2(entrance.x + x + width / 2f, canopyRoot.position.y), CanopyOrder,
                    CanopySwayStep, parent: canopyRoot);
                r.drawMode = SpriteDrawMode.Tiled;
                r.size = new Vector2(width, art.Find(sheet).sprites[0].rect.height / Ppu);
            }
        }

        void UpdateLightPositions()
        {
            for (int i = 0; i < 2; i++)
            {
                float x = ArenaCenter.x + ((i == 0 ? 176f : 512f) - 352f) / Ppu;
                var hole = new Vector2(x, canopyRoot.position.y - LightHoleY);
                var shaft = scenery.Find($"LightShaft{i + 1}");
                shaft.position = hole;
                // El charco permanece en el suelo aunque el borde de pantalla se mueva en vertical.
                float poolY = scenery.Find($"LightPool{i + 1}").position.y;
                shaft.localScale = new Vector3(1f, (hole.y - poolY) / LightDrop, 1f);
            }
        }

        void Animate()
        {
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                var v = visuals[i];
                if (v.renderer == null)
                {
                    visuals.RemoveAt(i);
                    continue;
                }
                var frames = art.Find(v.sheet).sprites;
                v.renderer.sprite = frames[(Mathf.FloorToInt(Time.time / v.step + v.phase)) % frames.Length];
            }
            var canopyFrames = art.Find(IsCanopyShaking ? "Canopy_Arena_Shake" : "Canopy_Arena_Sway").sprites;
            arenaCanopy.sprite = canopyFrames[Mathf.FloorToInt(Time.time / (IsCanopyShaking ? CanopyShakeStep : CanopySwayStep)) % canopyFrames.Length];
            foreach (var quad in dappleQuads)
                if (quad.source.sprite != quad.lastSprite)
                    UpdateDappleUV(quad);
        }

        static void UpdateDappleUV(DappleQuad quad)
        {
            var sprite = quad.source.sprite;
            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            var min = new Vector2(uv.x, uv.y);
            var size = new Vector2(uv.z - uv.x, uv.w - uv.y);
            var start = min + Vector2.Scale(size, quad.uvOffset);
            var end = start + Vector2.Scale(size, quad.uvSize);
            quad.mesh.uv = new[] { start, new Vector2(end.x, start.y), new Vector2(start.x, end.y), end };
            quad.lastSprite = sprite;
        }

        void DropLeaf(bool spore)
        {
            string sheet = spore ? "Fx_FallingSpore" : "Fx_FallingLeaf";
            var p = new Vector2(ArenaCenter.x + Random.Range(-ArenaWidth / 2f, ArenaWidth / 2f), canopyRoot.position.y - Random.Range(0f, LeafSpawnHeight));
            var r = Piece(spore ? "FallingSpore" : "FallingLeaf", sheet, p, LeavesOrder, spore ? SporeStep : LeafStep);
            falling.Add(new Falling { renderer = r, speed = Random.Range(LeafMinSpeed, LeafMaxSpeed), until = Time.time + LeafLife, phase = Random.value * 6f });
        }

    }
}
