#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Primordia.Core;
using Primordia.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    public class ArthropleuraBossTests
    {
        const string PrefabPath = "Assets/_Primordia/Prefabs/Enemies/Boss_Arthropleura.prefab";
        GameObject room;
        ArthropleuraBoss boss;

        [SetUp]
        public void SetUp()
        {
            room = new GameObject("SalaArthropleura");
            room.transform.position = new Vector2(300f, 100f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Ejecuta Primordia > Boss > Crear Arthropleura y escena de prueba.");
            boss = Object.Instantiate(prefab, room.transform).GetComponent<ArthropleuraBoss>();
            boss.SetArena(room.transform, new Vector2(ArthropleuraBoss.ArenaWidth, ArthropleuraBoss.ArenaHeight));
        }

        [TearDown]
        public void TearDown() => Object.Destroy(room);

        [UnityTest]
        public IEnumerator Chain_HasThirtyPiecesWithSprites()
        {
            yield return null;
            Assert.AreEqual(ArthropleuraBoss.SegmentCount + 2, boss.PieceCount);
            var renderers = boss.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(boss.PieceCount, renderers.Length);
            Assert.IsTrue(renderers.All(r => r.sprite != null), "Alguna pieza no tiene sprite.");
            Assert.AreEqual(600, boss.Health.Max);
        }

        [UnityTest]
        public IEnumerator Chain_DrawsTailBehindHead()
        {
            yield return null;
            var renderers = boss.GetComponentsInChildren<SpriteRenderer>();
            Assert.Greater(renderers[0].sortingOrder, renderers[renderers.Length - 1].sortingOrder);
        }

        [UnityTest]
        public IEnumerator Chain_KeepsItsPitchWhileMoving()
        {
            for (int i = 0; i < 60; i++)
                yield return null;
            for (int i = 0; i < boss.PieceCount - 1; i++)
            {
                float gap = (boss.NodeGround(i) - boss.NodeGround(i + 1)).magnitude;
                Assert.That(gap, Is.InRange(1.2f, ArthropleuraBoss.Pitch + 0.05f), $"Nudo {i}");
            }
        }

        [Test]
        public void Phases_FollowTheThresholds()
        {
            Assert.AreEqual(0, boss.PhaseIndex);
            Assert.IsTrue(boss.Health.IsInvulnerable, "El caparazón es invulnerable.");
            CollectionAssert.AreEqual(new[] { 0.7f, 0.35f, 0.1f }, boss.PhaseThresholds);
        }
    }
}
#endif
