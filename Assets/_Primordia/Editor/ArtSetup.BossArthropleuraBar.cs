using System;
using System.IO;
using System.Linq;
using Primordia.UI;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    public static partial class ArtSetup
    {
        const string ArthropleuraBarSource = "docs/multimedia-temp/floor3-bosses/arthropleura/BossBar";
        const string ArthropleuraBarFolder = UIArtFolder + "/BossBar/Arthropleura";
        const string ArthropleuraBarSkinPath = BossIntroDataFolder + "/BossBar_Arthropleura.asset";

        /// <summary>
        /// Barra de vida de la Arthropleura (arte aprobado en floor3-bosses/arthropleura/BossBar): el propio bicho visto desde arriba, con
        /// marco, relleno y ojos distintos en cada fase (cobre, ascua, carne abierta, carne viva de la rueda) y un efecto al pasar al 70 % y al 35 %.
        /// </summary>
        internal static BossBarSkin CreateArthropleuraBarSkin()
        {
            const string prefix = "UI_BossBar_Arthropleura_";
            const int frameWidth = 320, windowWidth = 260;
            Sprite[] Sheet(string name, int width) => ImportLooseSheet(
                $"{ArthropleuraBarSource}/{prefix}{name}.png", $"{ArthropleuraBarFolder}/{prefix}{name}.png", width);
            Sprite One(string name, int width) => Sheet(name, width).FirstOrDefault();
            var copper = Sheet("Frame_Copper", frameWidth);
            if (copper.Length == 0)
            {
                Debug.LogError("Falta el arte aprobado de la barra de la Arthropleura.");
                return null;
            }
            var phases = new[]
            {
                new BossBarSkin.PhaseLook { frame = copper, fill = Sheet("Fill_Copper", windowWidth), eyes = Sheet("Eyes_Hunt", frameWidth) },
                new BossBarSkin.PhaseLook { frame = Sheet("Frame_Ember", frameWidth), fill = Sheet("Fill_Ember", windowWidth), fillFrameTime = 0.12f,
                    eyes = Sheet("Eyes_Burn", frameWidth), eyesFrameTime = 0.26f },
                new BossBarSkin.PhaseLook { frame = Sheet("Frame_Open", frameWidth), fill = Sheet("Fill_Flesh", windowWidth), fillFrameTime = 0.26f,
                    eyes = Sheet("Eyes_Flesh", frameWidth), eyesFrameTime = 0.26f },
                new BossBarSkin.PhaseLook { frame = Sheet("Frame_Raw", frameWidth), fill = Sheet("Fill_Raw", windowWidth), fillFrameTime = 0.2f,
                    eyes = Sheet("Eyes_Roll", frameWidth), eyesFrameTime = 0.11f },
            };
            Directory.CreateDirectory(BossIntroDataFolder);
            var skin = AssetDatabase.LoadAssetAtPath<BossBarSkin>(ArthropleuraBarSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BossBarSkin>();
                AssetDatabase.CreateAsset(skin, ArthropleuraBarSkinPath);
            }
            // No hay marco en silueta: el jefe no usa el aspecto de inmune (su caparazón es invulnerable pero el fuego y las placas lo dañan).
            skin.Configure(copper[0], copper[0], new RectInt(46, 12, windowWidth, 8), One("Back", windowWidth),
                One("Fill_Trail", windowWidth), phases, phases[0], One("Notch", 8), One("Notch_Broken", 8), Sheet("Fx_NotchBreak", 16));
            skin.ConfigurePhaseEffects(new BossBarSkin.PhaseEffect[]
            {
                null,
                new() { frames = Sheet("Fx_WallBreak", frameWidth), frameTime = 0.08f },
                new() { frames = Sheet("Fx_PlatesOpen", frameWidth), frameTime = 0.08f },
                null,
            });
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        const string ArthropleuraArrowSource = "docs/multimedia-temp/floor3-bosses/arthropleura/EdgeArrows";
        const string ArthropleuraArrowFolder = UIArtFolder + "/EdgeArrows/Arthropleura";

        /// <summary>Flechas del borde de pantalla de la Arthropleura (arte aprobado): 4 insignias de 24 px y 3 punteros de 12 px en 16 direcciones.</summary>
        internal static Primordia.Enemies.EdgeArrowArt CreateArthropleuraArrowArt()
        {
            Sprite[] Sheet(string name, int width) => ImportLooseSheet(
                $"{ArthropleuraArrowSource}/UI_EdgeArrow_Arthropleura_{name}.png", $"{ArthropleuraArrowFolder}/UI_EdgeArrow_Arthropleura_{name}.png", width);
            var art = new Primordia.Enemies.EdgeArrowArt
            {
                badgeHead = Sheet("Badge_Head", 24), badgeHeadWarn = Sheet("Badge_HeadWarn", 24),
                badgeTail = Sheet("Badge_Tail", 24), badgeWeak = Sheet("Badge_Weak", 24),
                pointerHead = Sheet("Pointer_Head", 12), pointerTail = Sheet("Pointer_Tail", 12), pointerWeak = Sheet("Pointer_Weak", 12),
            };
            if (art.badgeHead.Length == 0)
                Debug.LogError("Falta el arte aprobado de las flechas de la Arthropleura.");
            return art;
        }
    }
}
