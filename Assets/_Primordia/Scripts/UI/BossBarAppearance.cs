using UnityEngine;

namespace Primordia.UI
{
    /// <summary>Barra de vida propia de este jefe (<see cref="BossBarSkin"/>). Sin este componente usa la barra lisa.</summary>
    public class BossBarAppearance : MonoBehaviour
    {
        [SerializeField] BossBarSkin skin;

        public BossBarSkin Skin => skin;

        internal void Configure(BossBarSkin newSkin) => skin = newSkin;
    }
}
