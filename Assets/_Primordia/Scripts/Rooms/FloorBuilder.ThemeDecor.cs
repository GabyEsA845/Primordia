using System.Collections.Generic;
using System.Linq;
using Primordia.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Primordia.Rooms
{
    /// <summary>
    /// Decorado de las salas con tema del piso 3 (paso 6, bloque 1): la gran charca de agua de la Charca de sapos y las piezas
    /// propias de cada tema (nidos, seda, matas, juncos, rayos de luz…). Solo decorado, salvo el agua (apaga el quemado y carga
    /// la Piel anfibia, como los charcos) y que lo que arde se prende. Reglas de colocación de la usuaria: las piezas salen algo
    /// alejadas de las paredes y nunca sobre lodo profundo, charcos de lodo o de agua ni sobre la gran charca.
    /// </summary>
    public static partial class FloorBuilder
    {
        enum DecorKind
        {
            /// <summary>Pieza con pie (se mece si tiene varios cuadros; arde si tiene fuego).</summary>
            Prop,
            /// <summary>Calcomanía del suelo: una de sus variantes.</summary>
            Decal,
            /// <summary>Animación en bucle pegada al suelo.</summary>
            Loop,
            /// <summary>Esporas flotando: animación en bucle por encima del suelo.</summary>
            Spores,
            /// <summary>Rayo de luz con su pozo de luz en el suelo.</summary>
            Shaft,
            /// <summary>Sombra de una libélula gigante que cruza la sala de vez en cuando.</summary>
            Shadow,
        }

        sealed class DecorRule
        {
            public string[] ids;
            public int min, max;
            public float radius;
            public DecorKind kind;
            public bool shore;
            public float seconds;
        }

        static DecorRule Rule(string[] ids, int min, int max, float radius, DecorKind kind = DecorKind.Prop, bool shore = false, float seconds = 0.22f) =>
            new() { ids = ids, min = min, max = max, radius = radius, kind = kind, shore = shore, seconds = seconds };

        static string[] Abc(string prefix) => new[] { prefix + "_A", prefix + "_B", prefix + "_C" };

        // Qué lleva cada tema (clave del letrero) y cuánto. Las cantidades son para una sala de unas 550 casillas de suelo.
        static readonly Dictionary<string, DecorRule[]> ThemeRules = new()
        {
            ["BeetleNest"] = new[]
            {
                Rule(new[] { "Nest_Big", "Nest_Small", "Nest_Long" }, 3, 5, 1.6f),
                Rule(Abc("LeafPile"), 4, 7, 1.0f),
                Rule(new[] { "Deco_BeetleScorch" }, 5, 8, 0.8f, DecorKind.Decal),
            },
            ["SpiderLoom"] = new[]
            {
                Rule(new[] { "SilkWrap_Fallen", "SilkWrap_Trunk", "SilkWrap_Stump" }, 3, 5, 1.6f),
                Rule(new[] { "Cocoon_Small", "Cocoon_Medium", "Cocoon_Large" }, 3, 5, 1.0f),
                Rule(Abc("EggSac"), 3, 5, 1.0f),
            },
            ["ToadPond"] = new[]
            {
                Rule(Abc("ShoreReeds"), 6, 9, 0.8f, shore: true),
                Rule(new[] { "Deco_ToadSpawn" }, 2, 3, 0.9f, DecorKind.Decal, shore: true),
                Rule(new[] { "Deco_WetMud" }, 3, 4, 0.9f, DecorKind.Decal, shore: true),
                Rule(new[] { "TadpolePool" }, 2, 3, 1.0f, DecorKind.Loop, seconds: 0.15f),
                Rule(Abc("MossRock"), 3, 5, 0.9f),
            },
            ["CalamitesMeadow"] = new[]
            {
                Rule(Abc("CalamitesClump"), 6, 9, 1.1f),
                Rule(new[] { "Deco_HorsetailGrass" }, 6, 9, 0.9f, DecorKind.Decal),
                Rule(new[] { "Deco_FallenCones" }, 5, 8, 0.6f, DecorKind.Decal),
                Rule(new[] { "Fx_SporeDrift" }, 3, 4, 1.0f, DecorKind.Spores, seconds: 0.14f),
            },
            ["DragonflySky"] = new[]
            {
                Rule(new[] { "A", "B" }, 2, 3, 2.2f, DecorKind.Shaft, seconds: 0.18f),
                Rule(Abc("Perch"), 4, 6, 1.0f, seconds: 0.2f),
                Rule(new[] { "Fx_DragonflyShadow" }, 1, 1, 0f, DecorKind.Shadow, seconds: 0.09f),
            },
        };

        const int DecalOrder = -7;
        const int ShaftOrder = 25;
        const int SporeOrder = 15;
        const float PondMargin = 2f;
        // Centros de prueba y reducciones del tamaño antes de rendirse (la Charca de sapos debe tener charca).
        const int PondTries = 60;

        static readonly Dictionary<Sprite, Tile> WaterTiles = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetThemeDecorStatics() => WaterTiles.Clear();

        static bool IsFloor(RoomTemplate template, int x, int y) => template.Get(x, y) == RoomTemplate.Mark.Floor;

        /// <summary>True si todas las casillas a menos de <paramref name="radius"/> de la celda (en cuadrado) son suelo liso.</summary>
        static bool HasMargin(RoomTemplate template, Vector2Int cell, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    if (!IsFloor(template, cell.x + dx, cell.y + dy))
                        return false;
            return true;
        }

        static int ThemeBand(PlacedRoom placed) => Mathf.Clamp(placed.BaseTheme - FirstBandTheme, 0, ThemeDecorArt.Bands - 1);

        static string ThemeKey(PlacedRoom placed) => placed.Layout.Encounter?.signKey ?? "";

        /// <summary>
        /// La gran charca de agua de la Charca de sapos: una mancha de agua poco profunda que se camina, lejos de las paredes, las
        /// puertas y los obstáculos. Devuelve sus celdas (en coordenadas globales); vacío si el tema no la tiene o no cabe.
        /// </summary>
        static HashSet<Vector2Int> BuildPond(Transform parent, Transform roomObject, PlacedRoom placed, List<(Vector2 position, float radius)> taken,
            List<(Vector2 position, float radius)> puddleSpots)
        {
            var pond = new HashSet<Vector2Int>();
            if (ThemeKey(placed) != "ToadPond")
                return pond;
            var art = ThemeDecorArt.Current?.GetWater(ThemeBand(placed));
            var level = parent.Find("Level");
            var ground = level != null ? level.Find("Ground")?.GetComponent<Tilemap>() : null;
            if (art == null || ground == null)
                return pond;

            var template = placed.Template;
            var random = new System.Random(placed.Origin.x * 31337 ^ placed.Origin.y * 7919 ^ 0x70d);
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var centers = EncounterPlanner.SpawnCells(template, doorCells);
            if (centers.Count == 0)
                return pond;

            float rx = 5.2f, ry = 3.4f;
            for (int shrink = 0; shrink < 4 && pond.Count == 0; shrink++, rx *= 0.8f, ry *= 0.8f)
            {
                foreach (var center in centers.OrderBy(_ => random.Next()).Take(PondTries))
                {
                    var cells = PondShape(center, rx, ry, random);
                    if (cells.Count < 12 || !cells.All(cell => HasMargin(template, cell, (int)PondMargin)))
                        continue;
                    foreach (var cell in cells)
                        pond.Add(placed.Origin + cell);
                    break;
                }
            }
            if (pond.Count == 0)
            {
                Debug.LogWarning($"FloorBuilder: no cabe la gran charca en {placed.Layout.Name}.");
                return pond;
            }

            PaintPond(level, ground, art, pond, random);
            var holder = new GameObject("Pond").transform;
            holder.SetParent(roomObject, false);
            // Brillos y reflejos sobre algunas celdas.
            foreach (var cell in pond)
            {
                if (art.shimmer.Length == 0 || random.NextDouble() > 0.22)
                    continue;
                var shimmer = new GameObject("Shimmer");
                shimmer.transform.SetParent(holder, false);
                shimmer.transform.position = (Vector2)cell + Vector2.one * 0.5f;
                var renderer = shimmer.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = -8;
                shimmer.AddComponent<SpriteFramePlayer>().Play(renderer, art.shimmer, 0.16f * (0.8f + 0.4f * (float)random.NextDouble()), newLoop: true);
            }
            GroundPuddle.CreateZone(holder, pond);

            // Lo demás del escenario la esquiva: círculos a lo largo de la charca.
            foreach (var cell in pond)
            {
                if (((cell.x + cell.y) & 1) != 0)
                    continue;
                var center = (Vector2)cell + Vector2.one * 0.5f;
                taken.Add((center, 1.1f));
                puddleSpots?.Add((center, 1.1f));
            }
            return pond;
        }

        // Mancha irregular: el núcleo del óvalo siempre; el borde, a trozos; y sin celdas sueltas.
        static HashSet<Vector2Int> PondShape(Vector2Int center, float rx, float ry, System.Random random)
        {
            var cells = new HashSet<Vector2Int>();
            int reachX = Mathf.CeilToInt(rx) + 1, reachY = Mathf.CeilToInt(ry) + 1;
            for (int dx = -reachX; dx <= reachX; dx++)
            {
                for (int dy = -reachY; dy <= reachY; dy++)
                {
                    float d = (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry);
                    if (d <= 0.6f || (d <= 1f && random.NextDouble() < 0.65))
                        cells.Add(center + new Vector2Int(dx, dy));
                }
            }
            cells.RemoveWhere(cell => Neighbours(cells, cell) < 2);
            return cells;
        }

        static int Neighbours(HashSet<Vector2Int> cells, Vector2Int cell) =>
            (cells.Contains(cell + Vector2Int.up) ? 1 : 0) + (cells.Contains(cell + Vector2Int.down) ? 1 : 0)
            + (cells.Contains(cell + Vector2Int.left) ? 1 : 0) + (cells.Contains(cell + Vector2Int.right) ? 1 : 0);

        static Tile WaterTile(Sprite sprite)
        {
            if (sprite == null)
                return null;
            if (!WaterTiles.TryGetValue(sprite, out var tile) || tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                WaterTiles[sprite] = tile;
            }
            return tile;
        }

        // Tile de orilla según los vecinos que no son agua (N=1, E=2, S=4, O=8) y esquinas de dentro, como el lodo profundo.
        static void PaintPond(Transform level, Tilemap ground, ThemeDecorArt.Water art, HashSet<Vector2Int> pond, System.Random random)
        {
            var decoration = level.Find("Decoration")?.GetComponent<Tilemap>();
            var caustics = level.Find("Caustics")?.GetComponent<Tilemap>();
            var corners = new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach (var cell in pond)
            {
                var at = new Vector3Int(cell.x, cell.y, 0);
                int mask = (pond.Contains(cell + Vector2Int.up) ? 0 : 1) | (pond.Contains(cell + Vector2Int.right) ? 0 : 2)
                    | (pond.Contains(cell + Vector2Int.down) ? 0 : 4) | (pond.Contains(cell + Vector2Int.left) ? 0 : 8);
                ground.SetTile(at, WaterTile(art.tiles[mask]));
                caustics?.SetTile(at, null);
                if (decoration == null)
                    continue;
                decoration.SetTile(at, null);
                for (int i = 0; i < corners.Length && art.corners.Length == 4; i++)
                {
                    var (dx, dy) = corners[i];
                    if (pond.Contains(cell + new Vector2Int(dx, 0)) && pond.Contains(cell + new Vector2Int(0, dy))
                        && !pond.Contains(cell + new Vector2Int(dx, dy)))
                    {
                        decoration.SetTile(at, WaterTile(art.corners[i]));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Las piezas de decorado del tema de la sala (nada si no es una sala con tema o no hay arte importado). Va después de
        /// los charcos y los odres: se coloca donde queda sitio libre.
        /// </summary>
        static void BuildThemeDecor(Transform roomObject, PlacedRoom placed, HashSet<Vector2Int> pond, List<(Vector2 position, float radius)> taken)
        {
            var art = ThemeDecorArt.Current;
            if (art == null || !ThemeRules.TryGetValue(ThemeKey(placed), out var rules))
                return;

            var random = new System.Random(placed.Origin.x * 92821 ^ placed.Origin.y * 689287 ^ 0x5eed);
            int band = ThemeBand(placed);
            var template = placed.Template;
            var doorCells = placed.DoorCells.Values.SelectMany(cells => cells).Select(cell => cell - placed.Origin);
            var open = EncounterPlanner.SpawnCells(template, doorCells);
            int floorCells = 0;
            for (int y = 0; y < template.Height; y++)
                for (int x = 0; x < template.Width; x++)
                    if (IsFloor(template, x, y))
                        floorCells++;
            float scale = Mathf.Clamp(floorCells / 550f, 0.7f, 1.6f);

            // Lejos de las paredes y de la charca; las de orilla, pegadas a ella por fuera.
            int PondDistance(Vector2Int cell)
            {
                if (pond.Count == 0)
                    return int.MaxValue;
                int best = int.MaxValue;
                var global = placed.Origin + cell;
                for (int dx = -2; dx <= 2; dx++)
                    for (int dy = -2; dy <= 2; dy++)
                        if (pond.Contains(global + new Vector2Int(dx, dy)))
                            best = Mathf.Min(best, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)));
                return best;
            }

            var inner = open.Where(cell => HasMargin(template, cell, 2) && PondDistance(cell) > 2).ToList();
            var shore = open.Where(cell => PondDistance(cell) == 1).ToList();
            var holder = new GameObject("ThemeDecor").transform;
            holder.SetParent(roomObject, false);

            foreach (var rule in rules)
            {
                int count = Mathf.RoundToInt(random.Next(rule.min, rule.max + 1) * scale);
                var cells = (rule.shore && shore.Count > 0 ? shore : inner).OrderBy(_ => random.Next()).ToList();
                int made = 0;
                foreach (var cell in cells)
                {
                    if (made == count)
                        break;
                    var position = (Vector2)(placed.Origin + cell) + Vector2.one * 0.5f;
                    if (rule.radius > 0f && !TakeSpot(taken, position, rule.radius))
                        continue;
                    if (PlaceDecor(holder, art, rule, band, position, random, placed))
                        made++;
                    else
                    {
                        // Sin dibujo en este tramo (p. ej. las esporas en la tierra quemada): nada más que probar.
                        if (rule.radius > 0f)
                            taken.RemoveAt(taken.Count - 1);
                        break;
                    }
                }
            }
        }

        static bool PlaceDecor(Transform holder, ThemeDecorArt art, DecorRule rule, int band, Vector2 position, System.Random random, PlacedRoom placed)
        {
            string id = rule.ids[random.Next(rule.ids.Length)];
            switch (rule.kind)
            {
                case DecorKind.Prop:
                {
                    var look = art.Get(id, band);
                    if (look == null)
                        return false;
                    var prop = new GameObject(id);
                    prop.transform.SetParent(holder, false);
                    // Pivote en el pie: la base de la pieza, en la parte de abajo de la celda.
                    prop.transform.position = new Vector3(position.x, position.y - 0.4f, 0f);
                    var renderer = prop.AddComponent<SpriteRenderer>();
                    renderer.flipX = look.idle.Length == 1 && random.NextDouble() < 0.5;
                    prop.AddComponent<ThemeProp>().Configure(renderer, look, rule.seconds, (float)random.NextDouble() * 10f);
                    return true;
                }
                case DecorKind.Decal:
                {
                    var look = art.Get(id, band);
                    if (look == null)
                        return false;
                    var decal = NewRenderer(holder, id, position, DecalOrder);
                    decal.sprite = look.idle[random.Next(look.idle.Length)];
                    decal.flipX = random.NextDouble() < 0.5;
                    return true;
                }
                case DecorKind.Loop:
                case DecorKind.Spores:
                {
                    var look = art.Get(id, band);
                    if (look == null)
                        return false;
                    bool spores = rule.kind == DecorKind.Spores;
                    var renderer = NewRenderer(holder, id, position + (spores ? Vector2.up * 0.5f : Vector2.zero), spores ? SporeOrder : DecalOrder);
                    renderer.gameObject.AddComponent<SpriteFramePlayer>().Play(renderer, look.idle, rule.seconds, newLoop: true);
                    return true;
                }
                case DecorKind.Shaft:
                {
                    var shaft = art.Get($"Fx_LightShaft_{id}", band);
                    var pool = art.Get($"Deco_LightPool_{id}", band);
                    if (shaft == null)
                        return false;
                    if (pool != null)
                    {
                        var poolRenderer = NewRenderer(holder, $"LightPool_{id}", position, DecalOrder);
                        poolRenderer.gameObject.AddComponent<SpriteFramePlayer>().Play(poolRenderer, pool.idle, rule.seconds, newLoop: true);
                    }
                    // La base del rayo cae abajo a la derecha de su dibujo, sobre el pozo.
                    var size = shaft.idle[0].bounds.size;
                    var shaftRenderer = NewRenderer(holder, $"LightShaft_{id}", position + new Vector2(0.4f - size.x / 2f, size.y / 2f - 0.3f), ShaftOrder);
                    shaftRenderer.gameObject.AddComponent<SpriteFramePlayer>().Play(shaftRenderer, shaft.idle, rule.seconds, newLoop: true);
                    return true;
                }
                case DecorKind.Shadow:
                {
                    var look = art.Get(id, band);
                    if (look == null)
                        return false;
                    var shadow = NewRenderer(holder, id, position, DecalOrder);
                    shadow.enabled = false;
                    var bounds = placed.Template.FloorBounds();
                    var area = new Rect(placed.Origin.x + bounds.x, placed.Origin.y + bounds.y, bounds.width, bounds.height);
                    shadow.gameObject.AddComponent<DragonflyShadow>().Configure(shadow, look.idle, rule.seconds, area, random.Next());
                    return true;
                }
            }
            return false;
        }

        static SpriteRenderer NewRenderer(Transform holder, string name, Vector2 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(holder, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
