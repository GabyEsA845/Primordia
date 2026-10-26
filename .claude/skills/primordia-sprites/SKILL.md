---
name: primordia-sprites
description: Cómo se hace el pixel art de Primordia (sprites, hojas de animación, efectos, proyectiles, iconos, tiles y decoración) con generadores de Python en docs/multimedia-temp/. Usar siempre que haya que crear, rehacer, corregir o revisar arte del juego, o preparar un encargo de arte, aunque no se diga "pixel art": personajes, enemigos, jefes, mutaciones, piezas de linaje, efectos, interfaz, escenario de cualquier piso.
---

# Sprites de Primordia

Todo el arte del juego se dibuja **por código**: un script de Python con Pillow por recurso, en
`docs/multimedia-temp/<recurso>/`, que escribe las hojas PNG, una vista previa y GIF de revisión. Esta skill
reúne las reglas fijas del estilo, la biblioteca común y la forma de trabajar con el usuario.

Referencias (leer la que toque, no todas):
- `references/pisos.md`: tamaños, paletas y temas de cada piso. **Leer la sección del piso en el que se trabaja.**
- `references/tecnica.md`: reglas de pixel art (rampas, sombra, contorno, animación) y la lista de comprobación.
- `references/plantilla_generador.py`: punto de partida de un generador nuevo.
- `scripts/pixel.py`: biblioteca común. `scripts/lint_sprites.py`: revisión automática.

## Reglas fijas

- **16 píxeles por unidad** en todo el juego. Nunca mezclar tamaños de píxel: el detalle viene de que las criaturas
  crecen piso a piso (célula 16, océano 24, siguientes ~32), no de escalar.
- **Luz arriba a la izquierda.** Contorno de 1 px `OUTL = (24, 20, 37)` (morado muy oscuro, nunca negro puro).
  Las partes finas de 1-2 px (patas, brazos, antenas, tentáculos) van sin contorno.
- **Sin antialiasing** en sprites de 32 px o menos. Sin negro puro; el blanco puro solo en el centro de chispas y
  destellos, y en el cuadro blanco de `Hurt`.
- **Orientación:** los personajes miran a la derecha (`side`); la izquierda la hace Unity con *Flip X*. `down` mira
  a la cámara y `up` está de espaldas. Bocas y armas, dibujadas hacia la derecha con la unión en el borde izquierdo
  a media altura.
- **Hojas horizontales**, un cuadro por celda; la celda es cuadrada salvo que el README diga otra cosa
  (proyectiles 16×8, 12×6...). Nombres: `<Personaje>_<Animación>[_<dirección>].png`, `Fx_<Nombre>`, `Proj_<Nombre>`,
  `Icon_<Nombre>`, `UI_<Nombre>`; en inglés y con mayúscula inicial, como los prefabs.
- **Estados comunes:** `Hurt` = 2 cuadros (silueta blanca y luego roja con `RED4`); `Death` con un lienzo mayor
  (24 → 32) y, según el piso, se apaga (`PALE4`) o se fosiliza (`STONE4`). Ojos de la familia de la célula; los
  enemigos, con los ojos enfadados.
- **El cuadro que cuenta** (cuándo sale el disparo, cuándo muerde, cuándo cura) se decide al dibujar y se apunta en
  el README, porque el código lo usa.
- Arte ya aprobado: **no se regenera ni se "mejora" sin que el usuario lo pida.** Para retocar un generador antiguo,
  usar `shade_legacy` y sus rampas de siempre, así lo demás no cambia.

## Flujo de trabajo

1. **Entender el encargo.** Leer el diseño (memoria del módulo, GDD, `PROMPT.md` o README de la carpeta) y la sección
   del piso en `references/pisos.md`. Para mantener la coherencia, **mirar las vistas previas** (`vista_previa*.png`)
   de las piezas vecinas con Read; no releer sus generadores enteros. Si falta una decisión de diseño visible
   (forma, color dominante, cuántos cuadros, qué hace la animación), preguntar antes de dibujar.
2. **Generador.** Crear `docs/multimedia-temp/<recurso>/generar_<recurso>.py` a partir de
   `references/plantilla_generador.py` (importa `pixel.py`). Rampas con `ramp(color_base)` o de la paleta del piso;
   formas redondeadas con `ellipse`/`rot_ellipse`/`fill` (ya sombrean bien). Un cuadro = una llamada con parámetros
   (fase, aplastamiento, pose), no píxeles copiados.
3. **Ejecutar** con `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_<recurso>.py` (el `python` del PATH
   es el alias de la Microsoft Store). Escribe las hojas, `vista_previa_<recurso>.png` y `gif/`.
4. **Revisar antes de enseñar (mínimo 2 pasadas):**
   - `python .claude/skills/primordia-sprites/scripts/lint_sprites.py docs/multimedia-temp/<recurso> --marcas <scratchpad>/lint`
     (con `--celda AxB` si las celdas no son cuadradas). Los **avisos** se corrigen; lo de **revisar** se mira en la
     imagen marcada y se decide (chispas sueltas y partes finas sin contorno suelen estar bien).
   - Abrir con Read la vista previa (y la imagen marcada) y repasar la lista de comprobación de `references/tecnica.md`.
   - Corregir el generador y volver a ejecutar.
5. **Enseñar al usuario** las rutas de la vista previa y los GIF, decir qué se decidió y qué falta por decidir.
   Esperar su aprobación; los cambios que pida se hacen en el generador y se vuelve al paso 3.
6. **Documentar:** `README.md` de la carpeta con la tabla de hojas (hoja, cuadros, celda, ms, uso), pivotes que no
   sean el centro, cuadro que cuenta y la línea para regenerar. Añadir el recurso a `docs/MULTIMEDIA.md`.
7. **Importar** en la rama del módulo (`feature/<módulo>`): menú de editor `Primordia > Art > Importar …` en
   `Assets/_Primordia/Editor/ArtSetup.<Módulo>.cs` (Multiple, 16 PPU, Point, sin compresión; cortes, clips y
   controladores), sin sobrescribir lo que no toca. El usuario lo prueba en Unity; **commit solo cuando lo confirme**.

## Git y carpetas

- `docs/multimedia-temp/` va en git (PNG por LFS), salvo los GIF y las cachés de Python.
- Las imágenes de trabajo (marcas del linter, pruebas, comparaciones) van al scratchpad de la sesión, no al repositorio.
- Esta skill vive en `.claude/skills/primordia-sprites/` y viaja con el repositorio a los dos equipos.

## Biblioteca `pixel.py` (resumen)

| Qué | Para qué |
|---|---|
| `Canvas(w, h=None)`, `put`, `put_in`, `put_out`, `outline()`, `clean()`, `img()` | Lienzo de píxeles; `put_in` dibuja solo encima del cuerpo y `put_out` solo detrás o alrededor; `clean()` quita esquinas dobles del contorno |
| `ramp(base, n=4)`, `ramp_info(r)` | Rampa con el tono desplazado (sombras a azul-morado, luces a amarillo); `ramp_info` dice si una rampa es plana |
| `ellipse`, `rot_ellipse`, `fill(cv, inside, ...)` | Formas sombreadas como una cúpula con la luz arriba a la izquierda (`shift` oscurece o aclara) |
| `shade`, `shade_legacy`, `SHADE` | Sombra nueva (por defecto) y la de antes; `SHADE = shade_legacy` en un generador antiguo |
| `sparks`, `bubble`, `EYE`, `SPARK`, `RED4`, `PALE4`, `STONE4`, `SAND`, `WHITE4` | Detalles y rampas comunes |
| `save_sheet`, `preview`, `gif`, `compare`, `ramp_strip` | Hojas, vista previa por filas, GIF, antes/después y muestrario de rampas |

`python pixel.py rampa '#e05a5a' 5` imprime una rampa y su giro de tono.
