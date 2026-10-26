# Mapa: minimapa del HUD y mapa grande (TAB)

Marcos, fondo, paleta del plano e iconos del mapa. Se generan con `generar_mapa.py`, que reutiliza `generar_menus.py` y `generar_ui.py` (la membrana de los paneles, el vidrio de la lente, `UI_Pause_Title` y las rampas).

Para regenerar: `python generar_mapa.py <carpeta multimedia-temp> <carpeta de salida>`. Necesita al lado las carpetas `ui/`, `menus/`, `mutations/` y `player/` con sus generadores.

Ajustes de importación en Unity:
- 16 PPU.
- Filtro Point.
- Sin compresión.
- Las hojas animadas van en fila, con el cuadro 0 a la izquierda.

La interfaz va sobre un lienzo de 480×270, con 1 píxel de interfaz por píxel de juego.

## Hojas

| Hoja | Cuadros | Lienzo | Centro / 9-slice | ms por cuadro |
|---|---|---|---|---|
| `UI_Minimap_Frame` | 1 | 32×32 | 9-slice, borde 6 (marco visible de 4 px, interior transparente) | – |
| `UI_Minimap_Back` | 1 | 16×16 | 9-slice, borde 2; color `Background` al 80 % (alfa 204) | – |
| `UI_Map_Panel` | 1 | 48×48 | 9-slice, borde 12; interior opaco color `Background` | – |
| `UI_Map_Glint` | 1 | 48×48 | 9-slice, borde 12; centro transparente; va encima del mapa | – |
| `UI_Map_Palette` | 1 | 8×1 | un píxel por color, en el orden de la tabla de abajo | – |
| `UI_Map_Player` | 4 (bucle) | 7×7 | centro (3, 3) | 150 |
| `UI_Map_Boss` | 1 | 9×9 | centro (4, 4) | – |
| `UI_Map_Reward` | 1 | 7×7 | centro (3, 3) | – |

## Paleta del plano (`UI_Map_Palette.png`)

| x | Nombre | Hex | Uso |
|---|---|---|---|
| 0 | `Background` | `#0d221d` | Fondo del minimapa y del mapa grande |
| 1 | `Wall` | `#9ad8bc` | Contorno de las salas visitadas |
| 2 | `Floor` | `#2e6652` | Suelo visitado y limpio |
| 3 | `FloorCurrent` | `#7cc98e` | Suelo de la sala del jugador |
| 4 | `FloorUncleared` | `#a34a5e` | Suelo visitado con enemigos vivos (rosa apagado; el jefe es magenta vivo) |
| 5 | `Corridor` | `#4a8a72` | Pasillos y huecos de puerta entre salas visitadas |
| 6 | `Unexplored` | `#1a3a32` | Silueta de las vecinas sin visitar |
| 7 | `UnexploredWall` | `#2b5446` | Contorno de esas salas |

Todos los colores son opacos.

## Cómo pinta el plano la vista previa

Es una propuesta; la regla de pintado es tuya.
- **Paredes:** son las casillas de la sala que tocan el exterior, incluidas las diagonales, más las columnas interiores.
- **Pasillos:** son de 4 de ancho y se pintan con `Corridor`, también en el hueco de la puerta.
- **Pasillo hacia una sala sin visitar:** se pinta con `Unexplored`. El hueco de la puerta en la sala visitada lleva `Corridor`.

## Montaje

**Minimapa**
- El marco va estirado a 88×68, a 8 px de la esquina superior derecha (x 384, y 8).
- `UI_Minimap_Back` va estirado a 80×60, en el interior (x 388, y 12).
- El plano se pinta encima del fondo, recortado a esos 80×60 y centrado en el jugador.
- Luego van los iconos y, al final, el marco.
- En la maqueta, el contador de biomasa baja debajo del minimapa, porque antes ocupaba esa esquina.

**Mapa grande**
- La partida va oscurecida al 75 %.
- El panel mide 400×220 y va centrado.
- El plano va ×2 (o ×1 si no cabe en el interior), con los iconos encima.
- `UI_Map_Glint` va estirado al tamaño del panel, encima de todo menos del título.
- El título "MAPA" va sobre `UI_Pause_Title`, montado sobre el borde superior del panel.

**Iconos**
- Van centrados en la casilla del jugador o en el centro de la sala.
- No se amplían con el mapa ×2: tienen el mismo tamaño en el minimapa y en el mapa grande.

## Vista previa

`vista_previa_mapa.png` está ampliada ×3 y muestra:
1. El HUD en partida, con el minimapa sobre la pared morada.
2. El mapa grande con 9 salas: inicio, cruce, 2 laterales sin salida (una con enemigos vivos), la sala del premio, la sala actual, el jefe y 2 vecinas sin visitar.
3. Una tira de comprobación: los tres iconos sobre cada color de la paleta, y el marco del minimapa sobre el suelo oscuro.

`gif/UI_Map_Player.gif` muestra el latido del icono del jugador.
