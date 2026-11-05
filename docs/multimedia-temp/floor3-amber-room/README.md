# Sala del ámbar (piso 3)

Claro bajo un Lepidodendron herido que sangra resina: el ámbar se ha apoderado de toda la sala. **Igual en los tres
tramos del incendio** (refugio que el fuego no toca). Regenerar: `python generar_sala_ambar.py`.

## Tileset propio (`Amber/`)

Mismo motor, mismas piezas y mismos nombres que los tramos del piso (`floor3-environment/generar_tilesets_piso3.py`,
tramo `Amber` con paletas de resina), así que sirve con WallRules/RoomPainter tal cual: `tiles/` (`Floor_0..5`,
`Floor_ShadowTop_*`, `Wall_*`), `deco/Spill_*`, `Doorway_Top`, `Door_{Top,Side,Bottom}_{Close,Open}`.
Diferencias: `Floor_1..5` llevan detalles de ámbar (cuentas, grieta con resina, costra, mosquito en una gota, raíz).
Las caras de la pared, limpias (la usuaria descartó los chorreones en las paredes). La sala usa este tileset en lugar del de su tramo (código, Sonnet).

## Piezas (`pieces/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Deco_BigTree` | 4 | 96×84 | 200 | Lepidodendron gigante y herido en la pared de arriba, centrado en x sobre el bloque. **Va en x múltiplo de 16 e y = 0 de la sala**: su copa es el tile `Wall_Cap` repetido y así casa con la espesura (toca el borde de arriba a propósito). El pie (y = 80) cae en el suelo |
| `Deco_ResinStream` | 6 | 28×40 | 140 | Reguero de resina del pie del árbol (y = 80) a la base del bloque; toca arriba y abajo a propósito. Si la distancia cambia, estirarlo o repetir el tramo central |
| `AmberBlock_Base` | 1 | 80×44 | | Bajo el bloque de ámbar (centrado con él): charco de resina y raíces; el bloque se apoya en su centro |
| `Fx_AmberGlow` | 8 | 96×56 | 160 | Resplandor cálido bajo el bloque, translúcido (mejor aditivo). Además, **luz 2D ámbar** `#ffba5a` en el bloque |
| `AmberChunk_{A,B,C,D,E}` | 1 | 18×20, 26×22, 16×26, 28×20, 28×18 | | Bloques de ámbar pequeños con un insecto dentro (mosquito, escarabajo, mosca, araña, hormigas); oval, piedra facetada, esquirla, roto y racimo. Decoración opcional: no hace falta que salgan todos |
| `Deco_AmberPool_{A,B,C}` | 1 | 38×22, 46×20, 40×24 | | Charcos de resina que se funden con el suelo (borde translúcido punteado; decal, pivote centro) |
| `AmberBeads_{A,B,C}` | 6 | 16×16 | 160 | Cuentas sueltas; una destella en el cuadro 3 (desfasar entre piezas) |
| `Deco_TreeWound` | 4 | 32×32 | 200 | Opcional: herida pequeña en la cara de la pared (2×2 celdas de cara); con el árbol grande quizá sobre |

Las decoraciones, algo alejadas de las paredes y nunca sobre lodo ni charcos (regla general del decorado).
`maqueta_sala_ambar.png` y `gif/escena_sala_ambar.gif`: la sala de 24×15 montada; `vista_previa_piezas.png`.
