namespace Primordia.Audio
{
    /// <summary>
    /// Nombres de los sonidos que el código pide directamente (los de los datos, como el disparo de cada ataque o
    /// la muerte de cada enemigo, los pone el menú Primordia > Audio > Importar audio en sus assets). Son los
    /// nombres de los archivos de docs/multimedia-temp/audio sin SFX_ ni el número de variante.
    /// </summary>
    public static class Sounds
    {
        // Jugador
        public const string PlayerDodge = "Player_Dodge";
        public const string PlayerDodgeReady = "Player_DodgeReady";
        public const string PlayerHurt = "Player_Hurt";
        public const string PlayerDeath = "Player_Death";
        public const string PlayerHeal = "Player_Heal";
        public const string PlayerLowHealthLoop = "Player_LowHealth_Loop";
        public const string PlayerAbilityReady = "Player_AbilityReady";
        public const string PlayerAbilityNotReady = "Player_AbilityNotReady";

        // Mutaciones
        public const string LegsPush = "Limbs_Legs_Push";
        public const string HyphaeTrail = "Limbs_Hyphae_Trail";
        public const string Thorns = "Skin_Thorns";
        public const string Quills = "Skin_Quills";
        public const string CamoOn = "Skin_Camo_On";
        public const string CamoOff = "Skin_Camo_Off";
        public const string CamoStrike = "Skin_Camo_Strike";
        public const string Ink = "Organ_Ink";
        public const string Shockwave = "Organ_Shockwave";
        public const string ShockwaveProjectileBreak = "Organ_Shockwave_ProjectileBreak";
        public const string Vacuole = "Organ_Vacuole";
        public const string VacuoleCharge = "Organ_Vacuole_Charge";
        public const string MutationGain = "Mutation_Gain";

        // Linajes
        public const string LineageComplete = "Lineage_Complete";
        public const string LineageFinisher = "Lineage_Finisher";
        public const string LineageBarrier = "Lineage_Barrier";
        public const string LineageBarrierBlock = "Lineage_BarrierBlock";
        public const string LineageColonize = "Lineage_Colonize";
        public const string LineageRetract = "Lineage_Retract";
        public const string LineageStingingTrail = "Lineage_StingingTrail";
        public const string LineagePuffOnHit = "Lineage_PuffOnHit";

        // Estados
        public const string Discovered = "Status_Discovered";
        public const string PoisonBurst = "Status_PoisonBurst";
        public const string SporeCloud = "Status_SporeCloud";

        // Jefe: virus gigante
        public const string VirusIntro = "Boss_Virus_Intro";
        public const string VirusBurstWindup = "Boss_Virus_BurstWindup";
        public const string VirusSummonWindup = "Boss_Virus_SummonWindup";
        public const string VirusSummon = "Boss_Virus_Summon";
        public const string VirusPhase2 = "Boss_Virus_Phase2";

        // Jefe: ameba gigante
        public const string AmoebaIntro = "Boss_Amoeba_Intro";
        public const string AmoebaWarning = "Boss_Amoeba_Warning";
        public const string AmoebaLaunch = "Boss_Amoeba_Launch";
        public const string AmoebaWallSlam = "Boss_Amoeba_WallSlam";
        public const string AmoebaEnrage = "Boss_Amoeba_Enrage";
        public const string AmoebaSplit = "Boss_Amoeba_Split";
        public const string AmoebaBounce = "Boss_Amoeba_Bounce";
        public const string AmoebaMerge = "Boss_Amoeba_Merge";
        public const string AmoebaFury = "Boss_Amoeba_Fury";
        public const string AmoebaImmune = "Boss_Amoeba_Immune";
        public const string AmoebaExhaustedLoop = "Boss_Amoeba_Exhausted_Loop";
        public const string CinematicBars = "Cinematic_Bars";

        // Salas y recompensas
        public const string DoorsClose = "Room_DoorsClose";
        public const string DoorsOpen = "Room_DoorsOpen";
        public const string RoomCleared = "Room_Cleared";
        public const string RoomExit = "Room_Exit";
        public const string BiomassSmall = "Biomass_Pickup_Small";
        public const string BiomassBig = "Biomass_Pickup_Big";
        public const string BiomassMagnet = "Biomass_Magnet";
        public const string MutationChoiceOpen = "Mutation_ChoiceOpen";
        public const string MutationBubbleSpawn = "Mutation_Bubble_Spawn";
        public const string MutationBubblePickup = "Mutation_Bubble_Pickup";

        // Tienda
        public const string ShopApproach = "Shop_Approach";
        public const string ShopCantAfford = "Shop_CantAfford";
        public const string ShopDarwinCheer = "Shop_Darwin_Cheer";
        public const string ShopHeartContainer = "Shop_HeartContainer";

        // Interfaz
        public const string UIHover = "UI_Hover";
        public const string UIClick = "UI_Click";
        public const string UIBack = "UI_Back";
        public const string UICardSelect = "UI_CardSelect";
        public const string UIStartGame = "UI_StartGame";
        public const string UIPauseOpen = "UI_PauseOpen";
        public const string UIPauseClose = "UI_PauseClose";
        public const string UIMapOpen = "UI_MapOpen";
        public const string UIMapClose = "UI_MapClose";

        // Ambiente
        public const string AmbFloor1 = "Amb_Floor1_Loop";
        public const string AmbBossRoom = "Amb_BossRoom_Loop";

        // Música
        public const string MusicMainMenu = "Music_MainMenu";
        public const string MusicFloor1Explore = "Music_Floor1_Explore";
        public const string MusicFloor1Combat = "Music_Floor1_Combat";
        public const string MusicShop = "Music_Shop";
        public const string MusicBossVirus = "Music_Boss_Virus";
        public const string MusicBossAmoeba = "Music_Boss_Amoeba";
        public const string MusicBossAmoebaFinal = "Music_Boss_Amoeba_Final";
        public const string StingLineageEvolution = "Music_Sting_LineageEvolution";
        public const string StingBossIntro = "Music_Sting_BossIntro";
        public const string StingVictory = "Music_Sting_Victory";
        public const string StingDefeat = "Music_Sting_Defeat";
    }
}
