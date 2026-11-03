using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Escamas carbonizadas (Piel, piso 3, ofensiva): inmune al fuego y a las explosiones de gas; tras pisar fuego, tus ataques queman
    /// a los enemigos durante 4 s. Nivel II: también inmune al veneno de las plantas y, al recibir un golpe, estallas en llamas (Brasa:
    /// radio 1,5, 4 de daño, prende lo inflamable; recarga de 5 s). Nivel III (Llamarada): todos tus ataques prenden fuego siempre, a
    /// los enemigos y al escenario (helechos, maleza, metano, charcos de veneno) por donde pasan y donde impactan. Ver
    /// <see cref="CharredScales"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Charred Scales", fileName = "Mutation_CharredScales")]
    public class CharredScalesMutation : MutationData
    {
        [Tooltip("Segundos que tus ataques queman tras pisar fuego.")]
        [SerializeField, Min(0.5f)] float primedSeconds = 4f;
        [Tooltip("Brasa (nivel II): daño, radio y recarga del estallido al recibir un golpe.")]
        [SerializeField, Min(0)] int blazeDamage = 4;
        [SerializeField, Min(0.1f)] float blazeRadius = 1.5f;
        [SerializeField, Min(0f)] float blazeCooldown = 5f;
        [SerializeField] Sprite[] primedFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] blazeFrames = System.Array.Empty<Sprite>();

        public override void Apply(PlayerMutations player, int level)
        {
            player.AddBehaviour<CharredScales>(Slot).Configure(player.Controller, player.Health, player.Weapon, level >= 2, level >= 2,
                level >= MaxLevel, primedSeconds, blazeDamage, blazeRadius, blazeCooldown, primedFrames, blazeFrames);
        }

        internal void ConfigureLook(Sprite[] primed, Sprite[] blaze)
        {
            primedFrames = primed;
            blazeFrames = blaze;
        }
    }
}
