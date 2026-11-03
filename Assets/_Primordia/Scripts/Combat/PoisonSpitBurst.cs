using System.Collections;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// Reventón del Escupitajo venenoso: el dibujo de la bola que estalla, el daño en un radio en el cuadro 1 y el charco salpicado
    /// que deja. Vive solo lo que dura su animación.
    /// </summary>
    public class PoisonSpitBurst : MonoBehaviour
    {
        // El dibujo mide 1,5 u de radio: se escala al del ataque.
        const float ArtRadius = 1.5f;
        const int DamageFrame = 1;
        // Radio al que frena y envenena el charco salpicado que deja: el del reventón normal y el de las bolitas de la Camada.
        const float PuddleRadius = 1f;
        const float SmallPuddleRadius = 0.45f;
        // Por debajo de este radio de reventón es el de las bolitas.
        const float SmallBurstRadius = 1f;

        public static void Spawn(Vector2 position, int damage, float radius, float puddleSeconds, Weapon source, Team victims)
        {
            var obj = new GameObject("PoisonSpitBurst");
            obj.transform.position = position;
            obj.AddComponent<PoisonSpitBurst>().StartCoroutine(Run(obj, position, damage, radius, puddleSeconds, source, victims));
        }

        static IEnumerator Run(GameObject obj, Vector2 position, int damage, float radius, float puddleSeconds, Weapon source, Team victims)
        {
            var kit = ReactionKit.Current;
            var frames = kit != null ? kit.SpitBurst : System.Array.Empty<Sprite>();
            if (puddleSeconds > 0f)
                PoisonPuddle.SpawnSplat(position, puddleSeconds, radius < SmallBurstRadius ? SmallPuddleRadius : PuddleRadius);

            if (frames.Length == 0)
            {
                Damage(position, damage, radius, source, victims);
                Object.Destroy(obj);
                yield break;
            }

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 21;
            obj.transform.localScale = Vector3.one * (radius / ArtRadius);
            for (int i = 0; i < frames.Length; i++)
            {
                renderer.sprite = frames[i];
                if (i == Mathf.Min(DamageFrame, frames.Length - 1))
                    Damage(position, damage, radius, source, victims);
                yield return new WaitForSeconds(kit.SpitBurstFrameSeconds);
            }
            Object.Destroy(obj);
        }

        static void Damage(Vector2 position, int damage, float radius, Weapon source, Team victims)
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
            }
        }
    }
}
