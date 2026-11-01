# Enemigos y trampas del piso 3 (pantano carbonífero)

Generados con `generar_enemigos_piso3.py` (skill primordia-sprites): 16 px por unidad, luz arriba a la izquierda,
contorno `OUTL`, rampas de 5 tonos con luz rebotada, ojos enfadados (las trampas no tienen ojos).

```
python generar_enemigos_piso3.py . plants ground flyers
```

`plants` = 3a, `ground` = 3b, `flyers` = 3c. El escarabajo, la araña y la libélula reutilizan la cámara 3D del
vinagrillo (`floor3-bodies/generar_cuerpos_piso3.py`, a 30°), así que comparten vista, luz y tamaño con el jugador.
**Regla del piso:** todos los bichos con la vista lateral del jugador; solo lo plano (charcos, telarañas, avisos en el
suelo) se ve desde arriba.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, cortar por celda con la tabla.
Pivote *Bottom Center* para lo que está de pie en el suelo (plantas, sapo, bichos) y *Center* para voladores y efectos.
Todos miran a la derecha; para la izquierda, *Flip X*.

## Plantas (3a)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Calamites_Idle` | 4 | 32×48 | 180 | Madre inmóvil: mata de cañas sobre un rizoma con ojos; se mece (bucle) |
| `Calamites_Spawn` | 5 | 32×48 | 110 | El rizoma se hincha y se agrieta; **el retoño sale en el cuadro 3** |
| `Calamites_Spores` | 5 | 32×48 | 110 | Encoge las cañas y se encienden los estróbilos; **el anillo de esporas sale en el cuadro 2** |
| `Calamites_Hurt` | 2 | 32×48 | 90 | Daño |
| `Calamites_Death` | 7 | 48×56 | 120 | Se seca (verde → pardo → pálido) y las cañas caen hacia fuera |
| `CalamitesSprout_Emerge` | 4 | 16×24 | 90 | Brota de la tierra |
| `CalamitesSprout_Idle` | 4 | 16×24 | 180 | Se mece (bucle) |
| `CalamitesSprout_Move` | 4 | 16×24 | 100 | Persigue al jugador a saltitos (bucle) |
| `CalamitesSprout_Wither` | 4 | 16×24 | 140 | Se seca al morir la madre |
| `CalamitesSprout_Hurt` | 2 | 16×24 | 90 | Daño |
| `CalamitesSprout_Death` | 4 | 16×24 | 120 | Se seca y se deshace |
| `Medullosa_Idle` | 4 | 32×40 | 180 | Helecho con semillas; las frondas se mecen (bucle) |
| `Medullosa_Aim` | 4 | 32×40 | 150 | Echa atrás la fronda-catapulta; la semilla se pone ámbar |
| `Medullosa_Shoot` | 4 | 32×40 | 70 | La fronda salta adelante; **la vaina sale en el cuadro 1** |
| `Medullosa_Hurt` | 2 | 32×40 | 90 | Daño |
| `Medullosa_Death` | 6 | 40×48 | 120 | Las frondas caen y se seca |
| `CarnivorousPlant_Idle` | 4 | 40×40 | 180 | Trampa (no muere): boca abierta, se mece y mueve la lengua (bucle) |
| `CarnivorousPlant_Snap` | 4 | 40×40 | 50 | Lanza la cabeza y muerde; **atrapa en el cuadro 2** |
| `CarnivorousPlant_Chew` | 4 | 40×40 | 130 | Mastica (bucle mientras retiene 1 s o se come a un enemigo); el bulto baja por el tallo |
| `CarnivorousPlant_Open` | 4 | 40×40 | 110 | Vuelve a abrirse |
| `CarnivorousPlant_Wilt` | 4 | 40×40 | 160 | Se marchita al limpiar la sala (se queda en el último cuadro) |

## Bichos de suelo (3b)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Beetle_Idle` | 4 | 32×32 | 180 | Escarabajo bombardero quieto (bucle) |
| `Beetle_Move` | 4 | 32×32 | 100 | Anda (bucle) |
| `Beetle_Turn` | 3 | 32×32 | 70 | Se da la vuelta (¾, de frente, ¾ de espaldas); **al acabar, Flip X** |
| `Beetle_Warn` | 4 | 32×32 | 150 | Se agacha, abre los élitros y la punta del abdomen se pone al rojo |
| `Beetle_Spray` | 3 | 32×32 | 70 | Dispara hacia atrás (bucle mientras dura el cono) |
| `Beetle_Hurt` | 2 | 32×32 | 90 | Daño |
| `Beetle_Death` | 6 | 40×40 | 120 | Panza arriba pataleando y se apaga |
| `Spider_Hidden` | 4 | 24×12 | 200 | **Capa**: sombra y ojos rojos que miran y parpadean; va encima de un helecho del escenario, centrada en su base, mientras el helecho hace `Fern_*_Rustle` |
| `Spider_Reveal` | 4 | 32×32 | 70 | Sale de golpe del helecho |
| `Spider_Idle` | 4 | 32×32 | 180 | Quieta (bucle) |
| `Spider_Move` | 4 | 32×32 | 100 | Anda (bucle) |
| `Spider_Jump` | 6 | 40×40 | 80 | Se agacha, despega, vuela con las patas atrás y aterriza; **el salto empieza en el cuadro 1** |
| `Spider_Shoot` | 4 | 32×32 | 70 | Alza el abdomen; **la seda sale en el cuadro 1** |
| `Spider_Hurt` | 2 | 32×32 | 90 | Daño |
| `Spider_Death` | 5 | 40×40 | 120 | Panza arriba con las patas encogidas y se apaga |
| `Toad_Idle` | 4 | 32×32 | 180 | Sapo de fango respirando con el saco (bucle) |
| `Toad_Hop` | 4 | 32×32 | 90 | Salto (bucle al moverse) |
| `Toad_Spit` | 5 | 32×32 | 90 | Hincha la garganta y escupe; **la bola sale en el cuadro 3** |
| `Toad_Inflate` | 4 | 40×40 | 120 | Se hincha temblando antes de reventar (qué dispara esto se decide al programar) |
| `Toad_Dive` | 3 | 32×32 | 90 | Se zambulle en el lodo profundo (al revés: sale) |
| `Toad_Swim` | 4 | 32×32 | 160 | Nada en el lodo: solo asoman ojos y lomo (bucle) |
| `Toad_Hurt` | 2 | 32×32 | 90 | Daño |
| `Toad_Death` | 6 | 40×40 | 120 | Panza arriba y se apaga |

**Escarabajo:** el chorro sale del píxel **(3, 14)** de su celda de 32×32 (punta del abdomen, mirando a la derecha).

## Voladores (3c)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Dragonfly_Fly` | 4 | 40×40 | 50 | Libélula gigante: aleteo (bucle); vuela por encima de todo |
| `Dragonfly_Warn` | 4 | 40×40 | 150 | Fija la línea de ataque 0,7 s: morro arriba y el ojo brilla |
| `Dragonfly_Dive` | 3 | 40×40 | 90 | Picado con las alas atrás y rayas de velocidad (bucle) |
| `Dragonfly_Land` | 3 | 40×40 | 90 | Se posa (al revés: despega) |
| `Dragonfly_Perch` | 4 | 40×40 | 200 | Posada 0,8 s, vulnerable (bucle) |
| `Dragonfly_Hurt` | 2 | 40×40 | 90 | Daño |
| `Dragonfly_Death` | 5 | 40×40 | 120 | Se arruga, cae y se apaga |
| `Mayflies_Idle` | 6 | 40×40 | 180 | Enjambre de efímeras (trampa): 12 efímeras en una neblina tenue (bucle) |
| `Mayflies_Burn` | 6 | 40×40 | 80 | Solo con fuego o explosiones: prenden y caen como ascuas; no vuelve |

## Efectos y proyectiles (`Effects/`)

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Proj_CalamitesSpore` | 2 | 8×8 | Espora del anillo |
| `Fx_CalamitesSpore_Pop` | 4 | 12×12 | Espora que se deshace |
| `Proj_MedullosaSeed` | 4 | 12×12 | Vaina dando vueltas en su parábola (la sombra, `Fx_FlyerShadow`) |
| `Fx_MedullosaSeed_Target` | 4 | 24×24 | Aviso en el suelo donde caerá |
| `Fx_MedullosaSeed_Burst` | 5 | 24×24 | Se raja y salpica veneno; después, `Fx_PoisonPuddleSmall` de `floor3-bodies/Effects` |
| `Fx_BeetleSpray` | 6 | 56×28 | Cono ardiente de 3 u: nace en el borde izquierdo a media altura y va a la derecha (con *Flip X* sale por detrás del escarabajo); prende gas y charcos |
| `Fx_SpiderWeb_Appear` | 3 | 32×32 | Telaraña en el suelo (desde arriba) |
| `Fx_SpiderWeb_Idle` | 1 | 32×32 | Telaraña que frena |
| `Fx_SpiderWeb_Burn` | 5 | 32×32 | Arde desde el centro y desaparece |
| `Proj_SpiderSilk` | 2 | 10×10 | Bola de seda lanzada |
| `Proj_ToadMud` | 2 | 12×12 | Bola de lodo |
| `Fx_ToadMud_Splat` | 5 | 24×24 | Salpicón al caer; el charco que ralentiza es `Puddle_Sludge_*` del escenario |
| `Fx_Toad_Burst` | 5 | 48×48 | Reventón del sapo hinchado |
| `Fx_FlyerShadow` | 1 | 24×8 | Sombra de un volador o de un proyectil en parábola (el código la escala con la altura) |
| `Fx_DragonflyAim` | 4 | 8×4 | Tramo de la línea de aviso del picado: se repite a lo largo del recorrido (80 ms) |

`vista_previa_*.png`, `fondos_*.png` (sobre Pantano, Humeante y Quemada), `vista_previa_arana_escondida.png` y `gif/`
son solo para revisar.
