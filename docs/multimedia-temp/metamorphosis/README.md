# Metamorfosis al pasar de piso

Generado con `generar_metamorfosis.py` (`python generar_metamorfosis.py`, necesita Pillow, numpy y scipy). Lee la célula de `../player` y los cuerpos de `../floor2-bodies`. 16 píxeles por unidad, filtro Point, sin compresión.

| Archivo | Cuadros | Tamaño | Uso |
|---|---|---|---|
| `Morph_<Cuerpo>.png` | 12 | 32×32 | Silueta blanca que pasa de la célula a la criatura (campos de distancia interpolados, cuadros repartidos por cuánto cambia la forma) |
| `Reveal_<Cuerpo>.png` | 6 | 32×32 | De la silueta blanca a los colores de la criatura (Idle_side, cuadro 0) |
| `Metamorphosis_Aura.png` | 4 | 64×64 | Halo con rayos que giran, en blanco: el juego lo tiñe del color de la familia |
| `Metamorphosis_Burst.png` | 6 | 96×96 | Estallido de la revelación, en blanco, también teñido |
| `Abyss_Background.png` | 1 | 480×272 | Fondo del abismo: degradado azul en escalones con motas |
| `Abyss_Rays.png` | 1 | 128×272 | Rayos de luz desde arriba, translúcidos |
| `Abyss_Mote.png` | 2 | 4×4 | Motas que suben (el código las mueve) |

`<Cuerpo>`: `Jellyfish` (Cnidocitos), `Placoderm` (Placas mandibulares) y `Crinoid` (Corona de brazos, añadido el 25 de octubre). `gif/` y `vista_previa_metamorfosis.png` son solo para revisar.

**Importar en Unity:** menú *Primordia > Floor transition > Crear sala de paso y escena de prueba*.
