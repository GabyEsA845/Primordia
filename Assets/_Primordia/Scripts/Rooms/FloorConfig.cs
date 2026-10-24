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

            public EnemyEntry(Enemy prefab, int cost, int minDepth = 1)
            {
                this.prefab = prefab;
                this.cost = cost;
                this.minDepth = minDepth;
            }
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

        [Header("Tileset")]
        [SerializeField] RoomTheme theme;
        [SerializeField] RoomTheme bossTheme;

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

        public RoomTemplate StartRoom => startRoom;
        public RoomTemplate BossRoom => bossRoom;
        public RoomTemplate[] CombatRooms => combatRooms;
        public RoomTemplate[] SideRooms => sideRooms;
        public Vector2Int MainCombatRooms => mainCombatRooms;
        public Vector2Int SideRoomCount => sideRoomCount;
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

        internal void ConfigureRewards(MutationPool newPool, int[] newRewardAfter)
        {
            rewardPool = newPool;
            rewardAfter = newRewardAfter;
        }
    }
}
