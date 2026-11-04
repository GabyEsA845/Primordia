---
name: primordia-floor3-enemies-decisions
description: "Paso 5 del piso 3 (enemigos y trampas): 6 enemigos + 2 trampas HECHOS y probados (2026-11-03); comportamiento, números, archivos, cambios al generador y qué falta (arte con Opus, documento, cierre)"
metadata:
  type: project
---

**Estado (2026-11-03):** los 6 enemigos y las 2 trampas del piso 3 están programados, probados por el usuario y con commit en `feature/floor-3` (b28b82b1 Escarabajo, a59a1292 Sapo, 4f296e8b Medullosa, 9dd63e1e Calamites, 5350e7a9 Araña, 332465b3 Libélula, 2953476a trampas). **Falta cerrar el paso 5:** la tanda de arte pendiente (Opus, ver [[primordia-floor3-enemy-art-pending]]; el usuario avisará para integrarla), el documento interactivo del piso 3 (enemigos nuevos) y el GDD/PLAN. Después: paso 6 (salas), 7 (jefes, uno cada vez), 8 (audio y cierre).

**Menú:** `Primordia > Floor 3 > Crear enemigos del piso 3` (`Editor/ArtSetup.Floor3Enemies.cs`): importa el arte de `docs/multimedia-temp/floor3-enemies`, crea datos (`Data/Enemies/Floor3`), prefabs, efectos y trampas, y rellena Floor_3 (entradas, trampas, presupuesto 8 + 1,6 por sala solo si seguía en 3/1,4, y **corrige la reserva de recompensas a `MutationPool_Floor3`**: Floor_3 usaba la del piso 2). Reusa `BuildFloor2Enemy`, `Floor2MouthsSetup.CreateFrameEffect/CreateLoopEffect`. Tests en `Floor3EnemiesTests`, `Floor3TrapsTests` y `FloorGenerationTests`.

**Enemigos (todos con `EnemyBrain`, en `Scripts/Enemies`):**
- **Escarabajo** (`BeetleBrain`, 12 vida, coste 3, sala 1): se acerca, se gira (Turn), avisa 0,6 s (la dirección del cono se fija 0,25 s antes), cono de 3 u 1,2 s por detrás; quema al jugador (barra `PlayerBurn.Feed`) y a enemigos (`Burn`), prende inflamables a 1,3 u y deja `GroundFire` de 2 s cada 1 u; inmune al fuego; +50 % de daño por la espalda (modificador de `Health`).
- **Sapo** (`ToadBrain` + `ToadMudBall`, 10 vida, coste 3, máx. 3, `needsMud`): bola de lodo en arco (charco `Puddle_Sludge_Splash` temporal 8 s, 1 de daño); se hincha y revienta si te acercas a ≤1,8 u o con ≤30 % de vida (corre hacia ti); busca el lodo profundo a ≤8 u y ataca desde ahí; en lodo, al ser golpeado se zambulle 2 s (inmune, sin colisión). `Toad_Emerge` = `Toad_Dive` al revés (generado a mano).
- **Medullosa** (`MedullosaBrain` + `MedullosaSeed`, 16 vida, coste 4, sala 1, máx. 2, `dryGround`): alcance 15 u, marca fija en el suelo, vaina en arco 1,1 s, daño 1 en radio 0,9 y charco de veneno (`PoisonPuddle.SpawnSplat`) 6 s; cadencia 4 s.
- **Calamites** (`CalamitesBrain` + `CalamitesSproutBrain`, madre 30 vida, coste 5, sala 3, máx. 1, `dryGround`): retoño cada 4 s (máx. 4 vivos, sin biomasa, registrados como hijos de la madre) y anillo de 10 esporas cada 7 s que gira 18°; al morir la madre, los retoños se secan.
- **Araña** (`SpiderBrain` + `SpiderSilk` + `SpiderWeb`, 12 vida, coste 3, sala 1, máx. 3, `hidesInFern`): nace dentro de un helecho entero (`Fern` implementa `IHidingPlace`: tiembla mientras hay araña), sale a ≤3,2 u, si el helecho arde o a los 10 s; seda en arco que deja telaraña (frena 50 %, arde y deja fuego), salta para apartarse (<2,5 u) o acercarse (>7 u).
- **Libélula** (`DragonflyBrain`, 14 vida, coste 4, sala 3, máx. 2): vuela inmune (`rb.excludeLayers` = jugador y sus proyectiles; `SwimmerLook.Grounded` la baja al posarse), da vueltas a 5,5 u, línea de aviso 0,7 s (se fija 0,3 s antes), picado 14 u/s con 2 de daño, se posa 0,8 s (vulnerable) y despega (`Dragonfly_Takeoff` = `Dragonfly_Land` al revés). `EnemyBrain.CanBePushed`: la Peonza ya no empuja ni aturde a quien es inmune (el aire).

**Trampas (no son `Enemy`; no cuentan para limpiar; cuelgan de `enemiesRoot`, inactivas hasta entrar):** `CarnivorousPlant` (atrapa 1 s con `PlayerController.MovementLocked`, daño 1 al soltar; se come enemigos que no sean jefes ni inmunes; se marchita al limpiar; sala 2, 35 %, hasta 2, `dryGround`) y `MayflySwarm` (flota hacia el jugador, sin cuerpo, pica 1 por segundo, solo muere con fuego, se disipa al limpiar; sala 3, 30 %).

**Cambios al generador:** `EnemyEntry` ganó `needsMud` (solo en salas con lodo y a 2–3 tiles de él, `EncounterPlanner.MudSideCells`), `dryGround` (plantas: no sobre charcos del escenario, `puddleSpots` de `BuildScenery`) y `hidesInFern` (sobre helechos de la sala, `fernSpots`); `EncounterPlanner.Place` reparte todo; `FloorConfig.TrapEntry` + `PickTraps` + `FloorBuilder.SpawnTraps`.

**Why / decisiones del usuario:** más enemigos desde la primera sala (cuevas de 34×22 a 44×26 daban 1 enemigo); las plantas solo sobre tierra por ambientación; el sapo solo en salas con lodo y cerca de él; el cono del escarabajo debe prender el terreno.

**How to apply:** tras la tanda de arte, ejecutar `Primordia > Floor 3 > Importar todo el arte del piso 3` y `Crear enemigos del piso 3`; el código no cambia salvo posibles variantes en lodo del sapo (estados nuevos + `InMud` en `AnimationState`). Pendientes numéricos a ajustar si hace falta: tinte real y muerte al revés del Zombi, números provisionales de linajes (ver [[primordia-floor3-mutations-decisions]]). Ver [[primordia-floor3-structure-decisions]], [[primordia-floor2-enemies]].
