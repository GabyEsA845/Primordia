using System;
using Primordia.Biomass;
using Primordia.Combat;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Cucaracha ladrona de la sala Ladronas (piso 3): sale de una grieta, corre al montón de biomasa más cercano que nadie haya
    /// cogido, se lo sube al lomo y se mete en otra grieta. A diferencia de la del almacén de corazones, esta SÍ se puede matar (tiene
    /// vida y cuerpo al correr): si muere con el botín, lo suelta donde cae. Mientras sale de la grieta o se mete en ella no se la
    /// puede golpear. Va girada hacia donde corre.
    /// </summary>
    public class BiomassThief : MonoBehaviour
    {
        enum Phase
        {
            Emerge,
            ToLoot,
            Grab,
            Flee,
            Hide,
            Dying,
            Done
        }

        const float RunSpeed = 6.5f;
        const float EmergeFrame = 0.08f;
        const float RunFrame = 0.06f;
        const float GrabFrame = 0.11f;
        const float HideFrame = 0.08f;
        const float DeathFrame = 0.09f;
        const float HurtSeconds = 0.18f;
        const float EmergeRunStart = 28f / 16f;
        const float HideRunStart = 29f / 16f;
        const int GrabHold = 2;
        const int HideHold = 2;
        const int MaxHealth = 3;
        const int Order = 4;
        static readonly Vector2 Back = new(-4f / 16f, 0f);

        HeartStorageArt art;
        ThiefRoomController controller;
        BiomassPickup dropPrefab;
        Sprite lootSprite;
        SpriteRenderer body;
        SpriteRenderer loot;
        GameObject hurtbox;
        Health health;
        BiomassPickup target;
        int lootValue;
        Phase phase;
        float phaseStart;
        float hurtUntil;
        Vector2 crackA, crackB, hidePoint;
        float side = 1f;
        float heading;

        /// <summary>True si lleva biomasa a cuestas.</summary>
        public bool HasLoot { get; private set; }

        /// <summary>True cuando ya no está en la sala (huyó por la grieta o murió).</summary>
        public bool IsDone => phase == Phase.Done;

        internal static BiomassThief Send(Transform parent, ThiefRoomController controller, Vector2 from, Vector2 to, BiomassPickup dropPrefab)
        {
            var art = HeartStorageArt.Current;
            if (art == null || !art.HasRoach)
                return null;

            var go = new GameObject("BiomassThief");
            go.transform.SetParent(parent, true);
            go.transform.position = from;
            var thief = go.AddComponent<BiomassThief>();
            thief.art = art;
            thief.controller = controller;
            thief.dropPrefab = dropPrefab;
            thief.crackA = from;
            thief.crackB = to;
            thief.lootSprite = dropPrefab != null ? dropPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
            thief.Begin();
            return thief;
        }

        void Begin()
        {
            body = gameObject.AddComponent<SpriteRenderer>();
            body.sortingOrder = Order;

            var kinematic = gameObject.AddComponent<Rigidbody2D>();
            kinematic.bodyType = RigidbodyType2D.Kinematic;
            health = gameObject.AddComponent<Health>();
            health.SetMax(MaxHealth, true);
            health.Died += OnDied;
            health.Damaged += _ => hurtUntil = Time.time + HurtSeconds;

            // Cuerpo para los golpes del jugador (capa Enemy), solo mientras corre.
            hurtbox = new GameObject("Hurtbox");
            hurtbox.transform.SetParent(transform, false);
            int layer = LayerMask.NameToLayer(CombatLayers.Enemy);
            if (layer >= 0)
                hurtbox.layer = layer;
            hurtbox.AddComponent<CircleCollider2D>().radius = 0.4f;
            hurtbox.SetActive(false);

            side = 1f;
            body.flipX = false;
            SetPhase(Phase.Emerge);
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseStart = Time.time;
            hurtbox.SetActive(next is Phase.ToLoot or Phase.Grab or Phase.Flee);
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
                case Phase.Dying:
                    UpdateDying();
                    break;
            }
            UpdateLoot();
        }

        void UpdateEmerge()
        {
            int frame = Mathf.FloorToInt(Age / EmergeFrame);
            if (frame >= art.emerge.Length)
            {
                transform.position = crackA + new Vector2(side * EmergeRunStart, 0f);
                body.flipX = false;
                target = controller != null ? controller.Claim(transform.position) : null;
                if (target == null)
                    BeginFlee();
                else
                    SetPhase(Phase.ToLoot);
                return;
            }
            body.sprite = art.emerge[frame];
        }

        void UpdateToLoot()
        {
            // Si el jugador se lo ha llevado antes, se retira.
            if (target == null)
            {
                BeginFlee();
                return;
            }
            if (MoveTowards(target.transform.position, 0.35f))
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
            if (frame >= GrabHold && !HasLoot && target != null)
            {
                HasLoot = true;
                lootValue = target.Value;
                Destroy(target.gameObject);
                target = null;
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
                Finish();
                return;
            }
            body.sprite = art.hide[frame];
            if (frame >= HideHold && loot != null)
            {
                Destroy(loot.gameObject);
                loot = null;
                HasLoot = false;
            }
        }

        void OnDied()
        {
            if (phase is Phase.Dying or Phase.Done)
                return;
            // Suelta lo que llevaba donde cae.
            if (HasLoot && dropPrefab != null)
                Instantiate(dropPrefab, transform.position, Quaternion.identity, transform.parent);
            HasLoot = false;
            if (loot != null)
                Destroy(loot.gameObject);
            body.flipX = false;
            SetPhase(Phase.Dying);
        }

        void UpdateDying()
        {
            int frame = Mathf.FloorToInt(Age / DeathFrame);
            if (frame >= art.death.Length)
            {
                Finish();
                return;
            }
            body.sprite = art.death[frame];
        }

        void Finish()
        {
            phase = Phase.Done;
            if (loot != null)
                Destroy(loot.gameObject);
            Destroy(gameObject, 0.05f);
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
            var frames = Time.time < hurtUntil && art.hurt.Length > 0 ? art.hurt : art.run;
            body.sprite = frames[Mathf.FloorToInt(Time.time / RunFrame) % frames.Length];
            return false;
        }

        // El botín va a cuestas, sin girar con ella, sobre el lomo.
        void UpdateLoot()
        {
            if (!HasLoot || lootSprite == null || phase is Phase.Dying or Phase.Done)
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
                float t = Mathf.Clamp01(Age / (HideFrame * HideHold));
                loot.transform.position = Vector2.Lerp(hidePoint, crackB, t) + new Vector2(Back.x * side, 0f);
                return;
            }
            loot.transform.position = (Vector2)transform.position + (Vector2)(Quaternion.Euler(0f, 0f, heading) * Back);
        }

        void OnDestroy()
        {
            if (loot != null)
                Destroy(loot.gameObject);
        }
    }
}
