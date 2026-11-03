# Chorro de vinagre, segunda versión (piso 3, vinagrillo)

Generado con `generar_chorro.py` y la skill `primordia-sprites`. Sustituye a los `Fx_VinegarJet_*` de `floor3-bodies/Effects/`
(esas hojas se quedan allí, sin usar). Colores: vinagre paja clara `#7a5a30 … #fffbe6`; suelo mojado `#3a3420 #4a4228`.

## Hojas (`Effects/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_VinegarJet_Start` | 8 | 16×12 | 60 | Primer tramo (de 0 a 1 u desde la cola): nace de 2 px y se ensancha |
| `Fx_VinegarJet_Stream_A/B/C` | 8 | 16×12 | 60 | Tramos que se repiten. El dibujo avanza 6 px por cuadro (6 u/s) y se repite cada 48 px, en tres tramos |
| `Fx_VinegarJet_Tip_A/B/C` | 8 | 16×12 | 60 | Punta libre (el chorro llega a su alcance sin chocar): se afila y se deshace en gotas y rocío |
| `Fx_VinegarJet_Muzzle` | 4 | 16×16 | 50 | Salida en la punta del abdomen, en bucle: cuenta de vinagre que late con rocío a los lados. Centrada en el origen del chorro |
| `Fx_VinegarJet_Hit` | 4 | 24×24 | 60 | Impacto cuando algo corta el chorro, en bucle. Centrado en el punto de impacto; dibujado con el chorro **llegando desde la izquierda**: **girarlo con el chorro** |
| `Fx_VinegarJet_Splash_0/1/2` | 7 | 80×80 | 65 | Salpicadura (III), radio dibujado 2 u (32 px, el del juego: no escalar). Dura 455 ms. **Cuadro que cuenta: el 1** |

Todas las hojas del chorro están dibujadas hacia la derecha, con el centro del chorro en y = 6 de la celda (pivote **(0, 0,5)** en los
tramos: borde izquierdo, a media altura).

## Montaje en el juego (para rehacer `JetAttack.DrawSegments`)

- El chorro se divide en **huecos fijos de 1 u desde la cola**: el hueco 0 va de 0 a 1 u, el 1 de 1 a 2 u, etc. Cada hueco se pone en
  `origen + dirección · k`, girado con el chorro. **Ni escala ni transparencia**: el inicio y la punta ya están dibujados.
- Hueco 0: `Start`. Huecos k ≥ 1: `Stream_` + `"ABC"[(k − 1) % 3]`. Si el chorro **no** choca, el último hueco usa `Tip_` con la misma
  letra que le tocaría. Si choca, el último hueco es un `Stream` y en el punto de impacto va `Hit`.
- El último hueco se **recorta** a lo que llega el chorro (no se aplasta): por ejemplo, con `SpriteMask`, `Draw Mode: Tiled` con el
  ancho visible, o un sprite por cuadro recortado por código. Lo mismo al crecer y al recogerse.
- **Todos los tramos van en el mismo cuadro a la vez** (mismo reloj de 60 ms), si no se ven costuras.
- Salpicadura: alternar las tres variantes (0, 1, 2) en cada golpe de 0,5 s.

## Revisión

`vista_previa_chorro.png`, `vista_previa_montado.png` (inicio, tramos y punta seguidos, en 4 cuadros) y `gif/`
(`gif/escenas/escena_chorro.gif`: chorro libre, cortado en diagonal por una pared y nivel III con Salpicadura). Solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_chorro.py`
