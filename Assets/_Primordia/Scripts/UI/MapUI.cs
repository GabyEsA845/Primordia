using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Minimapa del HUD (arriba a la derecha, centrado en el jugador y recortado por su recuadro) y mapa grande
    /// (TAB, con el juego detenido: todo lo descubierto, ×2 o ×1 si no cabe). Los dos muestran la misma textura
    /// de <see cref="MapPainter"/>, que se repinta al cambiar de sala o al cambiar el estado de alguna sala.
    /// Marcas: jugador, sala del jefe y salas con mutación aún sin limpiar; los iconos no se amplían con el mapa.
    /// Sin piso generado (escenas de prueba) se oculta.
    /// </summary>
    public class MapUI : MonoBehaviour
    {
        [Tooltip("Colores del plano en el orden de MapPainter.Swatch (tira UI_Map_Palette).")]
        [SerializeField] Color[] palette = System.Array.Empty<Color>();
        [SerializeField] Sprite[] playerFrames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float playerFrameDuration = 0.15f;
        [SerializeField] Sprite bossIcon;
        [SerializeField] Sprite rewardIcon;

        [Header("Minimapa")]
        [Tooltip("Raíz del minimapa (marco y fondo); se oculta si no hay piso.")]
        [SerializeField] GameObject minimapRoot;
        [Tooltip("Ventana con RectMask2D; el plano se centra en el jugador dentro de ella.")]
        [SerializeField] RectTransform minimapViewport;

        [Header("Mapa grande")]
        [SerializeField] GameObject bigMapRoot;
        [Tooltip("Interior del panel, donde cabe el plano.")]
        [SerializeField] RectTransform bigMapViewport;
        [SerializeField, Min(1)] int bigMapMaxScale = 2;

        /// <summary>Plano dentro de una vista: la imagen y sus marcas encima.</summary>
        class View
        {
            public RectTransform Content;
            public RawImage Image;
            public Image Player;
            public Image Boss;
            public readonly List<Image> Rewards = new();
        }

        FloorGenerator generator;
        Transform player;
        MapPainter painter;
        View minimap;
        View bigMap;
        MapRoom current;
        Room.RoomState[] states;
        bool bigMapShown;

        public MapPainter Painter => painter;
        public MapRoom CurrentRoom => current;
        public bool BigMapShown => bigMapShown;

        void Start()
        {
            generator = FindAnyObjectByType<FloorGenerator>();
            var playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            Initialize(generator != null ? generator.Built : null);
        }

        internal void Configure(Color[] newPalette, Sprite[] newPlayerFrames, Sprite newBossIcon, Sprite newRewardIcon)
        {
            palette = newPalette;
            playerFrames = newPlayerFrames ?? System.Array.Empty<Sprite>();
            bossIcon = newBossIcon;
            rewardIcon = newRewardIcon;
        }

        internal void ConfigureViews(GameObject newMinimapRoot, RectTransform newMinimapViewport, GameObject newBigMapRoot,
            RectTransform newBigMapViewport)
        {
            minimapRoot = newMinimapRoot;
            minimapViewport = newMinimapViewport;
            bigMapRoot = newBigMapRoot;
            bigMapViewport = newBigMapViewport;
        }

        /// <summary>Prepara el mapa de ese piso (las pruebas lo llaman con su propio piso y jugador).</summary>
        internal void Initialize(FloorBuilder.BuiltFloor floor, Transform newPlayer = null)
        {
            if (newPlayer != null)
                player = newPlayer;

            bool hasFloor = floor != null && floor.Map != null;
            if (minimapRoot != null)
                minimapRoot.SetActive(hasFloor);
            SetBigMap(false);
            if (!hasFloor)
            {
                enabled = false;
                return;
            }

            var colors = System.Array.ConvertAll(palette, color => (Color32)color);
            painter = new MapPainter(floor.Map, floor.MapRooms, floor.Corridors, colors);
            states = new Room.RoomState[floor.MapRooms.Count];
            minimap = CreateView(minimapViewport, floor.MapRooms);
            bigMap = CreateView(bigMapViewport, floor.MapRooms);
            Repaint(floor.MapRooms);
        }

        void LateUpdate()
        {
            if (painter == null)
                return;

            var rooms = generator != null && generator.Built != null ? generator.Built.MapRooms : null;
            if (rooms != null)
            {
                var room = painter.RoomAt(PlayerCell());
                // En los pasillos sigue contando la última sala.
                if (room != null && room != current || StatesChanged(rooms))
                {
                    if (room != null)
                        current = room;
                    Repaint(rooms);
                }
            }

            UpdateMinimap();

            bool show = GameManager.Instance != null && GameManager.Instance.State == GameState.Map;
            if (show != bigMapShown)
                SetBigMap(show);
            if (bigMapShown)
                UpdateBigMap();
        }

        /// <summary>Para las pruebas: vuelve a mirar en qué sala está el jugador y repinta.</summary>
        internal void Refresh(IReadOnlyList<MapRoom> rooms)
        {
            var room = painter.RoomAt(PlayerCell());
            if (room != null)
                current = room;
            Repaint(rooms);
            UpdateMinimap();
        }

        Vector2 PlayerFloorPosition()
        {
            if (player == null)
                return Vector2.zero;
            Vector2 origin = generator != null ? generator.transform.position : Vector3.zero;
            return (Vector2)player.position - origin;
        }

        Vector2Int PlayerCell() => Vector2Int.FloorToInt(PlayerFloorPosition());

        bool StatesChanged(IReadOnlyList<MapRoom> rooms)
        {
            bool changed = false;
            for (int i = 0; i < rooms.Count && i < states.Length; i++)
            {
                var state = rooms[i].Room != null ? rooms[i].Room.State : Room.RoomState.Waiting;
                changed |= state != states[i];
                states[i] = state;
            }
            return changed;
        }

        void Repaint(IReadOnlyList<MapRoom> rooms)
        {
            StatesChanged(rooms);
            painter.Paint(current);

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                bool visible = MapPainter.IsVisible(room);
                if (room.Role == RoomRole.Boss)
                {
                    SetIcon(minimap.Boss, visible, painter.MarkerPosition(room));
                    SetIcon(bigMap.Boss, visible, painter.MarkerPosition(room));
                }
            }

            int reward = 0;
            foreach (var room in rooms)
            {
                if (!room.HasReward)
                    continue;
                bool pending = MapPainter.IsVisible(room) && room.Room != null && room.Room.State != Room.RoomState.Cleared;
                if (reward < minimap.Rewards.Count)
                {
                    SetIcon(minimap.Rewards[reward], pending, painter.MarkerPosition(room));
                    SetIcon(bigMap.Rewards[reward], pending, painter.MarkerPosition(room));
                }
                reward++;
            }
        }

        // Las marcas guardan su posición en píxeles del plano (centro de la sala); cada vista las coloca a su escala.
        static void SetIcon(Image icon, bool visible, Vector2 floorCenter)
        {
            if (icon == null)
                return;
            icon.enabled = visible && icon.sprite != null;
            icon.GetComponent<MapMarker>().FloorPosition = floorCenter;
        }

        void UpdateMinimap()
        {
            if (minimap == null)
                return;

            Vector2 floorPosition = PlayerFloorPosition();
            Vector2 playerPixel = painter.ToPixel(floorPosition);
            // El plano a 1 px por celda, desplazado para que el jugador quede en el centro de la ventana.
            minimap.Image.rectTransform.sizeDelta = new Vector2(painter.Texture.width, painter.Texture.height);
            minimap.Image.uvRect = new Rect(0f, 0f, 1f, 1f);
            minimap.Content.anchoredPosition = -Floor(playerPixel);
            PlaceMarkers(minimap, Vector2.zero, 1, floorPosition);
        }

        void UpdateBigMap()
        {
            var area = painter.Discovered;
            if (area.width == 0 || area.height == 0)
                return;

            var room = bigMapViewport.rect.size;
            int scale = bigMapMaxScale;
            while (scale > 1 && (area.width * scale > room.x || area.height * scale > room.y))
                scale--;

            var texture = painter.Texture;
            bigMap.Image.uvRect = new Rect((float)area.x / texture.width, (float)area.y / texture.height,
                (float)area.width / texture.width, (float)area.height / texture.height);
            bigMap.Image.rectTransform.sizeDelta = (Vector2)area.size * scale;
            // Centrado en la ventana, en píxeles enteros.
            bigMap.Content.anchoredPosition = -Round((Vector2)area.size * scale / 2f);
            PlaceMarkers(bigMap, area.position, scale, PlayerFloorPosition());
        }

        /// <summary>Coloca las marcas de una vista: el plano empieza en <paramref name="origin"/> (píxeles de la textura).</summary>
        void PlaceMarkers(View view, Vector2 origin, int scale, Vector2 playerFloorPosition)
        {
            // El píxel central del icono, sobre el de su celda (en el centro de la celda si el plano va ×2).
            Vector2 ToView(Vector2 floorPosition) =>
                Floor((Floor(painter.ToPixel(floorPosition)) - origin) * scale + Vector2.one * (scale / 2));

            view.Player.rectTransform.anchoredPosition = ToView(playerFloorPosition);
            foreach (var marker in view.Content.GetComponentsInChildren<MapMarker>())
            {
                if (marker.Image != view.Player)
                    marker.Image.rectTransform.anchoredPosition = ToView(marker.FloorPosition);
            }
        }

        void SetBigMap(bool show)
        {
            bigMapShown = show;
            if (bigMapRoot != null)
                bigMapRoot.SetActive(show);
        }

        View CreateView(RectTransform viewport, IReadOnlyList<MapRoom> rooms)
        {
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var image = new GameObject("Plan", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(content, false);
            image.texture = painter.Texture;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.zero;
            image.rectTransform.pivot = Vector2.zero;

            var view = new View { Content = content, Image = image };
            view.Boss = CreateMarker(content, bossIcon);
            foreach (var room in rooms)
            {
                if (room.HasReward)
                    view.Rewards.Add(CreateMarker(content, rewardIcon));
            }

            // El jugador, encima de todo.
            view.Player = CreateMarker(content, playerFrames.Length > 0 ? playerFrames[0] : null);
            view.Player.enabled = view.Player.sprite != null;
            if (playerFrames.Length > 1)
                view.Player.gameObject.AddComponent<UISpriteAnimation>().Configure(playerFrames, playerFrameDuration, newLoop: true);
            return view;
        }

        static Image CreateMarker(RectTransform parent, Sprite sprite)
        {
            var image = new GameObject("Marker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.raycastTarget = false;
            image.enabled = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            if (sprite != null)
            {
                rect.sizeDelta = sprite.rect.size;
                // El pivote en la esquina del píxel central ((3, 3) o (4, 4)): así ese píxel cae justo en su sitio.
                rect.pivot = new Vector2(Mathf.Floor(sprite.rect.width / 2f) / sprite.rect.width,
                    Mathf.Floor(sprite.rect.height / 2f) / sprite.rect.height);
            }
            image.gameObject.AddComponent<MapMarker>().Image = image;
            return image;
        }

        static Vector2 Round(Vector2 value) => new(Mathf.Round(value.x), Mathf.Round(value.y));

        static Vector2 Floor(Vector2 value) => new(Mathf.Floor(value.x), Mathf.Floor(value.y));
    }
}
