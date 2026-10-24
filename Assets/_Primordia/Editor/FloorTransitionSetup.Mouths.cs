using System.IO;
using Primordia.Mutations;
using Primordia.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.Editor
{
    /// <summary>
    /// Elección de Boca de la sala de paso: las Bocas del piso 2 (datos, icono y criatura de la vista previa) y la ventana
    /// <see cref="MouthChoiceUI"/> dentro del prefab GameUI.
    /// </summary>
    public static partial class FloorTransitionSetup
    {
        const string Floor2MutationsFolder = MutationsFolder + "/Floor2";
        internal const string NextFloorName = "océano cámbrico";

        // Medidas de la ventana, en píxeles de la interfaz (480×270).
        const float CardWidth = 140f;
        const float CardHeight = 165f;
        const float CreatureSize = 72f;   // la criatura de 24 px, a ×3
        static readonly Color OverlayColor = new(0.01f, 0.03f, 0.08f, 0.62f);
        static readonly Color NoteColor = new(1f, 0.8f, 0.5f);

        /// <summary>
        /// Las Bocas del piso 2. Su ataque es provisional (el de Mandíbulas y el del Aguijón) hasta feature/floor-2: de
        /// momento, tras elegirlas llega la victoria. No están en la reserva del piso 1, así que allí no salen.
        /// </summary>
        static MutationData[] CreateFloor2Mouths()
        {
            Directory.CreateDirectory(Floor2MutationsFolder);
            var color = LineageSetup.SlotColors[(int)MutationSlot.Mouth];

            var cnidocytes = CreateMouth("Mutation_Cnidocytes", "Cnidocitos",
                "Arpones urticantes que atraviesan a un enemigo y lo envenenan.",
                WeaponFamily.Projectile, CombatSetup.LoadAttack("Attack_Stinger"), color);

            var jawPlates = CreateMouth("Mutation_JawPlates", "Placas mandibulares",
                "Mordisco de guillotina en un arco ancho (5 de daño). El enemigo mordido recibe un 25 % más de daño durante 2 s.",
                WeaponFamily.Melee, CombatSetup.LoadAttack("Attack_Mandibles"), color);
            jawPlates.SetLevelDescriptions("Daño 5 → 6 y Quebrar dura 3 s.",
                "Guillotina: cada cuarto mordisco es doble, hace el triple de daño y aturde 0,5 s.");
            EditorUtility.SetDirty(jawPlates);

            var art = ArtSetup.ImportFloor2MouthArt();
            ApplyArt(cnidocytes, art.Cnidocytes);
            ApplyArt(jawPlates, art.JawPlates);
            AssetDatabase.SaveAssets();
            return new[] { cnidocytes, jawPlates };
        }

        static AttackMutation CreateMouth(string assetName, string displayName, string description, WeaponFamily family,
            Combat.AttackData placeholderAttack, Color color)
        {
            string path = $"{Floor2MutationsFolder}/{assetName}.asset";
            var mutation = AssetDatabase.LoadAssetAtPath<AttackMutation>(path);
            if (mutation == null)
            {
                mutation = ScriptableObject.CreateInstance<AttackMutation>();
                mutation.ConfigureBase(displayName, description, MutationSlot.Mouth, color);
                mutation.Configure(placeholderAttack, newHealEveryKills: 0);
                AssetDatabase.CreateAsset(mutation, path);
            }

            mutation.SetFloor(2, family);
            EditorUtility.SetDirty(mutation);
            return mutation;
        }

        static void ApplyArt(MutationData mutation, ArtSetup.MouthArt art)
        {
            if (art == null)
                return;
            if (art.Icon != null)
                mutation.SetIcons(art.Icon, art.LargeIcon);
            mutation.SetCreatureFrames(art.Idle, art.Attack);
            mutation.SetCreature(art.Body, art.Morph, art.Reveal);
            EditorUtility.SetDirty(mutation);
        }

        /// <summary>Añade la ventana al prefab GameUI si no la tiene, y le pone la piel de las tarjetas.</summary>
        static void EnsureMouthChoice()
        {
            var root = PrefabUtility.LoadPrefabContents(UISetup.GameUIPath);
            try
            {
                if (root.GetComponent<MouthChoiceUI>() == null)
                    BuildMouthChoice(root);
                PrefabUtility.SaveAsPrefabAsset(root, UISetup.GameUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            ArtSetup.ApplyMouthChoiceSkin();
        }

        static void BuildMouthChoice(GameObject canvas)
        {
            var panel = UISetup.CreateRect("MouthChoicePanel", canvas.transform);
            UISetup.Stretch(panel);
            panel.SetAsLastSibling();
            // Translúcido: detrás se ve la criatura congelada sobre la fosa.
            panel.gameObject.AddComponent<Image>().color = OverlayColor;

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 5f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var title = UISetup.CreateText(panel, "ELIGE TU BOCA", 20, UISetup.TitleColor);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            var subtitle = UISetup.CreateText(panel, $"Así llegarás al {NextFloorName}", 9, UISetup.TextColor);

            var row = UISetup.CreateRect("Cards", panel);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 16f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

            // Tres huecos: el piso 2 tendrá una tercera Boca (la de área) más adelante.
            var cards = new MouthCard[3];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = BuildMouthCard(row);

            var note = UISetup.CreateText(panel, "", 9, NoteColor);
            note.gameObject.name = "Note";

            var slots = UISetup.CreateRect("Slots", panel);
            var slotsLayout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.childAlignment = TextAnchor.MiddleCenter;
            slotsLayout.spacing = 14f;
            slotsLayout.childControlWidth = slotsLayout.childControlHeight = true;
            slotsLayout.childForceExpandWidth = slotsLayout.childForceExpandHeight = false;
            slots.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            var icons = new Image[4];
            for (int i = 0; i < icons.Length; i++)
                icons[i] = UISetup.CreateIcon($"Slot_{(MutationSlot)i}", slots, 16f);

            canvas.AddComponent<MouthChoiceUI>().Configure(panel.gameObject, subtitle, note, cards, slots, icons);
            panel.gameObject.SetActive(false);
        }

        static MouthCard BuildMouthCard(Transform parent)
        {
            var card = UISetup.CreateRect("MouthCard", parent);
            var image = card.gameObject.AddComponent<Image>();
            card.gameObject.AddComponent<Button>().targetGraphic = image;

            var element = card.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var strip = UISetup.CreateRect("ColorStrip", card).gameObject.AddComponent<Image>();
            strip.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

            var creature = UISetup.CreateIcon("Creature", card, CreatureSize);
            creature.preserveAspect = true;
            creature.enabled = true;

            var nameRow = UISetup.CreateRow("Name", card, card.childCount, TextAnchor.MiddleCenter);
            var icon = UISetup.CreateIcon("Icon", nameRow, 16f);
            var title = UISetup.CreateText(nameRow, "Boca", 12, UISetup.TitleColor);

            var family = UISetup.CreateText(card, "Familia", 9, NoteColor);
            var description = UISetup.CreateText(card, "Descripción", 9, UISetup.TextColor);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.alignment = TextAnchor.UpperCenter;
            description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var mouthCard = card.gameObject.AddComponent<MouthCard>();
            mouthCard.Configure(creature, icon, strip, title, family, description);
            return mouthCard;
        }
    }
}
