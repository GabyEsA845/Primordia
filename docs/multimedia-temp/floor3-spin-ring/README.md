# Anillo de alcance de la Peonza (piso 3, Diplocaulus)

Aprobado el 2026-11-23 (Opus). Tras **1 s girando**, alrededor del jugador se abre un anillo de viento que **daña lo que
lo toca con el mismo daño del giro**: da alcance para no comerse el daño por contacto. **Radio 2,0 u (32 px) en los 3
niveles** (tamaño B; la comparación A 1,7 / B 2,0 / C 2,4 está en `vista_previa_anillo_tamanos.png`). Dibujado a su
tamaño: **no escalar** (el borde punteado es el área exacta del daño). Pivote: centro. Orden: anillo debajo del remolino.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| Fx_SpinningTop_Ring_Appear | 4 | 80×80 | 50 | Al cumplir 1 s de giro: la onda se abre desde el remolino (1,3 u) hasta 2,0 u. El daño empieza en el cuadro 2 |
| Fx_SpinningTop_Ring_Loop | 4 | 80×80 | 50 | Bucle mientras sigue girando (a la par que `Fx_SpinningTop_Whirl`) |
| Fx_SpinningTop_Ring_End | 3 | 80×80 | 50 | Al soltar, marearse o esquivar: se deshace (sin daño) |
| Fx_SpinClone_Ring_Appear | 4 | 80×80 | 40 | **Nivel III:** el clon sale ya con el anillo (a la vez que `Fx_SpinClone_Appear`), desde su remolino de 0,7 u |
| Fx_SpinClone_Ring_Loop | 8 | 80×80 | 40 | Bucle en la ida y la vuelta; un destello recorre el anillo en 8 cuadros |
| Fx_SpinClone_Ring_End | 3 | 80×80 | 40 | Al fundirse con el jugador (con `Fx_SpinClone_Merge`) |

Para el código (`Player/SpinAttack.cs`, `Combat/SpinBoomerang.cs`): el anillo daña con el daño y el intervalo del giro en
2,0 u; el clon también daña en 2,0 u (además de su golpe de 8 + aturdido en `HitRadius`). Si el bumerán queda demasiado
fuerte, la usuaria aceptó probar que el anillo del clon haga solo el daño del giro y el golpe fuerte siga en el centro.

Escenas: `vista_previa_anillo_escenas.png` + `gif/escenas/anillo_{jugador,clon}_{musgo,quemada}.gif`.
Regenerar: `python generar_anillo_peonza.py` (`tamanos` saca otra vez los GIF de comparación en `gif/tamanos/`).
