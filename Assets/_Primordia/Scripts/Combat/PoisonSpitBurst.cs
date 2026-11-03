using System;
using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Reventón del Escupitajo venenoso: el dibujo de la bola que estalla, el daño en un radio en el cuadro 1 y el charco salpicado
    /// que deja. Vive solo lo que dura su animación. Con el linaje Sanguijuela es rojizo y el charco también; con el Yesquero deja
    /// fuego en vez de charco y quema a lo que alcanza.
    /// </summary>
    public class PoisonSpitBurst : MonoBehaviour
    {
        /// <summary>Aspecto y efecto del reventón según el linaje del jugador.</summary>
        public enum Look
        {
            Normal,
            Leech,
            Fire
        }

        // El dibujo mide 1,5 u de radio: se escala al del ataque.
        const float ArtRadius = 1.5f;
        const int DamageFrame = 1;
        // Radio al que frena y envenena el charco salpicado que deja: el del reventón normal y el de las bolitas de la Camada.
        const float PuddleRadius = 1f;
        const float SmallPuddleRadius = 0.45f;
        // Por debajo de este radio de reventón es el de las bolitas.
        const float SmallBurstRadius = 1f;
        // Yesquero: el fuego que deja el reventón (en el centro y en cuatro puntos a esta fracción del radio) y lo que quema.
        const float FireSpread = 0.55f;
        const int FireBurnDamage = 1;
        const float FireBurnSeconds = 3f;

        /// <summary>Estalla una bola del jugador: (dónde, quién la disparó, si es una bolita de la Camada).</summary>
        public static event Action<Vector2, Weapon, bool> Burst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Burst = null;

        public static void Spawn(Vector2 position, int damage, float radius, float puddleSeconds, Weapon source, Team victims,
            Look look = Look.Normal)
        {
            var obj = new GameObject("PoisonSpitBurst");
            obj.transform.position = position;
            obj.AddComponent<PoisonSpitBurst>().StartCoroutine(Run(obj, position, damage, radius, puddleSeconds, source, victims, look));
            if (victims == Team.Enemy)
                Burst?.Invoke(position, source, radius < SmallBurstRadius);
        }

        static IEnumerator Run(GameObject obj, Vector2 position, int damage, float radius, float puddleSeconds, Weapon source, Team victims,
            Look look)
        {
            var kit = ReactionKit.Current;
            var frames = kit != null ? kit.SpitBurst : Array.Empty<Sprite>();
            if (look == Look.Leech && kit != null && kit.LeechBurst.Length > 0)
                frames = kit.LeechBurst;
            else if (look == Look.Fire && kit != null && kit.TinderBurst.Length > 0)
                frames = kit.TinderBurst;
            bool small = radius < SmallBurstRadius;

            if (look == Look.Fire)
                LeaveFire(position, radius, puddleSeconds);
            else if (puddleSeconds > 0f)
                PoisonPuddle.SpawnSplat(position, puddleSeconds, small ? SmallPuddleRadius : PuddleRadius, leech: look == Look.Leech);

            if (frames.Length == 0)
            {
                Damage(position, damage, radius, source, victims, look);
                UnityEngine.Object.Destroy(obj);
                yield break;
            }

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 21;
            obj.transform.localScale = Vector3.one * (radius / ArtRadius);
            for (int i = 0; i < frames.Length; i++)
            {
                renderer.sprite = frames[i];
                if (i == Mathf.Min(DamageFrame, frames.Length - 1))
                    Damage(position, damage, radius, source, victims, look);
                yield return new WaitForSeconds(kit.SpitBurstFrameSeconds);
            }
            UnityEngine.Object.Destroy(obj);
        }

        // Fuego en el centro y alrededor, y prende lo inflamable que alcanza (helechos, metano, charcos).
        static void LeaveFire(Vector2 position, float radius, float seconds)
        {
            float duration = seconds > 0f ? seconds : GroundFire.DefaultSeconds;
            TinderScorch.Attach(GroundFire.Spawn(position, duration, harmsPlayer: false));
            if (radius >= SmallBurstRadius)
            {
                foreach (var side in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
                    TinderScorch.Attach(GroundFire.Spawn(position + side * radius * FireSpread, duration, harmsPlayer: false));
            }
            Flammables.IgniteNear(position, radius);
        }

        static void Damage(Vector2 position, int damage, float radius, Weapon source, Team victims, Look look)
        {
            if (damage <= 0)
                return;

            int mask = LayerMask.GetMask(victims == Team.Enemy ? CombatLayers.Enemy : CombatLayers.Player);
            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapCircleAll(position, radius, mask))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !seen.Add(health))
                    continue;
                if (health.TakeDamage(damage) && source != null)
                    source.ReportHit(health);
                if (look == Look.Fire && !health.IsDead && health.TryGetComponent<Enemy>(out _))
                    Burn.Apply(health, FireBurnDamage, FireBurnSeconds, ReactionKit.Current != null ? ReactionKit.Current.BurningMarker : null);
            }
        }
    }
}
