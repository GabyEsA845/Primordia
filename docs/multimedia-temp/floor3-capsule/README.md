# Cápsula de ámbar (piso 3): rediseño de arte

Rediseño pedido por la usuaria (2026-11-06) de la **Cápsula de ámbar** (mutación especial de Piel). Sustituye a
`floor3-specials-look/shell/` y `level3/` (bloque octogonal 48×56) **solo en lo de la Cápsula**; la Gota y los Cercos siguen con lo anterior.
Todo se dibuja por código (Pillow). Python 3.12: `%LOCALAPPDATA%\Programs\Python\Python312\python.exe` (en un PC nuevo, `winget install Python.Python.3.12 --scope user` y `pip install pillow`).
Los GIF (`gif/`) y `__pycache__` no van a git; se regeneran. PNG por LFS.

## Decisiones de diseño (aprobadas)
- **Forma = propuesta D**: gota de agua alargada hacia arriba, punta roma de ~4 px, **pinchos laterales que suben desde el suelo** (punta arriba, pendiente larga por debajo), textura de ámbar de la foto de referencia (borde rojizo, centro amarillo translúcido, vetas fibrosas, grietas finas, reflejo largo, burbujas). 46×44 px, con holgura sobre la rana. Dos capas (Back detrás de la criatura, Front translúcida delante). Encaja con los 3 cuerpos (rana, Diplocaulus, vinagrillo; el vinagrillo casi se pierde y a la usuaria le parece bien).
- **Nivel III = la misma cápsula, ~20 % más ancha y con más grietas** (las 3 propuestas de «nivel III distinto» se descartaron). Lo que cambia son las animaciones: vetas de ámbar por el suelo al cristalizar, halo dorado, etc.
- **Formación**: el cristal sube de abajo arriba, remate con destello y brillo dorado; **el tiempo se detiene mientras se forma** (lo hará el código, pendiente).
- **Rotura**: 4 cuadros de grietas desde el centro → 1 cuadro de quiebre (8 trozos con rendijas de luz) → estallido (trozos con contorno que giran y caen + 16 esquirlas + polvo). El nivel III: las vetas del suelo vibran y se disuelven en motas.
- **Curación** (2 s): un pulso dorado por medio corazón curado (1/2/3) + pulso final más fuerte; un corazón dorado por pulso que sube ondulando; motas dentro del cristal; tinte dorado de la criatura por shader (curva `TINT`).
- **Halo con alas** (aspecto de la mutación ANTES de activarse): sustituye a la costra de resina del lomo (`floor3-mutations/Special/Special_AmberCapsule_*`). Objeto aparte que levita sobre la cabeza; 1 gema (I-II) o 2 (III, 2 cargas); al gastarse pasa a tono apagado.

## Generadores (ejecutar desde esta carpeta; cada uno importa a los anteriores)
| Script | Genera | Carpeta de salida |
|---|---|---|
| `generar_capsulas.py` | 3 propuestas A/B/C descartadas (referencia) | `propuestas/`, `vista_previa_capsulas*.png` |
| `generar_capsula_d.py` | **Cápsula D aprobada** (Back/Front 46×48) | `propuestas/Capsule_D_{Back,Front}.png`, `vista_previa_capsula_d*.png` |
| `comprobar_cuerpos.py` | los 3 cuerpos dentro de la cápsula | `comprobar_cuerpos_capsula_d.png` |
| `generar_capsula_nivel3.py` | 3 propuestas de nivel III distinto (DESCARTADAS; sus funciones las reutilizan los demás) | `nivel3/`, `vista_previa_nivel3.png` |
| `generar_cristalizacion.py` | formación (nivel I-II y III) + vetas del suelo | `cristalizacion/` |
| `generar_rotura.py` | agrietado, quiebre y estallido | `rotura/` |
| `generar_curacion.py` | curación (1, 2 y 3 medios corazones) | `curacion/` |
| `generar_halo.py` | halo con alas (listo/medio/apagado/fundido/resplandor) | `halo/` |

## Hojas para importar (todas con celda del ancho/alto indicado, cuadros horizontales, 16 PPU, Point, sin compresión)
Cápsula en reposo (imagen fija, sin hoja propia aún): `propuestas/Capsule_D_{Back,Front}.png` para I-II (celda 46×48, pies en y=41); el nivel III usa el generador (`capsule3()` en `generar_cristalizacion.py`). **Falta exportarlas** como hoja de reposo (ver «Pendiente»).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `cristalizacion/Fx_AmberCapsule_Form_{Back,Front}` | 12 | 58×60 | 90×8 + 110×4 | Formación I-II (8 sube + 4 remate). Pies en y=51, eje x=29. La criatura va entre Back y Front |
| `cristalizacion/Fx_AmberCapsule3_Form_{Back,Front}` | 12 | 58×60 | igual | Formación III (cápsula mayor) |
| `cristalizacion/Fx_AmberCapsule_Form_Glow` | 12 | 58×60 | igual | Halo y motas del remate, detrás de todo (igual en I-II y III) |
| `cristalizacion/Fx_AmberFloorVeins_Form` | 12 | 96×16 | igual | Vetas del suelo (solo III); fila 0 = y 44 de la celda de la cápsula; eje x=48 |
| `rotura/Fx_AmberCapsule_Break_{Back,Front}` / `3_` | 12 | 96×60 | 110×4, 90, 70×7 | Rotura I-II y III. **Cuadro que cuenta: el 5º (índice 4, el quiebre)**: libera al jugador y reparte esquirlas. Desde el 6º, Back vacío |
| `rotura/Fx_AmberCapsule_Break_Glow` | 12 | 96×60 | igual | Destello y polvo (detrás de todo) |
| `rotura/Fx_AmberFloorVeins_Break` | 12 | 96×16 | igual | Vetas del suelo vibran y se disuelven (III) |
| `curacion/Fx_AmberCapsule_Heal{1,2,3}_{Back,Front,Hearts,Glow}` | 20 | 96×60 | 100 | Capas superpuestas a la cápsula en reposo: núcleo+motas (Back), ondas+latido (Front), corazones+destellos (Hearts, lo más arriba), halo (Glow). Heal1/2 con la cápsula I-II, Heal3 con la III. Pulsos que curan: cuadro `s+3` de `STARTS` (1: [2]; 2: [2,7]; 3: [2,6,10]); pulso final en el 14 |
| `halo/Fx_AmberHalo_{I,III_Full,III_Half}` | 8 | 44×28 | 120 | Bucle del halo (1 gema / 2 encendidas / 1 encendida y 1 apagada). Pivote: centro del anillo (22,18); colocarlo ~9 px sobre el cráneo según el cuerpo |
| `halo/Fx_AmberHalo_Off_{I,III}` | 1 | 44×28 | | Apagado (alas caídas, gemas oscuras) |
| `halo/Fx_AmberHalo_Fade_I` | 4 | 44×28 | 90 | Se apaga al activarse; al revés = se repone |
| `halo/Fx_AmberHalo_Glow` | 8 | 56×36 | 120 | Resplandor tenue que respira, detrás del halo (centro (28,20)) |

Tinte de la criatura en la curación (shader, mezcla hacia `#fff0b0`): `TINT` en `generar_curacion.py` (I: 1 pulso; II: 2; III: 3; máx. 0,45).

## Pendiente
1. **Animación de REPOSO** de la cápsula (bucle: brillo que cruza, burbujas, latido del núcleo; variante III). Hoy solo hay imagen fija. Exportar también Back/Front de reposo en celda 58×60 (como la formación).
2. Importar todo en Unity (menú `Primordia > Floor 3 > ...`, Sonnet) y sustituir a `Fx_AmberShell*` en `AmberShell`/`AmberCapsuleShield`; **tiempo detenido durante la formación**, halo en lugar de la capa del lomo (`Special_AmberCapsule_*`), curación (pulsos y corazones por medio corazón).
3. Actualizar `docs/MULTIMEDIA.md` con esta carpeta.
4. Si la usuaria lo pide: agrandar/alargar corazones, gemas del halo (5 px, poco visibles), grietas de los cuadros 1-3 de la rotura (finas), trozos de la rotura (planos, sin textura).
5. La Gota de ámbar y los Cercos siguen por rediseñar (la usuaria dijo que empezaba por la Cápsula).
