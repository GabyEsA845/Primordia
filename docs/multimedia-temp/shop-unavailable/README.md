# Señal de "no disponible" (tienda)

Generado con `generar_no_disponible.py` (Python + Pillow), que reutiliza `shop/generar_tienda.py`
(y lo que carga: `generar_mapa.py`, `generar_menus.py`, `generar_ui.py`, `generar_mutaciones.py`,
`generar_jugador.py`, `generar_escenario.py`).

```
python generar_no_disponible.py [carpeta_multimedia_temp] [carpeta_salida]
```

16 px por unidad, filtro Point, sin compresión. Hojas en fila, cuadro 0 a la izquierda.
Se colocan donde iba el artículo (flotando sobre el pedestal, centradas en x) con
`Item_Mutation_Shadow` debajo; el juego las sube y baja 1 px.

| Hoja | Cuadros | Lienzo | Punto central | Duración por cuadro |
|------|---------|--------|---------------|---------------------|
| `Shop_Unavailable.png` | 4 (bucle) | 16×16 | Centro (8, 8). La señal ocupa 13×13 (15×15 con contorno) | 150 ms |
| `Shop_Unavailable_Sign.png` | 4 (bucle) | 16×16 | Pivote abajo en el centro (8, 15) | 150 ms |

- **Principal:** lente de vidrio rojizo con barra diagonal (arriba izquierda → abajo derecha).
  Cuadro 0 quieto; 1-3 un brillo cruza el vidrio y el rojo late un poco. El contorno no cambia.
- **Alternativa:** etiqueta de pergamino con una X roja pintada, clavada en un palito de madera
  de naufragio. Se mece 1 px (0, +1, 0, −1); la base del palito no se mueve.

## Paleta

| Uso | Colores |
|-----|---------|
| Contorno | `#181425` |
| Vidrio rojo (rampa) | `#5a1c28` `#8e3040` `#bc5464` `#e8a0a8` |
| Interior del vidrio | `#5a1c28` al 51-59 % de opacidad |
| Brillo | `#ffffff`, `#e8a0a8` (destello al 55 %) |
| Pergamino | `#a89a7a` `#d8ccaa` `#f2ead0` `#fffaf0` |
| Pintura de la X | `#6a2030` `#a03a48` `#c85a64` |
| Palito (madera de naufragio) | `#2e1c16` `#4e3022` `#74482e` |
| Clavo | `#8a7a6a` |

`vista_previa_no_disponible.png`: a ×4 sobre `#16352e`, el pedestal a la venta, vacío con la
señal principal y vacío con el letrero (cada uno con su sombra), y los cuadros de cada animación.
