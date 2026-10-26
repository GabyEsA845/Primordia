# Cuerpos del jugador del piso 2 (océano cámbrico)

Generados con `generar_cuerpos_piso2.py` (`python generar_cuerpos_piso2.py`, necesita Pillow), con el mismo estilo que `../player/generar_jugador.py`: 16 píxeles por unidad, luz arriba a la izquierda, contorno oscuro y los ojos de la célula.

Desde el piso 2 la Boca va dibujada en el cuerpo, y cada cuerpo tiene su propia animación de ataque en las 3 direcciones. El cuerpo ocupa unos 20 px dentro de un lienzo de 24×24; la zona de choque es la misma que la de la célula.

| Cuerpo | Familia | Boca |
|---|---|---|
| `Placoderm/` (pez acorazado) | Cuerpo a cuerpo | Placas mandibulares |
| `Jellyfish/` (medusa) | Proyectil | Cnidocitos |
| `Crinoid/` (lirio de mar) | Área | Corona de brazos |

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Player_<Cuerpo>_Idle_side/down/up.png` | 4 | 24×24 | Reposo |
| `Player_<Cuerpo>_Move_side/down/up.png` | 4 | 24×24 | Moverse |
| `Player_<Cuerpo>_Dodge_side/down/up.png` | 5 | 24×24 | Esquivar (se enrosca y da una vuelta) |
| `Player_<Cuerpo>_Attack_side/down/up.png` | 6 | 24×24 | Atacar: preparación (2), golpe (2) y recuperación (2) |
| `Player_<Cuerpo>_Hurt.png` | 2 | 24×24 | Recibir daño (blanco y rojo) |
| `Player_<Cuerpo>_Death.png` | 7 | 32×32 | Morir |
| `Effects/Fx_JawPlates_Bite.png` | 5 | 32×32 | Mordisco de las placas (rehecho el 25 de octubre con `generar_mordiscos.py`): dos placas de hueso con la punta en gancho que se cierran en arco de 110°. Hacia la derecha, unión en (1, 16); el alcance es lo dibujado (30 px) |
| `Effects/Fx_JawPlates_Guillotine.png` | 6 | 56×56 | Segunda dentellada de la Guillotina: más grande (52 px), filo al rojo y estela. Unión en (1, 28) |
| `Effects/Status_Shatter.png` | 4 | 12×12 | Marca de Quebrar sobre el enemigo: placa que se agrieta, en bucle |
| `Effects/Fx_ArmCrown_Whirl.png` | 4 | 48×48 | Remolino de la Corona de brazos, en bucle (radio dibujado 1,5 u; el juego lo escala al radio del ataque) |
| `Effects/Fx_ArmCrown_Tide.png` | 6 | 64×64 | Marea (`generar_marea.py`): los brazos del remolino se deshacen en un anillo de agua con espuma que se abre hasta 30 px |
| `Effects/Proj_Cnidocyte.png` | 2 | 16×8 | Arpón urticante, hacia la derecha |
| `Effects/Fx_Cnidocyte_Muzzle.png` | 4 | 16×16 | Destello al disparar, centrado |

- `side` mira a la derecha; para la izquierda se voltea con *Flip X*. `down` mira a la cámara y `up` está de espaldas.
- Cuadro del golpe: en el pez, el cuarto (índice 3), cuando las placas se cierran y debe hacer daño el mordisco; en la medusa, el tercero (índice 2), cuando la campana se contrae y debe salir el arpón; en el lirio, el tercero (índice 2), cuando la corona gira.
- `vista_previa_cuerpos_piso2.png` y `gif/` son solo para revisar, sobre el azul del piso 2.
