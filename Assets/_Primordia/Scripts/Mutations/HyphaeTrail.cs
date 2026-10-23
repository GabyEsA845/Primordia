using Primordia.Audio;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>Deja una mancha de micelio cada cierta distancia recorrida por el jugador (Hifas).</summary>
    public class HyphaeTrail : MonoBehaviour
    {
        MyceliumPatch patchPrefab;
        MyceliumPatch.Settings settings;
        float spacing;
        Vector2 lastSpawn;

        /// <summary>Manchas creadas (para las pruebas).</summary>
        public int Spawned { get; private set; }

        public void Configure(MyceliumPatch newPatch, MyceliumPatch.Settings newSettings, float newSpacing)
        {
            patchPrefab = newPatch;
            settings = newSettings;
            spacing = Mathf.Max(0.1f, newSpacing);
            lastSpawn = transform.position;
        }

        void Update()
        {
            Vector2 position = transform.position;
            if ((position - lastSpawn).sqrMagnitude < spacing * spacing)
                return;

            lastSpawn = position;
            Spawn(position);
        }

        void Spawn(Vector2 position)
        {
            Spawned++;
            // Una mancha de cada tres cruje: con todas sería un ruido continuo.
            if (Spawned % 3 == 1)
                Sfx.Play(Sounds.HyphaeTrail);
            // Sin prefab (pruebas, o antes de importar el arte) la mancha funciona igual, sin dibujo.
            var patch = patchPrefab != null
                ? Instantiate(patchPrefab, position, Quaternion.identity)
                : new GameObject("MyceliumPatch").AddComponent<MyceliumPatch>();
            patch.transform.position = position;
            patch.Configure(settings);
        }
    }
}
