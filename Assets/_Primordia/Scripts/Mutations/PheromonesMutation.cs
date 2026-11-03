using Primordia.Core;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Feromonas (Órgano, piso 3): clic derecho, lanzas un velo de feromonas hasta 5 u hacia el cursor; los enemigos que quedan en radio
    /// 2 se atacan entre sí durante 3 s (4 s en el nivel II). Recarga de 14 s. Los jefes no se confunden. Nivel III (Frenesí): el daño
    /// entre los confundidos es doble. Ver <see cref="Confusion"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Mutations/Pheromones", fileName = "Mutation_Pheromones")]
    public class PheromonesMutation : AbilityMutation
    {
        [Tooltip("Segundos de confusión, por nivel (I, II, III).")]
        [SerializeField] float[] seconds = { 3f, 4f, 4f };
        [SerializeField, Min(0.5f)] float radius = 2f;
        [Tooltip("Hasta dónde llega el lanzamiento hacia el cursor.")]
        [SerializeField, Min(0f)] float throwRange = 5f;
        [SerializeField] Sprite[] releaseFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] auraFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] markFrames = System.Array.Empty<Sprite>();
        [SerializeField] Sprite[] frenzyFrames = System.Array.Empty<Sprite>();

        public float SecondsAt(int level) =>
            seconds == null || seconds.Length == 0 ? 3f : seconds[Mathf.Clamp(level - 1, 0, seconds.Length - 1)];

        public float Radius => radius;

        public override void Activate(PlayerMutations player)
        {
            int level = Mathf.Max(1, player.LevelOf(Slot));
            Vector2 origin = player.transform.position;
            Vector2 offset = Vector2.ClampMagnitude(player.Controller.AimOffset, throwRange);
            Vector2 center = origin + offset;
            // Una pared corta el lanzamiento antes de atravesarla.
            if (offset.sqrMagnitude > 0.01f)
            {
                var hit = Physics2D.Raycast(origin, offset.normalized, offset.magnitude, LayerMask.GetMask("Default"));
                if (hit.collider != null && !hit.collider.isTrigger)
                    center = origin + offset.normalized * Mathf.Max(0f, hit.distance - 0.3f);
            }

            // El dibujo mide 72 px (4,5 u de diámetro): se escala para que su radio máximo sea el del área.
            SpriteFramePlayer.PlayOnce(releaseFrames, 0.07f, center, 20, radius / 2.25f);
            Confuse(center, SecondsAt(level), level >= MaxLevel);
        }

        /// <summary>Confunde a los enemigos en radio de ese punto. Devuelve a cuántos.</summary>
        public int Confuse(Vector2 center, float confusionSeconds, bool frenzy)
        {
            int count = 0;
            var seen = new System.Collections.Generic.HashSet<Health>();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(Combat.CombatLayers.Enemy));
            var hits = new System.Collections.Generic.List<Collider2D>();
            Physics2D.OverlapCircle(center, radius, filter, hits);
            foreach (var hit in hits)
            {
                var victim = hit.GetComponentInParent<Health>();
                if (victim == null || !seen.Add(victim))
                    continue;
                if (Confusion.Apply(victim, confusionSeconds, frenzy, auraFrames, markFrames, frenzyFrames) != null)
                    count++;
            }
            return count;
        }

        internal void ConfigureLook(Sprite[] release, Sprite[] aura, Sprite[] mark, Sprite[] frenzy)
        {
            releaseFrames = release;
            auraFrames = aura;
            markFrames = mark;
            frenzyFrames = frenzy;
        }
    }
}
