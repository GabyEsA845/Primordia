using System.Collections.Generic;
using System.Linq;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Decorado de la sala de paso del piso 3, «del bosque muerto al desierto» (docs/multimedia-temp/floor3-exit/README.md): cada
    /// franja (tierra quemada, arena intermedia, arena del Pérmico) con su juego de piezas, el rastro de un sinápsido hasta el
    /// sumidero y las ráfagas de arena. Regla de la usuaria: ninguna pieza se solapa con otra (cada una reserva su rectángulo
    /// entero, con holgura), ni con el sumidero, las puertas o las paredes. Solo decorado, salvo los peñascos y troncos de pie,
    /// que bloquean con una caja en la base.
    /// </summary>
    public static partial class FloorBuilder
    {
        const int ExitBurnt = 0;
        const int ExitMid = 1;
        const int ExitPermian = 2;
        const int ExitDecalOrder = -7;
        const int ExitLowOrder = -6;
        const float ExitPitMargin = 2f;
        const float ExitDoorMargin = 2.5f;
        const float ExitPad = 0.12f;
        const float ExitRowStride = 2f;

        sealed class ExitDecorContext
        {
            public ExitDecorArt art;
            public System.Random random;
            public PlacedRoom placed;
            public RoomTemplate template;
            public Transform holder;
            public int[,] bands;
            public readonly List<Rect> used = new();
            public readonly List<Rect> blockers = new();
            public readonly List<ExitDecorPlant> plants = new();
            public Rect pit;
            public Vector2 pitCenter;
            public Vector2 pitRadii;
            public float scale;
            public List<Vector2Int> open;
            public readonly List<Rect> rocks = new();
        }

        /// <summary>Piezas y franja de cada regla; los números son para una sala de unas 550 casillas de suelo.</summary>
        sealed class ExitRule
        {
            public int band;
            public string[] ids;
            public int min, max;
            public bool ground;
            public bool shelter;
            public bool plant;
            public bool obstacle;
        }

        static ExitRule ExitPiece(int band, string[] ids, int min, int max, bool ground = false, bool shelter = false, bool plant = false,
            bool obstacle = false) => new() { band = band, ids = ids, min = min, max = max, ground = ground, shelter = shelter, plant = plant, obstacle = obstacle };

        static void BuildExitDecor(Transform roomObject, PlacedRoom placed, RoomTheme theme, FloorConfig config, FloorMap map,
            List<(Vector2 position, float radius)> taken)
        {
            var art = config.ExitDecor;
            if (art == null || config.TransitionPit == null)
                return;

            var template = placed.Template;
            var origin = placed.Origin;
            var context = new ExitDecorContext
            {
                art = art,
                placed = placed,
                template = template,
                random = new System.Random(origin.x * 7919 ^ origin.y * 104729 ^ 0xdec0),
                bands = new int[template.Width, template.Height],
                pitCenter = PitCenter(placed, config),
                pitRadii = config.TransitionPit.EdgeRadii,
            };
            var margin = context.pitRadii + Vector2.one * ExitPitMargin;
            context.pit = new Rect(context.pitCenter - margin, margin * 2f);

            int floorCells = 0;
            for (int y = 0; y < template.Height; y++)
            {
                for (int x = 0; x < template.Width; x++)
                {
                    int themeIndex = map.GetTheme(origin + new Vector2Int(x, y));
                    context.bands[x, y] = themeIndex == NextTheme ? ExitPermian : themeIndex == MidTheme ? ExitMid : ExitBurnt;
                    if (IsFloor(template, x, y))
                        floorCells++;
                }
            }
            context.scale = Mathf.Clamp(floorCells / 550f, 0.7f, 1.6f);

            // Lo ya ocupado de la sala (odres, helechos…) y la zona de cada puerta no se pisan.
            foreach (var (position, radius) in taken)
                context.used.Add(new Rect(position - Vector2.one * radius, Vector2.one * radius * 2f));
            foreach (var cell in placed.DoorCells.Values.SelectMany(cells => cells))
                context.used.Add(new Rect(cell.x - ExitDoorMargin, cell.y - ExitDoorMargin, ExitDoorMargin * 2f + 1f, ExitDoorMargin * 2f + 1f));
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - origin);
            context.open = EncounterPlanner.SpawnCells(template, doorCells);

            // La franja de entrada es la última del piso (tierra quemada): sus troncos y tocones, no los del tema base.
            if (config.HasBands && config.BandThemes.Length > 0 && config.BandThemes[config.BandThemes.Length - 1] != null)
                theme = config.BandThemes[config.BandThemes.Length - 1];

            context.holder = new GameObject("ExitDecor").transform;
            context.holder.SetParent(roomObject, false);

            // 1) Peñascos pérmicos (los grandes primero, para que lo demás los esquive), 2) rastro desde uno de ellos hasta el sumidero.
            // El primer peñasco se elige por el pasillo despejado que deja hasta el sumidero: de él sale el rastro.
            var rockRule = ExitPiece(ExitPermian, art.Variants("Obstacle_SandRock").ToArray(), 2, 3, obstacle: true);
            var trailRock = ExitTrailRock(context, rockRule);
            if (trailRock.HasValue)
                BuildExitTrail(context, new List<Rect> { trailRock.Value });
            ExitPieces(context, rockRule);

            // 3) Resto de obstáculos y piezas de cada franja, de mayor a menor.
            ExitPieces(context, ExitPiece(ExitMid, new[] { "Obstacle_BuriedTrunk" }, 2, 3, obstacle: true));
            BuildExitBurnt(context, theme);
            foreach (var rule in ExitRules(art))
                ExitPieces(context, rule);

            BuildExitWind(context);
        }

        static IEnumerable<ExitRule> ExitRules(ExitDecorArt art) => new[]
        {
            ExitPiece(ExitMid, art.Variants("Deco_BuriedLog").ToArray(), 3, 4),
            ExitPiece(ExitMid, art.Variants("Deco_BuriedStump").ToArray(), 2, 3),
            ExitPiece(ExitPermian, art.Variants("Deco_BuriedStump").ToArray(), 1, 2, shelter: true),
            ExitPiece(ExitPermian, new[] { "Deco_BuriedLog_2" }, 1, 1, shelter: true),
            ExitPiece(ExitPermian, new[] { "Plant_Walchia_0", "Plant_Walchia_1" }, 2, 3, shelter: true, plant: true),
            ExitPiece(ExitPermian, new[] { "Plant_DryTuft_0", "Plant_DryTuft_1", "Plant_DryTuft_2" }, 4, 6, shelter: true, plant: true),
            ExitPiece(ExitPermian, new[] { "Plant_DryCalamites" }, 2, 3, shelter: true, plant: true),
            ExitPiece(ExitMid, art.Variants("Deco_DeadFern").ToArray(), 3, 5),
            ExitPiece(ExitPermian, art.Variants("Deco_Slab").ToArray(), 3, 4, shelter: true),
            ExitPiece(ExitPermian, art.Variants("Deco_Stone").ToArray(), 4, 6, shelter: true),
            ExitPiece(ExitPermian, art.Variants("Deco_Gravel").ToArray(), 4, 6, shelter: true),
            ExitPiece(ExitMid, art.Variants("Deco_Charcoal").ToArray(), 4, 6),
            ExitPiece(ExitMid, art.Variants("Deco_MudCracks").ToArray(), 2, 3, ground: true),
            ExitPiece(ExitPermian, art.Variants("Deco_MudCracks").ToArray(), 2, 3, ground: true),
            ExitPiece(ExitPermian, art.Variants("Deco_SaltCrust").ToArray(), 2, 3, ground: true),
        };

        static Vector2 ExitSize(ExitDecorArt art, string id)
        {
            var size = Vector2.zero;
            foreach (var suffix in new[] { "", "_Sway", "_Rustle" })
                foreach (var sprite in art.Frames(id + suffix))
                    size = Vector2.Max(size, sprite.bounds.size);
            return size;
        }

        static int ExitBandAt(ExitDecorContext context, Vector2 world)
        {
            var cell = Vector2Int.FloorToInt(world) - context.placed.Origin;
            if (cell.x < 0 || cell.y < 0 || cell.x >= context.template.Width || cell.y >= context.template.Height)
                return ExitBurnt;
            return context.bands[cell.x, cell.y];
        }

        static bool ExitOnFloor(ExitDecorContext context, Rect rect)
        {
            var origin = context.placed.Origin;
            int x0 = Mathf.FloorToInt(rect.xMin - origin.x), x1 = Mathf.CeilToInt(rect.xMax - origin.x) - 1;
            int y0 = Mathf.FloorToInt(rect.yMin - origin.y), y1 = Mathf.CeilToInt(rect.yMax - origin.y) - 1;
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (!IsFloor(context.template, x, y))
                        return false;
            return true;
        }

        static bool ExitOverlaps(Rect a, Rect b, float pad) =>
            a.xMin < b.xMax + pad && a.xMax > b.xMin - pad && a.yMin < b.yMax + pad && a.yMax > b.yMin - pad;

        /// <summary>True si el rectángulo cabe: todo sobre suelo, sin tocar nada reservado (ni el sumidero salvo que se pida).</summary>
        static bool ExitFits(ExitDecorContext context, Rect rect, bool ignorePit = false)
        {
            if (!ExitOnFloor(context, rect))
                return false;
            if (!ignorePit && ExitOverlaps(rect, context.pit, 0f))
                return false;
            foreach (var other in context.used)
                if (ExitOverlaps(rect, other, ExitPad))
                    return false;
            return true;
        }

        // Pie de una pieza con pivote abajo-centro: el centro de la celda, algo hacia abajo (como el resto de piezas).
        static Rect ExitFootRect(Vector2 cellCenter, Vector2 size) =>
            new(cellCenter.x - size.x / 2f, cellCenter.y - 0.4f, size.x, size.y);

        static Rect ExitCenterRect(Vector2 center, Vector2 size) => new(center - size / 2f, size);

        /// <summary>Distancia al sitio resguardado más cercano: un peñasco ya puesto o la base de una pared de arriba.</summary>
        static float ExitShelter(ExitDecorContext context, Vector2Int local)
        {
            var center = (Vector2)(context.placed.Origin + local) + Vector2.one * 0.5f;
            float best = float.MaxValue;
            foreach (var rock in context.rocks)
                best = Mathf.Min(best, Vector2.Distance(center, rock.center));
            // Al pie del acantilado: la celda de arriba (o la siguiente) ya no es suelo.
            if (!IsFloor(context.template, local.x, local.y + 1) || !IsFloor(context.template, local.x, local.y + 2))
                best = Mathf.Min(best, 1f);
            return best;
        }

        /// <summary>Coloca una regla entera; devuelve las piezas puestas (rectángulos reservados).</summary>
        static List<Rect> ExitPieces(ExitDecorContext context, ExitRule rule)
        {
            var placedRects = new List<Rect>();
            var ids = rule.ids.Where(id => context.art.Has(id) || context.art.Has(id + "_Sway")).ToArray();
            if (ids.Length == 0)
                return placedRects;

            int count = Mathf.RoundToInt(context.random.Next(rule.min, rule.max + 1) * context.scale);
            var origin = context.placed.Origin;
            var cells = context.open
                .Where(cell => context.bands[cell.x, cell.y] == rule.band && (!(rule.obstacle || rule.plant) || HasMargin(context.template, cell, 1)))
                .OrderBy(cell => rule.shelter ? ExitShelter(context, cell) + (float)context.random.NextDouble() * 4f : (float)context.random.NextDouble())
                .ToList();
            foreach (var cell in cells)
            {
                if (placedRects.Count == count)
                    break;
                string id = ids[context.random.Next(ids.Length)];
                var size = ExitSize(context.art, id);
                var center = (Vector2)(origin + cell) + Vector2.one * 0.5f;
                var rect = rule.ground ? ExitCenterRect(center, size) : ExitFootRect(center, size);
                // Los peñascos y troncos bloquean: nunca contra una pared ni en el estrecho de una puerta.
                if (rule.obstacle && !ExitFits(context, new Rect(rect.xMin - 0.5f, rect.yMin - 0.5f, rect.width + 1f, rect.height + 1f)))
                    continue;
                if (!ExitFits(context, rect))
                    continue;
                // Las plantas no crecen junto al sumidero (la arena de su borde las enterraría).
                if (rule.plant && Vector2.Distance(center, context.pitCenter) < context.pitRadii.x + 3f)
                    continue;

                context.used.Add(rect);
                placedRects.Add(rect);
                CreateExitPiece(context, rule, id, rect, center);
            }
            return placedRects;
        }

        static void CreateExitPiece(ExitDecorContext context, ExitRule rule, string id, Rect rect, Vector2 center)
        {
            var piece = new GameObject(id);
            piece.transform.SetParent(context.holder, false);
            var renderer = piece.AddComponent<SpriteRenderer>();
            if (rule.plant)
            {
                piece.transform.position = new Vector3(rect.center.x, rect.yMin, 0f);
                var plant = piece.AddComponent<ExitDecorPlant>();
                plant.Configure(renderer, context.art.Frames(id + "_Sway"), context.art.Frames(id + "_Rustle"), (float)context.random.NextDouble() * 10f);
                context.plants.Add(plant);
                return;
            }

            var sprite = context.art.Frames(id)[0];
            renderer.sprite = sprite;
            renderer.flipX = context.random.NextDouble() < 0.5;
            if (rule.ground)
            {
                piece.transform.position = new Vector3(rect.center.x, rect.center.y, 0f);
                renderer.sortingOrder = ExitDecalOrder;
                return;
            }

            piece.transform.position = new Vector3(rect.center.x, rect.yMin, 0f);
            if (!rule.obstacle)
            {
                renderer.sortingOrder = ExitLowOrder;
                return;
            }
            // Bloquea con una caja de 2×1 en la base; si el jugador pasa por detrás, se vuelve translúcido.
            var box = piece.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 1f);
            box.offset = new Vector2(0f, 0.5f);
            piece.AddComponent<ExitDecorObstacle>().Configure(renderer);
            var blocker = new Rect(rect.center.x - 1f, rect.yMin, 2f, 1f);
            context.blockers.Add(blocker);
            context.rocks.Add(rect);
        }

        /// <summary>
        /// La franja quemada ya tiene su arte (troncos, tocones, ascuas del tema Burnt): unas pocas piezas, para que no quede vacía
        /// pero sin cargar lo que ya arde.
        /// </summary>
        static void BuildExitBurnt(ExitDecorContext context, RoomTheme theme)
        {
            var origin = context.placed.Origin;
            var cells = context.open.Where(cell => context.bands[cell.x, cell.y] == ExitBurnt && HasMargin(context.template, cell, 1))
                .OrderBy(_ => context.random.Next()).ToList();

            void Obstacles(Sprite[] sprites, int size, int count)
            {
                if (sprites.Length == 0)
                    return;
                int made = 0;
                foreach (var cell in cells)
                {
                    if (made == count)
                        break;
                    var center = size == 1 ? (Vector2)(origin + cell) + Vector2.one * 0.5f : (Vector2)(origin + cell) + Vector2.one;
                    var sprite = sprites[context.random.Next(sprites.Length)];
                    // Se reserva el dibujo entero (suele sobresalir de su casilla), no solo la casilla.
                    var rect = ExitCenterRect(center, Vector2.Max(Vector2.one * size, sprite.bounds.size));
                    if (!ExitFits(context, new Rect(rect.xMin - 0.5f, rect.yMin - 0.5f, rect.width + 1f, rect.height + 1f)))
                        continue;
                    context.used.Add(rect);
                    context.blockers.Add(ExitCenterRect(center, Vector2.one * size));
                    made++;

                    var obstacle = new GameObject(size == 1 ? "Obstacle" : "ObstacleBig");
                    obstacle.transform.SetParent(context.holder, false);
                    obstacle.transform.position = center;
                    var renderer = obstacle.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = ObstacleOrder;
                    renderer.sprite = sprite;
                    obstacle.AddComponent<BoxCollider2D>().size = Vector2.one * size;
                    if (size == 1)
                        obstacle.AddComponent<JumpableObstacle>();
                    var outlineSprite = theme.OutlineOf(renderer.sprite);
                    if (outlineSprite == null)
                        continue;
                    var outline = new GameObject("Outline");
                    outline.transform.SetParent(obstacle.transform, false);
                    var outlineRenderer = outline.AddComponent<SpriteRenderer>();
                    outlineRenderer.sprite = outlineSprite;
                    outlineRenderer.color = theme.ObstacleOutline;
                    outlineRenderer.sortingOrder = ObstacleOrder - 1;
                }
            }

            int Scaled(int value) => Mathf.Max(1, Mathf.RoundToInt(value * context.scale));
            Obstacles(theme.BigObstacles, 2, Scaled(1));
            Obstacles(theme.SmallObstacles, 1, Scaled(2));

            var embers = theme.Scenery?.embers;
            if (embers == null || embers.Length == 0)
                return;
            int placedEmbers = 0;
            foreach (var cell in cells)
            {
                if (placedEmbers == Scaled(5))
                    break;
                var center = (Vector2)(origin + cell) + Vector2.one * 0.5f;
                var rect = ExitCenterRect(center, Vector2.one);
                if (!ExitFits(context, rect))
                    continue;
                context.used.Add(rect);
                placedEmbers++;
                var piece = new GameObject("Embers");
                piece.transform.SetParent(context.holder, false);
                piece.transform.position = center;
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = -2;
                piece.AddComponent<SpriteFramePlayer>().Play(renderer, embers, 0.3f, newLoop: true);
            }
        }

        // 0 = E, 1 = NE, 2 = N… (de 45° en 45°): la imagen base es E (pares) o NE (impares), girada de 90° en 90°.
        static int ExitDirection(Vector2 heading) => ((Mathf.RoundToInt(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg / 45f) % 8) + 8) % 8;

        sealed class ExitTrailPiece
        {
            public string id;
            public Vector2 at;
            public bool mirror;
            public Rect rect;
        }

        /// <summary>
        /// Rastro de un sinápsido que sale de detrás de un peñasco y llega al borde del sumidero: pares trasera-delantera que alternan
        /// de lado (pie izquierdo / derecho reflejado) y el surco de la cola por el eje. Las piezas del rastro se solapan entre sí
        /// a propósito (es un mismo trazo) pero no con nada más: todo el trazo se reserva y lo demás lo esquiva. Se prueba con cada
        /// peñasco y se usa el que deja el trazo más largo (un peñasco, otra pieza o una pared en medio lo cortarían).
        /// </summary>
        static void BuildExitTrail(ExitDecorContext context, List<Rect> rocks)
        {
            var art = context.art;
            if (!art.Has("Deco_Track_Hind_E") || !art.Has("Deco_Track_Fore_E") || !art.Has("Deco_TailDrag_E") || rocks.Count == 0)
                return;

            List<ExitTrailPiece> best = null;
            Vector2 bestHeading = Vector2.right;
            float bestLength = 0f;
            foreach (var rock in rocks)
            {
                var plan = PlanExitTrail(context, rock, out var heading, out float length);
                if (plan != null && length > bestLength)
                {
                    best = plan;
                    bestHeading = heading;
                    bestLength = length;
                }
            }
            if (best == null || best.Count == 0)
                return;

            int direction = ExitDirection(bestHeading);
            string kind = direction % 2 == 0 ? "E" : "NE";
            float rotation = (direction / 2) * 90f;
            var group = new GameObject("Trail").transform;
            group.SetParent(context.holder, false);
            foreach (var step in best)
            {
                var piece = new GameObject(step.id.Replace("{kind}", kind));
                piece.transform.SetParent(group, false);
                piece.transform.position = step.at;
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = art.Frames(step.id.Replace("{kind}", kind))[0];
                renderer.sortingOrder = ExitDecalOrder;
                float angle = rotation;
                if (step.mirror)
                {
                    // El pie derecho es el izquierdo reflejado sobre el eje de la marcha (E: voltear en Y; NE: trasponer).
                    if (kind == "E")
                        renderer.flipY = true;
                    else
                    {
                        renderer.flipX = true;
                        angle -= 90f;
                    }
                }
                piece.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                context.used.Add(step.rect);
            }
        }

        // Las piezas del trazo desde el borde del sumidero hacia el peñasco, cortadas en la primera que no cabe.
        static List<ExitTrailPiece> PlanExitTrail(ExitDecorContext context, Rect rock, out Vector2 heading, out float length)
        {
            heading = Vector2.right;
            length = 0f;
            var toPit = context.pitCenter - rock.center;
            float distance = toPit.magnitude;
            if (distance < context.pitRadii.x + 3f)
                return null;
            var dir = toPit / distance;
            heading = dir;
            var side = new Vector2(-dir.y, dir.x);
            int direction = ExitDirection(dir);
            string kind = direction % 2 == 0 ? "E" : "NE";

            // Hasta el borde del sumidero: donde el rayo desde el peñasco entra en su elipse (más un poco de aire).
            float ellipse = 1f / Mathf.Sqrt(Mathf.Pow(dir.x / context.pitRadii.x, 2f) + Mathf.Pow(dir.y / context.pitRadii.y, 2f));
            float end = distance - ellipse - 0.9f;
            const float start = 1.5f;
            if (end - start < 3.5f)
                return null;

            var origin = rock.center;
            var candidates = new List<(string id, Vector2 at, bool mirror)>();
            bool left = true;
            for (float s = end; s > start; s -= 1.625f)
            {
                float lateral = left ? 0.375f : -0.375f;
                candidates.Add(($"Deco_Track_Hind_{{kind}}", origin + dir * (s - 0.875f) + side * lateral, !left));
                candidates.Add(($"Deco_Track_Fore_{{kind}}", origin + dir * s + side * (lateral * (2f / 3f)), !left));
                left = !left;
            }
            for (float s = end; s > start; s -= 1f)
                candidates.Add(("Deco_TailDrag_{kind}", origin + dir * s, false));

            var plan = new List<ExitTrailPiece>();
            foreach (var (id, at, mirror) in candidates.OrderByDescending(item => Vector2.Dot(item.at - origin, dir)))
            {
                var sprite = context.art.Frames(id.Replace("{kind}", kind))[0];
                var rect = ExitCenterRect(at, sprite.bounds.size);
                // Las piezas que rozarían el sumidero se omiten (el rastro acaba un poco antes de su borde).
                if (!ExitOutsidePit(context, rect))
                    continue;
                // Un trazo es continuo: la primera pieza que no cabe, yendo del sumidero hacia el peñasco, lo corta ahí.
                if (!ExitFits(context, rect, ignorePit: true))
                    break;
                plan.Add(new ExitTrailPiece { id = id, at = at, mirror = mirror, rect = rect });
            }
            if (plan.Count == 0)
                return null;
            length = plan.Max(piece => Vector2.Dot(piece.at - origin, dir)) - plan.Min(piece => Vector2.Dot(piece.at - origin, dir));
            return plan;
        }

        /// <summary>
        /// Pone el peñasco del que sale el rastro: de entre varios sitios pérmicos a 7,5-14 casillas del sumidero, el que deja el trazo
        /// más largo sin chocar con nada. Null si no hay ninguno con al menos 3 casillas de rastro.
        /// </summary>
        static Rect? ExitTrailRock(ExitDecorContext context, ExitRule rule)
        {
            var ids = rule.ids.Where(id => context.art.Has(id)).ToArray();
            if (ids.Length == 0)
                return null;
            var origin = context.placed.Origin;
            Rect? best = null;
            string bestId = null;
            float bestLength = 3f;
            var cells = context.open
                .Where(cell => context.bands[cell.x, cell.y] == ExitPermian && HasMargin(context.template, cell, 1))
                .Where(cell =>
                {
                    float distance = Vector2.Distance((Vector2)(origin + cell) + Vector2.one * 0.5f, context.pitCenter);
                    return distance >= 7.5f && distance <= 14f;
                })
                .OrderBy(_ => context.random.Next()).Take(60).ToList();
            foreach (var cell in cells)
            {
                string id = ids[context.random.Next(ids.Length)];
                var center = (Vector2)(origin + cell) + Vector2.one * 0.5f;
                var rect = ExitFootRect(center, ExitSize(context.art, id));
                if (!ExitFits(context, new Rect(rect.xMin - 0.5f, rect.yMin - 0.5f, rect.width + 1f, rect.height + 1f)))
                    continue;
                // El peñasco todavía no está reservado: el rastro no debe empezar dentro de él.
                context.used.Add(rect);
                var plan = PlanExitTrail(context, rect, out _, out float length);
                context.used.Remove(rect);
                if (plan != null && length > bestLength)
                {
                    best = rect;
                    bestId = id;
                    bestLength = length;
                }
            }
            if (!best.HasValue)
                return null;
            context.used.Add(best.Value);
            CreateExitPiece(context, rule, bestId, best.Value, best.Value.center);
            return best;
        }

        // Las cuatro esquinas del rectángulo quedan fuera de la elipse del sumidero (con un poco de aire).
        static bool ExitOutsidePit(ExitDecorContext context, Rect rect)
        {
            var radii = context.pitRadii + Vector2.one * 0.2f;
            foreach (var corner in new[] { new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax), rect.center })
            {
                var offset = (corner - context.pitCenter) / radii;
                if (offset.sqrMagnitude < 1f)
                    return false;
            }
            return true;
        }

        /// <summary>Las filas de la sala partidas en tramos libres de obstáculos y de sumidero, para las ráfagas.</summary>
        static void BuildExitWind(ExitDecorContext context)
        {
            var art = context.art;
            var big = art.Frames("Fx_SandGust");
            if (big.Length == 0)
                return;
            var small = art.Frames("Fx_SandGust_Small");
            var origin = context.placed.Origin;
            var template = context.template;
            var rows = new List<List<ExitDecorWind.Run>>();
            const float MinBig = 8f;
            const float MinSmall = 3f;
            const float Speed = 6f;

            for (float local = 0f; local < template.Height; local += ExitRowStride)
            {
                int cellY = Mathf.FloorToInt(local);
                float y = origin.y + cellY + 0.5f;
                // Tramo de suelo continuo más a la izquierda de la fila.
                int x0 = -1;
                for (int x = 0; x < template.Width; x++)
                {
                    if (IsFloor(template, x, cellY) && IsFloor(template, x, cellY + 1))
                    {
                        x0 = x;
                        break;
                    }
                }
                if (x0 < 0)
                    continue;
                int x1 = x0;
                while (x1 < template.Width && IsFloor(template, x1, cellY) && IsFloor(template, x1, cellY + 1))
                    x1++;

                // Quita lo que frena la ráfaga: el sumidero y las cajas de los obstáculos que cruzan la fila.
                var blocks = new List<Vector2>();
                float dy = (y - context.pitCenter.y) / context.pitRadii.y;
                if (Mathf.Abs(dy) < 1f)
                {
                    float half = context.pitRadii.x * Mathf.Sqrt(1f - dy * dy);
                    blocks.Add(new Vector2(context.pitCenter.x - half - 0.2f, context.pitCenter.x + half + 0.2f));
                }
                foreach (var blocker in context.blockers)
                    if (y > blocker.yMin - 0.4f && y < blocker.yMax + 0.4f)
                        blocks.Add(new Vector2(blocker.xMin, blocker.xMax));
                blocks.Sort((a, b) => a.x.CompareTo(b.x));

                var runs = new List<ExitDecorWind.Run>();
                float cursor = origin.x + x0 + 0.3f;
                float limit = origin.x + x1 - 0.3f;
                float firstFrom = cursor;
                bool first = true;
                foreach (var block in blocks)
                {
                    if (block.y < cursor || block.x > limit)
                        continue;
                    AddRun(cursor, Mathf.Min(block.x, limit));
                    cursor = block.y + 2f;
                }
                AddRun(cursor, limit);
                if (runs.Count > 0 && !runs[0].small)
                    rows.Add(runs);

                void AddRun(float from, float to)
                {
                    float length = to - from;
                    if (first)
                    {
                        // Solo vale una fila cuya primera ráfaga sea grande; las siguientes, pequeñas, tras un obstáculo.
                        first = false;
                        if (length >= MinBig)
                            runs.Add(new ExitDecorWind.Run { y = y, from = from, to = to });
                        return;
                    }
                    if (length >= MinSmall && small.Length > 0 && runs.Count > 0 && context.random.NextDouble() < 0.5)
                        runs.Add(new ExitDecorWind.Run { y = y, from = from, to = to, small = true, delay = (from - firstFrom) / Speed });
                }
            }

            if (rows.Count == 0)
                return;
            var wind = new GameObject("Wind").AddComponent<ExitDecorWind>();
            wind.transform.SetParent(context.holder, false);
            wind.Configure(rows, big, small, context.plants, position =>
            {
                int band = ExitBandAt(context, position);
                return band == ExitPermian ? 1f : band == ExitMid ? 0.5f : 0f;
            }, context.random.Next());
        }
    }
}
