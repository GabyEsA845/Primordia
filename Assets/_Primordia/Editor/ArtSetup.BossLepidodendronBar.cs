using System;
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
        internal static BossBarSkin CreateLepidodendronBarSkin()
        {
            const string prefix = "UI_BossBar_Lepidodendron_";
            Sprite[] Sheet(string name, int width)
            {
                string file = prefix + name + ".png";
                string source = "docs/multimedia-temp/floor3-bosses/lepidodendron/BossBar/" + file;
                if (!File.Exists(source)) throw new FileNotFoundException(source);
                return ImportLooseSheet(source, UIArtFolder + "/BossBar/Lepidodendron/" + file, width);
            }
            var calm = new BossBarSkin.PhaseLook { frame = Sheet("Frame", 320), fill = Sheet("Fill_Sap", 260), fillFrameTime = 0.15f,
                eyes = Sheet("Head_Calm", 46), eyesFrameTime = 0.16f };
            var final = new BossBarSkin.PhaseLook { frame = Sheet("Frame_Final", 320), fill = Sheet("Fill_SapGold", 260), fillFrameTime = 0.09f,
                eyes = Sheet("Head_Final", 46), eyesFrameTime = 0.09f };
            const string path = BossIntroDataFolder + "/BossBar_Lepidodendron.asset";
            var skin = AssetDatabase.LoadAssetAtPath<BossBarSkin>(path);
            if (skin == null) { skin = ScriptableObject.CreateInstance<BossBarSkin>(); AssetDatabase.CreateAsset(skin, path); }
            // El README mide desde arriba; RectInt de la interfaz mide desde abajo.
            skin.Configure(calm.frame[0], calm.frame[0], new RectInt(46, 44 - 13 - 8, 260, 8), Sheet("Back", 260)[0], Sheet("Fill_Trail", 260)[0],
                new[] { calm, calm, calm, final }, calm, Sheet("Knot", 12)[0], Sheet("Knot_Broken", 12)[0], Sheet("Fx_KnotBurst", 32));
            skin.ConfigureLepidodendron(Sheet("Knot", 12), Sheet("Knot_Hit", 12), Sheet("Head_Hit", 46),
                new[] { Sheet("Cone_Appear", 12), Sheet("Cone_Armor", 12), Sheet("Cone_Charred", 12), Sheet("Cone_Exposed", 12), Sheet("Cone_Broken", 12) });
            EditorUtility.SetDirty(skin);
            return skin;
        }

        internal static BossIntroData CreateLepidodendronIntro()
        {
            Sprite Import(string folder, string prefix)
            {
                string file = prefix + "_Lepidodendron.png";
                string source = BossIntroSourceFolder + "/" + folder + "/" + file;
                if (!File.Exists(source)) throw new FileNotFoundException(source);
                return ImportSprite(source, BossIntroArtFolder + "/" + file, Vector4.zero);
            }
            var portrait = Import("Portraits", "Portrait");
            var title = Import("Titles", "Title");
            const string path = BossIntroDataFolder + "/BossIntro_Lepidodendron.asset";
            var data = AssetDatabase.LoadAssetAtPath<BossIntroData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<BossIntroData>(); AssetDatabase.CreateAsset(data, path); }
            data.Configure(title, "Corazón del bosque", portrait, new Color(0.12f, 0.05f, 0.1f), new Color(0.8f, 0.5f, 1f));
            EditorUtility.SetDirty(data);
            return data;
        }
    }
}
