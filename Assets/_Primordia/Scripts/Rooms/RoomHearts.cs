using System.Collections.Generic;
using Primordia.Audio;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Corazón de sala: al limpiar una sala de combate puede quedar medio corazón rojo. La probabilidad es de
    /// <see cref="BaseChance"/> y sube <see cref="MissStep"/> por cada sala limpia sin corazón (protección oculta: el
    /// juego no la explica); al soltar uno o al empezar un piso vuelve a la base. El corazón se queda en la sala
    /// hasta que el jugador lo recoge con la vida que le falte.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class RoomHearts : MonoBehaviour
    {
        public const float BaseChance = 0.12f;
        public const float MissStep = 0.04f;

        static int missedRooms;

        /// <summary>Salas limpias seguidas sin corazón desde el último o desde que empezó el piso.</summary>
        public static int MissedRooms => missedRooms;

        /// <summary>Probabilidad de que la próxima sala limpia suelte corazón.</summary>
        public static float Chance => Mathf.Min(1f, BaseChance + MissStep * missedRooms);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            missedRooms = 0;
            all.Clear();
        }

        /// <summary>Vuelve a la probabilidad base: lo llama el generador al montar cada piso.</summary>
        public static void ResetLuck() => missedRooms = 0;

        /// <summary>Tira la probabilidad actual con ese número (0 a 1) y actualiza la protección.</summary>
        public static bool Roll(float roll)
        {
            bool drop = roll < Chance;
            missedRooms = drop ? 0 : missedRooms + 1;
            return drop;
        }

        Room room;
        Vector2 lastEnemyPosition;
        bool hasEnemyPosition;

        public RoomHeartPickup Dropped { get; private set; }

        static readonly List<RoomHearts> all = new();

        /// <summary>Las salas del piso que pueden soltar corazón (el mapa marca las que tienen uno sin recoger).</summary>
        public static IReadOnlyList<RoomHearts> All => all;

        /// <summary>True si la sala soltó un medio corazón y sigue en el suelo.</summary>
        public bool HasUnclaimedHeart => Dropped != null && !Dropped.Collected;

        void Awake() => room = GetComponent<Room>();

        void OnEnable()
        {
            all.Add(this);
            room.EnemyDied += OnEnemyDied;
            room.Cleared += OnCleared;
        }

        void OnDisable()
        {
            all.Remove(this);
            room.EnemyDied -= OnEnemyDied;
            room.Cleared -= OnCleared;
        }

        void OnEnemyDied(Room _, Enemy enemy)
        {
            if (enemy == null)
                return;

            lastEnemyPosition = enemy.transform.position;
            hasEnemyPosition = true;
        }

        void OnCleared(Room _)
        {
            if (Dropped != null || !Roll(Random.value))
                return;

            Vector2 position = hasEnemyPosition ? lastEnemyPosition : (Vector2)room.GetComponent<Collider2D>().bounds.center;
            Dropped = RoomHeartPickup.Create(position, transform);
        }
    }

    /// <summary>
    /// Medio corazón rojo en el suelo. Con la vida llena se queda donde está; con vida que curar, el imán lo atrae
    /// y al tocarlo cura medio corazón. Sprite provisional dibujado por código, hasta que haya arte.
    /// </summary>
    public class RoomHeartPickup : MonoBehaviour
    {
        const float MagnetRadius = 1.5f;
        const float CollectRadius = 0.45f;
        const float MagnetSpeed = 8f;
        const float BobHeight = 0.08f;
        const float BobSpeed = 3f;
        const float IdleFrameSeconds = 0.13f;
        const float CollectFrameSeconds = 0.06f;

        static Sprite sprite;
        SpriteRenderer body;

        Transform player;
        Health health;
        Vector2 home;
        float phase;

        public bool Collected { get; private set; }

        /// <summary>Punto de reposo (sin el vaivén).</summary>
        public Vector2 Home => home;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => sprite = null;

        public static RoomHeartPickup Create(Vector2 position, Transform parent)
        {
            var go = new GameObject("RoomHeart");
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            var art = UiExtrasArt.Current;
            bool hasArt = art != null && art.roomHeartIdle.Length > 0;
            renderer.sprite = hasArt ? art.roomHeartIdle[0] : GetSprite();
            renderer.sortingOrder = 5;
            if (hasArt && art.roomHeartShadow != null)
            {
                // La sombra se queda en el suelo mientras el corazón sube y baja.
                var shadow = new GameObject("Shadow");
                shadow.transform.SetParent(go.transform, false);
                shadow.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = art.roomHeartShadow;
                shadowRenderer.sortingOrder = 4;
                shadow.AddComponent<FixedShadow>();
            }
            return go.AddComponent<RoomHeartPickup>();
        }

        void Awake()
        {
            home = transform.position;
            phase = Random.value * 10f;
            body = GetComponent<SpriteRenderer>();
        }

        /// <summary>La sombra no sigue el vaivén del corazón: se queda en el suelo, bajo el punto de reposo.</summary>
        sealed class FixedShadow : MonoBehaviour
        {
            RoomHeartPickup pickup;

            void Awake() => pickup = GetComponentInParent<RoomHeartPickup>();

            void LateUpdate()
            {
                if (pickup != null)
                    transform.position = pickup.Home + Vector2.down * 0.5f;
            }
        }

        void Update()
        {
            if (Collected)
                return;

            if (player == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject == null || !playerObject.TryGetComponent(out health))
                    return;
                player = playerObject.transform;
            }

            if (health.IsDead)
                return;

            bool needsHeart = health.Current < health.Max;
            float distance = Vector2.Distance(home, player.position);
            if (needsHeart && distance <= CollectRadius)
            {
                Collect();
                return;
            }

            if (needsHeart && distance <= MagnetRadius)
                home = Vector2.MoveTowards(home, player.position, MagnetSpeed * Time.deltaTime);

            transform.position = home + Vector2.up * (Mathf.Sin((Time.time + phase) * BobSpeed) * BobHeight);

            var art = UiExtrasArt.Current;
            if (art != null && art.roomHeartIdle.Length > 0 && body != null)
                body.sprite = art.roomHeartIdle[Mathf.FloorToInt((Time.time + phase) / IdleFrameSeconds) % art.roomHeartIdle.Length];
        }

        void Collect()
        {
            Collected = true;
            health.Heal(1);
            Sfx.Play(Sounds.PlayerHeal);
            var art = UiExtrasArt.Current;
            if (art != null)
                SpriteFramePlayer.PlayOnce(art.roomHeartCollect, CollectFrameSeconds, transform.position, 8);
            Destroy(gameObject);
        }

        /// <summary>Medio corazón (la mitad izquierda) en 8×11 px, a 16 px por unidad.</summary>
        static Sprite GetSprite()
        {
            if (sprite != null)
                return sprite;

            string[] rows =
            {
                "..###...",
                ".#rrr###",
                "#rhhrrrr",
                "#rhrrrrr",
                "#rrrrrrr",
                ".#rrrrrr",
                "..#rrrdd",
                "...#rrdd",
                "....#rdd",
                ".....#dd",
                "......##",
            };
            var colors = new System.Collections.Generic.Dictionary<char, Color32>
            {
                ['#'] = new Color32(60, 10, 24, 255),
                ['r'] = new Color32(214, 48, 64, 255),
                ['h'] = new Color32(255, 150, 150, 255),
                ['d'] = new Color32(150, 24, 48, 255),
            };
            int height = rows.Length;
            var texture = new Texture2D(8, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    char c = rows[height - 1 - y][x];
                    texture.SetPixel(x, y, colors.TryGetValue(c, out var color) ? color : new Color32(0, 0, 0, 0));
                }
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 8, height), new Vector2(0.5f, 0.5f), 16f);
            return sprite;
        }
    }
}
