---
name: primordia-audio
description: Cómo se hacen la música, los ambientes y los efectos de sonido de Primordia, sintetizados por código con Python (numpy/scipy) en docs/multimedia-temp/audio/ e importados con el menú Primordia > Audio > Importar audio. Usar siempre que haya que crear, rehacer, corregir, revisar o conectar cualquier sonido o pista del juego, o preparar un encargo de audio, aunque no se diga "audio": música de un piso o de un jefe, capas de exploración y combate, fragmentos, ambientes, sonidos de enemigos, jefes, mutaciones, linajes, interfaz o escenario.
---

# Audio de Primordia

Todo el audio del juego se **sintetiza por código**: osciladores, ruido, envolventes, filtros, retardo y
reverberación con numpy y scipy, con semilla fija, para que volver a ejecutar dé lo mismo. Todo vive en una sola
carpeta, `docs/multimedia-temp/audio/`, porque el menú de importación lee de ahí un único `sonidos.json`.

Referencias (leer la que toque, no todas):
- `references/pisos.md`: identidad sonora de cada piso (modo, tempo, instrumentos, paleta por familia).
  **Leer la sección del piso en el que se trabaja.**
- `references/tecnica.md`: recetas de síntesis, bucles, música, sonoridad y la lista de comprobación.
- `references/plantilla_piso.py`: punto de partida de los módulos de un piso nuevo.
- `scripts/lint_audio.py`: revisión automática y espectrogramas.

## Carpeta y piezas

| Archivo | Qué es |
|---|---|
| `PROMPT.md` | El encargo del piso 1: estilo, formatos y **tablas** de sonidos (`Archivo`, `Var.`, `Duración`, `Cuándo suena`, `Descripción`). Es la fuente de verdad de los nombres |
| `sintesis.py` | Biblioteca: osciladores (`sine`, `tri`, `square`, `saw`, `fm`), `noise`, envolventes (`expenv`, `adsr`, `swell`), filtros (`lp`, `hp`, `bp`, `svf`, `noise_sweep`), `reverb`, `delay`, `circular_conv`, montaje (`mix`, `place`), `bubble`/`bubbles`, `thump`, `click`, `finish_sfx`, `write_wav` |
| `efectos.py` | Efectos y ambientes del piso 1, registrados con `@sfx(nombre, variantes, vol, group, loop)` en `SFX`; ayudas `pv` (tono por variante), `note`, `arp`, `bell`, `loop_noise`, `loop_sine`, `puff`, `fizz` |
| `musica.py` | Secuenciador `Song` (bucles exactos que vuelven al principio, reverberación circular), instrumentos `i_*`, acordes y escalas, `section_melody`, `play_pad`, `bubble_bed`, y las pistas en `MUSIC = {nombre: (fn, bucle, vol)}` |
| `generar_audio.py` | Genera todo o solo lo que se le pase, escribe `sonidos.json` y `muestrario.html`, y compara con las tablas del encargo |
| `SFX/`, `Music/`, `muestrario/` | Salida: `.wav` (LFS), `.ogg` (LFS) y los bucles repetidos dos veces |

**Un piso nuevo** no se mete en `efectos.py` ni en `musica.py`: va en `efectos_pisoN.py` y `musica_pisoN.py`
(desde `references/plantilla_piso.py`), que registran en los mismos `SFX` y `MUSIC`, con su encargo en
`PROMPT_pisoN.md` (mismas tablas). La primera vez, `generar_audio.py` necesita dos cambios pequeños: importar los
módulos de los pisos y leer las tablas de todos los `PROMPT*.md`.

## Reglas fijas

- **Formatos:** efectos `.wav` PCM 16 bits, 44 100 Hz, **mono**, pico −1 dBFS, sin silencio al principio y con
  fundido de 5 ms al final (`finish_sfx` lo hace). Música `.ogg` Vorbis calidad 5, estéreo, unos −17 dBFS de RMS
  (`Song.render`). Bucles y música exactos en compases y sin empalme audible.
- **Nombres:** `SFX_<Categoría>_<Nombre>[_0N].wav` y `Music_<Nombre>.ogg`, en inglés, con mayúscula inicial, igual
  que en las tablas del encargo. Las variantes van de `_01` a `_03`; con una sola, sin número. El `id` que usa el
  código es el nombre sin `SFX_` ni número (`Sounds.cs`).
- **Legible y corto:** se reconoce en una fracción de segundo y no tapa a los demás. Lo que suena muchas veces
  (disparos, golpes, biomasa, salvas) dura ≤ 0,1 s, es suave y tiene 2-3 variantes; se escucha con «Ritmo real»
  (`RHYTHM` en `generar_audio.py`).
- **Paleta por familia**, siempre: jugador en tonos medios y limpios; enemigos más graves, viscosos y algo
  disonantes; jefes enormes con cuerpo; veneno y hongos con siseos y polvo; interfaz con blips de burbuja.
  Nada de metal, pólvora ni armas de fuego: todo es biología.
- **Tonalidad común a todo el juego: re** (re menor / re dórico), decidida en el encargo del piso 1 para que
  avisos, jingles, interfaz y música nunca choquen. Cada piso, franja o jefe cambia el **color con el modo**
  (dórico, eólico, frigio; la tienda en su relativo fa mayor), no la tónica (ver `pisos.md`).
- **Tiempos del código:** un aviso (windup) dura lo mismo que el aviso del juego y llega a su punto más alto justo
  cuando ocurre la acción; las escenas y cinemáticas duran exactamente lo que dura la escena en el código.
- **Capas sincronizadas** (exploración y combate de un piso): mismo tempo, tonalidad, armonía y duración exacta en
  muestras; el juego las reproduce a la vez y funde una con otra.
- **WebGL:** sin mezclador ni filtros de Unity; el volumen relativo va en `vol` (0 a 1) y en el grupo (`SFX`, `UI`,
  `Music`). La música de un piso no pasa de unos 12 MB; un efecto, de 200-300 KB.
- Audio ya aprobado: **no se regenera ni se "mejora" sin que el usuario lo pida.** Generar solo los nombres
  nuevos (`generar_audio.py Nombre …`), para no reescribir archivos de LFS sin necesidad.

## Flujo de trabajo

1. **Encargo.** Reunir qué tiene que sonar: memoria del módulo, GDD, eventos del código (`event`, `Changed`,
   `Started`…) y sonidos pendientes apuntados en otros módulos. Leer la sección del piso en `pisos.md`. Si falta la
   identidad sonora del piso (modo, tempo, instrumentos, ambiente), **proponerla y preguntar** antes de
   sintetizar. Escribir las tablas en `PROMPT_pisoN.md` (o añadir filas) y enseñarlas: **el usuario aprueba la
   lista** antes de generar nada.
2. **Generadores.** Un generador por sonido con `@sfx`, o una función por pista en `MUSIC`, con parámetros por
   capa (ataque, cuerpo, cola) y variantes con `pv(rng, v)`. Reutilizar las ayudas de `efectos.py` y `musica.py`.
3. **Ejecutar** desde la carpeta: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_audio.py Nombre …`
   (el `python` del PATH es el alias de la Microsoft Store). Sin nombres lo genera todo y hace la comparación
   completa con las tablas.
4. **Revisar antes de enseñar (no puedo escuchar: mínimo 2 pasadas):**
   - `python .claude/skills/primordia-audio/scripts/lint_audio.py --solo 'SFX_Boss_X*' --espectros <scratchpad>/espectros`.
     Los **avisos** (`!`) se corrigen; lo de **revisar** (`?`) se mira y se decide.
   - Abrir con Read los espectrogramas y repasar la lista de comprobación de `references/tecnica.md`: banda de
     frecuencias de su familia, forma de la envolvente, que las variantes se parezcan sin ser iguales, que un aviso
     suba hacia el final.
   - Corregir el generador y volver a ejecutar.
5. **Enseñar al usuario:** que abra `docs/multimedia-temp/audio/muestrario.html` en el navegador (reproductores,
   «Empalme ×2», «Ritmo real»); decir qué se buscó con cada sonido y qué falta por decidir. Los retoques que pida
   se hacen en el generador y se vuelve al paso 3.
6. **Importar y conectar** en la rama del módulo: menú **Primordia > Audio > Importar audio** (`AudioSetup.cs`
   copia a `Assets/_Primordia/Audio/SFX|Music`, crea la `SoundLibrary` y rellena los campos de sonido de los
   datos). Lo que pide el código va en `Sounds.cs` y se toca con `AudioManager`; la música por sala y estado, en
   `MusicDirector`. Los sonidos que dependen de datos (`AttackData`, `EnemyData`, `MutationData`) se asignan en
   `AudioSetup`. El usuario lo prueba en Unity; **commit solo cuando lo confirme**.
7. **Documentar:** tablas del encargo al día, `docs/MULTIMEDIA.md` y, si cambia la identidad de un piso,
   `references/pisos.md`.

## Git y carpetas

- `docs/multimedia-temp/audio/` va en git (WAV y OGG por LFS), salvo las cachés de Python.
- Espectrogramas, pruebas y comparaciones van al scratchpad de la sesión, no al repositorio.
- Esta skill vive en `.claude/skills/primordia-audio/` y viaja con el repositorio a los dos equipos.
