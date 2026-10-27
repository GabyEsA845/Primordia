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

## Barra de vida del Anomalocaris (`BossBar/`)

Generada con `generar_barra_anomalocaris.py` (usa la skill primordia-sprites). La barra es el propio animal visto desde arriba, mirando a la izquierda: la cabeza en punta con las fauces de garras y los
ojos con ceño, el caparazón a lo largo y la cola. Escala de interfaz: un píxel del dibujo = un píxel de la interfaz (480×270).
Todas las hojas empiezan por `UI_BossBar_Anomalocaris_`. La vida rellena la **ventana de 200×8, en (38, 12) desde la esquina
de abajo a la izquierda del marco**, de izquierda a derecha (Image *Filled*, horizontal).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Frame` / `FrameDark` | 1 | 258×32 | — | Marco con la ventana vacía; `FrameDark` es la silueta mientras es inmune (escena de los ojos y silueta del frenesí) |
| `Back` | 1 | 200×8 | — | Fondo de la ventana |
| `Fill_Stalk` / `Fill_Encircle` | 1 | 200×8 | — | Relleno del acecho (rojo) y del cerco (naranja) |
| `Fill_Frenzy` | 2 | 200×8 | 150 | Relleno del frenesí (carmesí), el cuadro 1 con brillos; bucle |
| `Fill_Immune` | 1 | 200×8 | — | Relleno mientras es inmune (gris violáceo) |
| `Fill_Trail` | 1 | 200×8 | — | Barra fantasma: la vida recién perdida (espera ~0,4 s y se vacía) |
| `Eyes_Stalk` / `Eyes_Encircle` | 1 | 258×32 | — | Ojos encima del marco: medio cerrados en el acecho, abiertos con ceño en el cerco |
| `Eyes_Frenzy` | 2 | 258×32 | 150 | Rojos; el cuadro 1 brilla con halo; bucle |
| `Eyes_Dark` | 2 | 258×32 | 150 | Amarillos en la oscuridad; el cuadro 1 es un parpadeo |
| `Notch` / `Notch_Broken` | 1 | 8×32 | — | Placa de hueso de un cambio de fase, **centrada en su umbral** (60 % y 25 %), y rota |
| `Fx_NotchBreak` | 5 | 16×34 | 70 | Se agrieta, destella y suelta esquirlas; centrada en el mismo punto que la placa |

La importa **Primordia > Boss > Crear Anomalocaris y escena de prueba** (asset `Data/Bosses/BossBar_Anomalocaris.asset`,
componente `BossBarAppearance` en el prefab del jefe).

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

## Sala del Anomalocaris (`AnomalocarisRoom/`) y su burbuja

Generadas con `generar_sala_anomalocaris.py` (skill primordia-sprites). Paleta del Abismo: las algas son una silueta oscura que tapa; las plantas, cian, dan casi toda la luz de la sala.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Deco_AlgaeColumn_A` / `_B` | 4 | 64×96 | 200 | Columna de algas (marca `A`), bucle. **Pivote en la base: (32, 92) px = (0,5; 0,042)**. Se dibuja por encima de los personajes, sin colisión; translúcida si el jugador está debajo |
| `Deco_LightPlant` | 4 | 32×32 | 220 | Planta luminosa (marca `L`), bucle; pivote (16, 28) = (0,5; 0,125). Luz 2D cian `#6ae8f0`, unas 4 u, que late con ella |
| `Deco_LightPlant_Off` | 3 | 32×32 | 120 | Se apaga (escena de los ojos); al revés, se enciende |
| `Effects/Proj_AnomalocarisBubble` | 4 | 16×16 | 120 | Burbuja que daña (estela del acecho y muro del cerco), bucle |
| `Effects/Fx_AnomalocarisBubble_Pop` | 4 | 16×16 | 60 | La burbuja revienta (al tocar al jugador o al recibir un disparo) |

`vista_previa_sala_anomalocaris.png` y `gif/` son para revisar.

## Sala del Euriptérido (`EurypteridRoom/`)

Generada con `generar_sala_euripterido.py` (`python generar_sala_euripterido.py`). Las 2 rocas de la marca `R` que se
derrumban con la muda. Parte de la roca grande del Abismo (`environment/Abyss/deco/Obstacle_RockBig.png`), así que es igual
que las demás salvo por las grietas.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Obstacle_RockCracked` | 1 | 32×32 | — | Roca grande con grietas finas (marca `R`): bloquea como las demás; el juego le crea su contorno de cobertura |
| `Fx_RockCollapse` | 10 | 48×48 | 90 | Derrumbe (solo visual): tiembla (0-2), **se parte en el cuadro 3** (deja de bloquear), los trozos caen entre el polvo y el último cuadro ya son los escombros. Centrado en la roca |
| `Deco_Rubble` | 1 | 32×32 | — | Escombros que se quedan en el suelo, sin colisión |
| `Fx_RockFallShadow` | 6 | 32×32 | 133 | Sombra de aviso de la piedra del techo (borde rojo discontinuo), crece en 0,8 s hasta 1 u de radio; una vez |
| `Proj_FallingRock` | 4 | 24×24 | 80 | Piedra que cae girando (bucle); el juego la baja desde 9 u de altura en los últimos 0,35 s |
| `Fx_RockImpact` | 6 | 48×48 | 70 | La piedra se rompe al tocar el suelo (el daño, al empezar); no deja restos |
| `Fx_FallingPebble` | 3 | 8×8 | — | Chinas decorativas que caen por la pantalla (una forma por cuadro), sin daño |
| `Fx_PebblePuff` | 4 | 16×16 | 70 | Polvillo de una china al tocar el suelo |
| `Fx_FloorCrack` | 8 | 48×48 | 250 / 120 | Zona Z de 3×3 casillas: **cuadros 0-5 = aviso de 1,5 s** (grietas que se abren); al terminar el suelo pasa a fosa y se ven los cuadros 6-7 (se hunde) |

`vista_previa_sala_euripterido.png` y `gif/Fx_RockCollapse.gif` son para revisar.

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
