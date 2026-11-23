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
