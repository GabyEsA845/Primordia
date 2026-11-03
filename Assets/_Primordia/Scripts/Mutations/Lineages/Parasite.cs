using System;
using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Parásito (piso 3) pegado a un enemigo: le quita vida cada segundo hasta que muere, y entonces salta al enemigo vivo más
    /// cercano que no tenga uno. Hay un máximo de parásitos vivos en total (<see cref="Limit"/>, 3 de base). Lo pega
    /// <see cref="ParasiteBenefit"/>; sin enemigos a los que saltar, se pierde.
    /// </summary>
    public class Parasite : MonoBehaviour
    {
        public const string Key = "Parasite";
        public const int DefaultLimit = 3;

        // Lo que tarda el salto de un enemigo a otro (los 5 cuadros del dibujo a 0,07 s) y lo alto que sube.
        public const float LeapSeconds = 0.35f;
        public const float LeapArc = 0.8f;

        static readonly List<Parasite> all = new();

        Health host;
        int damage = 1;
        float timer;
        GameObject markerPrefab;
        GameObject jumpEffect;
        bool released;

        /// <summary>Parásitos vivos ahora mismo, en todos los enemigos.</summary>
        public static int Alive => all.Count;

        /// <summary>Máximo de parásitos vivos a la vez.</summary>
        public static int Limit { get; set; } = DefaultLimit;

        /// <summary>True durante el tic de un parásito: su daño no cuenta para pegar otro (<see cref="ParasiteBenefit"/>).</summary>
        public static bool Ticking { get; private set; }

        /// <summary>Se lanza con la vida que ha quitado un parásito: (parásito, vida quitada).</summary>
        public static event Action<Parasite, int> Drained;

        /// <summary>Se lanza cuando un parásito salta al morir su enemigo: (parásito nuevo, enemigo anterior).</summary>
        public static event Action<Parasite, Health> Jumped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            Limit = DefaultLimit;
            Ticking = false;
            Drained = null;
            Jumped = null;
        }

        public Health Host => host;

        /// <summary>True si ese parásito está en un jefe (la vida que chupa cura la mitad).</summary>
        public bool OnBoss => host != null && host.GetComponentInParent<IBoss>() != null;

        public static bool Has(Health target) => target != null && target.TryGetComponent<Parasite>(out _);

        /// <summary>Pega un parásito. Null si no cabe (máximo), ya lo tiene, está muerto o no es un enemigo.</summary>
        public static Parasite Attach(Health target, int damagePerSecond, GameObject marker, GameObject jump)
        {
            if (target == null || target.IsDead || all.Count >= Limit || !target.TryGetComponent<Enemy>(out _) || Has(target))
                return null;

            var parasite = target.gameObject.AddComponent<Parasite>();
            parasite.host = target;
            parasite.damage = Mathf.Max(1, damagePerSecond);
            parasite.markerPrefab = marker;
            parasite.jumpEffect = jump;
            all.Add(parasite);
            target.Died += parasite.OnHostDied;
            // Pegado al cuerpo, no encima de la cabeza.
            StatusMarkers.Show(target, Key, marker, 0f, overhead: false);
            return parasite;
        }

        /// <summary>Quita todos los parásitos (al perder el linaje).</summary>
        public static void RemoveAll()
        {
            foreach (var parasite in all.ToArray())
            {
                if (parasite != null)
                    Destroy(parasite);
            }
            all.Clear();
        }

        void Update()
        {
            if (host == null || host.IsDead)
            {
                Release();
                Destroy(this);
                return;
            }

            timer += Time.deltaTime;
            if (timer < 1f)
                return;
            timer -= 1f;

            int before = host.Current;
            Ticking = true;
            try
            {
                host.TakeDamage(damage, DamageKind.Tick);
            }
            finally
            {
                Ticking = false;
            }

            // Si ha matado al enemigo, su muerte ya ha movido el parásito (OnHostDied).
            int taken = before - host.Current;
            if (taken > 0)
                Drained?.Invoke(this, taken);
        }

        void OnHostDied()
        {
            var from = host;
            Vector2 origin = from.transform.position;
            var next = FindNextHost(from);
            Release();
            Destroy(this);

            if (next == null)
                return;
            var moved = Attach(next, damage, markerPrefab, jumpEffect);
            if (moved != null)
            {
                // El bicho cruza en arco de un cuerpo al otro (el efecto se destruye al llegar).
                var leap = OneShotEffect.Spawn(jumpEffect, origin);
                if (leap != null && leap.TryGetComponent<ArcFlight>(out var flight))
                    flight.Launch(origin, next.transform, LeapSeconds, LeapArc);
                Jumped?.Invoke(moved, from);
            }
        }

        // Cuenta ya como libre su sitio (Release) antes de buscar, así que el límite no lo bloquea.
        static Health FindNextHost(Health from)
        {
            Health best = null;
            float bestDistance = float.MaxValue;
            Vector2 origin = from != null ? (Vector2)from.transform.position : Vector2.zero;
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            {
                var health = enemy.Health;
                if (health == null || health == from || health.IsDead || Has(health))
                    continue;
                float distance = ((Vector2)health.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = health;
                }
            }
            return best;
        }

        // Sale de la cuenta y de su enemigo (a la vez, para poder pegarse a otro en el mismo cuadro).
        void Release()
        {
            if (released)
                return;
            released = true;
            all.Remove(this);
            if (host != null)
            {
                host.Died -= OnHostDied;
                StatusMarkers.Hide(host, Key);
            }
        }

        void OnDestroy() => Release();
    }
}
