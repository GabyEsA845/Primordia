using Primordia.Mutations;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Estadísticas actuales del jugador en la pausa: una fila por <see cref="Stat"/> con su valor y una flecha
    /// si una mutación lo ha subido o bajado respecto a la célula base. Se actualiza cada vez que se muestra.
    /// Para añadir una estadística: un valor nuevo en <see cref="Stat"/>, su cálculo en <see cref="Read"/>
    /// y su fila en el prefab (Primordia > Art > Importar menús).
    /// </summary>
    public class PlayerStatsPanel : MonoBehaviour
    {
        public enum Stat
        {
            Health,
            Damage,
            Speed
        }

        [Tooltip("Texto del valor de cada estadística, en el orden de Stat.")]
        [SerializeField] Text[] values = new Text[3];
        [Tooltip("Flecha de cada estadística, en el orden de Stat. Se oculta si el valor es el de la célula base.")]
        [SerializeField] Image[] arrows = new Image[3];
        [SerializeField] Sprite upSprite;
        [SerializeField] Sprite downSprite;

        void OnEnable()
        {
            var player = GameObject.FindWithTag("Player");
            Refresh(player != null ? player.GetComponent<PlayerMutations>() : null);
        }

        internal void Configure(Text[] newValues, Image[] newArrows, Sprite up, Sprite down)
        {
            values = newValues;
            arrows = newArrows;
            upSprite = up;
            downSprite = down;
        }

        internal Text ValueText(Stat stat) => (int)stat < values.Length ? values[(int)stat] : null;
        internal Image Arrow(Stat stat) => (int)stat < arrows.Length ? arrows[(int)stat] : null;

        internal void Refresh(PlayerMutations player)
        {
            foreach (Stat stat in System.Enum.GetValues(typeof(Stat)))
            {
                var (text, trend) = player != null ? Read(player, stat) : ("-", 0);

                var value = ValueText(stat);
                if (value != null)
                    value.text = text;

                var arrow = Arrow(stat);
                if (arrow != null)
                {
                    arrow.sprite = trend > 0 ? upSprite : trend < 0 ? downSprite : null;
                    arrow.enabled = arrow.sprite != null;
                }
            }

            RefreshUnstable(player);
        }

        static readonly Color UnstableColor = new(1f, 0.85f, 0.38f);
        UnstableRow unstableRow;

        /// <summary>
        /// Con mutaciones especiales, una fila más bajo la vida que solo dice "Inestable"; al pasar el ratón, el cuadro
        /// de información dice cuánto daño extra recibes por golpe.
        /// </summary>
        void RefreshUnstable(PlayerMutations player)
        {
            int extra = player != null ? player.Health.ExtraHitDamage : 0;
            if (unstableRow == null)
            {
                if (extra == 0)
                    return;
                unstableRow = CreateUnstableRow();
                if (unstableRow == null)
                    return;
            }

            unstableRow.gameObject.SetActive(extra > 0);
            unstableRow.ExtraHalves = extra;
        }

        /// <summary>Copia la fila de la vida (sin icono, valor ni flecha) justo debajo de ella, con el nombre en dorado.</summary>
        UnstableRow CreateUnstableRow()
        {
            var healthValue = ValueText(Stat.Health);
            if (healthValue == null)
                return null;

            var source = healthValue.transform.parent;
            var row = Instantiate(source.gameObject, source.parent);
            row.name = "Row_Unstable";
            row.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
            // Sin la veta de fondo, para que no rompa la alternancia de las filas, pero recibe el ratón.
            if (!row.TryGetComponent<Image>(out var background))
                background = row.AddComponent<Image>();
            background.sprite = null;
            background.color = Color.clear;
            background.enabled = true;
            background.raycastTarget = true;

            int healthIndex = healthValue.transform.GetSiblingIndex();
            foreach (Transform child in row.transform)
            {
                if (child.TryGetComponent<Text>(out var text))
                {
                    text.color = UnstableColor;
                    text.raycastTarget = false;
                    text.text = child.GetSiblingIndex() == healthIndex ? "" : "Inestable";
                }
                else if (child.TryGetComponent<Image>(out var image))
                {
                    image.enabled = false;
                }
            }

            var unstable = row.AddComponent<UnstableRow>();
            // El cuadro de información de la pausa, el mismo de las ranuras y del linaje.
            var tooltip = transform.parent != null ? transform.parent.GetComponentInChildren<MutationTooltip>(true) : null;
            unstable.Configure(tooltip, UnstableColor);
            return unstable;
        }

        /// <summary>Texto del valor y si está por encima (1), por debajo (−1) o igual (0) que en la célula base.</summary>
        static (string text, int trend) Read(PlayerMutations player, Stat stat)
        {
            switch (stat)
            {
                case Stat.Health:
                    return ($"{player.Health.Current}/{player.Health.Max}", player.Health.Max.CompareTo(player.BaseMaxHealth));

                case Stat.Damage:
                    int damage = player.Weapon.Attack != null ? player.Weapon.Attack.Damage : 0;
                    int baseDamage = player.DefaultAttack != null ? player.DefaultAttack.Damage : damage;
                    return (damage.ToString(), damage.CompareTo(baseDamage));

                case Stat.Speed:
                    float speed = player.Controller.MoveSpeed;
                    return (FormatDecimal(speed), Trend(speed, player.BaseMoveSpeed));

                default:
                    return ("-", 0);
            }
        }

        /// <summary>Un decimal como mucho, con coma: 7, 5,5.</summary>
        internal static string FormatDecimal(float value) =>
            (Mathf.Round(value * 10f) / 10f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

        static int Trend(float value, float baseValue) => Mathf.Abs(value - baseValue) < 0.01f ? 0 : value > baseValue ? 1 : -1;
    }
}
