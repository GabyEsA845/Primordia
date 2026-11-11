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
        const string MeganeuraBarSource = "docs/multimedia-temp/floor3-bosses/meganeura/BossBar";
        const string MeganeuraBarFolder = UIArtFolder + "/BossBar/Meganeura";
        const string MeganeuraBarSkinPath = BossIntroDataFolder + "/BossBar_Meganeura.asset";
        const string MeganeuraBarPrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Meganeura.prefab";

        [MenuItem("Primordia/Boss/Crear barra de la Meganeura")]
        public static void CreateMeganeuraBar()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MeganeuraBarPrefabPath) == null)
            {
                Debug.LogError("Falta Boss_Meganeura; crea primero el jefe.");
                return;
            }
            var skin = CreateMeganeuraBarSkin();
            if (skin == null)
                return;
            EditPrefab(MeganeuraBarPrefabPath, root =>
            {
                var appearance = root.GetComponent<BossBarAppearance>();
                if (appearance == null)
                    appearance = root.AddComponent<BossBarAppearance>();
                appearance.Configure(skin);
            });
            AssetDatabase.SaveAssets();
            Debug.Log("Barra de la Meganeura creada y asignada al prefab.");
        }

        internal static BossBarSkin CreateMeganeuraBarSkin()
        {
            const string prefix = "UI_BossBar_Meganeura_";
            const int frameWidth = 300, windowWidth = 241;
            Sprite[] Sheet(string name, int width) => ImportLooseSheet(
                $"{MeganeuraBarSource}/{prefix}{name}.png", $"{MeganeuraBarFolder}/{prefix}{name}.png", width);
            Sprite One(string name, int width) => Sheet(name, width).FirstOrDefault();
            var frame = Sheet("Frame", frameWidth);
            if (frame.Length == 0)
            {
                Debug.LogError("Falta el arte aprobado de la barra de la Meganeura.");
                return null;
            }
            var phases = new[]
            {
                new BossBarSkin.PhaseLook { frame = frame, fill = Sheet("Fill_Teal", windowWidth), fillFrameTime = 0.12f, eyes = Sheet("Eyes_Hunt", frameWidth) },
                new BossBarSkin.PhaseLook { frame = Sheet("Frame_Torn1", frameWidth), fill = Sheet("Fill_Rage", windowWidth), eyes = Sheet("Eyes_Rage", frameWidth) },
                new BossBarSkin.PhaseLook { frame = Sheet("Frame_Torn2", frameWidth), fill = Sheet("Fill_Storm", windowWidth), fillFrameTime = 0.08f, eyes = Sheet("Eyes_Storm", frameWidth), eyesFrameTime = 0.12f },
            };
            var dark = new BossBarSkin.PhaseLook { fill = Sheet("Fill_Cloud", windowWidth), fillFrameTime = 0.16f };
            Directory.CreateDirectory(BossIntroDataFolder);
            var skin = AssetDatabase.LoadAssetAtPath<BossBarSkin>(MeganeuraBarSkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BossBarSkin>();
                AssetDatabase.CreateAsset(skin, MeganeuraBarSkinPath);
            }
            skin.Configure(frame[0], frame[0], new RectInt(43, 12, windowWidth, 8), One("Back", windowWidth),
                One("Fill_Trail", windowWidth), phases, dark, One("Notch", 8), One("Notch_Broken", 8), Array.Empty<Sprite>());
            skin.ConfigureMeganeura(new BossBarSkin.PhaseEffect[]
            {
                null,
                new() { frames = Sheet("Fx_EggBurst", frameWidth), frameTime = 0.08f },
                new() { frames = Sheet("Fx_BoltStrike", frameWidth), frameTime = 0.07f },
            }, Sheet("Fx_LowSparks", frameWidth));
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }
    }
}
