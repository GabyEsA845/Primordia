using Primordia.Audio;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Estela urticante (Medusa): mientras esquiva, el jugador deja un rastro de tentáculos que envenena a los
    /// enemigos que lo pisan (manchas de <see cref="MyceliumPatch"/> sin daño ni freno, con veneno).
    /// </summary>
    public class StingingTrailTouch : MonoBehaviour
    {
        PlayerController controller;
        MyceliumPatch patchPrefab;
        MyceliumPatch.Settings settings;
        float spacing = 0.5f;
        Vector2 lastSpawn;
        bool wasDodging;

        /// <summary>Manchas creadas (para las pruebas).</summary>
        public int Spawned { get; private set; }

        public void Configure(PlayerController newController, MyceliumPatch newPatch, MyceliumPatch.Settings newSettings, float newSpacing)
        {
            controller = newController;
            patchPrefab = newPatch;
            settings = newSettings;
            spacing = Mathf.Max(0.1f, newSpacing);
        }

        void Update()
        {
            if (controller == null)
                return;

            Vector2 position = transform.position;
            if (!controller.IsDodging)
            {
                wasDodging = false;
                return;
            }

            // La primera mancha sale donde empieza la esquiva.
            if (!wasDodging || (position - lastSpawn).sqrMagnitude >= spacing * spacing)
                Spawn(position);
            wasDodging = true;
        }

        void Spawn(Vector2 position)
        {
            lastSpawn = position;
            Spawned++;
            Sfx.Play(Sounds.LineageStingingTrail);
            var patch = patchPrefab != null
                ? Instantiate(patchPrefab, position, Quaternion.identity)
                : new GameObject("StingingTrail").AddComponent<MyceliumPatch>();
            patch.transform.position = position;
            patch.Configure(settings);
        }
    }
}
