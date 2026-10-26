# Jefes del piso 2 (océano cámbrico)

Generados con `generar_anomalocaris.py` y `generar_euripterido.py` (comparten `base_piso2.py`; `python <script>`, necesita Pillow). Mismo estilo que los enemigos del piso 2: 16 px por unidad, luz arriba a la izquierda, contorno oscuro.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, celda **112×112** en las hojas del Anomalocaris y **128×128** en las del Euriptérido, pivote *Center*. Vistos desde arriba, miran a la derecha; para la izquierda, *Flip X*.

## Anomalocaris (`Anomalocaris/`)

Unos 75 px de largo con la cola (≈ 4,7 u). Rojo con ojos en pedúnculo, para leerse en la sala oscura.

| Hoja | Cuadros | ms | Uso |
|---|---|---|---|
| `Anomalocaris_Swim` | 6 | 110 | Acecho de la fase 1 (bucle): las aletas ondulan, los apéndices enroscados |
| `Anomalocaris_SwimFast` | 4 | 70 | Cerco en espiral de la fase 2 (bucle) |
| `Anomalocaris_ClawWarn` | 5 | 140 | Aviso de 0,7 s: se echa atrás y abre los apéndices, ojos rojos |
| `Anomalocaris_ClawStrike` | 5 | 60 | Zarpazo: se lanza y cierra los apéndices; **daño en el cuadro 1** |
| `Anomalocaris_Vulnerable` | 4 | 250 | 1 s vulnerable tras el zarpazo: apéndices caídos, jadea burbujas (bucle) |
| `Anomalocaris_Charge` | 4 | 90 | Fase 3: se para y carga, tiembla (bucle; los dos zarpazos usan `ClawStrike`) |
| `Anomalocaris_Hurt` | 2 | 90 | Daño |
| `Anomalocaris_EyesDark` | 4 | 220 | Cinemática al 25 %: solo los ojos brillando y una sombra apenas visible; el cuadro 3 es un parpadeo |
| `Anomalocaris_Death` | 8 | 140 | Se apaga, se ladea, la arena lo cubre y queda fosilizado |

## Euriptérido (`Eurypterid/`)

Unos 90 px de largo con la cola (el abdomen se estrecha poco a poco hasta la cola, de una pieza). Fase 1 acorazada en verde bronce (púas en los segmentos, que son su arma a distancia; borde del escudo claro = frente blindado); fase 2 blanda en rosa.

| Hoja | Cuadros | ms | Uso |
|---|---|---|---|
| `Eurypterid_Walk` | 6 | 100 | Fase 1, andar (bucle) |
| `Eurypterid_PincerWarn` | 4 | 150 | Aviso de las pinzas: las abre y se echa atrás |
| `Eurypterid_PincerStrike` | 4 | 60 | Pinzas: **daño en el cuadro 1** |
| `Eurypterid_SpineWarn` | 4 | 150 | Las púas de los bordes del caparazón se erizan, crecen y se cargan de veneno (punta blanca en los cuadros 2-3); se encoge un poco |
| `Eurypterid_SpineShoot` | 3 | 80 | Abanico de 3 púas hacia el jugador: **salen en el cuadro 0** (las púas del caparazón se sueltan y dejan una nube de veneno). La cola no se usa en la fase 1: se estrena en la fase 2 |
| `Eurypterid_Hurt` | 2 | 90 | Daño en la fase 1 |
| `Eurypterid_Molt` | 12 | 250 | Cinemática de muda (3 s): tiembla, el escudo se raja y sale el cuerpo blando. **Al final el jefe está 16 px (1 u) más a la derecha**: crear la `Exuvia` en la posición original y mover el jefe 1 u hacia delante |
| `Eurypterid_Exuvia` | 1 | — | La muda vacía que queda como cobertura |
| `Eurypterid_Walk_Soft` | 6 | 60 | Fase 2, andar el doble de rápido (bucle) |
| `Eurypterid_TailWarn` | 4 | 150 | Aviso del barrido: lleva la cola a un lado, la punta se enciende |
| `Eurypterid_TailSweep` | 5 | 50 | Barrido de 180° por detrás con estela venenosa (usar con `Fx_Eurypterid_TailSweep`) |
| `Eurypterid_DoublePincer` | 6 | 70 | Doble pinza: **daño en los cuadros 1 y 3** (una pinza y luego la otra) |
| `Eurypterid_Hurt_Soft` | 2 | 90 | Daño en la fase 2 |
| `Eurypterid_Death` | 8 | 140 | Se encoge, se ladea, la arena lo cubre y queda fosilizado |

## Efectos (`Effects/`)

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Fx_Anomalocaris_Bubbles` | 6 | 16×16 | Estela de burbujas al nadar (soltar detrás cada poco) |
| `Fx_Anomalocaris_Swipe` | 5 | 64×64 | Tajo del zarpazo, hacia la derecha con el origen en el borde izquierdo (0, 32) |
| `Fx_Anomalocaris_ClawArea` | 4 | 64×64 | Fase 3: zona donde caerá el zarpazo (2 u de radio), en bucle mientras dura el marcador |
| `Fx_Anomalocaris_ClawPath` | 4 | 16×8 | Fase 3: trayectoria del zarpazo, en mosaico (*Draw Mode: Tiled*) hacia la derecha |
| `Proj_EurypteridSpine` | 2 | 16×8 | Púa venenosa, hacia la derecha |
| `Fx_Eurypterid_TailSweep` | 5 | 128×128 | Media luna venenosa de 3 u de radio que barre la mitad de atrás del jefe (la izquierda del dibujo), centrada en él |

`vista_previa_*.png` y `gif/` son solo para revisar (el Anomalocaris sobre el fondo de la sala oscura, el Euriptérido sobre el de la cueva).
