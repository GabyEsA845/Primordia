using System.Collections.Generic;
using Primordia.Mutations;
using Primordia.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Retrato del jugador en la interfaz (lo muestra <see cref="CreaturePanel"/>), mirando hacia abajo en reposo y con sus mutaciones, ampliado a la
    /// mayor escala entera (como mucho <see cref="maxScale"/>) a la que cabe en <see cref="fitSize"/>.
    /// Sigue animado en la pausa: cada pieza tiene un "títere" oculto (SpriteRenderer apagado + Animator en
    /// tiempo real) con las mismas animaciones que el jugador, y su cuadro se copia a una imagen de la interfaz.
    /// Da también el punto de cada ranura sobre la criatura, para las líneas de la pausa.
    /// </summary>
    public class CreaturePortrait : MonoBehaviour
    {
        enum Part
        {
            Limbs,
            Body,
            Organ,
            Mouth
        }

        const string FacingDown = "Idle_down";
        const string Idle = "Idle";
        // Píxeles del arte del jugador por unidad.
        const float PixelsPerUnit = 16f;

        [Tooltip("Lado del cuadrado (en píxeles de la interfaz) en el que debe caber la criatura.")]
        [SerializeField, Min(1f)] float fitSize = 95f;
        [SerializeField, Min(1)] int maxScale = 3;
        [Tooltip("Distancia del centro a la Boca sin Piel, en píxeles del arte (la de MutationLook).")]
        [SerializeField, Min(0f)] float cellMouthDistance = 6f;

        class Piece
        {
            public Animator Puppet;
            public SpriteRenderer Source;
            public Image Image;
            public bool Active;
            // Dónde va el pivote del sprite, en píxeles del arte, con y hacia arriba.
            public Vector2 Offset;
            public float Rotation;
        }

        readonly Piece[] pieces = new Piece[4];
        readonly Dictionary<MutationSlot, Vector2> anchors = new();
        Vector2 center;

        /// <summary>Escala entera a la que se dibuja ahora.</summary>
        public int Scale { get; private set; } = 1;

        /// <summary>Se lanza al rehacer el retrato (puede haber cambiado la escala y los puntos de las ranuras).</summary>
        public event System.Action Rebuilt;

        void Awake() => EnsurePieces();

        void LateUpdate()
        {
            if (pieces[0] == null)
                return;

            foreach (var piece in pieces)
                CopyFrame(piece);
        }

        // Se crean al primer uso: quien lo muestra puede activarse antes que este objeto.
        void EnsurePieces()
        {
            if (pieces[0] != null)
                return;

            // Hijos en orden de dibujo: Extremidades, cuerpo, Órgano y Boca (mirando abajo, delante).
            for (int i = 0; i < pieces.Length; i++)
                pieces[i] = CreatePiece(((Part)i).ToString());
        }

        /// <summary>Punto de la ranura sobre la criatura, en píxeles locales de este objeto (y hacia arriba).</summary>
        public Vector2 AnchorOf(MutationSlot slot) => anchors.TryGetValue(slot, out var point) ? point : Vector2.zero;

        internal Image ImageOf(MutationSlot slot)
        {
            EnsurePieces();
            return slot switch
            {
                MutationSlot.Limbs => pieces[(int)Part.Limbs].Image,
                MutationSlot.Organ => pieces[(int)Part.Organ].Image,
                MutationSlot.Mouth => pieces[(int)Part.Mouth].Image,
                _ => pieces[(int)Part.Body].Image
            };
        }

        public void Show(PlayerMutations player)
        {
            EnsurePieces();
            var bodyAnimator = player != null && player.TryGetComponent<PlayerAnimator>(out var playerAnimator) ? playerAnimator.BodyAnimator : null;
            float mouthDistance = cellMouthDistance;
            if (player != null && player.TryGetComponent<MutationLook>(out var look) && look.MouthDistance > 0f)
                mouthDistance = look.MouthDistance * PixelsPerUnit;

            Setup(Part.Body, bodyAnimator != null ? bodyAnimator.runtimeAnimatorController : null, FacingDown, Vector2.zero, 0f);
            Setup(Part.Limbs, LookOf(player, MutationSlot.Limbs), FacingDown, Vector2.zero, 0f);
            Setup(Part.Organ, LookOf(player, MutationSlot.Organ), Idle, Vector2.zero, 0f);
            // La Boca está dibujada mirando a la derecha: girada −90° apunta hacia abajo.
            Setup(Part.Mouth, LookOf(player, MutationSlot.Mouth), Idle, new Vector2(0f, -mouthDistance), -90f);

            Fit();
            foreach (var piece in pieces)
                CopyFrame(piece);
            Rebuilt?.Invoke();
        }

        static RuntimeAnimatorController LookOf(PlayerMutations player, MutationSlot slot)
        {
            var mutation = player != null ? player.Get(slot) : null;
            return mutation != null ? mutation.Look : null;
        }

        void Setup(Part part, RuntimeAnimatorController controller, string state, Vector2 offset, float rotation)
        {
            var piece = pieces[(int)part];
            piece.Offset = offset;
            piece.Rotation = rotation;
            piece.Puppet.runtimeAnimatorController = controller;
            piece.Source.sprite = null;

            int hash = Animator.StringToHash(state);
            piece.Active = controller != null && piece.Puppet.HasState(0, hash);
            if (piece.Active)
            {
                piece.Puppet.Play(hash, 0, 0f);
                // Pone ya el primer cuadro, para medir la criatura.
                piece.Puppet.Update(0f);
            }
        }

        /// <summary>Mide la criatura en su primer cuadro, elige la escala y la centra.</summary>
        void Fit()
        {
            var boxes = new Rect?[pieces.Length];
            Rect? all = null;
            for (int i = 0; i < pieces.Length; i++)
            {
                boxes[i] = Bounds(pieces[i]);
                if (boxes[i] is Rect box)
                    all = all is Rect sum ? Union(sum, box) : box;
            }

            var creature = all ?? new Rect(-8f, -8f, 16f, 16f);
            Scale = maxScale;
            while (Scale > 1 && Mathf.Max(creature.width, creature.height) * Scale > fitSize)
                Scale--;

            // Centrada en su caja, en píxeles enteros del arte.
            center = new Vector2(Mathf.Round(creature.center.x), Mathf.Round(creature.center.y));

            var body = boxes[(int)Part.Body] ?? new Rect(-8f, -8f, 16f, 16f);
            anchors[MutationSlot.Skin] = new Vector2(0f, body.yMax - 1f);
            anchors[MutationSlot.Organ] = Vector2.zero;
            anchors[MutationSlot.Mouth] = boxes[(int)Part.Mouth] is Rect mouth
                ? new Vector2(mouth.xMin + 1f, mouth.center.y)
                : new Vector2(body.xMin + 1f, body.center.y - 2f);
            anchors[MutationSlot.Limbs] = pieces[(int)Part.Limbs].Active && LowestRightPoint(pieces[(int)Part.Limbs]) is Vector2 tip
                ? tip
                : new Vector2(body.xMax - 3f, body.yMin + 1f);

            foreach (var slot in new List<MutationSlot>(anchors.Keys))
            {
                var point = (anchors[slot] - center) * Scale;
                anchors[slot] = new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
            }
        }

        /// <summary>Caja de lo que se ve del sprite (su contorno ajustado), en píxeles del arte respecto al centro.</summary>
        static Rect? Bounds(Piece piece)
        {
            var points = Outline(piece);
            if (points.Count == 0)
                return null;

            Vector2 min = points[0], max = points[0];
            foreach (var p in points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>El punto más bajo de la mitad derecha de la pieza (la punta de la aleta o pata derecha).</summary>
        static Vector2? LowestRightPoint(Piece piece)
        {
            Vector2? best = null;
            foreach (var p in Outline(piece))
            {
                if (p.x > 0f && (best == null || p.y < best.Value.y))
                    best = p;
            }
            return best == null ? null : new Vector2(Mathf.Round(best.Value.x) - 1f, Mathf.Round(best.Value.y) + 1f);
        }

        static List<Vector2> Outline(Piece piece)
        {
            var result = new List<Vector2>();
            var sprite = piece.Active ? piece.Source.sprite : null;
            if (sprite == null)
                return result;

            var rotation = Quaternion.Euler(0f, 0f, piece.Rotation);
            foreach (var vertex in sprite.vertices)
                result.Add((Vector2)(rotation * (vertex * sprite.pixelsPerUnit)) + piece.Offset);
            return result;
        }

        static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        void CopyFrame(Piece piece)
        {
            var sprite = piece.Active ? piece.Source.sprite : null;
            piece.Image.enabled = sprite != null;
            if (sprite == null)
                return;

            if (piece.Image.sprite != sprite)
            {
                piece.Image.sprite = sprite;
                var rect = piece.Image.rectTransform;
                rect.sizeDelta = sprite.rect.size * Scale;
                rect.pivot = sprite.pivot / sprite.rect.size;
            }

            var image = piece.Image.rectTransform;
            image.anchoredPosition = (piece.Offset - center) * Scale;
            image.localRotation = Quaternion.Euler(0f, 0f, piece.Rotation);
        }

        Piece CreatePiece(string name)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;

            // El títere no se dibuja: solo su Animator elige el cuadro, también en la pausa.
            var puppet = new GameObject($"{name}Puppet");
            puppet.transform.SetParent(transform, false);
            var source = puppet.AddComponent<SpriteRenderer>();
            source.enabled = false;
            var animator = puppet.AddComponent<Animator>();
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            return new Piece { Puppet = animator, Source = source, Image = image };
        }
    }
}
