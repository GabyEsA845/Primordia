using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Escenario fijo de la sala de la Meganeura (paso 7d, «sala viva»): 3 charcas visibles desde el principio (las mismas donde pone
    /// los huevos) y 5 Sigillarias quemadas repartidas por la sala, que serán los pararrayos de la fase 3. Posiciones respecto al
    /// centro de la sala; ninguna pisa el centro (picados y combate), las puertas ni el Lepidodendron partido (+8, +3).
    /// </summary>
    public partial class MeganeuraBoss
    {
        static readonly Vector2[] SigillariaOffsets = { new(-15f, 9f), new(-16f, -5f), new(-4f, -10f), new(14f, -7f), new(5f, 11f) };

        bool sceneryBuilt;

        void BuildScenery(Transform parent)
        {
            if (sceneryBuilt)
                return;
            sceneryBuilt = true;
            EnsurePuddles();

            if (effects.sigillaria.Length == 0)
                return;
            foreach (var offset in SigillariaOffsets)
            {
                var tree = new GameObject("Sigillaria");
                tree.transform.SetParent(parent, true);
                tree.transform.position = ArenaCenter + offset;
                var renderer = tree.AddComponent<SpriteRenderer>();
                renderer.sprite = effects.sigillaria[0];
                renderer.sortingOrder = 4;
                // Tronco sólido de ≈ 1,6 × 0,8 u al pie.
                var collider = tree.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.6f, 0.8f);
                collider.offset = new Vector2(0f, 0.4f);
                tree.AddComponent<BossSigillaria>();
            }
        }

        /// <summary>El metano solo cuenta en la fase 1: al empezar la 2 se disipa (la lluvia de la 3 no tendría con qué prender).</summary>
        void ClearMethane()
        {
            foreach (var sac in FindObjectsByType<MethaneSac>())
                Destroy(sac.gameObject);
            foreach (var mist in FindObjectsByType<MethaneMist>())
                Destroy(mist.gameObject);
        }
    }
}
