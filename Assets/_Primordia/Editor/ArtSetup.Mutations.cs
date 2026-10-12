using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Piezas de las mutaciones (docs/multimedia-temp/mutations/&lt;Ranura&gt;/). Cada mutación tiene su
    /// controlador; la Piel, con los mismos estados que el jugador, porque sustituye a la célula.
    /// </summary>
    public static partial class ArtSetup
    {
        const string MutationsSourceFolder = "docs/multimedia-temp/mutations";
        const string MutationsArtFolder = "Assets/_Primordia/Art/Mutations";
        const string OldVisualsFolder = MutationPrefabsFolder + "/Visuals";

        // Ritmos: la Piel y las Extremidades, los del jugador, para ir al mismo cuadro que la célula.
        static readonly (string, float, bool)[] MouthTimings = { ("Idle", 6f, true), ("Attack", 15f, false) };
        static readonly (string, float, bool)[] LimbsTimings = { ("Idle", 6f, true), ("Move", 10f, true) };
        static readonly (string, float, bool)[] OrganTimings = { ("Idle", 6f, true), ("Activate", 12f, false) };

        /// <summary>
        /// Una pieza por mutación. Piel: radio de colisión (la célula tiene 0,4 y mide 16 px; el caparazón
        /// ~22×18 px y la piel urticante ~18 px sin las púas) y distancia del centro al borde donde va la Boca.
        /// </summary>
        static readonly (MutationSlot slot, string name, float bodyRadius, float mouthPixels)[] MutationPieces =
        {
            (MutationSlot.Mouth, "Mandibles", 0f, 0f),
            (MutationSlot.Mouth, "Stinger", 0f, 0f),
            (MutationSlot.Mouth, "Spores", 0f, 0f),
            (MutationSlot.Limbs, "Fins", 0f, 0f),
            (MutationSlot.Limbs, "JointedLegs", 0f, 0f),
            (MutationSlot.Skin, "Carapace", 0.55f, 8.5f),
            (MutationSlot.Skin, "StingingSkin", 0.45f, 7f),
            (MutationSlot.Organ, "InkSac", 0f, 0f),
            (MutationSlot.Organ, "ShockGland", 0f, 0f),
        };

        static Character MutationPiece(MutationSlot slot, string name) => new()
        {
            Name = $"{slot}_{name}",
            SourceFolder = $"{MutationsSourceFolder}/{slot}",
            ArtFolder = $"{MutationsArtFolder}/{name}",
            DefaultState = slot is MutationSlot.Skin or MutationSlot.Limbs ? "Idle_down" : "Idle",
            // La Boca se une al cuerpo por el borde izquierdo, a media altura, y gira desde ahí.
            Pivot = slot == MutationSlot.Mouth ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
            Timings = slot switch
            {
                MutationSlot.Skin => PlayerArt.Timings,
                MutationSlot.Mouth => MouthTimings,
                MutationSlot.Limbs => LimbsTimings,
                _ => OrganTimings,
            },
        };

        [MenuItem("Primordia/Art/Importar piezas de mutación")]
        static void ImportMutations()
        {
            int imported = 0;
            foreach (var (slot, name, bodyRadius, mouthPixels) in MutationPieces)
            {
                var art = Import(MutationPiece(slot, name));
                if (art == null)
                    continue;

                var mutation = AssetDatabase.LoadAssetAtPath<MutationData>($"{MutationDataFolder}/Mutation_{name}.asset");
                if (mutation == null)
                {
                    Debug.LogWarning($"ArtSetup: no existe Mutation_{name}; créalo con Primordia > Mutations.");
                    continue;
                }

                mutation.SetLook(art.Controller, bodyRadius, mouthPixels / PixelsPerUnit);
                EditorUtility.SetDirty(mutation);
                imported++;
            }

            // El mordisco ya se ve en la Boca: Mandíbulas no lleva el efecto aparte de versiones anteriores.
            MutationSetup.ConfigureMandiblesBite(CombatSetup.LoadAttack("Attack_Mandibles"));

            AssetDatabase.SaveAssets();
            ApplyMutationLookToPlayer();

            // Los rectángulos provisionales ya no los usa nadie.
            if (AssetDatabase.IsValidFolder(OldVisualsFolder))
                AssetDatabase.DeleteAsset(OldVisualsFolder);

            AssetDatabase.SaveAssets();
            Debug.Log($"ArtSetup: piezas de {imported} mutaciones puestas al jugador.");
        }

        /// <summary>
        /// Añade al jugador las tres piezas (vacías hasta que se equipa algo) y <see cref="MutationLook"/>.
        /// Orden de dibujo: Extremidades 8, Boca detrás 9, cuerpo 10, Órgano 11, Boca 12.
        /// </summary>
        static void ApplyMutationLookToPlayer()
        {
            EditPrefab(PlayerSetup.PlayerPath, root =>
            {
                var aimPivot = root.transform.Find("AimPivot");
                if (aimPivot == null)
                {
                    Debug.LogWarning("ArtSetup: el jugador no tiene AimPivot; no se añaden las piezas.");
                    return;
                }

                if (!root.TryGetComponent<PlayerAnimator>(out _))
                {
                    Debug.LogWarning("ArtSetup: el jugador no tiene PlayerAnimator; importa antes sus animaciones.");
                    return;
                }

                var limbs = MutationPart(root.transform, "Limbs", sortingOrder: 8, Vector3.zero);
                var organ = MutationPart(root.transform, "Organ", sortingOrder: 11, Vector3.zero);
                var mouth = MutationPart(aimPivot, "MouthPiece", sortingOrder: 12, new Vector3(0.375f, 0f, 0f));

                SetFields(GetOrAdd<MutationLook>(root), ("limbs", limbs), ("organ", organ), ("mouth", mouth));

                // Las piezas son de colores reales: un destello blanco no se notaría, así que se tiñen de rojo.
                if (root.TryGetComponent<DamageFlash>(out var flash))
                {
                    var serialized = new SerializedObject(flash);
                    serialized.FindProperty("flashColor").colorValue = new Color(1f, 0.35f, 0.35f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            });
        }

        static Animator MutationPart(Transform parent, string name, int sortingOrder, Vector3 position)
        {
            var part = parent.Find(name);
            if (part == null)
            {
                part = new GameObject(name).transform;
                part.SetParent(parent, false);
            }

            part.localPosition = position;
            part.localRotation = Quaternion.identity;
            part.localScale = Vector3.one;

            var renderer = GetOrAdd<SpriteRenderer>(part.gameObject);
            renderer.sprite = null;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;

            var animator = GetOrAdd<Animator>(part.gameObject);
            animator.runtimeAnimatorController = null;
            return animator;
        }
    }
}
