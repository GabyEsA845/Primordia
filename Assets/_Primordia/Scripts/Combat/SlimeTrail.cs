using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// La banda de baba que deja la bola del Escupitajo venenoso al rodar: una fila de sellos pegados al suelo (borde y cuerpo en dos
    /// capas, así la unión no deja costuras) que nacen gruesos junto a la bola y se secan desde la punta, cada uno a los segundos del
    /// ataque desde que se pinta. La banda es continua: a lo largo de toda ella frena y envenena como un charco
    /// (<see cref="PoisonPuddle.Affect"/>), y arde (cada sello es <see cref="IFlammable"/>). Vive hasta que se seca el último sello.
    /// </summary>
    public class SlimeTrail : MonoBehaviour
    {
        // Distancia habitual entre sellos (la banda fina de la Camada va con el doble de densidad).
        public const float Spacing = 0.25f;
        public const float ThinSpacing = 0.125f;

        // Vida de un sello: 0 y 1 al nacer, 2 húmedo, 3-7 se seca en los últimos 0,5 s.
        const float BornFrameSeconds = 0.09f;
        const float DryFrameSeconds = 0.1f;
        const int DryStartFrame = 3;
        const int DryFrames = ReactionKit.SlimeFrames - DryStartFrame;

        // Variantes poco frecuentes (el goterón ancho de la banda normal, la última de la fina) y brillo húmedo.
        const float RareChance = 0.08f;
        const float RareChanceThin = 0.12f;
        const float ShineChance = 0.25f;
        const float ShineFrameSeconds = 0.14f;
        const float ShineOffset = 2f / 16f;

        // Quién está sobre la banda: se comprueba un sello de cada Stride, con este radio, cada CheckEvery.
        const float CheckRadius = 0.4f;
        const float ThinCheckRadius = 0.3f;
        const float CheckSpan = 0.5f;

        const int RimOrder = 3;
        const int BodyOrder = 4;
        const int ShineOrder = 5;
        const int ClotOrder = 6;
        // Sanguijuela: de cada tanto sello lleva un coágulo, de otros un gusanillo que nada por la banda.
        const float ClotChance = 0.2f;
        const float WormChance = 0.2f;
        const float WormFrameSeconds = 0.11f;
        // Separación en z para que, dentro de cada capa, el sello más nuevo quede encima.
        const float DepthStep = 0.00025f;
        const int DepthCycle = 4000;

        sealed class Stamp : IFlammable
        {
            public SlimeTrail Owner;
            public int Serial;
            public int Variant;
            public Vector2 Position;
            public float Born;
            public float Dies;
            public SpriteRenderer Rim;
            public SpriteRenderer Body;
            public SpriteRenderer Shine;
            public SpriteRenderer Clot;
            public int ClotVariant;
            public SpriteRenderer Worm;
            public bool Gone;

            Vector2 IFlammable.Position => Position;
            public bool CanIgnite => !Gone && Owner != null;
            public void Ignite() => Owner.Burn(this);
        }

        static readonly List<Collider2D> hits = new();

        static ContactFilter2D AnyCollider() => ContactFilter2D.noFilter;

        readonly List<Stamp> stamps = new();
        readonly HashSet<Health> seen = new();
        ReactionKit kit;
        bool thin;
        bool leech;
        bool harmsPlayer;
        float seconds;
        float spacing;
        bool painting = true;
        Vector2 last;
        float carry;
        int serial;
        float phase;
        float nextCheck;

        /// <summary>Sellos vivos ahora.</summary>
        public int StampCount => stamps.Count;

        /// <summary>
        /// Empieza una banda en ese punto del suelo (con un sello). <paramref name="thin"/>: la banda fina de las bolitas; <paramref name="spacing"/>: unidades entre sellos.
        /// Null si no hay arte de la baba.
        /// </summary>
        public static SlimeTrail Begin(Vector2 position, Vector2 direction, bool thin, float spacing, float seconds, bool harmsPlayer,
            bool leech = false)
        {
            var kit = ReactionKit.Current;
            if (kit == null || !kit.HasSlime)
                return null;
            // Sanguijuela: banda rojiza que no arde; sin su arte, la normal.
            leech &= kit.HasLeechSlime;

            var trail = new GameObject(thin ? "SlimeTrailThin" : "SlimeTrail").AddComponent<SlimeTrail>();
            trail.kit = kit;
            trail.thin = thin;
            trail.leech = leech;
            trail.harmsPlayer = harmsPlayer;
            trail.seconds = seconds;
            trail.spacing = Mathf.Max(0.05f, spacing);
            trail.phase = Random.value * Mathf.PI * 2f;
            trail.last = position;
            trail.Place(position, direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right);
            return trail;
        }

        /// <summary>La bola está ahora en ese punto: pinta los sellos que falten por el camino.</summary>
        public void Extend(Vector2 position)
        {
            if (!painting)
                return;
            var delta = position - last;
            float length = delta.magnitude;
            if (length < 0.0001f)
                return;

            var direction = delta / length;
            float at = spacing - carry;
            while (at <= length)
            {
                Place(last + direction * at, direction);
                at += spacing;
            }
            carry = length - (at - spacing);
            last = position;
        }

        /// <summary>La bola acabó: no se pinta más y la banda se seca sola.</summary>
        public void Finish() => painting = false;

        /// <summary>Quita la banda de golpe (cambio de sala).</summary>
        public void Clear()
        {
            painting = false;
            for (int i = stamps.Count - 1; i >= 0; i--)
                Remove(i);
            Destroy(gameObject);
        }

        void Place(Vector2 position, Vector2 direction)
        {
            var perpendicular = new Vector2(-direction.y, direction.x);
            float pixels = serial * spacing * 16f;
            float offset = thin
                ? Mathf.Round(1.3f * Mathf.Sin(pixels / 7f + phase) + Random.Range(-0.45f, 0.45f))
                : Random.value < 0.5f ? 0f : Random.value < 0.5f ? -1f : 1f;
            var spot = position + perpendicular * (offset / 16f);
            spot = new Vector2(Mathf.Round(spot.x * 16f) / 16f, Mathf.Round(spot.y * 16f) / 16f);

            int variants = leech ? kit.LeechSlimeVariants(thin) : kit.SlimeVariants(thin);
            bool rare = Random.value < (thin ? RareChanceThin : RareChance);
            int variant = rare ? variants - 1 : Random.Range(0, Mathf.Max(1, variants - 1));

            float depth = -(serial % DepthCycle) * DepthStep;
            var stamp = new Stamp
            {
                Owner = this,
                Serial = serial++,
                Variant = variant,
                Position = spot,
                Born = Time.time,
                Dies = Time.time + seconds,
                Rim = NewRenderer("Rim", spot, depth, RimOrder),
                Body = NewRenderer("Body", spot, depth, BodyOrder),
            };
            // Solo la banda normal brilla: la fina es demasiado estrecha para el brillo.
            if (!thin && Random.value < ShineChance && kit.SlimeShine.Length > 0)
            {
                stamp.Shine = NewRenderer("Shine", spot + new Vector2(-ShineOffset, ShineOffset), depth, ShineOrder);
                stamp.Shine.enabled = false;
            }
            // Solo la banda normal de la Sanguijuela lleva coágulos y gusanillos.
            if (leech && !thin)
            {
                float roll = Random.value;
                var clots = kit.LeechClot;
                if (roll < ClotChance && clots.Length >= ReactionKit.SlimeFrames)
                {
                    stamp.ClotVariant = Random.Range(0, clots.Length / ReactionKit.SlimeFrames);
                    stamp.Clot = NewRenderer("Clot", spot, depth, ClotOrder);
                }
                else if (roll < ClotChance + WormChance && kit.LeechWorm.Length > 0)
                {
                    stamp.Worm = NewRenderer("Worm", spot, depth, ClotOrder);
                    // Dibujado hacia la derecha: se gira con la banda.
                    stamp.Worm.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
            }
            Show(stamp, Time.time);
            stamps.Add(stamp);
            // La sangre no arde: la banda de la Sanguijuela no es inflamable.
            if (!leech)
                Flammables.Register(stamp);
        }

        SpriteRenderer NewRenderer(string label, Vector2 position, float depth, int order)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform, false);
            obj.transform.position = new Vector3(position.x, position.y, depth);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        // Dibuja el cuadro que le toca al sello en este momento.
        void Show(Stamp stamp, float now)
        {
            float age = now - stamp.Born;
            float remaining = stamp.Dies - now;
            int frame;
            if (remaining <= DryFrames * DryFrameSeconds)
                frame = DryStartFrame + Mathf.Clamp(Mathf.FloorToInt((DryFrames * DryFrameSeconds - remaining) / DryFrameSeconds), 0, DryFrames - 1);
            else
                frame = age < BornFrameSeconds ? 0 : age < BornFrameSeconds * 2f ? 1 : 2;

            stamp.Rim.sprite = leech ? kit.LeechSlimeRimFrame(thin, stamp.Variant, frame) : kit.SlimeRimFrame(thin, stamp.Variant, frame);
            stamp.Body.sprite = leech ? kit.LeechSlimeBodyFrame(thin, stamp.Variant, frame) : kit.SlimeBodyFrame(thin, stamp.Variant, frame);
            if (stamp.Clot != null)
                stamp.Clot.sprite = kit.LeechClot[stamp.ClotVariant * ReactionKit.SlimeFrames + frame];
            if (stamp.Worm != null)
            {
                // Nada mientras el sello está húmedo (cuadros 0-2), se hunde al empezar a secarse (3-5) y desaparece.
                var sink = kit.LeechWormSink;
                if (frame <= 2)
                {
                    var swim = kit.LeechWorm;
                    stamp.Worm.enabled = true;
                    stamp.Worm.sprite = swim[Mathf.FloorToInt(now / WormFrameSeconds) % swim.Length];
                }
                else if (frame <= 5 && sink.Length > 0)
                {
                    stamp.Worm.enabled = true;
                    stamp.Worm.sprite = sink[Mathf.Clamp(frame - 3, 0, sink.Length - 1)];
                }
                else
                {
                    stamp.Worm.enabled = false;
                }
            }
            if (stamp.Shine != null)
            {
                var shine = leech ? kit.LeechSlimeShine : kit.SlimeShine;
                stamp.Shine.enabled = frame == 2;
                stamp.Shine.sprite = shine[Mathf.FloorToInt(now / ShineFrameSeconds) % shine.Length];
            }
        }

        void Update()
        {
            float now = Time.time;
            for (int i = 0; i < stamps.Count; i++)
            {
                if (stamps[i].Dies <= now)
                {
                    Remove(i--);
                    continue;
                }
                Show(stamps[i], now);
            }

            if (now >= nextCheck)
            {
                nextCheck = now + PoisonPuddle.CheckEvery;
                Poison();
            }

            if (!painting && stamps.Count == 0)
                Destroy(gameObject);
        }

        // La banda frena y envenena a lo que la pisa: se mira un sello de cada tanto, con radio de sobra para que no queden huecos.
        void Poison()
        {
            int stride = Mathf.Max(1, Mathf.RoundToInt(CheckSpan / spacing));
            float radius = thin ? ThinCheckRadius : CheckRadius;
            seen.Clear();
            for (int i = 0; i < stamps.Count; i += stride)
            {
                hits.Clear();
                Physics2D.OverlapCircle(stamps[i].Position, radius, AnyCollider(), hits);
                foreach (var hit in hits)
                    PoisonPuddle.Affect(hit, harmsPlayer, seen);
            }
        }

        // El fuego se come el sello; de cada tanto sale una nube tóxica, como con los charcos sueltos de antes.
        void Burn(Stamp stamp)
        {
            int index = stamps.IndexOf(stamp);
            if (index < 0)
                return;
            int cloudEvery = Mathf.Max(1, Mathf.RoundToInt(CheckSpan / spacing));
            Vector2 position = stamp.Position;
            bool cloud = stamp.Serial % cloudEvery == 0;
            Remove(index);
            if (cloud)
                ToxicCloud.Spawn(position);
        }

        void Remove(int index)
        {
            var stamp = stamps[index];
            stamps.RemoveAt(index);
            stamp.Gone = true;
            Flammables.Unregister(stamp);
            if (stamp.Rim != null) Destroy(stamp.Rim.gameObject);
            if (stamp.Body != null) Destroy(stamp.Body.gameObject);
            if (stamp.Shine != null) Destroy(stamp.Shine.gameObject);
            if (stamp.Clot != null) Destroy(stamp.Clot.gameObject);
            if (stamp.Worm != null) Destroy(stamp.Worm.gameObject);
        }

        void OnDestroy()
        {
            foreach (var stamp in stamps)
                Flammables.Unregister(stamp);
        }
    }
}
