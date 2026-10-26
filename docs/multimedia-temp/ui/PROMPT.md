Necesito los **objetos e interfaz** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). El jugador es una célula verde que muta. Te adjunto `generar_jugador.py`, `generar_mutaciones.py` y sus vistas previas (`vista_previa.png`, `vista_previa_mutaciones.png`) para que todo encaje con el estilo, las paletas y el contorno que ya existen.

Hazlo con un script de Python + Pillow (`generar_ui.py`) que **reutilice las funciones y paletas de esos dos scripts** (`Canvas`, contorno oscuro, rampas de color, colores de linaje). Para los iconos de mutación, parte de las piezas que ya dibuja `generar_mutaciones.py`, para que cada icono se reconozca al instante como la pieza que lleva el jugador.

## Escala y estilo

- La interfaz se dibuja a **la misma escala que el juego**: el lienzo de la interfaz mide 480×270 px, igual que la vista del juego (30×16,9 unidades a 16 px por unidad). Un píxel de interfaz = un píxel del juego. El juego luego lo amplía ×2 o más sin suavizado.
- Filtro nítido (sin antialiasing), fondo transparente, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color.
- Aspecto orgánico y acuático: membranas, burbujas, bordes redondeados y algo irregulares, nada de metal ni de marcos de fantasía medieval.
- **Sin texto** dentro de los sprites: los textos (nombres, descripciones, cantidades) los pone el juego con su fuente.
- Colores de linaje (los mismos que en las mutaciones):
  - **Depredador** (Mandíbulas, Aletas, Vejiga de tinta): naranjas y rojos cálidos, hueso claro.
  - **Acorazado** (Patas articuladas, Caparazón, Glándula de choque): grises azulados, brillo frío.
  - **Tóxico** (Aguijón, Esporas, Piel urticante): lima ácido y morados.
- Fondo de referencia para las vistas previas: verde azulado oscuro `#16352e` (el suelo del piso 1).

## 1. Biomasa (moneda)

Cae de los enemigos al limpiar una sala y el jugador la recoge al pasar por encima. Es materia orgánica: una gotita o un glóbulo carnoso verde amarillento con un brillo, que flota en el agua.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Item_Biomass_Idle.png` | 4 (bucle) | 10×10 | Flota arriba y abajo 1 px y palpita un poco |
| `Item_Biomass_Big_Idle.png` | 4 (bucle) | 14×14 | Versión que vale más (un racimo de 2 o 3 glóbulos) |
| `Item_Biomass_Collect.png` | 4 | 16×16 | Se aplasta y se deshace en chispitas hacia arriba |
| `UI_Biomass.png` | 1 | 12×12 | Icono para el contador del HUD (al lado del número) |

## 2. Mutación en el suelo

Objeto que el jugador toca para equipar una mutación. Es una **burbuja o huevo de membrana translúcida**, con un núcleo que late; el juego dibuja el **icono de la mutación (16×16) encima, en el centro**, así que deja el centro despejado y legible (sin detalles que compitan con el icono). Una versión por linaje, teñida con su color.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Item_Mutation_<Linaje>_Idle.png` | 6 (bucle) | 24×24 | La membrana ondula, pero la burbuja **no se desplaza** dentro del lienzo: el juego mueve juntos burbuja e icono para que floten. El centro (16×16) queda para el icono |
| `Item_Mutation_<Linaje>_Collect.png` | 5 | 32×32 | La burbuja revienta hacia fuera |
| `Item_Mutation_Shadow.png` | 1 | 16×6 | Sombra elíptica bajo el objeto, semitransparente |

`<Linaje>`: `Predator`, `Armored`, `Toxic`.

## 3. Iconos de las 9 mutaciones

Un icono por mutación en **dos tamaños**, dibujados a mano en cada tamaño (no reescalados): 16×16 para el HUD y el objeto del suelo, 32×32 para las tarjetas de elección. Cada icono muestra la pieza de la mutación sola, bien recortada y reconocible, con los colores de su linaje.

| Mutación | Ranura | Idea del icono |
|----------|--------|----------------|
| Mandíbulas (`Mandibles`) | Boca | Las dos pinzas de hueso abiertas |
| Aguijón (`Stinger`) | Boca | La púa lima en diagonal con la gota morada |
| Esporas (`Spores`) | Boca | El racimo de 3 sacos morados |
| Aletas (`Fins`) | Extremidades | Una aleta naranja abierta |
| Patas articuladas (`JointedLegs`) | Extremidades | Dos o tres patas segmentadas grises |
| Caparazón (`Carapace`) | Piel | Las placas segmentadas del caparazón |
| Piel urticante (`StingingSkin`) | Piel | Trozo de membrana oscura con púas lima y moradas |
| Vejiga de tinta (`InkSac`) | Órgano | La bolsa de tinta con una gota saliendo |
| Glándula de choque (`ShockGland`) | Órgano | El núcleo eléctrico azul con chispas |

Nombres: `Icon_<Mutación>_16.png` y `Icon_<Mutación>_32.png`.

## 4. Iconos de linaje

Símbolo de cada linaje para las tarjetas y la lista de mutaciones de la pausa. 12×12, una sola pieza, legible sobre el fondo de la tarjeta.

| Linaje | Idea |
|--------|------|
| Depredador (`Predator`) | Colmillo o mandíbula, naranja y hueso |
| Acorazado (`Armored`) | Escudo de placas, gris azulado |
| Tóxico (`Toxic`) | Gota o espora, lima y morado |

Nombres: `Icon_Lineage_Predator.png`, `Icon_Lineage_Armored.png`, `Icon_Lineage_Toxic.png`.

## 5. Corazones (vida)

El jugador empieza con 3 corazones (4 como máximo, con el Caparazón); cada golpe quita medio o uno entero. Corazones con aspecto orgánico (un corazón-célula rojo, con brillo y contorno), 16×16 cada uno, alineados en fila en la esquina superior izquierda.

| Hoja | Cuadros | Lienzo |
|------|---------|--------|
| `UI_Heart_Full.png` | 1 | 16×16 |
| `UI_Heart_Half.png` | 1 | 16×16 (mitad izquierda llena, derecha vacía) |
| `UI_Heart_Empty.png` | 1 | 16×16 (solo el contorno y un fondo oscuro translúcido) |

## 6. Mira del ratón

Sustituye al cursor del sistema. El juego la dibuja como imagen de la interfaz que sigue al ratón (no con el cursor del sistema), así que se amplía igual que el resto. Debe verse bien sobre el suelo oscuro, sobre las paredes y sobre los enemigos (contorno oscuro y un color claro, por ejemplo lima pálido). Forma orgánica: un anillo de membrana con cuatro marcas o una diana tipo célula.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Crosshair.png` | 1 | 15×15 | El centro exacto es el píxel (7, 7) |
| `UI_Crosshair_Fire.png` | 2 | 15×15 | Se contrae un instante al disparar |

## 7. Ranuras del cuerpo (HUD)

Fila de 4 ranuras en el HUD (bajo los corazones) que muestran qué mutación lleva el jugador en cada parte del cuerpo; el juego pone dentro el icono de 16×16.

| Hoja | Lienzo | Notas |
|------|--------|-------|
| `UI_Slot_Frame.png` | 20×20 | Marco de membrana para una ranura, con 2 px de margen alrededor del icono |
| `UI_Slot_Mouth_Empty.png` | 16×16 | Silueta apagada de una boca, para cuando la ranura está vacía |
| `UI_Slot_Limbs_Empty.png` | 16×16 | Silueta apagada de una extremidad |
| `UI_Slot_Skin_Empty.png` | 16×16 | Silueta apagada de una piel o escama |
| `UI_Slot_Organ_Empty.png` | 16×16 | Silueta apagada de un órgano |

## 8. Indicador de recarga del órgano

Barrita bajo el jugador (en el mundo, no en el HUD) que se llena mientras se recarga la habilidad del clic derecho.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Cooldown_Back.png` | 1 | 18×4 | Fondo con contorno; el relleno va dentro, en 16×2 |
| `UI_Cooldown_Fill.png` | 1 | 16×2 | Relleno azul claro; el juego lo recorta de izquierda a derecha |
| `UI_Cooldown_Ready.png` | 3 | 18×4 | Destello cuando vuelve a estar lista |

## 9. Tarjetas "elige 1 de 3" y paneles

Al limpiar ciertas salas el juego se pausa y muestra 3 tarjetas de mutación en fila. Cada tarjeta mide **130×150 px** y lleva (de arriba abajo, puesto por el juego): franja del color del linaje (`UI_Card_Stripe`, teñida), icono de 32×32, nombre, ranura y linaje con su icono, descripción y "Reemplaza: …". Debajo hay un botón "Saltar". El mismo panel sirve para la pausa.

Todos son **9-slice** (se estiran sin deformar las esquinas): indica en el README el borde de cada uno en píxeles.

| Hoja | Lienzo | Notas |
|------|--------|-------|
| `UI_Card_Stripe.png` | 16×6 | Franja de membrana **blanca** (con sombreado en grises) que el juego tiñe con el color del linaje; 9-slice en horizontal |
| `UI_Card_Normal.png` | 48×48 | Fondo de tarjeta: membrana oscura translúcida con borde orgánico; centro liso para el texto |
| `UI_Card_Highlighted.png` | 48×48 | Ratón encima: borde más claro y brillante |
| `UI_Card_Pressed.png` | 48×48 | Pulsada: un poco más oscura y hundida |
| `UI_Card_Disabled.png` | 48×48 | Atenuada (el juego bloquea las tarjetas medio segundo al aparecer) |
| `UI_Panel.png` | 48×48 | Fondo del panel de elección y de la pausa |
| `UI_Button_Normal.png` / `_Highlighted.png` / `_Pressed.png` | 32×16 | Botones como "Saltar", "Continuar" o "Menú" |

## Qué me tienes que entregar

```
ui/
  Items/    (Item_Biomass_…, Item_Mutation_…)
  Icons/    (Icon_<Mutación>_16/32, Icon_Lineage_…)
  HUD/      (UI_Heart_…, UI_Crosshair…, UI_Slot_…, UI_Cooldown_…, UI_Biomass)
  Panels/   (UI_Card_…, UI_Card_Stripe, UI_Panel, UI_Button_…)
  gif/      (cada animación por separado)
  vista_previa_ui.png
  generar_ui.py
  README.md (tabla con cada hoja: cuadros, lienzo, punto central o borde 9-slice, y duración recomendada por cuadro)
```

La **vista previa** (ampliada ×3, sobre `#16352e`) debe montar el resultado como lo verá el jugador:
1. El HUD en un lienzo de 480×270: corazones (4, con Caparazón: 2 llenos, 1 medio y 1 vacío), las 4 ranuras con 2 iconos y 2 vacías, el contador de biomasa y la mira sobre el suelo.
2. La pantalla de elección: panel, 3 tarjetas de 130×150 (una por linaje, con su icono de 32×32 y el de linaje), una de ellas resaltada, y el botón "Saltar".
3. En el suelo: biomasa pequeña y grande, y una mutación de cada linaje con su icono de 16×16 encima, junto a la célula para comparar tamaños, y la barra de recarga bajo la célula.

Comprueba en la vista previa que los iconos se distinguen entre sí a 16×16, que la mira se ve sobre todos los fondos y que nada desentona con la célula y sus mutaciones.
