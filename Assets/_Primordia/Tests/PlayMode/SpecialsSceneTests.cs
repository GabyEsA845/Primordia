#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Primordia.Mutations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>La escena de prueba de las especiales del ámbar: lo que hacen los atajos 1/2/3 (equipar y subir de nivel).</summary>
    public class SpecialsSceneTests
    {
        const string ScenePath = "Assets/_Primordia/Scenes/Test_Floor3Specials.unity";

        [UnityTest]
        public IEnumerator Shortcuts_EquipEachSpecial_AndRepeatingRaisesTheLevel()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new UnityEngine.SceneManagement.LoadSceneParameters());
            yield return new WaitForSeconds(1.5f);

            var keys = Object.FindFirstObjectByType<SpecialsTestKeys>();
            Assert.IsNotNull(keys, "La escena no tiene SpecialsTestKeys.");
            var mutations = Object.FindFirstObjectByType<PlayerMutations>();
            Assert.IsNotNull(mutations, "No hay jugador.");
            var playerField = typeof(SpecialsTestKeys).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(playerField.GetValue(keys), "El script no ha encontrado al jugador (¿etiqueta Player?).");

            var pick = typeof(SpecialsTestKeys).GetMethod("Pick", BindingFlags.NonPublic | BindingFlags.Instance);
            pick.Invoke(keys, new object[] { 0 });
            Assert.AreEqual(1, mutations.LevelOf(MutationSlot.Limbs), "1 equipa los Cercos a nivel I.");
            pick.Invoke(keys, new object[] { 0 });
            Assert.AreEqual(2, mutations.LevelOf(MutationSlot.Limbs), "Repetir sube a II.");
            pick.Invoke(keys, new object[] { 0 });
            Assert.AreEqual(3, mutations.LevelOf(MutationSlot.Limbs));
            pick.Invoke(keys, new object[] { 1 });
            Assert.AreEqual("Mutation_AmberCapsule", mutations.Get(MutationSlot.Skin).name);
            pick.Invoke(keys, new object[] { 2 });
            Assert.AreEqual("Mutation_AmberGland", mutations.Get(MutationSlot.Organ).name);
        }
    }
}
#endif
