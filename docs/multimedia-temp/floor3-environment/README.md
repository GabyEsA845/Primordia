# Escenario del piso 3: pantano carbonífero (tanda 1a, tilesets)

Tres tramos del incendio con las mismas piezas y nombres: `Swamp/` (pantano verde), `Smoke/` (bosque humeante) y
`Burnt/` (tierra quemada). Mira primero `vista_previa_salas.png` (los tres tramos en una sala de muestra) y
`vista_previa_piezas.png` (todas las piezas a ×3); en `<Tema>/gif/` se ven las animaciones.

- `base_piso3.py`: paletas aprobadas (tanda 0, `../floor3-style/`) y utilidades comunes del piso.
- `generar_tilesets_piso3.py`: genera todo lo de esta carpeta.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_tilesets_piso3.py`

**Importar en Unity:** 16 PPU, *Point*, sin compresión; las hojas en *Multiple* con la celda de la tabla.

## Encajar en mosaico

Lo que toca los bordes de un tile se dibuja igual en todas sus variantes (lienzo que da la vuelta y misma semilla) y
lo propio de cada variante (troncos, raíces, musgo, manchas) queda dentro, así cualquier variante encaja con
cualquier vecina. Los bordes de la espesura ondulan con la misma curva en todos los tiles de ese lado.

## Tiles (16×16) en `tiles/` y `Tileset_<Tema>.png` (8 columnas, orden de la tabla)

| Tile | Uso |
|---|---|
| `Floor_0` … `Floor_5` | Suelo; 1 a 5 son variantes con detalles del tramo (musgo, brotes, piedras / hojarasca, ceniza, ramitas / grietas con ascuas, carbón) |
| `Floor_ShadowTop_0/1` | Primera fila de suelo bajo la cara de arriba |
| `Wall_Cap`, `Wall_Cap_1/2/3` | Espesura vista desde arriba; 1-3 son variantes con una mata más (**cambio de código**: hoy la tapa no tiene variantes) |
| `Wall_FaceUpper_0…3` / `Wall_FaceLower_0…3` | Cara de la pared de arriba **en pares**: `Upper_i` va encima de `Lower_i` (un tronco sigue de una a otra). **Cambio de código:** la de abajo elige variante con el hash de la celda de arriba |
| `Wall_EdgeLeft/Right/Bottom`, `Wall_CornerBL/BR` | Bordes y esquinas de dentro; el borde de hojas ondula y deja ver un poco de suelo en sombra |
| `Wall_OuterTopLeft/Right`, `Wall_Face{Upper,Lower}_End{Left,Right}` | Los 6 de salas con formas (esquinas de fuera redondeadas) |
| `Wall_EdgeLeft_FaceUpper/Lower`, `Wall_EdgeRight_FaceUpper/Lower` | Columna de pared **junto a la cara** (la rama de `WallRules` "junto a la cara de una pared de arriba"): el borde de hojas ondula y deja ver la cara que sigue detrás, no el suelo. **Cambio de código:** 4 piezas nuevas (con vuelta a `EdgeLeft/Right` si faltan); la de abajo es la de la fila con suelo debajo de la cara |

**Copa que cuelga:** todas las caras (y el arco y la maleza de arriba) llevan arriba la textura de la tapa en dos
lóbulos por tile que bajan hasta 6 px, con filo de hojas, sombra y puntas de fronda colgando. Así la tapa y la cara
no se unen en una línea recta.

Variantes de cara: 0 cortina de frondas, 1 tronco de Lepidodendron, 2 tronco fino con raíces colgando, 3 musgo y helecho.
En la tierra quemada: 0 ramas, 1 tronco carbonizado de pie, 2 tronco partido con la punta encendida, 3 tronco caído al pie.

## Solapas de espesura (`deco/`, 16×16, capa encima del suelo, sin colisión)

`Spill_Top_0…3` (bajo la cara de arriba), `Spill_Left_0…3` (junto a la pared izquierda; en la derecha, *Flip X*),
`Spill_Bottom_0…2` (asoman desde la pared de abajo). Ponerlas en más o menos la mitad de las celdas de suelo pegadas
a la pared: así el borde de la sala deja de ser una línea recta. Solo tocan el borde del lado de la pared.

## Lodo profundo (`mud/`), como las fosas del piso 2

| Archivo | Uso |
|---|---|
| `Mud_<máscara>` | 16 tiles; la máscara suma los vecinos que **no** son lodo: N=1, E=2, S=4, O=8. La orilla de arriba hace sombra y la de abajo coge luz |
| `Mud_Corner_{NE,NO,SE,SO}` | Capa encima para las esquinas de dentro (mismo orden que `pitCorners`) |
| `Mud_Bubbles` | 6 cuadros de 16×16, ~140 ms: burbujas que suben y revientan con una onda; en algunas celdas de lodo |
| `Mud_Tileset` | Los 16 en 4×4 (índice = máscara), para revisar |

## Arco y puertas de raíces

| Archivo | Cuadros | Celda | Uso |
|---|---|---|---|
| `Doorway_Top` | 1 | 32×32 | Arco en la cara de arriba (2×2 celdas), enmarcado por dos raíces gruesas. Siempre visible |
| `Door_Top_Close` / `_Open` | 7 / 6 | 32×32 | Encima del arco |
| `Door_Side_Close` / `_Open` | 7 / 6 | 16×32 | Pared izquierda vista desde arriba; la derecha con *Flip X* |
| `Door_Bottom_Close` / `_Open` | 7 / 6 | 32×16 | Pared de abajo |

Como las losas del piso 2 (`DoorAnimation`): **cerrar** = las raíces crecen desde los lados y se trenzan; cuadro 5
golpe con hojas o ceniza que saltan y se enciende el nudo; el último es la puerta cerrada. Duraciones
`70, 60, 60, 60, 60, 110, 140` ms. **Abrir** = el nudo brilla, destello blanco y tiemblan, las raíces se secan y se
retiran; el último está vacío. `130, 90, 60, 60, 60, 60` ms. El nudo es musgo que brilla (pantano), resina (humeante)
o una ascua (quemada).

## Maleza quemable (paso oculto que se abre con fuego)

| Archivo | Cuadros | Celda | Uso |
|---|---|---|---|
| `Brush_Top_Idle` / `_Burn` / `_Gone` | 1 / 8 / 1 | 32×32 | En la cara de arriba, en lugar de 2×2 celdas de cara |
| `Brush_Side_*` | 1 / 8 / 1 | 16×32 | Pared izquierda vista desde arriba (derecha con *Flip X*) |
| `Brush_Bottom_*` | 1 / 8 / 1 | 32×16 | Pared de abajo |

Maraña de tallos secos claros (más clara que la pared, para que se vea que es distinta) en un hueco de borde
irregular, con un paso oscuro detrás. Cada sprite lleva alrededor la textura de la pared de al lado (cara con la copa
que cuelga, o borde de la espesura), sombra en el filo del hueco, tallos que se salen un poco y frondas de la pared
que lo tapan, para que no parezca un rectángulo pegado.
`_Burn`: el fuego prende por el lado del suelo, avanza y carboniza los tallos (cuadros 0-4), se deshace (5-6) y
queda ceniza con ascuas (7). `90, 90, 90, 90, 100, 110, 120, 140` ms. **Cuadro que cuenta: el 5** (desde ahí ya no
bloquea). `_Gone` es el paso abierto para quedarse.

## Tanda 1b: plantas, charcos, obstáculos y animaciones (`<Tema>/props/`)

`generar_escenario_piso3.py` (después de `generar_tilesets_piso3.py`, porque la escena usa sus tiles). Revisar
`vista_previa_escena.png` (una escena por tramo) y `<Tema>/gif/Escena.gif`; cada pieza animada tiene su GIF.
Hojas horizontales; **pivote: el pie, en el centro de abajo** (los sprites llevan margen y sombra en el suelo).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fern_{Leafy,Sparse,Young,Bent,Tree}_Sway` | 4 | 84×50, 92×50, 44×34, 88×64, 84×74 | 220 | Helechos altos (frondoso, ralo, joven con báculos, inclinado, arborescente). Pie a 45, 45, 29, 45 y 69 px de arriba. Desfasar el cuadro inicial entre plantas. En la tierra quemada, carbonizados y casi tiesos |
| `Fern_*_Rustle` | 6 | igual | 70 | Se atraviesan: al pasar el jugador (o un enemigo), sacudida que se calma y vuelve a `Sway`. Dibujada empujando hacia la derecha; si llega por la derecha, *Flip X*. En humeante caen hojitas secas; en la quemada, ceniza |
| `Fern_*_Burn` / `Fern_*_Burnt` | 4 / 1 | igual | 110 | Solo pantano y humeante: ardiendo (bucle) y lo que queda |
| `Puddle_{Sludge,Water}_{Round,Long,Lobed,Chain,Island,Splash,Kidney}` | 4 | según la forma (34×22 … 62×26) | 260 / 220 | Charcos: lodo que frena (brillo y una burbuja) y agua de charca (reflejos que se deslizan; la Piel anfibia carga aquí). En la quemada, con película de ceniza |
| `Obstacle_Rock`, `Obstacle_Stump` | 1 | 24×18, 24×22 | | Bloquean 1×1 |
| `Obstacle_Sigillaria`, `Obstacle_BrokenTrunk`, `Obstacle_FallenLog` | 1 | 48×66, 40×46, 48×30 | | Bloquean 2×2 (la colisión es la base). Sigillaria joven con penacho; tronco partido de Lepidodendron; tronco caído |
| `Methane_Sac_Idle` | 4 | 24×26 | 260 | Odre de metano que respira (explota con cualquier ataque: efecto en la tanda 5). Luz 2D cian `#6ad0ec`, radio 1 u, 0,3 |
| `Deco_DripMoss` | 6 | 16×48 | 150 | Musgo que gotea: sobre las dos filas de la cara (arriba del todo); la gota cae a la primera fila de suelo. No en la quemada |
| `Vent_Idle` / `Vent_Warn` / `Vent_Blast` | 4 / 4 / 6 | 16×32, 16×32, 16×64 | 260 / 120 / 90 | Fumarola: la boca ocupa los 16 px de abajo. Reposo con un hilo de vapor; aviso (brilla y burbujea); columna de vapor. **Cuadros que cuentan del chorro: 1 a 3** (dañan) |
| `Deco_Embers` | 4 | 16×16 | 300 | Ascuas que parpadean (solo quemada) |
| `Fx_StumpSmoke` | 6 | 16×32 | 160 | Humo que sube de un tocón quemado (encima del tocón; solo quemada) |
| `Methane_Mist` | 8 | 64×32 | 140 | Metano a ras de suelo, translúcido (niebla que no se confunde con agua) |
| `Methane_Bubble` | 6 | 16×24 | 130 | Burbuja de metano que sube del lodo y suelta una voluta (sala del Burbujeo) |
| `Critter_Beetle_Idle` / `_Run` | 2 / 4 | 10×10 | 400 / 70 | Escarabajo decorativo que huye del jugador |
| `Critter_Gnats` | 4 | 16×16 | 120 | Mosquitos sobre el agua (no en la quemada) |
| `Deco_Ground` | 6 | 16×16 | | Detalles del suelo (variantes, no animación) para `decorations` del tema |

## Tanda 1c: piezas de sala y teletransportes

`generar_piezas_piso3.py` (usa funciones de `generar_tilesets_piso3.py`). Revisar `vista_previa_piezas_1c.png` y
`pieces/gif/`. Lo común va en `pieces/`; lo que cambia por tramo, en `<Tema>/pieces/`. Pivote: el pie, centro de abajo.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Cone_Small_Idle` / `_Open` / `_Opened` | 4 / 6 / 1 | 18×26 | 220 / 90 | Cofre pequeño: estróbilo de Lepidodendron (piña de escamas pardas con las puntas ámbar). Al abrirse las filas se separan, brilla la biomasa y salen tres bolas. **Cuadro que cuenta: 4** (suelta la biomasa) |
| `Cone_Big_*` | 4 / 6 / 1 | 32×42 | igual | Cofre grande (Tocón hueco y premios mayores), 2×2 |
| `<Tema>/pieces/Bridge_{H,V}_{Start,Mid,End}` | 1 / 2 (variantes) / 1 | 16×16 | | Tronco puente sobre el lodo profundo (se anda por encima, no hunde). Horizontal: pie con raíces a la izquierda, copa rota a la derecha (*Flip X* para el otro sentido); vertical: pie arriba. Fondo transparente con la sombra del tronco sobre el lodo |
| `Flower_Purple_Idle` / `_Pick` | 4 / 6 | 16×24 | 220 / 80 | Flores moradas del camino secreto: se mecen con un halo que late; al cogerlas estallan en chispas moradas |
| `<Tema>/pieces/RootWall_Locked` / `_Idle` | 1 / 4 | 128×72 | — / 200 | **Muro de raíces de la sala secreta:** toda la pared de arriba de la antesala (8 casillas de ancho, 4 de alto, más medio tile de pies sobre el suelo). Dos troncos enormes lo enmarcan; las raíces convergen en un nudo con el hueco del corazón (centro del hueco en x 64, y 30). `Locked`: apagado (también si nunca cogiste el corazón: se ve, como pista); `Idle`: el hueco late cuando llevas el corazón o la semilla |
| `RootWall_InsertHeart` / `_InsertSeed` | 5 / 6 | 128×72 | 110 | Poner el corazón morado; o la semilla (camino de las 3 flores), que brota en el hueco y se vuelve corazón |
| `RootWall_Open` / `_Opened` | 10 / 1 | 128×72 | `110, 110, 110, 160, 100, 100, 100, 100, 100, 200` | Las vetas moradas corren desde el nudo (0-3), el nudo se parte y las raíces se retiran a los lados como un telón mientras el corazón flota y sube (4-9). **Cuadro que cuenta: 8** (desde ahí el centro, unas 4 casillas, ya no bloquea). `Opened` para quedarse |
| `HeartPod_Stored` | 5 | 32×48 | | Vaina-almacén de la tienda: el cuadro *n* muestra *n* medios corazones guardados (0 a 4, piso 3). El generador acepta más huecos para los pisos siguientes (`pod_set(6)`…) |
| `HeartPod_Receive` | 4 | 32×48 | 110 | Late al recibir un corazón (luego se cambia al cuadro de `Stored` que toque) |
| `AmberBlock_Idle` / `_Break` | 4 / 6 | 40×44 | 200 / 90 | Bloque de ámbar con una cucaracha dentro (sala del ámbar, guarda las tres especiales). Un brillo lo cruza; al romperse se agrieta y salta en esquirlas. **Cuadro que cuenta: 3** (aparecen las especiales) |
| `Teleport_Floor{1,2,3}_Dormant` / `_Wake` / `_Idle` / `_Warp` | 1 / 6 / 6 / 6 | 32×32 (piso 3: 32×40) | — / 110 / 130 / 80 | Teletransportes: burbuja de membrana (piso 1), remolino en un anillo de piedras (piso 2), tronco hueco con raíces (piso 3). Misma luz aguamarina en los tres para que se reconozcan. `Dormant` sin activar; `Wake` al entrar por primera vez; `Idle` en bucle; `Warp` al usarlo (**cuadro que cuenta: 2**, el destello) |

Quedan para otras tandas: la cucaracha que roba el corazón (tanda 3), las explosiones del odre y los helechos
ardiendo de verdad (tanda 5), el pedestal dorado de la reliquia (tanda 6), la semilla morada del HUD y la marca de
"regreso" en el mapa (tanda 7).
