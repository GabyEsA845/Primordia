using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Enemigos del piso 2. Trilobite: al ver al jugador avisa y embiste en línea recta; si choca, se da la vuelta y queda
    /// aturdido; su cabeza blindada para la mitad del daño de frente, pero no panza arriba. Medusa azul: si te ve, avisa
    /// quieta y suelta un anillo de 8 con un hueco que gira de un anillo al siguiente; si no te ve, no dispara.
    /// </summary>
    public class Floor2EnemiesTests
    {
        readonly List<Object> created = new();
        GameObject player;

        [SetUp]
        public void SetUp()
        {
            player = Track(new GameObject("Player") { tag = "Player" });
            player.layer = LayerMask.NameToLayer(CombatLayers.Player);
            player.transform.position = new Vector2(6f, 0f);
            player.AddComponent<Health>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        EnemyData Data(int health, float detection = 10f)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: health, moveSpeed: 3f, detectionRange: detection, contactDamage: 1);
            return data;
        }

        T CreateEnemy<T>(EnemyData data, Vector2 position) where T : EnemyBrain
        {
            var go = Track(new GameObject("Enemy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            var enemy = go.AddComponent<Enemy>();
            go.AddComponent<EnemyAI>().Target = player.transform;
            var brain = go.AddComponent<T>();
            enemy.SetData(data);
            go.GetComponent<Rigidbody2D>().gravityScale = 0f;
            return brain;
        }

        [UnityTest]
        public IEnumerator Trilobite_Warns_ThenChargesStraightAtYou()
        {
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(14), Vector2.zero);
            yield return null;
            yield return null;

            Assert.AreEqual(TrilobiteBrain.Phase.Warn, trilobite.Current, "Al verte, avisa.");
            Assert.Less(trilobite.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 0.01f, "Quieto mientras avisa.");

            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(TrilobiteBrain.Phase.Charge, trilobite.Current);
            Assert.Greater(trilobite.GetComponent<Rigidbody2D>().linearVelocity.x, 5f, "Embiste muy rápido hacia ti.");
        }

        [UnityTest]
        public IEnumerator Trilobite_HittingAWall_FlipsAndIsStunned()
        {
            player.transform.position = new Vector2(0f, 6f);
            var wall = Track(new GameObject("Wall"));
            wall.transform.position = new Vector2(0f, 3f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(6f, 1f);
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(14, detection: 12f), Vector2.zero);
            // Ve al jugador por encima de la pared (sin rejilla de navegación, la vista no la tapa).
            yield return new WaitForSeconds(1.2f);

            Assert.IsTrue(trilobite.Current is TrilobiteBrain.Phase.Flip or TrilobiteBrain.Phase.Stunned,
                $"Al chocar se da la vuelta y queda aturdido (está en {trilobite.Current}).");
        }

        [UnityTest]
        public IEnumerator Trilobite_Armor_HalvesFrontDamage_ButNotFromBehind()
        {
            var trilobite = CreateEnemy<TrilobiteBrain>(Data(100), Vector2.zero);
            var health = trilobite.GetComponent<Health>();
            yield return null;
            yield return null;

            // Avisando mira al jugador: el golpe le entra de frente.
            Assert.IsTrue(trilobite.Shields(player.transform.position));
            int before = health.Current;
            health.TakeDamage(10);
            Assert.AreEqual(5, before - health.Current, "La cabeza blindada para la mitad.");

            player.transform.position = new Vector2(-6f, 0f);
            Assert.IsFalse(trilobite.Shields(player.transform.position), "Por detrás no.");
        }

        BlueJellyBrain CreateJelly(Vector2 position)
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), fireRate: 1f / 3f, damage: 1, speed: 3.5f, lifetime: 3f);

            var jelly = CreateEnemy<BlueJellyBrain>(Data(10), position);
            var weapon = jelly.GetComponent<Weapon>();
            weapon.SetTeam(Team.Enemy);
            weapon.Attack = attack;
            return jelly;
        }

        [UnityTest]
        public IEnumerator BlueJelly_Warns_ThenFiresARingOf8_WithARotatingGap()
        {
            var jelly = CreateJelly(Vector2.zero);
            var angles = new List<float>();
            jelly.GetComponent<Weapon>().Fired += direction => angles.Add(Vector2.SignedAngle(Vector2.right, direction));
            float warnStart = -1f;
            float firstGap = 0f;
            for (float t = 0f; t < 3f && angles.Count == 0; t += Time.deltaTime)
            {
                if (jelly.Current == BlueJellyBrain.Phase.Warn)
                {
                    if (warnStart < 0f)
                        warnStart = Time.time;
                    firstGap = jelly.GapAngle;
                    // La velocidad se aplica en FixedUpdate: tras un par de pasos de física ya está quieta.
                    if (Time.time - warnStart > Time.fixedDeltaTime * 3f)
                        Assert.Less(jelly.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 0.01f, "Quieta mientras avisa.");
                }
                yield return null;
            }

            Assert.GreaterOrEqual(warnStart, 0f, "Avisa antes de disparar.");
            Assert.AreEqual(8, angles.Count, "Un anillo de 8 proyectiles.");

            // Entre dos proyectiles vecinos hay 36°, salvo en el hueco (72° + 36°).
            angles.Sort();
            float widest = 0f;
            for (int i = 0; i < angles.Count; i++)
            {
                float gap = Mathf.Repeat(angles[(i + 1) % angles.Count] - angles[i], 360f);
                widest = Mathf.Max(widest, gap);
                if (gap < 90f)
                    Assert.AreEqual(36f, gap, 0.5f);
            }
            Assert.AreEqual(108f, widest, 0.5f, "Deja un hueco por donde pasar.");
            Assert.AreNotEqual(firstGap, jelly.GapAngle, "El hueco gira para el siguiente anillo.");
        }

        [UnityTest]
        public IEnumerator BlueJelly_DoesNotShoot_IfItDoesNotSeeYou()
        {
            player.transform.position = new Vector2(30f, 0f);
            var jelly = CreateJelly(Vector2.zero);
            int fired = 0;
            jelly.GetComponent<Weapon>().Fired += _ => fired++;
            yield return new WaitForSeconds(2.5f);

            Assert.AreEqual(0, fired);
            Assert.AreEqual(BlueJellyBrain.Phase.Drift, jelly.Current);
        }
    }
}
