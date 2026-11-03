using System.Collections;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Saco de metano (Órgano, piso 3): clic derecho, el saco se hincha, revienta y suelta una nube de gas que deriva despacio hacia
    /// donde apuntas durante 8 s. La nube explota con cualquier ataque tuyo (6 de daño en radio 2 a todo menos a ti), con el fuego y
    /// con otras explosiones; mientras llevas el saco ninguna explosión de gas te daña. Recarga de 10 s. Nivel II: dos nubes por
    /// uso, bien separadas. Nivel III (Incendio): la explosión deja fuego en el suelo 3 s que no quema al jugador. Ver <see cref="MethaneCloud"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Methane Sac", fileName = "Mutation_MethaneSac")]
    public class MethaneSacMutation : AbilityMutation
    {
        [Tooltip("Nubes por uso, por nivel (I, II, III).")]
        [SerializeField] int[] cloudCounts = { 1, 2, 2 };
        [SerializeField] MethaneCloud.Settings cloudSettings = new()
        {
            damage = 6, lifetime = 8f, speed = 0.9f, explosionRadius = 2f, fireSeconds = 0f,
        };
        [Tooltip("Incendio (nivel III): segundos que dura el fuego que deja la explosión.")]
        [SerializeField, Min(0f)] float incendioSeconds = 3f;
        [Tooltip("Ángulo entre las dos nubes del nivel II y separación inicial entre ellas.")]
        [SerializeField, Min(0f)] float fanDegrees = 36f;
        [SerializeField, Min(0f)] float fanGap = 1.6f;
        [Tooltip("Segundos desde que se abre el saco hasta que sale la nube (cuadro 5 del dibujo).")]
        [SerializeField, Min(0f)] float releaseDelay = 0.35f;
        [SerializeField] Sprite[] releaseFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] cloudFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] fadeFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] explodeFrames = System.Array.Empty<Sprite>();

        public int CloudCountAt(int level) => cloudCounts[Mathf.Clamp(level - 1, 0, cloudCounts.Length - 1)];

        public override void Apply(PlayerMutations player, int level)
        {
            base.Apply(player, level);
            // Mientras llevas el saco, las explosiones de gas no te dañan.
            player.AddBehaviour<Combat.ExplosionImmune>(Slot);
        }

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            Vector2 origin = player.transform.position;
            Vector2 aim = player.Controller.AimDirection.sqrMagnitude > 0.0001f ? player.Controller.AimDirection : Vector2.right;
            SpriteFramePlayer.PlayOnce(releaseFrames, 0.07f, origin, 22);
            var settings = cloudSettings;
            settings.fireSeconds = level >= MaxLevel ? incendioSeconds : 0f;
            player.StartCoroutine(Release(origin, aim, CloudCountAt(level), settings));
        }

        IEnumerator Release(Vector2 origin, Vector2 aim, int count, MethaneCloud.Settings settings)
        {
            if (releaseDelay > 0f)
                yield return new WaitForSeconds(releaseDelay);
            for (int i = 0; i < count; i++)
            {
                float side = count > 1 ? i - (count - 1) / 2f : 0f;
                Vector2 direction = Quaternion.Euler(0f, 0f, side * fanDegrees) * aim;
                // Salen separadas de lado y se abren aún más al avanzar.
                Vector2 start = origin + aim * 0.6f + Vector2.Perpendicular(aim) * (side * fanGap);
                MethaneCloud.Spawn(start, direction, settings, cloudFrames, fadeFrames, explodeFrames);
            }
        }

        internal void ConfigureLook(Sprite[] release, Sprite[] cloudLoop, Sprite[] fade, Sprite[] explode)
        {
            releaseFrames = release;
            cloudFrames = cloudLoop;
            fadeFrames = fade;
            explodeFrames = explode;
        }
    }
}
