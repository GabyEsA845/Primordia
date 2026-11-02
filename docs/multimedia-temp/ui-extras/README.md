# Arte pendiente de room-hearts, teleports y floor12-fixes

Común a todos los pisos. Lo dibuja `generar_ui_extras.py` (skill primordia-sprites), que toma el medio corazón y el corazón lleno de `../ui/HUD/` para que los píxeles casen con el HUD.

Para regenerar: `python generar_ui_extras.py [carpeta_salida]`. Escribe las hojas, `vista_previa_ui_extras.png`, `vista_previa_mapa_iconos.png` y `gif/` (con `escena_ui_extras.gif`, la escena de revisión).

Ajustes de importación: 16 PPU, filtro Point, sin compresión, hojas en fila con el cuadro 0 a la izquierda. Los iconos del mapa y el corazón del HUD van en la interfaz (480×270, 1 px de interfaz por píxel).

## Hojas

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Items/Item_RoomHeart_Idle` | 8 (bucle) | 16×16 | 130 | Medio corazón en el suelo. Brillo diagonal en los cuadros 4-6. El vaivén (1 px) lo hace el código; pivote en el centro de la celda |
| `Items/Item_RoomHeart_Shadow` | 1 | 16×6 | – | Sombra fija, unos 8 px por debajo del centro del corazón; no se mueve con el vaivén |
| `Items/Item_RoomHeart_Collect` | 6 | 16×16 | 60 | Al recogerlo: el cuadro 1 destella en blanco y luego estalla en chispas. **Cuenta el cuadro 0** (cura al tocarlo); el resto es efecto |
| `Items/Item_RoomHeart_Fly` | 4 (bucle) | 16×16 | 70 | Vuelo a la vaina de la tienda (piso 3): medio corazón pequeño mirando a la derecha con estela detrás. El código lo gira hacia donde vuela |
| `HUD/UI_Heart_BarrierFull` | 1 | 16×16 | – | Corazón verde entero (dos medios de Pastar biomasa); la misma paleta que `UI_Heart_Barrier` |
| `Map/UI_Map_Heart` | 1 | 7×7 | – | Sala con un medio corazón sin recoger: mitad roja y mitad vacía, como en el HUD. Centro (3, 3) |
| `Map/UI_Map_Teleport` | 4 (bucle) | 7×7 | 120 | Teletransporte activo: remolino aguamarina que gira (la luz común de los teletransportes). Centro (3, 3) |
| `Map/UI_Map_Teleport_Hover` | 4 (bucle) | 7×7 | 90 | Lo mismo, un tono más claro, con el ratón encima |
| `Map/UI_Map_Teleport_Dim` | 1 | 7×7 | – | Apagado: no se puede viajar (sala sin limpiar) |
| `Map/UI_Map_Return` | 1 | 9×9 | – | Marca de regreso: flecha curva ámbar. Es de 9×9 como `UI_Map_Boss` porque a 7×7 la curva no se leía. Centro (4, 4) |
| `Map/UI_Map_Return_Hover` / `_Dim` | 1 / 1 | 9×9 | – | Resaltada y apagada |
| `Effects/Fx_PurpleHeart_Aura` | 8 (bucle) | 32×16 | 150 | Resplandor ligero bajo el jugador mientras lleva el corazón morado. Centro de la celda a los pies, detrás del jugador |
| `Effects/Fx_PurpleHeart_Mote` | 6 | 4×8 | 110 | Mota que sube; el código suelta 2-3 de vez en cuando en puntos al azar del resplandor |
| `Effects/Fx_PurpleHeart_Block` | 8 | 48×48 | 50 | Onda al parar un golpe: anillo fino que se abre y se apaga. Centro en el jugador. El destello morado de pantalla sigue siendo del código |

## Decisiones

- El aura es deliberadamente ligera (pedido del usuario): visible a simple vista, sin tapar al jugador ni la pantalla.
- Iconos del mapa con contorno `OUTL`, como `UI_Map_Reward` y `UI_Map_Boss` (`../map/`).
