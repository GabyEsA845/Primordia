#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>Decorado de las salas con tema del piso 3 sobre el piso de pruebas (Floor_3_Themes), con el arte importado.</summary>
    public class ThemeDecorTests
    {
        const string ConfigPath = "Assets/_Primordia/Data/Rooms/Floor_3_Themes.asset";

        [Test]
        public void ShowcaseFloor_HasOneRoomPerTheme_EachWithItsDecor()
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(ConfigPath);
            Assert.IsNotNull(config, "falta Floor_3_Themes: Primordia > Floor 3 > Crear escena de prueba de las salas con tema.");
            Assert.IsNotNull(ThemeDecorArt.Current, "falta el arte: Primordia > Floor 3 > Importar decorado de las salas con tema.");

            for (int seed = 1; seed <= 6; seed++)
            {
                var parent = new GameObject("Floor").transform;
                var layout = FloorLayoutGenerator.Generate(config, new System.Random(seed));
                var themed = layout.Rooms.Where(room => room.Encounter.HasValue).ToList();
                Assert.AreEqual(config.EncounterThemes.Length, themed.Count, $"semilla {seed}: una sala por tema.");
                Assert.AreEqual(themed.Count, themed.Select(room => room.Encounter.Value.name).Distinct().Count(), $"semilla {seed}: sin repetir.");

                var built = FloorBuilder.Build(layout, config, parent, new System.Random(seed));
                foreach (var room in built.Rooms.Where(room => !string.IsNullOrEmpty(room.SignKey)))
                {
                    var decor = room.transform.Find("ThemeDecor");
                    Assert.IsNotNull(decor, $"semilla {seed}: {room.name} ({room.SignKey}) sin decorado.");
                    Assert.Greater(decor.childCount, 3, $"semilla {seed}: {room.name} ({room.SignKey}) con muy pocas piezas.");
                    if (room.SignKey == "ToadPond")
                    {
                        Assert.IsNotNull(room.transform.Find("Pond"), $"semilla {seed}: la Charca de sapos debe tener su gran charca.");
                        Assert.IsNotNull(room.GetComponentInChildren<GroundPuddle>(), $"semilla {seed}: la gran charca debe ser agua.");
                    }
                }

                // Nada del decorado sobre la gran charca (regla de colocación).
                foreach (var room in built.Rooms.Where(room => room.SignKey == "ToadPond"))
                {
                    var pond = room.GetComponentInChildren<GroundPuddle>();
                    foreach (var prop in room.GetComponentsInChildren<ThemeProp>())
                        Assert.IsFalse(GroundPuddle.IsOver(prop.Position + Vector2.up * 0.4f), $"semilla {seed}: {prop.name} sobre el agua.");
                }

                Object.DestroyImmediate(parent.gameObject);
                layout.ReleaseGenerated();
            }
        }
    }
}
#endif
