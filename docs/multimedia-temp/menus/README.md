# Menús: pausa y menú principal

Arte del menú de pausa (`Pause/`) y del menú principal (`Main/`). Generado con `generar_menus.py`, que reutiliza las paletas y funciones de `generar_ui.py`, `generar_mutaciones.py` y `generar_jugador.py`.

Para regenerar: `python generar_menus.py <carpeta multimedia-temp> <carpeta de salida>`. Necesita `ui/`, `mutations/` y `player/` con sus generadores al lado.

Todo está a 16 PPU, con filtro Point y sin compresión. Las hojas animadas van en fila (cuadro 0 a la izquierda). La interfaz se monta sobre un lienzo de 480×270.

## Pause/

| Hoja | Cuadros | Lienzo | Centro / 9-slice | ms por cuadro |
|---|---|---|---|---|
| `UI_Pause_Lens` | 1 | 112×112 | centro (56, 56); interior de radio 47,5 | – |
| `UI_Pause_Lens_Glint` | 1 | 112×112 | centro (56, 56); va encima de la célula | – |
| `UI_Pause_LensBubbles` | 8 (bucle) | 112×112 | centro (56, 56); va detrás de la célula | 160 |
| `UI_Callout_Line` | 1 | 3×3 | 9-slice, borde 1 | – |
| `UI_Callout_Dot` | 4 (bucle) | 5×5 | centro (2, 2) | 180 |
| `UI_Slot_Frame_Highlighted` | 1 | 20×20 | igual que `UI_Slot_Frame` | – |
| `UI_Tooltip` | 1 | 24×24 | 9-slice, borde 6 | – |
| `UI_Tooltip_Pointer` | 1 | 6×7 | punta a la izquierda; pisa 2 px el cuadro (lado derecho abierto) | – |
| `UI_Stats_Panel` | 1 | 48×48 | 9-slice, borde 10 | – |
| `UI_Stats_Row` | 1 | 16×14 | 9-slice, borde 4 a izquierda y derecha (L4 T0 R4 B0) | – |
| `UI_Stat_Health` | 1 | 12×12 | centro (6, 6) | – |
| `UI_Stat_Damage` | 1 | 12×12 | centro (6, 6) | – |
| `UI_Stat_Speed` | 1 | 12×12 | centro (6, 6) | – |
| `UI_Stat_Up` / `UI_Stat_Down` | 1 | 7×7 | centro (3, 3) | – |
| `UI_Pause_Title` | 1 | 32×16 | 9-slice, 8 px a izquierda y derecha (L8 T0 R8 B0) | – |

### Cómo se monta la lente

Orden de capas, de atrás adelante:
1. `UI_Pause_Lens` y `UI_Pause_LensBubbles`.
2. Las líneas (`UI_Callout_Line`).
3. El retrato de la célula, recortado con una máscara circular de radio 47,5 alrededor de (56, 56).
4. `UI_Pause_Lens_Glint`.
5. Los puntos (`UI_Callout_Dot`).
6. Los recuadros y el cuadro de información.

Las líneas pasan **por detrás** de la célula. Así, la del Órgano puede salir del centro del cuerpo sin taparlo.

**Escala del retrato.** La célula con Mandíbulas mide 31×38 px. A ×3 son 93×114, y no cabe en el interior de 96 de diámetro: la boca se sale por abajo. La vista previa aplica esta regla: **la mayor escala entera (máximo ×3) a la que la criatura cabe en el interior**, centrada en su caja.
- Sin mutaciones sale ×3.
- Con Caparazón, Aletas y Mandíbulas sale ×2.

Si se prefiere ×3 siempre, hay que recortar la boca o agrandar la lente.

**Rutas sin cruces.** Para que las líneas no se crucen con los recuadros en su sitio, cada línea nace en un punto distinto. Ver la vista previa:

| Recuadro | Dónde nace la línea | Recorrido |
|---|---|---|
| Boca (arriba izquierda) | costado izquierdo de la boca | a la izquierda, por el pasillo izquierdo y arriba |
| Piel (arriba derecha) | borde superior de la piel | arriba y a la derecha |
| Órgano (abajo derecha) | el órgano | a la derecha, por el pasillo derecho y abajo |
| Extremidades (abajo izquierda) | punta baja de la aleta derecha | abajo, por debajo de la boca, y a la izquierda |

Los pasillos van a ±61 px del centro de la lente. Los recuadros van a (−85, −82), (65, −82), (−85, 62) y (65, 62) respecto al centro de la lente; la zona entera mide 170×164.

### Iconos de estadística (para hacer más)

Los iconos que vengan (cadencia, recarga de esquiva, alcance…) deben seguir estas reglas:
- Silueta de unos 10×10 dentro de 12×12, con 1 px libre alrededor.
- Contorno de 1 px `#181425`.
- Luz arriba a la izquierda, con 1 píxel de brillo blanco.
- Rampa de 4 tonos con un color propio por estadística, que no repita el de otra.
- Forma reconocible por sí sola, sin depender del color.

| Icono | Forma | Rampa |
|---|---|---|
| Vida | corazón | `#7a1e2a` `#c8384a` `#f0706e` `#ffd0c0` (la de `HEART`) |
| Daño | púa de hueso en diagonal con base de carne | hueso (`BONE`) y carne (`FLESH`) de las mutaciones |
| Velocidad | aleta lanzada a la derecha con 3 líneas de movimiento | aleta (`FIN`) y líneas en aqua `#5ad0b0` / `#b0f0e0` |

Las flechas van a la derecha del valor: subida en lima, bajada en rojo.

## Main/

| Hoja | Cuadros | Lienzo | Centro | ms por cuadro | Desplazamiento |
|---|---|---|---|---|---|
| `Menu_Bg_Far` | 1 | 480×270 | – | – | 0 px/s (fijo) |
| `Menu_Bg_Rays` | 8 (bucle) | 480×270 | – | 180 | 0 px/s (anima en el sitio) |
| `Menu_Bg_Particles` | 1 | 480×270 | – | – | 6 px/s hacia arriba (enlaza en los dos ejes) |
| `Menu_Bg_Near` | 1 | 480×270 | – | – | 10 px/s hacia la izquierda (enlaza en horizontal) |
| `Menu_Logo` | 1 | 200×48 | centro (100, 24) | – | – |
| `Menu_Logo_Shine` | 8 | 200×48 | igual que el logo, encima | 50 | – |
| `Menu_Selector` | 4 (bucle) | 9×9 | centro (4, 4) | 150 | – |
| `Menu_Bubble` | 5 | 8×8 | centro (4, 4) | 90 | – |

- **Orden de capas:** Far, Rays, Particles, Near y luego la interfaz.
- **Brillo del logo:** se lanza cada pocos segundos. Por ejemplo, una pasada de 8 cuadros y luego unos 3 s de espera.
- **`Menu_Bubble`:** es un estallido de un solo uso (aparece, crece y revienta). Sirve, por ejemplo, para el clic en un botón.
- **Selector:** va a la izquierda del botón activo, centrado en su altura.
- **Maqueta:** el logo está en y = 14, la célula ×3 debajo y los botones Jugar y Salir (`UI_Button_*`) en y = 150 y y = 172.

## Vista previa

`vista_previa_menus.png` está ampliada ×3 y muestra tres pantallas:
1. Pausa con mutaciones y el cuadro de información abierto.
2. Pausa sin mutaciones.
3. Menú principal.

La fuente de la maqueta es una de 3×5 provisional; el juego usará la suya.

Otros archivos:
- `gif/`: cada animación y `Menu_Principal_capas.gif`, con el fondo desplazándose.
- `_iconos_estadistica_x4.png`: los iconos ampliados.
