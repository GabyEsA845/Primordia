using System.Collections.Generic;
using System.IO;
using System.Linq;
using Primordia.Enemies;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        const string MegarachneBarSource = MegarachneSource + "/BossBar";
        const string MegarachneBarFolder = UIArtFolder + "/BossBar/Megarachne";
        const string MegarachneBarSkinPath = BossIntroDataFolder + "/BossBar_Megarachne.asset";

        /// <summary>
        /// Arte de la entrada, la muerte y la recompensa de Megarachne (M6): Threat, Death y Dead con sus sombras (se añaden al arte de
        /// cuerpo), y los efectos de la recompensa (capullo, polvo) y los ojos del embudo. Devuelve los efectos para el prefab.
        /// </summary>
        internal static MegarachneBoss.EffectSheet[] ImportMegarachneFinal()
        {
            string[] bodies = { "Threat", "Death", "Dead" };
            var bodyFiles = bodies.SelectMany(a => new[] { "Sheets/Megarachne_" + a + ".png", "Shadows/Megarachne_Shadow_" + a + ".png" }).ToArray();
            string[] effectFiles = { "Nido/Nido_Eyes.png", "Effects/Megarachne_Cocoon_Fall.png", "Effects/Megarachne_Cocoon_Open.png", "Effects/Fx_MegarachneDust.png" };
            CopyMegarachneFiles(bodyFiles.Concat(effectFiles));
            var art = AssetDatabase.LoadAssetAtPath<DirectionalArt>(MegarachneArtPath);
            if (art == null) throw new FileNotFoundException("Falta el arte base de Megarachne (M1).");
            var sheets = art.Sheets.ToDictionary(s => s.name);
            foreach (string file in bodyFiles)
            {
                var sheet = SliceDirectional(MegarachneFolder + "/" + file);
                sheets[sheet.name] = sheet;
            }
            art.SetSheets(sheets.Values.ToArray());
            EditorUtility.SetDirty(art);

            var center = new Vector2(0.5f, 0.5f);
            var result = new List<MegarachneBoss.EffectSheet>
            {
                new() { name = "Nido_Eyes", frames = SliceFloor3Sheet(MegarachneFolder + "/Nido/Nido_Eyes.png", 160, new Vector2(0.5f, 12f / 176f)) },
                new() { name = "Megarachne_Cocoon_Fall", frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/Megarachne_Cocoon_Fall.png", 24, center) },
                new() { name = "Megarachne_Cocoon_Open", frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/Megarachne_Cocoon_Open.png", 24, center) },
                new() { name = "Fx_MegarachneDust", frames = SliceFloor3Sheet(MegarachneFolder + "/Effects/Fx_MegarachneDust.png", 72, center) },
            };
            if (result.Any(e => e.frames.Length == 0)) throw new InvalidDataException("Una hoja de la recompensa de Megarachne salió vacía.");
            AssetDatabase.SaveAssets();
            return result.ToArray();
        }

        /// <summary>El contenedor de corazón de la tienda (el mismo que suelta el Lepidodendron).</summary>
        internal static Sprite[] LoadShopHeartContainer() => AssetDatabase.LoadAllAssetsAtPath("Assets/_Primordia/Art/Shop/Items/Item_HeartContainer_Idle.png")
            .OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();

        /// <summary>
        /// Barra de vida de Megarachne (arte aprobado en megarachne/BossBar): 240×28 con ventana de 176×6 en (46, 11), cable de seda con ella
        /// a la izquierda. Fase 1 relleno violeta; en su cable (invulnerable) relleno de seda gris; fase 2 (enfado al 50 %) marco con espinas,
        /// relleno rojo que late y ojos encendidos, con el efecto de las espinas al pasar el umbral.
        /// </summary>
        internal static BossBarSkin CreateMegarachneBarSkin()
        {
            const string prefix = "UI_BossBar_Megarachne_";
            const int frameWidth = 240, windowWidth = 176;
            Sprite[] Sheet(string name, int width) => ImportLooseSheet(
                $"{MegarachneBarSource}/{prefix}{name}.png", $"{MegarachneBarFolder}/{prefix}{name}.png", width);
            Sprite One(string name, int width) => Sheet(name, width).FirstOrDefault();
            var frame = Sheet("Frame", frameWidth);
            if (frame.Length == 0)
            {
                Debug.LogError("Falta el arte aprobado de la barra de Megarachne.");
                return null;
            }
            var calm = new BossBarSkin.PhaseLook { frame = frame, fill = Sheet("Fill_Violet", windowWidth), eyes = Sheet("Eyes_Calm", frameWidth) };
            var rage = new BossBarSkin.PhaseLook { frame = Sheet("Frame_Rage", frameWidth), fill = Sheet("Fill_Rage", windowWidth), fillFrameTime = 0.18f,
                eyes = Sheet("Eyes_Rage", frameWidth), eyesFrameTime = 0.18f };
            // En su cable es invulnerable: el relleno se cubre de seda gris.
            var silk = new BossBarSkin.PhaseLook { frame = frame, fill = Sheet("Fill_Silk", windowWidth), eyes = calm.eyes };
            Directory.CreateDirectory(BossIntroDataFolder);
            var skin = AssetDatabase.LoadAssetAtPath<BossBarSkin>(MegarachneBarSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BossBarSkin>();
                AssetDatabase.CreateAsset(skin, MegarachneBarSkinPath);
            }
            skin.Configure(frame[0], frame[0], new RectInt(46, 11, windowWidth, 6), One("Back", windowWidth), One("Fill_Trail", windowWidth),
                new[] { calm, rage }, silk, One("Notch", 7), One("Notch_Broken", 7), Sheet("Fx_NotchBreak", 16));
            skin.ConfigurePhaseEffects(new BossBarSkin.PhaseEffect[] { null, new() { frames = Sheet("Fx_Spines", frameWidth), frameTime = 0.1f } });
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }
    }
}
