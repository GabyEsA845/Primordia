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
        // Recuadro del Órgano en el HUD: aviso de que la habilidad está lista (sustituye a PlayerAbilityReady).
        public const string UIOrganReady = "UI_OrganReady";

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

        // Mutaciones del piso 2
        public const string TrilobiteBounce = "Skin_Trilobite_Bounce";
        public const string WiwaxiaVolley = "Skin_Wiwaxia_Volley";
        public const string CompoundEyesOn = "Organ_CompoundEyes_On";
        public const string CompoundEyesOff = "Organ_CompoundEyes_Off";
        public const string FlagellaLash = "Special_Flagella_Lash";
        public const string CuticleReflect = "Special_Cuticle_Reflect";
        public const string CuticleStun = "Special_Cuticle_Stun";
        public const string CarrionEat = "Lineage_Carrion_Eat";
        public const string CnidocytesFan = "Mouth_Cnidocytes_Fan";
        public const string ArmCrownWhirlLoop = "Mouth_ArmCrown_Whirl_Loop";
        public const string TailBubbles = "Limbs_Tail_Bubbles";
        public const string HallucigeniaPlace = "Limbs_Hallucigenia_Place";
        public const string HallucigeniaStrike = "Limbs_Hallucigenia_Strike";
        public const string HallucigeniaLaunch = "Limbs_Hallucigenia_Launch";
        public const string TrilobiteRoll = "Skin_Trilobite_Roll";
        public const string WiwaxiaOrbit = "Skin_Wiwaxia_Orbit";
        public const string BroodSacLarvaBite = "Organ_BroodSac_LarvaBite";
        public const string FlagellaReady = "Special_Flagella_Ready";
        public const string BudSpawn = "Special_Bud_Spawn";
        public const string BudDissolve = "Special_Bud_Dissolve";
        public const string UnstableCrack = "Special_Unstable_Crack";

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

        // Enemigos del piso 2
        public const string TrilobiteWarn = "Enemy_Trilobite_Warn";
        public const string TrilobiteCharge = "Enemy_Trilobite_Charge";
        public const string TrilobiteCrash = "Enemy_Trilobite_Crash";
        public const string OttoiaWarn = "Enemy_Ottoia_Warn";
        public const string OttoiaBite = "Enemy_Ottoia_Bite";
        public const string OttoiaSink = "Enemy_Ottoia_Sink";
        public const string ShieldBlock = "Enemy_Shield_Block";
        public const string PikaiaDash = "Enemy_Pikaia_Dash";
        public const string EnemyPitFall = "Enemy_PitFall";

        // Jefe: Anomalocaris
        public const string AnomalocarisBubble = "Boss_Anomalocaris_Bubble";
        public const string AnomalocarisWarn = "Boss_Anomalocaris_Warn";
        public const string AnomalocarisDash = "Boss_Anomalocaris_Dash";
        public const string AnomalocarisCircle = "Boss_Anomalocaris_Circle";
        public const string AnomalocarisLightsOut = "Boss_Anomalocaris_LightsOut";
        public const string AnomalocarisEyes = "Boss_Anomalocaris_Eyes";
        public const string AnomalocarisRoar = "Boss_Anomalocaris_Roar";
        public const string AnomalocarisCharge = "Boss_Anomalocaris_Charge";
        public const string AnomalocarisFrenzy = "Boss_Anomalocaris_Frenzy";
        public const string AnomalocarisLeap = "Boss_Anomalocaris_Leap";
        public const string AnomalocarisStun = "Boss_Anomalocaris_Stun";
        public const string AnomalocarisSilhouette = "Boss_Anomalocaris_Silhouette";

        // Jefe: Euriptérido
        public const string EurypteridStep = "Boss_Eurypterid_Step";
        public const string EurypteridPincerWarn = "Boss_Eurypterid_PincerWarn";
        public const string EurypteridPincer = "Boss_Eurypterid_Pincer";
        public const string EurypteridArmorBlock = "Boss_Eurypterid_ArmorBlock";
        public const string EurypteridSpineWarn = "Boss_Eurypterid_SpineWarn";
        public const string EurypteridSpineShot = "Boss_Eurypterid_SpineShot";
        public const string EurypteridStunned = "Boss_Eurypterid_Stunned";
        public const string EurypteridMolt = "Boss_Eurypterid_Molt";
        public const string EurypteridTailWarn = "Boss_Eurypterid_TailWarn";
        public const string EurypteridTailSweep = "Boss_Eurypterid_TailSweep";
        public const string EurypteridStagger = "Boss_Eurypterid_Stagger";
        public const string EurypteridDowned = "Boss_Eurypterid_Downed";
        public const string EurypteridRise = "Boss_Eurypterid_Rise";
        public const string EurypteridFuryShot = "Boss_Eurypterid_FuryShot";

        // Cueva del Euriptérido
        public const string CaveCreak = "Cave_Creak";
        public const string CaveQuake = "Cave_Quake";
        public const string CaveCollapse = "Cave_Collapse";
        public const string CaveRockFall = "Cave_RockFall";
        public const string CavePebble = "Cave_Pebble";
        public const string CaveFloorCrack = "Cave_FloorCrack";
        public const string CaveFloorSink = "Cave_FloorSink";
        public const string CinematicBars = "Cinematic_Bars";

        // Salas y recompensas
        public const string DoorsClose = "Room_DoorsClose";
        public const string DoorsOpen = "Room_DoorsOpen";
        public const string RoomCleared = "Room_Cleared";
        public const string RoomExit = "Room_Exit";
        public const string TransitionJump = "Transition_Jump";
        public const string TransitionFall = "Transition_Fall";
        public const string BiomassSmall = "Biomass_Pickup_Small";
        public const string BiomassBig = "Biomass_Pickup_Big";
        public const string BiomassMagnet = "Biomass_Magnet";
        public const string MutationChoiceOpen = "Mutation_ChoiceOpen";
        public const string MutationBubbleSpawn = "Mutation_Bubble_Spawn";
        public const string MutationBubblePickup = "Mutation_Bubble_Pickup";

        // Salas del piso 2
        public const string SlabsClose = "Room_Slabs_Close";
        public const string SlabsOpen = "Room_Slabs_Open";
        public const string ClamOpen = "Room_Clam_Open";
        public const string ClamOpenBig = "Room_Clam_Open_Big";
        public const string HeartCoral = "Room_HeartCoral";
        public const string ExchangeOpen = "Exchange_Capsule_Open";
        public const string ExchangeAccept = "Exchange_Capsule_Accept";
        public const string WaveStart = "Room_WaveStart";
        public const string WaveVent = "Room_WaveVent";
        public const string PurpleHeartBlock = "Player_PurpleHeart_Block";
        public const string PlayerPitFall = "Player_PitFall";

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
        public const string AmbFloor2 = "Amb_Floor2_Loop";
        public const string AmbFloor2Abyss = "Amb_Floor2_Abyss_Loop";
        public const string AmbFloor2BossRoom = "Amb_Floor2_BossRoom_Loop";
        // Pantano carbonífero: ambiente del piso 3 y lo que se oye junto a la charca de la sala de paso del piso 2.
        public const string AmbFloor3 = "Amb_Floor3_Loop";

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
        public const string MusicExchange = "Music_Exchange";
        public const string MusicFloor2ShallowsExplore = "Music_Floor2_Shallows_Explore";
        public const string MusicFloor2ShallowsCombat = "Music_Floor2_Shallows_Combat";
        public const string MusicFloor2ReefExplore = "Music_Floor2_Reef_Explore";
        public const string MusicFloor2ReefCombat = "Music_Floor2_Reef_Combat";
        public const string MusicFloor2AbyssExplore = "Music_Floor2_Abyss_Explore";
        public const string MusicFloor2AbyssCombat = "Music_Floor2_Abyss_Combat";
        public const string MusicBossAnomalocaris = "Music_Boss_Anomalocaris";
        public const string MusicBossAnomalocarisFrenzy = "Music_Boss_Anomalocaris_Frenzy";
        public const string MusicBossEurypterid = "Music_Boss_Eurypterid";
        public const string MusicBossEurypteridLastStand = "Music_Boss_Eurypterid_LastStand";
    }
}
