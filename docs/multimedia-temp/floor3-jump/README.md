# Salto alto de las Patas de salto (piso 3)

Generado con `generar_salto_piso3.py` (skill primordia-sprites; importa `../floor3-bodies/generar_cuerpos_piso3.py` para las formas de los tres cuerpos):

```
python generar_salto_piso3.py frog diplo whip marker web
```

Las hojas de salto se escriben junto a las de cada cuerpo (`../floor3-bodies/<Cuerpo>/`) para que las recoja el mismo importador. Los efectos del marcador van a `Effects/` de esta carpeta.

## Hojas de los cuerpos (en `../floor3-bodies/<Cuerpo>/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Player_<Cuerpo>_Jump_side/down/up` | 5 | 40×40 | (a mano) | Salto de las Patas de salto: agacharse, impulso, vuelo, caída, amortiguar. **El cuadro lo elige el código por el avance del salto**: empieza en 0 %, 12 %, 35 %, 62 % y 88 % |
| `Player_<Cuerpo>_JumpCharge_side/down/up` | 4 | 40×40 | 110 | Cargar el salto del nivel III (bucle): agachado, ojos entornados, temblando 1 px |

`<Cuerpo>`: `Frog`, `Diplocaulus`, `Whipscorpion`. El cuerpo va en el mismo centro que en las hojas de 32×32. El código sube el sprite en arco (altura a medio vuelo = 0,375 × distancia, tope 2,2 u; 1,5 u a 4 u) y deja la sombra en el suelo. Las capas de Piel no tienen estas hojas: se ocultan durante el salto.

## Efectos del marcador (`Effects/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_JumpMarker_Spot` | 6 (bucle) | 24×16 | 110 | Diana de huellas: anillo punteado que gira con una huella de rana en el centro |
| `Fx_JumpMarker_Full` | 6 (bucle) | 24×16 | 110 | Lo mismo con la carga al máximo (late en amarillo) |
| `Fx_JumpMarker_Corner` | 4 | 8×8 | — | Esquinas tenues del radio del impacto (arriba-izq., arriba-der., abajo-der., abajo-izq.); el código las pone a 0,7 × el radio en cada eje |

## Red de las Patas hilanderas

No hay hojas: el prototipo (`web_pixels` / `web_frame` en el generador) está portado a C# en `SilkNetArt.cs`, que teje la red con la forma exacta del polígono: radios, anillos combados, hilo del contorno y rocío dorado. 9 cuadros: 4 se teje, 2 brilla, 3 se deshilacha. Vista previa: `vista_previa_red.png`.
