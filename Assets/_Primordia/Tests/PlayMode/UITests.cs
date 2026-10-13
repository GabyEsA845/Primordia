using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using Primordia.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class UITests
    {
        readonly List<Object> created = new();
        GameObject root;
        GameManager manager;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
            if (manager != null)
                Object.DestroyImmediate(manager.gameObject);
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
            Time.timeScale = 1f;
            Cursor.visible = true;
        }

        [TestCase(6, 0, HeartsDisplay.Fill.Full)]
        [TestCase(6, 2, HeartsDisplay.Fill.Full)]
        [TestCase(5, 2, HeartsDisplay.Fill.Half)]
        [TestCase(4, 2, HeartsDisplay.Fill.Empty)]
        [TestCase(0, 0, HeartsDisplay.Fill.Empty)]
        public void HeartFill_MatchesHalfHeartPoints(int health, int index, HeartsDisplay.Fill expected)
        {
            Assert.AreEqual(expected, HeartsDisplay.HeartFill(health, index));
        }

        [UnityTest]
        public IEnumerator Hearts_FollowThePlayerHealth()
        {
            root = new GameObject("UI");
            var health = root.AddComponent<Health>();
            var full = CreateSprite();
            var half = CreateSprite();
            var empty = CreateSprite();

            var display = new GameObject("Hearts").AddComponent<HeartsDisplay>();
            display.transform.SetParent(root.transform);
            display.Configure(health, full, half, empty);
            yield return null; // Start

            Assert.AreEqual(3, display.Hearts.Count);
            Assert.IsTrue(display.Hearts.All(h => h.sprite == full));

            health.TakeDamage(3);

            Assert.AreSame(full, display.Hearts[0].sprite);
            Assert.AreSame(half, display.Hearts[1].sprite);
            Assert.AreSame(empty, display.Hearts[2].sprite);

            health.SetMax(8);

            Assert.AreEqual(4, display.Hearts.Count(h => h.gameObject.activeSelf));
        }

        [UnityTest]
        public IEnumerator GameUI_ShowsThePanelForEachState()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
            root = new GameObject("UI");
            var pause = CreatePanel("Pause");
            var defeat = CreatePanel("Defeat");
            var victory = CreatePanel("Victory");
            var gameUI = root.AddComponent<GameUI>();
            gameUI.Configure(pause, defeat, victory, delay: 0.2f);
            yield return null; // Start

            Assert.IsFalse(pause.activeSelf || defeat.activeSelf || victory.activeSelf);

            manager.SetState(GameState.Paused);
            Assert.IsTrue(pause.activeSelf);

            manager.SetState(GameState.Playing);
            Assert.IsFalse(pause.activeSelf);

            manager.SetState(GameState.Defeat);
            Assert.IsFalse(defeat.activeSelf, "La derrota espera un momento antes de mostrarse.");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(defeat.activeSelf);

            manager.SetState(GameState.Victory);
            Assert.IsTrue(victory.activeSelf);
            Assert.IsFalse(defeat.activeSelf);
        }

        [Test]
        public void Resume_OnlyLeavesPause()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();

            manager.SetState(GameState.Paused);
            manager.Resume();
            Assert.AreEqual(GameState.Playing, manager.State);

            manager.SetState(GameState.Defeat);
            manager.Resume();
            Assert.AreEqual(GameState.Defeat, manager.State);
        }

        [UnityTest]
        public IEnumerator MutationChoice_IgnoresClicks_RightAfterOpening()
        {
            root = new GameObject("UI");
            root.SetActive(false);
            var panel = CreatePanel("ChoicePanel");
            var skip = new GameObject("Skip").AddComponent<Button>();
            skip.transform.SetParent(root.transform);
            var choice = root.AddComponent<MutationChoiceUI>();
            choice.Configure(panel, new MutationCard[0], skip, newInputLockDuration: 0.3f);
            root.SetActive(true);

            var mutation = ScriptableObject.CreateInstance<StatMutation>();
            try
            {
                choice.Show(new[] { mutation }, null);

                Assert.IsTrue(choice.IsLocked);
                Assert.IsFalse(skip.interactable);
                choice.Skip();
                Assert.IsTrue(choice.IsOpen, "Un clic durante el bloqueo no cuenta.");

                yield return new WaitForSecondsRealtime(0.4f);

                Assert.IsFalse(choice.IsLocked);
                Assert.IsTrue(skip.interactable);
                choice.Skip();
                Assert.IsFalse(choice.IsOpen);
            }
            finally
            {
                Object.DestroyImmediate(mutation);
            }
        }

        [UnityTest]
        public IEnumerator MutationSlots_ShowIconsOrEmptySilhouettes_AndPopOnChange()
        {
            var mutations = CreatePlayer();
            var empty = new[] { CreateSprite(), CreateSprite(), CreateSprite(), CreateSprite() };
            root = new GameObject("UI");
            var display = root.AddComponent<MutationSlotsDisplay>();
            display.Configure(mutations);
            display.ConfigureLook(CreateSprite(), empty);
            yield return null; // Start

            Assert.AreSame(empty[0], display.IconOf(MutationSlot.Mouth).sprite);
            Assert.AreSame(empty[1], display.IconOf(MutationSlot.Limbs).sprite);

            var fins = Track(ScriptableObject.CreateInstance<StatMutation>());
            fins.ConfigureBase("Aletas", "", MutationSlot.Limbs, Lineage.Predator, Color.white);
            var icon = CreateSprite();
            fins.SetIcons(icon, null);
            mutations.Equip(fins);

            Assert.AreSame(icon, display.IconOf(MutationSlot.Limbs).sprite);
            yield return null;
            Assert.Greater(display.SlotOf(MutationSlot.Limbs).localScale.x, 1f, "Salta al cambiar.");
            Assert.AreEqual(1f, display.SlotOf(MutationSlot.Mouth).localScale.x);

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(1f, display.SlotOf(MutationSlot.Limbs).localScale.x);

            mutations.Unequip(MutationSlot.Limbs);
            Assert.AreSame(empty[1], display.IconOf(MutationSlot.Limbs).sprite);
        }

        [UnityTest]
        public IEnumerator OrganSlot_IsShaded_WhileTheAbilityRecharges()
        {
            var mutations = CreatePlayer();
            root = new GameObject("UI");
            var display = root.AddComponent<MutationSlotsDisplay>();
            display.Configure(mutations);
            yield return null; // Start

            var ink = Track(ScriptableObject.CreateInstance<InkCloudMutation>());
            ink.ConfigureBase("Tinta", "", MutationSlot.Organ, Lineage.Toxic, Color.white);
            ink.ConfigureAbility(newCooldown: 1f, newEffect: null);
            ink.SetIcons(CreateSprite(), null);
            mutations.Equip(ink);
            yield return null;

            var shade = display.ShadeOf(MutationSlot.Organ);
            Assert.IsFalse(shade.enabled, "Lista: sin sombra.");

            Assert.IsTrue(mutations.Ability.TryUse());
            yield return null;
            Assert.IsTrue(shade.enabled);
            Assert.Greater(shade.fillAmount, 0.5f, "Recién usada, casi todo oscuro.");

            yield return new WaitForSeconds(1.1f);
            Assert.IsFalse(shade.enabled);
        }

        [UnityTest]
        public IEnumerator Crosshair_ReplacesTheCursor_OnlyWhilePlaying_AndContractsWhenFiring()
        {
            manager = new GameObject("GameManager").AddComponent<GameManager>();
            var mutations = CreatePlayer();
            var weapon = mutations.GetComponent<Weapon>();
            var bite = Track(ScriptableObject.CreateInstance<AttackData>());
            bite.ConfigureMelee(range: 1f, arc: 90f, effect: null);
            weapon.Attack = bite;

            root = new GameObject("UI", typeof(RectTransform));
            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshairObject.transform.SetParent(root.transform, false);
            var crosshair = crosshairObject.AddComponent<Crosshair>();
            crosshair.readPointer = false;
            crosshair.Configure(weapon);
            var idle = CreateSprite();
            var fire = new[] { CreateSprite(), CreateSprite() };
            crosshair.ConfigureLook(idle, fire, 0.2f);
            yield return null; // Start
            yield return null;

            Assert.IsTrue(crosshair.IsShown);
            Assert.IsFalse(Cursor.visible);
            Assert.AreSame(idle, crosshair.CurrentSprite);

            weapon.Fire(Vector2.right);
            yield return null;
            Assert.IsTrue(crosshair.IsFiring);
            Assert.Contains(crosshair.CurrentSprite, fire);

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreSame(idle, crosshair.CurrentSprite);

            manager.SetState(GameState.Paused);
            yield return null;
            Assert.IsFalse(crosshair.IsShown);
            Assert.IsTrue(Cursor.visible, "En la pausa vuelve el cursor normal.");

            manager.SetState(GameState.Playing);
            yield return null;
            Assert.IsTrue(crosshair.IsShown);

            Object.DestroyImmediate(root);
            Assert.IsTrue(Cursor.visible, "Al quitar la mira vuelve el cursor.");
        }

        [TestCase(7f, "7")]
        [TestCase(5.5f, "5,5")]
        [TestCase(7.8f, "7,8")]
        [TestCase(6.04f, "6")]
        public void Stats_FormatDecimalsWithComma(float value, string expected)
        {
            Assert.AreEqual(expected, PlayerStatsPanel.FormatDecimal(value));
        }

        [UnityTest]
        public IEnumerator Stats_ShowValues_AndArrowsAgainstTheBaseCell()
        {
            var mutations = CreatePlayer();
            yield return null; // Awake y Start del jugador

            root = new GameObject("Stats");
            root.SetActive(false);
            var values = new Text[3];
            var arrows = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                values[i] = new GameObject($"Value{i}").AddComponent<Text>();
                values[i].transform.SetParent(root.transform);
                arrows[i] = new GameObject($"Arrow{i}").AddComponent<Image>();
                arrows[i].transform.SetParent(root.transform);
            }
            var up = CreateSprite();
            var stats = root.AddComponent<PlayerStatsPanel>();
            stats.Configure(values, arrows, up, CreateSprite());

            var fins = Track(ScriptableObject.CreateInstance<StatMutation>());
            fins.ConfigureBase("Aletas", "", MutationSlot.Limbs, Lineage.Predator, Color.white);
            fins.Configure(speedMultiplier: 1.5f, dodgeMultiplier: 1f, healthBonus: 0);
            mutations.Equip(fins);
            mutations.Health.TakeDamage(1);

            stats.Refresh(mutations);

            Assert.AreEqual("5/6", stats.ValueText(PlayerStatsPanel.Stat.Health).text);
            Assert.IsFalse(stats.Arrow(PlayerStatsPanel.Stat.Health).enabled, "La vida máxima es la de la célula.");
            Assert.AreEqual(PlayerStatsPanel.FormatDecimal(mutations.BaseMoveSpeed * 1.5f), stats.ValueText(PlayerStatsPanel.Stat.Speed).text);
            Assert.IsTrue(stats.Arrow(PlayerStatsPanel.Stat.Speed).enabled);
            Assert.AreSame(up, stats.Arrow(PlayerStatsPanel.Stat.Speed).sprite);
        }

        [UnityTest]
        public IEnumerator CreaturePanel_DrawsALinePerSlot_AndOpensTheTooltipOnHover()
        {
            var mutations = CreatePlayer();
            yield return null;

            var fins = Track(ScriptableObject.CreateInstance<StatMutation>());
            fins.ConfigureBase("Aletas", "Más velocidad.", MutationSlot.Limbs, Lineage.Predator, Color.red);
            fins.SetIcons(CreateSprite(), null);
            mutations.Equip(fins);

            root = new GameObject("Creature", typeof(RectTransform));
            root.SetActive(false);
            var zone = (RectTransform)root.transform;
            zone.sizeDelta = new Vector2(170f, 164f);

            var portrait = UIChild("Portrait", zone).gameObject.AddComponent<CreaturePortrait>();
            var lines = UIChild("Lines", zone);
            var dots = UIChild("Dots", zone);

            var empty = CreateSprite();
            var positions = new[] { new Vector2(-75f, 72f), new Vector2(-75f, -72f), new Vector2(75f, 72f), new Vector2(75f, -72f) };
            var callouts = new SlotCallout[4];
            for (int i = 0; i < 4; i++)
            {
                var box = UIChild($"Slot{i}", zone);
                box.sizeDelta = new Vector2(20f, 20f);
                box.anchoredPosition = positions[i];
                var frame = box.gameObject.AddComponent<Image>();
                var icon = UIChild("Icon", box).gameObject.AddComponent<Image>();
                callouts[i] = box.gameObject.AddComponent<SlotCallout>();
                callouts[i].Configure((MutationSlot)i, frame, icon);
                callouts[i].ConfigureLook(null, null, empty);
            }

            var tooltipRect = UIChild("Tooltip", zone);
            Text TooltipText(string name) => UIChild(name, tooltipRect).gameObject.AddComponent<Text>();
            var tooltip = tooltipRect.gameObject.AddComponent<MutationTooltip>();
            tooltip.Configure(UIChild("Stripe", tooltipRect).gameObject.AddComponent<Image>(), TooltipText("Title"),
                UIChild("Lineage", tooltipRect).gameObject.AddComponent<Image>(), TooltipText("Details"), TooltipText("Description"),
                UIChild("Pointer", tooltipRect));

            var panel = root.AddComponent<CreaturePanel>();
            panel.Configure(portrait, callouts, tooltip, lines, dots);
            root.SetActive(true);
            panel.Refresh(mutations);

            Assert.AreSame(fins.Icon, panel.CalloutOf(MutationSlot.Limbs).Icon.sprite);
            Assert.AreSame(empty, panel.CalloutOf(MutationSlot.Mouth).Icon.sprite);
            // Boca y Órgano: 3 tramos por su pasillo; Piel y Extremidades: 2.
            Assert.AreEqual(10, panel.Lines.Count(l => l.gameObject.activeSelf));
            Assert.IsFalse(tooltip.IsShown);

            panel.Hover(panel.CalloutOf(MutationSlot.Limbs));
            Assert.IsTrue(tooltip.IsShown);
            Assert.IsTrue(panel.CalloutOf(MutationSlot.Limbs).IsHighlighted);
            Assert.AreEqual("Aletas", tooltip.Title.text);
            Assert.AreEqual("Extremidades · Depredador · Nivel I", tooltip.Details.text);
            Assert.AreEqual("Más velocidad.", tooltip.Description.text);

            panel.Hover(panel.CalloutOf(MutationSlot.Mouth));
            Assert.IsFalse(panel.CalloutOf(MutationSlot.Limbs).IsHighlighted);
            Assert.AreEqual("Boca", tooltip.Title.text);
            Assert.AreEqual("Ranura libre", tooltip.Details.text);

            panel.Unhover(panel.CalloutOf(MutationSlot.Mouth));
            Assert.IsFalse(tooltip.IsShown);
        }

        [UnityTest]
        public IEnumerator SpriteAnimation_PlaysOnce_AndDestroysItselfWhenAsked()
        {
            root = new GameObject("UI");
            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(root.transform);
            var frames = new[] { CreateSprite(), CreateSprite() };
            var animation = bubble.AddComponent<UISpriteAnimation>();
            animation.Configure(frames, 0.05f, newLoop: false, newDestroyWhenDone: true);

            Assert.AreSame(frames[0], bubble.GetComponent<Image>().sprite);
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsTrue(bubble == null, "Se destruye al acabar.");
        }

        static RectTransform UIChild(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        PlayerMutations CreatePlayer()
        {
            var player = Track(new GameObject("Player"));
            player.AddComponent<Weapon>();
            player.AddComponent<PlayerController>().readInput = false;
            var mutations = player.AddComponent<PlayerMutations>();
            player.AddComponent<PlayerAbility>().readInput = false;
            return mutations;
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        GameObject CreatePanel(string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(root.transform);
            panel.SetActive(false);
            return panel;
        }

        static Sprite CreateSprite() =>
            Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
    }
}
