# Tarea 10 — Lepidodendron ancestral, parte 5b: recompensa, sala del tocón y reliquia (L5b)

- **Rama:** `codex/lepido-5b-recompensa` (desde `codex/lepido-5a-desplome`)
- **Escrita por:** Claude (2026-11-13) · **Implementa:** Codex
- **Estado:** pendiente
- **Plan completo:** `docs/codex/lepidodendron-plan.md`. Haz **solo esta** tarea.

## Objetivo
Tras el desplome (tarea 09), el jugador recibe la recompensa del jefe secreto: **+1 corazón permanente**, la **Espora morada** (objeto clave) bajando por la columna de luz, y, entrando por la grieta del tocón con **E**, una **sala interior** con la **reliquia Raíz ancestral** en un pedestal. Hay que crear el **sistema mínimo de reliquias** y de objetos clave. Arte aprobado: solo importar y programar.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`, sección **«Recompensa»** (hojas, celdas, pivotes, posiciones, sala 480×270 con suelo circular r 92 en (240, 158), setas, salida E en (240, 252), presentación común de reliquias/objetos clave con todas sus medidas y textos).
- `Scripts/Rooms/SecretReward.cs` (el corazón: `health.SetMax(health.Max + 2)`, sonido `Sounds.HeartCoral`; imita `PromptInteractable`), `Scripts/Rooms/Interactable` / `Core/Interactable.cs`, `Scripts/Mutations/MutationDrop.cs` (recompensas de otros jefes), `Scripts/UI/CinematicOverlay.cs`, `Core/GameManager.cs` (estado de la partida y cambio de piso), el menú/panel de pausa donde se ven las mutaciones (`CreaturePanel` y vecinos en `Scripts/UI/`), y `Core/Health.cs`.
- Código ya hecho de la tarea 09: ancla `StumpCrack` del tocón y el evento al terminar el desplome.
- Lección de la tarea 04: lo creado por código se destruye con el objeto.

## Qué hacer
1. **Importar** `Reward/*` (23 hojas) y cualquier hoja de presentación que falte (`UI_RelicHalo`, `UI_RelicFillet_*`, `Title_*`, iconos). Si falta algo, **para y avisa**.
2. **Recompensa en la arena** (al terminar el desplome): por la columna de luz bajan, ~1,8 s con vaivén lateral, **el corazón** (`Item_HeartContainer_Idle` de la tienda + `Fx_HeartGlow` detrás) y **la Espora morada** (`Item_PurpleSpore_Fall`, gira); se posan delante del tocón (corazón en pie + (−22, +26) px, espora en pie + (+22, +26) px) con `Fx_RewardShadow` debajo y **flotan** (`Item_PurpleSpore_Idle`). **Se cogen al tocarlos.** Corazón: **+1 corazón máximo permanente** (como `SecretReward`, dos medios; cura la diferencia) con su sonido. Espora morada: se guarda como **objeto clave** (ver 5) y lanza su **presentación pequeña** («OBJETO CLAVE», `Title_Key_PurpleSpore`, efecto «Abre un camino secreto en el piso 4»).
3. **Entrada al tocón:** en la grieta (`StumpCrack`) un `PromptInteractable` con **E** → **fundido dorado** (`#e0a234`, ~0,45 s) → la sala interior. Al salir, al revés, apareciendo junto a la grieta. Solo se puede entrar cuando termina el desplome.
4. **Sala interior** (escena aparte o sala montada en el mismo mundo lejos de la arena; elige lo más simple y estable en `Test_BossLepidodendron`, y dilo en `Dudas`): fondo fijo `Lepido_StumpRoom` 480×270, suelo círculo r 92 en (240, 158) **con colisión**; el jugador aparece en (240, 244) mirando al norte; **salida** `Lepido_StumpRoom_Exit` (E) en (240, 252); `Fx_StumpRoom_Rays` encima de todo y `Fx_StumpRoom_Light` (se quita al coger la reliquia); setas `Lepido_Shroom{A,B,C}_Idle` en las posiciones del README; **pedestal** `Lepido_Pedestal_Lit` en (240, 164) con la reliquia `Relic_RootAncestral_Idle` flotando encima (esquina en pedestal + (−12, −61)). Sin enemigos, sin daño, sin música de combate (déjala como esté).
5. **Reliquia y objetos clave (sistema mínimo, reutilizable por futuras reliquias):**
   - Registro persistente durante la partida (donde viva el estado de la partida en `GameManager` o un componente de progreso nuevo; haz lo más pequeño y descríbelo en `Resultado`): `Relics` (lista) y `KeyItems` (lista). La Raíz ancestral y la Espora morada se añaden con identificadores estables (`RootAncestral`, `PurpleSpore`).
   - **Efecto de la Raíz ancestral: al llegar a un piso nuevo recuperas toda la vida.** Engánchalo al evento de cambio de piso que ya exista; si no hay evento claro, deja el método `OnFloorEntered()` listo y una prueba que lo invoque, y dilo en `Dudas` (el enganche real al piso es posterior).
   - **Presentación** de reliquia/objeto clave (formato común, juego en pausa; **todas las medidas y textos en el README «Recompensa»**): pantalla atenuada a `(6, 4, 10)` alfa 175, `UI_RelicHalo` girando, la reliquia sube al centro (240, 98) en versión `_Big`, etiqueta «R E L I Q U I A» (Silkscreen 8, `#ffd468`; para la espora «O B J E T O   C L A V E»), `Title_Relic_RootAncestral` + filetes, efecto «Al llegar a un piso nuevo recuperas toda la vida» en crema; ~0,5 s de entrada, 0,3 s de texto, se cierra con **E** o a los ~2,5 s. Al cerrarse con la reliquia, el pedestal hace `Pedestal_Fade` → `Pedestal_Empty` (para siempre), `Fx_StumpRoom_Light` se apaga.
   - **Iconos en la pausa** (`Icon_RootAncestral_16/32`, `Icon_PurpleSpore_16/32`): una fila **Reliquias** y otra **Objetos clave** en el panel de pausa si el jugador tiene alguno (oculta la fila si no). Mínimo: icono con nombre y efecto al pasar el cursor/seleccionar, en el estilo del panel.
6. **Cogerlas una sola vez:** si se destruye y se recrea la sala en la misma partida, lo ya cogido no reaparece (corazón, espora, reliquia). El estado de «ya cogido» vive en el mismo registro (provisional hasta el enganche al piso).
7. **Teclas de prueba** (en `DebugCheats`, libres, documentadas): forzar la bajada de la recompensa, teletransportar junto al tocón, abrir la sala interior, dar la reliquia y la espora (sin presentación), reiniciar el estado de recompensa.
8. **Pruebas** en `LepidodendronBossTests`: (a) la recompensa aparece solo tras el desplome; (b) el corazón da +2 de vida máxima una sola vez; (c) la espora entra en `KeyItems` y lanza su presentación; (d) E en la grieta lleva a la sala y E en la salida vuelve; (e) la reliquia se coge con E, entra en `Relics`, el pedestal queda `Empty` y no se puede volver a coger; (f) `OnFloorEntered()` con la reliquia deja la vida al máximo y sin ella no hace nada; (g) la pausa muestra las filas solo si hay objetos; (h) sin fugas al destruir en medio; (i) **no se rompen** L1-L5a ni `MeganeuraBossTests`/`BossHealthBarTests`.

## Números y reglas (cerrados con la usuaria 2026-11-13)
- +1 corazón máximo (2 medios), permanente.
- Espora morada = objeto clave, no se pierde nunca; **abre un camino secreto en el piso 4** (el uso real no se programa aquí).
- Raíz ancestral: al llegar a un piso nuevo recuperas toda la vida.
- Fundido dorado ~0,45 s; presentación 0,5 s + 0,3 s + hasta 2,5 s.

## Archivos que puede tocar
- `Scripts/Enemies/LepidodendronBoss*.cs` (nuevo `.Reward.cs`), nuevos `LepidodendronStumpRoom.cs`, `Relics.cs` (o similar) en el sitio que encaje, el panel de pausa en `Scripts/UI/` (ampliación mínima), `Core/GameManager.cs` (solo el registro), `Editor/ArtSetup.BossLepidodendron.cs`, `Editor/LepidodendronBossSetup.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Solo lo imprescindible y justificado fuera de eso — avísalo en `Resultado`.

## Fuera de alcance (NO hacer)
- No dibujar ni tocar PNG ni `.py`. No engancharlo al piso (`Floor3.asset`, puerta secreta, uso de la Espora en el piso 4). No audio nuevo (déjalo para el paso 8). No tocar otros jefes ni el comportamiento de L1-L5a.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba`; `LepidodendronBossTests`, `MeganeuraBossTests`, `BossHealthBarTests`.
- A ojo (usuaria): la bajada de las recompensas, las presentaciones, la sala del tocón (luz, setas, pedestal), la reliquia al cogerla, y la pausa con sus iconos.

## Resultado (implementada por Claude el 2026-11-13: Codex se quedo sin tokens)
- **Hecho:** L5b completa en `codex/lepido-5b-recompensa`.
  - **Recompensa:** al acabar el desplome (`Collapsed` -> `BeginReward`) bajan por la columna de luz el corazon (`Item_HeartContainer_Idle` de la tienda + `Fx_HeartGlow`) y la Espora morada; flotan con sombra y se cogen al tocarlas. Corazon: +2 de vida maxima (`SetMax`). Espora: objeto clave + presentacion pequena «OBJETO CLAVE».
  - **Sala del tocon:** `PromptInteractable` (E) en la grieta (`StumpCrack`) -> fundido dorado (0,45 s) -> sala interior montada lejos de la arena (origen = entrada + 400 u en y; telon negro, suelo circular r 92 px con `EdgeCollider2D`, setas, rayos, luz, pedestal con la reliquia, salida E). La camara se queda en la sala sin zoom (`InStumpRoom` en `UpdateCamera`). Salir devuelve junto a la grieta.
  - **Reliquia:** E junto al pedestal -> presentacion con halo (formato comun del README) -> pedestal `Pedestal_Fade` -> `Pedestal_Empty` para siempre; se apaga la luz.
  - **Sistema minimo:** `Core/RunInventory.cs` (estatico: reliquias, objetos clave y banderas «ya cogido»; se vacia al empezar/reiniciar/volver al menu en `GameManager` y en `SubsystemRegistration`). `RunInventory.OnFloorEntered(player)` cura toda la vida con la Raiz ancestral. `UI/RewardPresenter.cs` (presentacion y fundidos, congela `timeScale` y lo restaura), `UI/PauseRelicsRow.cs` (filas «OBJETOS CLAVE» y «RELIQUIAS» con icono, nombre y efecto en la pausa, ocultas si no hay nada; `GameUI` la anade al panel de pausa).
- **Archivos:** nuevos `LepidodendronBoss.Reward.cs`, `LepidodendronReward.cs` (`LepidodendronPrompt`, `LepidodendronDrop`), `RunInventory.cs`, `RewardPresenter.cs`, `PauseRelicsRow.cs`; ampliados `LepidodendronBoss.cs` (camara en la sala), `.Death.cs`, `.Scenery.cs` (hojas obligatorias), `GameManager.cs` (reinicio del inventario), `GameUI.cs`, `ArtSetup.BossLepidodendron.cs` (importa `Reward/`, 23 hojas), `LepidodendronBossSetup.cs` (corazon de la tienda y tecla E), `DebugCheats.cs`, pruebas.
- **Teclas (Alt izquierdo/derecho):** Alt+F5 recompensa (baja lo no cogido), Alt+F6 junto a la grieta, Alt+F7 dentro de la sala, Alt+F8 dar reliquia y espora sin presentacion, Alt+F9 reiniciar el estado de recompensa. Antes: Alt+F1 desplome (hace falta primero).
- **Pruebas:** `LepidodendronBossTests` + `MeganeuraBossTests` + `BossHealthBarTests`: **135/135** (7 nuevas: recompensa baja y corazon una vez, espora y presentacion, entrada/salida del tocon, reliquia unica y pedestal vacio, OnFloorEntered con y sin reliquia, filas de la pausa, sala no creada antes del desplome).
- **Dudas / pendiente:**
  - `OnFloorEntered` NO esta enganchado a un cambio de piso real (el enganche al piso queda fuera, como se acordo); la prueba lo invoca a mano.
  - El estado «ya cogido» vive en `RunInventory` (provisional hasta el enganche al piso).
  - La sala interior esta en la misma escena, lejos de la arena; si hay minimapa o salas con camara propia en el piso real, habra que revisarlo al enganchar.
  - Sin sonido nuevo (se reusa `Sounds.HeartCoral`); el audio es del paso 8.
  - Falta la comprobacion visual de la usuaria: bajada de las recompensas, presentaciones, sala del tocon (luz, setas, pedestal, colisiones del circulo) y la pausa con sus iconos.
