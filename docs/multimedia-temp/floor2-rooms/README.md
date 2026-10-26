# Salas especiales del piso 2

Tres scripts (necesitan Pillow), en este orden:

1. `generar_salas_especiales.py`: los 10 planos (`Layouts/Room_Special_*.txt`, formato de `RoomTemplate`), su comprobación (`informe.txt`) y las vistas previas con los tilesets reales (`vista_previa_<sala>.png`, `catalogo.png`).
2. `generar_fosas.py`: los tiles de fosa de cada franja, en `../environment/<Tema>/pits/`.
3. `generar_piezas.py`: las piezas nuevas de las salas, en `Pieces/` (vista previa en `vista_previa_piezas.png` y `gif/`).

Después de cambiar fosas o piezas, vuelve a lanzar el 1 para ver las salas con el arte nuevo.

## Contorno de cueva

Cada sala se dibuja primero con su estructura (fosas, islas, obstáculos, puertas) y después se le deforma el contorno: la roca entra de 0 a 2-3 tiles desde cada lado, sin picos de 1 tile y sin tocar nada delante de las puertas (quedan cuellos de 6 tiles). La semilla es fija, así que la forma es siempre la misma. Se usa la primera semilla con la que la sala sigue cumpliendo las reglas y conserva el alcance de la versión sin deformar (mismas puertas, cofres, corazón y esponjas alcanzables, y ningún hueco nuevo al que solo se llegue esquivando). En el Parkour la roca entra de 2 en 2, para no dejar fosas de 1 tile junto a las plataformas.

## Símbolos de los planos

Los de `RoomTemplate` (`#` pared, `.` suelo, `^ v < >` puertas de 2 tiles) más:

| Símbolo | Qué es | Arte |
|---|---|---|
| `~` | Fosa (bloques de al menos 2×2) | `pits/Pit_<máscara>` + esquinas |
| `o` | Roca pequeña, bloquea | `deco/Obstacle_Rock` del tema |
| `O` | Obstáculo grande del tema (2×2), bloquea | Someras: arqueociatos; Arrecife: coral grande; Abismo: aguja |
| `F` | Hueso del fósil, bloquea | `Fossil_Skeleton` (dibujado sobre las celdas `F`) |
| `S` | Esponja Vauxia (enemigo fijo) | la del enemigo |
| `C` | Cofre: almeja pequeña; `CC`, almeja grande | `Clam_Small_*`, `Clam_Big_*` |
| `H` | Coral con el corazón morado (2×2) | `HeartCoral_*` |
| `E` | Entrada de oleadas | `WaveVent_*` |

## Comprobación

Las reglas de `RoomTemplate.Validate` (con las fosas y los objetos contados como suelo para las paredes), fosas de al menos 2×2, obstáculos 2×2 completos y alcance. Cuenta lo que se llega andando desde la primera puerta y lo que solo se llega con la esquiva (3 u: salta fosas de hasta 2 tiles en recto o 1 en diagonal). Lo que solo se alcanza esquivando es a propósito: la salida y el cofre del Parkour, la isla del Anillo, el santuario del Corazón morado y la esponja del islote del Jardín.

## Fosas (`../environment/<Tema>/pits/`)

16×16, misma vista en 3/4 que las paredes: se ve la pared del fondo cayendo a la oscuridad bajo el suelo de arriba, un filo fino a los lados y el borde del suelo de abajo.

- `Pit_<máscara>`: 16 tiles. La máscara suma los vecinos que **no** son fosa (suelo, pared u objeto): N=1, E=2, S=4, O=8.
- `Pit_Corner_{NE,NO,SE,SO}`: capa encima para las esquinas de dentro (la diagonal no es fosa y los dos vecinos de ese lado sí).
- `Pit_Motes`: 6 cuadros de 16×16 en bucle, motas que suben del fondo; ponerlas en algunas celdas de fosa.
- `Pit_Tileset`: los 16 en una hoja de 4×4 (índice = máscara), para revisar.

## Piezas (`Pieces/`)

Importar como el resto: *Multiple* (las hojas), 16 PPU, *Point*, sin compresión.

| Hoja | Celda | Cuadros | Uso |
|---|---|---|---|
| `Clam_Small_Idle` / `_Open` / `_Opened` | 16×16 | 4 / 6 / 1 | Cofre pequeño (Parkour, Anillo): una vieira que respira; al abrirse muestra la biomasa y la suelta |
| `Clam_Big_Idle` / `_Open` / `_Opened` | 32×24 | 4 / 6 / 1 | Cofre grande de la Gruta escondida (ocupa las dos celdas `CC`, sobresale 8 px por arriba) |
| `HeartCoral_Idle` | 32×32 | 6 | Coral con el corazón morado flotando y latiendo |
| `HeartCoral_Taken` | 32×32 | 1 | El coral sin el corazón |
| `Pickup_HeartPurple` | 12×12 | 4 | El corazón suelto, por si se usa en otro sitio |
| `UI_Heart_Purple` | 16×16 | 1 | Icono del HUD: como el medio corazón verde del Herbívoro, en morado |
| `WaveVent_Idle` / `_Active` | 16×16 | 4 / 6 | Entrada de oleadas: grieta del suelo; activa, se abre, brilla en rojo y burbujea |
| `Shadow_Swim_S/M/L` | 12×4, 20×6, 32×8 | 1 | Sombra separada de los que nadan (Pikaia, medusa, Anomalocaris) |
| `Fossil_Skeleton` | la sala entera | 1 | Esqueleto de un pez acorazado gigante sobre las celdas `F` del Fósil gigante; origen en la esquina de arriba a la izquierda de la sala |
| `Fossil_Vertebra` / `Fossil_Rib` | 16×16 | 1 | Huesos sueltos para otras salas |

La caída a la fosa (del jugador y de los enemigos) se hará por código: encoger, oscurecer y unas burbujas. No hace falta una animación por cuerpo.
