using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Piel anfibia (Piel, piso 3, defensiva): mientras estás de pie en agua, lodo o un charco (incluido el de veneno) se carga un
    /// moco. Cargado, eres inmune al fuego, a las explosiones de gas y al veneno de las plantas; un golpe de un enemigo rompe el moco
    /// (y te hace daño). Nivel I: 3 s de carga. Nivel II: 2 s y +20 % de velocidad. Nivel III (Bloqueo): con el moco puesto, el
    /// primer daño que recibas, del tipo que sea, no te hace daño: solo rompe el moco y hay que volver a cargarlo. Funciona sin
    /// Escupitajo. Ver <see cref="AmphibianMucus"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Amphibian Skin", fileName = "Mutation_AmphibianSkin")]
    public class AmphibianSkinMutation : MutationData
    {
        [Tooltip("Segundos en el agua hasta cargar el moco, por nivel (I, II, III).")]
        [SerializeField] float[] chargeSeconds = { 3f, 2f, 2f };
        [Tooltip("Velocidad extra por nivel, en fracción de la base (0,2 = +20 %).")]
        [SerializeField] float[] speedBonus = { 0f, 0.2f, 0.2f };
        [SerializeField] Sprite[] chargeFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] shieldFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] absorbFrames = System.Array.Empty<Sprite>();
        [Tooltip("Nivel III: el moco que bloquea (destella).")]
        [SerializeField] Sprite[] blockFrames = System.Array.Empty<Sprite>();

        static float At(float[] values, int level) =>
            values == null || values.Length == 0 ? 0f : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        public float ChargeSecondsAt(int level) => At(chargeSeconds, level) > 0f ? At(chargeSeconds, level) : 3f;
        public float SpeedBonusAt(int level) => At(speedBonus, level);

        public override void Apply(PlayerMutations player, int level)
        {
            player.Controller.MoveSpeed += player.BaseMoveSpeed * SpeedBonusAt(level);
            player.AddBehaviour<AmphibianMucus>(Slot).Configure(player.Controller, player.Health, ChargeSecondsAt(level),
                level >= MaxLevel, chargeFrames, shieldFrames, absorbFrames, blockFrames);
        }

        public override void Remove(PlayerMutations player, int level) =>
            player.Controller.MoveSpeed -= player.BaseMoveSpeed * SpeedBonusAt(level);

        internal void ConfigureLook(Sprite[] charge, Sprite[] shield, Sprite[] absorb, Sprite[] block)
        {
            chargeFrames = charge;
            shieldFrames = shield;
            absorbFrames = absorb;
            blockFrames = block;
        }
    }
}
