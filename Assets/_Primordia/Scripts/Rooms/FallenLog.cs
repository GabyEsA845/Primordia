using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Tronco del Tronco caído (piso 3): un Lepidodendron derribado que cruza el lago de lodo profundo por dos filas de casillas y es el
    /// paso seguro. Se monta con la cepa de la izquierda, la copa de la derecha y, entre ellas, tramos de 16 px: en la orilla, apoyados;
    /// en el lago, con el lodo que sube y baja en reposo (todos con el mismo reloj). Al pisar una casilla del lago el lodo se hunde por
    /// los costados (<c>Step</c>) y en las dos vecinas se mueve menos (<c>Dip</c>). Con el jugador dentro suelta burbujas de lodo al azar
    /// junto a los costados. El tronco va por debajo de los personajes y por encima del lodo. No tiene colisión: las casillas del lago
    /// vuelven a ser suelo tras pintarse como lodo (<see cref="RoomTemplate.Mark.Log"/>).
    /// </summary>
    public class FallenLog : MonoBehaviour
    {
        const float IdleFrameSeconds = 0.16f;
        const float StepFrameSeconds = 0.07f;
        const float DipFrameSeconds = 0.09f;
        const float BubbleFrameSeconds = 0.11f;
        const float BigBubbleShare = 0.6f;
        static readonly Vector2 BubbleEvery = new(0.6f, 1.5f);
        const int LogOrder = -6;
        // Casillas que ocupan los extremos (40 px y 48 px de ancho, sobre tramos de 16 px).
        const int LeftEndCells = 2;
        const int RightEndCells = 3;

        class Piece
        {
            public SpriteRenderer renderer;
            public SpecialRoomArt.LogPiece art;
            public float stepAt = -1f;
            public float dipAt = -1f;
        }

        // Un elemento por casilla entre los extremos; null en las de la orilla (apoyadas, sin animación).
        readonly List<Piece> pieces = new();
        float originX;
        float bottomY;
        int columns;
        SpecialRoomArt art;
        RoomPresence presence;
        Transform player;
        int lastColumn = -1;
        float nextBubble;

        /// <summary>
        /// Monta el tronco. <paramref name="firstX"/> y <paramref name="lastX"/> son la primera y la última casilla del tronco (la cepa y la
        /// copa, dentro de ellas); <paramref name="lake"/> dice si cada casilla (por x del mundo) es de lago. <paramref name="bottom"/> es la y
        /// del borde de abajo de la casilla de abajo. Null si el arte no tiene el tronco de ese tramo.
        /// </summary>
        internal static FallenLog Create(Transform parent, SpecialRoomArt art, int band, int firstX, int lastX, float bottom,
            System.Func<int, bool> lake, RoomPresence presence, int seed)
        {
            var bandArt = art != null ? art.LogFor(band) : null;
            if (bandArt == null || bandArt.mid.Length == 0 || bandArt.endLeft == null || bandArt.endRight == null)
                return null;

            var go = new GameObject("FallenLog");
            go.transform.SetParent(parent, false);
            var log = go.AddComponent<FallenLog>();
            log.art = art;
            log.presence = presence;
            log.originX = firstX;
            log.bottomY = bottom;
            log.columns = lastX - firstX + 1;

            var random = new System.Random(seed);
            int previous = -1;
            for (int x = firstX + LeftEndCells; x <= lastX - RightEndCells; x++)
            {
                if (lake(x))
                {
                    // Muchos tramos 0, algunos 3 y de vez en cuando los demás, sin dos iguales seguidos.
                    int variant = PickVariant(random, bandArt.mid.Length);
                    if (variant == previous && bandArt.mid.Length > 1)
                        variant = (variant + 1 + random.Next(bandArt.mid.Length - 1)) % bandArt.mid.Length;
                    previous = variant;
                    var piece = bandArt.mid[variant];
                    var renderer = log.NewRenderer(x, piece.idle.frames.Length > 0 ? piece.idle.frames[0] : null, LogOrder);
                    log.pieces.Add(new Piece { renderer = renderer, art = piece });
                }
                else
                {
                    if (bandArt.ground.Length > 0)
                        log.NewRenderer(x, bandArt.ground[random.Next(bandArt.ground.Length)], LogOrder);
                    log.pieces.Add(null);
                }
            }

            // Los extremos van encima de los tramos que se les cuelan.
            log.NewRenderer(firstX, bandArt.endLeft, LogOrder + 1);
            log.NewRenderer(lastX - RightEndCells + 1, bandArt.endRight, LogOrder + 1);
            return log;
        }

        static int PickVariant(System.Random random, int count)
        {
            int[] weights = { 40, 10, 10, 20, 10, 10 };
            int total = 0;
            for (int i = 0; i < count; i++)
                total += weights[i % weights.Length];
            int roll = random.Next(total);
            for (int i = 0; i < count; i++)
            {
                roll -= weights[i % weights.Length];
                if (roll < 0)
                    return i;
            }
            return 0;
        }

        SpriteRenderer NewRenderer(int x, Sprite sprite, int order)
        {
            var child = new GameObject("LogPiece");
            child.transform.SetParent(transform, false);
            child.transform.position = new Vector3(x, bottomY, 0f);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>Pisa la casilla de esa columna (desde la primera del tronco): hunde esa y mueve las dos vecinas.</summary>
        internal void Step(int column)
        {
            int index = column - LeftEndCells;
            if (PieceAt(index) is not { } stepped)
                return;
            stepped.stepAt = Time.time;
            if (PieceAt(index - 1) is { } before)
                before.dipAt = Time.time;
            if (PieceAt(index + 1) is { } after)
                after.dipAt = Time.time;
        }

        Piece PieceAt(int index) => index >= 0 && index < pieces.Count ? pieces[index] : null;

        void Update()
        {
            if (presence != null && !presence.PlayerInside)
                return;
            float now = Time.time;
            foreach (var piece in pieces)
            {
                if (piece == null)
                    continue;
                var sprite = Frame(piece.art.step.frames, now - piece.stepAt, StepFrameSeconds, piece.stepAt)
                    ?? Frame(piece.art.dip.frames, now - piece.dipAt, DipFrameSeconds, piece.dipAt);
                if (sprite == null)
                {
                    var idle = piece.art.idle.frames;
                    sprite = idle.Length > 0 ? idle[Mathf.FloorToInt(now / IdleFrameSeconds) % idle.Length] : null;
                }
                piece.renderer.sprite = sprite;
            }

            TrackPlayer();
            if (now >= nextBubble)
            {
                nextBubble = now + Random.Range(BubbleEvery.x, BubbleEvery.y);
                StartCoroutine(Bubble());
            }
        }

        static Sprite Frame(Sprite[] frames, float elapsed, float seconds, float startedAt)
        {
            if (startedAt < 0f)
                return null;
            int index = Mathf.FloorToInt(elapsed / seconds);
            return index >= 0 && index < frames.Length ? frames[index] : null;
        }

        // Cada vez que el jugador pisa una casilla nueva del tronco (a la altura de sus dos filas), la hunde.
        void TrackPlayer()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }
            var feet = (Vector2)player.position;
            bool onLog = feet.y > bottomY - 0.3f && feet.y < bottomY + 2.3f;
            int column = onLog ? Mathf.FloorToInt(feet.x - originX) : -1;
            if (column != lastColumn && column >= 0 && column < columns)
                Step(column);
            lastColumn = column;
        }

        // Una burbuja de lodo (la grande el 60 %) en un punto de lodo a menos de una casilla de los costados del tronco.
        IEnumerator Bubble()
        {
            bool big = Random.value < BigBubbleShare;
            var frames = big ? art.mudBubble : art.mudBubbleSmall;
            if (frames.Length == 0)
                yield break;

            Vector2 spot = default;
            bool found = false;
            for (int attempt = 0; attempt < 8 && !found; attempt++)
            {
                float x = originX + LeftEndCells + Random.value * Mathf.Max(1, columns - LeftEndCells - RightEndCells);
                bool above = Random.value < 0.5f;
                float y = above ? bottomY + 2f + Random.value : bottomY - Random.value;
                spot = new Vector2(x, y);
                found = Pits.IsMud(spot);
            }
            if (!found)
                yield break;

            var go = new GameObject("MudBubble");
            go.transform.SetParent(transform, false);
            go.transform.position = spot;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = LogOrder + 2;
            foreach (var frame in frames)
            {
                renderer.sprite = frame;
                yield return new WaitForSeconds(BubbleFrameSeconds);
            }
            Destroy(go);
        }
    }
}
