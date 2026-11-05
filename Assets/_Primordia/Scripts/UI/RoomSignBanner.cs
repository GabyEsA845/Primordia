using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Letrero de las salas con tema (piso 3): al cerrarse la sala por primera vez, un tronco de Lepidodendron con el nombre
    /// y el icono del tema baja desde arriba de la pantalla (480×270) con un pequeño rebote, se queda un rato y vuelve a
    /// subir. Montaje y tiempos de docs/multimedia-temp/floor3-room-signs/README.md. Se crea solo y se destruye al acabar.
    /// </summary>
    public class RoomSignBanner : MonoBehaviour
    {
        public const float Delay = 0.5f;
        public const float InTime = 0.35f;
        public const float HoldTime = 2.3f;
        public const float OutTime = 0.35f;

        const float LogHeight = 42f;
        const float RestTop = 18f;
        const float Overshoot = 5f;
        // Cuánto de la entrada se gasta en bajar hasta pasarse; el resto, en volver.
        const float DropShare = 0.7f;

        RectTransform log;
        float time;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            Room.CombatStartedAny -= OnCombatStarted;
            Room.CombatStartedAny += OnCombatStarted;
        }

        static void OnCombatStarted(Room room)
        {
            if (room == null || string.IsNullOrEmpty(room.SignKey))
                return;
            var art = RoomSignArt.Current;
            if (art == null || art.log == null || !art.TryGet(room.SignKey, out var entry))
                return;
            Create(art, entry);
        }

        static void Create(RoomSignArt art, RoomSignArt.Entry entry)
        {
            var go = new GameObject("RoomSign");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480f, 270f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var banner = go.AddComponent<RoomSignBanner>();
            banner.Build(art, entry);
        }

        void Build(RoomSignArt art, RoomSignArt.Entry entry)
        {
            log = NewImage("Log", transform, art.log, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            // Nombre: la parte de arriba de uno sin tilde (21 px) va en y = 9; si es más alto, se sube lo que sobra.
            float titleHeight = entry.title.rect.height;
            var title = NewImage("Title", log, entry.title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            title.anchorMin = title.anchorMax = new Vector2(0.5f, 1f);
            title.anchoredPosition = new Vector2(0f, -(9f - (titleHeight - 21f)));
            if (entry.icon != null)
            {
                // Los dos iconos van centrados en (29, 21) y (220, 21) contando desde arriba a la izquierda.
                PlaceIcon(entry.icon, 29f);
                PlaceIcon(entry.icon, 220f);
            }
            Apply();
        }

        void PlaceIcon(Sprite icon, float x)
        {
            var rect = NewImage("Icon", log, icon, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f));
            rect.anchoredPosition = new Vector2(x, -21f);
        }

        static RectTransform NewImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = sprite.rect.size;
            return rect;
        }

        /// <summary>Segundos totales hasta que desaparece.</summary>
        public static float TotalTime => Delay + InTime + HoldTime + OutTime;

        /// <summary>
        /// Distancia (px) de la parte de arriba del tronco al borde de arriba de la pantalla, <paramref name="t"/> segundos
        /// después de cerrarse la sala. Negativa = fuera de la pantalla, por arriba.
        /// </summary>
        public static float TopAt(float t)
        {
            float hidden = -LogHeight;
            t -= Delay;
            if (t <= 0f)
                return hidden;
            if (t < InTime)
            {
                float u = t / InTime;
                float drop = InTime * DropShare;
                if (t < drop)
                {
                    float k = t / drop;
                    float ease = 1f - (1f - k) * (1f - k);
                    return Mathf.Lerp(hidden, RestTop + Overshoot, ease);
                }
                float back = (t - drop) / (InTime - drop);
                return Mathf.Lerp(RestTop + Overshoot, RestTop, back * back * (3f - 2f * back));
            }
            t -= InTime;
            if (t < HoldTime)
                return RestTop;
            t -= HoldTime;
            if (t < OutTime)
            {
                float k = t / OutTime;
                return Mathf.Lerp(RestTop, hidden, k * k);
            }
            return hidden;
        }

        void Apply() => log.anchoredPosition = new Vector2(0f, -TopAt(time));

        void Update()
        {
            time += Time.deltaTime;
            if (time >= TotalTime)
            {
                Destroy(gameObject);
                return;
            }
            Apply();
        }
    }
}
