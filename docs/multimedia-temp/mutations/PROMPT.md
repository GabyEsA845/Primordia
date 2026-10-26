Necesito los sprites de las **9 mutaciones del jugador** de mi juego en pixel art (Primordia: roguelike de vista cenital en 3/4, piso 1 bajo el agua). El jugador es una célula verde, y cada mutación tiene que **cambiar su aspecto de forma muy visible**, al estilo de *Everything is Crab*: con varias mutaciones la célula se convierte en una criatura distinta. Te adjunto `generar_jugador.py` (genera la célula), las hojas actuales del jugador y su `vista_previa.png`.

Hazlo con un script de Python + Pillow (`generar_mutaciones.py`) que **reutilice las funciones y parámetros de `generar_jugador.py`** (paletas, `Canvas`, `body`, `features`, contorno oscuro y el balanceo de cada cuadro), para que todo encaje píxel a píxel con la célula.

## Cómo es el jugador ahora

- Célula de 16×16 px (16 píxeles por unidad), centrada en su lienzo, con contorno de 1 px `#181425`.
- Paleta verde: `#2f7d4f`, `#4fb85f`, `#8be36a`, `#d6ff9a`; núcleo `#3a9a58` / `#2a6e45`; brillos `#ffffff` / `#f0ffd8`; ojos `#f4f4e8`.
- Animaciones (tiras horizontales): `Idle` (4 cuadros, reposo), `Move` (4), `Dodge` (5, rueda), en tres direcciones: `side` (mira a la derecha; para la izquierda el juego la voltea), `down` (de frente) y `up` (de espaldas). Además `Hurt` (2 cuadros, de lado, blanco y rojo) y `Death` (7 cuadros, 24×24).
- En `Idle` y `Move` la célula se aplasta, se estira y se desplaza un poco en cada cuadro (ver `anims` en `generar_jugador.py`).

## Cómo funcionan las mutaciones (4 ranuras, una mutación por ranura, combinables)

Cada ranura cambia una parte distinta, para que cualquier combinación se pueda montar por capas. Orden de dibujo, de atrás hacia delante: **Extremidades → Cuerpo (Piel) → Órgano → Boca** (la Boca va detrás del cuerpo cuando el jugador mira hacia arriba).

Colores por linaje, para que se reconozcan de un vistazo (mezclados con detalles de la propia mutación):
- **Depredador** (Mandíbulas, Aletas, Vejiga de tinta): naranjas y rojos cálidos, hueso claro.
- **Acorazado** (Patas articuladas, Caparazón, Glándula de choque): grises azulados, placas, brillo frío.
- **Tóxico** (Aguijón, Esporas, Piel urticante): lima ácido y morados.

### 1. Piel: sustituye el cuerpo entero (el cambio más grande)

El cuerpo nuevo **reemplaza** la célula en todas sus animaciones, con los mismos cuadros y en las mismas direcciones. La célula crece de verdad: el juego agranda también su colisión. Conserva los ojos, el núcleo y el brillo de la célula para que se reconozca al jugador.

| Mutación | Idea | Tamaño del cuerpo |
|----------|------|-------------------|
| Caparazón (Acorazado; +1 corazón, más lento) | Célula cubierta de placas segmentadas gris azuladas, como un isópodo; la membrana verde asoma entre las placas | ~22×18 px |
| Piel urticante (Tóxico; daña a quien la toca) | Célula con la membrana más oscura y una corona de pelillos y púas lima y moradas por todo el contorno, que se erizan al moverse | ~18×18 px más las púas |

Lienzo **24×24** con el centro del cuerpo en (12, 12) para `Idle`, `Move`, `Dodge` y `Hurt`; **32×32** para `Death`. Mismos cuadros que el jugador. En `Dodge` el cuerpo rueda igual que la célula (encogido y girando); en `Hurt`, blanco y rojo; en `Death`, revienta (en el caparazón saltan las placas).

Nombres: `Skin_Carapace_Idle_side.png`, `Skin_Carapace_Move_down.png`, `Skin_Carapace_Dodge_up.png`, `Skin_Carapace_Hurt.png`, `Skin_Carapace_Death.png`… y lo mismo con `Skin_StingingSkin_…`.

### 2. Boca: el arma, en la boca, girando hacia el cursor

Dibújalas **mirando a la derecha**; el juego las gira hacia donde se apunta. Lienzo **24×24**. El punto de unión con el cuerpo es el **borde izquierdo del lienzo, a media altura** (x = 0, y = 12): el juego lo coloca en el borde de la célula. Que se vean grandes y amenazantes.

| Mutación | Idea | Ataque |
|----------|------|--------|
| Mandíbulas (Depredador; mordisco cuerpo a cuerpo) | Dos pinzas de hueso curvas y serradas, abiertas, con base carnosa naranja | Se cierran de golpe hacia delante |
| Aguijón (Tóxico; dispara rápido y envenena) | Púa larga lima, segmentada, con gota de veneno morada en la punta | Retrocede y se dispara (retroceso de 2 px) |
| Esporas (Tóxico; tres esporas en abanico) | Racimo de 3 sacos morados translúcidos con manchas lima, sobre un tallo corto | Los sacos se aprietan y se abren en abanico |

Hojas: `Mouth_<Mutación>_Idle.png` (4 cuadros, bucle suave) y `Mouth_<Mutación>_Attack.png` (3 cuadros). `<Mutación>`: `Mandibles`, `Stinger`, `Spores`.

### 3. Extremidades: apéndices que se mueven al andar

Lienzo **32×32** con el centro de la célula en (16, 16), en las tres direcciones (`side` mira a la derecha). Se dibujan **detrás** del cuerpo, así que pueden empezar bajo la célula. Tienen que asomar al menos 13 px desde el centro, para verse también con el caparazón (que es más grande). Cuatro cuadros que siguen el balanceo de la célula en `Idle` y en `Move` (usa los desplazamientos de cada cuadro de `generar_jugador.py`).

| Mutación | Idea |
|----------|------|
| Aletas (Depredador; más rápida) | Dos aletas translúcidas naranja y rojo a los lados, como de pez; en `Move` aletean con fuerza, en `Idle` ondulan suave |
| Patas articuladas (Acorazado; empuja al esquivar) | Seis patitas segmentadas gris azuladas, tres a cada lado, como de cangrejo o insecto; en `Move` dan pasos alternos |

Hojas: `Limbs_<Mutación>_Idle_<dir>.png` y `Limbs_<Mutación>_Move_<dir>.png` (4 cuadros cada una). `<Mutación>`: `Fins`, `JointedLegs`; `<dir>`: `side`, `down`, `up`.

### 4. Órgano: visible dentro de la célula

Se ve a través de la membrana, sobre el núcleo. Lienzo **16×16** (mismo encuadre que la célula), una sola versión para todas las direcciones, con algo de transparencia para que parezca dentro del cuerpo. Que se note bastante: ocupa casi la mitad de la célula.

| Mutación | Idea | Al usarla |
|----------|------|-----------|
| Vejiga de tinta (Depredador; nube que ciega) | Bolsa oscura azul tinta que se hincha y deshincha | Se contrae de golpe y suelta tinta |
| Glándula de choque (Acorazado; onda que empuja) | Núcleo eléctrico azul claro con chispas | Destello fuerte que se expande |

Hojas: `Organ_<Mutación>_Idle.png` (4 cuadros, bucle) y `Organ_<Mutación>_Activate.png` (4 cuadros, en lienzo 32×32 con el centro en (16, 16), porque el destello sale de la célula). `<Mutación>`: `InkSac`, `ShockGland`.

## Reglas para todo

- 16 píxeles por unidad, filtro nítido (sin suavizado), fondo transparente, contorno `#181425` de 1 px como la célula.
- Tiras horizontales con todos los cuadros del mismo tamaño.
- Las piezas no se dibujan en `Dodge` ni en `Death` (la célula rueda o revienta): el juego las oculta; solo la Piel tiene esas animaciones.
- Mismo estilo que el jugador: luz desde arriba a la izquierda y 3 o 4 tonos por color.

## Qué me tienes que entregar

```
mutations/
  Skin/     (Skin_Carapace_…, Skin_StingingSkin_…)
  Mouth/    (Mouth_Mandibles_…, Mouth_Stinger_…, Mouth_Spores_…)
  Limbs/    (Limbs_Fins_…, Limbs_JointedLegs_…)
  Organ/    (Organ_InkSac_…, Organ_ShockGland_…)
  gif/      (cada animación por separado)
  vista_previa_mutaciones.png
  generar_mutaciones.py
  README.md (tabla con hojas, cuadros, lienzo y punto de unión)
```

La **vista previa** (ampliada ×4, sobre fondo verde oscuro `#16352e`) debe montar las capas como lo hará el juego, en las tres direcciones:
1. La célula sola.
2. Cada mutación sola sobre la célula.
3. Tres criaturas combinadas: Mandíbulas + Aletas + Caparazón + Vejiga de tinta; Aguijón + Patas articuladas + Piel urticante + Glándula de choque; Esporas + Aletas + Piel urticante.

Comprueba en la vista previa que cada combinación se lea como una criatura distinta y que las piezas encajen con el balanceo de la célula en todos los cuadros.
