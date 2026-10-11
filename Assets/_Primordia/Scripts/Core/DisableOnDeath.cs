using UnityEngine;

namespace Primordia.Core
{
    /// <summary>Desactiva el objeto cuando su <see cref="Health"/> llega a 0.</summary>
    [RequireComponent(typeof(Health))]
    public class DisableOnDeath : MonoBehaviour
    {
        Health health;

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied() => gameObject.SetActive(false);
    }
}
