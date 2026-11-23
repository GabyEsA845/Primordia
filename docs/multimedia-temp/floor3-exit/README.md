# Sala de paso del piso 3 al 4: el sumidero de arena

Generado con `generar_salida_piso3.py` (`"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_salida_piso3.py`;
usa `../floor3-environment/base_piso3.py` y `generar_tilesets_piso3.py`) y la skill `primordia-sprites`.
16 px por unidad, filtro Point, sin compresión. Aprobado el 2026-11-23.

La cueva de la sala de paso pasa por franjas: **tierra quemada (`Burnt`, ya existe) → franja intermedia (`BurntSand`) →
arena roja del Pérmico (`Permian`)**, con el sumidero en la última. La frontera entre franjas **no es una línea**:
ver «Mezcla entre franjas» (regla en `docs/memoria-claude/primordia-theme-transitions.md`).

Revisar: `vista_previa_sala.png` (sala de muestra), `vista_previa_sumidero.png`, `vista_previa_piezas.png`, `gif/`.

## Tilesets `Permian/` y `BurntSand/`

Mismas piezas, nombres y orden que los tramos del piso 3 (`../floor3-environment/README.md`): `tiles/` (35 tiles),
`Tileset_<Tema>.png` (8 columnas) y `deco/Spill_*` (solapas). Sin lodo, puertas ni maleza.

- **Permian:** suelo de arena rojo óxido apagado con ondas del viento (variantes: guijarros, costra agrietada, matas
  secas, montoncito); tapa = meseta de arenisca vista desde arriba (variantes: mata, piedras, grieta); caras = acantilado
  de arenisca con estratos y arena al pie (0 agujeritos, 1 grieta, 2 bloque caído, 3 chorrito de arena); solapas =
  arena amontonada contra la pared.
- **BurntSand:** paredes de tierra quemada; suelo con granos de arena sueltos y lenguas de arena apagada (Floor_1-5);
  caras 1 y 3 con arena al pie; solapas alternas de arena y de ramas quemadas.

## Mezcla entre franjas (`blend/`)

| Archivo | Va en | Uso |
|---|---|---|
| `SandEdge_<1-15>`, `SandEdge_Corner_{NE,NO,SE,SO}` | suelo `BurntSand` junto a `Permian` | La arena entra con borde ondulado y en trama |
| `DustEdge_*` | suelo `Burnt` junto a `BurntSand` | Granos de arena cada vez más ralos |
| `AshEdge_*` | suelo `Permian` junto a franjas quemadas | Ceniza y carbón que se cuelan en la arena |
| `SandCap_*` | tapa/bordes quemados junto a pared `Permian` | La meseta de arenisca avanza sobre la espesura, filo con luz |
| `Wall_Face{Upper,Lower}_BurntToSand_{0,1}` / `_SandToBurnt_{0,1}` | cara quemada con cara `Permian` a la derecha / a la izquierda | Juntura que ondula, tronco carbonizado medio enterrado |
| `Sand_Drift_0-2` (48×32, 32×24, 40×20) | suelo cerca de las fronteras | Calcomanías grandes para romper la cuadrícula |
| `<Set>_Tileset.png` | — | Las 15 máscaras + 4 esquinas en una hoja de 8 columnas |

## Sumidero y polvo

| Archivo | Cuadros | Tamaño | ms | Uso |
|---|---|---|---|---|
| `Transition_Pit_Sand.png` | 8 | 112×80 (7×5 tiles) | 167 (en bucle) | Borde de arenisca con arena encima, pared interior con estratos y 3 chorros finos de arena que caen, embudo que gira hacia el centro con luz cálida que late, polvo que sube. Mismo tamaño y pivote (centro) que la fosa y la charca |
| `Transition_Dust_Sand.png` | 4 | 8×8 | — | Como la burbuja de la charca: 0-1 el grano sube (en bucle), 2 se deshace en polvo, 3 motas (últimos 0,25 s) |

## Qué necesita el código (Sonnet, rama del piso 3)

- Menú `Primordia > Floor transition` para el piso 3: importar los dos temas, el sumidero, el polvo y `blend/`;
  `Floor_3.ConfigureTransition(...)` con Burnt → BurntSand → Permian; la fosa del piso 3 elige estas hojas.
- **Frontera con ruido:** el tema de cada celda sale de una frontera que ondula por fila (no una columna fija); la cara
  de abajo usa el tema de la de arriba.
- **Capas de mezcla** (como `Mud_<máscara>` y `pitCorners`): en cada celda, máscara de vecinos de la franja de más
  rango (o de menos, para `AshEdge`); esquinas solo si sus dos lados no están en la máscara. Caras de frontera en la
  celda quemada cuando la cara vecina es `Permian`. Calcomanías sueltas cerca de las fronteras.
- Icono del mapa: el de siempre (`../floor-transition/UI_Map_Transition.png`). El calor que ondula sobre el sumidero,
  si se quiere, por código (no está dibujado).

## Decoración: «del bosque muerto al desierto» (2026-11-23, `generar_decor_salida.py` → `decor/`)

Aprobada por subtandas (A-E). Muestra: `vista_previa_sala_decor.png` (sala amplia 60×26, ×2), recortes por franja
`vista_previa_sala_decor_{quemada,intermedia,permica}.png`, `vista_previa_sala_decor_rastro.png` (×5),
`vista_previa_decor_{A,B,C,D,E}.png`, `gif/Plant_*.gif`, `gif/Fx_SandGust*.gif`, `gif/escenas/rafagas_permica.gif`.
Todo a 16 PPU, Point. **Pivote abajo-centro** salvo las calcomanías del suelo (centro). Orden por altura (Y) como el
resto de props; las calcomanías (huellas, cola, barro, sal) van al nivel del suelo, debajo de todo.

| Pieza | Cuadros | Tamaño | ms | Franja y uso |
|---|---|---|---|---|
| (franja quemada) | | | | Lo que ya existe: `floor3-environment/Burnt/props/Obstacle_{BrokenTrunk,FallenLog,Stump}`, `Smoke/props/Fern_*_Burnt`, `Deco_Embers` |
| Deco_BuriedLog_{0,1,2} | 1 | 56×24, 44×20, 36×18 | — | Intermedia (el 2 también en el borde pérmico): tronco carbonizado frío, hundido a medias, dunas que lo cruzan. Se pisa |
| Obstacle_BuriedTrunk | 1 | 40×48 | — | Intermedia: tronco partido de pie con duna. **Colisión en la base (2×1)**, translúcido si el jugador pasa detrás |
| Deco_BuriedStump_{0,1} | 1 | 28×26, 30×24 | — | Intermedia/pérmica: tocones enterrados a medias |
| Deco_DeadFern_{0,1,2} · Deco_Charcoal_{0,1,2} | 1 | ~26×20 · ~16×12 | — | Intermedia: helechos y carbón que asoman |
| Obstacle_SandRock_{0,1} | 1 | 44×44, 40×40 | — | Pérmica: peñasco de estratos. **Colisión en la base (~2×1)**, translúcido si el jugador pasa detrás |
| Deco_Slab_{0,1,2} · Deco_Stone_{0,1,2} · Deco_Gravel_{0,1,2} | 1 | varios | — | Pérmica: lajas, piedras y cascajo; se pisan |
| Plant_{Walchia_0,Walchia_1,DryTuft_0-2,DryCalamites}_Sway | 4 | 30×36 … 20×17 | 220 | Pérmica, a la sombra de rocas y del acantilado (no junto al sumidero). Desfasar el cuadro inicial |
| Plant_*_Rustle | 6 | igual | 70 | Al pasar una ráfaga: se dobla a la derecha, suelta granos y vuelve a `Sway` (*Flip X* si viene por la izquierda) |
| Deco_Track_{Hind,Fore}_{E,NE} · Deco_TailDrag_{E,NE} | 1 | 22×22, 18×18 · 24×24 | — | Pérmica: huellas de sinápsido (pie IZQUIERDO) y surco de la cola |
| Deco_MudCracks_{0,1,2} · Deco_SaltCrust_{0,1} | 1 | varios | — | Pérmica (el barro también en la intermedia): calcomanías del suelo |
| Fx_SandGust / _Small | 6 | 64×24 / 44×16 | 80 | Ráfaga: velo bajo translúcido que el código mueve |

**Rastro (código):** de un punto de la franja pérmica (mejor si sale de detrás de un peñasco) al borde del sumidero.
Pares trasera → delantera (14 px por delante y 2 px más cerca del centro), alternando lado (±6 px del eje), zancada
26 px; la cola cada 16 px por el eje. 8 direcciones: la versión E o NE más cercana girada 90°/180°/270°; el pie
derecho = el izquierdo reflejado sobre el eje de la marcha (E: *Flip Y*; NE: trasponer).

**Ráfagas (código):** una cada 4-7 s, de izquierda a derecha a ~6 u/s, en una fila al azar; opacidad 1 en la pérmica,
0,5 en la intermedia, nada en la quemada. **No atraviesan nada:** nacen dentro de la sala junto a la pared izquierda
con fundido de opacidad y se desvanecen (~0,3 s) antes de la pared derecha; la fila se elige para no cruzar el
sumidero (si llegara, se deshace en su borde); al tocar un obstáculo con colisión (peñasco, tronco de pie) se deshace
contra él y deja ~2 casillas de «sombra de viento» detrás, luego puede renacer una `_Small`. Al solaparse con una
planta, lanzar su `Rustle`. El fundido es solo alfa (no escalar ni girar).

**Colocación (código):** cada franja con su juego de piezas (arriba); los obstáculos lejos de puertas y del sumidero
(≥2 casillas); plantas y cascajo junto a rocas y al pie de las caras; densidad baja en la quemada (ya tiene ascuas),
media en las otras dos. Semilla por sala para que no se repita.
