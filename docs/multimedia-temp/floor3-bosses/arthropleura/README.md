# Arthropleura (jefe del piso 3)

Generado con `generar_arthropleura.py` (skill primordia-sprites; numpy):

```
python generar_arthropleura.py todo       # hojas de la 6a en Sheets/, vistas previas y GIF
python generar_arthropleura.py muestra    # muestra del aspecto (para revisar cambios rápido)
python generar_escala_arthropleura.py     # láminas de escala del paso 0 (y "grande": la versión aprobada)
```

Milpiés gigante en **picada** (cámara a 55°, como el Diplocaulus), rediseñado con la foto de referencia del usuario:
quitina de **cobre oxidado** (con textura: estrías, poros oscuros y desgaste claro en los bultos, distinta en cada placa), placas planas y anchas que se solapan, patas largas con anillos, cabeza de escudo con ojos
negros y colmillos en hoz. Modelado con superficies de altura, tubos y esferas en un z-buffer (misma luz y escalones
de sombra que `floor3-bodies/generar_cuerpos_piso3.trace()`).

## Tamaño y cadena (aprobado)

- **28 piezas de tronco** + cabeza + cola, **a 2,6 u de paso** (≈ 78 u de largo). Cada pieza mide 64 px (4 u) de
  ancho sin patas y lleva **2 placas** (56 placas a la vista).
- **Pivote = centro de la celda = nudo de la cadena.** El código calcula la cadena en coordenadas de suelo y
  **multiplica la y de pantalla por 0,82** (sen 55°), porque los sprites están dibujados en picada.
- **Orden de dibujo:** de la cola a la cabeza (cada placa tapa a la de detrás).
- **Ola de patas:** cada segmento usa `Walk` desfasado **un cuadro por pieza** (pieza i → cuadro (t + i) mod 6).
- Sala de la F2: suelo de 60×33 u; cámara ×1 (o ×1,5 si al probar se ve mal) y ×1,5 solo en la F3.

## Direcciones

16 direcciones: **9 dibujadas** (filas de cada hoja, de arriba abajo) y las otras 7 con *Flip X*.

| Fila | Nombre | Ángulo (0° = derecha, 90° = arriba) | Para mirar a la izquierda |
|---|---|---|---|
| 0 | `Up90` | 90° | — |
| 1 | `Up68` | 67,5° | 112,5° = fila 1 + Flip X |
| 2 | `Up45` | 45° | 135° = fila 2 + Flip X |
| 3 | `Up23` | 22,5° | 157,5° = fila 3 + Flip X |
| 4 | `Side` | 0° | 180° = fila 4 + Flip X |
| 5 | `Dn23` | -22,5° | -157,5° = fila 5 + Flip X |
| 6 | `Dn45` | -45° | -135° = fila 6 + Flip X |
| 7 | `Dn68` | -67,5° | -112,5° = fila 7 + Flip X |
| 8 | `Dn90` | -90° | — |

## Hojas (`Sheets/`, una fila por dirección)

Importar: *Multiple*, 16 PPU, *Point*, sin compresión, pivote *Center*.

| Hoja | Cuadros | Celda | ms | Cuadro que cuenta / uso |
|---|---|---|---|---|
| `Arthropleura_Head_Move` | 6 | 160×160 | 110 | Anda: antenas que tantean, patas del cuello |
| `Arthropleura_Head_Open` | 6 | 160×160 | 90 | Abre y cierra los colmillos (amenaza); boca granate |
| `Arthropleura_Head_Rear` | 6 | 160×160 | 110 | **Se alza** con las patas delanteras al aire y ruge (asomar en la F1, transiciones, antes de embestir). Rugido: cuadro 3 |
| `Arthropleura_Head_ChargeWarn` | 4 | 160×160 | 120 | Aviso de embestida: se echa atrás con los colmillos abiertos. Al acabar → `Charge` |
| `Arthropleura_Head_Charge` | 4 | 160×160 | 60 | Embiste (daña desde el cuadro 0) |
| `Arthropleura_Head_Eat` | 8 | 160×160 | 100 | Come un helecho: **el bocado es el cuadro 1** (el helecho desaparece; si ardía, se quema) |
| `Arthropleura_Head_Hurt` | 2 | 160×160 | 80 | Blanco y rojo (RED4), desde `Move` |
| `Arthropleura_Segment_Idle` | 4 | 112×112 | 200 | Quieto (respira) |
| `Arthropleura_Segment_Walk` | 6 | 112×112 | 90 | Anda: 4 pares de patas en ola (desfase de un cuadro por pieza) |
| `Arthropleura_Segment_Hurt` | 2 | 112×112 | 80 | Blanco y rojo |
| `Arthropleura_Tail_Idle` / `_Walk` / `_Hurt` | 4 / 6 / 2 | 112×112 | 200 / 90 / 80 | Cola: placa, pigidio y dos espinas color cuero |

La cabeza llega a 25-27 colores por cuadro cuando se ve la boca (ojos, boca, patas, colmillos y placas en una pieza de
160 px): aceptado a propósito.

## Paleta (`PALS` en el generador; en la skill, `references/pisos.md`)

Placas `#1f120e #43241a #6e3a22 #9c5a34 #c88a58` · borde trasero, anillos y colmillos `#170d0b #2e1a14 #4a2a1c` ·
cuero (filos, pinchos, puntas) `#4a2a1a … #f0d0a0` · patas `#2a1510 #5a2c18 #9a4e26 #c8743a #e8a868` · ojos
`#08060a … #e8dcd4` con reflejo rojo `#c0302a` · boca `#22080f … #c8687a` · vientre `#3a2418 … #d8aa80`.

## 6b: estados, placas abiertas, rueda y muerte

**Daño (decisión del usuario):** el caparazón es invulnerable todo el combate; solo entra daño en las **placas abiertas**.
En F1 y F2 el fuego abre las placas del tramo que arde; en la F3 se abren solas por **tramos de 2-3 piezas** cuya carne
late (flecha rosa en el borde si están fuera de cámara).

**Capas** (`Sheets/Arthropleura_<Pieza>_<Anim>_<Capa>.png`): van ENCIMA de la pieza, con la misma celda, filas y cuadros
que su animación (`Head`: Move, Open, Rear, ChargeWarn, Charge, Eat; `Segment` y `Tail`: Idle, Walk). Se combinan.

| Capa | Uso |
|---|---|
| `Burning` | Arde: focos de llamas sobre las placas (pasa de pieza en pieza como una mecha; el código la enciende con retardo) |
| `Charred` | Chamuscado tras arder: placas de carbón con grietas de ascuas que laten; se queda el resto del combate |
| `Cracks1` | Grietas al 70 % de vida |
| `Cracks2` | Grietas al 35 % (sustituye a `Cracks1`) |

Con las placas abiertas, el código oculta `Charred` y las grietas (la carne no se quema ni se agrieta).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Arthropleura_Segment_PlatesOpen` | 4 | 112×112 | 90 | Las 2 placas se levantan por detrás (bisagra delante) y enseñan la carne rosada |
| `Arthropleura_Segment_OpenWalk` | 6 | 112×112 | 90 | Abierta andando; la carne late (punto débil: aquí entra el daño) |
| `Arthropleura_Segment_PlatesClose` | 4 | 112×112 | 90 | Se cierra |
| `Arthropleura_Roll` | 8 | 144×144 | 60 | Último 10 %: rueda de ~8 u (dos vueltas de placas) que rueda hacia delante; bucle. **Pivote = centro de la rueda; el suelo está 26 px por debajo** (sombra incluida) |
| `Arthropleura_Head_Death` | 8 | 160×160 | 110 | Espasmo alzado con los colmillos abiertos, cae y se apaga en gris (cuadros 6-7) |
| `Arthropleura_<Head\|Segment\|Tail>_Dead` | 1 | 160 / 112 | — | Muerto: gris ceniza con las patas encogidas. El código enrosca la cadena en espiral, cambia cada pieza a `Dead` de la cola a la cabeza y pone humo |
| `Effects/Fx_Arthropleura_DeathSmoke` | 6 | 24×40 | 120 | Humo gris que sube de las piezas muertas (bucle) |

Las hojas con la boca o la carne a la vista llegan a 25-27 colores por cuadro: aceptado.

## 6c-1: efectos del combate y escenario (`generar_efectos_arthropleura.py`)

```
python generar_efectos_arthropleura.py
```

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Effects/Fx_Arthropleura_Stomp` | 5 | 32×24 | 70 | abajo | Pisotón de las patas (grieta en estrella y polvo): avisa de la ola, junto al segmento que la lanza |
| `Effects/Fx_Arthropleura_Shockwave` | 3 | 16×8 | 80 | — | Onda expansiva (F2): tramo que el código repite a lo largo de un **arco de ~140° hacia el jugador** (LineRenderer, textura en mosaico; elipse achatada ×0,82 por la picada) y cuyo radio crece; **3 crestas por pisotón con 0,3 s entre ellas**, se apagan al alejarse. Sustituye a la ola de tierra |
| `Sheets/Arthropleura_CanopyRidge` | 4 | 112×112 | 140 | centro = nudo | F1: **cresta continua de frondas** sobre la espesura donde va el cuerpo escondido: un tramo por pieza escondida, en su posición y dirección (filas Up90…Dn90, Flip X), de la cola a la cabeza; los tramos se solapan y su textura se repite cada 2,6 u, así que forman un solo lomo. Se ve solo sobre la espesura (máscara tramada invertida). Generado con `generar_arthropleura.py maleza` |
| `Sheets/Arthropleura_CanopyRidge_Cap` | 4 | 112×112 | 140 | centro = nudo | Remate redondeado de la cresta: delante de la cabeza (dirección de la cabeza) y detrás de la cola (dirección + 180°) |
| `Effects/Fx_Arthropleura_EdgeBreak` | 8 | 96×80 | 80 | arriba (48, 0) | F1: el borde de la espesura revienta hacia dentro del claro (dibujado para el borde de arriba; girar 90/180/270° exactos para los otros). **Cuenta el 1**: se abren los tiles de la brecha (≥ 5 u) |
| `Smoke/Breach_Top` / `_Side` / `_Bottom` | 1 | 96×64 / 64×96 / 96×48 | — | — | Restos de la brecha encima de los tiles abiertos: flecos rotos, tocones y frondas aplastadas (Side = borde izquierdo; Flip X para el derecho) |
| `Effects/Fx_Arthropleura_Tremor` | 4 | 48×24 | 90 | abajo | Presentación: piedrecitas que saltan con polvillo; se reparten por la sala mientras se acerca |
| `Effects/Fx_Arthropleura_CanopyShake` | 6 | 32×40 | 110 | arriba | F1: la espesura suelta hojas y ramitas por donde pasa el cuerpo escondido; bucle |
| `Effects/Fx_Arthropleura_ChargeAim` | 4 | 16×8 | 80 | — | Aviso de embestida: tramo que se repite a lo largo de la trayectoria (como `Fx_DragonflyAim`), chevrones de ascua que laten hacia delante |
| `Effects/Fx_Arthropleura_ChargeDust` | 6 | 48×32 | 70 | derecha (la cabeza) | Polvo y hojarasca al arrancar la embestida; mira a la derecha y sale hacia atrás (Flip X / girar con la cabeza) |
| `Effects/Fx_Arthropleura_WallBreak` | 9 | 96×96 | 80 | centro del tramo derribado | 70 %: la pared de vegetación revienta (frondas, ramas, terrones, polvo). **Cuadro que cuenta: 0** (el código ya cambia los tiles de pared por suelo) |
| `Smoke/Rubble_Brush_0..2` | 1 | 32×24 | — | abajo | Restos de la pared en el suelo (3 variantes; no bloquean) |
| `Smoke/Sigillaria_Fall` | 9 | 112×80 | 80 | pie del tronco (28, 70) | La Sigillaria derribada se parte por la base y cae a la derecha (Flip X a la izquierda). **Cuadro que cuenta: 6** (golpe: daña y deja el tronco) |
| `Smoke/Sigillaria_Fallen` | 1 | 112×80 | — | (28, 70) | Tronco tumbado: obstáculo nuevo el resto del combate (colisión a lo largo del tronco, ~3,5 × 1 u) |

El fuego que pasa de pieza en pieza usa la capa `Burning` (6b) con retardo y `Fx_Ignite` (tanda 5) al prender cada pieza.

## Escenas de prueba (`generar_escenas_arthropleura.py` → `escenas/`)

GIF a tamaño de cámara (480×270, ×2) que combinan la cadena, las capas y los efectos: F1 presentación, ola de tierra,
embestida contra una Sigillaria y fuego que abre las placas. Solo para revisar (los GIF no van a git).
**F1 (rehecha a petición del usuario):** claro de **28×16 u cerrado**, espesura de 6-7 u, cámara ×1,3 durante la F1 (pendiente de confirmar); las piezas escondidas se **hunden con trama** (máscara tramada de 1 u en el borde de la espesura) y sobre la copa va la cresta continua (`CanopyRidge`); el bicho revienta el borde (`EdgeBreak`) y entra por la brecha. **Lección para montar la sala de la F1:** los huecos de la espesura por donde asoma deben medir **≥ 5 u** de ancho
(el cuerpo mide 4 u más las patas); en la lámina de escala eran de 2 u.

## 6c-2: interfaz (`generar_interfaz_arthropleura.py`)

### Barra de vida (`BossBar/UI_BossBar_Arthropleura_*`)

El propio bicho visto desde arriba, mirando a la izquierda: cabeza con colmillos de cuero y antenas, 20 placas de cobre con
paranotos de punta dorada y cola con dos cercos dorados. Marco **320×32**; la vida rellena la **ventana de 260×8 en (46, 12)
desde la esquina de abajo a la izquierda del marco**, de izquierda a derecha (Image *Filled*, horizontal). Una placa del
relleno = 13 px = un segmento. Muescas centradas en el 70 % y el 35 % de la ventana (x = 46 + 260 × umbral, ancho 8).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Frame_Copper` | 1 | 320×32 | — | Marco de la F1 (cobre) |
| `Frame_Ember` | 3 | 320×32 | 120 | Marco de la F2: placas chamuscadas con grietas de ascuas que parpadean; bucle |
| `Frame_Open` | 2 | 320×32 | 260 | Marco de la F3: placas levantadas con la carne rosada que late; bucle |
| `Frame_Raw` | 2 | 320×32 | 200 | Marco del último 10 % (rueda): carne más viva; bucle |
| `Back` | 1 | 260×8 | — | Fondo de la ventana |
| `Fill_Copper` | 1 | 260×8 | — | Relleno F1 |
| `Fill_Ember` | 3 | 260×8 | 120 | Relleno F2 (ascuas); bucle |
| `Fill_Flesh` | 2 | 260×8 | 260 | Relleno F3 (carne rosada que late); bucle |
| `Fill_Raw` | 2 | 260×8 | 200 | Relleno de la rueda (carne viva con brillo húmedo); bucle |
| `Fill_Trail` | 1 | 260×8 | — | Barra fantasma (espera ~0,4 s y se vacía) |
| `Eyes_Hunt` | 1 | 320×32 | — | Ojos de la F1, encima del marco |
| `Eyes_Burn` / `Eyes_Flesh` | 2 | 320×32 | 260 | Ojos de la F2 (ascua) y la F3 (carmesí con halo); bucle |
| `Eyes_Roll` | 4 | 320×32 | 110 | Ojos en espiral de la rueda; bucle |
| `Notch` / `Notch_Broken` | 1 | 8×32 | — | Espina de quitina de un cambio de fase, **centrada en su umbral**, y rota |
| `Fx_NotchBreak` | 5 | 16×34 | 70 | La espina se rompe; centrada en el mismo punto |
| `Fx_WallBreak` | 6 | 320×32 | 80 | 70 %: astillas, piedras, hojas y polvo salen de la muesca del 70 % |
| `Fx_PlatesOpen` | 6 | 320×32 | 80 | 35 %: ola rosa que abre las placas de la cola a la cabeza |

Marco, relleno y ojos van juntos por fase (F1 Copper/Hunt, F2 Ember/Ember/Burn, F3 Open/Flesh/Flesh, rueda Raw/Raw/Roll).

### Flechas del borde (`EdgeArrows/UI_EdgeArrow_Arthropleura_*`)

Insignia redonda y puntero aparte. El puntero se coloca a ~21 px del centro de la insignia, hacia el objetivo.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Badge_Head` | 1 | 24×24 | — | Cabeza (aro rojo) |
| `Badge_HeadWarn` | 2 | 24×24 | 160 | Cabeza a punto de embestir (aro que parpadea); bucle |
| `Badge_Tail` | 1 | 24×24 | — | Cola (aro dorado) |
| `Badge_Weak` | 3 | 24×24 | 200 | Punto débil: carne rosada que late; bucle |
| `Pointer_Head` / `_Tail` / `_Weak` | 16 | 12×12 | — | Puntero: **cuadro 0 = derecha, antihorario cada 22,5°** |

Vistas: `vista_previa_6c2_barra.png`, `vista_previa_6c2_flechas.png`. Regenerar: `python generar_interfaz_arthropleura.py`.

Vistas previas: `vista_previa_6c1.png`, `vista_previa_6b_capas.png`, `vista_previa_6b_animaciones.png`, `vista_previa_6b_curva.png` (estados mezclados), `vista_previa_6a_animaciones.png` (lado), `vista_previa_6a_direcciones.png`, `vista_previa_6a_curva.png`
(las 16 direcciones encadenadas), `vista_previa_6a_cadena.png` y `vista_previa_escala_*.png`; `gif/` (no va a git).

## Arte final de la sala (2026-11-23, aprobado): espesura, brechas, caída y lodo

Generadores: `generar_espesura_arthropleura.py` (subtandas A-C) y `generar_lodo_arthropleura.py` (D). Tramo: **Bosque
humeante** (el código usa hoy `BandThemes[2]`, tierra quemada: cambiarlo a `BandThemes[1]`). Vistas: `vista_previa_espesura.png`,
`vista_previa_brechas*.png`, `vista_previa_caida*.png`, `vista_previa_lodo.png`, escena `escenas/Arthropleura_F2_lodo.gif`.
Sustituye a los tiles provisionales de la espesura y a `Smoke/Breach_*` (quedan sin uso; `Smoke/Rubble_Brush_*` sigue valiendo).

### A. Espesura (`Thicket/`, autotile como el lodo)

| Pieza | Uso |
|---|---|
| `Thicket_Inner_0..5` (16×16) | Interior. 0-3 = la tapa aprobada del tramo (`Wall_Cap`, `_1.._3`); 4 mata clara; 5 humo. Elegir por hash de celda |
| `Thicket_Edge_<m>_<c>` (46) + `_v1`/`_v2` en las que tienen cortina abajo | m = lados abiertos (N=1, E=2, S=4, O=8), c = esquinas abiertas (NE=1, SE=2, SO=4, NO=8; solo si sus dos lados están cerrados). Lo abierto es transparente (se ve el suelo) con sombra translúcida; abajo, cortina de frondas. Variante `v` por hash de celda (si no existe, la 0) |
| `Thicket_Crown_0..3` (40×40) y `Thicket_TreeCrown_0..2` (48×48) | Coronas de helecho y copas de Lepidodendron: sprites encima, centrados en bloques de 2×2 celdas cuyo entorno de 4×4 sea todo espesura, desplazados ±4 px por hash; ~4 de cada 5 bloques, 2 de cada 5 árboles. Se quitan si se rompe alguna de sus celdas |
| `Thicket_Tileset.png` + `.txt` | Todas las piezas en una hoja y su orden |

Sobre el suelo junto a la espesura, las solapas del tramo (`floor3-environment/Smoke/deco/Spill_*`), como en las paredes.

### B. Brechas (`Breach/`)

| Pieza | Uso |
|---|---|
| `Thicket_Break_<m>_<c>[_v]` | Capa encima del borde (misma máscara y variante) en la espesura que toca celdas abiertas por una embestida: tallos partidos con pulpa, frondas desgarradas, copa aplastada. El claro conserva el filo limpio |
| `Breach_Debris_Dense_0..3` / `_Light_0..3` (16×16) | Restos en las celdas de la brecha: densos si tocan espesura (8 vecinas), ligeros si no |
| `Breach_Track_0..1` (32×16, hacia la derecha) | Surcos de las patas: girar a la dirección de la embestida y poner cada 2 u por el eje de la brecha, solo en celdas de brecha |

### C. Caída de la F2 (`Fallen/`)

Solo en las celdas que eran espesura (todo `thicket`, pasillos de entrada incluidos); **los muros reales de la sala no
llevan nada**. `Fallen_Mat_0..3` (16×16, encajan en mosaico) en el interior; `Fallen_Edge_<m>_<c>` donde la máscara cuenta
como abiertas **solo las celdas de suelo que nunca fueron espesura** (claro, brechas) **y las de lodo** (los lagos que se
revelan); junto a los muros reales la alfombra sigue entera. `Fallen_Tree_0/1` (60×30, Flip X), `Fallen_Tree_V` (30×60) y
`Fallen_Fern_0/1` (40×28): decorado **sin colisión** donde había copas en la F1 (árbol → tronco caído; helecho → helecho
tumbado), desplazados ±6 px.

### D. Lodo (`Effects/`)

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Fx_Arthropleura_MudDouse` | 7 | 48×48 | 90 | (24, 33) | Una pieza ardiendo entra en el lodo y se apaga (`ExtinguishInMud`). **Cuenta el 0** |
| `Fx_Arthropleura_MudWake` | 6 | 128×80 | 100 | centro | Onda que se abre más allá del cuerpo; una cada ~0,3 s por pieza que avanza por el lodo, sin girar. **Recortarla al lodo** (no debe pisar la orilla) |
