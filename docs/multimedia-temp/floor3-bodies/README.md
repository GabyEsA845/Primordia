# Cuerpos del jugador del piso 3 (pantano carbonífero)

Generados con `generar_cuerpos_piso3.py` y la skill `primordia-sprites`: 16 píxeles por unidad, luz arriba a la izquierda, contorno `OUTL`, rampas de 5 tonos y luz rebotada. Las paletas son las de `references/pisos.md` (sección del piso 3).

Como en el piso 2, la Boca va dibujada en el cuerpo y cada cuerpo tiene su propio ataque. Desde este piso, además, cada uno esquiva a su manera y todos mueren hundiéndose en un charco de lodo.

| Cuerpo | Familia | Boca | Esquiva |
|---|---|---|---|
| `Frog/` (rana venenosa, amarillo oscuro con manchas azules) | Área | Escupitajo venenoso | Salto largo y plano |
| `Diplocaulus/` (salmón con manchas granate) | Cuerpo a cuerpo | Peonza (se mantiene) | Ondulación rápida en S |
| `Whipscorpion/` (vinagrillo, caoba) | Proyectil | Chorro de vinagre (se mantiene) | Se hace bola y rueda |

**Volumen:** el Diplocaulus y el vinagrillo se modelan con elipsoides en 3D y un pequeño trazador de rayos (`trace()`), con el sombreado de cúpula de `pixel.py` y la normal suavizada. Así las tres vistas y el giro de la Peonza salen del mismo modelo. La cámara del Diplocaulus está a 55° (animal plano: se lee el bumerán) y la del vinagrillo a 30° (más de perfil: se lee la cola alzada). Las partes finas (patas, palpos, látigo) son líneas de 1 px sin contorno. La rana está dibujada con elipses en 2D, como en la prueba de estilo aprobada.

## Rana (`Frog/`)

| Hoja | Cuadros | Lienzo | ms | Uso |
|---|---|---|---|---|
| `Player_Frog_Idle_side/down/up` | 4 | 32×32 | 160 | Reposo: respira e hincha un poco la garganta |
| `Player_Frog_Move_side/down/up` | 4 | 32×32 | 90 | Saltitos |
| `Player_Frog_Dodge_side/down/up` | 5 | 32×32 | 60 | Salto largo y plano, estirada |
| `Player_Frog_Attack_side/down/up` | 12 | **40×40** | 80 | Escupitajo: hincha la garganta, entorna los ojos, la hinchazón sube, la bola viscosa asoma entre los labios y sale a empujones; babea y se recupera |
| `Player_Frog_Hurt` | 2 | 32×32 | 80 | Blanco y rojo |
| `Player_Frog_Death` | 7 | 40×40 | 120 | Se aplasta con los ojos en X, se apaga y se hunde en el lodo |

**Cuadro que cuenta:** el 7 (índice 7) del ataque, a 0,56 s, cuando la bola se suelta. La Boca dispara cada 2,8 s, así que el código puede empezar la animación 0,56 s antes del disparo. La bola en vuelo es un efecto aparte.

## Diplocaulus (`Diplocaulus/`)

| Hoja | Cuadros | Lienzo | ms | Uso |
|---|---|---|---|---|
| `Player_Diplocaulus_Idle_side/down/up` | 4 | 32×32 | 170 | Reposo: respira y mece la cola |
| `Player_Diplocaulus_Move_side/down/up` | 4 | 32×32 | 100 | Anda ondulando, con las patas alternas en diagonal |
| `Player_Diplocaulus_Dodge_side/down/up` | 5 | 32×32 | 55 | Se pega al suelo y se impulsa con una onda en S |
| `Player_Diplocaulus_AttackStart_side/down/up` | 3 | 40×40 | 70 | Se agacha, se enrosca y empieza a girar |
| `Player_Diplocaulus_Spin` | 8 | 40×40 | 40 | **Peonza, en bucle mientras se mantiene:** gira en plano (cabeza y cola como aspa) con estelas. Sin dirección; sirve también para el Bumerán (III) |
| `Player_Diplocaulus_SpinEnd` | 3 | 40×40 | 80 | Frena, da un vaivén y vuelve a mirar a la derecha |
| `Player_Diplocaulus_Dizzy` | 4 | 40×40 | 125 | Mareo de 0,5 s al llegar al máximo: cabeza que tambalea y estrellas, en bucle |
| `Player_Diplocaulus_Hurt` | 2 | 32×32 | 80 | Blanco y rojo |
| `Player_Diplocaulus_Death` | 7 | 40×40 | 120 | Se queda flojo, se apaga y se hunde en el lodo |

Una vuelta de `Spin` dura 320 ms. El daño lo pone el código mientras dura el bucle (el remolino visible es un efecto aparte).

## Vinagrillo (`Whipscorpion/`)

| Hoja | Cuadros | Lienzo | ms | Uso |
|---|---|---|---|---|
| `Player_Whipscorpion_Idle_side/down/up` | 4 | 32×32 | 170 | Reposo: respira y tantea con los palpos |
| `Player_Whipscorpion_Move_side/down/up` | 4 | 32×32 | 90 | Anda con las patas alternas |
| `Player_Whipscorpion_Dodge_side/down/up` | 5 | 32×32 | 60 | Se agacha, se hace bola, rueda (dos cuadros) y se despliega |
| `Player_Whipscorpion_AttackStart_side/down/up` | 3 | 40×40 | 70 | Alza el abdomen en arco sobre el lomo y abre las pinzas |
| `Player_Whipscorpion_AttackLoop_side/down/up` | 4 | 40×40 | 60 | **Chorro, en bucle mientras se mantiene:** el abdomen tiembla y salen gotitas por la punta |
| `Player_Whipscorpion_AttackEnd_side/down/up` | 2 | 40×40 | 80 | Baja el abdomen |
| `Player_Whipscorpion_Hurt` | 2 | 32×32 | 80 | Blanco y rojo |
| `Player_Whipscorpion_Death` | 7 | 40×40 | 120 | Se encoge, se apaga y se hunde en el lodo |

**Origen del chorro** (punta del abdomen, en píxeles del lienzo de 40×40 con el origen arriba a la izquierda, igual en los 4 cuadros del bucle): `side` (21, 11), `down` (19, 15), `up` (19, 11). Para la izquierda, voltear la x (39 − x). El chorro en sí es un efecto aparte.

## Efectos de las Bocas (`Effects/`, con `generar_efectos_bocas_piso3.py`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Proj_PoisonSpit` | 6 | 24×24 | 70 | Bola viscosa en vuelo hacia la derecha, en bucle: tiembla, se estira hacia atrás y gotea. El juego la encoge mientras deja el rastro |
| `Proj_PoisonSpitSmall` | 4 | 12×12 | 80 | Bolitas de la Camada (III) |
| `Fx_PoisonPuddleSmall_Appear/Idle/Fade` | 3/4/3 | 16×16 | 90 | Charco del rastro (radio ~0,4 u): cae, burbujea en bucle y se seca |
| `Fx_PoisonPuddleBig_Appear/Idle/Fade` | 3/4/3 | 48×48 | 90 | Charco grande al reventar (radio ~1,2 u; escalar al del juego). También es el charco de veneno de las reacciones |
| `Fx_PoisonSpit_Burst` | 7 | 56×56 | 60 | Reventón de radio 1,5 u: la bola se aplasta, estalla en gotas que vuelan y caen. Cuadro que cuenta: el 1 (índice 1) |
| `Fx_SpinningTop_Whirl` | 4 | 48×48 | 50 | Remolino de la Peonza en bucle, en sentido horario como el giro; radio dibujado 1,5 u (escalar a 1,3 u en el nivel I) |
| `Fx_SpinningTop_Deflect` | 4 | 16×16 | 50 | Destello al devolver un proyectil (II) |
| `Fx_VinegarJet_Stream` | 4 | **16×8** | 60 | Tramo del chorro hacia la derecha, en bucle. Encaja consigo mismo en horizontal: se repite a lo largo (*Draw Mode: Tiled*) desde el origen hasta el impacto |
| `Fx_VinegarJet_Muzzle` | 3 | 12×12 | 50 | Salida en la punta del abdomen |
| `Fx_VinegarJet_Hit` | 4 | 16×16 | 60 | Impacto al final del chorro, en bucle, centrado en el punto de impacto |
| `Fx_VinegarJet_Splash` | 5 | 32×32 | 60 | Salpicadura (III), radio 1 u, cada 0,5 s. Cuadro que cuenta: el 1 |

Colores: veneno lima (`VENOM`), vinagre paja clara (`#7a5a30 … #fffbe6`, lejos del lima y del fuego), remolino de salmón a crema (el color del Diplocaulus). El tramo del chorro toca el borde a propósito (se repite); para el linter, `--celda 16x8`.

## Iconos (`Icons/`)

`Icon_PoisonSpit`, `Icon_SpinningTop` e `Icon_VinegarJet`, en 32 y 16 px, como los de las Bocas del piso 2: la bola viscosa, el Diplocaulus girando visto desde arriba y el vinagrillo con el abdomen alzado rociando.

## Notas

- `side` mira a la derecha; para la izquierda se voltea con *Flip X*. `down` mira a la cámara y `up` está de espaldas.
- Las hojas de 40×40 tienen el cuerpo en el mismo sitio que las de 32×32 (centrado): con el pivote en el centro no salta.
- `vista_previa_*.png`, `fondos_*.png` (el reposo y el ataque sobre los tres tramos) y `gif/` son solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_cuerpos_piso3.py . frog diplo whip` y `generar_efectos_bocas_piso3.py`
