Necesito un sprite más para la **tienda** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). Es la **señal de "no disponible"** que flota sobre un pedestal vacío. Te adjunto `generar_tienda.py` y su vista previa `vista_previa_tienda.png` (y los scripts que carga: `generar_mapa.py`, `generar_menus.py`, `generar_ui.py`, `generar_mutaciones.py`, `generar_jugador.py`, `generar_escenario.py`) para que encaje con el estilo, las paletas y el contorno que ya existen.

Hazlo con un script de Python + Pillow (`generar_no_disponible.py`) que **reutilice las funciones y paletas de `generar_tienda.py`** (`Cv`, contorno oscuro, rampas de 4 tonos, el vidrio, el pedestal).

## Para qué sirve

En la tienda hay 4 pedestales con un artículo cada uno. Algunos pedestales venden una mutación o la mejora de nivel de una mutación. Si el jugador lleva esa mutación al **nivel máximo (III)**, el artículo ya no se puede comprar nunca más: desaparece del pedestal y en su lugar flota esta señal, para que se entienda de un vistazo que ese pedestal está **cerrado para siempre**. No lleva texto: se tiene que entender solo con la forma y el color.

El juego la coloca donde estaba el artículo (flotando sobre el pedestal, centrada en x) y la sube y baja 1 px como al resto de artículos. Bajo ella se ve la sombra de siempre (`Item_Mutation_Shadow`), así que no la dibujes.

## Qué dibujar

**Versión principal:** un **círculo rojo con una barra diagonal** (estilo señal de prohibido), de arriba a la izquierda hacia abajo a la derecha. Que parezca hecha del mundo del juego: una burbuja o lente de vidrio rojizo, o una membrana orgánica, con un pequeño brillo arriba a la izquierda, no una señal de tráfico plana.

**Alternativa (para que yo elija):** un **letrerito con una X roja**, como una etiqueta de laboratorio del siglo XIX (pergamino, concha o madera de naufragio) clavada en un palito, con la X pintada encima.

## Escala y estilo

- **16 píxeles por unidad**, igual que el juego. La célula del jugador mide 16×16; el pedestal mide 20×16; los artículos que sustituye miden de 12×12 a 24×24.
- Filtro nítido (sin antialiasing), fondo transparente, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color. Rojo legible sobre el suelo `#16352e` y sobre el pedestal (tonos apagados), pero sin chillar más que los artículos que siguen a la venta: que se lea "cerrado", no "peligro".
- **Sin texto** dentro del sprite.

## Hojas que necesito

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Shop_Unavailable.png` | 4 (bucle) | 16×16 | Versión principal (círculo con barra). Centro (8, 8). La señal ocupa unos 12×12 o 14×14 centrados. Animación sutil: un brillo que recorre el vidrio o un leve pulso del rojo; el contorno no cambia de tamaño. 150 ms por cuadro |
| `Shop_Unavailable_Sign.png` | 4 (bucle) | 16×16 | Alternativa (letrero con X). Pivote abajo en el centro (8, 15). Animación sutil: el letrero se balancea 1 px como mecido por el agua. 150 ms por cuadro |

Hojas en fila, cuadro 0 a la izquierda.

## Entregables

Guárdalo todo en la carpeta **`docs/multimedia-temp/shop-unavailable/`** de mi proyecto (la misma donde está este PROMPT.md):

1. `Shop_Unavailable.png` y `Shop_Unavailable_Sign.png`.
2. `generar_no_disponible.py`.
3. `vista_previa_no_disponible.png`: a ×4 sobre el fondo `#16352e`, con un pedestal (`Shop_Pedestal`) en tres estados uno al lado del otro: con un artículo a la venta, vacío con la señal principal y vacío con la alternativa (cada señal con su sombra debajo).
4. Un GIF de cada animación en `gif/`.
5. Un `README.md` corto con la tabla de hojas (cuadros, lienzo, punto central y duración por cuadro) y la paleta en hexadecimal.
