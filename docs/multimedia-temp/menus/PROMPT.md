Necesito el arte del **menú de pausa** y del **menú principal** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). El jugador es una célula verde que muta. Te adjunto `generar_jugador.py`, `generar_mutaciones.py`, `generar_ui.py` y sus vistas previas (`vista_previa.png`, `vista_previa_mutaciones.png`, `vista_previa_ui.png`) para que todo encaje con el estilo, las paletas, el contorno y la interfaz que ya existen.

Hazlo con un script de Python + Pillow (`generar_menus.py`) que **reutilice las funciones y paletas de esos tres scripts** (`Canvas`, contorno oscuro, rampas de color, colores de linaje, el panel y los botones 9-slice de `generar_ui.py`). Lo nuevo debe parecer de la misma familia que `UI_Panel`, `UI_Card_*`, `UI_Button_*` y `UI_Slot_Frame`.

## Escala y estilo

- La interfaz se dibuja a **la misma escala que el juego**: el lienzo mide **480×270 px**. Un píxel de interfaz = un píxel del juego. El juego luego lo amplía ×2 o más sin suavizado.
- Filtro nítido (sin antialiasing), fondo transparente salvo en los fondos de pantalla, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color.
- Aspecto orgánico y acuático, de laboratorio de biología: membranas, burbujas, vidrio de microscopio, bordes redondeados y algo irregulares. Nada de metal ni de marcos de fantasía medieval.
- **Sin texto** dentro de los sprites (títulos, nombres, cifras y descripciones los pone el juego con su fuente). **Única excepción: el logo del menú principal.**
- Colores de linaje (los mismos que en las mutaciones):
  - **Depredador** (Mandíbulas, Aletas, Vejiga de tinta): naranjas y rojos cálidos, hueso claro.
  - **Acorazado** (Patas articuladas, Caparazón, Glándula de choque): grises azulados, brillo frío.
  - **Tóxico** (Aguijón, Esporas, Piel urticante): lima ácido y morados.
- Fondo de referencia para las vistas previas: verde azulado oscuro `#16352e` (el suelo del piso 1).

---

# Parte A · Menú de pausa

## Cómo es la pantalla

El juego oscurece la partida (negro al 75 %) y monta encima tres zonas, de izquierda a derecha, dentro del lienzo de 480×270:

```
+-------------------------------------------------------------------------------+
|                                   PAUSA                                       |
|  +--------------+            +-----------+       +--------------------------+ |
|  | ESTADÍSTICAS |            | Continuar |       | [Boca]           [Piel]  | |
|  | ♥  Vida  6/8 |            | Reiniciar |       |    \   .------.   /      | |
|  | ✶  Daño   2  |            |   Menú    |       |     o-(  célula )-o      | |
|  | »  Vel.  7   |            +-----------+       |    /   '------'   \      | |
|  |              |                                | [Extr.]          [Órgano]| |
|  +--------------+                                +--------------------------+ |
|  Semilla: 123456                                                              |
+-------------------------------------------------------------------------------+
```

1. **Izquierda: estadísticas** (unos 110×100 px). Panel con una fila por estadística: icono de 12×12, nombre y valor (texto del juego). Ahora: **Vida, Daño y Velocidad**; más adelante habrá más (cadencia, recarga de esquiva, alcance…), así que el panel crece hacia abajo.
2. **Centro: título "PAUSA" y botones** Continuar, Reiniciar y Menú principal (los botones ya existen: `UI_Button_*`, 32×16 en 9-slice).
3. **Derecha: la criatura** (unos 170×190 px). En el centro, una **lente de microscopio** con la célula del jugador dentro, **ampliada ×3 por el juego** con sus mutaciones reales. De cada parte del cuerpo sale una **línea fina** que acaba en un **recuadro** con el icono de 16×16 de la mutación que lleva esa ranura, o la silueta apagada si está libre (las ranuras del HUD: `UI_Slot_Frame` y `UI_Slot_<Ranura>_Empty`, que ya existen). Los 4 recuadros van en las esquinas de la lente: Boca y Piel arriba, Extremidades y Órgano abajo.
4. **Al pasar el ratón por un recuadro** se resalta y aparece al lado un **cuadro de información**: franja del color del linaje, nombre, ranura y linaje, y la descripción de lo que hace (todo texto del juego). Con la ranura vacía dice "Ranura libre".

**No dibujes la célula ni sus piezas:** el juego monta el retrato con los sprites que ya existen. En la vista previa, usa `generar_jugador.py` y `generar_mutaciones.py` para pintarla (cuadro `Idle_down` con Caparazón, Aletas y Mandíbulas, ampliada ×3) y comprobar que cabe y se lee dentro de la lente.

## Hojas

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Pause_Lens.png` | 1 | 112×112 | Lente circular de microscopio: aro de vidrio grueso con brillo arriba a la izquierda y un interior de agua algo más clara que el fondo, con viñeta suave en el borde. Cabe una célula de 32×32 ×3 = 96×96 en el interior. Centro exacto (56, 56) |
| `UI_Pause_Lens_Glint.png` | 1 | 112×112 | Capa que va **encima** de la célula: reflejo curvo del vidrio, semitransparente, solo en el cuarto superior izquierdo |
| `UI_Pause_LensBubbles.png` | 8 (bucle) | 112×112 | Burbujitas que suben despacio dentro de la lente (detrás de la célula). Transparente salvo las burbujas |
| `UI_Callout_Line.png` | 1 | 3×3 | Trazo de 1 px claro (verde agua pálido) con contorno oscuro: el juego lo estira en horizontal y en vertical (9-slice, borde 1) para hacer líneas en codo, sin diagonales |
| `UI_Callout_Dot.png` | 4 (bucle) | 5×5 | Punto donde nace la línea, sobre la pieza de la célula; late suavemente. Centro (2, 2) |
| `UI_Slot_Frame_Highlighted.png` | 1 | 20×20 | `UI_Slot_Frame` con el borde más claro y brillante (ratón encima). Mismo encuadre |
| `UI_Tooltip.png` | 1 | 24×24 | Fondo del cuadro de información: membrana oscura **más opaca** que las tarjetas (debe leerse sobre la lente y la célula). 9-slice, borde 6 px. El juego le pone arriba la franja `UI_Card_Stripe` teñida |
| `UI_Tooltip_Pointer.png` | 1 | 6×7 | Piquito que une el cuadro de información con su recuadro, apuntando a la izquierda (el juego lo voltea si hace falta). Mismos colores y borde que `UI_Tooltip` |
| `UI_Stats_Panel.png` | 1 | 48×48 | Fondo del panel de estadísticas. Variante de `UI_Panel` algo más compacta y discreta. 9-slice, borde 10 px |
| `UI_Stats_Row.png` | 1 | 16×14 | Fondo muy sutil de cada fila (una veta de membrana), alternando con filas sin fondo. 9-slice horizontal, borde 4 px a izquierda y derecha |
| `UI_Stat_Health.png` | 1 | 12×12 | Icono de vida: corazón pequeño en el estilo de `UI_Heart_Full` |
| `UI_Stat_Damage.png` | 1 | 12×12 | Icono de daño: colmillo o púa de hueso, cálido |
| `UI_Stat_Speed.png` | 1 | 12×12 | Icono de velocidad: aleta o flagelo con estela de 2–3 líneas de movimiento |
| `UI_Stat_Up.png` / `UI_Stat_Down.png` | 1 | 7×7 | Flechita verde lima (sube) y roja (baja) que el juego pone junto al valor cuando una mutación lo cambia respecto a la célula base |
| `UI_Pause_Title.png` | 1 | 32×16 | Cinta o membrana donde va escrito "PAUSA" (el texto lo pone el juego). 9-slice, borde 8 px a izquierda y derecha |

Los iconos de estadística deben compartir estilo y peso visual, porque más adelante añadiré otros con el mismo encargo (cadencia, recarga de esquiva, alcance…). Deja en el README una nota con la paleta y las reglas que usaste para ellos.

---

# Parte B · Menú principal

## Cómo es la pantalla

Hoy es un fondo liso con "PRIMORDIA", un subtítulo ("De la sopa primordial a la cima") y los botones Jugar y Salir. La quiero así:

- **Fondo animado de caldo primordial** en capas, con algo de profundidad: agua oscura al fondo, rayos de luz que bajan ondulando desde la superficie, partículas y plancton que flotan, y siluetas desenfocadas de células grandes que pasan por delante muy despacio. El juego desplaza cada capa a distinta velocidad.
- **Logo "PRIMORDIA"** arriba en el centro, con letras orgánicas (como hechas de membrana o de colonias de células), con brillo y alguna burbuja.
- Debajo del logo, **la célula del jugador** ampliada ×3 por el juego, flotando en su animación de reposo (no la dibujes; en la vista previa píntala con `generar_jugador.py`).
- **Botones** Jugar y Salir (y más adelante Opciones) en columna bajo la célula, con los `UI_Button_*` existentes, y un **marcador** animado junto al botón que tiene el ratón encima.
- El subtítulo y la versión del juego los pone el juego como texto.

## Hojas

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `Menu_Bg_Far.png` | 1 | 480×270 | Agua profunda: degradado de `#16352e` abajo a un verde azulado algo más claro arriba, con textura muy sutil. Sin elementos llamativos |
| `Menu_Bg_Rays.png` | 8 (bucle) | 480×270 | Rayos de luz semitransparentes que bajan en diagonal desde arriba y ondulan. Capa aditiva o de transparencia suave |
| `Menu_Bg_Particles.png` | 1 | 480×270 | Plancton, motas y burbujitas sueltas. **Se repite sin cortes en horizontal y en vertical** (el juego la desplaza hacia arriba despacio) |
| `Menu_Bg_Near.png` | 1 | 480×270 | 3 o 4 siluetas grandes y desenfocadas de células, bacterias y amebas, oscuras y semitransparentes, en los bordes (no tapes el centro). **Se repite sin cortes en horizontal** (el juego la desplaza de lado) |
| `Menu_Logo.png` | 1 | unos 200×48 | Logo "PRIMORDIA" en mayúsculas, orgánico, verde lima y verde agua con contorno oscuro, legible a 1:1. Centro en el centro del lienzo |
| `Menu_Logo_Shine.png` | 8 | mismo lienzo que el logo | Brillo que recorre el logo de izquierda a derecha (el juego lo repite cada pocos segundos). Transparente salvo el brillo |
| `Menu_Selector.png` | 4 (bucle) | 9×9 | Marcador junto al botón activo: una célula pequeñita o un flagelo que señala a la derecha y palpita. Centro (4, 4) |
| `Menu_Bubble.png` | 5 | 8×8 | Burbuja que sube y revienta, para soltar algunas sobre el logo o al pulsar un botón. Centro (4, 4) |

---

## Qué me tienes que entregar

```
menus/
  Pause/    (UI_Pause_…, UI_Callout_…, UI_Slot_Frame_Highlighted, UI_Tooltip…, UI_Stats_…, UI_Stat_…)
  Main/     (Menu_Bg_…, Menu_Logo…, Menu_Selector, Menu_Bubble)
  gif/      (cada animación por separado, y el fondo del menú con sus capas desplazándose)
  vista_previa_menus.png
  generar_menus.py
  README.md (tabla con cada hoja: cuadros, lienzo, punto central o borde 9-slice, y duración recomendada por cuadro;
             para el menú principal, la velocidad recomendada de desplazamiento de cada capa en px/s)
```

La **vista previa** (ampliada ×3) debe montar el resultado como lo verá el jugador, cada pantalla en un lienzo de 480×270:

1. **Pausa**, sobre una partida oscurecida al 75 %: estadísticas a la izquierda (Vida 6/8, Daño 2, Velocidad 7 con flecha de subida por las Aletas), "PAUSA" y los 3 botones en el centro, y a la derecha la lente con la célula ×3 (Caparazón, Aletas y Mandíbulas), las 4 líneas con sus recuadros (3 con icono y la del Órgano vacía) y el **cuadro de información abierto** junto al recuadro de la Boca, con la franja naranja de Depredador y texto de ejemplo. Usa una fuente pixel cualquiera para los textos de la maqueta.
2. **Pausa sin mutaciones**: la célula sola y los 4 recuadros con su silueta apagada.
3. **Menú principal**: las 4 capas del fondo, el logo, la célula ×3 debajo, los botones Jugar y Salir con el marcador junto a Jugar.

Comprueba en la vista previa que las líneas en codo no cruzan la célula ni se tapan entre sí, que el cuadro de información se lee sobre la lente, que los 3 iconos de estadística se distinguen a 12×12, que el logo se lee a tamaño 1:1 y que el fondo del menú no compite con el logo ni con los botones.
