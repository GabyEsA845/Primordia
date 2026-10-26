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
    /// quieta y suelta un anillo de 8 con un hueco que gira de un anillo al siguiente; si no te ve, no dispara. Ottoia:
    /// enterrada es inmune y no choca; se pone debajo de ti, avisa, muerde a quien siga en el círculo, queda vulnerable
    /// un momento y se vuelve a hundir. Esponja Vauxia: su pulso cura y escuda a los demás enemigos cercanos (no a sí
    /// misma ni a los lejanos) y nada la mueve. Pikaia: los que nacen juntos forman un cardumen, te rodean y hacen las
    /// pasadas de uno en uno.
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

        EnemyData Data(int health, float detection = 10f, int contactDamage = 1)
        {
            var data = Track(ScriptableObject.CreateInstance<EnemyData>());
            data.Configure(EnemyBehaviour.Chaser, maxHealth: health, moveSpeed: 3f, detectionRange: detection, contactDamage: contactDamage);
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

        OttoiaBrain CreateOttoia(Vector2 position)
        {
            // El mordisco busca colisionadores en la capa del jugador.
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            // Como en el juego, sin daño por contacto: solo muerde.
            return CreateEnemy<OttoiaBrain>(Data(9, contactDamage: 0), position);
        }

        [UnityTest]
        public IEnumerator Ottoia_Burrowed_IsImmune_AndCreepsUnderYou()
        {
            player.transform.position = new Vector2(3f, 0f);
            var ottoia = CreateOttoia(Vector2.zero);
            var health = ottoia.GetComponent<Health>();
            yield return null;
            yield return null;

            Assert.IsTrue(ottoia.IsBurrowed);
            Assert.IsFalse(health.TakeDamage(5), "Enterrada no recibe daño.");
            Assert.IsFalse(ottoia.GetComponent<Collider2D>().enabled, "Enterrada no choca con nada.");

            for (float t = 0f; t < 3f && ottoia.Current == OttoiaBrain.Phase.Burrowed; t += Time.deltaTime)
                yield return null;

            Assert.AreEqual(OttoiaBrain.Phase.Warn, ottoia.Current, "Al llegar debajo de ti, avisa.");
            Assert.Less(Vector2.Distance(ottoia.transform.position, player.transform.position), 0.8f);
        }

        [UnityTest]
        public IEnumerator Ottoia_BitesWhoStaysInTheCircle_IsVulnerableOutside_ThenSinks()
        {
            player.transform.position = new Vector2(0.3f, 0f);
            var playerHealth = player.GetComponent<Health>();
            var ottoia = CreateOttoia(Vector2.zero);
            var health = ottoia.GetComponent<Health>();
            Health bitten = null;
            bool bit = false;
            ottoia.Bit += target => { bit = true; bitten = target; };
            int before = playerHealth.Current;

            for (float t = 0f; t < 2f && !bit; t += Time.deltaTime)
                yield return null;

            Assert.IsTrue(bit, "Sale y muerde.");
            Assert.AreSame(playerHealth, bitten);
            Assert.AreEqual(2, before - playerHealth.Current, "El mordisco quita un corazón.");
            Assert.IsFalse(ottoia.IsBurrowed);
            Assert.IsTrue(health.TakeDamage(1), "Asomada sí recibe daño.");
            Assert.IsTrue(ottoia.GetComponent<Collider2D>().enabled);

            for (float t = 0f; t < 2.5f && ottoia.Current != OttoiaBrain.Phase.Burrowed; t += Time.deltaTime)
                yield return null;

            Assert.IsTrue(ottoia.IsBurrowed, "Se vuelve a hundir.");
            Assert.IsFalse(health.TakeDamage(1), "Y vuelve a ser inmune.");
        }

        [UnityTest]
        public IEnumerator Ottoia_DoesNotBite_IfYouLeaveTheCircle()
        {
            player.transform.position = new Vector2(0.3f, 0f);
            var playerHealth = player.GetComponent<Health>();
            var ottoia = CreateOttoia(Vector2.zero);
            bool bit = false;
            Health bitten = null;
            ottoia.Bit += target => { bit = true; bitten = target; };
            int before = playerHealth.Current;

            for (float t = 0f; t < 1f && ottoia.Current != OttoiaBrain.Phase.Warn; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual(OttoiaBrain.Phase.Warn, ottoia.Current);
            player.transform.position = new Vector2(3f, 0f);

            for (float t = 0f; t < 2f && !bit; t += Time.deltaTime)
                yield return null;

            Assert.IsTrue(bit);
            Assert.IsNull(bitten, "Al ver el círculo te apartas y no te alcanza.");
            Assert.AreEqual(before, playerHealth.Current);
        }

        /// <summary>Enemigo quieto, sin IA ni cerebro (sin blindaje ni paseos que estropeen las cuentas).</summary>
        Enemy CreateDummy(Vector2 position)
        {
            var go = Track(new GameObject("Dummy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.5f;
            var enemy = go.AddComponent<Enemy>();
            enemy.SetData(Data(14));
            if (go.TryGetComponent<Rigidbody2D>(out var body))
                body.gravityScale = 0f;
            return enemy;
        }

        [UnityTest]
        public IEnumerator Vauxia_Pulse_HealsAndShieldsNearbyEnemies_NotItselfNorFarOnes()
        {
            var vauxia = CreateEnemy<VauxiaBrain>(Data(16), Vector2.zero);
            var near = CreateDummy(new Vector2(2f, 1f));
            var far = CreateDummy(new Vector2(-7f, 0f));
            var vauxiaHealth = vauxia.GetComponent<Health>();
            var nearHealth = near.GetComponent<Health>();
            var farHealth = far.GetComponent<Health>();
            yield return null;
            vauxiaHealth.TakeDamage(5);
            nearHealth.TakeDamage(5);
            farHealth.TakeDamage(5);
            int vauxiaBefore = vauxiaHealth.Current, nearBefore = nearHealth.Current, farBefore = farHealth.Current;

            bool pulsed = false;
            vauxia.Pulsed += _ => pulsed = true;
            for (float t = 0f; t < 2.5f && !pulsed; t += Time.deltaTime)
                yield return null;

            Assert.IsTrue(pulsed, "Al haber combate, emite su pulso.");
            Assert.AreEqual(nearBefore + 2, nearHealth.Current, "Cura 2 a los cercanos.");
            Assert.AreEqual(farBefore, farHealth.Current, "No llega a los lejanos.");
            Assert.AreEqual(vauxiaBefore, vauxiaHealth.Current, "No se cura a sí misma.");

            // El escudo leve se come los 2 primeros puntos de daño.
            var shield = near.GetComponent<VauxiaShield>();
            Assert.IsNotNull(shield);
            Assert.AreEqual(2, shield.Points);
            int beforeHit = nearHealth.Current;
            nearHealth.TakeDamage(3);
            Assert.AreEqual(1, beforeHit - nearHealth.Current, "El escudo absorbe 2 de los 3.");
            Assert.AreEqual(0, shield.Points, "Y se rompe.");
            Assert.IsNull(far.GetComponent<VauxiaShield>());
            Assert.IsNotNull(near.GetComponent<HealGlow>(), "El curado destella en verde.");
            Assert.IsNull(far.GetComponent<HealGlow>());
        }

        [UnityTest]
        public IEnumerator Vauxia_CannotBeMoved()
        {
            var vauxia = CreateEnemy<VauxiaBrain>(Data(16), Vector2.zero);
            yield return null;
            vauxia.GetComponent<EnemyAI>().Knockback(new Vector2(10f, 0f), 0.3f);
            yield return new WaitForSeconds(0.2f);

            Assert.Less(Vector2.Distance(vauxia.transform.position, Vector2.zero), 0.01f);
        }

        List<PikaiaBrain> CreateShoal(Vector2 center, int count = 5)
        {
            var fish = new List<PikaiaBrain>();
            for (int i = 0; i < count; i++)
            {
                var offset = (Vector2)(Quaternion.Euler(0f, 0f, 360f * i / count) * Vector2.right) * 0.7f;
                var brain = CreateEnemy<PikaiaBrain>(Data(3), center + offset);
                brain.GetComponent<CircleCollider2D>().radius = 0.3f;
                fish.Add(brain);
            }
            return fish;
        }

        [UnityTest]
        public IEnumerator Pikaia_BornTogether_FormOneShoal()
        {
            player.transform.position = new Vector2(40f, 0f);
            var shoal = CreateShoal(Vector2.zero);
            var loner = CreateEnemy<PikaiaBrain>(Data(3), new Vector2(-15f, 0f));
            yield return null;

            Assert.IsNotNull(shoal[0].Shoal);
            Assert.AreEqual(5, shoal[0].Shoal.Members.Count);
            Assert.IsTrue(shoal.TrueForAll(fish => fish.Shoal == shoal[0].Shoal), "Los cinco son un mismo cardumen.");
            Assert.AreNotSame(shoal[0].Shoal, loner.Shoal, "Uno lejano va por su cuenta.");
        }

        [UnityTest]
        public IEnumerator Pikaia_SurroundYou_AndDashOneAtATime()
        {
            var shoal = CreateShoal(Vector2.zero);
            int dashes = 0;
            int mostAtOnce = 0;
            var dashing = new HashSet<PikaiaBrain>();
            float fastest = 0f;
            for (float t = 0f; t < 5f && dashes < 2; t += Time.deltaTime)
            {
                int now = 0;
                foreach (var fish in shoal)
                {
                    bool busy = fish.Current is PikaiaBrain.Phase.Aim or PikaiaBrain.Phase.Dash;
                    if (busy)
                        now++;
                    if (fish.Current == PikaiaBrain.Phase.Dash)
                    {
                        if (dashing.Add(fish))
                            dashes++;
                        fastest = Mathf.Max(fastest, fish.GetComponent<Rigidbody2D>().linearVelocity.magnitude);
                    }
                    else
                    {
                        dashing.Remove(fish);
                    }
                }
                mostAtOnce = Mathf.Max(mostAtOnce, now);
                yield return null;
            }

            Assert.GreaterOrEqual(dashes, 2, "Hacen pasadas por turnos.");
            Assert.AreEqual(1, mostAtOnce, "De una en una.");
            Assert.Greater(fastest, 8f, "La pasada es muy rápida.");
        }
    }
}
