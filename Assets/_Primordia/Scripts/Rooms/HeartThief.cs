using System.Collections.Generic;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Cucaracha ladrona del almacén de corazones (piso 3): si el jugador sale de una sala dejando en el suelo su medio corazón, la
    /// cucaracha sale de una grieta, corre hasta él, lo engancha, se lo sube al lomo y se mete en otra grieta. No se puede matar (no
    /// tiene vida ni cuerpo). Va girada hacia donde corre; al salir y al esconderse usa las hojas sin girar (volteadas si va a la izquierda).
    /// </summary>
    public class HeartThief : MonoBehaviour
    {
        enum Phase
        {
            Emerge,
            ToLoot,
            Grab,
            Flee,
            Hide,
            Done
        }

        const float RunSpeed = 7f;
        const float EmergeFrame = 0.08f;
        const float RunFrame = 0.06f;
        const float GrabFrame = 0.11f;
        const float HideFrame = 0.08f;
        // Medidas de las hojas de salir y esconderse (50×28): la grieta está a 10 px del borde izquierdo al salir y a 39 al esconderse.
        const float EmergeRunStart = 28f / 16f;
        const float HideRunStart = 29f / 16f;
        // El cuadro que cuenta del agarre y de esconderse (el 3.º).
        const int GrabHold = 2;
        const int HideHold = 2;
        // El lomo en la celda de 40×28: (16, 14) respecto al centro (20, 14).
        static readonly Vector2 Back = new(-4f / 16f, 0f);
        const int Order = 4;

        HeartStorageArt art;
        RoomHeartPickup heart;
        SpriteRenderer body;
        SpriteRenderer loot;
        Sprite lootSprite;
        Phase phase;
        float phaseStart;
        Vector2 crackA, crackB;
        float side;
        Vector2 hidePoint;
        float heading;

        /// <summary>True si la cucaracha ya se ha llevado el corazón.</summary>
        public bool HasLoot { get; private set; }

        public bool IsDone => phase == Phase.Done;

        /// <summary>
        /// Manda una cucaracha a por ese corazón. <paramref name="cracks"/> son los sitios del borde de la sala donde puede haber
        /// una grieta (en el mundo). Null si no hay arte o sitios.
        /// </summary>
        public static HeartThief Send(RoomHeartPickup heart, IList<Vector2> cracks, Transform parent)
        {
            var art = HeartStorageArt.Current;
            if (art == null || !art.HasRoach || heart == null || cracks == null || cracks.Count < 1)
                return null;

            Vector2 target = heart.Home;
            // Sale de una grieta cercana al corazón y se mete en otra lejos de él.
            Vector2 a = Nearest(cracks, target, minDistance: 3f);
            Vector2 b = Farthest(cracks, target, a);

            var go = new GameObject("HeartThief");
            go.transform.SetParent(parent, true);
            var thief = go.AddComponent<HeartThief>();
            thief.art = art;
            thief.heart = heart;
            thief.crackA = a;
            thief.crackB = b;
            thief.Begin(parent);
            return thief;
        }

        static Vector2 Nearest(IList<Vector2> points, Vector2 from, float minDistance)
        {
            Vector2 best = points[0];
            float bestDistance = float.MaxValue;
            foreach (var point in points)
            {
                float d = Vector2.Distance(point, from);
                if (d >= minDistance && d < bestDistance)
                {
                    best = point;
                    bestDistance = d;
                }
            }
            return best;
        }

        static Vector2 Farthest(IList<Vector2> points, Vector2 from, Vector2 not)
        {
            Vector2 best = points[0];
            float bestDistance = -1f;
            foreach (var point in points)
            {
                if (Vector2.Distance(point, not) < 2f)
                    continue;
                float d = Vector2.Distance(point, from);
                if (d > bestDistance)
                {
                    best = point;
                    bestDistance = d;
                }
            }
            return best;
        }

        void Begin(Transform parent)
        {
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = Order;
            var uiArt = UiExtrasArt.Current;
            lootSprite = uiArt != null && uiArt.roomHeartIdle.Length > 0 ? uiArt.roomHeartIdle[0] : null;

            // Las dos grietas se quedan en el suelo.
            if (art.crack != null)
            {
                MakeCrack(crackA, parent);
                MakeCrack(crackB, parent);
            }

            side = Mathf.Sign(heart.Home.x - crackA.x);
            if (side == 0f)
                side = 1f;
            transform.position = crackA;
            body.flipX = side < 0f;
            SetPhase(Phase.Emerge);
        }

        void MakeCrack(Vector2 position, Transform parent)
        {
            var crack = new GameObject("RoachCrack");
            crack.transform.SetParent(parent, true);
            crack.transform.position = position;
            var renderer = crack.AddComponent<SpriteRenderer>();
            renderer.sprite = art.crack;
            renderer.sortingOrder = -7;
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
        }

        float Age => Time.time - phaseStart;

        void Update()
        {
            switch (phase)
            {
                case Phase.Emerge:
                    UpdateEmerge();
                    break;
                case Phase.ToLoot:
                    UpdateToLoot();
                    break;
                case Phase.Grab:
                    UpdateGrab();
                    break;
                case Phase.Flee:
                    UpdateFlee();
                    break;
                case Phase.Hide:
                    UpdateHide();
                    break;
            }
            UpdateLoot();
        }

        void UpdateEmerge()
        {
            int frame = Mathf.FloorToInt(Age / EmergeFrame);
            if (frame >= art.emerge.Length)
            {
                // Termina de salir y echa a correr, girada hacia el corazón.
                transform.position = crackA + new Vector2(side * EmergeRunStart, 0f);
                body.flipX = false;
                SetPhase(Phase.ToLoot);
                return;
            }
            body.sprite = art.emerge[frame];
        }

        void UpdateToLoot()
        {
            if (heart == null || heart.Collected)
            {
                // Se lo han llevado antes: se retira.
                BeginFlee();
                return;
            }
            if (MoveTowards(heart.Home, 0.35f))
                SetPhase(Phase.Grab);
        }

        void UpdateGrab()
        {
            int frame = Mathf.FloorToInt(Age / GrabFrame);
            if (frame >= art.grab.Length)
            {
                BeginFlee();
                return;
            }
            body.sprite = art.grab[frame];
            if (frame >= GrabHold && !HasLoot)
            {
                HasLoot = true;
                if (heart != null && !heart.Collected)
                    heart.Steal();
            }
        }

        void BeginFlee()
        {
            // Hacia la grieta de huida, parando antes de su sitio para meterse corriendo.
            float dir = Mathf.Sign(crackB.x - transform.position.x);
            side = dir == 0f ? 1f : dir;
            hidePoint = crackB - new Vector2(side * HideRunStart, 0f);
            SetPhase(Phase.Flee);
        }

        void UpdateFlee()
        {
            if (MoveTowards(hidePoint, 0.1f))
            {
                transform.position = crackB;
                transform.rotation = Quaternion.identity;
                body.flipX = side < 0f;
                SetPhase(Phase.Hide);
            }
        }

        void UpdateHide()
        {
            int frame = Mathf.FloorToInt(Age / HideFrame);
            if (frame >= art.hide.Length)
            {
                SetPhase(Phase.Done);
                if (loot != null)
                    Destroy(loot.gameObject);
                Destroy(gameObject);
                return;
            }
            body.sprite = art.hide[frame];
            if (frame >= HideHold && loot != null)
            {
                Destroy(loot.gameObject);
                loot = null;
            }
        }

        /// <summary>Corre hacia ese punto, girada hacia él. True al llegar.</summary>
        bool MoveTowards(Vector2 point, float arrive)
        {
            Vector2 position = transform.position;
            Vector2 toTarget = point - position;
            if (toTarget.magnitude <= arrive)
                return true;

            heading = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, heading);
            body.flipX = false;
            transform.position = Vector2.MoveTowards(position, point, RunSpeed * Time.deltaTime);
            body.sprite = art.run[Mathf.FloorToInt(Time.time / RunFrame) % art.run.Length];
            return false;
        }

        // El botín va a cuestas, sin girar con ella, sobre el lomo.
        void UpdateLoot()
        {
            if (!HasLoot || lootSprite == null)
                return;
            if (loot == null)
            {
                if (phase == Phase.Hide && Age / HideFrame >= HideHold)
                    return;
                var go = new GameObject("Loot");
                loot = go.AddComponent<SpriteRenderer>();
                loot.sprite = lootSprite;
                loot.sortingOrder = Order + 1;
            }
            if (phase == Phase.Hide)
            {
                // Entra con ella: del punto de partida a la grieta mientras el lomo llega a ella.
                float t = Mathf.Clamp01(Age / (HideFrame * HideHold));
                loot.transform.position = Vector2.Lerp(hidePoint, crackB, t) + new Vector2(Back.x * side, 0f);
                return;
            }
            Vector2 offset = Quaternion.Euler(0f, 0f, heading) * Back;
            loot.transform.position = (Vector2)transform.position + offset;
        }

        void OnDestroy()
        {
            if (loot != null)
                Destroy(loot.gameObject);
        }
    }
}
