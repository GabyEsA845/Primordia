using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>Algo en el jugador que lo hace inmune al veneno de las plantas (las Escamas carbonizadas del nivel II).</summary>
    public interface IPoisonImmune
    {
        bool ImmuneToPlantPoison { get; }
    }

    /// <summary>
    /// Inmunidad a TODO el veneno (el Ciempiés): además del de las plantas, el de los ataques y las púas
    /// (<see cref="Poison"/> no se aplica).
    /// </summary>
    public interface IFullPoisonImmune : IPoisonImmune
    {
    }

    /// <summary>
    /// Comprueba si el jugador no sufre el veneno de las plantas (los charcos de los bulbos y la nube tóxica). Lo consulta
    /// <see cref="Primordia.Rooms.PlayerToxin"/>.
    /// </summary>
    public static class PoisonImmune
    {
        public static bool Has(Component target)
        {
            if (target == null)
                return false;
            foreach (var immune in target.GetComponentsInParent<IPoisonImmune>())
            {
                if (immune.ImmuneToPlantPoison)
                    return true;
            }
            return false;
        }
    }
}
