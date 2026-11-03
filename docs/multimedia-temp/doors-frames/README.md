# Coronas de las puertas de arriba y marcos de las laterales (pisos 1-3, 10 temas)

Generado con `generar_marcos_puertas.py` (piezas) y `escenas_puertas.py` (escenas de revisión), skill `primordia-sprites`.
Todo se pinta con la **textura y los colores de los tiles de cada tema** (tapa, cara, borde y arco), así encaja con lo que
ya hay. Los temas: `Soup`, `Boss`, `Amoeba` (piso 1); `Shore`, `Shallows`, `Cambrian`, `Abyss` (piso 2, aunque `Shore` es la
sala de paso); `Swamp`, `Smoke`, `Burnt` (piso 3). Una carpeta por tema.

## Piezas de cada tema

| Hoja | Tamaño | Capa | Qué es |
|---|---|---|---|
| `Doorway_Top_Crown` | 48×24 | encima de las paredes y de `Doorway_Top_Front` | Corona sobre el arco de arriba: la cara de la pared sube en joroba y se mete en la tapa (pisos 1 y 2: venas y nódulos / dovelas, clave y algas); en el piso 3, abanico de frondas de helecho que cuelga de la espesura |
| `Doorway_Side_Back` | 40×72 | encima del suelo, **debajo** del jugador | Puerta lateral: remate pequeño al pie del hueco y sombra del puente en el suelo |
| `Doorway_Side_Front` | 40×72 | **encima** del jugador | Puerta lateral: puente con su cara sobre la parte alta del hueco y, encima, la corona que se sale hacia la sala (piso 1: pliegue de membrana con cilios; piso 2: reborde de roca, coral y cintas de alga; piso 3: mata de espesura con frondas y lianas) |
| `Wall_Side_Deco_0/1/2` | 24×16 | encima de las paredes | Adornos sueltos para la pared lateral (cilios, algas o frondas) |

Las laterales y los adornos están dibujados para la **pared izquierda**; para la derecha, *Flip X* (y el desplazamiento en x, al revés).

## Dónde va cada una (16 px = 1 u)

- **Corona de arriba:** centrada en x con el arco (`Doorway_Top`, 32×32). Su fila 16 es la de arriba del arco, así que el centro de
  la corona queda **4 px por encima de lo alto del arco**: `centro del arco + (0, 1,25 u)`.
- **Puerta lateral (Front y Back):** columnas 0-15 = la columna del tile de borde (`Wall_EdgeLeft`); 16-39 entran en la sala. Filas
  28-59 = el hueco (las 2 celdas de la puerta). Centro del sprite respecto al centro del hueco: **(+0,75 u, +0,5 u)** en la izquierda y
  **(−0,75 u, +0,5 u)** en la derecha (con *Flip X*). La hoja de la puerta (`Door_Side_*`) sigue donde está: el puente le tapa la parte
  de arriba, como un marco.
- **Adornos de pared:** columnas 0-15 sobre un tile de borde lateral, 16-23 asoman a la sala. Uno de cada 4-5 tiles de borde, al
  azar (variante al azar), nunca a menos de 1 tile de una puerta ni en las esquinas.

## Cambios de código (cuando se integre, con Sonnet)

- `FloorBuilder.BuildDoor`: poner la corona en las puertas de arriba y el par Back/Front en las laterales (los sprites, en
  `RoomTheme`, uno por tema). Las de abajo no cambian.
- `DoorwayCrossing`: hoy solo cruza las de arriba. En las laterales, `Front` va siempre por encima del jugador (está por encima del hueco
  y de su cabeza) y `Back`, por debajo.
- Adornos: al montar las paredes laterales (`WallRules`), con la semilla de la sala.

## Revisión

`vista_previa_coronas.png`, `vista_previa_laterales.png` (antes y después) y `gif/escenas/escena_<Tema>.gif` (antes a la izquierda y
después a la derecha: un jugador del piso entra por la puerta de arriba y otro sale por la lateral). Solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_marcos_puertas.py` y `escenas_puertas.py`.
