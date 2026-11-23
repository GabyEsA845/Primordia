# Tarea 21 — Sala de paso del piso 3: bajar el sumidero

- **Rama:** `codex/piso3-salida` (continúa la tarea 20, ya commiteada)
- **Escrita por:** Claude (2026-11-23) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Tras probar `Test_Floor3Transition`, la usuaria ve el sumidero **demasiado arriba**: la sala se siente muy grande por debajo de él. Hay que **bajarlo un poco** (ligeramente: unas 3-4 celdas, a ajustar a ojo), **solo en la sala de paso del piso 3**. Las de los pisos 1 y 2 no cambian.

## Qué hacer
1. Hoy la fosa va en `placed.FloorCenter` (`FloorBuilder.cs`, instancia de la fosa ~931; también lo usan `PitCells`/zona de claro ~522-547, el filtro de helechos ~829-852 y `ThemeTransition`/`RippleTransitionDistance` ~479-499 para el reparto de franjas). Añade un desplazamiento configurable de la fosa (p. ej. `FloorConfig.PitOffset`, `Vector2`, por defecto `(0,0)`, con `ConfigureTransition`/setter internal) y úsalo en **todos** esos puntos con un único cálculo (`PitCenter(placed, config)`), para que fosa, claro de decorado, helechos y reparto de franjas sigan coincidiendo.
2. En `Floor_3` ponlo a `(0, -3.5)` (valor provisional; el menú del piso 3 lo fija, para que repetirlo sea seguro). `Floor_1`/`Floor_2` quedan en `(0,0)`.
3. Comprueba que la fosa sigue cabiendo en suelo transitable con su holgura (`PitClearance`) y que las calcomanías de arena y la mezcla siguen sin tocarla. Si con -3,5 se sale del suelo en alguna orientación de la entrada, recorta el desplazamiento al máximo que quepa y dilo en el Resultado.
4. Pruebas: ampliar `FloorTransitionPiso3Tests` (la fosa de `Floor_3` queda más abajo que el centro y sobre suelo libre en las semillas 7, 42, 91; `Floor_1`/`Floor_2` idénticas a antes). Relanzar `FloorTransition*` y `RoomArtTests`. Regenerar `Test_Floor3Transition` con el menú.

## Fuera de alcance
Decoraciones/objetos de la sala (arte pendiente, lo hace Opus), plantilla de la cueva, piso 4, audio. No editar PNG ni ejecutar `.py`.

## Resultado
Codex se quedó sin tokens a medias; la terminó Claude (2026-11-23). `FloorConfig.PitOffset` (`ConfigurePitOffset`), `FloorBuilder.PitCenter(placed, config)` usado por la fosa, su claro (`PitCells`), los helechos y el reparto de franjas; `Floor_3` = (0, -3,5) desde el menú del piso 3; el claro de la fosa se mantiene en el tema final. Pruebas ampliadas en `FloorTransitionPiso3Tests`; `Floor_1`/`Floor_2` con offset cero y mapa idéntico. Pendiente: que la usuaria lo vea (si hay que subirlo o bajarlo más, cambiar el valor en `FloorTransitionSetup.Floor3.cs` y repetir el menú).
