# Tarea 05 — Lepidodendron ancestral, parte 1: sala, escenario y escena de prueba (L1)

- **Rama:** `codex/lepido-1-sala` (desde `feature/floor-3`)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** pendiente (SIN dar a Codex)
- **Plan completo del jefe:** `docs/codex/lepidodendron-plan.md` (esta es la primera de 5 tareas; léelo para saber qué viene después, pero **haz solo esta**).

## Objetivo
Dejar **la sala del jefe secreto vista y recorrible, sin combate**: el pasillo largo con sus 3 paredes de raíces (sólidas, aún sin vida), la arena final con el árbol al fondo, la capa de hojas fija en pantalla, los decorados, la luz y la sombra, más una **escena de prueba** `Test_BossLepidodendron` desde la que la usuaria lo ve y recorre. El arte está aprobado y ya importado en `Art/Floor3/floor3-bosses/lepidodendron/`; solo hay que **montarlo y programarlo**. Es el mismo método que usamos con la Meganeura (7a): menú `Primordia > Boss > Crear Lepidodendron y escena de prueba` que lo crea todo, y una escena de prueba para verlo.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`: **fuente de verdad del arte**. Para esta tarea: «Combate (resumen)», «Tronco, pie y vetas», «Capa de hojas, luz y sombra», «Decorados del pie», «Paredes de raíces» (tablas de hojas, celdas, pivotes, ms, orden de dibujo). Las secciones Conos, Ataques, Desplome, Recompensa y Barra son de tareas posteriores: **no las uses**.
- Código a imitar (mismo patrón): `Editor/MeganeuraBossSetup.cs` (`CreateAll`, `CreateRoom`, `CreatePrefab`, `CreateTestConfig`, `CreateTestScene`), `Editor/ArtSetup.BossMeganeura.cs` (importar hojas, cortar con `SliceFloor3Sheet`), `Scripts/Enemies/MeganeuraBoss.cs` y `MeganeuraBoss.Scenery.cs` (`SetArena`, `BuildScenery`), `Scripts/Enemies/IBoss.cs`, `Scripts/Rooms/RoomLayouts.cs`, `Scripts/Core/DebugCheats.cs` (teclas de prueba).
- Memoria: `docs/memoria-claude/primordia-boss-lepidodendron.md` (solo la parte «TERMINADO», arriba).

## Qué hacer
1. **Importar el arte** — nuevo `Editor/ArtSetup.BossLepidodendron.cs` (partial de `ArtSetup`) con `ImportLepidodendronSheets()` y menú `Primordia > Boss > Importar hojas del Lepidodendron`: corta en cuadros (celda de la tabla del README, pivote de la tabla) las hojas de `Tree/`, `Canopy/`, `Decor/` y `Walls/` y las reúne en un asset de datos (`Data/Bosses/LepidodendronArt.asset`, como `MeganeuraArt.asset`; reutiliza `DirectionalArt` solo si encaja, si no crea un `ScriptableObject` simple con `Sprite[]` por nombre). Primero llama a `ImportFloor3Art()` como hace la Meganeura. Las hojas de `Cones/`, `Attacks/`, `Death/`, `Reward/` y `BossBar/` **no** se cortan todavía.
2. **Sala** — plantilla `Room_Boss_Lepidodendron` (en `Data/Rooms/Floor3/`): pasillo de **116×18 u** seguido de la **arena de 40×26 u**, en una sola sala horizontal (el pasillo desemboca en la arena por su lado este; el jugador entra por el oeste). Mira `RoomLayouts`: si no hay una forma que sirva, construye el trazado con `RoomLayouts.Rectangle` + el recorte necesario, o dímelo en `Dudas` en vez de inventar una API grande. Tileset del suelo: el de la **Tierra quemada** (como la Meganeura: `floor3.BandThemes[2]`).
3. **Clase del jefe** — `Scripts/Enemies/LepidodendronBoss.cs` (+ parciales `LepidodendronBoss.Scenery.cs` si crece) que implementa `IBoss` como lo hace `MeganeuraBoss` (nombre «Lepidodendron ancestral», vida provisional **1000** como constante marcada provisional, `BarStyle = Normal`, `SetArena`, etc.). **Sin combate**: no ataca, es invulnerable (`Health.IsInvulnerable = true`) y no tiene IA. `SetArena` construye todo el escenario de abajo. No hace falta presentación en esta tarea (sí en la L5).
4. **Escenario** (todo hijo de un objeto «Scenery» del jefe; orden de dibujo del README: BackRoots → MoundShadow → Mound → sombra moteada → Trunk → Veins → jugador y enemigos → rayos de luz → capa de hojas):
   - **Árbol en la arena:** el pie del tronco está a **10,5 u de la pared norte de la arena** (así está compuesto el arte; es ~2,5 u al norte del centro de una arena de 26 u). Piezas con pivote en el pie: `Lepido_Mound` + `Lepido_MoundShadow` (suelo), `Lepido_BackRoots` (suelo, **su colisión cierra el paso por detrás del árbol**), `Lepido_Trunk` y `Lepido_Veins_Rest` (detrás del jugador siempre). Colisión del tronco/pie: un `CircleCollider2D` o polígono bajo el pie que impida meterse dentro; **no se pasa por detrás** (las raíces de atrás se meten en la pared norte).
   - **Decorados del pie** (sin colisión): hongos en repisa, setas moradas (`Idle`, bucle), florecillas, lianas pegadas al tronco, charco oscuro, restos casi enterrados y bichos (cucaracha que huye si el jugador se acerca, milpiés, libélula posada que vuela al acercarse, colémbolos). Coloca cada uno según `DECOR_LAYOUT` de `docs/multimedia-temp/floor3-bosses/lepidodendron/generar_pie_lepidodendron.py` (px respecto al pie, /16 para unidades). Los bichos pueden ser simples: reposo + huida.
   - **Capa de hojas:** `Canopy_Arena_Sway` (bucle 150 ms) fija **arriba de la pantalla en vertical y con el mundo en horizontal**, 10 px por encima del borde, centrada en la arena, encima de todo (orden alto, por debajo de la interfaz). Pasillo: `Canopy_StripThin_Sway` desde la pared 2 y `Canopy_StripMid_Sway` desde la pared 3, en mosaico horizontal. 2 rayos de luz (`Fx_LightShaft`) en los huecos de la capa de la arena (x = 176 y 512 px de la pieza, y = 40) cada uno con su `Fx_LightPool` en el suelo; rayos por encima del jugador. `Canopy_Arena_Shake` queda importado y **expuesto con un método** `ShakeCanopy(float seconds)` (la usarán las tareas L3/L4), sin llamarlo todavía salvo desde la tecla de prueba.
   - **Hojas y esporas que caen** (`Fx_FallingLeaf`, `Fx_FallingSpore`): adorno; pocas en reposo, ~70 hojas en `ShakeCanopy`.
   - **Sombra moteada** `Fx_Dapple` en mosaico por todo el suelo desde la pared 1; su opacidad sube de **0** (pared 1) a **1** (arena) linealmente con la x.
   - **Paredes de raíces:** 3 paredes en x = **34 / 64 / 94** u desde la entrada (oeste), de **3 / 3,5 / 4 u** de grosor, cruzando los 18 u del pasillo; usa `Lepido_Wall{n}_D0` y su pivote (centro del borde sur). Con **colisión sólida** que cierra el pasillo. Los 4 nudos/estados, daño, fuego y retirada son de la L2: aquí solo la pared entera, quieta, y los nudos con su `Idle` latiendo (capa encima, en la posición del recorte del README).
5. **Cámara:** en el pasillo la normal; en la arena **alejada ×0,75** (se ven 40×22,5 u). Mira cómo lo hacen los otros jefes con `CameraRig` (zoom/límites) y haz la transición suave al cruzar a la arena. Si el límite de cámara de la sala no admite dos zooms, deja el zoom de la arena y avísalo.
6. **Menú y escena de prueba** — `Editor/LepidodendronBossSetup.cs`, menú `Primordia > Boss > Crear Lepidodendron y escena de prueba` (igual que `MeganeuraBossSetup.CreateAll`): importa hojas, crea la plantilla, el `EnemyData` y el prefab `Boss_Lepidodendron` (rehecho cada vez conservando el GUID), un `FloorConfig` de prueba `Floor_BossLepidodendronTest.asset` **clonado del piso 3 con ese jefe como único jefe** (no toques el `Floor3.asset` real: el jefe secreto se engancha al piso en una tarea posterior) y la escena `Test_BossLepidodendron.unity` con el mismo `BuildFloor`/`AddTestLoadout`/`AddHUD` que `Test_BossMeganeura`. Se debe poder abrir la escena, entrar a la sala y verlo todo.
7. **Teclas de prueba** (solo en el editor / `DebugCheats`): una tecla para **teletransportar al jugador** a delante de la pared 1, a delante de la pared 2, a delante de la pared 3 y a la arena (como las paredes son sólidas y aún no se rompen, sin esto no se podría ver la arena); otra para `ShakeCanopy(1f)`. Indícalas en el comentario de `DebugCheats` y en `Resultado`.
8. **Pruebas** en `Tests/PlayMode/LepidodendronBossTests.cs`: (a) el menú deja el prefab y la sala sin referencias perdidas; (b) `SetArena` crea el escenario (tronco, 3 paredes con colisión sólida en x=34/64/94, capa de hojas, 2 rayos de luz); (c) las paredes bloquean el paso del jugador en el pasillo; (d) el tronco no se puede atravesar y las raíces de atrás cierran el paso por detrás; (e) la opacidad de la sombra moteada es 0 en la pared 1 y 1 en la arena; (f) `ShakeCanopy` cambia a `Shake` y vuelve a `Sway`; (g) el jefe es invulnerable y no ataca.

## Números y reglas (cerrados)
- Sala: pasillo 116×18 u + arena 40×26 u. Paredes x = 34/64/94, grosores 3/3,5/4 u. Cámara de la arena ×0,75.
- Pie del árbol: **10,5 u desde la pared norte de la arena** (valor del arte; manda sobre cualquier «2 u al norte del centro»). Todo el resto (celdas, pivotes, ms, alturas, capas) está en el README del arte: **respétalo tal cual**.
- Vida del jefe: 1000, **provisional** (la L2 la reparte: 25 % por pared). Sin daño, sin ataques.
- Constantes (posiciones de las paredes, grosores, orden de dibujo, número de hojas, zoom) **al inicio del archivo**, con nombre, y las provisionales marcadas.

## Archivos que puede tocar
- Nuevos: `Editor/ArtSetup.BossLepidodendron.cs`, `Editor/LepidodendronBossSetup.cs`, `Scripts/Enemies/LepidodendronBoss*.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Pequeños y justificados: `Scripts/Core/DebugCheats.cs` (teclas de prueba), `Scripts/Rooms/RoomLayouts.cs` (si hace falta una forma), el código de cámara solo si no hay otra forma de hacer el zoom.
- Se generan: `Data/Bosses/LepidodendronArt.asset`, `Data/Rooms/Floor3/Room_Boss_Lepidodendron.asset`, `Data/Enemies/Floor3/Enemy_LepidodendronBoss.asset`, `Prefabs/Enemies/Boss_Lepidodendron.prefab`, `Data/Rooms/Floor_BossLepidodendronTest.asset`, `Scenes/Test_BossLepidodendron.unity`.

## Fuera de alcance (NO hacer)
- No dibujar ni modificar PNG, no ejecutar los `.py`. Si falta una hoja, **para y avisa**.
- Nada de combate, daño a las paredes, fuego, nudos que revientan, oleadas, conos, ataques, desplome, recompensa, barra de vida propia ni presentación (tareas L2-L5).
- No enganchar el jefe al `Floor3.asset` real ni a la puerta secreta; no tocar la Meganeura ni otros jefes.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba`; después `LepidodendronBossTests` y, para no romper vecinos, `MeganeuraBossTests` y `BossHealthBarTests`. Fallos que ya existían: ver `AGENTS.md`.
- A ojo (usuaria), escena `Test_BossLepidodendron`: caminar el pasillo (sombra que se espesa, tiras de hojas desde la pared 2), ver las 3 paredes quietas y sólidas, teletransportarse a la arena y ver el árbol con su capa de hojas, rayos de luz, decorados y bichos; sacudir la copa con la tecla de prueba.

## Resultado (lo rellena Codex)
- Hecho:
- Archivos tocados:
- Pruebas (cuántas pasan):
- Teclas de prueba:
- Dudas / no comprobado:
