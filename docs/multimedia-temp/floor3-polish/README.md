# Pulido del piso 3

Tanda de arte de noviembre de 2026: lo provisional que dejaron la estructura y las reacciones del piso 3. Todo sale de
`generar_pulido_piso3.py` (skill primordia-sprites): lee los cuerpos, los enemigos y los arcos ya aprobados y **no los
cambia**. Las escenas de revisión (`gif/`, no van a git) simulan en Python lo que hará el código, para que se vea igual.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_pulido_piso3.py`

Vistas previas: `vista_previa_pulido.png` (hojas), `vista_previa_cuerpos.png` (los estados sobre los 3 cuerpos en 3
direcciones y 3 enemigos), `vista_previa_puertas.png` (arco · delante · sombra, 10 temas).

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, cortar por celda.
`Fx_MudBody_Tex` y `Fx_MudBody_Noise`: *Single*, **Wrap Mode = Repeat**, *Read/Write* si el shader no los muestrea.

## Hojas

| Hoja | Cuadros | Celda | ms | Pivote | Uso |
|---|---|---|---|---|---|
| `Effects/Fx_MudBody_Tex` | 1 | 16×16 | — | — | Textura de lodo tileable (grumos con luz arriba a la izquierda, brillo húmedo) |
| `Effects/Fx_MudBody_Noise` | 1 | 32×32 | — | — | Ruido tileable en gris (8 escalones): perfil del borde y manchas al salir |
| `Effects/Fx_MudRing_<S/M/L>` / `_Back` | 4 (bucle) | 16×8 / 30×8 / 44×8 | 120 | centro | Anillo de lodo en la superficie: la mitad de delante (va delante del cuerpo) y la de detrás. S para bichos pequeños, M para el jugador y los normales, L para los grandes |
| `Effects/Fx_MudBubble` | 7 | 8×8 | 70 | abajo | Burbuja en el borde del lodo: se hincha y revienta |
| `Effects/Fx_MudDrip` | 6 | 6×10 | 80 | arriba | Goterón que cae del cuerpo al salir |
| `Effects/Fx_BodyFlame_S` / `_M` / `_L` | 6 (bucle) | 16 / 24 / 32 | 80 | abajo | Hoguera sobre el cuerpo; la L lleva humo. S para cuerpos de < 14 px de alto, M para el jugador y los normales, L para los grandes |
| `Effects/Fx_FlameLick` | 5 | 8×10 | 70 | abajo | Lengua suelta que nace en el contorno y se apaga en chispa (el código las reparte por la parte de arriba de la silueta) |
| `Effects/Fx_WaterRipple` | 5 | 20×10 | 90 | centro | Onda bajo los pies, una por paso |
| `Effects/Fx_WaterSplash` | 6 | 24×20 | 70 | abajo | Salpicadura al entrar en la charca |
| `Effects/Fx_WaterDrip` | 4 | 4×8 | 90 | arriba | Gota que cae del cuerpo al salir del agua (~1 s) |
| `Effects/Fx_SteamHiss` | 7 | 32×32 | 80 | abajo | Vapor y chispas que se apagan: entrar ardiendo en el agua |
| `Effects/Fx_VentGust` | 4 | 24×16 | 70 | derecha, centro | Ráfaga detrás de lo que empuja la fumarola; mira hacia donde va (*Flip X* a la izquierda) |
| `UI/Icon_StatusBurn_Empty` / `_Full` | 1 | 14×16 | — | centro | Icono de la barra de quemado |
| `UI/Icon_StatusToxic_Empty` / `_Full` | 1 | 17×11 | — | centro | Icono de la barra de la nube tóxica (nube con calavera) |
| `Doors/<Tema>/Doorway_Top_Front` | 1 | 32×32 | — | = arco | El arco sin el hueco: va **delante** del jugador |
| `Doors/<Tema>/Doorway_Top_Shade` | 1 | 32×32 | — | = arco | El hueco, translúcido y más opaco arriba: oscurece al jugador al entrar |

Temas de las puertas: Soup, Amoeba, Shallows, Cambrian, Abyss, Shore, Boss (pisos 1-2) y Swamp, Smoke, Burnt (piso 3).

## Lo que hace el código

**Lodo profundo** (sustituye la máscara recta de `MudSinkVisual`; nivel `L = PlayerMudSink.Sink × 0,85 × alto del cuerpo`):
- El cuerpo **baja** `L × 0,5` px (`SINK_SHARE`) y lo que queda por debajo de la superficie no se ve. En la superficie,
  `Fx_MudRing_<talla>_Back` detrás del cuerpo y `Fx_MudRing_<talla>` delante.
- Encima, un shader por capa (cuerpo, Boca, mutaciones) pinta `Fx_MudBody_Tex` donde `altura sobre los pies ≤ L +
  (ruido(x, t) − 0,5) × 3,2 + sen(1,3x + 0,9t) × 0,6`. La fila de arriba (borde) va en el tono claro o el brillo húmedo.
  Ajuste por la luz del cuerpo: un tono más oscuro donde el cuerpo es oscuro (cuartil bajo) y uno más claro en el alto.
  **No se tapan** el contorno (`#181425`) ni el blanco de los ojos (`#f4f4e8`): los ojos asoman siempre.
- `Fx_MudBubble` en el borde cada ~0,3 s.
- Al salir: quedan manchas donde `ruido ≥ limpio` hasta el nivel máximo alcanzado; `limpio` sube de ~0,25 a 1 con la
  distancia andada (~2,5 tiles), y mientras, `Fx_MudDrip` cae de vez en cuando.
- **Enemigos:** igual, pero sin textura (dos tonos por la luz del cuerpo + borde) y sin manchas al salir.

**Puertas de arriba** (todos los pisos): `Doorway_Top` sigue detrás. Mientras el jugador cruza, `Doorway_Top_Shade` se
aplica **solo sobre el jugador** (máscara de su silueta o tinte por altura) y `Doorway_Top_Front` se dibuja delante de él.

**Arder:** `Fx_BodyFlame_<S/M/L>` con la base cerca del centro bajo del cuerpo (en la rana, ~3 px por encima de los pies),
2-3 `Fx_FlameLick` en la parte de arriba de la silueta y el cuerpo con brillo naranja **sumado** (`#f88c2a ×` 0,08 → 0,22,
titila por cuadro; no se mezcla, para que un cuerpo oscuro no se vuelva gris). El contorno no se tiñe. Sustituye a
`Fx_Burning` en el jugador; los enemigos también lo usan.

**Iconos de barra** (sustituyen a `BurnIcon` por código): `_Empty` debajo y `_Full` recortado de abajo arriba por la barra.

**Agua de charca:** al entrar `Fx_WaterSplash`; dentro, la franja de 4 px de abajo del cuerpo se ve al 55 % del agua
(`#2a5e54`) con la línea de agua (`#9cc8a0` / `#4e8a72`, titila) y una `Fx_WaterRipple` por paso; al salir, `Fx_WaterDrip`
~1 s. Si entra ardiendo, `Fx_SteamHiss` y se apaga el quemado (igual que el lodo).

**Fumarola:** en el cuadro 1 del chorro, empuja ~2 tiles en 0,2 s hacia el lado de la columna más cercano a jugador y
enemigos (la esquiva lo evita) con `Fx_VentGust` detrás. Los odres de metano llevan una luz 2D cian suave (solo código).
