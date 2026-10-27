using Primordia.Audio;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="TailPropulsorMutation"/> desde el nivel II: al acabar la segunda esquiva de la cadena
    /// (o cualquiera, con el Torbellino) suelta un corro de burbujas donde termina.
    /// </summary>
    public class TailBubbles : MonoBehaviour
    {
        PlayerController controller;
        TailBubble prefab;
        int count;
        int damage;
        float lifetime;
        float push;
        float ringRadius;
        bool everyDodge;

        /// <summary>Se lanza al soltar un corro, con su centro.</summary>
        public event System.Action<Vector2> Released;

        public void Configure(PlayerController newController, TailBubble newPrefab, int newCount, int newDamage, float newLifetime,
            float newPush, float newRingRadius, bool newEveryDodge)
        {
            Unsubscribe();
            controller = newController;
            prefab = newPrefab;
            count = newCount;
            damage = newDamage;
            lifetime = newLifetime;
            push = newPush;
            ringRadius = newRingRadius;
            everyDodge = newEveryDodge;
            if (controller != null)
                controller.DodgeEnded += OnDodgeEnded;
        }

        void OnDestroy() => Unsubscribe();

        void Unsubscribe()
        {
            if (controller != null)
                controller.DodgeEnded -= OnDodgeEnded;
        }

        void OnDodgeEnded()
        {
            if (!everyDodge && controller.DodgeInChain < 2)
                return;
            Release(transform.position);
        }

        void Release(Vector2 center)
        {
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = (start + 360f * i / count) * Mathf.Deg2Rad;
                var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                TailBubble.Spawn(prefab, position, damage, lifetime, push);
            }
            Sfx.Play(Sounds.TailBubbles);
            Released?.Invoke(center);
        }
    }
}
