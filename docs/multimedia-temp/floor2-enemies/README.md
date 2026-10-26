# Enemigos del piso 2 (océano cámbrico)

Generados con `generar_enemigos_piso2.py` (`python generar_enemigos_piso2.py`, necesita Pillow), con el mismo estilo que los cuerpos del piso 2: 16 px por unidad, luz arriba a la izquierda, contorno oscuro y ojos enfadados.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, cortar por celda con la tabla. Pivote *Center* salvo la Ottoia. Todos miran a la derecha; para la izquierda, *Flip X* (la medusa y la esponja son simétricas).

| Enemigo | Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|---|
| Trilobite | `Trilobite_Move` | 4 | 24×24 | 120 | Anda (bucle). Visto desde arriba, céfalo blindado de acero delante |
| | `Trilobite_Warn` | 4 | 24×24 | 150 | Aviso de 0,6 s: se echa atrás, ojos rojos, rasca la arena, el filo brilla |
| | `Trilobite_Charge` | 3 | 32×32 | 70 | Embestida (bucle) con estela |
| | `Trilobite_Flip` | 3 | 24×24 | 60 | Al chocar se da la vuelta |
| | `Trilobite_Stunned` | 4 | 24×24 | 120 | Panza arriba 1 s con estrellitas (bucle) |
| | `Trilobite_Recover` | 4 | 24×24 | 70 | Se endereza |
| | `Trilobite_Hurt` | 2 | 24×24 | 90 | Daño |
| | `Trilobite_Death` | 7 | 32×32 | 120 | Se vuelca, se parte y queda fosilizado |
| BlueJelly | `BlueJelly_Move` | 4 | 24×24 | 120 | Late (bucle). Azul translúcida |
| | `BlueJelly_Warn` | 4 | 32×32 | 150 | Se llena y se encienden las 8 cargas alrededor |
| | `BlueJelly_Shoot` | 4 | 32×32 | 80 | Se contrae; **el anillo sale en el cuadro 0** |
| | `BlueJelly_Hurt` | 2 | 24×24 | 90 | Daño |
| | `BlueJelly_Death` | 6 | 32×32 | 120 | Se desinfla y se deshace en gotas |
| Ottoia | `Ottoia_Burrowed` | 4 | 32×32 | 160 | Bajo la arena, inmune: montículo con ondas (bucle) |
| | `Ottoia_Warn` | 4 | 32×32 | 200 | Aviso de 0,8 s: círculo rojo de 0,8 u de radio y la arena se agrieta |
| | `Ottoia_Attack` | 6 | 32×32 | 70 | Sale, abre la boca y **muerde en el cuadro 3** |
| | `Ottoia_Sink` | 4 | 32×32 | 90 | Se hunde y el agujero se cierra |
| | `Ottoia_Hurt` | 2 | 32×32 | 90 | Daño (solo fuera) |
| | `Ottoia_Death` | 6 | 32×32 | 120 | Cae sobre la arena y se cubre |
| Vauxia | `Vauxia_Idle` | 4 | 32×32 | 180 | Inmóvil, los tubos se mecen (bucle) |
| | `Vauxia_Heal` | 5 | 32×32 | 110 | Se hincha y brilla en verde; **el pulso sale en el cuadro 2** |
| | `Vauxia_Hurt` | 2 | 32×32 | 90 | Daño |
| | `Vauxia_Death` | 6 | 32×32 | 120 | Se desmorona en espículas |
| Pikaia | `Pikaia_Swim` | 4 | 16×16 | 110 | Nada ondulando (bucle), unos 12 px |
| | `Pikaia_Dash` | 3 | 24×24 | 60 | Pasada rápida en formación (bucle) |
| | `Pikaia_Hurt` | 2 | 16×16 | 90 | Daño |
| | `Pikaia_Death` | 5 | 16×16 | 120 | Panza arriba y se deshace en copos |

**Ottoia:** el agujero está en (16, 24) del lienzo de 32×32, así que el pivote es **(0,5; 0,25)** para que el gusano salga del punto donde está.

## Efectos y proyectiles (`Effects/`)

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Proj_BlueJelly` | 2 | 8×8 | Carga de la medusa, redonda (sin dirección) |
| `Fx_BlueJelly_Impact` | 4 | 16×16 | Al chocar la carga |
| `Fx_Vauxia_HealPulse` | 6 | 168×168 | Onda de curación hasta 5,2 u (83 px de radio), centrada en la esponja |
| `Fx_Vauxia_Shield` | 4 | 32×32 | Escudo sobre un enemigo curado (bucle) |
| `Fx_Vauxia_HealMotes` | 5 | 16×16 | Cruces verdes que suben sobre el curado |
| `Fx_Vauxia_Thread` | 4 | 8×8 | Cuenta del hilo de luz de la esponja al curado: se ponen en hilera a 1:1 cada 4 px; cada una se enciende y se apaga (60 ms por cuadro) |

`vista_previa_enemigos_piso2.png`, `vista_previa_franjas.png` (sobre Someras, Arrecife y Abismo), `vista_previa_efectos.png` y `gif/` son solo para revisar.
