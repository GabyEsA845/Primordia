#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Tests
{
    /// <summary>
    /// Prueba de banco del límite de daño por fuente de los jefes: reproduce el ritmo de daño de cada Boca (números de
    /// Floor3MouthsSetup) contra un jefe quieto durante una ventana de daño, con y sin el límite, y escribe la tabla en el log.
    /// Es un techo (todo acierta): sirve para comparar bocas entre sí, no para medir puntería.
    /// </summary>
    public class BossDamageBenchTests
    {
        const int BossHealth = 450;
        const float Window = 4f;

        struct Event
        {
            public float time;
            public int amount;
            public DamageSource source;
        }

        static List<Event> Jet(int level)
        {
            var events = new List<Event>();
            float bar = level == 1 ? 3f : 4f;
            for (float t = 0f; t < Mathf.Min(Window, bar); t += 0.15f)
                events.Add(new Event { time = t, amount = 1, source = DamageSource.Jet });
            if (level == 3)
            {
                for (float t = 0f; t < Mathf.Min(Window, bar); t += 0.5f)
                    events.Add(new Event { time = t, amount = 1, source = DamageSource.Jet });
            }
            return events;
        }

        static List<Event> Spin(int level)
        {
            var events = new List<Event>();
            float seconds = level == 1 ? 3f : 4f;
            int damage = level == 1 ? 2 : 3;
            for (float t = 0f; t < Mathf.Min(Window, seconds); t += 0.25f)
                events.Add(new Event { time = t, amount = damage, source = DamageSource.Spin });
            return events;
        }

        static List<Event> Spit(int level)
        {
            var events = new List<Event>();
            float rate = level == 1 ? 2.8f : 2.4f;
            int burst = level == 1 ? 6 : 8;
            // La bola da 1 al atravesar y el estallido llega al final de su vuelo; el charco y el estallido dejan veneno virulento.
            for (float t = 0.5f; t < Window; t += rate)
            {
                events.Add(new Event { time = t, amount = 1, source = DamageSource.Spit });
                events.Add(new Event { time = t + 0.1f, amount = burst, source = DamageSource.Spit });
            }
            // Veneno virulento tras el estallido: 2 cada 0,5 s (el jefe solo lo sufre mientras es vulnerable).
            for (float t = 0.7f; t < Window; t += 0.5f)
                events.Add(new Event { time = t, amount = 2, source = DamageSource.Venom });
            return events;
        }

        static (int uncapped, int capped) Run(List<Event> events)
        {
            var go = new GameObject("BenchBoss");
            try
            {
                go.AddComponent<Health>();
                var limiter = go.AddComponent<BossDamageLimiter>();
                int uncapped = 0, capped = 0;
                events.Sort((a, b) => a.time.CompareTo(b.time));
                foreach (var e in events)
                {
                    uncapped += e.amount;
                    capped += limiter.Apply(e.amount, e.source, e.time);
                }
                return (uncapped, capped);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TablaDeDanoPorVentanaDeCadaBoca()
        {
            var table = new StringBuilder($"Daño al jefe en una ventana de {Window} s (vida {BossHealth}), todo acierta:\n");
            var rows = new Dictionary<string, (int uncapped, int capped)>();
            for (int level = 1; level <= 3; level++)
            {
                rows[$"Chorro {level}"] = Run(Jet(level));
                rows[$"Peonza {level}"] = Run(Spin(level));
                rows[$"Escupitajo {level}"] = Run(Spit(level));
            }
            foreach (var row in rows)
                table.AppendLine($"  {row.Key,-14} sin límite {row.Value.uncapped,3}  con límite {row.Value.capped,3}  → ventanas para matar: {Mathf.CeilToInt((float)BossHealth / row.Value.capped)}");
            Debug.Log(table.ToString());

            // El límite solo recorta: nunca sube el daño ni deja a una Boca en cero.
            foreach (var row in rows)
            {
                Assert.LessOrEqual(row.Value.capped, row.Value.uncapped, row.Key);
                Assert.Greater(row.Value.capped, 0, row.Key);
            }
            // El Chorro no puede superar su tope en ninguna ventana de un segundo.
            Assert.LessOrEqual(rows["Chorro 3"].capped, 5 * Window);
        }

        [Test]
        public void ElLimiteRecortaElSobrantePorSegundoYSeRenueva()
        {
            var go = new GameObject("BenchBoss");
            try
            {
                go.AddComponent<Health>();
                var limiter = go.AddComponent<BossDamageLimiter>();
                int first = 0;
                for (int i = 0; i < 10; i++)
                    first += limiter.Apply(1, DamageSource.Jet, i * 0.05f);
                Assert.AreEqual(5, first, "el chorro admite 5 por segundo");
                Assert.AreEqual(1, limiter.Apply(1, DamageSource.Jet, 1.2f), "pasado el segundo se renueva");
                Assert.AreEqual(7, limiter.Apply(7, DamageSource.Unknown, 1.2f), "sin etiquetar no se limita");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ElGateDeLaVidaAplicaElLimite()
        {
            var go = new GameObject("BenchBoss");
            try
            {
                var health = go.AddComponent<Health>();
                health.SetMax(450, healDifference: false);
                health.ResetHealth();
                go.AddComponent<BossDamageLimiter>();
                int before = health.Current;
                for (int i = 0; i < 40; i++)
                    health.TakeDamage(1, DamageKind.Hit, DamageSource.Jet);
                Assert.AreEqual(5, before - health.Current);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif