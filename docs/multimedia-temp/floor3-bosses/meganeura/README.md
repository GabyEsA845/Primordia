# Meganeura (jefe 2 del piso 3)

Generado con `generar_meganeura.py` (skill primordia-sprites; numpy; usa el motor 3D de
`../arthropleura/generar_arthropleura.py`):

```
python generar_meganeura.py todo       # 6d: hojas en Sheets/ y Shadows/, vistas previas, GIF y escenas
python generar_meganeura.py escenas    # solo el vuelo en círculo y la escena del picado
python generar_meganeura.py muestra    # muestra del aspecto (una dirección)
python generar_meganeura.py poses      # prueba rápida: un cuadro de cada animación
python generar_escala_meganeura.py     # láminas de escala del paso 0 (6d-0)
```

Libélula gigante en **picada alta** (cámara a 55°, como el Arthropleura), hecha a partir de la referencia del usuario
(`referencia_meganeura.png`) con la **paleta C**: cuerpo verde azulado iridiscente con el dibujo de la referencia
(bandas oscuras en el abdomen, rayas ámbar en el tórax, cara ámbar, ojos enormes oscuros con reflejo rojo, patas verde
oliva con anillos) y **alas puramente translúcidas** gris azulado con venación densa, borde oscuro y pterostigma.

## Tamaño (aprobado en la 6d-0)

- **10 u de envergadura (160 px)**, unos 7 u de largo. Sala de **40×24 u** de suelo en la Tierra quemada, cámara ×1,3.
- **Celda 192×192. Pivote = centro de la celda = centro del tórax** (en el aire). La sombra (`Shadows/`) usa el
  mismo pivote: el código pone la sombra en el punto del suelo y el jefe encima, subido `altura × cos 55° ≈ 0,57` px
  por cada px de altura.
- **Alas translúcidas:** la membrana tiene alfa ~100/255 y deja ver el suelo; venas, borde y pterostigma son más
  opacos. Importar con transparencia (sin *Alpha Is Transparency* raro: el PNG ya va premezclado con el cuerpo).

## Orden de dibujo (para el código)

La Meganeura tiene alas y cabeza en alto: **mientras vuela o está posada se dibuja en una capa por encima de los
enemigos del suelo** (ninfas, huevos, charcas), aunque estén más abajo en la pantalla; si no, una ninfa que pasa
bajo sus alas la taparía y parecería que el jefe es parte del fondo. Su sombra sí va en la capa del suelo.

## Direcciones

16 direcciones: **9 dibujadas** (filas de cada hoja, de arriba abajo) y las otras 7 con *Flip X*, igual que el
Arthropleura.

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

| Hoja | Cuadros | ms | Uso / cuadro que cuenta |
|---|---|---|---|
| `Meganeura_Fly` | 4 | 50 | Vuelo (bucle): alas anteriores y posteriores a contrafase. Subir, bajar, cruzar la sala, presentación |
| `Meganeura_Hover` | 4 | 40 | Cernido **a ras de suelo** tras el picado (bucle): aleteo corto y rápido, morro algo arriba. Vulnerable |
| `Meganeura_DiveWarn` | 4 | 120 | Se prepara para el picado (cuando está baja y se la ve): alas arriba y atrás, morro al blanco, **ojos encendidos desde el cuadro 2**. Al acabar → `Dive` |
| `Meganeura_Dive` | 3 | 50 | **Picado** (bucle): alas pegadas hacia atrás que vibran, morro abajo, patas por delante, ojos encendidos. Daña al llegar a la marca |
| `Meganeura_Climb` | 4 | 60 | Remonta (bucle mientras sube): aletazos amplios, morro arriba, patas recogidas. Al salir de pantalla → solo sombra |
| `Meganeura_Gust` | 6 | 80 | **Aleteo fuerte** que empuja gas, veneno y proyectiles: se alza y bate. **El empujón sale en el cuadro 3** |
| `Meganeura_Land` | 3 | 90 | Se posa (al revés: despega): frena, alas que se aplanan, patas que se estiran |
| `Meganeura_Perch` | 4 | 200 | Posada (bucle): alas planas y algo caídas que tiemblan. Presentación en el tronco y bajada furiosa de la F2 |
| `Meganeura_Lay` | 6 | 120 | Pone un huevo (posada): el abdomen baja hasta el agua; **el huevo aparece en el cuadro 3** |
| `Meganeura_Hurt` | 2 | 80 | Blanco y rojo (RED4), desde `Fly`; la transparencia de las alas se mantiene |

## Sombras (`Shadows/`, mismas filas y cuadros)

`Meganeura_Shadow_<Animación>`: la silueta aplastada en el suelo (cuerpo y alas, negro opaco), con los mismos
cuadros que la animación. El código le da la transparencia (~45 % a ras de suelo) y, cuando vuela alto, la muestra
sola, más tenue y tramada (es lo único que se ve de ella: invulnerable).

## Vistas previas

`vista_previa_6d_animaciones.png` (todas en `Side`), `vista_previa_6d_direcciones.png` (Fly, Dive y Perch en las 9
direcciones), `vista_previa_6d_curva.png` + `gif/escenas/Meganeura_curva.gif` (vuelo en círculo: las 16 direcciones),
`vista_previa_6d_picado.png` + `gif/escenas/Meganeura_escena_picado.gif` (sombra → marca → picado → a ras de suelo →
remonta; la marca es provisional hasta la 6f), `gif/Meganeura_<Animación>.gif`. Escala: `vista_previa_escala_*.png`.
Todo eso es solo para revisar.

## 6e-1: daño de las alas (variantes de hoja)

Cada hoja de `Sheets/` tiene variantes con el **mismo tamaño, cuadros y filas**; la sombra es la de la base.
El código cambia de juego de hojas según el estado:

| Sufijo | Cuándo | Qué cambia |
|---|---|---|
| *(ninguno)* | F1 sin quemar | Alas sanas |
| `_Burnt` | F1 tras arder en el metano | Agujeros transparentes con borde carbonizado y membrana chamuscada (permanente) |
| `_Torn1` / `_Torn1Burnt` | F2 (≤ 65 %) | Pocos rasgones desde el borde de salida (+ quemaduras si ardió) |
| `_Torn2` / `_Torn2Burnt` | F3 (≤ 30 %) | Muchos rasgones y una punta arrancada en cada lado (+ quemaduras) |

`Meganeura_Hurt_<Var>` existe para cada variante. Vistas: `vista_previa_6e1_alas.png`, `vista_previa_6e1_juego.png`.

## 6e-2: golpes (`python generar_meganeura.py golpes [Anim] [Var]`)

| Hoja | Cuadros | ms | Uso / cuadro que cuenta |
|---|---|---|---|
| `Meganeura_Stuck` | 6 | 70 | **Clavada** 1,5 s en el Lepidodendron o una Sigillaria: cabeza y patas dentro de la madera (collar de astillas), alas desesperadas, abdomen que se retuerce. El collar va a 12 px por delante del pivote, sobre la herida del tronco (y ≈ 50 del tronco) |
| `Meganeura_TearFree` | 5 | 80 | Se arranca: **astillas (`Fx_Meganeura_Splinters`) en el cuadro 2**; el tronco pasa a `Lepido_Split_Gash` |
| `Meganeura_Shocked` | 4 | 60 | Alcanzada por el rayo: destellos azules (se le ve la silueta) y arcos eléctricos. Después → `Fall` |
| `Meganeura_Fall` | 4 | 70 | Cae dando vueltas (bucle mientras baja). Al tocar el suelo: `Fx_Meganeura_Crash` → `Grounded` |
| `Meganeura_Grounded` | 4 | 150 | **Boca arriba** en el suelo (vulnerable): patas que patalean, alas que tiemblan |
| `Meganeura_Recover` | 6 | 80 | Se endereza y despega → `Climb` |
| `Meganeura_Fall_Burning` / `_Grounded_Burning` | 4 / 4 | = | **Capa encima** (mismas celdas): cae con las alas ardiendo (la prendiste en el metano) |

Todas tienen sombra en `Shadows/` y las 5 variantes de alas.

Escenario y efectos (`python generar_efectos_meganeura.py golpes`):

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Burnt/Lepido_Split_Idle` | 4 | 64×96 | Lepidodendron partido grande de la sala (~3 u), ascuas que laten en la rotura. Pivote abajo-centro (pie en y = 90), colisión ≈ 2×1 u al pie |
| `Burnt/Lepido_Split_Hit` | 4 | 64×96 | El picado se clava: tiembla y salta corteza (golpe en el cuadro 0) |
| `Burnt/Lepido_Split_Gash` | 4 | 64×96 | Herida abierta que queda tras arrancarse (se queda así el resto del combate) |
| `Effects/Fx_Meganeura_Splinters` | 6 | 64×64 | Astillas y corteza al arrancarse |
| `Effects/Fx_Meganeura_Crash` | 6 | 96×64 | Cae al suelo: anillo de ceniza y ascuas |

## 6e-3: momentos (`python generar_meganeura.py momentos [Var]`)

| Hoja | Cuadros | ms | Uso / cuadro que cuenta |
|---|---|---|---|
| `Meganeura_Intro` | 8 | 110 | Presentación posada en el Lepidodendron: alas recogidas que se abren y un **reflejo azulado** las recorre (cuadros 2-6). Nombre en pantalla al acabar. Siempre con alas sanas |
| `Meganeura_Screech` | 6 | 90 | **Furia de la F2** (rompiste todos los huevos): se alza, abre las mandíbulas y chilla; **las ondas (`Fx_Meganeura_Screech`) salen en el cuadro 2**. Variantes `_Torn1`, `_Torn1Burnt` |
| `Meganeura_PerchRage` | 4 | 120 | Posada furiosa junto a la charca (ventana de daño de la F2): ojos encendidos, alas que tiemblan. Variantes `_Torn1`, `_Torn1Burnt` |
| `Meganeura_DeathFall` | 8 | 90 | **Una sola fila**: cae en espiral (gira ella sola) sin el ala trasera izquierda y con las alas de la F3 |
| `Meganeura_WingDetached` | 8 | 70 | Celda 128×128: el ala que se desprende y cae girando por su cuenta (sale al empezar `DeathFall`) |
| `Meganeura_Dead` | 1 | — | Restos flotando en la charca (9 filas = 9 direcciones; la última dirección de la espiral es `Side`). Color apagado. Con `Fx_Meganeura_DeadRipple` debajo |

`DeathFall`, `WingDetached` y `Dead` tienen versión `_Burnt` (si ardió en la F1).

Efectos (`python generar_efectos_meganeura.py momentos`): `Fx_Meganeura_Screech` (8, 160×112, centro = la cabeza),
`Fx_Meganeura_Splash` (7, 128×96, el agua en y = 70: se estrella en la charca), `Fx_Meganeura_DeadRipple` (4,
160×80, bucle de 200 ms bajo los restos).

## 6f-1: picado, aleteo, huevos y ninfas

Efectos (`python generar_efectos_meganeura.py picado`):

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Effects/Fx_Meganeura_DiveMark_Follow` | 4 | 64×64 | 160 | Marca del picado (3 u) que **sigue** a la rana: ámbar, silueta de la libélula y 4 muescas |
| `Effects/Fx_Meganeura_DiveMark_Locked` | 4 | 64×64 | 90 | La marca **fija**: roja, las muescas se cierran, late más rápido. Después → picado |
| `Effects/Fx_Meganeura_DiveImpact` | 7 | 112×72 | 70 | Picado contra el suelo: onda de ceniza y ascuas (centro = la marca, a la altura del suelo) |
| `Effects/Fx_Meganeura_DiveCrack` | 5 | 64×40 | 400 | Grieta que deja el picado y se desvanece (capa del suelo) |
| `Effects/Fx_Meganeura_DiveSplash` | 6 | 70×52 | 70 | Picado sobre una charca (salpicadura pequeña; el agua en y ≈ 38) |
| `Effects/Fx_Meganeura_Gust` | 6 | 112×64 | 60 | Ráfaga del aleteo fuerte (~6 u, sale del borde izquierdo hacia la derecha; el código la gira). Bucle mientras dura el empujón |
| `Effects/Fx_Meganeura_WindDrag` | 4 | 16×16 | 80 | Rayas de arrastre sobre gas, charcos de veneno y proyectiles que empuja el viento |
| `Eggs/Meganeura_Eggs_Appear` | 4 | 40×32 | 100 | El racimo aparece en la orilla (al soltar el huevo en el cuadro 3 de `Lay`). Pie en y = 26 |
| `Eggs/Meganeura_Eggs_Idle` | 4 | 40×32 | 200 | Racimo gelatinoso translúcido: larvas enroscadas que se mueven (bucle) |
| `Eggs/Meganeura_Eggs_Hatch` | 6 | 40×32 | 90 | Eclosiona: **la ninfa aparece en el cuadro 3** (el racimo deja de contar) |
| `Eggs/Meganeura_Eggs_Break` | 6 | 40×32 | 70 | Lo rompes: gelatina que salpica. Cuando no queda ninguno → la madre baja furiosa (`Screech`) |

Ninfa (`python generar_ninfa_meganeura.py todo`; picada, 9 filas de dirección como la madre; pivote = centro):

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Meganeura_Nymph_Idle` | 4 | 64×64 | 200 | Quieta |
| `Meganeura_Nymph_Move` | 6 | 64×64 | 80 | Anda deprisa (patas en trípode); un pequeño impulso al acercarse |
| `Meganeura_Nymph_Warn` | 3 | 64×64 | 130 | Aviso de 0,4 s: se agacha y el labio brilla en ámbar |
| `Meganeura_Nymph_Strike` | 4 | **112×112** | 60 | El labio sale disparado ~2,5 u y vuelve; **daña en el cuadro 1**; si te pega **te tira un poco hacia ella** |
| `Meganeura_Nymph_Hurt` | 2 | 64×64 | 80 | Blanco y rojo |
| `Meganeura_Nymph_Death` | 5 | 64×64 | 110 | Se da la vuelta y encoge las patas. Veneno: esta misma + tinte lima y `Fx_Poisoned` por código |
| `Meganeura_Nymph_DeathBurn` | 6 | 64×64 | 110 | Muere por fuego: arde, se carboniza y queda ceniza con ascuas |

Sombras en `Shadows/Meganeura_Nymph_Shadow_<Anim>`. La muerte electrocutada (charca electrificada) llega con la 6f-2.
Escenas: `vista_previa_6e_escena_marca`, `_aleteo`, `_huevos` (GIF en `gif/escenas/`).

## 6f-2: la tormenta de la F3

Rayos (`python generar_tormenta_meganeura.py rayos`) y ambiente (`... ambiente`); silueta (`python generar_meganeura.py
silueta`) y ninfa electrocutada (`python generar_ninfa_meganeura.py shock`).

**Capas (para el código):** oscuridad = capa de color `#0C1428` con alfa ~0,5 sobre la sala (se aclara al morir el
jefe); destello del relámpago = capa `#E8F4FF` con alfa 0,3-0,85 que baja en ~0,25 s; **todo lo que brilla (rayos,
señales, impactos, corriente, charcas electrificadas, fuego, jefe electrocutado) va ENCIMA de la oscuridad y del
destello**; la lluvia, encima de la oscuridad.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Effects/Fx_Bolt_Big_Mark` | 10 | 96×80 | — | Señal del **rayo grande que sigue al jugador** (centro = el jugador). Cuadros 0-7 = la carga (el código elige el cuadro según lo que falta: chispas y corriente que crecen, con el sonido de carga); 8-9 = **fijo** un instante (blanco que late) justo antes de caer |
| `Effects/Fx_Bolt_Big` | 5 | 96×360 | 50 | El rayo grande: cae desde arriba; el pie (abajo-centro, y = 352) en el punto de impacto. Daña en el cuadro 0 |
| `Effects/Fx_Bolt_Big_Impact` | 6 | 128×96 | 60 | Destello, chispas y arcos (el suelo en y ≈ 60) |
| `Effects/Fx_Bolt_Huge` | 6 | 192×420 | 50 | **El primer trueno** de la tormenta (ramificado), junto al jugador, con destello de toda la pantalla (alfa 0,85) |
| `Effects/Fx_Bolt_Small_Mark` | 8 | 48×40 | — | Señal de un **rayo suelto** en el terreno (la carga, como la grande; varios a la vez por la sala) |
| `Effects/Fx_Bolt_Small` / `_Small_Impact` | 4 / 5 | 48×360 / 64×48 | 50 / 60 | El rayo suelto y su impacto |
| `Effects/Fx_Bolt_Scorch` | 4 | 64×40 | 500 | Quemadura en el suelo que se desvanece |
| `Effects/Fx_Bolt_Ring` | 16 | 96×80 | 250 | **Corriente circular** (~4 s): se expande de ~1,6 a ~2,6 u y se apaga; daña al cruzarla |
| `Burnt/Puddle_Water_<Forma>_Electric` | 6 | = charca | 70 | **Charca electrificada entera** (bucle, 5-6 s): encima de `Puddle_Water_<Forma>` (Round, Kidney, Lobed, Splash, Island, Long, Chain). Mata ninfas (`Meganeura_Nymph_DeathShock`) y daña al jugador. Si la Meganeura pica sobre ella con el rayo → aturdida |
| `Burnt/Sigillaria_Struck` | 8 | 72×72 | 80 | La Sigillaria quemada atrae el rayo (pararrayos, **un solo uso**): destello, se raja y las mitades se abren y prenden. Pie abajo-centro |
| `Burnt/Sigillaria_Burning` → `_Smolder` → `_Split` | 4 / 4 / 1 | 72×72 | 110 / 140 | Arde → la lluvia la va apagando (humea) → apagada para siempre |
| `Sheets/Meganeura_Silhouette_Fly` | 4 × 9 filas | 192 | = Fly | **Silueta a contraluz** cuando un relámpago la pilla volando alto: 2 cuadros en alto + la sombra nítida |
| `Sheets/Meganeura_Nymph_DeathShock` | 5 × 9 filas | 64 | 90 | Ninfa electrocutada en la charca |
| `Effects/Fx_Storm_Rain` / `_Drizzle` | 4 | 128×128 | 60 | Lluvia en mosaico (capa de pantalla) / llovizna del final |
| `Effects/Fx_Storm_RainSplash` / `_Ripple` | 4 / 5 | 12×8 / 16×10 | 70 | Gotas en el suelo / ondas en las charcas (el código las reparte al azar) |
| `Burnt/Puddle_Water_<Round|Kidney|Splash>_Grow` | 6 | = charca | 300 | **Charcos nuevos** que aparecen con la lluvia (el último cuadro = la charca entera) |
| `Effects/Fx_Storm_CloudShadow` | 1 | 256×256 | — | Sombra de nubes en mosaico; el código la desliza despacio |
| `Effects/Fx_Storm_WindAsh` | 4 | 128×128 | 80 | Viento con ceniza de la entrada a la tormenta (mosaico) |
| `Effects/Fx_Storm_Steam` | 6 | 48×48 | 120 | Vapor del suelo cuando amaina (final) |

Escenas: `vista_previa_6e_escena_tormenta_entrada`, `_tormenta_rayos`, `_tormenta_silueta`, `_tormenta_final` (GIF en `gif/escenas/`).

## 6g: barra de vida (`BossBar/UI_BossBar_Meganeura_*`, versión A «alas», elegida por el usuario)

`python generar_interfaz_meganeura.py hojas` (y `comparar` para volver a ver las dos versiones). La barra va **abajo
de la pantalla, centrada** (el jefe vive en la mitad de arriba), en la capa de interfaz: por encima de la oscuridad
de la tormenta.

Cabeza y tórax a la izquierda; las alas anteriores y posteriores, abiertas, translúcidas y con venas, forman el borde
de arriba y el de abajo. Marco **300×32**; la vida rellena la **ventana de 241×8 en (43, 12)** desde la esquina de abajo
a la izquierda del marco, de izquierda a derecha (Image *Filled*, horizontal). Muescas centradas en el **65 %** y el
**30 %** de la ventana (x = 43 + 241 × umbral, ancho 8).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Frame` / `Frame_Torn1` / `Frame_Torn2` | 1 | 300×32 | — | Marco F1 / F2 (alas algo rasgadas) / F3 (muy rasgadas). Sin quemaduras |
| `Back` | 1 | 241×8 | — | Fondo de la ventana |
| `Fill_Teal` | 4 | 241×8 | 120 | Relleno F1: verde azulado con un brillo iridiscente que corre (bucle) |
| `Fill_Cloud` | 4 | 241×8 | 160 | **Vuela alta (invulnerable)**: gris azulado con nubes (bucle); vuelve a `Fill_<fase>` al bajar |
| `Fill_Rage` | 1 | 241×8 | — | Relleno F2: rojo de furia |
| `Fill_Storm` | 4 | 241×8 | 80 | Relleno F3: azul eléctrico con chispas que lo recorren (bucle) |
| `Fill_Trail` | 1 | 241×8 | — | Barra fantasma (espera ~0,4 s y se vacía) |
| `Eyes_Hunt` / `Eyes_Rage` / `Eyes_Storm` | 1 / 1 / 2 | 300×32 | — / — / 120 | Ojos encima del marco: F1 oscuros con reflejo rojo, F2 rojo fijo con halo, F3 parpadeo azul |
| `Notch` / `Notch_Broken` | 1 | 8×32 | — | Vena del ala con nudo ámbar, **centrada en su umbral**, y rota |
| `Fx_EggBurst` | 6 | 300×32 | 80 | **65 %**: un racimo de huevos revienta sobre la muesca |
| `Fx_BoltStrike` | 5 | 300×32 | 70 | **30 %**: un rayo cae sobre la muesca y recorre la barra |
| `Fx_LowSparks` | 4 | 300×32 | 90 | **Último 10 %**: chispas del marco (bucle); el código además hace **temblar** el marco ±1 px |

Por fase: F1 `Frame` + `Fill_Teal` (`Fill_Cloud` en alto) + `Eyes_Hunt`; F2 `Frame_Torn1` + `Fill_Rage` + `Eyes_Rage`;
F3 `Frame_Torn2` + `Fill_Storm` + `Eyes_Storm`. Vistas: `vista_previa_6g_barra.png`, `vista_previa_6g_escenas.png`,
`vista_previa_6g_comparar.png`, `gif/Meganeura_BossBar_combate.gif` (la barra a lo largo del combate).
