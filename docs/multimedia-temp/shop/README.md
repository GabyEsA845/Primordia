# Tienda (Primordia, piso 1)

Generado con `generar_tienda.py` (Python + Pillow). Reutiliza `generar_mapa.py`, que carga
`generar_menus.py` → `generar_ui.py` → `generar_mutaciones.py` → `generar_jugador.py`, y las
definiciones de `generar_escenario.py`: mismo `Cv`, contorno `#181425`, rampas de 4 tonos, luz
arriba a la izquierda, la burbuja de mutación (con la rampa dorada del nivel III), los corazones
del HUD, la biomasa, la membrana de los botones, el vidrio y el plano del minimapa.

```
python generar_tienda.py [carpeta_multimedia_temp] [carpeta_salida]
```
Por defecto lee los otros generadores de la carpeta de arriba (`player/`, `mutations/`, `ui/`,
`menus/`, `map/`, `environment/`) y escribe aquí.

16 px por unidad, filtro Point, sin compresión. Hojas en fila, cuadro 0 a la izquierda.
Coordenadas en píxeles desde la esquina de arriba a la izquierda de cada cuadro.

| Hoja | Cuadros | Lienzo | Punto central / de apoyo | Duración por cuadro |
|------|---------|--------|--------------------------|---------------------|
| `Shop_Darwin_Idle.png` | 6 (bucle) | 24×32 | Pies: (12, 31), abajo en el centro | 220, 160, 90, 180, 180, 220 ms |
| `Shop_Darwin_Buy.png` | 6 | 24×32 | Pies: (12, 31). El cuadro 5 es igual que Idle 0 | 70, 80, 110, 80, 90, 120 ms |
| `Shop_Counter.png` | 1 | 48×24 | Abajo en el centro (24, 23). Cara de arriba en y 9-13, frente en y 14-22 | — |
| `Shop_Pedestal.png` | 1 | 20×16 | Abajo en el centro (10, 15). Cara de arriba centrada en (10, 4) | — |
| `Shop_Pedestal_Glow.png` | 4 (bucle) | 20×16 | Igual que el pedestal; se dibuja encima | 150 ms |
| `Item_Upgrade_Idle.png` | 6 (bucle) | 24×24 | Centro (12, 12). Centro 16×16 libre para el icono | 120 ms |
| `Item_Heal_Idle.png` | 4 (bucle) | 12×12 | Centro (6, 6) | 150 ms |
| `Item_HeartContainer_Idle.png` | 4 (bucle) | 16×16 | Centro (8, 8) | 130 ms |
| `Item_Purchase.png` | 6 | 32×32 | Centro (16, 16), en el centro del artículo | 60 ms |
| `UI_Key_E.png` | 2 (estados) | 13×13 | Abajo en el centro (6, 12). 0 normal, 1 pulsada | — |
| `UI_Map_Shop.png` | 1 | 7×7 | Centro (3, 3) | — |

## Cómo se montan

- **Darwin tras el mostrador:** con el pivote de los dos abajo en el centro, Darwin va 5 px
  (0,3125 u) por encima del pivote del mostrador y se dibuja **detrás** (orden menor). Así asoma
  de cintura para arriba.
- **Artículo sobre el pedestal:** la sombra `Item_Mutation_Shadow` (16×6) va en la cara de arriba,
  con su esquina en (2, 1) del pedestal. El artículo flota centrado en x con su borde de abajo
  2 px por debajo de la parte alta del pedestal (el juego lo hace subir y bajar).
- **Mejora:** el icono de 16×16 va en el centro de `Item_Upgrade_Idle` y el texto "I → II"
  debajo del pedestal, encima del precio.
- **Precio:** `UI_Biomass` (12×12) + número con Silkscreen a 8 px, bajo el pedestal.
- **Tecla E:** encima del artículo; el juego la sube y baja 1 px. El cuadro 1 al pulsar.
- La vista previa usa la fuente de la maqueta de 3×5; en el juego va Silkscreen.

`gif/` tiene cada animación por separado (ampliada, sobre el suelo `#16352e`).
`vista_previa_tienda.png`: la sala ×3, la tira de animaciones y el minimapa con la tienda.
`_darwin_x8.png`: Darwin a tamaño real y a ×8 para revisarlo.
