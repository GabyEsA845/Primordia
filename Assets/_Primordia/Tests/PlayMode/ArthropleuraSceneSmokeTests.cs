#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Primordia.Enemies;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>Recorre las fases del jefe en la escena de prueba real (barra, cámara, jugador...): cualquier error de consola falla la prueba.</summary>
    public class ArthropleuraSceneSmokeTests
    {
        [UnityTest, Explicit("Recorre todas las fases en la escena real (~50 s); lanzar a mano para cazar errores de consola.")]
        public IEnumerator Scene_RunsEveryPhaseWithoutConsoleErrors()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Primordia/Scenes/Test_BossArthropleura.unity", new LoadSceneParameters(LoadSceneMode.Single));
            ArthropleuraBoss boss = null;
            for (float t = 0f; t < 10f && boss == null; t += Time.unscaledDeltaTime)
            {
                boss = Object.FindAnyObjectByType<ArthropleuraBoss>(FindObjectsInactive.Include);
                yield return null;
            }
            Assert.IsNotNull(boss, "La escena de prueba debería tener al jefe.");
            // El jefe nace con su sala: se entra a ella teletransportando al jugador dentro (la sala se activa por su disparador).
            var player = Object.FindAnyObjectByType<Primordia.Player.PlayerController>();
            Assert.IsNotNull(player, "Falta el jugador.");
            Vector3 inside = boss.transform.position + Vector3.left * 22f;
            if (player.TryGetComponent<Rigidbody2D>(out var body)) body.position = inside;
            player.transform.position = inside;
            for (float t = 0f; t < 10f && !boss.isActiveAndEnabled; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSeconds(1f);
            boss.ForcePresentation();
            for (float t = 0f; t < 25f && boss.IntroStage != ArthropleuraBoss.EntryStage.Complete; t += Time.deltaTime)
            {
                if (boss.IntroStage == ArthropleuraBoss.EntryStage.Roaring) boss.BeginFight();
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            boss.DebugPhase2(0); // 69 %
            yield return new WaitForSeconds(5f);
            Assert.IsTrue(boss.TransitionStarted, "F2 empieza.");
            boss.DebugPhase2(2);
            yield return new WaitForSeconds(2f);
            boss.DebugPhase2(3); // 34 %
            yield return new WaitForSeconds(5f);
            Assert.IsTrue(boss.Transition3Started, "F3 empieza.");
            boss.DebugPhase2(5);
            yield return new WaitForSeconds(3f);
            boss.DebugPhase2(6); // 10 %: rueda
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(boss.WheelActive, "La rueda aparece.");
            yield return new WaitForSeconds(8f);
            for (int i = 0; i < 6 && boss != null; i++)
            {
                boss.DebugPhase2(7);
                yield return new WaitForSeconds(0.7f);
            }
            yield return new WaitForSeconds(5f);
            Assert.IsNotNull(Object.FindAnyObjectByType<ArthropleuraCorpse>(), "Queda el cadáver.");
        }
    }
}
#endif
