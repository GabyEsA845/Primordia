using System.Collections;
using Primordia.Audio;
using Primordia.Core;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Muro de raíces de la sala secreta (piso 3): cierra la puerta de arriba de la antesala. Se ve siempre, apagado, aunque nunca hayas
    /// cogido el corazón morado (es la pista para la siguiente partida). Con un corazón morado o con la semilla morada, el hueco del nudo
    /// late y con E se pone: el muro se abre y las raíces se retiran. Gastar el corazón (o la semilla) es el precio de abrirlo; si hay
    /// las dos cosas, se usa la semilla.
    /// </summary>
    public class RootWall : PromptInteractable
    {
        const float InsertFrameSeconds = 0.11f;
        const float IdleFrameSeconds = 0.2f;
        // Segundos de cada cuadro de la apertura (10 cuadros) y el que cuenta (el 8.º, desde 1): desde ahí el centro ya no bloquea.
        static readonly float[] OpenSeconds = { 0.11f, 0.11f, 0.11f, 0.16f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.2f };
        const int OpensAtFrame = 8;
        // Por encima del jugador (y de lo que lleva), como el marco de las puertas normales.
        const int FrontOrder = 40;
        // Los pies del jugador respecto al centro de su cuerpo.
        const float FootOffset = -0.3f;
        static readonly Color Purple = new(0.75f, 0.45f, 1f);

        [SerializeField] SpriteRenderer body;
        [SerializeField] Collider2D blocker;
        [SerializeField] SpriteRenderer front;
        Transform player;
        Vector2 doorCenter;
        float halfWidth;
        float archBottom;
        SpecialRoomArt.RootWallArt art;
        Health health;
        bool busy;

        public bool IsOpen { get; private set; }

        /// <summary>True mientras el jugador cruza el hueco del muro abierto (para las pruebas).</summary>
        public bool Crossing { get; private set; }

        /// <summary>Se dispara cuando el muro deja de bloquear.</summary>
        public event System.Action Opened;

        public override bool IsAvailable => !IsOpen && !busy;

        bool HasSeed => health != null && health.PurpleSeed;
        bool HasHeart => health != null && health.PurpleHearts > 0;
        bool HasKey => HasSeed || HasHeart;

        protected override string TooltipTitle => "Muro de raíces";

        protected override string TooltipDetails => HasSeed ? "E para plantar la semilla morada"
            : HasHeart ? "E para poner el corazón morado"
            : "Un hueco con forma de corazón. Le falta algo morado";

        protected override Color TooltipColor => Purple;

        /// <summary>Monta el muro: <paramref name="art"/>, su sprite y el colisionador que cierra el paso hasta que se abre.</summary>
        internal static RootWall Create(Transform parent, SpecialRoomArt.RootWallArt art, Vector2 doorCenter, float wallBottom, Vector2 blockerSize,
            Sprite[] keyFrames, float keyHeight)
        {
            var go = new GameObject("RootWall");
            go.transform.SetParent(parent, false);
            go.transform.position = doorCenter;

            var sprite = new GameObject("Sprite");
            sprite.transform.SetParent(go.transform, false);
            sprite.transform.position = new Vector3(doorCenter.x, wallBottom, 0f);
            var renderer = sprite.AddComponent<SpriteRenderer>();
            renderer.sprite = art.locked;
            // Sobre las paredes (-10) y los marcos de puerta, bajo los personajes.
            renderer.sortingOrder = -7;

            // Capa de delante (el muro sin el interior del hueco): mientras el jugador cruza, pasa por detrás de las raíces y la fronda.
            SpriteRenderer frontRenderer = null;
            if (art.openedFront != null)
            {
                var frontObject = new GameObject("Front");
                frontObject.transform.SetParent(sprite.transform, false);
                frontRenderer = frontObject.AddComponent<SpriteRenderer>();
                frontRenderer.sprite = art.openedFront;
                frontRenderer.sortingOrder = FrontOrder;
                frontRenderer.enabled = false;
            }

            var box = go.AddComponent<BoxCollider2D>();
            box.size = blockerSize;

            var wall = go.AddComponent<RootWall>();
            wall.art = art;
            wall.body = renderer;
            wall.blocker = box;
            wall.front = frontRenderer;
            wall.doorCenter = doorCenter;
            wall.halfWidth = blockerSize.x / 2f;
            wall.archBottom = doorCenter.y - blockerSize.y / 2f;
            wall.ConfigurePrompt(keyFrames, keyHeight);
            wall.SetRadius(3f);
            return wall;
        }

        protected override void Start()
        {
            base.Start();
            FindPlayer();
        }

        void FindPlayer()
        {
            if (health != null)
                return;
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                player.TryGetComponent(out health);
        }

        void Update()
        {
            UpdateCrossing();
            if (IsOpen || busy || art == null)
                return;
            FindPlayer();
            // Sin llave, el muro está apagado; con ella, el hueco late.
            body.sprite = HasKey && art.idle.Length > 0 ? art.idle[Mathf.FloorToInt(Time.time / IdleFrameSeconds) % art.idle.Length] : art.locked;
        }

        /// <summary>Con el muro abierto, la capa de delante tapa al jugador mientras está dentro del hueco (pies entre las raíces).</summary>
        void UpdateCrossing()
        {
            if (front == null)
                return;
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found == null)
                    return;
                player = found.transform;
            }
            var feet = (Vector2)player.position + Vector2.up * FootOffset;
            Crossing = IsOpen && !busy && Mathf.Abs(feet.x - doorCenter.x) < halfWidth && feet.y >= archBottom;
            front.enabled = Crossing;
        }

        public override void Interact(GameObject player)
        {
            if (IsOpen || busy || player == null || !player.TryGetComponent(out health) || !HasKey)
                return;
            bool seed = HasSeed;
            // Con las dos cosas se planta la semilla: el corazón morado sigue parando un golpe.
            if (seed)
                health.PurpleSeed = false;
            else
                health.PurpleHearts--;
            HidePrompt();
            StartCoroutine(Open(seed));
        }

        IEnumerator Open(bool seed)
        {
            busy = true;
            Sfx.Play(Sounds.HeartCoral);
            foreach (var frame in seed ? art.insertSeed : art.insertHeart)
            {
                body.sprite = frame;
                yield return new WaitForSeconds(InsertFrameSeconds);
            }

            for (int i = 0; i < art.open.Length; i++)
            {
                body.sprite = art.open[i];
                if (i == OpensAtFrame - 1)
                    Release();
                yield return new WaitForSeconds(OpenSeconds[Mathf.Min(i, OpenSeconds.Length - 1)]);
            }
            if (!IsOpen)
                Release();
            body.sprite = art.opened;
            busy = false;
        }

        void Release()
        {
            IsOpen = true;
            if (blocker != null)
                blocker.enabled = false;
            Opened?.Invoke();
        }
    }
}
