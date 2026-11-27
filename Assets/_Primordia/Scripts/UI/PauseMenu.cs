using Primordia.Audio;
using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Menú de pausa de membrana: raíz (Reanudar, Configuración, Personaje, Bestiario, Salir) y sus submenús.
    /// Esc vuelve un paso atrás y, desde la raíz, reanuda. La criatura con sus ranuras es un objeto del prefab
    /// (también la usa la sala de cambio); el resto se dibuja por código. Lo monta Primordia > Art > Importar menú de pausa.
    /// </summary>
    public partial class PauseMenu : MemScreen
    {
        public enum Submenu
        {
            Root,
            Settings,
            Character,
            Bestiary,
            Exit,
            Confirm
        }

        [Tooltip("La criatura en su lente con las 4 ranuras (pestaña Criatura).")]
        [SerializeField] GameObject creatureZone;

        BestiaryView bestiary;
        GameManager manager;
        int rootIndex;
        FlowAction pending;

        public Submenu Current { get; private set; }

        internal void Configure(GameObject zone) => creatureZone = zone;

        void OnEnable()
        {
            Art = PauseMenuArt.Current;
            manager = GameManager.Instance;
            if (manager != null)
                manager.PauseBack = Back;
            rootIndex = 0;
            characterTab = 0;
            Open(Submenu.Root);
        }

        void OnDisable()
        {
            if (manager != null && manager.PauseBack == Back)
                manager.PauseBack = null;
            ClearPage();
            if (creatureZone != null)
                creatureZone.SetActive(false);
        }

        /// <summary>Un paso atrás. False en la raíz: ahí Esc reanuda.</summary>
        public bool Back()
        {
            if (Current == Submenu.Root)
                return false;
            Sfx.Play(Sounds.UIClick);
            Open(Current == Submenu.Confirm ? Submenu.Exit : Submenu.Root);
            return true;
        }

        public void Open(Submenu submenu)
        {
            Current = submenu;
            if (Art == null)
                return;

            if (creatureZone != null)
                creatureZone.SetActive(false);
            // La página va detrás de la zona de la criatura, para que su cuadro de información quede encima.
            ResetPage(transform);
            switch (submenu)
            {
                case Submenu.Root:
                    DrawRoot();
                    break;
                case Submenu.Settings:
                    DrawSettings();
                    break;
                case Submenu.Character:
                    DrawCharacter();
                    break;
                case Submenu.Bestiary:
                    bestiary = new BestiaryView(this, () => Back());
                    bestiary.Show(0);
                    break;
                case Submenu.Exit:
                    DrawExit();
                    break;
                case Submenu.Confirm:
                    DrawConfirm();
                    break;
            }
        }

        protected override void ChangeTab(int step)
        {
            if (Current == Submenu.Character)
                ShowTab((characterTab + step + TabNames.Length) % TabNames.Length);
            else if (Current == Submenu.Bestiary)
                bestiary?.ChangeTab(step);
        }

        // ---------- Raíz ----------

        void DrawRoot()
        {
            const float y = 72f;
            Panel(176f, y, 128f, 136f);
            TitleBanner("PAUSA", y);
            (string label, System.Action action)[] entries =
            {
                ("REANUDAR", () => { if (manager != null) manager.Resume(); }),
                ("CONFIGURACIÓN", () => Open(Submenu.Settings)),
                ("PERSONAJE", () => Open(Submenu.Character)),
                ("BESTIARIO", () => Open(Submenu.Bestiary)),
                ("SALIR", () => Open(Submenu.Exit)),
            };
            int focus = rootIndex;
            for (int i = 0; i < entries.Length; i++)
            {
                int index = i;
                CellButton(entries[i].label, 240f, y + 20f + i * 22f, 104f, () =>
                {
                    rootIndex = index;
                    entries[index].action();
                });
            }
            FocusIndex(focus);
        }

        // ---------- Configuración ----------

        void DrawSettings()
        {
            const float x = 122f, y = 74f, width = 236f, height = 128f, left = x + 16f;
            Panel(x, y, width, height);
            TitleBanner("CONFIGURACIÓN", y);
            VolumeRow(SoundGroup.Music, "MÚSICA", Art.musicIcon, left, y + 24f, width);
            VolumeRow(SoundGroup.Sfx, "EFECTOS", Art.effectsIcon, left, y + 44f, width);
            ShakeRow(left, y + 64f, width - 32f);
            CellButton("VOLVER", 240f, y + height - 28f, 104f, () => Back());
            FocusIndex(0);
        }

        void VolumeRow(SoundGroup group, string name, Sprite icon, float x, float y, float panelWidth)
        {
            const float barWidth = 92f;
            var strip = Box(Art.row, x, y, panelWidth - 32f, 14f, Image.Type.Sliced);
            Picture(icon, x + 4f, y + 1f);
            var label = Label(name, x + 20f, y + 7f, Cream, TextAnchor.MiddleLeft);
            // El porcentaje, sin el signo: no cabe junto a la barra.
            var value = Label("", x + panelWidth - 38f, y + 7f, Bright, TextAnchor.MiddleRight);

            var bar = Place(NewRect("Slider_" + group, Page), x + 84f, y + 1f, barWidth, 13f);
            var track = Box(Art.sliderTrack, 0f, 2f, barWidth, 9f, Image.Type.Sliced, bar);
            // El riel recibe el clic: pulsar en cualquier punto lleva el tirador allí.
            track.raycastTarget = true;
            var fillArea = Place(NewRect("Fill Area", bar), 0f, 2f, barWidth - 10f, 9f);
            var fill = NewRect("Fill", fillArea);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = new Vector2(10f, 0f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = Art.sliderFill;
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;

            var handleArea = Place(NewRect("Handle Slide Area", bar), 4f, 0f, barWidth - 8f, 13f);
            var handle = NewRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(13f, 0f);
            var knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = Art.sliderKnob;

            var slider = bar.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(v => value.text = Mathf.RoundToInt(v * 100f).ToString());
            var volume = bar.gameObject.AddComponent<VolumeSlider>();
            volume.Configure(group, null);
            volume.Refresh();
            value.text = Mathf.RoundToInt(slider.value * 100f).ToString();

            // Señalado, el tirador es el núcleo que late.
            UISpriteAnimation pulse = null;
            AddItem(strip.rectTransform, focus =>
            {
                label.color = focus ? Bright : Cream;
                if (focus && pulse == null)
                {
                    Animate(knob, Art.nucleus, 0.15f);
                    pulse = knob.GetComponent<UISpriteAnimation>();
                }
                if (pulse != null)
                    pulse.enabled = focus;
                if (!focus)
                    knob.sprite = Art.sliderKnob;
            }, adjust: step => slider.value = Mathf.Round(slider.value * 10f + step) / 10f);
        }

        void ShakeRow(float x, float y, float width)
        {
            var strip = Box(Art.row, x, y, width, 14f, Image.Type.Sliced);
            Picture(Art.shakeIcon, x + 4f, y + 1f);
            var label = Label("TEMBLOR", x + 20f, y + 7f, Cream, TextAnchor.MiddleLeft);
            var toggle = Picture(Art.toggleOff, x + 84f, y + 1f);
            toggle.name = "Toggle_Shake";
            var value = Label("", x + 114f, y + 7f, Dim, TextAnchor.MiddleLeft);

            void Refresh()
            {
                bool on = ScreenShake.Setting != ScreenShake.Level.Off;
                toggle.sprite = on ? Art.toggleOn : Art.toggleOff;
                value.text = on ? "SÍ" : "NO";
            }

            void Set(bool on)
            {
                ScreenShake.Setting = on ? ScreenShake.Level.Normal : ScreenShake.Level.Off;
                Refresh();
            }

            Refresh();
            AddItem(strip.rectTransform, focus => label.color = focus ? Bright : Cream,
                () => Set(ScreenShake.Setting == ScreenShake.Level.Off), step => Set(step > 0));
        }

        // ---------- Salir ----------

        void DrawExit()
        {
            const float y = 84f;
            Panel(176f, y, 128f, 92f);
            TitleBanner("SALIR", y);
            CellButton("REINICIAR", 240f, y + 20f, 104f, () => Ask(FlowAction.Restart));
            CellButton("MENÚ PRINCIPAL", 240f, y + 42f, 104f, () => Ask(FlowAction.MainMenu));
            CellButton("VOLVER", 240f, y + 64f, 104f, () => Back());
            FocusIndex(0);
        }

        void Ask(FlowAction action)
        {
            pending = action;
            Open(Submenu.Confirm);
        }

        // La confirmación sustituye al panel: encima se transparentaría el de debajo.
        void DrawConfirm()
        {
            const float y = 88f;
            Panel(142f, y, 196f, 90f);
            TitleBanner(pending == FlowAction.Restart ? "REINICIAR" : "MENÚ PRINCIPAL", y);
            Label("¿SEGURO?", 240f, y + 24f, Bright, bold: true);
            Label("PERDERÁS LA PARTIDA EN CURSO", 240f, y + 38f, Dim);
            CellButton("SÍ, SALIR", 196f, y + 56f, 80f, Confirm, danger: true);
            CellButton("NO", 284f, y + 56f, 80f, () => Back());
            // Por defecto, "No".
            FocusIndex(1);
        }

        void Confirm()
        {
            if (manager == null)
                return;
            if (pending == FlowAction.Restart)
                manager.RestartLevel();
            else
                manager.GoToMainMenu();
        }
    }
}
