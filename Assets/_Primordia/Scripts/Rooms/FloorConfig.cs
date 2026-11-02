using System;
using Primordia.Biomass;
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Shop;
using UnityEngine;

namespace Primordia.Rooms
{
    /// <summary>
    /// Todo lo que el generador necesita para montar un piso al azar: formas de sala, cuántas salas,
    /// tileset, enemigos con su coste, jefe y recompensas. Un asset por piso.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Rooms/Floor Config", fileName = "Floor_")]
    public class FloorConfig : ScriptableObject
    {
        /// <summary>Un tipo de enemigo que el generador puede poner, con lo que gasta del presupuesto de la sala.</summary>
        [Serializable]
        public struct EnemyEntry
        {
            public Enemy prefab;
            [Min(1)] public int cost;
            [Tooltip("A partir de qué sala de combate del camino puede salir (1 = desde la primera).")]
            [Min(1)] public int minDepth;
            [Tooltip("Cuántos de este enemigo puede haber como mucho en una sala (la Esponja Vauxia, una). 0 = sin tope.")]
            [Min(0)] public int maxPerRoom;
            [Tooltip("Cuántos salen juntos cada vez que se elige (el cardumen de Pikaia, 5); el coste es por grupo. 0 o 1 = uno.")]
            [Min(0)] public int groupSize;

            public EnemyEntry(Enemy prefab, int cost, int minDepth = 1, int maxPerRoom = 0, int groupSize = 1)
            {
                this.prefab = prefab;
                this.cost = cost;
                this.minDepth = minDepth;
                this.maxPerRoom = maxPerRoom;
                this.groupSize = groupSize;
            }

            /// <summary>Cuántos salen juntos (al menos 1).</summary>
            public int GroupSize => Mathf.Max(1, groupSize);
        }

        /// <summary>Un jefe posible del piso, con su sala y su tileset. En cada partida sale uno al azar.</summary>
        [Serializable]
        public struct BossEntry
        {
            public Enemy prefab;
            public RoomTemplate room;
            public RoomTheme theme;
            [Tooltip("Posición del jefe respecto al centro del suelo de su sala.")]
            public Vector2 offset;

            public BossEntry(Enemy prefab, RoomTemplate room, RoomTheme theme, Vector2 offset)
            {
                this.prefab = prefab;
                this.room = room;
                this.theme = theme;
                this.offset = offset;
            }

            public bool IsValid => prefab != null && room != null;
        }

        /// <summary>Peso de cada dirección al trazar el camino principal en la rejilla.</summary>
        [Serializable]
        public struct PathWeights
        {
            [Min(0f)] public float down;
            [Tooltip("Peso de la izquierda y, por separado, el de la derecha.")]
            [Min(0f)] public float side;
            [Min(0f)] public float up;

            public PathWeights(float down, float side, float up)
            {
                this.down = down;
                this.side = side;
                this.up = up;
            }

            public static PathWeights Even => new(1f, 1f, 1f);

            /// <summary>El camino del piso 2: abajo a menudo, a los lados a veces, casi nunca hacia arriba.</summary>
            public static PathWeights Descending => new(6f, 2f, 0.3f);

            public bool IsEven => Mathf.Approximately(down, side) && Mathf.Approximately(side, up);

            public float For(Vector2Int direction) =>
                direction == Vector2Int.down ? down : direction == Vector2Int.up ? up : side;
        }

        /// <summary>Un tipo de cueva de combate (piso 3): tamaño, silueta, si lleva lagos de lodo y lo frecuente que es.</summary>
        [Serializable]
        public struct CaveSpec
        {
            public Vector2Int size;
            public CaveGenerator.CaveShape shape;
            [Tooltip("Con lagos de lodo profundo y sus islas.")]
            public bool lakes;
            [Min(0.01f)] public float weight;

            public CaveSpec(Vector2Int size, CaveGenerator.CaveShape shape, bool lakes, float weight = 1f)
            {
                this.size = size;
                this.shape = shape;
                this.lakes = lakes;
                this.weight = weight;
            }
        }

        /// <summary>Dónde sale una sala especial: en el camino principal o colgando de él.</summary>
        public enum SpecialPlacement
        {
            Path,
            Side
        }

        /// <summary>Una sala especial hecha a mano (piso 2: Parkour, Corazón morado, Anillo de la sima…).</summary>
        [Serializable]
        public struct SpecialRoom
        {
            public RoomTemplate template;
            public SpecialPlacement placement;
            [Tooltip("En el camino, cuenta como una de las salas de combate (con su recompensa si le toca). Si no, se añade aparte.")]
            public bool countsAsCombat;
            [Tooltip("Enemigos respecto a una sala normal: 1 igual, 2 el doble, 0 ninguno.")]
            [Min(0f)] public float enemyMultiplier;
            [Tooltip("Sale en todas las partidas (el Corazón morado); las demás se sortean.")]
            public bool always;
            [Tooltip("Franja de profundidad en la que tiene que salir (0 Someras, 1 Arrecife, 2 Abismo; -1 cualquiera).")]
            public int band;
            [Tooltip("Ocupa dos casillas en vertical: se entra por arriba y se sale por abajo (Parkour).")]
            public bool tall;
            [Tooltip("Oleadas de enemigos que salen por las entradas de oleadas (E), una tras otra (0: los enemigos de siempre). "
                + "Cada oleada lleva los enemigos de una sala normal por el multiplicador de arriba.")]
            [Min(0)] public int waves;
            [Tooltip("Ofrece una mutación al limpiarla, toque o no por su número (la Sala sellada).")]
            public bool rewardAtEnd;
            [Tooltip("Candidata a la sala de la almeja: lleva almeja grande y en cada partida sale una de las candidatas aparte "
                + "del sorteo de especiales (Gruta escondida, Anillo de la sima, Jardín de esponjas).")]
            public bool clamRoom;

            public bool IsValid => template != null;
        }

        /// <summary>Sprites de las piezas de las salas especiales (huesos, almejas, coral, entradas de oleadas).</summary>
        [Serializable]
        public struct RoomPieces
        {
            [Tooltip("Esqueleto del Fósil gigante, con el origen en la esquina de arriba a la izquierda de la sala.")]
            public Sprite fossil;
            public Sprite clamSmall;
            public Sprite clamBig;
            public Sprite heartCoral;
            public Sprite waveVent;

            [Tooltip("Almeja pequeña: respira (bucle), se abre (una vez) y abierta.")]
            public Sprite[] clamSmallIdle;
            public Sprite[] clamSmallOpen;
            public Sprite clamSmallOpened;
            [Tooltip("Almeja grande (32×24, sobresale 8 px por arriba de sus dos celdas).")]
            public Sprite[] clamBigIdle;
            public Sprite[] clamBigOpen;
            public Sprite clamBigOpened;
            [Tooltip("Coral con el corazón morado latiendo, y el coral sin él.")]
            public Sprite[] heartCoralIdle;
            public Sprite heartCoralTaken;
            [Tooltip("Entrada de oleadas: en reposo (bucle) y activa (se abre, brilla en rojo y burbujea).")]
            public Sprite[] waveVentIdle;
            public Sprite[] waveVentActive;
            [Tooltip("Tecla E normal y pulsada, sobre lo que se puede usar.")]
            public Sprite[] keyPrompt;
            [Tooltip("Columnas de algas de la sala del Anomalocaris (dos variantes, bucle), con el pivote en la base.")]
            public Sprite[] algaeColumnA;
            public Sprite[] algaeColumnB;
            [Tooltip("Planta luminosa: latido (bucle) y apagarse.")]
            public Sprite[] lightPlant;
            public Sprite[] lightPlantOff;
            [Tooltip("Rocas que se derrumban con la muda del Euriptérido (marca R): agrietada, su contorno, el derrumbe y los escombros.")]
            public Sprite crackedRock;
            public Sprite crackedRockOutline;
            public Sprite[] rockCollapse;
            public Sprite rubble;
            [Tooltip("Derrumbe de la cueva del Euriptérido: sombra de aviso, piedra que cae, impacto, chinas decorativas y su polvillo, y grietas del suelo que se hunde (3×3).")]
            public Sprite[] fallShadow;
            public Sprite[] fallingRock;
            public Sprite[] rockImpact;
            public Sprite[] pebbles;
            public Sprite[] pebblePuff;
            public Sprite[] floorCrack;
        }

        /// <summary>Lo que guardan las almejas: biomasa siempre y, la grande, una mutación especial si las hay.</summary>
        [Serializable]
        public struct ClamContents
        {
            [Tooltip("Biomasa de la almeja pequeña (mínimo y máximo).")]
            public Vector2Int smallBiomass;
            [Tooltip("Biomasa de la almeja grande (mínimo y máximo).")]
            public Vector2Int bigBiomass;
            [Tooltip("Mutaciones especiales que solo salen en almejas grandes (una al azar que no se lleve). Vacío: solo biomasa.")]
            public MutationData[] bigMutations;
            [Tooltip("Burbuja de mutación que suelta la almeja grande.")]
            public MutationPickup mutationPickup;

            public static ClamContents Default => new()
            {
                smallBiomass = new Vector2Int(5, 15),
                bigBiomass = new Vector2Int(15, 25),
                bigMutations = Array.Empty<MutationData>(),
            };
        }

        [Header("Piso")]
        [Tooltip("Número del piso (1 = sopa primordial, 2 = océano cámbrico…).")]
        [SerializeField, Min(1)] int floorNumber = 1;
        [Tooltip("Piso al que lleva la fosa de la sala de paso. Vacío: la fosa (o la salida) da la victoria.")]
        [SerializeField] FloorConfig nextFloor;

        [Header("Salas")]
        [SerializeField] RoomTemplate startRoom;
        [SerializeField] RoomTemplate bossRoom;
        [Tooltip("Formas para las salas de combate del camino principal.")]
        [SerializeField] RoomTemplate[] combatRooms = Array.Empty<RoomTemplate>();
        [Tooltip("Formas para las salas laterales (sin salida).")]
        [SerializeField] RoomTemplate[] sideRooms = Array.Empty<RoomTemplate>();
        [Tooltip("Salas de combate en el camino principal, entre el inicio y el jefe (mínimo y máximo).")]
        [SerializeField] Vector2Int mainCombatRooms = new(4, 5);
        [Tooltip("Salas laterales que cuelgan del camino (mínimo y máximo).")]
        [SerializeField] Vector2Int sideRoomCount = new(1, 2);

        [Header("Cuevas y pasillos")]
        [Tooltip("Las salas de inicio, de combate y laterales son cuevas generadas con la semilla del piso (CaveGenerator). "
            + "Si una no pasa la comprobación, se usa una de las formas de arriba como catálogo de reserva.")]
        [SerializeField] bool generateCaves;
        [Tooltip("Tamaños posibles (paredes incluidas) de las cuevas de combate; se elige uno al azar por sala.")]
        [SerializeField] Vector2Int[] caveSizes = Array.Empty<Vector2Int>();
        [Tooltip("Si no está vacío, sustituye a los tamaños de arriba en las salas de combate: cada sala sortea un tipo con su peso "
            + "(óvalo o L, con o sin lagos de lodo).")]
        [SerializeField] CaveSpec[] caveSpecs = Array.Empty<CaveSpec>();
        [Tooltip("Primera sala de combate de cada franja nueva: su suelo mezcla el tileset de la franja anterior y el de la suya "
            + "con una mancha irregular (más del nuevo hacia la salida). Piso 3: el incendio avanza.")]
        [SerializeField] bool blendBandBorders;
        [Tooltip("Atajos de maleza quemable por partida (mínimo y máximo): una puerta extra entre dos salas vecinas del mapa que el "
            + "camino no unía (con al menos 3 salas de diferencia), sellada con maleza hasta que arde. 0: sin atajos.")]
        [SerializeField] Vector2Int shortcuts = Vector2Int.zero;
        [Tooltip("Tamaños posibles de las cuevas de inicio y laterales.")]
        [SerializeField] Vector2Int[] smallCaveSizes = Array.Empty<Vector2Int>();
        [Tooltip("Largo mínimo del pasillo entre salas vecinas, en tiles (mínimo y máximo; uno al azar por hueco de la rejilla).")]
        [SerializeField] Vector2Int corridorLength = new(4, 4);
        [Tooltip("Ancho de los pasillos (mínimo y máximo). Con más de 2, túneles irregulares que se estrechan a 2 junto a las puertas.")]
        [SerializeField] Vector2Int corridorWidth = new(2, 2);
        [Tooltip("Peso de cada dirección al trazar el camino: abajo, cada lado y arriba. Iguales: giros al azar, como en el piso 1.")]
        [SerializeField] PathWeights pathWeights = PathWeights.Even;

        [Header("Salas especiales y fijas (piso 2)")]
        [SerializeField] SpecialRoom[] specialRooms = Array.Empty<SpecialRoom>();
        [Tooltip("Salas especiales por partida, contando las que salen siempre (mínimo y máximo).")]
        [SerializeField] Vector2Int specialRoomCount = new(2, 4);
        [Tooltip("Sala de cambio (laboratorio de Darwin), en fondo de saco detrás de la tienda. Vacío: no hay.")]
        [SerializeField] RoomTemplate exchangeRoom;
        [Tooltip("Antesala del jefe, en línea recta con él: el túnel vertical (puertas arriba y abajo). Vacío: sin antesala.")]
        [SerializeField] RoomTemplate antechamberVertical;
        [Tooltip("El túnel horizontal (puertas a izquierda y derecha).")]
        [SerializeField] RoomTemplate antechamberHorizontal;
        [SerializeField] RoomPieces pieces;
        [SerializeField] ClamContents clams = ClamContents.Default;

        [Header("Ambiente")]
        [Tooltip("Copos de nieve marina (variantes). La cantidad por sala la da el tileset de su franja.")]
        [SerializeField] Sprite[] marineSnowFlakes = Array.Empty<Sprite>();
        [Tooltip("Material de las partículas de nieve. Vacío: sin nieve.")]
        [SerializeField] Material marineSnowMaterial;

        [Header("Tileset")]
        [SerializeField] RoomTheme theme;
        [SerializeField] RoomTheme bossTheme;
        [Tooltip("Franjas de profundidad por tercios del camino (de la primera sala a la última), cada una con su tileset. "
            + "Vacío: todas las salas con el tileset de arriba.")]
        [SerializeField] RoomTheme[] bandThemes = Array.Empty<RoomTheme>();

        [Header("Enemigos")]
        [SerializeField] EnemyEntry[] enemies = Array.Empty<EnemyEntry>();
        [Tooltip("Presupuesto de la primera sala de combate en una sala normal (26×12 de suelo).")]
        [SerializeField, Min(0f)] float baseBudget = 3f;
        [Tooltip("Cuánto sube el presupuesto en cada sala de combate siguiente del camino.")]
        [SerializeField, Min(0f)] float budgetPerRoom = 1.4f;
        [Tooltip("Suelo de referencia, en tiles: en salas más grandes el presupuesto crece con la raíz del área.")]
        [SerializeField, Min(1)] int referenceFloorArea = 26 * 12;
        [SerializeField] Enemy boss;
        [Tooltip("Posición del jefe respecto al centro del suelo de su sala.")]
        [SerializeField] Vector2 bossOffset = new(0f, 2.5f);
        [Tooltip("Jefes posibles, cada uno con su sala y su tileset; en cada partida sale uno al azar, todos con la misma "
            + "probabilidad. Vacío: el jefe, la sala y el tileset de arriba.")]
        [SerializeField] BossEntry[] bosses = Array.Empty<BossEntry>();

        [Header("Recompensas")]
        [SerializeField] MutationPool rewardPool;
        [SerializeField, Min(1)] int rewardOptions = 3;
        [Tooltip("Salas de combate del camino (1 = la primera) que ofrecen mutación al limpiarlas. Las laterales siempre.")]
        [SerializeField] int[] rewardAfter = { 2, 4 };

        [Header("Biomasa")]
        [Tooltip("Vacío: las salas no sueltan biomasa.")]
        [SerializeField] BiomassPickup smallBiomass;
        [SerializeField] BiomassPickup bigBiomass;
        [Tooltip("Biomasa mínima de cada sala con enemigos.")]
        [SerializeField, Min(0)] int minimumBiomassPerRoom = 2;

        [Header("Tienda")]
        [Tooltip("Forma de la sala de la tienda. Vacío (o sin prefab): el piso no tiene tienda.")]
        [SerializeField] RoomTemplate shopRoom;
        [SerializeField] ShopRoom shopPrefab;
        [Tooltip("Tramo del camino en el que puede colgar la tienda, en fracción de las salas de combate (0 = la primera, "
            + "1 = la última). Piso 1: la segunda mitad; piso 2: el tercio del medio.")]
        [SerializeField] Vector2 shopDepth = new(0.5f, 1f);
        [Tooltip("Cápsula del laboratorio de Darwin, en la K de la sala de cambio. Vacío: la sala de cambio sale vacía.")]
        [SerializeField] ExchangeCapsule exchangeCapsule;
        [Tooltip("Teletransporte de la sala inicial, la tienda y la antesala del jefe. Vacío: el piso no tiene.")]
        [SerializeField] Teleporter teleporterPrefab;

        [Header("Sala de paso")]
        [Tooltip("Forma de la sala de paso, pegada a la del jefe. Vacío (o sin fosa): el jefe deja la salida de siempre.")]
        [SerializeField] RoomTemplate transitionRoom;
        [SerializeField] TransitionPit transitionPit;
        [Tooltip("Tileset de la zona intermedia de la sala de paso, a medio camino entre este piso y el siguiente.")]
        [SerializeField] RoomTheme transitionMidTheme;
        [Tooltip("Tileset del piso siguiente: el de la zona de la fosa.")]
        [SerializeField] RoomTheme nextFloorTheme;
        [Tooltip("Bocas del piso siguiente (una por familia): al saltar a la fosa hay que elegir una.")]
        [SerializeField] MutationData[] nextFloorMouths = Array.Empty<MutationData>();
        [Tooltip("Nombre del piso siguiente, para la ventana de elección de Boca (\"Así llegarás al …\").")]
        [SerializeField] string nextFloorName = "océano cámbrico";

        [Header("Salida")]
        [SerializeField] Sprite exitSprite;
        [SerializeField] Color exitColor = new(0.3f, 0.95f, 0.8f);

        public int FloorNumber => floorNumber;
        public FloorConfig NextFloor => nextFloor;
        public RoomTheme[] BandThemes => bandThemes ?? Array.Empty<RoomTheme>();
        public bool HasBands => Array.Exists(BandThemes, band => band != null);
        public Vector2 ShopDepth => shopDepth;
        public RoomTemplate StartRoom => startRoom;
        public RoomTemplate BossRoom => bossRoom;
        public RoomTemplate[] CombatRooms => combatRooms;
        public RoomTemplate[] SideRooms => sideRooms;
        public Vector2Int MainCombatRooms => mainCombatRooms;
        public Vector2Int SideRoomCount => sideRoomCount;
        public bool GeneratesCaves => generateCaves;
        public Vector2Int[] CaveSizes => caveSizes ?? Array.Empty<Vector2Int>();
        public CaveSpec[] CaveSpecs => caveSpecs ?? Array.Empty<CaveSpec>();
        public bool BlendBandBorders => blendBandBorders;
        public Vector2Int ShortcutCount => shortcuts;
        public Vector2Int[] SmallCaveSizes => smallCaveSizes is { Length: > 0 } ? smallCaveSizes : CaveSizes;
        public Vector2Int CorridorLength => corridorLength;
        public Vector2Int CorridorWidth => corridorWidth;
        public PathWeights Path => pathWeights;
        public SpecialRoom[] SpecialRooms => Array.FindAll(specialRooms ?? Array.Empty<SpecialRoom>(), room => room.IsValid);
        public Vector2Int SpecialRoomCount => specialRoomCount;
        public RoomTemplate ExchangeRoom => exchangeRoom;
        public bool HasExchange => exchangeRoom != null && HasShop;
        public RoomTemplate AntechamberVertical => antechamberVertical;
        public RoomTemplate AntechamberHorizontal => antechamberHorizontal;
        public bool HasAntechamber => antechamberVertical != null && antechamberHorizontal != null;
        public RoomPieces Pieces => pieces;
        public ClamContents Clams => clams;
        public Sprite[] MarineSnowFlakes => marineSnowFlakes ?? Array.Empty<Sprite>();
        public Material MarineSnowMaterial => marineSnowMaterial;
        public RoomTheme Theme => theme;
        public RoomTheme BossTheme => bossTheme;
        public EnemyEntry[] Enemies => enemies;
        public float BaseBudget => baseBudget;
        public float BudgetPerRoom => budgetPerRoom;
        public int ReferenceFloorArea => referenceFloorArea;
        public Enemy Boss => boss;
        public Vector2 BossOffset => bossOffset;
        public BossEntry[] Bosses => bosses;

        /// <summary>Los jefes posibles; si no hay lista, el jefe de siempre con su sala y su tileset.</summary>
        public BossEntry[] BossOptions()
        {
            var valid = Array.FindAll(bosses ?? Array.Empty<BossEntry>(), entry => entry.IsValid);
            if (valid.Length > 0)
                return valid;
            return new[] { new BossEntry(boss, bossRoom, bossTheme, bossOffset) };
        }

        /// <summary>Elige el jefe de la partida, todos con la misma probabilidad.</summary>
        public BossEntry ChooseBoss(System.Random random)
        {
            var options = BossOptions();
            return options[random.Next(options.Length)];
        }
        public MutationPool RewardPool => rewardPool;
        public int RewardOptions => rewardOptions;
        public int[] RewardAfter => rewardAfter;
        public BiomassPickup SmallBiomass => smallBiomass;
        public BiomassPickup BigBiomass => bigBiomass;
        public int MinimumBiomassPerRoom => minimumBiomassPerRoom;
        public RoomTemplate ShopTemplate => shopRoom;
        public ShopRoom ShopPrefab => shopPrefab;
        public ExchangeCapsule ExchangeCapsule => exchangeCapsule;
        public Teleporter TeleporterPrefab => teleporterPrefab;
        public bool HasShop => shopRoom != null && shopPrefab != null;
        public RoomTemplate TransitionRoom => transitionRoom;
        public TransitionPit TransitionPit => transitionPit;
        public RoomTheme TransitionMidTheme => transitionMidTheme;
        public RoomTheme NextFloorTheme => nextFloorTheme;
        public bool HasTransition => transitionRoom != null && transitionPit != null;
        public MutationData[] NextFloorMouths => nextFloorMouths ?? Array.Empty<MutationData>();
        public string NextFloorName => nextFloorName;
        public Sprite ExitSprite => exitSprite;
        public Color ExitColor => exitColor;

        internal void ConfigureRooms(RoomTemplate newStart, RoomTemplate newBoss, RoomTemplate[] newCombat, RoomTemplate[] newSide,
            Vector2Int newMainCombat, Vector2Int newSideCount)
        {
            startRoom = newStart;
            bossRoom = newBoss;
            combatRooms = newCombat;
            sideRooms = newSide;
            mainCombatRooms = newMainCombat;
            sideRoomCount = newSideCount;
        }

        internal void ConfigureArt(RoomTheme newTheme, RoomTheme newBossTheme, Sprite newExitSprite)
        {
            theme = newTheme;
            bossTheme = newBossTheme;
            exitSprite = newExitSprite;
        }

        internal void ConfigureEnemies(EnemyEntry[] newEnemies, Enemy newBoss, float newBaseBudget = 3f, float newBudgetPerRoom = 1.4f)
        {
            enemies = newEnemies;
            boss = newBoss;
            baseBudget = newBaseBudget;
            budgetPerRoom = newBudgetPerRoom;
        }

        /// <summary>Cambia solo los enemigos (el jefe y el presupuesto se quedan).</summary>
        internal void SetEnemies(EnemyEntry[] newEnemies) => enemies = newEnemies ?? Array.Empty<EnemyEntry>();

        internal void ConfigureBosses(BossEntry[] newBosses) => bosses = newBosses ?? Array.Empty<BossEntry>();

        internal void ConfigureBiomass(BiomassPickup newSmall, BiomassPickup newBig, int newMinimum = 2)
        {
            smallBiomass = newSmall;
            bigBiomass = newBig;
            minimumBiomassPerRoom = Mathf.Max(0, newMinimum);
        }

        internal void ConfigureShop(RoomTemplate newShopRoom, ShopRoom newShopPrefab)
        {
            shopRoom = newShopRoom;
            shopPrefab = newShopPrefab;
        }

        internal void SetExchangeCapsule(ExchangeCapsule newCapsule) => exchangeCapsule = newCapsule;

        internal void SetTeleporter(Teleporter newTeleporter) => teleporterPrefab = newTeleporter;

        internal void ConfigureTransition(RoomTemplate newRoom, TransitionPit newPit, RoomTheme newMidTheme, RoomTheme newNextTheme)
        {
            transitionRoom = newRoom;
            transitionPit = newPit;
            transitionMidTheme = newMidTheme;
            nextFloorTheme = newNextTheme;
        }

        internal void ConfigureNextFloor(MutationData[] newMouths, string newName)
        {
            nextFloorMouths = newMouths ?? Array.Empty<MutationData>();
            nextFloorName = newName;
        }

        internal void ConfigureFloor(int newNumber, FloorConfig newNext)
        {
            floorNumber = Mathf.Max(1, newNumber);
            nextFloor = newNext;
        }

        internal void ConfigureCaves(bool generate, Vector2Int[] newCaveSizes, Vector2Int[] newSmallCaveSizes)
        {
            generateCaves = generate;
            caveSizes = newCaveSizes ?? Array.Empty<Vector2Int>();
            smallCaveSizes = newSmallCaveSizes ?? Array.Empty<Vector2Int>();
        }

        internal void ConfigureShortcuts(Vector2Int newShortcuts) => shortcuts = newShortcuts;

        internal void ConfigureCaveSpecs(CaveSpec[] newSpecs, bool newBlendBandBorders)
        {
            caveSpecs = newSpecs ?? Array.Empty<CaveSpec>();
            blendBandBorders = newBlendBandBorders;
        }

        internal void ConfigureSpecialRooms(SpecialRoom[] newRooms, Vector2Int newCount)
        {
            specialRooms = newRooms ?? Array.Empty<SpecialRoom>();
            specialRoomCount = newCount;
        }

        internal void ConfigureFixedRooms(RoomTemplate newExchange, RoomTemplate newAntechamberVertical, RoomTemplate newAntechamberHorizontal)
        {
            exchangeRoom = newExchange;
            antechamberVertical = newAntechamberVertical;
            antechamberHorizontal = newAntechamberHorizontal;
        }

        internal void ConfigurePieces(RoomPieces newPieces) => pieces = newPieces;

        internal void ConfigureClams(ClamContents newClams) => clams = newClams;

        internal void ConfigureMarineSnow(Sprite[] newFlakes, Material newMaterial)
        {
            marineSnowFlakes = newFlakes ?? Array.Empty<Sprite>();
            marineSnowMaterial = newMaterial;
        }

        internal void ConfigureCorridors(Vector2Int newLength, Vector2Int newWidth, PathWeights newPath)
        {
            corridorLength = newLength;
            corridorWidth = newWidth;
            pathWeights = newPath;
        }

        internal void ConfigureBands(RoomTheme[] newBands) => bandThemes = newBands ?? Array.Empty<RoomTheme>();

        internal void ConfigureShopDepth(Vector2 newDepth) => shopDepth = newDepth;

        internal void ConfigureRewards(MutationPool newPool, int[] newRewardAfter)
        {
            rewardPool = newPool;
            rewardAfter = newRewardAfter;
        }
    }
}
