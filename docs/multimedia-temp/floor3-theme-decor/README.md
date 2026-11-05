# Decorado de las salas con tema (piso 3)

Solo decorado (no cambia el combate) para las 5 salas con tema del paso 6. Una carpeta por tramo del incendio
(`Swamp/`, `Smoke/`, `Burnt/`). Aprobado por la usuaria el 2026-11-04.

Regenerar: `python generar_decorado_temas.py [nest|loom|pond|meadow|sky]` (sin argumentos, todos).
Importar como el resto del escenario del piso 3: *Multiple*, 16 PPU, *Point*, sin compresión.
**Pivote: el pie, en el centro de abajo** (salvo decals, efectos y tiles: centro). Hojas horizontales.

**Fuego:** lo que arde tiene `_Idle` / `_Burn` (4 cuadros en bucle, 110 ms) / `_Burnt`, como los helechos. En la
tierra quemada solo hay `_Idle`, que ya es la versión quemada.

**Reglas de colocación (para el código):** las piezas salen **algo alejadas de las paredes** y **nunca sobre lodo
profundo, charcos de lodo ni de agua**. La Charca de sapos tiene **siempre una gran charca de agua** (con `Water_*`).

| Tema | Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|---|
| Nido de bombarderos | `Nest_{Big,Small,Long}_*` | 1 / 4 / 1 | 48×34, 36×26, 58×30 | 110 | Montículo con galerías y élitros; arde la hojarasca de encima |
| | `LeafPile_{A,B,C}_*` | 1 / 4 / 1 | 30×22, 24×20, 40×22 | 110 | Pilas de hojarasca |
| | `Deco_BeetleScorch` | 4 variantes | 34×22 | | Manchas de chorro quemado (decal, sin sombra) |
| Telar de la araña | `SilkWrap_{Fallen,Trunk,Stump}_*` | 1 / 4 / 1 | 60×38, 52×54, 32×28 | 110 | Troncos del piso envueltos en seda crema con hilos al suelo (la seda del decorado nunca es una tela plana: esas son las que frenan) |
| | `Cocoon_{Small,Medium,Large}_*` | 1 / 4 / 1 | 20×16, 26×18, 32×22 | 110 | Presas envueltas |
| | `EggSac_{A,B,C}_*` | 1 / 4 / 1 | 18×20, 24×20, 30×22 | 110 | Sacos de huevos (pelusa translúcida) |
| Charca de sapos | `Water_<0-15>`, `Water_Corner_{NE,NO,SE,SO}` | 1 | 16×16 | | Tiles de la gran charca: misma máscara y orden que `Mud_*` (vecinos que **no** son agua: N=1, E=2, S=4, O=8); agua poco profunda que se camina |
| | `Water_Shimmer` | 6 | 16×16 | 160 | Capa encima de algunas celdas de agua (reflejos y onda) |
| | `ShoreReeds_{A,B,C}_*` | 4 / 4 / 1 | 24×26, 18×22, 32×24 | 220 / 110 | Matas de orilla (se mecen); pegadas por fuera al borde del agua |
| | `Deco_ToadSpawn`, `Deco_WetMud` | 3 variantes | 34×18, 38×22 | | Puestas de huevos y barro con huellas (decals de orilla) |
| | `TadpolePool` | 6 | 24×16 | 150 | Charquito de una casilla con renacuajos |
| | `MossRock_{A,B,C}` | 1 | 18×16, 26×20, 28×18 | | Piedras musgosas |
| Pradera de Calamites | `CalamitesClump_{A,B,C}_*` | 4 / 4 / 1 | 34×46, 22×32, 38×38 | 220 / 110 | Cañaverales de Calamites jóvenes (distintos de la enemiga: sin cara, tallo fino, verde apagado) |
| | `Deco_HorsetailGrass` | 4 variantes | 36×22 | | Hierba de equisetos (decal) |
| | `Deco_FallenCones` | 3 variantes | 20×12 | | Estróbilos caídos |
| | `Fx_SporeDrift` | 8 | 24×24 | 140 | Esporas que flotan (no en la quemada) |
| Cielo de libélulas | `Fx_LightShaft_{A,B}` | 6 | 52×112, 72×96 | 180 | Rayos de luz del dosel, translúcidos (mejor con mezcla aditiva), por encima de todo; la base del rayo cae abajo a la derecha del lienzo, sobre su pozo |
| | `Deco_LightPool_{A,B}` | 6 | 48×18, 64×24 | 180 | Pozo de luz en el suelo (respira con el rayo) |
| | `Perch_{A,B,C}_*` | 4 / 4 / 1 | 28×44, 30×40, 26×30 | 200 / 110 | Cañas secas con libélulas pequeñas posadas (sacuden las alas); al arder se van |
| | `Fx_DragonflyShadow` | 4 | 56×40 | 90 | Sombra de libélula gigante que el código cruza por el suelo (mirando a la derecha; *Flip X* para el otro lado) |

`maqueta_<tema>_<tramo>.png` y `gif/escena_<tema>_<tramo>.gif`: escenas de revisión (no respetan las reglas de
colocación). `vista_previa_<tema>.png`: todas las hojas del tema.
