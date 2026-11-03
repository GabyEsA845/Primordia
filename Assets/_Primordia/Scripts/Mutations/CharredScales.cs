using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="CharredScalesMutation"/>. Es un <see cref="FireImmune"/> (el fuego y las explosiones de gas no
    /// le hacen nada) y, desde el nivel II, inmune al veneno de las plantas y con la Brasa (un golpe recibido hace estallar al
    /// jugador en llamas). Al pisar fuego en el suelo, durante unos segundos los ataques del jugador queman a quien dan. En el nivel
    /// III queman siempre y además prenden el escenario donde impactan y por donde pasan.
    /// </summary>
    public class CharredScales : FireImmune, IPoisonImmune
    {
        const float CheckEvery = 0.1f;
        // Cuánto se acerca al fuego del suelo para contar como pisarlo (su radio es 0,55 más el cuerpo).
        const float StepRadius = 0.8f;
        const int BurnDamagePerSecond = 1;
        const float BurnSeconds = 3f;
        const int FxOrder = 22;
        // Nivel III: cuánto prenden los proyectiles, el giro (la Peonza mide 1,3 a 1,5) y el chorro (5 u de alcance).
        const float ProjectileIgniteRadius = 0.6f;
        const float SpinIgniteRadius = 1.4f;
        const float JetIgniteRadius = 0.8f;
        const float JetRange = 5f;
        const float TargetIgniteRadius = 0.9f;

        PlayerController controller;
        Health health;
        Weapon weapon;
        bool poisonProof;
        bool blaze;
        bool alwaysBurn;
        float primedSeconds;
        int blazeDamage;
        float blazeRadius;
        float blazeCooldown;
        Sprite[] primedFx, blazeFx;
        float primedUntil;
        float nextBlaze;
        float nextCheck;
        float nextScenery;
        GameObject primedObject;

        public bool ImmuneToPlantPoison => poisonProof;

        /// <summary>True mientras los ataques del jugador queman (siempre en el nivel III).</summary>
        public bool IsPrimed => alwaysBurn || Time.time < primedUntil;

        /// <summary>Estallidos de la Brasa hasta ahora (para las pruebas).</summary>
        public int Blazes { get; private set; }

        public void Configure(PlayerController newController, Health newHealth, Weapon newWeapon, bool newPoisonProof, bool newBlaze,
            bool newAlwaysBurn, float newPrimedSeconds, int newBlazeDamage, float newBlazeRadius, float newBlazeCooldown, Sprite[] newPrimedFx, Sprite[] newBlazeFx)
        {
            Unsubscribe();
            controller = newController;
            health = newHealth;
            weapon = newWeapon;
            poisonProof = newPoisonProof;
            blaze = newBlaze;
            alwaysBurn = newAlwaysBurn;
            primedSeconds = newPrimedSeconds;
            blazeDamage = newBlazeDamage;
            blazeRadius = newBlazeRadius;
            blazeCooldown = newBlazeCooldown;
            primedFx = newPrimedFx;
            blazeFx = newBlazeFx;
            if (weapon != null)
                weapon.HitTarget += OnHitTarget;
            if (health != null)
                health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            Unsubscribe();
            HidePrimedFx();
        }

        void Unsubscribe()
        {
            if (weapon != null)
                weapon.HitTarget -= OnHitTarget;
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        void Update()
        {
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + CheckEvery;
                if (controller == null || !controller.IsDodging)
                    CheckFire();
            }

            if (alwaysBurn && Time.time >= nextScenery)
            {
                nextScenery = Time.time + CheckEvery;
                IgniteAlongAttacks();
            }

            if (IsPrimed)
                ShowPrimedFx();
            else
                HidePrimedFx();
        }

        /// <summary>
        /// Nivel III: lo inflamable cerca de tus proyectiles en vuelo, del giro de la Peonza y del chorro de vinagre se prende. Los
        /// golpes cuerpo a cuerpo y los que dan en un enemigo prenden donde impactan (<see cref="OnHitTarget"/>).
        /// </summary>
        void IgniteAlongAttacks()
        {
            int playerLayer = CombatLayers.ProjectileLayer(Team.Player);
            foreach (var projectile in Projectile.Active)
            {
                if (projectile != null && projectile.gameObject.layer == playerLayer)
                    Flammables.IgniteNear(projectile.transform.position, ProjectileIgniteRadius);
            }

            Vector2 origin = transform.position;
            if (TryGetComponent<SpinAttack>(out var spin) && spin.IsActive)
                Flammables.IgniteNear(origin, SpinIgniteRadius);
            if (TryGetComponent<JetAttack>(out var jet) && jet.IsActive && controller != null)
            {
                for (float along = 0.8f; along <= JetRange; along += 1f)
                    Flammables.IgniteNear(origin + controller.AimDirection * along, JetIgniteRadius);
            }
        }

        /// <summary>Pisar el fuego del suelo cebaba los ataques durante unos segundos.</summary>
        void CheckFire()
        {
            Vector2 feet = transform.position;
            foreach (var fire in GroundFire.All)
            {
                if (((Vector2)fire.transform.position - feet).sqrMagnitude <= StepRadius * StepRadius)
                {
                    Prime();
                    return;
                }
            }
        }

        /// <summary>Ceba los ataques durante el tiempo de siempre (también lo usan las pruebas).</summary>
        public void Prime() => primedUntil = Time.time + primedSeconds;

        void OnHitTarget(Health target)
        {
            if (!IsPrimed || target == null || target.IsDead)
                return;
            Burn.Apply(target, BurnDamagePerSecond, BurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
            if (alwaysBurn)
                Flammables.IgniteNear(target.transform.position, TargetIgniteRadius);
        }

        // ---------- Brasa ----------

        void OnDamaged(int _)
        {
            if (!blaze || Time.time < nextBlaze)
                return;
            nextBlaze = Time.time + blazeCooldown;
            Blazes++;
            Vector2 center = transform.position;
            FrameEffect.Play("CharredBlaze", blazeFx, 0.06f, FxOrder, null, center);

            var seen = new HashSet<Health>();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(LayerMask.GetMask(CombatLayers.Enemy));
            var hits = new List<Collider2D>();
            Physics2D.OverlapCircle(center, blazeRadius, filter, hits);
            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !seen.Add(target))
                    continue;
                if (blazeDamage > 0)
                    target.TakeDamage(blazeDamage);
                if (!target.IsDead)
                    Burn.Apply(target, BurnDamagePerSecond, BurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
            }
            Flammables.IgniteNear(center, blazeRadius);
        }

        // ---------- Efectos ----------

        void ShowPrimedFx()
        {
            if (primedObject != null || primedFx == null || primedFx.Length == 0)
                return;
            primedObject = new GameObject("CharredPrimed");
            primedObject.transform.SetParent(transform, false);
            var renderer = primedObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = FxOrder;
            primedObject.AddComponent<SpriteFramePlayer>().Play(renderer, primedFx, 0.1f, newLoop: true);
        }

        void HidePrimedFx()
        {
            if (primedObject != null)
                Destroy(primedObject);
            primedObject = null;
        }
    }
}
