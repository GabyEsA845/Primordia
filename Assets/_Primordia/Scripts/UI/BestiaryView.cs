using System;
using System.Linq;
using Primordia.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// La pantalla del bestiario (pausa y menú principal): pestañas por piso y jefes, cuadrícula de retratos y la
    /// ficha de la criatura señalada. Lo nunca eliminado se ve como silueta "???".
    /// </summary>
    public class BestiaryView
    {
        const float X = 58f, Y = 54f, Width = 364f, Height = 196f, Step = 34f;
        const int Columns = 5;
        static readonly string[] TabNames = { "PISO 1", "PISO 2", "PISO 3", "JEFES" };

        readonly MemScreen screen;
        readonly Action back;
        RectTransform card;

        public int Tab { get; private set; }
        public BestiaryEntry Shown { get; private set; }

        internal BestiaryView(MemScreen screen, Action back)
        {
            this.screen = screen;
            this.back = back;
        }

        /// <summary>Con casilla: todas menos las secretas sin encontrar.</summary>
        internal static BestiaryEntry[] Visible(PauseMenuArt art) =>
            art.entries.Where(e => !e.secret || BestiaryProgress.IsDiscovered(e.id)).ToArray();

        internal void ChangeTab(int step) => Show((Tab + step + TabNames.Length) % TabNames.Length);

        /// <summary>Cambia de pestaña rehaciendo la página.</summary>
        internal void Show(int tab)
        {
            screen.ResetPage(screen.transform, screen.transform.childCount);
            Draw(tab);
        }

        /// <summary>Dibuja la pestaña en una página recién vaciada de la pantalla.</summary>
        internal void Draw(int tab)
        {
            Tab = tab;
            card = null;
            Shown = null;
            var all = Visible(screen.Art);
            var shown = all.Where(e => (int)e.tab == tab).ToArray();

            screen.Panel(X, Y, Width, Height);
            screen.TitleBanner($"BESTIARIO  {all.Count(e => BestiaryProgress.IsDiscovered(e.id))}/{all.Length}", 17f);
            screen.Tabs(TabNames, screen.Art.bestiaryIcons, tab, X, Y, Width, Show);

            float gridX = X + 12f, gridY = Y + 26f;
            screen.Label($"DESCUBIERTOS {shown.Count(e => BestiaryProgress.IsDiscovered(e.id))}/{shown.Length}",
                gridX + 2f, Y + 16f, MemScreen.Heading, TextAnchor.MiddleLeft);

            var creatures = shown.Where(e => !e.trap).ToArray();
            var traps = shown.Where(e => e.trap).ToArray();
            MemItem first = null;
            for (int i = 0; i < creatures.Length; i++)
            {
                var item = Slot(creatures[i], gridX + i % Columns * Step, gridY + i / Columns * Step);
                if (first == null)
                    first = item;
            }
            if (traps.Length > 0)
            {
                float trapY = gridY + (creatures.Length + Columns - 1) / Columns * Step + 18f;
                screen.Label("TRAMPAS Y PLANTAS", gridX + 2f, trapY - 8f, MemScreen.Dim, TextAnchor.MiddleLeft);
                for (int i = 0; i < traps.Length; i++)
                    Slot(traps[i], gridX + i % Columns * Step, trapY + i / Columns * Step);
            }

            screen.BackHint(X, Y, Width, Height, back);
            screen.Focus(first);
        }

        MemItem Slot(BestiaryEntry entry, float x, float y)
        {
            var art = screen.Art;
            bool found = BestiaryProgress.IsDiscovered(entry.id);
            var frame = screen.Picture(found ? art.beastSlot : art.beastSlotLocked, x, y);
            frame.name = "Beast_" + entry.id;
            screen.Picture(found ? entry.portrait : entry.lockedPortrait, x + 5f, y + 5f);
            return screen.AddItem(frame.rectTransform, focus =>
            {
                frame.sprite = focus ? art.beastSlotHighlighted : found ? art.beastSlot : art.beastSlotLocked;
                if (focus)
                    DrawCard(entry);
            });
        }

        void DrawCard(BestiaryEntry entry)
        {
            Shown = entry;
            if (card != null)
                MemScreen.Remove(card.gameObject);
            card = MemScreen.NewRect("Card", screen.Page);
            MemScreen.Place(card, 0f, 0f, 480f, 270f);

            var art = screen.Art;
            bool found = BestiaryProgress.IsDiscovered(entry.id);
            float x = X + 188f, vx = x + 33f, vy = Y + 8f;
            screen.Picture(art.vacuole, vx, vy, card);
            Creature(entry, found, vx + 48f, vy + 50f);
            screen.Picture(art.vacuoleGlint, vx, vy, card);

            screen.Label(found ? entry.displayName : "???", x + 81f, vy + 102f, MemScreen.Bright, bold: true, parent: card);
            var notes = found ? entry.notes : new[] { "???" };
            for (int i = 0; i < notes.Length; i++)
                screen.Label(notes[i], x + 81f, vy + 113f + i * 10f, MemScreen.Dim, parent: card);

            var stats = new (string name, string value)[]
            {
                ("VIDA", entry.data != null ? entry.data.MaxHealth.ToString() : "—"),
                ("DAÑO", entry.damage),
                ("VELOC.", entry.speed),
                ("ELIMINADOS", BestiaryProgress.Kills(entry.id).ToString()),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                // "MUY RÁPIDA" no cabe junto a su nombre en una franja de 80.
                string value = stats[i].value == "MUY RÁPIDA" ? "MUY RÁP." : stats[i].value;
                float rx = x + i % 2 * 82f, ry = vy + 143f + i / 2 * 16f;
                screen.Box(art.row, rx, ry, 80f, 14f, Image.Type.Sliced, card);
                screen.Label(stats[i].name, rx + 3f, ry + 7f, MemScreen.Cream, TextAnchor.MiddleLeft, parent: card);
                screen.Label(found ? value : "?", rx + 77f, ry + 7f, MemScreen.Bright, TextAnchor.MiddleRight, parent: card);
            }
        }

        /// <summary>El sprite animado a la mayor escala entera que quepa en 64 px; si no cabe, el retrato a 3x.</summary>
        void Creature(BestiaryEntry entry, bool found, float centerX, float centerY)
        {
            float largest = Mathf.Max(entry.contentSize.x, entry.contentSize.y);
            bool animated = found && entry.frames.Length > 0 && largest > 0f && largest <= 64f;
            var sprite = animated ? entry.frames[0] : found ? entry.portrait : entry.lockedPortrait;
            if (sprite == null)
                return;

            int scale = animated ? Mathf.Clamp(Mathf.FloorToInt(64f / largest), 1, 3) : 3;
            Vector2 size = sprite.rect.size * scale;
            Vector2 offset = animated ? entry.contentOffset * scale : Vector2.zero;
            var image = screen.Box(sprite, centerX - size.x / 2f - offset.x, centerY - size.y / 2f + offset.y,
                size.x, size.y, Image.Type.Simple, card);
            image.name = "Creature";
            if (animated)
                MemScreen.Animate(image, entry.frames, 0.14f);
        }
    }
}
