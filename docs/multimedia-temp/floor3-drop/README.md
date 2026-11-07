# Gota de ámbar (piso 3): rediseño de arte

Rediseño pedido por la usuaria (2026-11-06) de la **Gota de ámbar** (mutación especial de Órgano). Sustituye **todo** el arte
anterior de la Gota: `floor3-specials-look/shell/` y `level3/` (bloque octogonal, gota, estrellas, cristales, fósil) y la
glándula `floor3-mutations/Special/Special_AmberGland_{Rest,Full}_*`. La Cápsula tiene su propio rediseño en `floor3-capsule/`.
Todo se dibuja por código (Pillow; Python 3.12 en `%LOCALAPPDATA%\Programs\Python\Python312\python.exe`). Los GIF (`gif/`) y
`__pycache__` no van a git. PNG por LFS. 16 PPU, Point, sin compresión, cuadros en horizontal.

## Decisiones (aprobadas)
- **Bloque = forma C «agujas»**: bloque bajo con puntas de cristal arriba y pinchos que suben pegados a los lados, textura de
  la Cápsula (borde rojizo, interior translúcido, vetas, grietas, reflejo, burbujas). Propuestas A (escalonado) y B (estratos)
  descartadas (`generar_bloque.py`, `propuestas/`).
- **2 tallas dibujadas, sin escalar** (el código elige la más cercana al tamaño del enemigo; nada de escalados no enteros):
  **Normal** 48×40 (pies en y=34, eje x=24): escarabajo, sapo, araña, voladores. **Grande** 56×68 (pies en y=62, eje x=28): plantas.
- Formación rápida (~0,5 s) que sube como la Cápsula; grietas en 3 fases; rotura con esquirlas hasta el radio de daño;
  estrellas de aturdido; disparo = lágrima con estela; impacto = salpicadura.
- **Glándula = cristal de ámbar en el lomo** que crece con la recarga (3 niveles + cargando).
- **Nivel III = bloque-cadáver en ámbar antiguo** (resina vieja más oscura, vetas doradas) con destello y fosilizado al formarse.

## Generadores (ejecutar desde esta carpeta; se importan entre sí)
| Script | Genera |
|---|---|
| `generar_bloque.py` | propuestas A/B/C (referencia) y las funciones de textura comunes |
| `generar_bloque_final.py` | bloque C en las 2 tallas → `bloque/` |
| `generar_formacion.py` | formación → `formacion/` |
| `generar_rotura.py` | grietas, rotura y estrellas → `grietas/`, `rotura/`, `aturdido/` |
| `generar_disparo.py` | gota y salpicadura → `disparo/` |
| `generar_glandula.py` | cristal del lomo en los 3 cuerpos → `glandula/` (usa los anclajes de `floor3-mutations/generar_mutaciones_piso3.py`) |
| `generar_nivel3.py` | bloque-cadáver, su formación y su rotura → `nivel3/`; escena `gif/escenas/escena_gota_III_*.gif` |
| `generar_escena.py` | escena del flujo completo I-II con las hojas exportadas → `gif/escenas/escena_gota_*.gif` |
| `muestra_gema.py`, `muestra_cristal.py` | muestras de glándula (gema DESCARTADA; cristal aprobado) |

## Hojas para importar (`{talla}` = `Normal` o `Large`)
| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `disparo/Proj_AmberDrop` | 4 | 24×12 | 70 | Gota en vuelo, mirando a la derecha (el código la gira). **Pivote (17, 6)** = centro de la cabeza |
| `disparo/Fx_AmberDrop_Hit` | 6 | 32×32 | 60 | Salpicadura centrada en el enemigo. **La formación empieza en el cuadro 2** |
| `formacion/Fx_AmberDropBlock_{talla}_Form_{Back,Front}` | 7 | 48×40 / 56×68 | 70×5 + 90×2 | Sube de abajo arriba; remate con destello y chispa. Enemigo entre Back y Front |
| `bloque/Fx_AmberDropBlock_{talla}_{Back,Front}` | 1 | 48×40 / 56×68 | | Bloque formado (enemigo entre las dos capas) |
| `grietas/Fx_AmberDropBlock_{talla}_Crack` | 3 | 48×40 / 56×68 | | Encima de Front. Fase 1-2-3 según el tiempo que queda (I-II) o los golpes (cadáver III) |
| `aturdido/Fx_AmberDropStun_Stars` | 6 | 32×16 | 100 | Bucle encima del bloque: borde de arriba de la celda **12 px sobre la punta más alta**, centrado en el eje |
| `rotura/Fx_AmberDropBlock_{talla}_Break_{Back,Front}` | 7 | 96×96 | 90 + 60×6 | **Centro del bloque en (48, 48)**. Cuadro 0 = quiebre (rendijas de luz); **cuadro que cuenta = 1** (enemigo libre y daño de esquirlas); anillo de polvo a 35 px = radio de daño (~2,2 casillas). Back vacío desde el 1 |
| `nivel3/Fx_AmberDropBlock3_{talla}_{Back,Front}` | 1 | 48×40 / 56×68 | | Bloque-cadáver (escudo 10 s) |
| `nivel3/Fx_AmberDropBlock3_{talla}_Form_{Back,Front}` | 8 | 48×40 / 56×68 | 70×5 + 90, 90, 110 | Formación III: subida, destello fuerte (5), oscurecimiento (6-7). **Fosilizado del enemigo por shader** desde el cuadro 5, ~0,5 s, mezcla hacia `#3a1a08` **hasta el 75 %** |
| `nivel3/Fx_AmberDropBlock3_{talla}_Break_{Back,Front}` | 7 | 96×96 | 90 + 60×6 | Rotura del cadáver; el código **oculta el cadáver en el cuadro 1** |
| `glandula/Special_AmberGland_{Empty,Half,Full,Charge}_{Frog,Diplocaulus,Whipscorpion}_{anim}_{side,down,up}` | según anim | 56×56 | los del cuerpo | Capa encima del cuerpo, cuadro a cuadro (mismo formato que las capas de floor3-mutations). Empty = recién disparada; Half = a medias; Full = lista (destello en Idle cuadro 1); Charge = mientras se mantiene el clic derecho |

A los jefes, el nivel III solo los aturde 2 s: usan el bloque normal (I-II) con estrellas.

## Pendiente
1. ~~Importar y programar~~ HECHO (2026-11-06, menú `Primordia > Floor 3 > Crear mutaciones especiales del ámbar`; falta probarlo en juego). Lo que se pedía: sustituir el arte viejo de la Gota en `AmberCocoon`,
   `AmberCorpse`, `AmberDropShot`, `AmberGlandGlow` y la capa del Órgano; **cambiar el escalado de `AmberShell`** por elegir talla;
   fases de grietas; glándula con 4 estados; fosilizado por shader. Actualizar `docs/MULTIMEDIA.md`.
2. Después, los **Cercos** (rediseño) y el contorno dorado/motas común de las especiales del ámbar.
