# Objetos e interfaz

Generado con `generar_ui.py`, que reutiliza `../player/generar_jugador.py` y `../mutations/generar_mutaciones.py` (paletas, contorno #181425, rampas y piezas de las mutaciones).

- Misma escala que el juego: lienzo de interfaz de 480×270, un píxel de interfaz = un píxel del juego. 16 píxeles por unidad, filtro Point, sin compresión, fondo transparente.
- Tiras horizontales; cada hoja se corta por cuadros del ancho del lienzo indicado.
- Sin texto dentro de los sprites.
- La burbuja de mutación no se desplaza dentro del sprite: el juego hace flotar el objeto padre con la burbuja, el icono de 16×16 (centrado, en (4, 4)) y la sombra.
- La mira se dibuja como imagen de la interfaz que sigue al ratón (el cursor del sistema se oculta).
- Burbujas, iconos de 12×12 y franja de las tarjetas van por **ranura** (`LineageSetup.SlotColors`): Boca naranja (0.95, 0.5, 0.35), Extremidades morado (0.66, 0.35, 0.82), Piel azul (0.55, 0.7, 0.9) y Órgano lima (0.55, 0.9, 0.35). Se generan `Items/Item_Mutation_<Mouth|Limbs|Skin|Organ>_Idle/_Collect.png` e `Icons/Icon_Slot_<ranura>.png`. Las hojas de los linajes antiguos (Predator, Armored, Toxic) se siguen generando para la vista previa, pero el juego ya no las importa.
- La barra de recarga actual está a 0,85 unidades bajo el jugador; Back mide 18×4 y el relleno 16×2 va dentro con 1 px de margen.

| Hoja | Carpeta | Cuadros | Lienzo | Punto central o borde 9-slice | ms por cuadro |
|---|---|---|---|---|---|
| `Items/Item_Biomass_Idle.png` | Items | 4 | 10×10 | centro (5, 5) | 150 |
| `Items/Item_Biomass_Big_Idle.png` | Items | 4 | 14×14 | centro (7, 7) | 150 |
| `Items/Item_Biomass_Collect.png` | Items | 4 | 16×16 | centro (8, 8) | 60 |
| `HUD/UI_Biomass.png` | HUD | 1 | 12×12 | a la izquierda del número | — |
| `Items/Item_Mutation_Predator_Idle.png` | Items | 6 | 24×24 | centro (12, 12); icono de 16×16 en (4, 4) | 120 |
| `Items/Item_Mutation_Predator_Collect.png` | Items | 5 | 32×32 | centro (16, 16) | 60 |
| `Items/Item_Mutation_Armored_Idle.png` | Items | 6 | 24×24 | centro (12, 12); icono de 16×16 en (4, 4) | 120 |
| `Items/Item_Mutation_Armored_Collect.png` | Items | 5 | 32×32 | centro (16, 16) | 60 |
| `Items/Item_Mutation_Toxic_Idle.png` | Items | 6 | 24×24 | centro (12, 12); icono de 16×16 en (4, 4) | 120 |
| `Items/Item_Mutation_Toxic_Collect.png` | Items | 5 | 32×32 | centro (16, 16) | 60 |
| `Items/Item_Mutation_Shadow.png` | Items | 1 | 16×6 | centro (8, 3); va 10 px bajo el centro de la burbuja | — |
| `Icons/Icon_Mandibles_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_Mandibles_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_Stinger_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_Stinger_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_Spores_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_Spores_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_Fins_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_Fins_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_JointedLegs_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_JointedLegs_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_Carapace_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_Carapace_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_StingingSkin_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_StingingSkin_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_InkSac_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_InkSac_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_ShockGland_16.png` | Icons | 1 | 16×16 | centro del lienzo | — |
| `Icons/Icon_ShockGland_32.png` | Icons | 1 | 32×32 | centro del lienzo | — |
| `Icons/Icon_Lineage_Predator.png` | Icons | 1 | 12×12 | centro del lienzo | — |
| `Icons/Icon_Lineage_Armored.png` | Icons | 1 | 12×12 | centro del lienzo | — |
| `Icons/Icon_Lineage_Toxic.png` | Icons | 1 | 12×12 | centro del lienzo | — |
| `HUD/UI_Heart_Full.png` | HUD | 1 | 16×16 | fila en la esquina superior izquierda, cada uno 16×16 | — |
| `HUD/UI_Heart_Half.png` | HUD | 1 | 16×16 | fila en la esquina superior izquierda, cada uno 16×16 | — |
| `HUD/UI_Heart_Empty.png` | HUD | 1 | 16×16 | fila en la esquina superior izquierda, cada uno 16×16 | — |
| `HUD/UI_Crosshair.png` | HUD | 1 | 15×15 | centro exacto (7, 7) | — |
| `HUD/UI_Crosshair_Fire.png` | HUD | 2 | 15×15 | centro exacto (7, 7) | 40 |
| `HUD/UI_Slot_Frame.png` | HUD | 1 | 20×20 | icono de 16×16 en (2, 2) | — |
| `HUD/UI_Slot_Mouth_Empty.png` | HUD | 1 | 16×16 | va en (2, 2) dentro del marco | — |
| `HUD/UI_Slot_Limbs_Empty.png` | HUD | 1 | 16×16 | va en (2, 2) dentro del marco | — |
| `HUD/UI_Slot_Skin_Empty.png` | HUD | 1 | 16×16 | va en (2, 2) dentro del marco | — |
| `HUD/UI_Slot_Organ_Empty.png` | HUD | 1 | 16×16 | va en (2, 2) dentro del marco | — |
| `HUD/UI_Cooldown_Back.png` | HUD | 1 | 18×4 | centro (9, 2); el relleno va en (1, 1) | — |
| `HUD/UI_Cooldown_Fill.png` | HUD | 1 | 16×2 | se recorta de izquierda a derecha | — |
| `HUD/UI_Cooldown_Ready.png` | HUD | 3 | 18×4 | mismo encuadre que Back | 50 |
| `Panels/UI_Card_Normal.png` | Panels | 1 | 48×48 | 9-slice, borde 12 px | — |
| `Panels/UI_Card_Highlighted.png` | Panels | 1 | 48×48 | 9-slice, borde 12 px | — |
| `Panels/UI_Card_Pressed.png` | Panels | 1 | 48×48 | 9-slice, borde 12 px | — |
| `Panels/UI_Card_Disabled.png` | Panels | 1 | 48×48 | 9-slice, borde 12 px | — |
| `Panels/UI_Panel.png` | Panels | 1 | 48×48 | 9-slice, borde 12 px | — |
| `Panels/UI_Button_Normal.png` | Panels | 1 | 32×16 | 9-slice, borde 7 px | — |
| `Panels/UI_Button_Highlighted.png` | Panels | 1 | 32×16 | 9-slice, borde 7 px | — |
| `Panels/UI_Button_Pressed.png` | Panels | 1 | 32×16 | 9-slice, borde 7 px | — |
| `Panels/UI_Card_Stripe.png` | Panels | 1 | 16×6 | 9-slice: izquierda 4, derecha 4, arriba 0, abajo 0; blanca, el juego la tiñe; alto recomendado 6 px | — |

`gif/` tiene cada animación por separado. `_iconos_x4.png` muestra los 18 iconos ampliados para comparar.

Para regenerar: `python generar_ui.py .. .`
