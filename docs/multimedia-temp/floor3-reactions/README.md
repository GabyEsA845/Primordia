# Reacciones del piso 3 (pantano carbonífero)

Generadas con `generar_reacciones_piso3.py` (skill primordia-sprites; importa los generadores del escenario, de las
Bocas y de las mutaciones para el odre, el charco de veneno, las gotas, las llamas y el humo):

```
python generar_reacciones_piso3.py
```

Reacciones del piso: **gas + ataque = explosión de fuego** y **fuego + charco de veneno = nube tóxica**. Colores
legibles en los tres tramos: veneno lima, metano cian, fuego naranja, nube tóxica mostaza. El veneno arde con
**llamas de base lima** y el metano con **base azul**, para que cada fuego diga de dónde viene.

Importar: *Multiple*, 16 PPU, *Point*, sin compresión, pivote *Center* salvo que se diga.

## Ya hechos en otras carpetas (se reutilizan)

| Hoja | Carpeta | Uso en las reacciones |
|---|---|---|
| `Fx_Burning` (16×16) | `floor3-mutations/Effects` | Enemigo que arde |
| `Fx_GroundFire` (32×24) | `floor3-mutations/Effects` | Fuego en el suelo (helechos, telarañas, Incendio) |
| `Fx_MethaneSac_Explode` (64×64) | `floor3-mutations/Effects` | Explosión de gas (nube del jugador y odre del escenario) |
| `Fx_PoisonPuddleSmall/Big_*` | `floor3-bodies/Effects` | Charcos de veneno |
| `Methane_Mist`, `Methane_Bubble`, `Vent_*`, `Fern_*_Burn/Burnt` | `floor3-environment/<Tema>/props` | Niebla de metano, burbujeo, fumarolas, helechos que arden |

## Efectos (`Effects/`)

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Fx_ToxicCloud_Form` | 6 | 48×64 | 100 | abajo, y = 58 | La columna tóxica nace del charco; acaba donde empieza `Idle` |
| `Fx_ToxicCloud_Idle` | 8 | 48×64 | 120 | abajo, y = 58 | Bucle: columna mostaza que sube y se abre arriba, con motas lima y grumos oscuros |
| `Fx_ToxicCloud_Fade` | 5 | 48×64 | 110 | abajo, y = 58 | Se deshace |
| `Fx_PoisonPuddle_Ignite` | 6 | 48×48 | 80 | centro (el del charco grande) | El fuego corre por el charco desde la izquierda. **Cuadro 3: sale la nube tóxica.** El charco se gasta: el último cuadro es la mancha quemada (el código la desvanece). Para el charco pequeño, escalar |
| `Fx_MistIgnite` | 7 | 32×24 | 70 | abajo | Casilla de niebla que se prende: brillo, fogonazo azul, fuego, humo. **Daña en los cuadros 1-3.** El código lanza una por trozo de niebla con retardo según la distancia al punto de ignición |
| `Fx_MistFlare` | 6 | 24×40 | 70 | abajo | Llamarada suelta al final del fogonazo (base azul) que se apaga en humo |
| `Fx_Ignite` | 4 | 16×16 | 50 | centro | Chispa de ignición (fuego que toca gas). **Cuadro 1: prende** |
| `Fx_Poisoned` | 4 | 16×16 | 120 | centro, sobre el enemigo | Bucle: gotas que caen y burbujas que revientan. El código tiñe además al enemigo de lima (`#a8e040`, ≈ 35 %) |
| `Fx_FireOut` | 6 | 32×32 | 90 | abajo (como `Fx_GroundFire`) | El fuego se apaga: llamas que bajan, chisporroteo y vapor gris frío (lluvia de la Meganeura, fin de las llamas) |

## Escenario (`<Swamp|Smoke|Burnt>/`)

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Methane_Sac_Burst` | 5 | 32×32 | 60 | abajo (como `Methane_Sac_Idle`) | El odre se hincha, se raja de luz y revienta en jirones. **Cuadro 2: explota** (lanzar `Fx_MethaneSac_Explode` encima) |
| `Methane_Sac_Remains` | 1 | 32×32 | — | abajo | La bolsa rota que queda el resto de la sala (el terreno no se recupera) |
| `PoisonBulb_Idle` | 4 | 32×32 | 260 | abajo | Planta venenosa: bulbo lima con gajos y manchas de aviso; respira y gotea |
| `PoisonBulb_Hit` | 3 | 32×32 | 60 | abajo | Golpe: blanco, aplastado, estirado |
| `PoisonBulb_Burst` | 5 | 32×32 | 70 | abajo | Se hincha, se raja y revienta en gotas. **Cuadro 2: sale el charco** (`Fx_PoisonPuddleBig_Appear` en su base) |
| `PoisonBulb_Stump` | 1 | 32×32 | — | abajo | Cáscara abierta mientras espera el rebrote |
| `PoisonBulb_Regrow` | 5 | 32×32 | 140 | abajo | Rebrota (el tiempo de espera se decide al programar) |

`vista_previa_efectos.png` y `vista_previa_escenario.png` muestran todas las hojas; `gif/` (no va a git) las anima.
