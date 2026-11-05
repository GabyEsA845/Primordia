# Salas especiales del piso 3: Veta de carbón y Bosque en humo

Regenerar: `python generar_salas_especiales.py`. Importar: *Multiple* (o *Single*), 16 PPU, *Point*, sin compresión.

## Veta de carbón (`coal/`)

Franja de carbón de 1-2 casillas que cruza la sala de pared a pared. Al prender arde fuerte (muro de fuego que parte
la sala), luego queda en brasas que **se pueden cruzar** y al final, apagada.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `<Tramo>/Coal_<0-15>`, `Coal_Corner_{NE,NO,SE,SO}` | 1 | 16×16 | | Tiles de la veta: **misma máscara y orden que `Mud_*`** (vecinos que NO son veta: N=1, E=2, S=4, O=8). Carbón negro en facetas con destellos fríos; terrones sueltos en la orilla. Uno por tramo (cambia el suelo de la orilla) |
| `Coal_Ignite` | 6 | 16×32 | 90 | Capa encima de cada casilla al prender (la casilla es la **mitad de abajo**; pivote abajo-centro): grietas que se encienden y primeras llamas |
| `Coal_Burn_{0,1,2}` | 6 | 16×32 | 90 | Arde (bucle; 3 variantes para que las casillas vecinas no repitan): lenguas que suben por encima de la casilla. Mientras dura, daña y bloquea |
| `Coal_Embers` | 6 | 16×16 | 140 | Brasas (bucle): las grietas laten en naranja; ya se cruza (decidir en código si quema un poco) |
| `Coal_Out` | 1 | 16×16 | | Apagada: carbón ceniciento con grietas grises (para siempre) |

## Bosque en humo (`smoke/`)

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Fx_SmokeLayer_A` / `_B` | 1 | 128×128 | Capas de humo que **se repiten sin costura** (tilear y desplazar despacio, B más rápida y más tenue, en sentidos algo distintos). A cubre toda la sala (base densa); B añade volutas |
| `UI_SmokeVisionMask` | 1 | 160×160 | Máscara del claro alrededor del jugador: alfa 0 en el centro (≈ 5 casillas de diámetro) y opaca fuera, con 4 escalones de trama. Recortar las capas de humo con ella (multiplicar su alfa por el de la máscara), centrada en el jugador |
| `Fx_SmokeWisp` | 8 | 48×24 | Voluta suelta que nace, se estira y se deshace (140 ms), por encima del humo de vez en cuando |
| `Fx_FloatingEmbers` | 8 | 32×48 | Ascuas que suben despacio (140 ms, bucle), repartidas por la sala por encima del humo |

`maqueta_veta_<tramo>.png` (la veta con sus estados repartidos a lo largo: apagada arriba, brasas, arde, prende e
intacta abajo) y `gif/escena_veta.gif`; `maqueta_humo.png` y `gif/escena_humo.gif`.
