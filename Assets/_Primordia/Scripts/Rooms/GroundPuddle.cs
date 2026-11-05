using System.Collections.Generic;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using Primordia.Player;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Charco del escenario del piso 3 (no el de veneno): <b>lodo</b>, que frena un 40 % a quien lo pisa (jugador y enemigos que
    /// andan; la esquiva pasa por encima), o <b>agua de charca</b> (con reflejos; la Piel anfibia y el sapo la usarán más
    /// adelante), que no frena pero apaga el quemado de quien la pisa (la barra del jugador baja a toda prisa; a los enemigos
    /// se les quita el <see cref="Burn"/>) y hace su efecto: salpicadura al entrar, una onda por paso, gotas al salir y vapor
    /// si entra ardiendo. Pegado al suelo, por debajo de todo.
    /// </summary>
    public class GroundPuddle : MonoBehaviour
    {
        /// <summary>El lodo deja al 60 % de la velocidad (frena un 40 %).</summary>
        public const float SludgeSpeed = 0.6f;

        const float CheckEvery = 0.1f;
        const float SlowHold = 0.25f;

        // Lo que baja la barra de quemado por segundo mientras se pisa agua (igual que el lodo profundo).
        const float BurnDouseSpeed = 3f;
        // Los pies respecto al centro del cuerpo, para colocar los efectos.
        static readonly Vector2 FootOffset = new(0f, -0.3f);
        const float RippleEvery = 0.3f;
        const float MinStep = 0.03f;
        const float FrameSeconds = 0.08f;
        // Encima de la criatura (salpicadura, vapor, gotas) y justo sobre el charco (ondas).
        const int FxOrder = 20;
        const int RippleOrder = -5;

        /// <summary>Quien está en el agua: dónde estaba la última vez y cuándo toca la siguiente onda.</summary>
        class Wader
        {
            public Transform body;
            public Vector2 last;
            public float nextRipple;
            public bool seen;
        }

        readonly Dictionary<Health, Wader> waders = new();
        readonly List<Health> gone = new();

        static readonly List<GroundPuddle> all = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        bool sludge;
        Vector2 area;
        // Charca de forma libre (la gran charca de un tema): sus celdas; el área es su caja.
        HashSet<Vector2Int> cells;
        float nextCheck;

        public bool IsSludge => sludge;

        void OnEnable() => all.Add(this);

        void OnDisable() => all.Remove(this);

        /// <summary>True si ese punto está dentro de algún charco de lodo o de agua del escenario (la Piel anfibia se carga ahí).</summary>
        public static bool IsOver(Vector2 point)
        {
            foreach (var puddle in all)
            {
                if (puddle.Covers(point))
                    return true;
            }
            return false;
        }

        bool Covers(Vector2 point)
        {
            if (cells != null)
                return cells.Contains(new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)));
            Vector2 offset = point - (Vector2)transform.position;
            return Mathf.Abs(offset.x) <= area.x / 2f && Mathf.Abs(offset.y) <= area.y / 2f;
        }

        /// <summary>Agua de forma libre: una zona que cubre esas celdas (globales). Sin dibujo propio: lo ponen sus tiles.</summary>
        internal static GroundPuddle CreateZone(Transform parent, HashSet<Vector2Int> zoneCells)
        {
            var obj = new GameObject("Puddle_Water_Zone");
            obj.transform.SetParent(parent, false);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in zoneCells)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }
            obj.transform.position = new Vector2((minX + maxX + 1) / 2f, (minY + maxY + 1) / 2f);
            var puddle = obj.AddComponent<GroundPuddle>();
            puddle.cells = new HashSet<Vector2Int>(zoneCells);
            puddle.area = new Vector2(maxX - minX + 1, maxY - minY + 1);
            return puddle;
        }

        /// <summary>True si ese punto está dentro de algún charco de lodo (no de agua) del escenario.</summary>
        public static bool IsSludgeOver(Vector2 point)
        {
            foreach (var puddle in all)
            {
                if (!puddle.sludge)
                    continue;
                if (puddle.Covers(point))
                    return true;
            }
            return false;
        }

        internal static GroundPuddle Create(Transform parent, Vector2 position, Sprite[] frames, float frameSeconds, bool sludge)
        {
            var obj = new GameObject(sludge ? "Puddle_Sludge" : "Puddle_Water");
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -6;
            var puddle = obj.AddComponent<GroundPuddle>();
            puddle.sludge = sludge;
            if (frames.Length > 0)
            {
                renderer.sprite = frames[0];
                // La zona que frena es el interior del dibujo (sin los bordes irregulares).
                var size = frames[0].bounds.size;
                puddle.area = new Vector2(size.x * 0.8f, size.y * 0.7f);
                obj.AddComponent<SpriteFramePlayer>().Play(renderer, frames, frameSeconds, newLoop: true);
            }
            return puddle;
        }

        void Update()
        {
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + CheckEvery;

            if (!sludge)
            {
                CheckWater();
                return;
            }

            var seen = new HashSet<Health>();
            foreach (var hit in Physics2D.OverlapBoxAll(transform.position, area, 0f))
            {
                if (hit.isTrigger)
                    continue;

                if (hit.GetComponentInParent<PlayerController>() is { } player)
                {
                    if (!player.IsDodging && !player.IsLunging)
                        player.Slow(SludgeSpeed, SlowHold);
                    continue;
                }

                var health = hit.GetComponentInParent<Health>();
                if (health != null && !health.IsDead && seen.Add(health) && health.TryGetComponent<EnemyAI>(out var ai))
                    ai.Slow(SludgeSpeed, SlowHold);
            }
        }

        /// <summary>Agua: apaga el quemado y hace los efectos de entrar, andar y salir.</summary>
        void CheckWater()
        {
            foreach (var wader in waders.Values)
                wader.seen = false;

            foreach (var hit in Physics2D.OverlapBoxAll(transform.position, area, 0f))
            {
                if (hit.isTrigger)
                    continue;
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead)
                    continue;
                if (cells != null && !Covers((Vector2)health.transform.position + FootOffset))
                    continue;

                var player = health.GetComponentInParent<PlayerController>();
                bool isEnemy = player == null && health.GetComponentInParent<Enemy>() != null;
                if (player == null && !isEnemy)
                    continue;
                // La esquiva pasa por encima del agua.
                if (player != null && (player.IsDodging || player.IsLunging))
                    continue;

                var body = health.transform;
                bool entered = !waders.TryGetValue(health, out var wader);
                if (entered)
                    waders[health] = wader = new Wader { body = body, last = body.position, nextRipple = Time.time + RippleEvery };
                wader.seen = true;
                if (entered)
                    MudBodyVisual.SetWater(body, true);

                bool burning = Douse(health, player);
                var feet = (Vector2)body.position + FootOffset;
                if (entered)
                {
                    Fx(PolishArt.Current?.waterSplash, feet, FxOrder);
                    if (burning)
                        Fx(PolishArt.Current?.steamHiss, feet, FxOrder);
                }
                else if (Time.time >= wader.nextRipple && Vector2.Distance(wader.last, body.position) > MinStep)
                {
                    wader.nextRipple = Time.time + RippleEvery;
                    Fx(PolishArt.Current?.waterRipple, feet, RippleOrder);
                }
                wader.last = body.position;
            }

            // Quien ya no está: sale del agua y gotea.
            gone.Clear();
            foreach (var pair in waders)
                if (!pair.Value.seen)
                    gone.Add(pair.Key);
            foreach (var key in gone)
            {
                var wader = waders[key];
                waders.Remove(key);
                if (wader.body == null)
                    continue;
                MudBodyVisual.SetWater(wader.body, false);
                var feet = (Vector2)wader.body.position + FootOffset;
                Fx(PolishArt.Current?.waterDrip, feet + new Vector2(-0.25f, 0.5f), FxOrder);
                Fx(PolishArt.Current?.waterDrip, feet + new Vector2(0.25f, 0.4f), FxOrder);
            }
        }

        /// <summary>Apaga el quemado de quien está en el agua. True si estaba ardiendo.</summary>
        static bool Douse(Health health, PlayerController player)
        {
            if (player != null)
            {
                if (!health.TryGetComponent<PlayerBurn>(out var burn) || (burn.Bar <= 0f && !burn.Burning))
                    return false;
                burn.Douse(BurnDouseSpeed * CheckEvery);
                return true;
            }

            var enemyBurn = health.GetComponentInParent<Burn>();
            if (enemyBurn == null)
                return false;
            Destroy(enemyBurn);
            return true;
        }

        static void Fx(Sprite[] frames, Vector2 position, int order) =>
            SpriteFramePlayer.PlayOnce(frames, FrameSeconds, position, order);
    }
}
