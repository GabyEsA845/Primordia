Necesito el arte del **minimapa** y del **mapa grande** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua, en un caldo primordial). El jugador es una célula verde que muta. Te adjunto `generar_ui.py`, `generar_menus.py` y sus vistas previas (`vista_previa_ui.png`, `vista_previa_menus.png`) para que todo encaje con la interfaz que ya existe (paneles de membrana, `UI_Panel`, `UI_Slot_Frame`, `UI_Pause_Title`, la lente del microscopio).

Hazlo con un script de Python + Pillow (`generar_mapa.py`) que **reutilice las funciones y paletas de esos scripts** (`Canvas`, contorno oscuro, rampas de color, paneles 9-slice).

## Cómo funciona el mapa en el juego

- El piso es una rejilla de salas unidas por pasillos de 4 casillas de ancho. Cada sala tiene su forma (rectangular, en cruz, en L, con columnas…). Una sala normal mide 28×16 casillas.
- **El juego dibuja las salas**, no tú: pinta el plano del piso píxel a píxel, **1 píxel por casilla**, con los colores de la paleta que me entregues (ver la sección 3). Tú haces los marcos, el fondo, los iconos y la paleta.
- **Niebla:** las salas visitadas se ven enteras; las salas conectadas con una visitada aparecen como silueta apagada hasta que entras; el resto no se ve.
- **Minimapa:** recuadro fijo en la **esquina superior derecha** del HUD, de **80×60 px** de interior, centrado en el jugador. Lo que se sale del recuadro queda recortado.
- **Mapa grande:** se abre con TAB, pausa el juego y muestra **todo lo descubierto** centrado en la pantalla, ampliado ×2 (o ×1 si no cabe), sobre un panel grande. TAB o Esc lo cierran.
- **Marcas:** el jugador (se mueve dentro de la sala), la sala del jefe y las salas que dan mutación (hasta que se recoge). Las salas con enemigos aún vivos se pintan de otro color.

## Escala y estilo

- La interfaz se dibuja a **la misma escala que el juego**: lienzo de **480×270 px**, un píxel de interfaz = un píxel del juego. El juego luego lo amplía ×2 o más sin suavizado.
- Filtro nítido (sin antialiasing), fondo transparente, contorno `#181425` de 1 px como el resto del arte.
- Luz desde arriba a la izquierda, 3 o 4 tonos por color.
- Aspecto orgánico y acuático, de laboratorio de biología (vidrio, membrana, placa de Petri), en la misma familia que los paneles y la lente de la pausa. Nada de metal ni de pergamino de fantasía.
- **Sin texto** dentro de los sprites (el título "MAPA" lo pone el juego sobre `UI_Pause_Title`).

## 1. Minimapa (HUD)

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Minimap_Frame.png` | 1 | 32×32 | Marco del minimapa: borde de membrana o vidrio de **4 px**, interior **transparente** (el juego pinta el mapa debajo). **9-slice, borde 6 px**: el juego lo estira a 88×68 (80×60 de interior + el borde). Debe verse sobre el suelo oscuro y sobre las paredes moradas |
| `UI_Minimap_Back.png` | 1 | 16×16 | Fondo del interior, detrás del mapa: agua oscura **semitransparente** (~80 % opaca), para que el plano se lea sobre cualquier zona del juego. 9-slice, borde 2 px. Mismo color que `Background` de la paleta |

## 2. Mapa grande (TAB)

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Map_Panel.png` | 1 | 48×48 | Panel grande donde va el mapa: una placa de Petri o portaobjetos de vidrio vista desde arriba, con borde grueso y brillo, interior liso del color `Background`. **9-slice, borde 12 px**: el juego lo estira a unos 400×220 |
| `UI_Map_Glint.png` | 1 | 48×48 | Reflejo del vidrio que va **encima** del mapa: una franja curva semitransparente en la esquina superior izquierda. 9-slice, borde 12 px, centro transparente |

## 3. Paleta del plano

El juego pinta cada casilla con un color de esta paleta. Entrégala como **una tira de 8×1 px** (un píxel por color, en este orden de izquierda a derecha), y repite los valores en el README:

| x | Nombre | Uso |
|---|--------|-----|
| 0 | `Background` | Fondo del minimapa y del mapa grande (agua oscura) |
| 1 | `Wall` | Contorno de las salas visitadas (las casillas de pared que tocan el suelo) |
| 2 | `Floor` | Suelo de las salas visitadas y limpias |
| 3 | `FloorCurrent` | Suelo de la sala donde está el jugador (más claro, se distingue a primera vista) |
| 4 | `FloorUncleared` | Suelo de las salas visitadas con enemigos vivos (tono de alerta, rojizo o rosado, que no se confunda con el jefe) |
| 5 | `Corridor` | Pasillos y huecos de puerta entre salas visitadas |
| 6 | `Unexplored` | Silueta apagada de las salas vecinas aún sin visitar (suelo y contorno del mismo color, poco contraste con el fondo) |
| 7 | `UnexploredWall` | Contorno de esas salas sin visitar, un poco más claro que `Unexplored` |

`UI_Map_Palette.png`: 8×1 px, colores **opacos**. Deben leerse bien a 1 píxel por casilla, en el minimapa y en el mapa grande ×2, y armonizar con el verde del suelo `#16352e` y los paneles existentes.

## 4. Iconos

Se dibujan centrados sobre el plano, encima de las salas. Contorno oscuro para que se vean sobre cualquier color de la paleta.

| Hoja | Cuadros | Lienzo | Notas |
|------|---------|--------|-------|
| `UI_Map_Player.png` | 4 (bucle) | 7×7 | El jugador: la célula verde vista desde arriba, muy simplificada, que palpita. Centro (3, 3). ~150 ms por cuadro |
| `UI_Map_Boss.png` | 1 | 9×9 | Sala del jefe: el virus (esfera con púas) simplificado, en rojo o magenta. Centro (4, 4) |
| `UI_Map_Reward.png` | 1 | 7×7 | Sala con mutación sin recoger: la burbuja de mutación (membrana con un núcleo brillante), en dorado o blanco cálido para que no se confunda con ningún linaje. Centro (3, 3) |

## Qué me tienes que entregar

```
map/
  UI_Minimap_Frame.png, UI_Minimap_Back.png
  UI_Map_Panel.png, UI_Map_Glint.png
  UI_Map_Palette.png
  UI_Map_Player.png, UI_Map_Boss.png, UI_Map_Reward.png
  gif/      (UI_Map_Player animado)
  vista_previa_mapa.png
  generar_mapa.py
  README.md (tabla con cada hoja: cuadros, lienzo, centro o borde 9-slice y duración por cuadro; y los 8 colores de la paleta en hexadecimal)
```

La **vista previa** (ampliada ×3) debe montar el resultado como lo verá el jugador, sobre un lienzo de 480×270:

1. **HUD en partida:** una captura simulada del juego (suelo `#16352e` y una pared morada arriba), con los corazones y las ranuras arriba a la izquierda (`vista_previa_ui.png`), y el minimapa de 88×68 arriba a la derecha, centrado en el jugador. El plano de ejemplo: la sala actual (28×16, rectangular) con el jugador; a su izquierda una sala visitada en cruz unida por un pasillo de 4 de ancho; a su derecha una sala vecina sin visitar (silueta); y abajo una sala visitada con enemigos vivos, recortada por el borde del recuadro.
2. **Mapa grande:** la partida oscurecida al 75 %, el panel de unos 400×220 centrado, el título "MAPA" sobre `UI_Pause_Title`, y dentro un piso de ejemplo de 8 o 9 salas ampliado ×2: inicio, camino con giros, 2 salas laterales sin salida, sala del jefe al final con su icono (sin visitar), una sala con recompensa con su icono, la sala actual con el jugador y 2 salas vecinas sin visitar. Encima, el reflejo del vidrio.

Comprueba en la vista previa que se distinguen a 1 píxel por casilla la sala actual, las visitadas, las que tienen enemigos y las sin visitar; que los pasillos de 4 casillas se ven como uniones claras; y que los iconos se leen sobre todos los colores de la paleta.
