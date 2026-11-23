using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>Quién pasa por detrás de una pieza alta: el jugador, para taparlo o volverse translúcida.</summary>
    static class ExitDecorCover
    {
        static Transform player;

        public static bool PlayerBehind(Transform piece, float height, float halfWidth)
        {
            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                player = found != null ? found.transform : null;
            }
            if (player == null)
                return false;
            float feet = player.position.y - 0.3f;
            return feet > piece.position.y + 0.05f && feet < piece.position.y + height
                && Mathf.Abs(player.position.x - piece.position.x) < halfWidth * 0.8f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => player = null;
    }

    /// <summary>
    /// Peñasco o tronco de pie de la sala de paso: bloquea con una caja en la base y, si el jugador pasa por detrás, lo tapa y se
    /// vuelve translúcido para que no se pierda.
    /// </summary>
    public class ExitDecorObstacle : MonoBehaviour
    {
        const int BehindOrder = 3;
        const int InFrontOrder = 12;
        const float SeeThrough = 0.55f;

        SpriteRenderer body;
        float height;
        float halfWidth;

        internal void Configure(SpriteRenderer newBody)
        {
            body = newBody;
            height = body.sprite.bounds.size.y;
            halfWidth = body.sprite.bounds.extents.x;
        }

        void Update()
        {
            if (body == null)
                return;
            bool covers = ExitDecorCover.PlayerBehind(transform, height, halfWidth);
            body.sortingOrder = covers ? InFrontOrder : BehindOrder;
            var color = body.color;
            color.a = covers ? SeeThrough : 1f;
            body.color = color;
        }
    }

    /// <summary>Planta seca de la franja pérmica: se mece en reposo; al pasar una ráfaga se dobla (<see cref="Rustle"/>) y vuelve.</summary>
    public class ExitDecorPlant : MonoBehaviour
    {
        const float SwaySeconds = 0.22f;
        const float RustleSeconds = 0.07f;
        const int BehindOrder = 3;
        const int InFrontOrder = 12;

        SpriteRenderer body;
        Sprite[] sway;
        Sprite[] rustle;
        float phase;
        float rustleStart = float.NegativeInfinity;
        float height;
        float halfWidth;

        public Vector2 Position => transform.position;

        public bool IsRustling => rustle != null && Time.time - rustleStart < rustle.Length * RustleSeconds;

        internal void Configure(SpriteRenderer newBody, Sprite[] newSway, Sprite[] newRustle, float newPhase)
        {
            body = newBody;
            sway = newSway;
            rustle = newRustle;
            phase = newPhase;
            height = sway[0].bounds.size.y;
            halfWidth = sway[0].bounds.extents.x;
            Show();
        }

        /// <summary>Lanza la animación de doblarse a la derecha (si ya se dobla, no la reinicia).</summary>
        public void Rustle()
        {
            if (rustle == null || rustle.Length == 0 || IsRustling)
                return;
            rustleStart = Time.time;
        }

        void Update()
        {
            Show();
            bool covers = ExitDecorCover.PlayerBehind(transform, height, halfWidth);
            body.sortingOrder = covers ? InFrontOrder : BehindOrder;
        }

        void Show()
        {
            if (body == null)
                return;
            if (IsRustling)
                body.sprite = rustle[Mathf.Min(rustle.Length - 1, Mathf.FloorToInt((Time.time - rustleStart) / RustleSeconds))];
            else
            {
                int frame = Mathf.FloorToInt((Time.time + phase) / SwaySeconds);
                body.sprite = sway[((frame % sway.Length) + sway.Length) % sway.Length];
            }
        }
    }

    /// <summary>
    /// Ráfaga de arena: un velo bajo que cruza de izquierda a derecha por una fila de la sala, con fundido al nacer y al
    /// deshacerse, y que hace doblarse a las plantas que roza. Solo decorado: no empuja ni daña.
    /// </summary>
    public class ExitDecorGust : MonoBehaviour
    {
        const float Speed = 6f;
        const float FrameSeconds = 0.08f;
        const float FadeInDistance = 1.2f;
        const float FadeOutSeconds = 0.3f;
        const float RustleReach = 0.8f;

        SpriteRenderer body;
        Sprite[] frames;
        float startX;
        float endX;
        float born;
        List<ExitDecorPlant> plants;
        System.Func<Vector2, float> strength;

        internal void Configure(SpriteRenderer newBody, Sprite[] newFrames, float from, float to, List<ExitDecorPlant> roomPlants,
            System.Func<Vector2, float> newStrength, float delay)
        {
            body = newBody;
            frames = newFrames;
            startX = from;
            endX = to;
            plants = roomPlants;
            strength = newStrength;
            born = Time.time + delay;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            // Las ráfagas que renacen tras un obstáculo esperan a que la anterior llegue hasta él.
            if (Time.time < born)
            {
                body.enabled = false;
                return;
            }
            body.enabled = true;
            float x = startX + (Time.time - born) * Speed;
            if (x >= endX)
            {
                Destroy(gameObject);
                return;
            }

            // El fundido final cubre los últimos 0,3 s de camino: la ráfaga se deshace antes de tocar lo que la para.
            float fadeOut = Mathf.Clamp01((endX - x) / (Speed * FadeOutSeconds));
            float fadeIn = Mathf.Clamp01((x - startX) / FadeInDistance);
            var position = transform.position;
            position.x = x;
            transform.position = position;
            body.sprite = frames[Mathf.FloorToInt((Time.time - born) / FrameSeconds) % frames.Length];
            var color = body.color;
            color.a = Mathf.Min(fadeIn, fadeOut) * strength(position);
            body.color = color;
            foreach (var plant in plants)
                if (plant != null && Mathf.Abs(plant.Position.x - position.x) < RustleReach && Mathf.Abs(plant.Position.y - position.y) < RustleReach)
                    plant.Rustle();
        }
    }

    /// <summary>
    /// Lanza las ráfagas de la sala de paso: una cada 4-7 s por una fila al azar, de la pared de la izquierda hacia la derecha. Cada
    /// fila ya viene partida en tramos libres (sin sumidero ni obstáculos): la ráfaga recorre el primero y, tras un obstáculo, puede
    /// renacer una pequeña.
    /// </summary>
    public class ExitDecorWind : MonoBehaviour
    {
        public struct Run
        {
            public float y, from, to, delay;
            public bool small;
        }

        const float MinSeconds = 4f;
        const float MaxSeconds = 7f;
        const int GustOrder = -4;

        List<List<Run>> rows;
        Sprite[] big;
        Sprite[] small;
        List<ExitDecorPlant> plants;
        System.Func<Vector2, float> strength;
        System.Random random;
        float next;

        public int RowCount => rows?.Count ?? 0;

        internal void Configure(List<List<Run>> newRows, Sprite[] newBig, Sprite[] newSmall, List<ExitDecorPlant> newPlants,
            System.Func<Vector2, float> newStrength, int seed)
        {
            rows = newRows;
            big = newBig;
            small = newSmall;
            plants = newPlants;
            strength = newStrength;
            random = new System.Random(seed);
            next = Time.time + Wait();
        }

        float Wait() => MinSeconds + (float)random.NextDouble() * (MaxSeconds - MinSeconds);

        void Update()
        {
            if (rows == null || rows.Count == 0 || Time.time < next)
                return;
            next = Time.time + Wait();
            Launch(rows[random.Next(rows.Count)]);
        }

        internal void Launch(List<Run> row)
        {
            foreach (var run in row)
            {
                var frames = run.small ? small : big;
                if (frames.Length == 0)
                    continue;
                var gust = new GameObject(run.small ? "SandGustSmall" : "SandGust");
                gust.transform.SetParent(transform, false);
                gust.transform.position = new Vector3(run.from, run.y, 0f);
                var renderer = gust.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = GustOrder;
                renderer.color = new Color(1f, 1f, 1f, 0f);
                gust.AddComponent<ExitDecorGust>().Configure(renderer, frames, run.from, run.to, plants, strength, run.delay);
            }
        }
    }
}
