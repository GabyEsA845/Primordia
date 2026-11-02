using System.Collections.Generic;
using Primordia.Core;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Primordia.UI
{
    /// <summary>
    /// Minimapa del HUD (arriba a la derecha, centrado en el jugador y recortado por su recuadro) y mapa grande
    /// (TAB, con el juego detenido: todo lo descubierto, ×2 o ×1 si no cabe; si ni así cabe, se abre centrado en el jugador y
    /// se mueve arrastrando con el clic derecho). El minimapa se oculta en la pausa. Los dos muestran la misma textura
    /// de <see cref="MapPainter"/>, que se repinta al cambiar de sala o al cambiar el estado de alguna sala.
    /// Marcas: jugador, sala del jefe, salas con mutación aún sin limpiar y la tienda (cuando se descubre: al limpiar
    /// la sala de la que cuelga); los iconos no se amplían con el mapa.
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
        [SerializeField] Sprite shopIcon;
        [SerializeField] Sprite transitionIcon;

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
            public Image Shop;
            public Image Transition;
            public readonly List<Image> Rewards = new();
            public readonly Dictionary<Teleporter, Image> Teleports = new();
            public Image Return;
            public readonly Dictionary<RoomHearts, Image> Hearts = new();
        }

        // Radio (en unidades de la interfaz) en que el ratón alcanza una marca de teletransporte.
        const float TeleportClickRadius = 6f;
        Text teleportHint;
        [SerializeField] Font hintFont;
        Teleporter hoveredTeleporter;
        bool hoveredReturn;

        FloorGenerator generator;
        Transform player;
        MapPainter painter;
        View minimap;
        View bigMap;
        MapRoom current;
        Room.RoomState[] states;
        bool bigMapShown;
        bool hasFloor;
        bool minimapHidden;
        // Desplazamiento del mapa grande arrastrado con el clic derecho (unidades de la interfaz), y si ya se centró en el
        // jugador al abrirlo.
        Vector2 bigMapPan;
        bool bigMapPanReady;
        Canvas canvas;

        public MapPainter Painter => painter;
        public MapRoom CurrentRoom => current;
        public bool BigMapShown => bigMapShown;
        public Vector2 BigMapPan => bigMapPan;

        void OnEnable() => FloorGenerator.FloorChanged += OnFloorChanged;

        void OnDisable() => FloorGenerator.FloorChanged -= OnFloorChanged;

        // Piso nuevo en la misma escena (al saltar a la fosa): el mapa vuelve a empezar con él.
        void OnFloorChanged(int floor)
        {
            if (generator == null)
                generator = FindAnyObjectByType<FloorGenerator>();
            if (generator != null && generator.Built != null)
                Initialize(generator.Built);
        }

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

        internal void SetShopIcon(Sprite newShopIcon) => shopIcon = newShopIcon;

        internal void SetTransitionIcon(Sprite newTransitionIcon) => transitionIcon = newTransitionIcon;

        /// <summary>Fuente pixel del letrero del mapa grande (si no, la integrada de Unity).</summary>
        internal void SetHintFont(Font newFont) => hintFont = newFont;

        /// <summary>La tienda se marca al entrar en ella o al limpiar una sala vecina (la de la que cuelga).</summary>
        public static bool ShopDiscovered(MapRoom room)
        {
            if (MapPainter.IsVisited(room))
                return true;
            foreach (var neighbor in room.Neighbors)
            {
                if (neighbor.Room != null && neighbor.Room.State == Room.RoomState.Cleared)
                    return true;
            }
            return false;
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

            // El mapa del piso anterior, si lo había.
            foreach (var old in new[] { minimap, bigMap })
            {
                if (old != null && old.Content != null)
                    Destroy(old.Content.gameObject);
            }
            minimap = bigMap = null;
            current = null;
            if (painter != null)
                Destroy(painter.Texture);
            painter = null;

            hasFloor = floor != null && floor.Map != null;
            minimapHidden = false;
            enabled = hasFloor;
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

            // En la pausa tapaba el menú: se oculta hasta volver a jugar. También si el jugador lo ha ocultado con TAB.
            var manager = GameManager.Instance;
            bool hide = manager != null && (manager.State == GameState.Paused || manager.MinimapHidden);
            if (hide != minimapHidden)
                SetMinimapHidden(hide);

            bool show = manager != null && manager.State == GameState.Map;
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
                else if (room.Role == RoomRole.Shop)
                {
                    bool discovered = ShopDiscovered(room);
                    SetIcon(minimap.Shop, discovered, painter.MarkerPosition(room));
                    SetIcon(bigMap.Shop, discovered, painter.MarkerPosition(room));
                }
                else if (room.Role == RoomRole.Transition)
                {
                    // Como la tienda: se marca al limpiar la sala de la que cuelga, que es la del jefe.
                    bool open = ShopDiscovered(room);
                    SetIcon(minimap.Transition, open, painter.MarkerPosition(room));
                    SetIcon(bigMap.Transition, open, painter.MarkerPosition(room));
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
            UpdateHeartMarkers(minimap);
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
            Vector2 size = (Vector2)area.size * scale;
            bigMap.Image.rectTransform.sizeDelta = size;

            // Si no cabe (el piso 2 entero), se abre centrado en el jugador y se arrastra con el clic derecho.
            if (!bigMapPanReady)
            {
                bigMapPanReady = true;
                Vector2 playerPixel = (Floor(painter.ToPixel(PlayerFloorPosition())) - area.position) * scale;
                bigMapPan = size / 2f - playerPixel;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                bigMapPan += mouse.delta.ReadValue() / CanvasScale();
            bigMapPan = ClampPan(bigMapPan, size, room);

            // Centrado en la ventana (más lo arrastrado), en píxeles enteros.
            bigMap.Content.anchoredPosition = -Round(size / 2f) + Round(bigMapPan);
            UpdateTeleportMarkers();
            UpdateHeartMarkers(bigMap);
            PlaceMarkers(bigMap, area.position, scale, PlayerFloorPosition());
            HandleTeleportClick();
        }

        Vector2 ToFloor(Vector2 world)
        {
            Vector2 origin = generator != null ? generator.transform.position : Vector3.zero;
            return world - origin;
        }

        /// <summary>
        /// Marcas del mapa grande: un anillo por cada teletransporte activo que no sea el de esta sala y, si la hay, la
        /// marca de regreso. Se apagan (más tenues) mientras no se pueda viajar (sala sin limpiar).
        /// </summary>
        void UpdateTeleportMarkers()
        {
            bool canTravel = TeleportNetwork.CanTravel();
            bool art = MapTeleportIcons.HasArt;
            // Con arte, el apagado es su propio icono; el provisional se apaga con la transparencia.
            var tint = new Color(1f, 1f, 1f, canTravel || art ? 1f : 0.45f);
            int frame = Mathf.FloorToInt(Time.unscaledTime / 0.12f);
            foreach (var teleporter in Teleporter.All)
            {
                if (teleporter == null)
                    continue;
                if (!bigMap.Teleports.TryGetValue(teleporter, out var icon))
                {
                    icon = CreateMarker(bigMap.Content, MapTeleportIcons.Teleport);
                    bigMap.Teleports[teleporter] = icon;
                    // El jugador sigue siendo lo de más arriba.
                    bigMap.Player.transform.SetAsLastSibling();
                }
                bool visible = teleporter.IsActive && teleporter.Room != TeleportNetwork.CurrentRoom;
                var sprite = !canTravel && art && MapTeleportIcons.TeleportDim != null
                    ? MapTeleportIcons.TeleportDim
                    : MapTeleportIcons.TeleportFrame(frame, canTravel && teleporter == hoveredTeleporter);
                SetMarkerSprite(icon, sprite);
                icon.enabled = visible;
                icon.color = tint;
                icon.GetComponent<MapMarker>().FloorPosition = ToFloor(teleporter.Position);
            }

            bool showReturn = TeleportNetwork.HasReturn && TeleportNetwork.ReturnRoom != TeleportNetwork.CurrentRoom;
            SetMarkerSprite(bigMap.Return, MapTeleportIcons.ReturnIcon(canTravel && hoveredReturn, !canTravel));
            bigMap.Return.enabled = showReturn;
            bigMap.Return.color = tint;
            if (showReturn)
                bigMap.Return.GetComponent<MapMarker>().FloorPosition = ToFloor(TeleportNetwork.ReturnPosition);

            UpdateTeleportHint(canTravel);
        }

        /// <summary>Cambia el sprite de una marca ajustando su tamaño y pivote (el píxel central, como en CreateMarker).</summary>
        static void SetMarkerSprite(Image icon, Sprite sprite)
        {
            if (icon.sprite == sprite || sprite == null)
                return;
            icon.sprite = sprite;
            var rect = icon.rectTransform;
            rect.sizeDelta = sprite.rect.size;
            rect.pivot = new Vector2(Mathf.Floor(sprite.rect.width / 2f) / sprite.rect.width,
                Mathf.Floor(sprite.rect.height / 2f) / sprite.rect.height);
        }

        /// <summary>Un medio corazón en cada sala que tenga uno sin recoger, en la posición donde está.</summary>
        void UpdateHeartMarkers(View view)
        {
            if (view == null)
                return;
            foreach (var hearts in RoomHearts.All)
            {
                if (hearts == null)
                    continue;
                if (!view.Hearts.TryGetValue(hearts, out var icon))
                {
                    icon = CreateMarker(view.Content, MapTeleportIcons.Heart);
                    view.Hearts[hearts] = icon;
                    view.Player.transform.SetAsLastSibling();
                }
                bool visible = hearts.HasUnclaimedHeart;
                icon.enabled = visible;
                if (visible)
                    icon.GetComponent<MapMarker>().FloorPosition = ToFloor(hearts.Dropped.transform.position);
            }
        }

        void UpdateTeleportHint(bool canTravel)
        {
            bool any = TeleportNetwork.Destinations().Length > 0 || TeleportNetwork.HasReturn;
            if (teleportHint == null)
            {
                if (bigMapRoot == null)
                    return;
                var go = new GameObject("TeleportHint", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(bigMapRoot.transform, false);
                teleportHint = go.GetComponent<Text>();
                teleportHint.font = hintFont != null ? hintFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                teleportHint.fontSize = 8;
                teleportHint.alignment = TextAnchor.LowerCenter;
                teleportHint.raycastTarget = false;
                teleportHint.horizontalOverflow = HorizontalWrapMode.Overflow;
                var rect = teleportHint.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 6f);
                rect.sizeDelta = new Vector2(0f, 14f);
            }
            teleportHint.enabled = any;
            teleportHint.text = canTravel ? "Clic en un teletransporte para viajar" : "Limpia la sala para teletransportarte";
            teleportHint.color = canTravel ? new Color(0.5f, 0.95f, 0.88f) : new Color(1f, 0.75f, 0.45f);
        }

        /// <summary>Clic izquierdo sobre una marca de teletransporte (o la de regreso): viaja.</summary>
        void HandleTeleportClick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !TeleportNetwork.CanTravel())
                return;
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bigMap.Content, mouse.position.ReadValue(), camera, out var local))
                return;

            Teleporter hit = null;
            bool hitReturn = false;
            float best = TeleportClickRadius;
            foreach (var (teleporter, icon) in bigMap.Teleports)
            {
                if (teleporter == null || !icon.enabled)
                    continue;
                float distance = Vector2.Distance(local, icon.rectTransform.anchoredPosition);
                if (distance <= best)
                {
                    best = distance;
                    hit = teleporter;
                    hitReturn = false;
                }
            }
            if (bigMap.Return.enabled && Vector2.Distance(local, bigMap.Return.rectTransform.anchoredPosition) <= best)
            {
                hit = null;
                hitReturn = true;
            }

            // Bajo el ratón la marca se resalta (con arte, su variante más clara; el provisional crece un poco).
            hoveredTeleporter = hit;
            hoveredReturn = hitReturn;
            float grow = MapTeleportIcons.HasArt ? 1f : 1.4f;
            foreach (var (teleporter, icon) in bigMap.Teleports)
                icon.rectTransform.localScale = teleporter == hit ? Vector3.one * grow : Vector3.one;
            bigMap.Return.rectTransform.localScale = hitReturn ? Vector3.one * grow : Vector3.one;

            if (!mouse.leftButton.wasPressedThisFrame)
                return;
            if (hitReturn)
                TeleportNetwork.TravelBack();
            else if (hit != null)
                TeleportNetwork.TravelTo(hit);
        }

        /// <summary>
        /// Lo que se puede mover el plano en cada eje: nada si cabe; si no, hasta que su borde llega al de la ventana.
        /// </summary>
        internal static Vector2 ClampPan(Vector2 pan, Vector2 content, Vector2 viewport)
        {
            Vector2 slack = Vector2.Max(Vector2.zero, content - viewport) / 2f;
            return new Vector2(Mathf.Clamp(pan.x, -slack.x, slack.x), Mathf.Clamp(pan.y, -slack.y, slack.y));
        }

        float CanvasScale()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            float factor = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            return factor > 0f ? factor : 1f;
        }

        void SetMinimapHidden(bool hide)
        {
            minimapHidden = hide;
            if (minimapRoot != null)
                minimapRoot.SetActive(hasFloor && !hide);
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
            // Cada vez que se abre vuelve a centrarse en el jugador.
            bigMapPanReady = false;
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
                if (room.Role == RoomRole.Shop)
                    view.Shop = CreateMarker(content, shopIcon);
                else if (room.Role == RoomRole.Transition)
                    view.Transition = CreateMarker(content, transitionIcon);
            }
            foreach (var room in rooms)
            {
                if (room.HasReward)
                    view.Rewards.Add(CreateMarker(content, rewardIcon));
            }

            view.Return = CreateMarker(content, MapTeleportIcons.Return);

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
