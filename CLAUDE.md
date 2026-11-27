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

**Menú de pausa nuevo y bestiario: HECHO, probado por la usuaria, con commit y en origin (2026-11-26, rama `feature/pause-menu`).** Esta es la rama desde la que seguir (contiene el audio del piso 3, la sala de la Arthropleura y el registro de los dos jefes finales en `Floor_3`). En un ordenador nuevo: Git LFS, `git config user.name/email`, `git fetch`, `git checkout feature/pause-menu`, Python 3.12 (`winget install Python.Python.3.12 --scope user`, `pip install pillow numpy`) y leer `docs/memoria-claude/primordia-unity-batchmode.md`. Sin subir a propósito: `Assets/TextMesh Pro*` (se importa desde Unity) y `Pantano carbonífero de Primordia.html` de `docs/multimedia-temp/documento-piso3/` (preguntar a la usuaria). Detalle en `docs/memoria-claude/primordia-pause-menu-redesign.md`.

**Siguiente (por decidir con la usuaria):** cierre del piso 3 (lo de abajo) y, opcional, arreglar las piezas inflamables de la Arthropleura que quedan en `Flammables` (rompe pruebas posteriores en la suite completa).

**Audio del piso 3 CONECTADO en Unity (2026-11-25, rama `feature/floor3-audio-impl`, sale de `feature/floor3-integration`; las dos en origin):** las 10 tandas del README de audio están hechas y probadas. El fuego sobre el cuerpo de los jefes ya está arreglado y probado. **Pendiente:** sonido de la sala de paso del piso 3 (con el piso 4), suite completa de pruebas, `docs/MULTIMEDIA.md`, `references/pisos.md` de la skill y documento del piso 3; luego fusionar a `main`. Detalle en `primordia-project-status.md` (última sección, 2026-11-25) y `primordia-floor3-audio.md`.
