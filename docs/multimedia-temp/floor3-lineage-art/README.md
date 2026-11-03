# Arte de los toques de los linajes del piso 3

Generado con `generar_arte_linajes.py` (skill primordia-sprites). Tanda de Opus del 2026-11-03, aprobada por la usuaria pieza a pieza.
Se apoya en los generadores ya aprobados: `floor3-mutations` (parásito), `floor3-jet` (chorro), `floor3-spit` y `floor3-bodies`
(Escupitajo y charco grande), `floor3-polish` (lodo pintado en el cuerpo) y `floor3-environment` (lodo de las escenas).

```
"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_arte_linajes.py [parasite] [jet] [leech] [fungus] [eryops] [zombie] [tinder]
```

Sin argumentos lo genera todo. **Importar** (lo hace Sonnet en `feature/floor-3`): Multiple, 16 PPU, Point, sin compresión, cortar por
celda (cuadrada salvo donde se diga); `Eryops/Fx_MudBody_TexDry` como *Single* con **Wrap Mode = Repeat** (como `Fx_MudBody_Tex`).
Revisión: `vista_previa_*.png`, `gif/` y `gif/escenas/` (cada pieza comparada con la normal; `*_cuadros.png` = cuadros clave). Los GIF no van a git.

## 1. Parásito grande (solo linaje Lamprea) — `Parasite/`

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Parasite_Attached_Lamprey` | 4 (bucle) | 16×16 | 150 | Sustituye a `Parasite_Attached` (12×12): lamprea de 5 anillos clavada en diagonal, labio aplastado y gota que sorbe |
| `Parasite_Leap_Lamprey` | 5 | 20×20 | 80 | Sustituye a `Parasite_Leap` (16×16): enroscada, gira con el disco abierto y su anillo de dientes (el código la mueve en arco) |

## 2. Chorro venenoso (solo linaje Pulmonoscorpius) — `Effects/Fx_VinegarJet_Poison_*`

Las mismas 13 hojas, cuadros, celdas, ms y montaje que el chorro normal (`floor3-jet/README.md`): `Start`, `Stream_A/B/C`, `Tip_A/B/C`
(16×12, pivote (0, 0,5)), `Muzzle` (16×16), `Hit` (24×24, girar con el chorro), `Splash_0/1/2` (80×80, cuadro que cuenta: el 1).
Vinagre paja con una **veta verde veneno** que ondula y late con el pulso; salida, impacto y Salpicadura con gotas de vinagre virado a
lima y suelo verdoso que humea. El código solo cambia el prefijo `Fx_VinegarJet_` → `Fx_VinegarJet_Poison_` con el linaje. El metano
tóxico usa la nube tóxica de siempre.

## 3. Escupitajo de la Sanguijuela (solo linaje Sanguijuela) — `Effects/*Leech*`

Verde turbio con nubes de sangre y gusanillos. Mismas celdas, pivotes y ms que las hojas de las que salen (`floor3-spit/README.md`):

| Hoja | Sale de | Cambios |
|---|---|---|
| `Proj_PoisonSpit_Leech_Roll_<v>` / `_Swell_<v>` | `Proj_PoisonSpit_Roll/Swell_<v>` | Sangre posada abajo y la sombra de un gusanillo que se retuerce dentro |
| `Proj_PoisonSpitSmall_Leech_Roll_<v>` | bolitas de la Camada | Solo el color |
| `Fx_PoisonSpit_Leech_Burst` | `Fx_PoisonSpit_Burst` | Gotas en parte rojas y 3 gusanillos despedidos (cuadros 2-5). Cuadro que cuenta: el 1 |
| `Fx_PoisonLeechSplat_Appear/Idle/Fade` | `Fx_PoisonSplat_*` | 3 gusanillos nadando. **`Idle` pasa a 8 cuadros** (90 ms) para que naden sin saltos |
| `Fx_PoisonLeechPuddleBig_Appear/Idle/Fade` | `Fx_PoisonPuddleBig_*` (ReactionKit) | Igual; `Idle` de 8 cuadros |
| `Fx_SlimeTrailLeech_Rim_<k>` / `_Body_<k>` / `Thin…` / `_Shine` | banda de baba | La misma banda, turbia (sin sangre: se repetiría en rayas) |
| `Fx_SlimeTrailLeech_Clot_0/1/2` | nueva | 8 cuadros, 10×10, **los mismos índices que el sello** (0-1 nace, 2 húmedo, 3-7 se seca). Coágulo encima de ~1 de cada 5 sellos (como el brillo), en una capa por encima de los `Body` |
| `Fx_SlimeTrailLeech_Worm` | nueva | 8 cuadros en bucle, 12×12, 110 ms. Gusanillo que nada a lo largo de la banda (dibujado hacia la derecha: **girarlo con la banda**), encima de ~1 de cada 5 sellos (otros que los del coágulo) mientras el sello está húmedo |
| `Fx_SlimeTrailLeech_WormSink` | nueva | 3 cuadros, 12×12: el gusanillo se hunde cuando su sello empieza a secarse (cuadros 3-5 del sello) |

## 4. Matas de hongo del piso 3 (Colonizar la sala, clase Hongo) — `FungusMat/`

`Fx_FungusMat_<Especie>_<Talla>_<Anim>`: 4 especies × 2 tallas, el código elige una al azar en cada mata (sustituyen al micelio del piso 1
solo en el piso 3). Alfombra de micelio con fieltro blanco, hilos que pasan del borde, motas y setillas verde-azuladas que brillan.

- Especies: `Bells` (campanillas luminosas), `Puffballs` (bejines de tamaños desiguales), `Earthstars` (estrellas de tierra), `Clubs` (mazas
  tipo Cordyceps con la punta que brilla).
- Tallas: `M` = celda 48, radio dibujado de la alfombra 19 px (1,2 u, `matRadius`); `L` = celda 64, radio 27 px (~1,7 u, Colonia grande).
  Escalar para que el radio dibujado coincida con el que frena y daña. Pivote: centro.

| Anim | Cuadros | ms | Uso |
|---|---|---|---|
| `Appear` | 5 | 90 | Brota al empezar el combate o al morir un enemigo encima |
| `Idle` | 8 (bucle) | 110 | El brillo late y suben esporas |
| `Pulse` | 4 | 90 | Cuando daña (cada tic): destella y sopla esporas; luego vuelve a `Idle` |

Al acabar se apaga con transparencia, como ahora (`MyceliumPatch.FadeTime`). `Fx_FungusMat_Snare` (4 cuadros en bucle, 16×16, 120 ms, pivote
abajo): hilos que se enredan en las patas de cada enemigo que está encima (frenado).

## 5. Eryops (solo Diplocaulus) — `Eryops/`

No hay capa por animación: se usa el **lodo pintado por shader** del pulido (`floor3-polish/README.md`), que tapa cada capa (cuerpo, Boca,
Piel) respetando el contorno y el blanco de los ojos.

- **Al acecho en el lodo** (normal y profundo): nivel fijo en el **55 %** del alto del cuerpo y hundido el **80 %** de ese nivel (el lodo
  normal se hunde el 50 %): asoman el lomo y la cabeza, nunca se hunde del todo. Moviéndose, `Fx_EryopsMudWake` detrás.
- **Fuera del lodo, 5 s:** el barro se queda a ese nivel. 0-1,4 s gotea (`Fx_MudDrip`); de 0,8 a 4 s se va secando por manchas
  (`ruido < seco` → `Fx_MudBody_TexDry` en vez de `Fx_MudBody_Tex`); de 4,4 a 5 s se desprende (las manchas de siempre con `limpio` de
  0,2 a 1 por tiempo) y caen 2-3 `Fx_EryopsMudFlake`.
- **Marcador del +30 %:** `Fx_EryopsMudKick` en las patas a cada paso (~0,25 s) mientras dura la mejora.

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Fx_EryopsMudWake` | 4 (bucle) | 32×12 | 100 | derecha, centro | Estela en V en el lodo; dibujada yendo a la derecha, **girarla con el movimiento** |
| `Fx_EryopsMudKick` | 6 | 24×24 | 50 | abajo, x = 16 | Terrones que saltan de las patas hacia atrás; *Flip X* a la izquierda |
| `Fx_EryopsMudFlake` | 5 | 8×10 | 100 | arriba | Escama de barro seco que cae y se deshace |
| `Fx_MudBody_TexDry` | 1 | 16×16 | — | — | Barro seco tileable (placas y grietas) |

La escena compara el Diplocaulus normal, el Eryops y el Eryops con la Piel anfibia (el aspecto completo del linaje: las Patas de salto y el
Saco de metano no cambian el dibujo).

## 6. Cordyceps (Zombi): el enemigo muerto se levanta 4 s como aliado — `Zombie/`

Vale para todo enemigo del piso 3 **menos los jefes y las trampas** (planta carnívora y efímeras): escarabajo, sapo, araña, libélula,
Calamites, brote de Calamites y Medullosa. Mantiene su diseño y sus animaciones; el aspecto de zombi lo pone el código:

- **Tinte (shader):** quita el 35 % de la saturación y acerca el color un 32 % a `#ced2bc` (+10 de luz). El contorno (`#181425`) no se toca.
- **Ojos:** el blanco de los ojos (`#f4f4e8`) pasa a `#a8f4d8` y los rojos de la araña a `#4ac8a4` (verde-azulado = aliado).
- **Hongos:** 2-3 `Fx_ZombieFungus_<0-3>` repartidos por la parte de arriba de la silueta (como `Fx_FlameLick`), con el pie en el borde
  del cuerpo; `Sprout` al levantarse y luego `Idle`.
- **Estela:** `Fx_ZombieSporeTrail` a sus pies cada ~0,15 s mientras se mueve o ataca (solo visual).
- **Levantarse (~0,5 s):** la muerte del enemigo al revés, ya con el tinte, y `Fx_ZombieRise` en sus pies.
- **Caer de verdad (a los 4 s):** el cuerpo se apaga en los cuadros 0-2 de `Fx_ZombieCrumble` y no hace su muerte normal.

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Fx_ZombieFungus_<0-3>_Sprout` | 4 | 8×12 | 90 | abajo, centro | Brota: 0-1 mazas de Cordyceps, 2 tallitos, 3 sombrerillo |
| `Fx_ZombieFungus_<0-3>_Idle` | 4 (bucle) | 8×12 | 150 | abajo, centro | Se mece; la punta brilla |
| `Fx_ZombieSporeTrail` | 6 | 12×12 | 90 | abajo, centro | Soplo de esporas que sube y se apaga |
| `Fx_ZombieRise` | 8 | 48×48 | 65 | abajo, centro | Hilos que suben del suelo, anillo de esporas y destello |
| `Fx_ZombieCrumble` | 8 | 48×48 | 80 | abajo, centro | Nube de esporas pálidas y trocitos de hongo que caen |

## 7. Yesquero (Yesca): bola de brasa, reventón de brasas y banda chamuscada — `Effects/*Tinder*`

Segunda tanda de Opus (2026-11-03). Sustituye el tinte naranja provisional (`PoisonSpitBall.EmberTint`: quitarlo y dejar el color en blanco) y el estallido
de fuego genérico. La bola es una **costra de hongo yesquero** seca con grietas al rojo: giran las placas y las grietas, mientras que la luz, las
llamitas (inclinadas hacia atrás en `side`) y las chispas no giran. Al hincharse, las grietas se abren y se ponen blancas, y la costra se pone al rojo en los cuadros 2, 4 y 5.
**No deja baba:** el rastro sigue siendo `GroundFire.Spawn` cada `SpitTrailEvery`, ahora con una marca chamuscada debajo.

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Proj_PoisonSpit_Tinder_Roll_side/down/up` | 8 (bucle) | **32×32** | 135 | apoyo (16, 20,5) → (0,5, 0,359) | Bola rodando. Es la misma regla de direcciones que la verde (izquierda = `side` con *Flip X*) |
| `Proj_PoisonSpit_Tinder_Swell_side/down/up` | 6 | **32×32** | 100 | apoyo (16, 20,5) | Últimos 0,6 s: crece hasta 1,3, las grietas se abren y blanquean, y la costra se pone al rojo en los cuadros 2, 4 y 5 |
| `Proj_PoisonSpitSmall_Tinder_Roll_side/down/up` | 4 (bucle) | 12×12 | 110 | apoyo (6, 8,5), como la verde | Bolitas de la Camada (III) |
| `Fx_PoisonSpit_Tinder_Burst` | 7 | 64×64 | 60 | centro, igual que `Fx_PoisonSpit_Burst` | Reventón de brasas con la forma del verde (radio 1,5 u, tres chorretones): costra al rojo → llamarada → lenguas de fuego, trozos de costra con estela de chispas, ascuas en el suelo y bocanada de humo. **Cuadro que cuenta: el 1.** Después, `LeaveFire` igual que ahora |
| `Fx_TinderScorch_0…3` | 8 | 20×20 | ver abajo | centro | Marca chamuscada (~13×9 px) en el pie de **cada** `GroundFire` del rastro y del estallido. Se elige una variante al azar |

- **Por qué 32×32:** sin el pie pegajoso, la brasa se dibuja un 20 % mayor (`TINDER_R`) para pesar como la verde, y hinchada no cabe en 24 junto con
  sus llamas. El apoyo queda 4,5 px por debajo del centro, igual que la verde (12, 16,5 en 24), así que **se coloca en el mismo sitio**. El radio de daño no cambia.
- **Marca chamuscada:** cuadros 0-1 a 90 ms (aparece), 2-3 en bucle a 200 ms **mientras arde su fuego** (las ascuas titilan) y 4-7 a 350 ms cuando
  se apaga (1,4 s: las ascuas se apagan y el hollín se desvanece). Después, se destruye. Va por debajo del fuego (orden menor que 6) y por encima del suelo. Si
  `GroundFire.Spawn` alarga un fuego que ya existía, la marca también se alarga.
- **Bolitas de la Camada (III):** igual que las de la Sanguijuela (`LeechMiniRoll…`), con el Yesquero las bolitas usan `Proj_PoisonSpitSmall_Tinder_Roll_*`.
  Su estallido es `Fx_PoisonSpit_Tinder_Burst` escalado por `radius / ArtRadius`, como el verde, y deja un solo fuego (estallido pequeño).
  Su rastro de fuego también lleva la marca chamuscada.
- **Rastro:** con un fuego de 2 u cada 0,5 u, el rastro sale como un muro de fuego continuo. **La usuaria lo dejó así** (2026-11-03).
