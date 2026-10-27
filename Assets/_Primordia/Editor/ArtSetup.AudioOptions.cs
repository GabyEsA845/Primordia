using Primordia.Audio;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Volumen en la pausa (docs/multimedia-temp/menus/Pause/, de generar_volumen.py): cuelga bajo el panel de
    /// estadísticas un panel "AUDIO" del mismo estilo con dos barras, Música (nota) y Efectos (altavoz; la interfaz
    /// va con los efectos), cada una con su icono y el porcentaje, y la opción Temblor (normal, suave o no; la guarda
    /// <see cref="Core.ScreenShake"/>). El volumen lo guarda <see cref="AudioVolumes"/>.
    /// También lo hace Importar menús, porque rehace el panel de estadísticas.
    /// </summary>
    public static partial class ArtSetup
    {
        const float VolumeGap = 6f;
        const float SliderHeight = 10f;

        struct VolumeArt
        {
            public Sprite Panel, Track, Fill, Knob, Music, Effects;
        }

        [MenuItem("Primordia/Audio/Añadir volumen a la pausa")]
        static void AddVolumeMenu()
        {
            AddVolumeToPause();
            ApplyPixelFont(UISetup.GameUIPath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Rehace el panel del volumen y el temblor de la pausa (lo usa también el menú del Euriptérido).</summary>
        internal static void RebuildPauseOptions()
        {
            AddVolumeToPause();
            ApplyPixelFont(UISetup.GameUIPath);
            AssetDatabase.SaveAssets();
        }

        static void AddVolumeToPause()
        {
            var art = new VolumeArt
            {
                Panel = MenuSprite("Pause", "UI_Stats_Panel", 10),
                Track = MenuSprite("Pause", "UI_Slider_Track", new Vector4(4, 0, 4, 0)),
                Fill = MenuSprite("Pause", "UI_Slider_Fill", new Vector4(4, 0, 4, 0)),
                Knob = MenuSprite("Pause", "UI_Slider_Knob"),
                Music = MenuSprite("Pause", "UI_Stat_Music"),
                Effects = MenuSprite("Pause", "UI_Stat_Sfx"),
            };
            if (art.Track == null || art.Knob == null)
                Debug.LogWarning("ArtSetup: faltan las piezas del volumen; ejecuta generar_volumen.py en docs/multimedia-temp/menus.");

            EditPrefab(UISetup.GameUIPath, root =>
            {
                var stats = root.transform.Find("PausePanel/Stats");
                if (stats == null)
                {
                    Debug.LogWarning("ArtSetup: la pausa no tiene panel de estadísticas; ejecuta antes Importar menús.");
                    return;
                }
                BuildVolumePanel(stats, art);
            });
        }

        /// <summary>Panel bajo el de estadísticas (hijo suyo, fuera de su columna): baja con él si crece.</summary>
        static void BuildVolumePanel(Transform stats, VolumeArt art)
        {
            var panel = Recreate("Audio", stats);
            panel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(0f, -VolumeGap);
            panel.sizeDelta = new Vector2(StatsWidth, 0f);
            SlicedImage(panel.gameObject, art.Panel);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 10);
            layout.spacing = 2f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var heading = Label(panel, "AUDIO", UISetup.TitleColor, TextAnchor.MiddleLeft);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            VolumeRow(panel, SoundGroup.Music, art.Music, art);
            VolumeRow(panel, SoundGroup.Sfx, art.Effects, art);
            ShakeRow(panel);
        }

        /// <summary>Fila "Temblor" con el ajuste a la derecha; al pulsar el ajuste, pasa al siguiente.</summary>
        static void ShakeRow(Transform panel)
        {
            var row = UISetup.CreateRow("Row_Shake", panel, panel.childCount, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 1, 0, 0);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = SliderHeight + 3f;

            var name = Label(row, "Temblor", UISetup.TextColor, TextAnchor.MiddleLeft);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var value = Label(row, "Normal", UISetup.TitleColor, TextAnchor.MiddleRight);
            value.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            value.raycastTarget = true;
            value.gameObject.AddComponent<ScreenShakeOption>().Configure(value);
        }

        /// <summary>Una fila con el icono, la barra y el porcentaje. Cabe bajo las estadísticas sin tapar la semilla.</summary>
        static void VolumeRow(Transform panel, SoundGroup group, Sprite icon, VolumeArt art)
        {
            var row = UISetup.CreateRow($"Row_{group}", panel, panel.childCount, TextAnchor.MiddleLeft);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(3, 1, 0, 0);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = SliderHeight + 3f;

            var iconImage = UISetup.CreateIcon("Icon", row, 12f);
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;

            var holder = UISetup.CreateRect("Slider", row);
            var element = holder.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredHeight = SliderHeight + 2f;
            var slider = BuildSlider(holder, art);

            var value = Label(row, "80 %", UISetup.TextColor, TextAnchor.MiddleRight);
            value.gameObject.AddComponent<LayoutElement>().preferredWidth = 26f;
            slider.gameObject.AddComponent<VolumeSlider>().Configure(group, value);
        }

        /// <summary>
        /// Slider de uGUI con las piezas pixel: riel a lo ancho, relleno que llega hasta el tirador y tirador de 8×10.
        /// El hueco de 3 px a cada lado deja sitio al tirador en los extremos.
        /// </summary>
        static Slider BuildSlider(RectTransform holder, VolumeArt art)
        {
            var root = UISetup.CreateRect("Bar", holder);
            UISetup.Stretch(root);
            root.offsetMin = new Vector2(3f, 1f);
            root.offsetMax = new Vector2(-3f, -1f);

            var track = UISetup.CreateRect("Track", root);
            track.anchorMin = new Vector2(0f, 0.5f);
            track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 8f);
            var trackImage = SlicedImage(track.gameObject, art.Track);
            // El riel recibe el clic: pulsar en cualquier punto lleva el tirador allí.
            trackImage.raycastTarget = true;

            var fillArea = UISetup.CreateRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-4f, 8f);
            fillArea.anchoredPosition = new Vector2(-2f, 0f);
            var fill = UISetup.CreateRect("Fill", fillArea);
            UISetup.Stretch(fill);
            SlicedImage(fill.gameObject, art.Fill);

            var handleArea = UISetup.CreateRect("Handle Slide Area", root);
            UISetup.Stretch(handleArea);
            handleArea.offsetMin = new Vector2(4f, 0f);
            handleArea.offsetMax = new Vector2(-4f, 0f);
            var handle = UISetup.CreateRect("Handle", handleArea);
            // El slider la estira en vertical a toda la zona (10 px, el alto del tirador): solo cuenta el ancho.
            handle.sizeDelta = new Vector2(art.Knob != null ? art.Knob.rect.width : 8f, 0f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = art.Knob;
            handleImage.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = AudioVolumes.Default;
            var colors = slider.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.85f, 1f);
            colors.pressedColor = new Color(0.85f, 1f, 0.8f, 1f);
            slider.colors = colors;
            // Sin navegación con teclado: se controla con el ratón, como el resto de la pausa.
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }
    }
}
