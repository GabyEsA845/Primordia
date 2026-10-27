using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Primordia.Tests
{
    /// <summary>
    /// Mutaciones del piso 2 que no son Bocas. Cola propulsora: dos esquivas seguidas; en el nivel II la segunda deja
    /// burbujas que dañan y empujan. Espinas de Hallucigenia: al esquivar dejas púas que dañan una vez a quien las pisa y,
    /// con el Erizo, salen disparadas al desaparecer. Placas de trilobite: +1 corazón y rebota 1 de cada 4 proyectiles (todos
    /// con Enrollarse, destruye los que toca al esquivar). Escleritos de Wiwaxia: tandas de escamas hacia los enemigos cercanos; con la Órbita,
    /// antes giran y destruyen proyectiles. Saco de crías: larvas aliadas que muerden y, con el Enjambre, se dividen al matar.
    /// Ojos compuestos: cámara lenta de 3 s en la que el jugador sigue a su ritmo.
    /// </summary>
    public class Floor2MutationsTests
    {
        readonly List<Object> created = new();
        PlayerMutations player;
        PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            var playerObject = Track(new GameObject("Player"));
            playerObject.layer = LayerMask.NameToLayer(CombatLayers.Player);
            playerObject.AddComponent<Weapon>();
            controller = playerObject.AddComponent<PlayerController>();
            controller.readInput = false;
            player = playerObject.AddComponent<PlayerMutations>();
            playerObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
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
            foreach (var bubble in Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None))
                Object.DestroyImmediate(bubble.gameObject);
            foreach (var spine in Object.FindObjectsByType<GroundSpine>(FindObjectsSortMode.None))
                Object.DestroyImmediate(spine.gameObject);
            foreach (var larva in Object.FindObjectsByType<Larva>(FindObjectsSortMode.None))
                Object.DestroyImmediate(larva.gameObject);
            TimeWarp.Clear();
            Time.timeScale = 1f;
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);
            ProjectilePool.Clear();
        }

        T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        T Mutation<T>(MutationSlot slot) where T : MutationData
        {
            var mutation = Track(ScriptableObject.CreateInstance<T>());
            mutation.ConfigureBase(typeof(T).Name, "", slot, Color.white);
            return mutation;
        }

        void EquipAt(MutationData mutation, int level)
        {
            player.Equip(mutation);
            for (int i = 1; i < level; i++)
                player.LevelUp(mutation.Slot);
        }

        /// <summary>Enemigo quieto: vida, cuerpo y capa Enemy.</summary>
        Health Enemy(Vector2 position, int health = 20, float radius = 0.4f)
        {
            var go = Track(new GameObject("Enemy"));
            go.layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f;
            go.AddComponent<CircleCollider2D>().radius = radius;
            var life = go.AddComponent<Health>();
            life.SetMax(health, healDifference: false);
            life.ResetHealth();
            return life;
        }

        [UnityTest]
        public IEnumerator TailPropulsor_AllowsTwoDodgesInARow_ButNotThree()
        {
            var tail = Mutation<TailPropulsorMutation>(MutationSlot.Limbs);
            EquipAt(tail, 1);
            Assert.AreEqual(1, controller.ExtraDodges);

            Assert.IsTrue(controller.TryDodge());
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(controller.TryDodge(), "La segunda, encadenada antes de la recarga.");
            Assert.AreEqual(2, controller.DodgeInChain);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(controller.TryDodge(), "La tercera espera a la recarga.");

            player.Unequip(MutationSlot.Limbs);
            Assert.AreEqual(0, controller.ExtraDodges);
        }

        [UnityTest]
        public IEnumerator TailPropulsor_LevelII_SecondDodgeLeavesBubbles()
        {
            var tail = Mutation<TailPropulsorMutation>(MutationSlot.Limbs);
            EquipAt(tail, 2);

            controller.TryDodge();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None).Length, "La primera no deja burbujas.");

            controller.TryDodge();
            yield return new WaitForSeconds(0.3f);
            var bubbles = Object.FindObjectsByType<TailBubble>(FindObjectsSortMode.None);
            Assert.AreEqual(3, bubbles.Length, "La segunda deja 3.");
            Assert.AreEqual(2, bubbles[0].Damage);
        }

        [UnityTest]
        public IEnumerator TailBubble_PopsOnTheFirstEnemy_DamagingIt()
        {
            var enemy = Enemy(new Vector2(10f, 0f));
            var bubble = TailBubble.Spawn(null, new Vector2(10.2f, 0f), 2, 3f, 6f);
            yield return null;
            yield return null;

            Assert.AreEqual(18, enemy.Current);
            Assert.IsTrue(bubble == null || bubble.Popped, "Revienta al tocarlo.");
        }

        [UnityTest]
        public IEnumerator HallucigeniaSpines_DodgeLeavesSpines_ThatHurtEachEnemyOnce()
        {
            var spines = Mutation<HallucigeniaSpinesMutation>(MutationSlot.Limbs);
            EquipAt(spines, 1);
            player.transform.position = new Vector2(20f, 0f);
            controller.SetInput(Vector2.right, Vector2.right);

            controller.TryDodge();
            yield return null;
            var placed = Object.FindObjectsByType<GroundSpine>(FindObjectsSortMode.None);
            Assert.AreEqual(2, placed.Length, "Nivel I: 2 púas.");
            foreach (var spine in placed)
                Assert.Less(Vector2.Distance(spine.transform.position, new Vector2(20f, 0f)), 0.6f, "Donde empezó la esquiva.");

            // Pequeño, para que no llegue a la púa de al lado.
            var enemy = Enemy(placed[0].transform.position, radius: 0.1f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(18, enemy.Current, "2 de daño, una sola vez aunque siga encima.");
        }

        [UnityTest]
        public IEnumerator GroundSpine_Hedgehog_ShootsAtTheNearestEnemy_WhenItExpires()
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.1f;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), 1f, 2, 12f, 0.8f);

            Enemy(new Vector2(33f, 0f));
            var spine = GroundSpine.Spawn(null, new Vector2(30f, 0f), 2, 0.1f, attack, 7f, player.GetComponent<Weapon>());
            Vector2? shot = null;
            spine.Shot += direction => shot = direction;
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(shot.HasValue, "Al desaparecer sale disparada.");
            Assert.Greater(shot.Value.x, 0.9f, "Hacia el enemigo.");
        }

        AttackData ProjectileAttack(float speed = 12f, float lifetime = 0.8f)
        {
            var template = Track(new GameObject("ProjectileTemplate"));
            template.SetActive(false);
            template.AddComponent<Rigidbody2D>().gravityScale = 0f;
            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
            var attack = Track(ScriptableObject.CreateInstance<AttackData>());
            attack.Configure(template.AddComponent<Projectile>(), 1f, 2, speed, lifetime);
            return attack;
        }

        [Test]
        public void TrilobitePlates_GiveAHeart_WhileEquipped()
        {
            var plates = Mutation<TrilobitePlatesMutation>(MutationSlot.Skin);
            int before = player.Health.Max;
            EquipAt(plates, 1);
            Assert.AreEqual(before + 2, player.Health.Max);
            player.Unequip(MutationSlot.Skin);
            Assert.AreEqual(before, player.Health.Max);
        }

        [UnityTest]
        public IEnumerator TrilobitePlates_BounceOneOfFour_AndAllWhileRolling()
        {
            var plates = Mutation<TrilobitePlatesMutation>(MutationSlot.Skin);
            EquipAt(plates, 1);
            var bounce = player.GetComponent<PlatesBounce>();
            var shotObject = Track(new GameObject("Projectile"));
            shotObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            shotObject.AddComponent<CircleCollider2D>().isTrigger = true;
            var projectile = shotObject.AddComponent<Projectile>();

            var results = new List<bool>();
            for (int i = 0; i < 8; i++)
                results.Add(bounce.TryReflect(projectile));
            CollectionAssert.AreEqual(new[] { false, false, false, true, false, false, false, true }, results, "1 de cada 4.");

            Assert.IsTrue(controller.TryDodge());
            Assert.IsFalse(bounce.TryReflect(projectile), "Sin Enrollarse, esquivando lo atraviesa como siempre.");
            // La esquiva (0,25 s) y su recarga (0,4 s).
            yield return new WaitForSeconds(0.8f);

            player.LevelUp(MutationSlot.Skin);
            player.LevelUp(MutationSlot.Skin);
            // El comportamiento del nivel anterior se destruye al final del cuadro.
            yield return null;
            bounce = player.GetComponent<PlatesBounce>();
            Assert.IsTrue(controller.TryDodge(), "La esquiva ya está lista.");
            Assert.IsTrue(bounce.TryReflect(projectile), "Enrollarse: esquivando destruye todos.");
            Assert.IsTrue(bounce.TryReflect(projectile));
        }

        [UnityTest]
        public IEnumerator WiwaxiaSclerites_ReleaseAVolley_TowardNearbyEnemies()
        {
            var sclerites = Mutation<WiwaxiaScleritesMutation>(MutationSlot.Skin);
            sclerites.ConfigureLook(ProjectileAttack(), null, null, null);
            EquipAt(sclerites, 1);
            player.transform.position = new Vector2(50f, 0f);
            Enemy(new Vector2(53f, 0f));
            var volley = player.GetComponent<ScleriteVolley>();
            IReadOnlyList<Projectile> launched = null;
            volley.Released += list => launched = list;
            volley.Ready();
            yield return null;
            yield return null;

            Assert.AreEqual(1, volley.Volleys);
            Assert.IsNotNull(launched);
            Assert.AreEqual(3, launched.Count, "Nivel I: 3 escamas.");
            Assert.Greater(launched[0].Velocity.x, 0f, "Hacia el enemigo.");
        }

        [UnityTest]
        public IEnumerator WiwaxiaSclerites_Orbit_DestroysEnemyProjectiles_ThenFires()
        {
            var sclerites = Mutation<WiwaxiaScleritesMutation>(MutationSlot.Skin);
            sclerites.ConfigureLook(ProjectileAttack(), null, null, null);
            EquipAt(sclerites, 3);
            player.transform.position = new Vector2(70f, 0f);
            Enemy(new Vector2(74f, 0f));
            var volley = player.GetComponent<ScleriteVolley>();

            // Un proyectil enemigo casi quieto en el camino de la órbita.
            var enemyShot = ProjectileAttack(speed: 0.1f, lifetime: 5f);
            var incoming = ProjectilePool.Get(enemyShot.ProjectilePrefab);
            incoming.Launch(new Vector2(71.1f, 0f), Vector2.left, enemyShot, CombatLayers.ProjectileLayer(Team.Enemy), null);

            volley.Ready();
            yield return null;
            Assert.IsTrue(volley.IsOrbiting, "Primero giran.");
            for (float t = 0f; t < 3f && volley.IsOrbiting; t += Time.deltaTime)
                yield return null;

            Assert.GreaterOrEqual(volley.Blocked, 1, "Destruyen el proyectil enemigo.");
            Assert.AreEqual(1, volley.Volleys, "Y luego salen.");
        }

        [UnityTest]
        public IEnumerator TrilobitePlates_Roll_BouncesEnemyProjectilesInsideTheBall()
        {
            var plates = Mutation<TrilobitePlatesMutation>(MutationSlot.Skin);
            EquipAt(plates, 3);
            player.transform.position = new Vector2(90f, 0f);
            var bounce = player.GetComponent<PlatesBounce>();

            // Una carga enemiga casi quieta dentro de la bola, sin tocar el cuerpo del jugador.
            var enemyShot = ProjectileAttack(speed: 0.1f, lifetime: 5f);
            var incoming = ProjectilePool.Get(enemyShot.ProjectilePrefab);
            incoming.Launch(new Vector2(90.6f, 0f), Vector2.up, enemyShot, CombatLayers.ProjectileLayer(Team.Enemy), null);
            yield return null;
            Assert.AreEqual(0, bounce.Bounced, "Sin esquivar, la bola no está.");

            // Hacia la carga: así está dentro de la bola sea cual sea el ritmo de los cuadros.
            controller.SetInput(Vector2.right, Vector2.right);
            Assert.IsTrue(controller.TryDodge());
            yield return null;
            yield return null;

            Assert.GreaterOrEqual(bounce.Bounced, 1, "Enrollado, destruye lo que hay dentro de la bola.");
            Assert.IsFalse(incoming.isActiveAndEnabled);
        }

        static Larva.Settings LarvaSettings(bool splits = false) => new()
        {
            lifetime = 8f, speed = 5.5f, range = 9f, biteDamage = 2, biteInterval = 0.7f, biteReach = 0.55f, splits = splits,
        };

        [UnityTest]
        public IEnumerator BroodSac_ReleasesTwoLarvae_ThreeAtLevelII()
        {
            var sac = Mutation<BroodSacMutation>(MutationSlot.Organ);
            EquipAt(sac, 1);
            sac.Activate(player);
            yield return null;
            Assert.AreEqual(2, Larva.CountOf(player));

            foreach (var larva in Object.FindObjectsByType<Larva>(FindObjectsSortMode.None))
                Object.DestroyImmediate(larva.gameObject);
            player.LevelUp(MutationSlot.Organ);
            sac.Activate(player);
            yield return null;
            Assert.AreEqual(3, Larva.CountOf(player));
        }

        [UnityTest]
        public IEnumerator Larva_ChasesAndBitesTheNearestEnemy()
        {
            player.transform.position = new Vector2(110f, 0f);
            var enemy = Enemy(new Vector2(112f, 0f));
            var larva = Larva.Spawn(null, new Vector2(110f, 0f), LarvaSettings(), player);
            yield return new WaitForSeconds(0.8f);

            Assert.AreSame(enemy, larva.Target);
            Assert.Less(enemy.Current, 20, "Lo alcanza y lo muerde.");
        }

        [UnityTest]
        public IEnumerator Larva_Swarm_SplitsWhenItKills()
        {
            player.transform.position = new Vector2(130f, 0f);
            Enemy(new Vector2(130.4f, 0f), health: 2);
            Larva.Spawn(null, new Vector2(130f, 0f), LarvaSettings(splits: true), player);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(2, Larva.CountOf(player), "La que mata se divide en dos.");
        }

        [UnityTest]
        public IEnumerator CompoundEyes_SlowTheWorld_ForTheirDuration_AndTotalVisionAddsDamage()
        {
            var eyes = Mutation<CompoundEyesMutation>(MutationSlot.Organ);
            EquipAt(eyes, 3);
            float damage = player.Weapon.DamageMultiplier;
            eyes.Activate(player);

            Assert.AreEqual(0.6f, TimeWarp.Scale, 0.001f);
            Assert.AreEqual(0.6f, Time.timeScale, 0.001f);
            Assert.IsTrue(eyes.IsRunning(player), "La recarga espera mientras dura.");
            Assert.AreEqual(damage * 1.3f, player.Weapon.DamageMultiplier, 0.001f, "Visión total: +30 % de daño.");

            // Nivel III: 4 s en tiempo real.
            yield return new WaitForSecondsRealtime(4.3f);
            Assert.AreEqual(1f, TimeWarp.Scale);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(eyes.IsRunning(player));
            Assert.AreEqual(damage, player.Weapon.DamageMultiplier, 0.001f);
        }

        [UnityTest]
        public IEnumerator TimeWarp_PlayerDodgeCoversTheSameDistance()
        {
            player.transform.position = new Vector2(150f, 0f);
            controller.SetInput(Vector2.right, Vector2.right);
            TimeWarp.Set(0.5f);
            Assert.IsTrue(controller.TryDodge());
            // Solo la esquiva: después se queda quieto.
            controller.SetInput(Vector2.zero, Vector2.right);
            yield return new WaitForSecondsRealtime(0.6f);

            // La esquiva acaba en el paso de física siguiente a su fin: según dónde caiga, dura un paso más o menos. A mitad de
            // velocidad cada paso (0,02 s) la mueve 0,48 u (24 u/s), así que la tolerancia es un paso: 2,88 o 3,36 son lo esperado.
            Assert.AreEqual(3f, player.transform.position.x - 150f, 0.5f, "A mitad de velocidad, la esquiva recorre lo de siempre.");
        }

        [UnityTest]
        public IEnumerator Larvae_SpreadAroundTheirTarget_InsteadOfPilingUp()
        {
            player.transform.position = new Vector2(170f, -3f);
            Enemy(new Vector2(170f, 0f), health: 1000);
            var larvae = new List<Larva>();
            for (int i = 0; i < 3; i++)
                larvae.Add(Larva.Spawn(null, new Vector2(170f, -2f), LarvaSettings(), player));
            yield return new WaitForSeconds(1f);

            float closest = float.MaxValue;
            for (int i = 0; i < larvae.Count; i++)
                for (int j = i + 1; j < larvae.Count; j++)
                    closest = Mathf.Min(closest, Vector2.Distance(larvae[i].transform.position, larvae[j].transform.position));
            Assert.Greater(closest, 0.3f, "Cada una ataca desde su lado.");
            foreach (var larva in larvae)
                Assert.LessOrEqual(Vector2.Distance(larva.transform.position, new Vector2(170f, 0f)), 0.6f, "Todas a distancia de mordisco.");
        }
    }
}
