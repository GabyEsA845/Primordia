Necesito el arte de la **tienda** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). El jugador es una célula verde que muta. Te adjunto `generar_jugador.py`, `generar_mutaciones.py`, `generar_ui.py`, `generar_escenario.py` y `generar_mapa.py` con sus vistas previas (`vista_previa.png`, `vista_previa_mutaciones.png`, `vista_previa_ui.png`, `Sala_muestra_abierta_x3.png`, `vista_previa_mapa.png`) para que todo encaje con el estilo, las paletas y el contorno que ya existen.

Hazlo con un script de Python + Pillow (`generar_tienda.py`) que **reutilice las funciones y paletas de esos scripts** (`Canvas`, contorno oscuro, rampas de color, colores de linaje, la burbuja de mutación, los corazones del HUD, la biomasa).

## Cómo funciona la tienda en el juego

- Hay **una tienda por piso**: una sala lateral sin salida, sin enemigos, con el suelo y las paredes normales del piso 1 (el escenario ya existe, no lo hagas).
- Al fondo de la sala (arriba) está el **tendero: Charles Darwin**, en versión diminuta, detrás de su mostrador. Por ahora es **solo decoración**: está en reposo y hace una animación de alegría cada vez que el jugador compra algo.
- Delante, en fila, hay **4 pedestales** con un artículo cada uno:
  1. **Mutación** al azar (la burbuja de linaje que ya existe, `Item_Mutation_<Linaje>_Idle`, con el icono de 16×16 encima). Hay 2 pedestales de mutación.
  2. **Mejora de nivel** de una mutación que el jugador ya lleva: el juego pone encima el icono de esa mutación (16×16) y, debajo, el texto "I → II".
  3. **Curación** (medio corazón). Si el jugador tiene toda la vida, en su lugar se vende un **corazón extra** (un corazón más de vida máxima).
- Bajo cada artículo, el juego escribe el **precio** con su fuente pixel (Silkscreen a 8 px) y el icono `UI_Biomass` (12×12) que ya existe. Si al jugador no le llega, el precio se pone rojo y el artículo tiembla (lo hace el juego).
- Cuando el jugador se acerca lo bastante a un artículo, aparece **encima del artículo** el botón de la tecla **E**. Al pulsarla compra: el artículo desaparece con un efecto, el pedestal queda vacío y Darwin celebra.

## Escala y estilo

- **16 píxeles por unidad**, igual que el juego. La célula del jugador mide 16×16 y una casilla del suelo también.
- Filtro nítido (sin antialiasing), fondo transparente, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color.
- Vista cenital en 3/4, como los obstáculos del escenario (`Obstacle_Rock`, `Obstacle_Colony`): se ve la cara de arriba y un poco del frente.
- Aspecto orgánico y acuático, de laboratorio de biología del siglo XIX bajo el agua (conchas, coral, esponja, frascos de especímenes, vidrio). Nada de metal moderno ni de fantasía medieval.
- **Sin texto** dentro de los sprites (excepto la letra E del botón).
- Fondo de referencia para las vistas previas: verde azulado oscuro `#16352e` (el suelo del piso 1).

## 1. Darwin, el tendero

Una figurita **caricaturesca y simpática** de Charles Darwin ya mayor, reconocible al instante a ese tamaño: calvo en la coronilla, gran **barba blanca**, cejas pobladas, levita oscura, quizá con una libreta o una lupa en la mano. Proporciones "chibi" (cabeza grande) para que encaje con la célula y los enemigos. Como está bajo el agua, puede soltar de vez en cuando **burbujitas** que suben de la barba (o llevar una escafandra de vidrio; elige lo que quede mejor y más legible).

Mira hacia abajo (hacia el jugador). Va **detrás del mostrador**, así que la parte de abajo del cuerpo puede quedar tapada, pero dibújalo entero por si lo movemos.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Shop_Darwin_Idle.png` | 6 (bucle) | 24×32 | Respira, parpadea una vez y se acaricia la barba o suelta una burbuja. Pies en la fila de abajo, centrado en x |
| `Shop_Darwin_Buy.png` | 6 | 24×32 | Celebra la venta: se levanta el sombrero o alza la libreta, sonríe, salta un poquito, y vuelve a la postura de reposo en el último cuadro (para enlazar con Idle) |

## 2. Mostrador y pedestales

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Shop_Counter.png` | 1 | 48×24 | Mostrador de Darwin: una tabla de concha, coral o madera de naufragio, con 2 o 3 **frascos de especímenes** encima (algo flotando dentro), quizá un libro abierto. Darwin se dibuja detrás, asomando de cintura para arriba. Es un obstáculo: el juego le pone colisión |
| `Shop_Pedestal.png` | 1 | 20×16 | Pedestal donde va cada artículo: una columna corta de coral, esponja o roca con la cara de arriba plana (más clara). El artículo flota **encima**, centrado en x, con su sombra en la cara de arriba. Que no compita con los artículos (tonos apagados) |
| `Shop_Pedestal_Glow.png` | 4 (bucle) | 20×16 | Brillo suave semitransparente que se dibuja sobre el pedestal cuando el jugador puede comprar ese artículo (está cerca). Mismo contorno que el pedestal |

## 3. Artículos nuevos

La mutación en venta usa la burbuja de linaje que ya existe. Faltan estos tres:

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Item_Upgrade_Idle.png` | 6 (bucle) | 24×24 | Mejora de nivel: una burbuja o cápsula como la de mutación, pero **dorada** (el color de nivel III de la insignia), con destellos. Igual que la burbuja de mutación, **no se desplaza** dentro del lienzo y deja el **centro (16×16) despejado** para el icono de la mutación que pone el juego |
| `Item_Heal_Idle.png` | 4 (bucle) | 12×12 | Curación (medio corazón): un glóbulo de líquido rojo con forma de corazón, de la familia de `UI_Heart_Full`, que late |
| `Item_HeartContainer_Idle.png` | 4 (bucle) | 16×16 | Corazón extra: un corazón entero más grande y vistoso, con membrana y brillo, que late. Debe verse más valioso que la curación |
| `Item_Purchase.png` | 6 | 32×32 | Efecto al comprar, para cualquier artículo: destello y chispas doradas y verde biomasa hacia arriba, que se desvanecen. Centrado |

Todos los artículos van sobre la sombra que ya existe (`Item_Mutation_Shadow`, 16×6).

## 4. Botón E

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Key_E.png` | 2 | 13×13 | Tecla con la letra **E** en pixel (cuadro 0 normal, cuadro 1 pulsada, 1 px más abajo y más oscura). Mismo estilo de membrana/vidrio que los botones y ranuras del HUD. Va en el mundo, flotando sobre el artículo; el juego la hace subir y bajar 1 px |

## 5. Icono del mapa

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Map_Shop.png` | 1 | 7×7 | Sala de la tienda en el minimapa y el mapa grande: un glóbulo de biomasa simplificado (verde amarillento) o un frasco de espécimen. Centro (3, 3). Que no se confunda con `UI_Map_Reward` (dorado) ni con el jugador (célula verde) y se lea sobre todos los colores de la paleta del mapa |

## Qué me tienes que entregar

```
shop/
  Shop_Darwin_Idle.png, Shop_Darwin_Buy.png
  Shop_Counter.png, Shop_Pedestal.png, Shop_Pedestal_Glow.png
  Item_Upgrade_Idle.png, Item_Heal_Idle.png, Item_HeartContainer_Idle.png, Item_Purchase.png
  UI_Key_E.png
  UI_Map_Shop.png
  gif/      (cada animación por separado)
  vista_previa_tienda.png
  generar_tienda.py
  README.md (tabla con cada hoja: cuadros, lienzo, punto central o de apoyo y duración recomendada por cuadro)
```

Las hojas animadas van **en fila, con el cuadro 0 a la izquierda**.

La **vista previa** (ampliada ×3) debe montar la tienda como la verá el jugador, en un lienzo de 480×270 con el suelo `#16352e` y la pared morada del piso 1 arriba:

1. **La sala:** Darwin detrás del mostrador, arriba en el centro; debajo, a unas 2 casillas, los 4 pedestales en fila separados 2 casillas (mutación Depredador con el icono de Mandíbulas, mutación Tóxica con el de Esporas, mejora dorada con el icono de Aletas y el texto "I → II" debajo, y la curación). Bajo cada uno, el precio de ejemplo (icono `UI_Biomass` + número: 15, 15, 20, 5). La célula del jugador junto al segundo pedestal, con ese pedestal brillando y la tecla E encima de su artículo.
2. **Tira de animaciones:** los cuadros de `Shop_Darwin_Idle` y `Shop_Darwin_Buy` en fila, el corazón extra sobre un pedestal (la variante con vida llena), el efecto de compra cuadro a cuadro y un pedestal vacío.
3. **El minimapa** (como en `vista_previa_mapa.png`) con una sala lateral marcada con `UI_Map_Shop`.

Comprueba en la vista previa que Darwin se reconoce a primera vista a tamaño real, que los artículos se distinguen entre sí y del suelo, que la tecla E se lee sobre el suelo y sobre los artículos, y que nada desentona con la célula, la biomasa y la interfaz existente.
