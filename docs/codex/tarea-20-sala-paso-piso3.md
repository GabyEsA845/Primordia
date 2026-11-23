# Tarea 20 — Sala de paso del piso 3 → 4: importar y programar el arte (sumidero de arena y mezcla entre franjas)

- **Rama:** `codex/piso3-salida` (crearla desde `feature/arthropleura-canopy-art`, que trae el arte aprobado y todo el código del piso 3)
- **Escrita por:** Claude (2026-11-23) · **Implementa:** Codex
- **Estado:** implementada y comprobada en batchmode; pendiente de prueba manual de la usuaria

## Objetivo
La sala de paso del piso 3 es una cueva con **tres franjas seguidas**: tierra quemada (`Burnt`, ya existe) → arena intermedia (`BurntSand`) → arena roja del Pérmico (`Permian`), con un **sumidero de arena** (la fosa) en la última. Hoy `Floor_3` no tiene sala de paso (`Floor3Setup.cs:102` llama a `ConfigureTransition(null, null, null, null)`). Hay que importar el arte ya aprobado y programar el sistema para que **la frontera entre franjas nunca sea una línea de corte** (regla de la usuaria, `docs/memoria-claude/primordia-theme-transitions.md`: «luce muy poco natural» si es una línea).

El arte es solo importar; no se genera ni se edita ningún PNG ni se ejecutan los `.py`.

## Contexto mínimo (léelo antes)
- `AGENTS.md`, `docs/memoria-claude/primordia-theme-transitions.md` (la receta de las 5 reglas) y **el README completo `docs/multimedia-temp/floor3-exit/README.md`** (tablas de piezas y sección «Qué necesita el código»).
- Modelo a copiar: la sala de paso del piso 2, `Assets/_Primordia/Editor/FloorTransitionSetup.Floor2.cs` (menú, `CreateCaveTemplate`, `CreatePitPrefab`, `ConfigureTransition`, `ConfigureNextFloor`, escena de prueba `Test_Floor2Transition`) y `ArtSetup.FloorTransition.cs` (`ImportSwampPitArt`, `ImportTransitionThemes`).
- Importar temas: `ArtSetup.Floor3Themes.cs` (`ImportFloor3Theme(themeName, …, folderOverride)`: ya acepta una carpeta distinta; crea tiles, `RoomTheme`, etc.).
- Pintado: `Scripts/Rooms/RoomPainter.cs` (cómo se pone `MudTile(máscara)` + `MudCorner` encima del suelo con `KindMask` y `Corner`; las `pitCorners`), `RoomTheme.cs` (`mudTiles` 16, `mudCorners`, `ConfigureMud`), `FloorBuilder.cs` (`ThemeTransition` ~línea 470: reparte 3 temas por celda con `entryTheme`/`MidTheme`/`NextTheme` y ruido `CellNoise`/`ThemeNoise`; ver también ~829 helechos de la fosa y ~931 instancia de la fosa), `FloorMap` (`SetTheme`/`GetTheme`), `TransitionPit.cs` (fosa y `LeaveFloor`: sin piso siguiente da `GameState.Victory`).

## Arte (en `docs/multimedia-temp/floor3-exit/`, 16 px/unidad, filtro Point, sin compresión)
- `Permian/` y `BurntSand/`: `tiles/` (35 tiles cada uno, mismos nombres y orden que los tramos del piso 3), `Tileset_<Tema>.png`, `deco/Spill_*` (solapas). Importarlos como dos `RoomTheme` nuevos (`Permian`, `BurntSand`) con el mismo método que `Swamp/Smoke/Burnt`, sin lodo, puertas ni maleza (comprueba qué campos pueden quedar vacíos sin romper `RoomPainter`/`FloorBuilder`; si algo exige puertas, reutiliza las de `Burnt`).
- `blend/` (ver tabla del README): `SandEdge_<1-15>` + 4 `Corner_*` (suelo `BurntSand` junto a `Permian`), `DustEdge_*` (suelo `Burnt` junto a `BurntSand`), `AshEdge_*` (suelo `Permian` junto a franjas quemadas), `SandCap_*` (tapas/bordes quemados junto a pared `Permian`), `Wall_Face{Upper,Lower}_{BurntToSand,SandToBurnt}_{0,1}` (caras de frontera), `Sand_Drift_0-2` (calcomanías 48×32, 32×24, 40×20). La máscara es **N=1, E=2, S=4, O=8** (15 piezas numeradas 1-15; el 0 no tiene pieza) y las esquinas `NE/NO/SE/SO`.
- Sumidero: `Transition_Pit_Sand.png` (8 cuadros, 112×80, 167 ms en bucle, **mismo tamaño y pivote que la fosa/charca del piso 2**) y `Transition_Dust_Sand.png` (4 cuadros de 8×8: 0-1 el grano sube en bucle, 2 se deshace en polvo, 3 motas; como la burbuja de la charca, `SetBubblePop`). Revisar `vista_previa_*.png` para ver el resultado esperado.
- Icono del mapa: el de siempre (`docs/multimedia-temp/floor-transition/UI_Map_Transition.png`, ya importado por la sala del piso 1/2).

## Qué hacer
1. **Importación** (`ArtSetup.Floor3Exit.cs`, parcial nuevo de `ArtSetup`): temas `Permian` y `BurntSand`; piezas de `blend/` como tiles (tiles sin colisión salvo las caras y tapas, que sí la tienen si el tile al que sustituyen la tiene: copia el criterio de `CreateTile(collides: name.StartsWith("Wall_"))`); calcomanías `Sand_Drift_*` como sprites; sumidero y polvo con el mismo método que `ImportSwampPitArt`.
2. **Datos del tema**: añade a `RoomTheme` (con `ConfigureXxx` internal como los demás) los campos serializados para las capas de mezcla: `blendFloorTiles` (3 juegos: `SandEdge`, `DustEdge`, `AshEdge`; cada uno 15 + 4 esquinas), `blendCapTiles` (`SandCap`, 15 + 4), y las caras de frontera y calcomanías. Piensa en una estructura pequeña y genérica (p. ej. `BlendSet` con `TileBase[16]` + `corners[4]`) en vez de campos sueltos. Sin esos datos el tema se comporta exactamente como hoy.
3. **Frontera con ruido** (`FloorBuilder.ThemeTransition`): hoy decide por distancia a la puerta con ruido pequeño (`ThemeNoise`); para la sala del piso 3 el tema de cada celda debe salir de una **frontera que ondula por fila** (un desplazamiento suave, p. ej. suma de dos senos o ruido de valor por fila, de amplitud ±3-4 celdas, además del ruido por celda), no una columna fija. **La cara de abajo usa el tema de la cara de arriba** (el par `Wall_FaceUpper`/`Wall_FaceLower` no puede partirse entre dos temas). Mantén el comportamiento actual para los pisos 1 y 2 (flag en `FloorConfig` o en el propio tema, p. ej. `rippleBorders`).
4. **Capas de mezcla** (`RoomPainter`, como `Mud_<máscara>` y `pitCorners`): en cada celda de suelo del tema de **menos rango**, ponle en una capa encima (la misma `Tilemap` de detalles o una nueva) la pieza de máscara de sus vecinos N/E/S/O que son de **más rango**; esquina solo si sus dos lados no están ya en la máscara (usa `KindMask`/`Corner` como el lodo, pero por tema, no por tipo de celda). Rangos: `Burnt` < `BurntSand` < `Permian`. Reglas: `BurntSand` junto a `Permian` → `SandEdge`; `Burnt` junto a `BurntSand` → `DustEdge`; en sentido inverso (la franja vieja deja restos en la nueva) `Permian` junto a franjas quemadas → `AshEdge`. Paredes: `SandCap_*` en las tapas/bordes quemados junto a pared `Permian`; **caras de frontera** `Wall_Face*_BurntToSand_{0,1}` / `_SandToBurnt_{0,1}` en la celda quemada cuando la cara vecina es `Permian` (a la derecha o izquierda), eligiendo variante 0/1 por ruido de celda.
5. **Calcomanías sueltas**: 1 `Sand_Drift_<n>` cada ~25-40 celdas de suelo cerca de las fronteras (posición/variante por `CellNoise`, nunca solapadas con la fosa ni puertas).
6. **Sala de paso del piso 3**: menú nuevo `Primordia > Floor transition > Crear sala de paso del piso 3 (sumidero) y escena de prueba` en `FloorTransitionSetup.Floor3.cs`, modelado sobre el del piso 2: prefab `TransitionPit_Sand` (sumidero + polvo con `SetBubblePop`); `Floor_3.ConfigureTransition(cave, pit, BurntSand, Permian)` (la franja de entrada es `Burnt`, la del tramo anterior del piso 3 — revisa cómo `entryTheme` se elige con `HasBands` y adáptalo si hace falta para que la entrada use `Burnt`); icono del mapa de siempre; escena de prueba `Test_Floor3Transition` (inicio + un «jefe» perseguidor + sala de paso, como la del piso 2; la sala de paso es lo que se quiere ver). **Sin piso 4 todavía**: `nextFloor = null` → `LeaveFloor` da victoria; `ConfigureNextFloor` sin Bocas (array vacío) hasta que exista el piso 4 (anótalo en el Resultado). No toques los jefes ni el enganche de la Arthropleura a `Floor_3`: comprueba solo que `Floor_3` sigue generándose (los jefes de `Floor_3` actuales siguen colocándose; `FloorBuilder` línea ~227 trata el jefe distinto cuando hay transición).
7. **Helechos y decorado** de la sala: sin maleza en `BurntSand`/`Permian`; los helechos solo de la sala de paso del piso 2 (en celdas `Swamp`) no deben aparecer aquí (revisa `FloorBuilder` ~829: está atado a `NextFloorTheme`; que solo se use si el tema tiene helechos).
8. **Pruebas EditMode/PlayMode** (nuevo `FloorTransitionPiso3Tests` en `Tests/`): los dos temas importan sin tiles nulos en las listas obligatorias; `ThemeTransition` con frontera ondulada **produce una frontera no recta** (la columna de cambio de tema varía al menos 3 celdas entre filas) y siempre tres temas en orden `Burnt → BurntSand → Permian` de la puerta a la fosa; las dos caras del par Upper/Lower comparten tema; las capas de mezcla ponen una pieza donde un vecino es de más rango y ninguna donde no; `Floor_1`/`Floor_2` generan exactamente igual que antes (compara el mapa/seed de un caso con las pruebas existentes; no se rompe ninguna de las suites de transición actuales); la escena `Test_Floor3Transition` se genera y la fosa existe.

## Números (provisionales)
Ondulación de la frontera ±3-4 celdas (suave, periodo ~8-14 filas); calcomanías 1 cada ~25-40 celdas; capas de mezcla solo en celdas de menos rango que tengan un vecino de más rango; sumidero a 167 ms por cuadro en bucle; el resto, como en la sala del piso 2.

## Archivos que puede tocar
`Editor/ArtSetup.Floor3Exit.cs` (nuevo), `Editor/FloorTransitionSetup.Floor3.cs` (nuevo), `Scripts/Rooms/RoomTheme.cs`, `RoomPainter.cs`, `FloorBuilder.cs`, `FloorConfig.cs` (solo si necesitas un flag), pruebas nuevas, escena de prueba nueva, `docs/codex/` (el «Resultado»). Arte solo importar (los `.meta` de lo importado se pueden subir).

## Fuera de alcance
Piso 4 y sus Bocas, audio (ambiente/sonido del sumidero), calor que ondula sobre el sumidero (opcional, por código, no hace falta ahora), enganchar los jefes (Arthropleura/Megarachne) a `Floor_3`, cambiar la sala de paso de los pisos 1 y 2, **ejecutar generadores `.py` o editar PNG**.

## Cómo probar
Ejecutar el menú del paso 6 y abrir `Test_Floor3Transition`: desde la puerta se ve la cueva `Burnt` → arena que entra con borde ondulado → `Permian` con el sumidero; **sin ninguna línea recta de cambio de tema** en suelo ni en paredes (revisa especialmente las caras de las paredes en la frontera); calcomanías de arena sueltas; el sumidero gira y suelta polvo; al saltar dentro, victoria. Comparar con `docs/multimedia-temp/floor3-exit/vista_previa_sala.png`. Lanzar las pruebas nuevas y las suites de transición/pisos existentes.

## Resultado

Implementada el 2026-11-23 en `codex/piso3-salida`.

### Implementación
- Importados `BurntSand`, `Permian`, solapas, cuatro juegos de máscaras/esquinas, ocho caras de frontera, tres calcomanías, sumidero y polvo. Los **187 PNG importados coinciden byte por byte** con sus fuentes aprobadas. No se ejecutaron generadores ni se retocaron imágenes.
- `RoomTheme` incorpora reglas opcionales `BlendSet` y `rippleBorders`. Las reglas viven en `BurntSand`, con referencias a los temas participantes; el asset compartido de `Burnt` queda intacto.
- Frontera por coordenadas relativas a la entrada, ondulada con dos senos (amplitud máxima 3,5 celdas, periodos 12 y 8), más grano por celda. Funciona con las cuatro orientaciones de entrada. Las caras inferiores heredan el tema de las superiores.
- Máscaras de suelo en ambos sentidos, capas separadas para las cuatro esquinas simultáneas, tapas y caras de pared emparejadas. Calcomanías deterministas con frecuencia provisional de 1/32 entre candidatas cercanas a fronteras; su huella evita puertas y la zona reservada de la fosa.
- `Floor_3` usa la cueva existente y `TransitionPit_Sand`, con entrada `Burnt` (la selección existente ya toma la última franja), intermedio `BurntSand` y final `Permian`. El prefab tiene ocho cuadros a 167 ms y polvo de cuatro cuadros, con los dos finales de desvanecimiento (`SetBubblePop(2)`).
- El icono habitual ya estaba asignado en `GameUI`; se reutiliza. Los temas nuevos no tienen lodo, maleza ni helechos; el filtro existente de helechos ya omite temas sin vegetación. Las puertas utilizan el tema de entrada de la sala.
- **No existe piso 4:** `nextFloor = null`, `ConfigureNextFloor` con Bocas vacías; se conserva la victoria de `LeaveFloor`. No se modificó la selección ni la configuración de jefes de `Floor_3`, ni audio.

### Menús ejecutados y escena
- `Primordia > Floor transition > Crear sala de paso del piso 3 (sumidero) y escena de prueba` (`FloorTransitionSetup.CreateFloor3Transition`), ejecutado en Unity 6000.6.2f1, batchmode, con el editor cerrado. Resultado correcto; log: `Logs/tarea20-import.log`.
- Generados `Test_Floor3Transition.unity` y `Floor_3TransitionTest.asset`: inicio, jefe perseguidor y sala de paso, con HUD y equipamiento de prueba.
- `Primordia > Floor transition > Capturar revisión de la sala del piso 3` (`CaptureFloor3Transition`), batchmode con renderizado. Captura: `Logs/tarea20-sala.png`; log: `Logs/tarea20-captura.log`. Revisada visualmente junto a `vista_previa_sala.png`: fronteras onduladas en suelo y paredes, mezcla de arena/ceniza y sumidero en Permian. La captura no modifica el arte ni guarda cambios de escena.

### Pruebas y comprobaciones
- PlayMode, filtro `Floor`: **263 ejecutadas, 257 pasaron, 6 fallaron**. Son las seis ya documentadas: `Floor3EnemiesTests` (Calamites ×2, Dragonfly ×2) y `Floor3LimbsTests` (JumpingLegs ×2). `FloorTransitionTests`: **8/8**. XML/log: `Logs/tarea20-tests.xml` y `.log`.
- Verificación final PlayMode, `FloorTransitionPiso3Tests;RoomArtTests`: **27/27 pasaron** (**15/15 nuevas**, **12/12 de arte**). XML/log: `Logs/tarea20-final-tests.xml` y `.log`.
- Las nuevas pruebas cubren piezas obligatorias y arrays sin nulos, ausencia de vegetación, máscaras/esquinas simultáneas, tapas y pares de caras en ambos sentidos, ondulación de al menos tres celdas y orden de las tres franjas en las cuatro orientaciones, generación real de `Floor_3` con jefe y fosa (semillas 7, 42 y 91), escena/prefab guardados y comparación celda por celda con el algoritmo anterior de las transiciones de `Floor_1` y `Floor_2`.
- La primera tentativa de baseline no ejecutó pruebas por conexión al cliente de licencias dentro del aislamiento; se resolvió ejecutando Unity con acceso a la licencia. Una compilación inicial del test se corrigió (`RectInt.PositionEnumerator` no admite `Cast`). En la primera comprobación adicional de arte pasaron 25/27: los dos tests nuevos esperaban erróneamente colisión `Sprite`; se corrigió a `Grid`, el criterio ya utilizado por `CreateTile`, y la ejecución final pasó completa.
- Revisadas las referencias de 160 assets/prefab/escena: resueltas en Assets o en los paquetes de Unity. C# conservado en CRLF. `Floor_1.asset`, `Floor_2.asset` y `ProjectSettings/ProjectSettings.asset` sin cambios.

### Archivos tocados
- Nuevos: `Editor/ArtSetup.Floor3Exit.cs`, `Editor/FloorTransitionSetup.Floor3.cs`, `Tests/PlayMode/FloorTransitionPiso3Tests.cs` y sus `.meta` (bajo `Assets/_Primordia/`).
- Modificados: `Scripts/Rooms/RoomTheme.cs`, `RoomPainter.cs`, `FloorBuilder.cs` y `Data/Rooms/Floor_3.asset`.
- Generados: `Art/FloorTransition/Sand/` (PNG, tiles, temas y metadatos), `Prefabs/Rooms/TransitionPit_Sand.prefab`, `Data/Rooms/Floor_3TransitionTest.asset`, `Scenes/Test_Floor3Transition.unity` y sus `.meta`.
- Esta tarea. Los logs/XML/captura de comprobación quedan en `Logs/`, fuera del commit. Los cambios ajenos presentes en el directorio se dejan intactos y fuera del commit.

### Qué queda
- Prueba manual de la usuaria en `Test_Floor3Transition`: movimiento, animación/polvo y salto interactivo hasta victoria. Se han comprobado los datos de animación y la lógica mediante las suites; no se ha realizado una sesión manual interactiva.
- Piso 4, sus Bocas y audio siguen fuera de alcance. Sin dudas de diseño pendientes. Commit en la rama solicitada; sin push ni merge.
