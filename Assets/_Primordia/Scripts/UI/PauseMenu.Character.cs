using System.Collections.Generic;
using Primordia.Biomass;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>Personaje: Criatura, Estado, Objetos y Partida.</summary>
    public partial class PauseMenu
    {
        const float TabX = 58f, TabY = 54f, TabWidth = 364f, TabHeight = 196f;
        const float ContentX = TabX + 22f, ContentY = TabY + 24f;
        static readonly string[] TabNames = { "CRIATURA", "ESTADO", "OBJETOS", "PARTIDA" };
        static readonly string[] FloorNames = { "SOPA", "MAR", "PANTANO" };

        int characterTab;

        public int CharacterTab => characterTab;

        public void ShowTab(int tab)
        {
            characterTab = tab;
            Open(Submenu.Character);
        }

        void DrawCharacter()
        {
            Panel(TabX, TabY, TabWidth, TabHeight);
            TitleBanner("PERSONAJE", 17f);
            Tabs(TabNames, Art.characterIcons, characterTab, TabX, TabY, TabWidth, ShowTab);
            BackHint(TabX, TabY, TabWidth, TabHeight, () => Back());

            var player = GameObject.FindWithTag("Player");
            switch (characterTab)
            {
                case 0:
                    DrawCreature(player);
                    break;
                case 1:
                    DrawState(player);
                    break;
                case 2:
                    DrawItems(player);
                    break;
                default:
                    DrawRun();
                    break;
            }
        }

        // ---------- Criatura ----------

        void DrawCreature(GameObject player)
        {
            if (creatureZone != null)
            {
                creatureZone.SetActive(true);
                if (creatureZone.TryGetComponent<CreaturePanel>(out var panel))
                {
                    foreach (var callout in creatureZone.GetComponentsInChildren<SlotCallout>(true))
                    {
                        var slot = callout;
                        AddItem(slot.Rect, focus =>
                        {
                            if (focus)
                                panel.Hover(slot);
                            else if (slot.IsHighlighted)
                                panel.Unhover(slot);
                        });
                    }
                }
            }

            const float center = TabX + 288f;
            var mutations = player != null ? player.GetComponent<PlayerMutations>() : null;
            var mouth = mutations != null ? mutations.Get(MutationSlot.Mouth) : null;
            Label("BOCA", center, TabY + 30f, Heading);
            Picture(Art.slot, center - 13f, TabY + 37f);
            if (mouth != null && mouth.Icon != null)
                Box(mouth.Icon, center - 8f, TabY + 42f, 16f, 16f, Image.Type.Simple);
            Label(mouth != null ? mouth.DisplayName.ToUpperInvariant() : "BOCA BÁSICA", center, TabY + 74f, Cream);
            if (mouth == null)
                return;
            int level = Mathf.Max(1, mutations.LevelOf(MutationSlot.Mouth));
            Label($"NIVEL {MutationText.LevelName(level)}", center, TabY + 86f, Dim);
            Label(mouth.Summary.ToUpperInvariant(), center, TabY + 106f, Dim, TextAnchor.UpperCenter, wrapWidth: 112f);
        }

        // ---------- Estado ----------

        void DrawState(GameObject player)
        {
            Label("VIDA", ContentX, ContentY + 7f, Heading, TextAnchor.MiddleLeft);
            var mutations = player != null ? player.GetComponent<PlayerMutations>() : null;
            if (mutations == null)
                return;

            DrawHearts(mutations.Health, mutations.BaseMaxHealth, ContentX + 34f, ContentY);
            DrawStats(mutations);
            DrawLineage(player.GetComponent<PlayerLineage>(), ContentX + 176f);
        }

        /// <summary>Un corazón cada 2 de vida; los ganados por encima de la vida base, en ámbar, con "+N EXTRA".</summary>
        void DrawHearts(Health health, int baseMax, float x, float y)
        {
            int count = (health.Max + 1) / 2;
            int normal = Mathf.Min(count, (baseMax + 1) / 2);
            // Con muchos corazones se juntan para no salirse del panel.
            float step = Mathf.Min(15f, Mathf.Floor(220f / Mathf.Max(1, count)));
            for (int i = 0; i < count; i++)
            {
                int halves = Mathf.Clamp(health.Current - i * 2, 0, 2);
                bool extra = i >= normal;
                Sprite sprite;
                if (halves == 2)
                    sprite = extra ? null : Art.heartFull;
                else if (halves == 1)
                    sprite = extra ? Art.heartExtraHalf : Art.heartHalf;
                else
                    sprite = extra ? Art.heartExtraEmpty : Art.heartEmpty;
                var heart = Box(sprite, x + i * step, y - 1f, 16f, 16f, Image.Type.Simple);
                heart.name = extra ? "HeartExtra" : "Heart";
                if (extra && halves == 2)
                    Animate(heart, Art.heartExtraFull, 0.15f);
            }
            if (count > normal)
                Label($"+{count - normal} EXTRA", x + (count - 1) * step + 22f, y + 7f, Amber, TextAnchor.MiddleLeft);
        }

        void DrawStats(PlayerMutations mutations)
        {
            const float width = 150f;
            float y = ContentY + 26f;
            Row(ContentX, y, width, Art.damageIcon, "DAÑO", null);
            Row(ContentX, y + 18f, width, Art.speedIcon, "VELOCIDAD", null);

            // La vida ya se ve en los corazones: su texto queda oculto.
            var holder = NewRect("Stats", Page);
            var health = Label("", 0f, 0f, Bright, parent: holder);
            var values = new[]
            {
                health,
                Label("", ContentX + width - 5f, y + 7f, Bright, TextAnchor.MiddleRight),
                Label("", ContentX + width - 5f, y + 25f, Bright, TextAnchor.MiddleRight),
            };
            var arrows = new[]
            {
                null,
                Box(null, ContentX + width - 36f, y + 3f, 7f, 7f, Image.Type.Simple),
                Box(null, ContentX + width - 36f, y + 21f, 7f, 7f, Image.Type.Simple),
            };
            var stats = holder.gameObject.AddComponent<PlayerStatsPanel>();
            stats.Configure(values, arrows, Art.upArrow, Art.downArrow);
            stats.Refresh(mutations);
            health.enabled = false;

            int extra = mutations.Health.ExtraHitDamage;
            if (extra <= 0)
                return;
            string amount = extra % 2 == 0 ? $"+{extra / 2}" : $"+{extra}/2";
            var row = Row(ContentX, y + 36f, width, Art.unstableIcon, "INESTABLE", amount, Amber);
            row.name = "Row_Unstable";
            float centerY = y + 43f;
            AddItem(row.rectTransform, focus =>
            {
                if (focus)
                    ShowInfo(ContentX + width + 10f, centerY, "INESTABLE", "MUTACIONES ESPECIALES", Amber,
                        $"Recibes {PlayerStatsPanel.Hearts(extra)} de daño extra por cada golpe (no con el veneno ni las caídas).");
                else
                    HideInfo();
            });
        }

        void DrawLineage(PlayerLineage lineage, float x)
        {
            Label("LINAJE", x, ContentY + 33f, Heading, TextAnchor.MiddleLeft);
            var area = Place(NewRect("Lineage", Page), x - 4f, ContentY + 42f, 150f, 108f);
            var title = Label("", x, ContentY + 50f, Bright, TextAnchor.UpperLeft, wrapWidth: 146f);
            var benefit = Label("", x, ContentY + 72f, Dim, TextAnchor.UpperLeft, wrapWidth: 146f);
            var panel = area.gameObject.AddComponent<LineagePanel>();
            panel.Configure(lineage, title, benefit);
            panel.Refresh();
            // Con dos linajes completos, pulsar la fila cambia al otro.
            if (panel.CanSwitch)
                AddItem(area, focus => benefit.color = focus ? Bright : Dim, panel.Switch);
        }

        // ---------- Objetos ----------

        void DrawItems(GameObject player)
        {
            const float width = 190f;
            float y = ContentY;
            y = ItemGroup("RELIQUIAS", "RELIQUIA", RunInventory.Relics, y, width);
            y = ItemGroup("OBJETOS CLAVE", "OBJETO CLAVE", RunInventory.KeyItems, y + 8f, width);

            var biomass = player != null ? player.GetComponent<PlayerBiomass>() : null;
            Label("BIOMASA", ContentX, y + 12f, Heading, TextAnchor.MiddleLeft);
            Row(ContentX, y + 20f, width, Art.biomassIcon, "BIOMASA", biomass != null ? biomass.Amount.ToString() : "0");
        }

        float ItemGroup(string title, string kind, IReadOnlyList<RunInventory.Entry> entries, float y, float width)
        {
            Label(title, ContentX, y + 4f, Heading, TextAnchor.MiddleLeft);
            y += 12f;
            if (entries.Count == 0)
            {
                Row(ContentX, y, width, null, "NADA TODAVÍA", null, Faint);
                return y + 16f;
            }

            foreach (var entry in entries)
            {
                var row = Row(ContentX, y, width, entry.Icon16, entry.Name.ToUpperInvariant(), null);
                row.name = "Item_" + entry.Id;
                var shown = entry;
                float centerY = y + 7f;
                AddItem(row.rectTransform, focus =>
                {
                    if (focus)
                        ShowInfo(ContentX + width + 10f, centerY, shown.Name.ToUpperInvariant(), kind, Heading, shown.Effect);
                    else
                        HideInfo();
                });
                y += 16f;
            }
            return y;
        }

        // ---------- Partida ----------

        void DrawRun()
        {
            const float width = 220f;
            int floor = FloorGenerator.CurrentFloor;
            string floorName = floor >= 1 && floor <= FloorNames.Length ? $"{floor}  {FloorNames[floor - 1]}"
                : floor > 0 ? floor.ToString() : "-";
            int seed = FloorGenerator.CurrentSeed;
            var rows = new (Sprite icon, string name, string value)[]
            {
                (Art.floorIcon, "PISO", floorName),
                (Art.seedIcon, "SEMILLA", seed != 0 ? seed.ToString() : "-"),
                (Art.timeIcon, "TIEMPO", RunStats.FormatTime(RunStats.PlayTime)),
                (Art.enemiesIcon, "ENEMIGOS", RunStats.EnemiesDefeated.ToString()),
                (Art.bossesIcon, "JEFES", RunStats.BossesDefeated.ToString()),
            };
            for (int i = 0; i < rows.Length; i++)
                Row(ContentX, ContentY + i * 18f, width, rows[i].icon, rows[i].name, rows[i].value).name = "Run_" + rows[i].name;
        }
    }
}
