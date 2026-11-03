# Clon de la Peonza (piso 3, Diplocaulus, nivel III: Bumerán)

Generado con `generar_clon_peonza.py` y la skill `primordia-sprites`. Sale de `floor3-bodies/Diplocaulus/Player_Diplocaulus_Spin.png`
(aprobado): mismos cuadros, recoloreados como un doble translúcido de salmón claro a crema `#e07a64 … #fff0e0`, contorno claro
`#ffe2cc`, brillo en el centro y un destello que lo recorre. Sustituye a los cuadros de `Spin` tintados que lanza ahora `SpinBoomerang`.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_SpinClone_Spin` | 8 | 40×40 | 40 | El clon girando, en bucle, mientras vuela (ida y vuelta) |
| `Fx_SpinClone_Echo` | 8 | 40×40 | 40 | Ecos detrás del clon: el código pone **2**, con los dos cuadros anteriores de esta hoja en las dos posiciones anteriores, al 100 % y al ~55 % de opacidad. En damero y sin contorno para que no se agrise (no tintarlo: solo bajar el alfa) |
| `Fx_SpinClone_Whirl` | 4 | 32×32 | 40 | Remolino propio alrededor del clon, en bucle, en sentido horario; radio dibujado 0,7 u (el de golpe, `HitRadius`) |
| `Fx_SpinClone_Appear` | 3 | 40×40 | 50 | Al lanzarlo: silueta clara del jugador que pasa a clon. Se pone en el clon (que ya sale desde el jugador); después, `Spin` |
| `Fx_SpinClone_Merge` | 3 | 40×40 | 50 | Al recogerlo: se aclara, se recoge en un núcleo con anillo y destello. Centrado en el jugador; el clon desaparece en el cuadro 0 |

Pivotes: centro. Orden de dibujo: ecos debajo, remolino, clon encima.

Revisión: `vista_previa_clon.png` (con el `Spin` original debajo para comparar) y `gif/` (`gif/escenas/escena_bumeran.gif`: sale,
vuela 9 u a 14 u/s, vuelve y se funde). Solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_clon_peonza.py`
