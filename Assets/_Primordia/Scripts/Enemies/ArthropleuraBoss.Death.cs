using Primordia.Audio;
using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Arthropleura, subpaso E2: la muerte. <see cref="Enemy"/> destruye el objeto del jefe tras <c>Died</c>, así que el cadáver es un objeto
    /// aparte en la sala: la cabeza hace su espasmo, el cuerpo se enrosca en espiral y cada pieza se vuelve gris de la cola a la cabeza
    /// con humo. Se queda de escenario (con colisión) el resto del combate.
    /// </summary>
    public partial class ArthropleuraBoss
    {
        [SerializeField] Sprite[] deathSmoke = System.Array.Empty<Sprite>();

        internal void ConfigureDeathSmoke(Sprite[] sprites) => deathSmoke = sprites;

        void BuildCorpse()
        {
            SetMode(Mode.Dead);
            // Después de morir, el cadáver humea: bucle muy bajo solo cerca de él.
            Vector2 corpseAt = GroundToWorld(headPos);
            NearbyLoop.Ensure(Sounds.BossArthropleuraSmolderLoop, from => Vector2.Distance(from, corpseAt), 7f, 0.5f);
            HideAim();
            ClearWaves();
            HideGap();
            ScreenShake.ClearRumble(this);
            ScreenShake.Pulse(6f, 0.8f);
            var go = new GameObject("ArthropleuraCorpse");
            go.transform.SetParent(transform.parent, true);
            go.transform.position = wheelActive ? wheelPos : GroundToWorld(headPos);
            go.layer = wallMap != null ? wallMap.gameObject.layer : 0;
            go.AddComponent<ArthropleuraCorpse>().Configure(art, deathSmoke, pieces.Count);
        }
    }

    /// <summary>
    /// Cadáver independiente del jefe: sobrevive a <see cref="Enemy"/> y se descarta con la sala. Aparece ya enroscado en espiral con las
    /// piezas vivas; cada pieza se vuelve gris de la cola a la cabeza. Cada pieza es sólida (el cuerpo muerto hace de escenario).
    /// </summary>
    internal sealed class ArthropleuraCorpse : MonoBehaviour
    {
        // Provisionales (a ojo).
        const float Pitch = 2.6f, GroundSquash = 0.82f, SpiralStart = 0.8f, SpiralGrowth = 0.32f;
        const float FirstDeath = 0.3f, DeathGap = 0.09f, HeadFrameSeconds = 0.11f, SmokeFrameSeconds = 0.12f;
        const float PieceCollisionRadius = 1.5f, HeadCollisionRadius = 1.8f;
        const int BaseOrder = 10, HeadOrder = 80, SmokeEvery = 6;

        DirectionalArt art;
        Sprite[] smokeFrames = System.Array.Empty<Sprite>();
        SpriteRenderer head;
        readonly List<SpriteRenderer> pieces = new();
        readonly List<float> dieAt = new();
        readonly List<Sprite> deadSprites = new();
        readonly List<bool> dead = new();
        readonly List<SpriteRenderer> smokes = new();
        readonly List<float> smokeStart = new();
        float timer;

        internal int PieceCount => pieces.Count;
        internal int DeadPieces
        {
            get
            {
                int count = 0;
                foreach (var isDead in dead) if (isDead) count++;
                return count;
            }
        }
        internal bool HeadDead { get; private set; }
        internal int ColliderCount => GetComponentsInChildren<Collider2D>().Length;

        internal void Configure(DirectionalArt newArt, Sprite[] newSmoke, int count)
        {
            art = newArt;
            smokeFrames = newSmoke ?? System.Array.Empty<Sprite>();
            Build(count);
        }

        void Build(int count)
        {
            // Espiral de Arquímedes: la cabeza en el centro y el cuerpo saliendo hacia fuera (una pieza cada 2,6 u de arco).
            var points = new List<(Vector2 position, float angle)>();
            float theta = 0f, arc = 0f, next = 0f;
            while (points.Count < count)
            {
                float r = SpiralStart + SpiralGrowth * theta;
                if (arc >= next)
                {
                    var p = new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r * GroundSquash);
                    // Tangente a la espiral, vista en pantalla.
                    var tangent = new Vector2(-Mathf.Sin(theta) * r + Mathf.Cos(theta) * SpiralGrowth,
                        (Mathf.Cos(theta) * r + Mathf.Sin(theta) * SpiralGrowth) * GroundSquash);
                    points.Add((p, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg));
                    next += Pitch;
                }
                const float step = 0.02f;
                arc += Mathf.Sqrt(r * r + SpiralGrowth * SpiralGrowth) * step;
                theta += step;
            }
            var headSheet = art != null ? art.Find("Arthropleura_Head_Death") : null;
            head = NewRenderer("Head", HeadOrder, Vector2.zero, HeadCollisionRadius);
            head.sprite = headSheet?.Get(DirectionalArt.RowFor(-90f, out _), 0);
            // Pieza 29 = cola (la más lejos del centro); la primera en apagarse. La 0 es la cabeza.
            for (int k = count - 1; k >= 1; k--)
            {
                var (position, angle) = points[k];
                bool tail = k == count - 1;
                var deadSheet = art != null ? art.Find(tail ? "Arthropleura_Tail_Dead" : "Arthropleura_Segment_Dead") : null;
                var aliveSheet = art != null ? art.Find(tail ? "Arthropleura_Tail_Idle" : "Arthropleura_Segment_Idle") : null;
                var renderer = NewRenderer("Piece_" + k, BaseOrder + (count - k), position, PieceCollisionRadius);
                int row = DirectionalArt.RowFor(angle, out bool flip);
                renderer.sprite = aliveSheet?.Get(row, 0);
                renderer.flipX = flip;
                pieces.Add(renderer);
                deadSprites.Add(deadSheet?.Get(row, 0));
                dead.Add(false);
                dieAt.Add(FirstDeath + (count - 1 - k) * DeathGap);
                if (smokeFrames.Length > 0 && (count - 1 - k) % SmokeEvery == 2)
                {
                    var smoke = NewRenderer("Smoke_" + k, HeadOrder + 5, position, 0f);
                    smoke.enabled = false;
                    smokes.Add(smoke);
                    smokeStart.Add(dieAt[dieAt.Count - 1]);
                }
            }
        }

        SpriteRenderer NewRenderer(string label, int order, Vector2 local, float colliderRadius)
        {
            var obj = new GameObject(label) { layer = gameObject.layer };
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = local;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            // Escenario sólido: no se atraviesa el cuerpo muerto (un círculo por pieza; se solapan y forman un muro continuo).
            if (colliderRadius > 0f)
                obj.AddComponent<CircleCollider2D>().radius = colliderRadius;
            return renderer;
        }

        void Update()
        {
            timer += Time.deltaTime;
            var deathSheet = art != null ? art.Find("Arthropleura_Head_Death") : null;
            if (deathSheet != null && !HeadDead)
            {
                int frame = Mathf.FloorToInt(timer / HeadFrameSeconds);
                int row = DirectionalArt.RowFor(-90f, out bool flip);
                head.sprite = deathSheet.Get(row, Mathf.Min(frame, deathSheet.frames - 1));
                head.flipX = flip;
                if (frame >= deathSheet.frames)
                {
                    HeadDead = true;
                    var deadHead = art.Find("Arthropleura_Head_Dead");
                    if (deadHead != null) head.sprite = deadHead.Get(row, 0);
                }
            }
            for (int i = 0; i < pieces.Count; i++)
                if (!dead[i] && timer >= dieAt[i])
                {
                    dead[i] = true;
                    if (deadSprites[i] != null) pieces[i].sprite = deadSprites[i];
                }
            for (int i = 0; i < smokes.Count; i++)
            {
                if (timer < smokeStart[i]) continue;
                smokes[i].enabled = true;
                smokes[i].sprite = smokeFrames[Mathf.FloorToInt((timer + i) / SmokeFrameSeconds) % smokeFrames.Length];
            }
        }
    }
}
