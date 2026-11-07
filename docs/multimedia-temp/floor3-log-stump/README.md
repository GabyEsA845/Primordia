# Tronco caído y Tocón hueco (piso 3, salas especiales)

Arte aprobado por la usuaria el 2026-11-06 (Opus). Falta importarlo y programarlo (Sonnet).
Regenerar: `python generar_tronco.py` y `python generar_tocon.py` (con el Python 3.12 de `%LOCALAPPDATA%`, ver la skill
`primordia-sprites`). Usan `floor3-environment/base_piso3.py` y el motor `generar_tilesets_piso3.py`.

**Cofre de cono:** no se ha dibujado nada nuevo. Ya existe en `floor3-environment/pieces/Cone_Big_*` y `Cone_Small_*`
(importado en `Art/Floor3/floor3-environment/pieces/`). Hay que cambiar `ConeChestPlaceholder` por esas hojas en
`ConeChest.cs` (Idle 4 cuadros a 220 ms, Open 6 a 90 ms, Opened 1; **cuadro que cuenta: 4**, suelta la biomasa; ver
`floor3-environment/README.md`).

## Tronco caído (`Swamp/`, `Smoke/`, `Burnt/`: una versión por tramo)

Lepidodendron derribado y hundido a medias que cruza el lago de lodo profundo de `Room_Special_FallenLog` por las dos
filas de la puerta lateral (es el paso seguro). Se monta con piezas de 16 px de ancho:
`Log_EndLeft` + tramos (`Log_Ground_*` en la orilla, `Log_Mid_*` en el lago) + `Log_EndRight`.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Log_Mid_{0..5}_Idle` | 8 | 16×48 | 160 | Tramo en el lago, en reposo: el lodo de los costados sube y baja 1 px, un brillo recorre la orilla, ondas que se alejan. Todas las casillas con el **mismo reloj** (las ondas encajan de un tramo a otro) |
| `Log_Mid_{0..5}_Step` | 6 | 16×48 | 70 | La casilla que pisa el jugador: el lodo sube hasta 4 px por los lados (se hunde) y salpica. Se lanza en cada pisada y vuelve a `Idle` |
| `Log_Mid_{0..5}_Dip` | 4 | 16×48 | 90 | Las dos casillas vecinas de la pisada: suben menos, sin salpicar |
| `Log_Ground_{0,1}` | 1 | 16×48 | — | Tramo apoyado en la orilla (contorno y sombra) |
| `Log_EndLeft` | 1 | 40×64 | — | Cepa con las raíces arrancadas (extremo izquierdo, sobre la orilla, contra la pared) |
| `Log_EndRight` | 1 | 48×64 | — | Copa partida con la horquilla y sus hojas (extremo derecho) |
| `bubbles/Fx_MudBubble` | 7 | 12×12 | 110 | Burbuja de lodo grande: asoma, crece, revienta y deja un anillo. Igual en los 3 tramos |
| `bubbles/Fx_MudBubble_Small` | 6 | 8×8 | 110 | Burbuja pequeña |

Variantes del lago: 0 musgo que cuelga hasta el lodo, 1 rama gruesa con hojas que se mete en el lodo de detrás,
2 desconchón de corteza y raíz hacia el lodo de delante, 3 más hundido con charco encima, 4 hojas caídas y un hongo,
5 nudo hueco con hongos. Reparto sugerido: muchas 0, algunas 3 y el resto de vez en cuando, sin dos iguales seguidas.

**Pivotes (importante):**
- Tramos (16×48): las filas **8..39** de la hoja son las dos casillas del tronco. Pivote **abajo a la izquierda,
  8 px por encima del borde**, es decir `(0, 8/48)`: el sprite se pone en la esquina de abajo a la izquierda de la
  casilla de abajo. Por encima y por debajo sobresalen las ondas, ramas y raíces.
- Extremos (40×64 y 48×64): las filas **16..47** son las dos casillas. Pivote `(0, 16/64)`. `Log_EndLeft` ocupa las
  casillas x0..x0+1 (2 casillas y media de dibujo); `Log_EndRight`, las 3 últimas (su dibujo acaba en astillas y la
  rama sobresale a la derecha).
- Orden de dibujo: el tronco va **por debajo** de personajes y enemigos (es suelo), por encima del lodo.

**Para el código (Sonnet):**
- En el lago, las casillas del tronco deberían **pintarse como lodo** (para que el lodo rodee el tronco sin la orilla
  de pradera) pero **dejar andar sin hundirse**. Hoy la plantilla las deja como suelo (`.`).
- Pisada: al pisar una casilla del lago (o en cada paso del jugador sobre ella) se lanza `Step` en esa casilla y `Dip`
  en las dos vecinas; al acabar vuelven a `Idle` con el reloj común. Los enemigos que caminan pueden hacer lo mismo.
- Burbujas: cada 0,6-1,5 s, una burbuja (60 % grande) en un punto al azar del lodo a menos de 1 casilla de los
  costados del tronco, solo con el jugador en la sala.
- El linter avisa de 25-33 colores por cuadro en los tramos (mezclas de corteza, musgo, lodo y brillos); aceptado.

## Tocón hueco (`Stump/` y `pieces/`: igual en todos los tramos)

La sala `Room_Special_HollowStump` es el interior de un tocón gigante.

- **Tileset `Stump/`**: mismo motor y **mismos nombres** que los tilesets de los tramos (`tiles/`, `Tileset_Stump.png`,
  `deco/Spill_*`, `Doorway_Top`, `Door_*_Close/Open`), así que sirve tal cual con `WallRules`/`RoomPainter`, como el
  tileset `Amber` de la sala del ámbar. Paredes: madera por dentro (fibras, grietas, hongos de repisa, raicillas,
  huecos de nudo); borde del tocón desde arriba con anillos y musgo que cuelga; suelo de serrín y madera blanda.
- **Piezas `pieces/`:**

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_StumpLight` | 8 | 64×112 | 140 | Rayo de luz que entra por arriba y su charco en el suelo, con motas de polvo. Translúcido: mejor aditivo + luz 2D cálida. El **centro del charco está en (38, 100)** de la hoja: va bajo el pie del cono |
| `Deco_ShelfFungus_{A,B,C}` | 1 | 14×14 / 18×9 / 12×17 | — | Hongos de repisa sueltos para las caras de la pared |
| `Deco_Mushrooms_{A,B,C}` | 1 | 18×14 | — | Matas de setas en el suelo (pivote: el pie) |
| `Deco_WoodChunk_{A,B}` | 1 | 22×13 / 16×11 | — | Trozos de madera podrida caídos (pivote: el pie) |
| `Deco_Sawdust_{A,B}` | 1 | 26×12 / 18×10 | — | Manchas de serrín (del suelo, sin contorno) |

Maquetas: `maqueta_tocon.png`, `maqueta_tronco.png`; GIF (no van a git): `gif/escena_tocon.gif`,
`gif/escena_tronco_<tramo>.gif` (la rana cruza), `gif/tramo_<tramo>.gif`, `gif/burbujas.gif`.
