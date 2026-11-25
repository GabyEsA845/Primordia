---
name: primordia-floor3-audio
description: "Audio del piso 3 EN CURSO (desde 2026-11-24, Opus): generación por tandas; todas las tandas hechas; README_piso3.md para que Sonnet lo conecte; decisiones de la usuaria"
metadata:
  type: project
---

**Decisiones de la usuaria (2026-11-24):** generar TODO el sonido del piso 3 por tandas en `docs/multimedia-temp/audio/`
(`efectos_piso3.py`, tablas en `PROMPT_piso3.md`). **Crear todos los sonidos nuevos posibles** en vez de reutilizar
los de pisos anteriores. La identidad musical (modo y tempo por tramo) se decide con las recomendaciones de Claude.
La sala de paso del piso 3 ya está diseñada, pero su sonido será lo último (cuando se piense la música del piso 4).

**Plan de tandas:** 1 reacciones y terreno · 2 jugador (esquivas por cuerpo, Bocas, mutaciones) · 3 especiales del
ámbar, clases y linajes · 4 enemigos y trampas · 5 salas y escenario · 6-9 un jefe por tanda (Meganeura,
Arthropleura, Megarachne, Lepidodendron) · 10 ambientes y música (proponer antes modo y tempo de cada tramo).

**Estado:** TODAS las tandas generadas (1-10: efectos, 16 pistas en `musica_piso3.py` y 4 ambientes); 1-9 APROBADAS, la 10 (música) a falta de que la escuche. Música: verde re dórico 98, humo re eólico 105, quemado re frigio dominante 112; Meganeura 126/140, Arthropleura 84/105, Megarachne 120/126, Lepidodendron 72/90; stings SecretBoss y Relic. Pauta de tamaño subida a ~16-18 MB (la usuaria, 2026-11-25); la música nueva ocupa ~11,6 MB. Falta: sonido de la sala de paso (con la música del piso 4), commit, MULTIMEDIA.md, pisos.md de la skill, documento del piso, y conectarlo todo (Sonnet, con README_piso3.md). El código de la Arthropleura se lee con `git show feature/boss-arthropleura:<ruta>` (rama sin fusionar). Efectos de escena largos: no pasar de ~300 KB (3,4 s mono); si la escena es más larga, partirla y que el código toque el remate (StormEntry de la Meganeura). En la 4, golpe y muerte propios de cada enemigo (EnemyData los admite). Sin commit todavía (commit
cuando la usuaria lo confirme).

**Piezas nuevas:**
- `muestrario_piso3.html`: muestrario solo del piso 3, lo rehace `generar_audio.py` (filtra por el módulo del
  generador: `FLOOR3_MODULES`).
- `README_piso3.md`: **la usuaria pidió ir anotando en un README cómo implementar los casos complicados para
  Sonnet.** Ampliarlo con cada tanda (bucles con contador, sustituciones, intervalos, avisos).
- Ayudas en `efectos_piso3.py`: `crackle`, `roar`, `whoosh`, `splat`, `leaves`, `clack`, `twang`, `amber_chime`
  (se apaga del todo, sin corte), `resin`, `shards`, `crack`, `suck`, `spore_puff`, `glimmer`.

**Trampas de esta sesión:**
- `lint_audio.py --solo` toma **un** patrón por bandera: repetir `--solo` antes de cada uno (si no, revisa la
  carpeta equivocada y da 200 avisos falsos).
- Heredocs largos con comillas fallan en el Bash: escribir el código en el scratchpad con Write y añadirlo con `cat >>`.
- `sine(f)` / `tri(f)` con frecuencia fija **necesitan la duración** (`sine(f, d)`); sin ella, `n(None)` revienta.
- **No lanzar `generar_audio.py` sin nombres**: lo regenera todo y los `.ogg` cambian en bytes (metadatos del codificador) aunque la música sea igual; los `.wav` salen idénticos. Si pasa, `git restore` de los `.ogg` de pisos anteriores.
- Avisos y graves: lo que va por debajo de ~300 Hz apenas se oye en un portátil; subir burbujeos y añadir siseo.

**How to apply:** usar la skill [[primordia-audio-skill]]; continuar por la tanda siguiente; ampliar el README; al
acabar todo, MULTIMEDIA.md, `references/pisos.md` y el documento del piso ([[primordia-floor-docs]]).
