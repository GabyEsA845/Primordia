# Mutaciones especiales de las almejas (piso 2)

Generado con `generar_mutaciones_almejas.py` (Python 3.12 + Pillow). Diseño en el GDD, "Mutaciones especiales de las almejas".
Las capas se calculan desde los sprites de `../floor2-bodies/` cuadro a cuadro: si se rehace un cuerpo, se vuelve a lanzar.

- 16 píxeles por unidad, filtro Point, sin compresión, fondo transparente. Tiras horizontales, cuadros cuadrados del alto de la hoja.
- `side` mira a la derecha (para la izquierda se voltea). Sin capas en Dodge, Hurt ni Death, como en el piso 1.
- La Piel va delante del cuerpo. Los Flagelos de Leanchoilia no tienen capa (excepción aprobada el 2026-10-25): solo el latigazo y el destello de listo.

| Hoja | Cuadros | Lienzo | Uso |
|---|---|---|---|
| `Skin/Skin_MarrellaCuticle_<Cuerpo>_<Idle/Move/Attack>_<side/down/up>.png` | 4 / 4 / 6 | 48×48 | Capa de la Cutícula (brillo iridiscente + espinas); centro en (24, 24) |
| `Effects/Fx_LeanchoiliaFlagella_Lash.png` | 12 (0,1 s) | 48×48 | Latigazo junto al enemigo, centrado en él; el daño entra en el cuadro 4 |
| `Effects/Fx_LeanchoiliaFlagella_Ready.png` | 5 | 32×32 | Destello sobre el jugador cuando el latigazo vuelve a estar listo |
| `Effects/Fx_MarrellaCuticle_Reflect.png` | 6 | 32×32 | Esquiva justa: anillo alrededor del jugador |
| `Effects/Fx_MarrellaCuticle_Stun.png` | 5 | 32×32 | Destello que aturde, sobre el atacante |
| `Effects/Proj_MarrellaCuticle_Reflected.png` | 4 | 12×12 | Proyectil reflejado, hacia la derecha |
| `Effects/Fx_ClonalBud_Sprout.png` | 8 | 32×32 | Brote de la copia; la copia aparece en el cuadro 6 |
| `Effects/Fx_ClonalBud_Dissolve.png` | 8 | 32×32 | La copia se deshace en burbujas |
| `UI/UI_Heart_UnstableInlay_0..2.png` | 1 | 16×16 | Borde dorado agrietado sobre cada corazón (Inestable) |
| `UI/UI_Heart_UnstableCracks_0..2.png` | 1 | 16×16 | Solo las grietas doradas, cuando también está el corazón morado |
| `UI/Fx_Heart_UnstableBreak.png` | 6 | 24×24 | El medio corazón extra se rompe |
| `UI/UI_Special_Badge.png` | 1 | 8×8 | Insignia de mutación especial (esquina del icono) |
| `Icons/Icon_<LeanchoiliaFlagella/MarrellaCuticle/ClonalBud>_<16/32>.png` | 1 | 16 / 32 | Iconos |

- **La copia de la Yema clonal** no tiene hojas: es el sprite del jugador con un shader que lleva su luminancia a la rampa verde agua `#1e6a6a #3aa8a0 #7ee8cc #d8fff0` con un 60 % de opacidad y el contorno en `#b8fff0`.
- `vista_previa.png` (resumen), `vista_previa_capas.png` (todas las hojas de capas sobre su cuerpo) y `gif/` son solo para revisar.
