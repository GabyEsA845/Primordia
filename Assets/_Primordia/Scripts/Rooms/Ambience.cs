using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Primordia.Rooms
{
    /// <summary>
    /// Ambiente de las salas del piso 2, que monta <see cref="FloorBuilder"/>: luces 2D de la bioluminiscencia (con la luz
    /// global que hace falta para que lo demás no quede a oscuras), rayos de luz animados y nieve marina.
    /// </summary>
    public static class Ambience
    {
        // Por encima de los personajes (0) y de los efectos, por debajo del HUD (que va en su lienzo).
        const int LightRayOrder = 40;
        const int MarineSnowOrder = 30;

        // Copos: 8 px a 16 px por unidad, cayendo despacio y en diagonal.
        const float SnowSize = 0.5f;
        static readonly Vector2 SnowLifetime = new(10f, 16f);
        static readonly Vector2 SnowFall = new(-0.35f, -0.15f);
        static readonly Vector2 SnowDrift = new(-0.12f, 0.06f);

        /// <summary>
        /// Luz global de intensidad 1: con alguna luz 2D en escena, lo que no ilumina ninguna quedaría negro. Las escenas hechas
        /// con la plantilla Lit2D ya traen una (y dos a la vez dan error): entonces no se crea y devuelve la que hay.
        /// </summary>
        public static Light2D CreateGlobalLight(Transform parent)
        {
            // Sin isActiveAndEnabled: el piso se monta en el Awake del generador, al cargar la escena, cuando la luz de la escena
            // aún no ha pasado por su OnEnable y esa propiedad todavía dice false.
            foreach (var existing in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude))
            {
                if (existing.lightType == Light2D.LightType.Global && existing.enabled && existing.gameObject.activeInHierarchy)
                    return existing;
            }

            var light = CreateLight(parent, "GlobalLight", Vector2.zero, Light2D.LightType.Global);
            light.intensity = 1f;
            light.color = Color.white;
            light.gameObject.SetActive(true);
            return light;
        }

        /// <summary>Luz suave de una pieza que brilla, con su latido si lo tiene.</summary>
        public static Light2D CreateGlow(Transform parent, Vector2 position, Glow glow, float phase)
        {
            var light = CreateLight(parent, "Glow", position, Light2D.LightType.Point);
            light.color = glow.color;
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = glow.radius;
            light.falloffIntensity = 0.7f;
            light.intensity = glow.intensity.y;
            if (glow.period > 0f && !Mathf.Approximately(glow.intensity.x, glow.intensity.y))
                light.gameObject.AddComponent<GlowPulse>().Configure(light, glow.intensity, glow.period, phase);
            light.gameObject.SetActive(true);
            return light;
        }

        // Se crea desactivada para que la luz se registre con su tipo ya puesto al activarse.
        static Light2D CreateLight(Transform parent, string name, Vector2 position, Light2D.LightType type)
        {
            var lightObject = new GameObject(name);
            lightObject.SetActive(false);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = type;
            return light;
        }

        /// <summary>Rayo de luz que se mece, con la parte de arriba en <paramref name="top"/>.</summary>
        public static SpriteLoop CreateLightRay(Transform parent, Vector2 top, Sprite[] frames, float frameDuration, float phase)
        {
            var ray = new GameObject("LightRay");
            ray.transform.SetParent(parent, false);
            var renderer = ray.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = LightRayOrder;
            renderer.sprite = frames[0];
            float height = frames[0].bounds.size.y;
            ray.transform.position = top + Vector2.down * (height / 2f);
            var loop = ray.AddComponent<SpriteLoop>();
            loop.Configure(renderer, frames, frameDuration, phase);
            return loop;
        }

        /// <summary>
        /// Nieve marina sobre <paramref name="area"/> (en unidades del mundo): unos <paramref name="count"/> copos a la vez que
        /// caen despacio y en diagonal, ya repartidos por la sala al entrar.
        /// </summary>
        public static ParticleSystem CreateMarineSnow(Transform parent, Rect area, int count, Sprite[] flakes, Material material)
        {
            var snowObject = new GameObject("MarineSnow");
            snowObject.SetActive(false);
            snowObject.transform.SetParent(parent, false);
            // Nace en una franja algo más alta que la sala para que no se vea el borde de donde salen.
            snowObject.transform.position = new Vector3(area.center.x, area.center.y + 1f, 0f);

            var system = snowObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(SnowLifetime.x, SnowLifetime.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(SnowSize * 0.6f, SnowSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.Max(1, count);

            var emission = system.emission;
            emission.rateOverTime = count / ((SnowLifetime.x + SnowLifetime.y) / 2f);

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.width, area.height + 2f, 0f);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(SnowDrift.x, SnowDrift.y);
            velocity.y = new ParticleSystem.MinMaxCurve(SnowFall.x, SnowFall.y);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Aparecen y se apagan poco a poco.
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.15f), new GradientAlphaKey(0.75f, 0.8f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            // Un copo distinto por partícula (las variantes de la hoja), sin animación.
            if (flakes != null && flakes.Length > 0)
            {
                var sheet = system.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                foreach (var flake in flakes)
                    sheet.AddSprite(flake);
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, flakes.Length - 0.01f);
            }

            var renderer = snowObject.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = MarineSnowOrder;
            if (material != null)
                renderer.sharedMaterial = material;

            snowObject.SetActive(true);
            system.Play();
            return system;
        }
    }
}
