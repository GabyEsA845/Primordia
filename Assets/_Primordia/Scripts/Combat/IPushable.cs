using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>Algo que se puede empujar y aturdir (los enemigos): el empujón de las Placas mandibulares y la Guillotina.</summary>
    public interface IPushable
    {
        /// <summary>Empuja con esa velocidad y lo deja sin actuar durante el aturdimiento, con su marca si la hay.</summary>
        void Knockback(Vector2 velocity, float stunDuration, GameObject stunMarker = null);
    }
}
