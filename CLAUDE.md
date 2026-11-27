# Primordia — instrucciones para Claude

Antes de hacer nada, lee estos archivos, en este orden:

1. `docs/memoria-claude/MEMORY.md` — índice de la memoria del proyecto (reglas de trabajo, estado, diseño de cada piso, decisiones). Abre después los archivos que enlaza y que vengan al caso, empezando por `primordia-project-status.md`.
2. `.claude/skills/` — skills del proyecto (`primordia-sprites`, `primordia-audio`). Úsalas siempre que haya arte o audio nuevo.

## Reglas básicas (resumen; el detalle está en la memoria)

- Responde siempre en español.
- Una rama por módulo. Haz commit solo después de que el usuario confirme que lo probó.
- Los cambios de editor de Unity se hacen con menús `Primordia` (no a mano). Cuidado con Unity abierto, statics, CRLF y referencias perdidas (ver `primordia-unity-gotchas.md`).
- Con Codex: Claude dirige y escribe las tareas en `docs/codex/`; Codex implementa en ramas `codex/*` (ver `AGENTS.md` y `primordia-codex-paused.md`).
- Antes de crear cualquier sprite o animación, pregunta al usuario.
- Cuando termines algo, actualiza el documento del piso correspondiente y la copia de la memoria en `docs/memoria-claude/`.
- El usuario tiene un límite de uso estricto: sé económico con los tokens y avisa si una tarea de diseño/arte (modelo más potente) o de implementación (modelo más barato) conviene hacerla con otro modelo.

## Memoria

La carpeta `docs/memoria-claude/` es la fuente de verdad compartida entre cuentas. Si aprendes algo nuevo y no obvio, guárdalo ahí (un hecho por archivo, con su línea en `MEMORY.md`) y no solo en la memoria local de tu cuenta.

## Siguiente paso

**Jefes opcionales ENGANCHADOS al piso 3 (2026-11-27, rama `feature/floor3-boss-hook`, sale de `feature/pause-menu`; con commit y en origin). Esta es la rama desde la que seguir.** El Nido de la Megarachne (33 %, Bosque humeante) y el Lepidodendron (tras el muro de raíces, con fundido a su arena) ya salen en partida. Corregidos el bucle de la transición en la arena y el sonido de fumarolas desde todo el piso. La usuaria probó la arena del Lepidodendron («funciona mucho mejor»); **falta confirmar** el combate completo y la recompensa dentro del piso, el Nido en partida y el sonido de las fumarolas. **Siguiente:** revisar los fallos de pruebas, empezando por las piezas inflamables de la Arthropleura que quedan en `Flammables`; luego el cierre del piso 3. En un ordenador nuevo: Git LFS, `git config user.name/email`, `git fetch`, `git checkout feature/floor3-boss-hook`, Unity 6000.6.2f1 y leer `docs/memoria-claude/primordia-unity-batchmode.md` (la ruta del proyecto cambia en cada equipo). Detalle en `primordia-project-status.md` (última sección).

**Menú de pausa nuevo y bestiario: HECHO, probado por la usuaria, con commit y en origin (2026-11-26, rama `feature/pause-menu`).** Contiene el audio del piso 3, la sala de la Arthropleura y el registro de los dos jefes finales en `Floor_3`). En un ordenador nuevo: Git LFS, `git config user.name/email`, `git fetch`, `git checkout feature/pause-menu`, Python 3.12 (`winget install Python.Python.3.12 --scope user`, `pip install pillow numpy`) y leer `docs/memoria-claude/primordia-unity-batchmode.md`. Sin subir a propósito: `Assets/TextMesh Pro*` (se importa desde Unity) y `Pantano carbonífero de Primordia.html` de `docs/multimedia-temp/documento-piso3/` (preguntar a la usuaria). Detalle en `docs/memoria-claude/primordia-pause-menu-redesign.md`.

**Audio del piso 3 CONECTADO en Unity (2026-11-25, rama `feature/floor3-audio-impl`, sale de `feature/floor3-integration`; las dos en origin):** las 10 tandas del README de audio están hechas y probadas. El fuego sobre el cuerpo de los jefes ya está arreglado y probado. **Pendiente:** sonido de la sala de paso del piso 3 (con el piso 4), suite completa de pruebas, `docs/MULTIMEDIA.md`, `references/pisos.md` de la skill y documento del piso 3; luego fusionar a `main`. Detalle en `primordia-project-status.md` (última sección, 2026-11-25) y `primordia-floor3-audio.md`.
