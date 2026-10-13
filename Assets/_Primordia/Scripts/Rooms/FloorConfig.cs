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
        public MutationPool RewardPool => rewardPool;
        public int RewardOptions => rewardOptions;
        public int[] RewardAfter => rewardAfter;
        public BiomassPickup SmallBiomass => smallBiomass;
        public BiomassPickup BigBiomass => bigBiomass;
        public int MinimumBiomassPerRoom => minimumBiomassPerRoom;
        public RoomTemplate ShopTemplate => shopRoom;
        public ShopRoom ShopPrefab => shopPrefab;
        public bool HasShop => shopRoom != null && shopPrefab != null;
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

        internal void ConfigureRewards(MutationPool newPool, int[] newRewardAfter)
        {
            rewardPool = newPool;
            rewardAfter = newRewardAfter;
        }
    }
}
