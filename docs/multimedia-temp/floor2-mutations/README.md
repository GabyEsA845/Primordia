# Mutaciones del piso 2 (las 6 que no son Bocas)

Generadas con `generar_mutaciones_piso2.py` (`python generar_mutaciones_piso2.py`, necesita Pillow), por tandas. Las capas
se calculan desde los sprites de `../floor2-bodies/` cuadro a cuadro, como la Cutícula de `../clam-mutations/`: si se rehace
un cuerpo, se vuelve a lanzar.

16 px por unidad, filtro Point, sin compresión. Hojas en fila, cuadro 0 a la izquierda. `side` mira a la derecha (para la
izquierda se voltea). Sin capas en Dodge, Hurt ni Death. Las Extremidades van **detrás** del cuerpo (solo se pinta fuera de
su silueta).

## Tanda 1: Extremidades

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Limbs/Limbs_TailPropulsor_<Cuerpo>_<Idle/Move/Attack>_<side/down/up>.png` | 4 / 4 / 6 | 48×48 | Cola propulsora: aleta turquesa de dos lóbulos que ondea detrás del cuerpo. Centro en (24, 24) |
| `Limbs/Limbs_HallucigeniaSpines_<Cuerpo>_<…>_<…>.png` | 4 / 4 / 6 | 48×48 | Espinas de Hallucigenia: espinas de hueso en el lomo (en el lirio de mar, a los lados del cáliz y del tallo, porque arriba están los brazos) |
| `Effects/Fx_TailPropulsor_Bubble.png` | 4 (bucle) | 12×12 | Burbuja que deja la segunda esquiva (2 de daño, empuja). 120 ms |
| `Effects/Fx_TailPropulsor_BubbleBig.png` | 4 (bucle) | 16×16 | Burbuja del Torbellino (nivel III, 3 de daño). 120 ms |
| `Effects/Fx_TailPropulsor_Pop.png` | 4 | 16×16 | Estallido de una burbuja. 60 ms |
| `Effects/Fx_HallucigeniaSpine_Ground.png` | 8 | 12×12 | Púa del suelo: sale (0-2), espera (3-4 en bucle) y se deshace (5-7). Apoyo abajo en el centro (6, 10). 80 ms |
| `Effects/Proj_HallucigeniaSpine.png` | 2 | 12×6 | Púa disparada del Erizo (nivel III), hacia la derecha, con estela |

`<Cuerpo>`: `Placoderm`, `Jellyfish`, `Crinoid`. `vista_previa_*.png` son solo para revisar.

## Tanda 2: Piel

La Piel va **delante** del cuerpo y deja a la vista la cara: de lado cubre la parte de atrás y de arriba, de frente solo la
frente y de espaldas casi todo.

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Skin/Skin_TrilobitePlates_<Cuerpo>_<Idle/Move/Attack>_<side/down/up>.png` | 4 / 4 / 6 | 48×48 | Placas de trilobite: bandas de acero segmentadas con el filo iluminado |
| `Skin/Skin_WiwaxiaSclerites_<Cuerpo>_<…>_<…>.png` | 4 / 4 / 6 | 48×48 | Escleritos de Wiwaxia: escamas en hoja al tresbolillo con brillo bronce y verde, y espinas en el lomo |
| `Effects/Fx_TrilobitePlates_Bounce.png` | 5 | 16×16 | Un proyectil rebota en las placas. 60 ms |
| `Effects/Fx_TrilobitePlates_Roll.png` | 4 (bucle) | 32×32 | Enrollarse (nivel III): bola de placas que gira durante la esquiva. 60 ms |
| `Effects/Proj_WiwaxiaSclerite.png` | 3 (bucle) | 10×6 | Escama disparada, hacia la derecha |
| `Effects/Fx_WiwaxiaSclerites_Release.png` | 4 | 24×24 | Las escamas se erizan y salen. 70 ms |
| `Effects/Fx_WiwaxiaSclerite_Block.png` | 4 | 12×12 | Una escama de la Órbita destruye un proyectil. 60 ms |

## Tanda 3: Órganos

Sin capa en el cuerpo (como en el GDD): efectos de activación.

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Larva/Larva_Swim.png` | 4 (bucle) | 16×16 | Larva aliada del Saco de crías, mirando a la derecha: gota lima con cilios que baten. 110 ms |
| `Larva/Larva_Bite.png` | 4 | 16×16 | Se lanza y muerde (boca abierta en el cuadro 1). 70 ms |
| `Larva/Larva_Death.png` | 5 | 16×16 | Se deshace en motas lima al acabar sus 8 s. 90 ms |
| `Effects/Fx_BroodSac_Open.png` | 6 | 32×32 | El saco se abre sobre el jugador y suelta las larvas. 70 ms |
| `Effects/Fx_BroodSac_Split.png` | 4 | 16×16 | Enjambre (nivel III): la larva que mata se parte en dos. 60 ms |
| `Effects/Fx_CompoundEyes_Flash.png` | 6 | 32×32 | Ojos compuestos al activarse: ojo de facetas ámbar que se abre y destella sobre el jugador. 70 ms |
| `UI/UI_CompoundEyes_Vignette.png` | 1 | 480×270 | Marco de facetas ámbar en el borde de la pantalla mientras dura la cámara lenta (a 1:1 con la interfaz de referencia) |

## Tanda 4: Carroñero (restos de Carroña)

Generada con `generar_carrona.py` (`python generar_carrona.py`, con la skill `primordia-sprites`; ya no está en `generar_mutaciones_piso2.py`). Paleta de la clase: `ramp('#a07a48')` (marrón arena, sombras hacia el morado), carne `ramp('#b04a48')`, restos dobles `ramp('#d4a42c')`.

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Effects/Fx_Carrion_Remains.png` | 4 (bucle) | 16×16 | Restos que deja un enemigo al morir (4 s): montón de arena con un trozo de carne y dos costillas, un destello que lo cruza y una chispa en los cuadros 0 y 2. Apoyo abajo en el centro. 150 ms |
| `Effects/Fx_Carrion_RemainsDouble.png` | 4 (bucle) | 16×16 | Restos dobles (Doble ración de la Hallucigenia), dorados y algo más grandes |
| `Effects/Fx_Carrion_Eat.png` | 5 | 24×24 | Al tocarlos: los trozos (carne y arena, 3 px con contorno) giran en espiral, suben y acaban en chispa. 60 ms |
| `Effects/Status_Carrion_1/2/3.png` | 2 (bucle) | 12×8 | Marca sobre el jugador con el bonus acumulado (1, 2 o 3 colmillos que comparten contorno; en el cuadro 1 brillan las puntas). 250 ms |
