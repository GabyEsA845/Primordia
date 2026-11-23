# Tarea 22 — Decoración de la sala de paso del piso 3 (hecha por Claude)

- **Rama:** `codex/piso3-salida` · **Estado:** implementada y con pruebas, pendiente de que la usuaria la vea (sin commit)

Arte aprobado en `docs/multimedia-temp/floor3-exit/decor/` (README, sección «Decoración»). Menú: el mismo de la sala de paso del piso 3 (`Primordia > Floor transition > Crear sala de paso del piso 3…`), que ahora importa el decorado (`ArtSetup.ImportFloor3ExitDecor` → `ExitDecorArt_Floor3.asset`) y lo asigna a `Floor_3` (`FloorConfig.ExitDecor`).

- `Scripts/Rooms/ExitDecorArt.cs` (piezas por nombre), `ExitDecorParts.cs` (`ExitDecorObstacle` con caja 2×1 y translúcido al pasar por detrás, `ExitDecorPlant` Sway/Rustle, `ExitDecorGust`, `ExitDecorWind`), `FloorBuilder.ExitDecor.cs` (colocación).
- **Sin solapes:** cada pieza reserva su rectángulo entero (imagen completa) con 0,12 de holgura; también esquiva el sumidero (+2), las puertas (+2,5), lo ya colocado y exige suelo debajo. Peñascos y troncos de pie, además, con 0,5 de aire y lejos de paredes. Solo el rastro se solapa consigo mismo (es un trazo).
- Franja quemada: troncos/tocones/ascuas del tema Burnt (pocos); intermedia: troncos hundidos, tocones, helechos secos, carbón, barro; pérmica: peñascos, lajas, piedras, cascajo, plantas (a la sombra de peñascos y al pie de los acantilados, no junto al sumidero), barro y sal. Rastro de sinápsido desde el peñasco con pasillo más despejado hasta el sumidero (8 direcciones, pie derecho reflejado). Ráfagas cada 4-7 s por filas libres de obstáculos y sumidero; las plantas se doblan al pasar.
- Pruebas (`FloorTransitionPiso3Tests.Decoracion_*`, semillas 7/42/91): ninguna pieza se solapa, todo sobre suelo, nada sobre el sumidero (ni el rastro, contra su elipse), obstáculos con caja, plantas, ráfagas, y los pisos 1 y 2 salen sin decorado. 48/48 con `FloorTransition*`, `RoomArtTests` y `Floor2StructureTests`.
- Números provisionales (a ajustar al verlo): cantidades por regla en `ExitRules`/`BuildExitDecor`, holguras, 3,5 celdas del sumidero.
