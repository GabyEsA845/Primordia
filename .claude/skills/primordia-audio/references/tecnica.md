# Técnica de síntesis y lista de comprobación

Recetas que funcionaron en el piso 1 y reglas para que lo nuevo salga igual de bien. Las funciones son de
`sintesis.py`, `efectos.py` y `musica.py`.

## Efectos: cómo se construye un sonido

- **Tres capas:** un **ataque** de 2-10 ms que dice "cuándo" (`click`, ruido filtrado muy corto, el arranque de un
  `thump`), un **cuerpo** que dice "qué es" (tono, burbuja, `noise_sweep`, campana `fm`) y una **cola** opcional
  (burbujas, `reverb` corta, `delay`). Los sonidos frecuentes casi no llevan cola.
- **Agua y biología:** burbuja = seno que sube de tono mientras se apaga (`bubble`, resonancia de Minnaert);
  chapoteo = ruido paso banda con envolvente rápida + 2-3 burbujas; succión/soplo = `noise_sweep` que sube o baja;
  gelatina = `thump` grave + `am` lenta (tiembla) + un filtro que se cierra; quitina y roca = `click` y ruido paso alto
  muy cortos, en grupos; veneno = `fizz` (efervescencia); esporas = `puff` seco.
- **Banda por familia** (para que no se tapen): jugador 400 Hz-4 kHz; enemigos 80-800 Hz con algo de suciedad
  (`dist`, intervalos disonantes); jefes con subgrave 30-120 Hz **más un armónico medio** (`dist` suave o una copia
  a ×2-×3), porque en altavoces de portátil el subgrave solo no se oye; interfaz 1-6 kHz.
- **Filtros:** `lp`, `hp` y `bp` tienen el corte fijo (Butterworth); para un corte que se abre o se cierra con el
  tiempo, `svf(x, glide(...), q, "lp"|"bp"|"hp")` (es lento: reservarlo para sonidos cortos o capas pequeñas).
- **Variantes:** `pv(rng, v)` desplaza el tono un 6 % por variante con algo de azar; cambiar también un detalle
  (cuántas burbujas, dónde caen). Deben parecer el mismo sonido dicho otra vez, no otro sonido.
- **Avisos** (windup, warning, charge): suben de tono o de brillo y llegan al máximo justo al final, cuando ocurre
  la acción; duran exactamente lo que el aviso del código. Un aviso se tiene que oír aunque haya mucho jaleo.
  **Cuidado:** `finish_sfx` recorta lo que está por debajo del 0,3 % del pico al principio, así que un aviso que
  crece desde silencio queda más corto (un `swell` de 0,6 s salió de 0,59 s). Que empiece con un ataque audible
  (un `click` o un 10-20 % de volumen desde la primera muestra) y comprobar la duración con `--tabla`.
- **Golpe recibido por un jefe** (`Hurt`, `Immune`): muy corto (0,1 s), con 3 variantes; el inmune suena "sordo"
  (`lp` bajo, sin brillo) para que diga "no hace daño".
- **Notas:** usar las del piso (constantes como `D4…A6` en `efectos.py`, o `midi(n)`), así los jingles casan con
  la música.
- `finish_sfx` hace el resto: quita la continua, recorta el silencio inicial, fundido de 5 ms y normaliza a −1 dBFS.
  Por eso **el volumen relativo no se decide con la amplitud sino con `vol`** en `@sfx`.

## Bucles (`_Loop` y ambientes)

- Todo lo que dure el bucle tiene que ser cíclico: `loop_noise` (ruido filtrado en frecuencia, ya cíclico),
  `loop_sine` (número entero de ciclos), modulaciones con un número entero de vueltas por bucle
  (`np.sin(2π·k·t/N)`), elementos colocados con `place(..., wrap=True)` y la reverberación con `circular_conv`.
- Los ambientes no llevan ritmo (no pelean con la música), duran 15-30 s y van bajos (`vol` 0,25-0,3).

## Música

- **Tempo con pulso entero de muestras:** `Song` exige que 60·44100/BPM sea entero. Valen, entre otros: 60, 63,
  70, 72, 75, 80, 84, 90, 98, 100, 105, 108, 112, 120, 125, 126, 135, 140, 144, 147, 150, 168, 175, 180.
- Un bucle dura un número exacto de compases; las notas y la reverberación que se salen del final vuelven al
  principio (lo hace `Song`). Los fragmentos (`loop=False`) llevan `tail` y terminan en silencio, salvo que el
  encargo pida corte seco.
- **Capas sincronizadas:** las dos funciones comparten el material (progresión y melodía de una función común, como
  `floor1_material()`), el mismo `Song(bpm, compases)` y solo cambian arreglo y energía.
- Estructura de 32 compases tipo A A' B A con `section_melody` (semilla fija); el pad y el bajo siguen la
  progresión; la melodía la llevan campanas o un lead con eco desplazado (`offset`) para dar espacio.
- `Song.render(target_db=-17)` iguala la sonoridad y limita a −1 dBFS. **El Vorbis rebasa un poco el pico en tramos
  muy densos** (pasó en `Music_Boss_Amoeba_Final`: 2 muestras a 0 dBFS): en pistas con mucho golpe, apuntar a
  `target_db` −18 o bajar el techo de `soft_limit` a −1,5 dBFS.
- Tamaño: a calidad 5, un minuto estéreo ocupa ~1,1 MB; con 12 MB por piso caben ~10 minutos de música.

## Lista de comprobación (antes de enseñar)

- [ ] `lint_audio.py` sin avisos (`!`); lo de revisar (`?`), mirado y decidido.
- [ ] Cada nombre está en las tablas del encargo con sus variantes, y `generar_audio.py` (completo) no da avisos.
- [ ] En el espectrograma, el sonido está en la banda de su familia y se distingue de los que sonarán a la vez.
- [ ] El ataque está en la primera muestra; los frecuentes duran ≤ 0,1 s y no tienen cola larga.
- [ ] Las variantes se parecen sin ser iguales.
- [ ] Los avisos suben hacia el final y duran lo que el aviso del código; las escenas duran lo que la escena.
- [ ] Los bucles no tienen salto (lint) y las capas duran lo mismo en muestras.
- [ ] Lo melódico está en la tonalidad del piso; la interfaz, en re menor.
- [ ] `vol` puesto según la tabla de `pisos.md` y los nuevos frecuentes añadidos a `RHYTHM`.
