using System.IO;
using Primordia.Combat;
using Primordia.Mutations;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Anillo de alcance de la Peonza (docs/multimedia-temp/floor3-spin-ring, aprobado el 2026-11-23): tras 1 s girando se abre un anillo
    /// de 2,0 u que golpea con el daño y el intervalo del giro, en los tres niveles; el clon del nivel III lleva el suyo, además de su
    /// golpe fuerte. Importa las 6 hojas de 80×80, las pone en los 3 ataques y en el prefab del clon y actualiza los textos de la Boca.
    /// </summary>
    public static class Floor3SpinRingSetup
    {
        const string Source = "docs/multimedia-temp/floor3-spin-ring/Effects";
        const string Target = "Assets/_Primordia/Art/Floor3/floor3-spin-ring/Effects";
        const string BoomerangPrefab = "Assets/_Primordia/Prefabs/Combat/Boomerang_Diplocaulus.prefab";
        const string MutationPath = "Assets/_Primordia/Data/Mutations/Floor3/Mutation_SpinningTop.asset";
        const float RingAfter = 1f;
        const float RingRadius = 2f;
        const int Cell = 80;

        internal const string BaseDescription =
            "Mantén el botón para girar como una peonza: golpea a los enemigos cercanos (2 de daño cada 0,25 s en 1,3 u) y los empuja, pero te mueves un 35 % más lento. Tras 1 s girando se abre un anillo de viento de 2 u que golpea igual. Aguanta 3 s y después te mareas 1 s, sin poder moverte ni atacar.";
        internal const string LevelTwoDescription =
            "3 de daño, radio de 1,5 u (el anillo sigue en 2 u), aguanta 4 s y devuelve los proyectiles que entran en el radio.";
        internal const string LevelThreeDescription =
            "Bumerán: si giras al menos 1 s, al soltar lanza un clon translúcido 9 u hacia el cursor que va y vuelve con 8 de daño y aturde, y lleva su propio anillo de 2 u que daña como el giro. No puedes atacar hasta que regrese.";

        [MenuItem("Primordia/Floor 3/Importar anillo de la Peonza")]
        internal static void Apply()
        {
            var jugador = Sheets("Fx_SpinningTop_Ring");
            var clon = Sheets("Fx_SpinClone_Ring");
            if (jugador.appear.Length != 4 || jugador.loop.Length != 4 || jugador.end.Length != 3
                || clon.appear.Length != 4 || clon.loop.Length != 8 || clon.end.Length != 3)
                throw new InvalidDataException("Faltan hojas del anillo de la Peonza o no tienen los cuadros aprobados (4/4/3 y 4/8/3).");

            foreach (string name in new[] { "Attack_SpinningTop", "Attack_SpinningTop_2", "Attack_SpinningTop_3" })
            {
                var attack = CombatSetup.LoadAttack(name);
                if (attack == null)
                    throw new InvalidDataException($"Falta {name}; ejecuta antes Primordia > Floor 3 > Crear Bocas del piso 3.");
                attack.ConfigureSpinRing(RingAfter, RingRadius, jugador.appear, jugador.loop, jugador.end);
                EditorUtility.SetDirty(attack);
            }

            var root = PrefabUtility.LoadPrefabContents(BoomerangPrefab);
            try
            {
                var ring = root.transform.Find("Ring");
                if (ring == null)
                {
                    var child = new GameObject("Ring");
                    child.transform.SetParent(root.transform, false);
                    ring = child.transform;
                }
                var renderer = ring.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = ring.gameObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 18;
                renderer.enabled = false;
                root.GetComponent<SpinBoomerang>().ConfigureRing(renderer, clon.appear, clon.loop, clon.end);
                PrefabUtility.SaveAsPrefabAsset(root, BoomerangPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var mutation = AssetDatabase.LoadAssetAtPath<AttackMutation>(MutationPath);
            if (mutation != null)
            {
                mutation.ConfigureBase(mutation.DisplayName, BaseDescription, MutationSlot.Mouth, mutation.Color);
                mutation.SetLevelDescriptions(LevelTwoDescription, LevelThreeDescription);
                EditorUtility.SetDirty(mutation);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3SpinRingSetup: anillo de la Peonza (2 u tras 1 s) en los 3 niveles y en el clon, y textos de la Boca actualizados.");
        }

        static (Sprite[] appear, Sprite[] loop, Sprite[] end) Sheets(string prefix) => (
            ArtSetup.ImportLooseSheet($"{Source}/{prefix}_Appear.png", $"{Target}/{prefix}_Appear.png", Cell),
            ArtSetup.ImportLooseSheet($"{Source}/{prefix}_Loop.png", $"{Target}/{prefix}_Loop.png", Cell),
            ArtSetup.ImportLooseSheet($"{Source}/{prefix}_End.png", $"{Target}/{prefix}_End.png", Cell));
    }
}
