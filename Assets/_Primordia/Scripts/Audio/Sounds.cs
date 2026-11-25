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

        // Piso 3, tanda 1: reacciones y terreno
        public const string FireIgnite = "Fire_Ignite";
        public const string FireBurnLoop = "Fire_Burn_Loop";
        public const string FireExtinguish = "Fire_Extinguish";
        public const string StatusBurnStart = "Status_Burn_Start";
        public const string StatusBurnTick = "Status_Burn_Tick";
        public const string MethaneExplosion = "Methane_Explosion";
        public const string MethaneGasBagBurst = "Methane_GasBag_Burst";
        public const string MethaneGasBagRegrow = "Methane_GasBag_Regrow";
        public const string MethaneFogFlash = "Methane_FogFlash";
        public const string MethaneMistLoop = "Methane_Mist_Loop";
        public const string MethaneBubbleWarn = "Methane_Bubble_Warn";
        public const string MethaneBubblePop = "Methane_Bubble_Pop";
        public const string PoisonPuddle = "Poison_Puddle";
        public const string PoisonToxicCloud = "Poison_ToxicCloud";
        public const string PoisonBulbBurst = "Poison_Bulb_Burst";
        public const string PoisonBulbRegrow = "Poison_Bulb_Regrow";
        public const string FumaroleWarn = "Fumarole_Warn";
        public const string FumaroleErupt = "Fumarole_Erupt";
        public const string FernRustle = "Fern_Rustle";
        public const string FernBurn = "Fern_Burn";
        public const string BrushBurnAway = "Brush_BurnAway";
        public const string MudSinkLoop = "Mud_Sink_Loop";
        public const string MudEscape = "Mud_Escape";
        public const string MudDrown = "Mud_Drown";
        public const string MudSplash = "Mud_Splash";
        public const string WaterSplash = "Water_Splash";
        public const string WebStick = "Web_Stick";
        public const string WebBurn = "Web_Burn";

        // Piso 3, tanda 2: jugador (esquivas, Bocas y mutaciones)
        public const string MouthSpitShoot = "Mouth_Spit_Shoot";
        public const string MouthSpitTrail = "Mouth_Spit_Trail";
        public const string MouthSpitBurst = "Mouth_Spit_Burst";
        public const string MouthSpitSplit = "Mouth_Spit_Split";
        public const string MouthSpinStart = "Mouth_Spin_Start";
        public const string MouthSpinLoop = "Mouth_Spin_Loop";
        public const string MouthSpinHit = "Mouth_Spin_Hit";
        public const string MouthSpinDizzy = "Mouth_Spin_Dizzy";
        public const string MouthSpinDeflect = "Mouth_Spin_Deflect";
        public const string MouthSpinBoomerang = "Mouth_Spin_Boomerang";
        public const string MouthVinegarStart = "Mouth_Vinegar_Start";
        public const string MouthVinegarLoop = "Mouth_Vinegar_Loop";
        public const string MouthVinegarHit = "Mouth_Vinegar_Hit";
        public const string MouthVinegarEmpty = "Mouth_Vinegar_Empty";
        public const string MouthVinegarReady = "Mouth_Vinegar_Ready";
        public const string MouthVinegarSplash = "Mouth_Vinegar_Splash";
        public const string LimbsJumpTakeoff = "Limbs_Jump_Takeoff";
        public const string LimbsJumpLand = "Limbs_Jump_Land";
        public const string LimbsJumpStun = "Limbs_Jump_Stun";
        public const string LimbsSpinnerThread = "Limbs_Spinner_Thread";
        public const string LimbsSpinnerSnare = "Limbs_Spinner_Snare";
        public const string LimbsSpinnerWeb = "Limbs_Spinner_Web";
        public const string SkinMucusCharged = "Skin_Mucus_Charged";
        public const string SkinMucusAbsorb = "Skin_Mucus_Absorb";
        public const string SkinMucusReflect = "Skin_Mucus_Reflect";
        public const string SkinCharKindle = "Skin_Char_Kindle";
        public const string SkinCharFlare = "Skin_Char_Flare";
        public const string OrganMethaneRelease = "Organ_Methane_Release";
        public const string OrganMethaneCloudLoop = "Organ_Methane_Cloud_Loop";
        public const string OrganPheromoneCast = "Organ_Pheromone_Cast";
        public const string StatusConfused = "Status_Confused";
        public const string LineageTinderShoot = "Lineage_Tinder_Shoot";
        public const string LineageTinderTrail = "Lineage_Tinder_Trail";
        public const string LineageTinderBurst = "Lineage_Tinder_Burst";
        public const string LineageLeechBurst = "Lineage_Leech_Burst";
        public const string PlayerDodgeFrog = "Player_Dodge_Frog";
        public const string PlayerDodgeDiplocaulus = "Player_Dodge_Diplocaulus";
        public const string PlayerDodgeScorpion = "Player_Dodge_Scorpion";

        // Piso 3, tanda 3: especiales del ámbar, clases y linajes
        public const string RoomAmberBlockBreak = "Room_AmberBlock_Break";
        public const string SpecialCerciDash = "Special_Cerci_Dash";
        public const string SpecialCerciReady = "Special_Cerci_Ready";
        public const string SpecialCapsuleEncase = "Special_Capsule_Encase";
        public const string SpecialCapsuleHeal = "Special_Capsule_Heal";
        public const string SpecialCapsuleHit = "Special_Capsule_Hit";
        public const string SpecialCapsuleShatter = "Special_Capsule_Shatter";
        public const string SpecialDropShoot = "Special_Drop_Shoot";
        public const string SpecialDropEncase = "Special_Drop_Encase";
        public const string SpecialDropHit = "Special_Drop_Hit";
        public const string SpecialDropShatter = "Special_Drop_Shatter";
        public const string SpecialDropFossil = "Special_Drop_Fossil";
        public const string SpecialDropReady = "Special_Drop_Ready";
        public const string LineageParasiteAttach = "Lineage_Parasite_Attach";
        public const string LineageParasiteSuck = "Lineage_Parasite_Suck";
        public const string LineageParasiteDrain = "Lineage_Parasite_Drain";
        public const string LineageParasiteJump = "Lineage_Parasite_Jump";
        public const string LineageColonizeFloor3 = "Lineage_Colonize_Floor3";
        public const string LineageMushroomSprout = "Lineage_Mushroom_Sprout";
        public const string LineageEryopsWadeLoop = "Lineage_Eryops_Wade_Loop";
        public const string LineageEryopsEmerge = "Lineage_Eryops_Emerge";
        public const string LineageXenacanthusSpine = "Lineage_Xenacanthus_Spine";
        public const string LineageGasStingToxify = "Lineage_GasSting_Toxify";
        public const string LineageCentipedePuddle = "Lineage_Centipede_Puddle";
        public const string LineageZombieRise = "Lineage_Zombie_Rise";
        public const string LineageZombieCollapse = "Lineage_Zombie_Collapse";
        public const string LineageLampreyAttach = "Lineage_Lamprey_Attach";

        // Piso 3, tanda 4: enemigos y trampas (el golpe y la muerte van en los datos de cada enemigo)
        public const string EnemyCalamitesSprout = "Enemy_Calamites_Sprout";
        public const string EnemyCalamitesSporeRing = "Enemy_Calamites_SporeRing";
        public const string EnemyCalamitesSproutWither = "Enemy_CalamitesSprout_Wither";
        public const string EnemyMedullosaLaunch = "Enemy_Medullosa_Launch";
        public const string EnemyMedullosaFall = "Enemy_Medullosa_Fall";
        public const string EnemyMedullosaImpact = "Enemy_Medullosa_Impact";
        public const string EnemyDragonflyBuzzLoop = "Enemy_Dragonfly_Buzz_Loop";
        public const string EnemyDragonflyWarn = "Enemy_Dragonfly_Warn";
        public const string EnemyDragonflyDive = "Enemy_Dragonfly_Dive";
        public const string EnemyDragonflyLand = "Enemy_Dragonfly_Land";
        public const string EnemyBombardierWarn = "Enemy_Bombardier_Warn";
        public const string EnemyBombardierFlameStart = "Enemy_Bombardier_Flame_Start";
        public const string EnemyBombardierFlameLoop = "Enemy_Bombardier_Flame_Loop";
        public const string EnemyBombardierFlameEnd = "Enemy_Bombardier_Flame_End";
        public const string EnemySpiderFernShakeLoop = "Enemy_Spider_FernShake_Loop";
        public const string EnemySpiderEmerge = "Enemy_Spider_Emerge";
        public const string EnemySpiderSilk = "Enemy_Spider_Silk";
        public const string EnemySpiderLeap = "Enemy_Spider_Leap";
        public const string EnemyToadSpit = "Enemy_Toad_Spit";
        public const string EnemyToadBallSplat = "Enemy_Toad_BallSplat";
        public const string EnemyToadDive = "Enemy_Toad_Dive";
        public const string EnemyToadSurface = "Enemy_Toad_Surface";
        public const string EnemyToadSwell = "Enemy_Toad_Swell";
        public const string EnemyToadBurst = "Enemy_Toad_Burst";
        public const string TrapCarnivoreSnap = "Trap_Carnivore_Snap";
        public const string TrapCarnivoreChew = "Trap_Carnivore_Chew";
        public const string TrapCarnivoreRelease = "Trap_Carnivore_Release";
        public const string TrapCarnivoreEat = "Trap_Carnivore_Eat";
        public const string TrapCarnivoreOpen = "Trap_Carnivore_Open";
        public const string TrapCarnivoreWither = "Trap_Carnivore_Wither";
        public const string TrapMayflySwarmLoop = "Trap_Mayfly_Swarm_Loop";
        public const string TrapMayflySting = "Trap_Mayfly_Sting";
        public const string TrapMayflyBurn = "Trap_Mayfly_Burn";

        // Piso 3, tanda 5: salas y escenario
        public const string RoomRootsClose = "Room_Roots_Close";
        public const string RoomRootsOpen = "Room_Roots_Open";
        public const string RoomClearedFloor3 = "Room_Cleared_Floor3";
        public const string RoomWaveStartFloor3 = "Room_WaveStart_Floor3";
        public const string RoomConeChestOpen = "Room_ConeChest_Open";
        public const string RoomConeChestOpenBig = "Room_ConeChest_Open_Big";
        public const string RoomTeleportWake = "Room_Teleport_Wake";
        public const string RoomTeleportWarp = "Room_Teleport_Warp";
        public const string RoomHeartDrop = "Room_Heart_Drop";
        public const string RoomHeartPodSend = "Room_HeartPod_Send";
        public const string RoomHeartPodTake = "Room_HeartPod_Take";
        public const string RoomHeartPodEmpty = "Room_HeartPod_Empty";
        public const string RoachEmerge = "Roach_Emerge";
        public const string RoachGrab = "Roach_Grab";
        public const string RoachHide = "Roach_Hide";
        public const string RoachScurryLoop = "Roach_Scurry_Loop";
        public const string RoachHurt = "Roach_Hurt";
        public const string RoachDeath = "Roach_Death";
        public const string CritterScurry = "Critter_Scurry";
        public const string RoomRootWallHeartPlace = "Room_RootWall_HeartPlace";
        public const string RoomRootWallOpen = "Room_RootWall_Open";
        public const string RoomPurpleFlowerPickup = "Room_PurpleFlower_Pickup";
        public const string RoomPurpleSeedSprout = "Room_PurpleSeed_Sprout";
        public const string RoomLogStep = "Room_Log_Step";
        public const string RoomLogBubble = "Room_Log_Bubble";
        public const string RoomCoalIgnite = "Room_Coal_Ignite";
        public const string RoomCoalBurnLoop = "Room_Coal_Burn_Loop";
        public const string RoomFireFrontLoop = "Room_FireFront_Loop";
        public const string RoomStumpLightLoop = "Room_Stump_Light_Loop";

        // Piso 3, tanda 6: jefe Meganeura (el golpe va en los datos del jefe y de la ninfa)
        public const string BossMeganeuraIntro = "Boss_Meganeura_Intro";
        public const string BossMeganeuraWingsLoop = "Boss_Meganeura_Wings_Loop";
        public const string BossMeganeuraTakeOff = "Boss_Meganeura_TakeOff";
        public const string BossMeganeuraMark = "Boss_Meganeura_Mark";
        public const string BossMeganeuraMarkLock = "Boss_Meganeura_MarkLock";
        public const string BossMeganeuraDive = "Boss_Meganeura_Dive";
        public const string BossMeganeuraDiveImpact = "Boss_Meganeura_DiveImpact";
        public const string BossMeganeuraTrunkStuck = "Boss_Meganeura_TrunkStuck";
        public const string BossMeganeuraGust = "Boss_Meganeura_Gust";
        public const string BossMeganeuraFallBurning = "Boss_Meganeura_FallBurning";
        public const string BossMeganeuraCry = "Boss_Meganeura_Cry";
        public const string BossMeganeuraEggLay = "Boss_Meganeura_EggLay";
        public const string BossMeganeuraEggHit = "Boss_Meganeura_Egg_Hit";
        public const string BossMeganeuraEggBreak = "Boss_Meganeura_Egg_Break";
        public const string BossMeganeuraEggHatch = "Boss_Meganeura_Egg_Hatch";
        public const string BossMeganeuraNymphWarn = "Boss_Meganeura_Nymph_Warn";
        public const string BossMeganeuraNymphLash = "Boss_Meganeura_Nymph_Lash";
        public const string BossMeganeuraNymphPull = "Boss_Meganeura_Nymph_Pull";
        public const string BossMeganeuraScreech = "Boss_Meganeura_Screech";
        public const string BossMeganeuraLand = "Boss_Meganeura_Land";
        public const string BossMeganeuraStormEntry = "Boss_Meganeura_StormEntry";
        public const string BossMeganeuraThunder = "Boss_Meganeura_Thunder";
        public const string BossMeganeuraBigBoltWarn = "Boss_Meganeura_BigBolt_Warn";
        public const string BossMeganeuraLooseBoltWarn = "Boss_Meganeura_LooseBolt_Warn";
        public const string BossMeganeuraBoltStrike = "Boss_Meganeura_Bolt_Strike";
        public const string BossMeganeuraElectrocuted = "Boss_Meganeura_Electrocuted";
        public const string BossMeganeuraPuddleElectricLoop = "Boss_Meganeura_Puddle_Electric_Loop";
        public const string BossMeganeuraRingLoop = "Boss_Meganeura_Ring_Loop";
        public const string BossMeganeuraSigillariaBurn = "Boss_Meganeura_Sigillaria_Burn";
        public const string BossMeganeuraDeath = "Boss_Meganeura_Death";
        public const string BossMeganeuraStormFade = "Boss_Meganeura_StormFade";
        public const string AmbFloor3RainLoop = "Amb_Floor3_Rain_Loop";

        // Piso 3, tanda 7: jefe Arthropleura
        public const string BossArthropleuraIntro = "Boss_Arthropleura_Intro";
        public const string BossArthropleuraRustleLoop = "Boss_Arthropleura_Rustle_Loop";
        public const string BossArthropleuraLegsLoop = "Boss_Arthropleura_Legs_Loop";
        public const string BossArthropleuraChargeWarn = "Boss_Arthropleura_Charge_Warn";
        public const string BossArthropleuraCharge = "Boss_Arthropleura_Charge";
        public const string BossArthropleuraEdgeBreak = "Boss_Arthropleura_EdgeBreak";
        public const string BossArthropleuraShellHit = "Boss_Arthropleura_Shell_Hit";
        public const string BossArthropleuraPlateOpen = "Boss_Arthropleura_Plate_Open";
        public const string BossArthropleuraPlateClose = "Boss_Arthropleura_Plate_Close";
        public const string BossArthropleuraHurt = "Boss_Arthropleura_Hurt";
        public const string BossArthropleuraIgnite = "Boss_Arthropleura_Ignite";
        public const string BossArthropleuraFuseLoop = "Boss_Arthropleura_Fuse_Loop";
        public const string BossArthropleuraHiss = "Boss_Arthropleura_Hiss";
        public const string BossArthropleuraWallCollapse = "Boss_Arthropleura_WallCollapse";
        public const string BossArthropleuraStompWarn = "Boss_Arthropleura_Stomp_Warn";
        public const string BossArthropleuraStompWave = "Boss_Arthropleura_Stomp_Wave";
        public const string BossArthropleuraTreeFall = "Boss_Arthropleura_TreeFall";
        public const string BossArthropleuraStunned = "Boss_Arthropleura_Stunned";
        public const string BossArthropleuraEatFern = "Boss_Arthropleura_EatFern";
        public const string BossArthropleuraDouse = "Boss_Arthropleura_Douse";
        public const string BossArthropleuraRingClose = "Boss_Arthropleura_Ring_Close";
        public const string BossArthropleuraSqueeze = "Boss_Arthropleura_Squeeze";
        public const string BossArthropleuraExpel = "Boss_Arthropleura_Expel";
        public const string BossArthropleuraCoil = "Boss_Arthropleura_Coil";
        public const string BossArthropleuraRollLoop = "Boss_Arthropleura_Roll_Loop";
        public const string BossArthropleuraRollCrash = "Boss_Arthropleura_Roll_Crash";
        public const string BossArthropleuraDeath = "Boss_Arthropleura_Death";
        public const string BossArthropleuraSmolderLoop = "Boss_Arthropleura_Smolder_Loop";

        // Piso 3, tanda 8: minijefe Megarachne (el golpe va en los datos del jefe)
        public const string BossMegarachneIntro = "Boss_Megarachne_Intro";
        public const string BossMegarachneCableRunLoop = "Boss_Megarachne_CableRun_Loop";
        public const string BossMegarachneWalkLoop = "Boss_Megarachne_Walk_Loop";
        public const string BossMegarachneClimb = "Boss_Megarachne_Climb";
        public const string BossMegarachneDrop = "Boss_Megarachne_Drop";
        public const string BossMegarachneJumpWarn = "Boss_Megarachne_Jump_Warn";
        public const string BossMegarachneJumpLand = "Boss_Megarachne_Jump_Land";
        public const string BossMegarachneSilkVolley = "Boss_Megarachne_SilkVolley";
        public const string BossMegarachneSilkSplat = "Boss_Megarachne_Silk_Splat";
        public const string BossMegarachneNetWarn = "Boss_Megarachne_Net_Warn";
        public const string BossMegarachneNetDrop = "Boss_Megarachne_Net_Drop";
        public const string BossMegarachneNetTrap = "Boss_Megarachne_Net_Trap";
        public const string BossMegarachneSacWeave = "Boss_Megarachne_Sac_Weave";
        public const string BossMegarachneSacTremble = "Boss_Megarachne_Sac_Tremble";
        public const string BossMegarachneSacHit = "Boss_Megarachne_Sac_Hit";
        public const string BossMegarachneSacBreak = "Boss_Megarachne_Sac_Break";
        public const string BossMegarachneSacBurst = "Boss_Megarachne_Sac_Burst";
        public const string BossMegarachneBroodLoop = "Boss_Megarachne_Brood_Loop";
        public const string BossMegarachneBroodBite = "Boss_Megarachne_Brood_Bite";
        public const string BossMegarachneBroodDeath = "Boss_Megarachne_Brood_Death";
        public const string BossMegarachneCableWeave = "Boss_Megarachne_Cable_Weave";
        public const string BossMegarachneCableIgnite = "Boss_Megarachne_Cable_Ignite";
        public const string BossMegarachneCableFuseLoop = "Boss_Megarachne_Cable_Fuse_Loop";
        public const string BossMegarachneCableSnap = "Boss_Megarachne_Cable_Snap";
        public const string BossMegarachneFallStunned = "Boss_Megarachne_Fall_Stunned";
        public const string BossMegarachneRageScreech = "Boss_Megarachne_Rage_Screech";
        public const string BossMegarachneNestWither = "Boss_Megarachne_Nest_Wither";
        public const string BossMegarachneNestBurn = "Boss_Megarachne_Nest_Burn";
        public const string BossMegarachneWebCollapse = "Boss_Megarachne_Web_Collapse";
        public const string BossMegarachneDeath = "Boss_Megarachne_Death";
        public const string BossMegarachneCablesDrop = "Boss_Megarachne_Cables_Drop";
        public const string BossMegarachneCocoonFall = "Boss_Megarachne_Cocoon_Fall";
        public const string BossMegarachneCocoonOpen = "Boss_Megarachne_Cocoon_Open";
    }
}
