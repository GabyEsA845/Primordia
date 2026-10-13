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
