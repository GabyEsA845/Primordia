using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Primordia.Combat
{
    /// <summary>
    /// Reutiliza los proyectiles en lugar de crearlos y destruirlos en cada disparo.
    /// Hay un pool por cada prefab de proyectil; se vacía al descargar una escena.
    /// </summary>
    public static class ProjectilePool
    {
        static readonly Dictionary<Projectile, ObjectPool<Projectile>> Pools = new();

        // El proyecto entra en Play sin recargar el dominio, así que los static hay que limpiarlos a mano.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pools.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public static Projectile Get(Projectile prefab)
        {
            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                Pools[prefab] = pool;
            }

            return pool.Get();
        }

        internal static void Clear() => Pools.Clear();

        static ObjectPool<Projectile> CreatePool(Projectile prefab)
        {
            ObjectPool<Projectile> pool = null;
            pool = new ObjectPool<Projectile>(
                createFunc: () =>
                {
                    var projectile = Object.Instantiate(prefab);
                    projectile.SetPool(pool);
                    return projectile;
                },
                actionOnGet: projectile => projectile.gameObject.SetActive(true),
                actionOnRelease: projectile => projectile.gameObject.SetActive(false),
                actionOnDestroy: projectile =>
                {
                    if (projectile != null)
                        Object.Destroy(projectile.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 32,
                maxSize: 512);
            return pool;
        }

        // Los proyectiles viven en la escena; al descargarla ya están destruidos.
        static void OnSceneUnloaded(Scene _) => Pools.Clear();
    }
}
