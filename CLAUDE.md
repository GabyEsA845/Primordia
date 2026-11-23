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

Mira `primordia-project-status.md` (la última sección, 2026-11-23, «ESTADO PARA RETOMAR EN OTRO ORDENADOR (tanda de arte)»). **Antes de nada: en el PC anterior quedaron cambios sin subir; hacer allí commit y push y fusionarlos.** Arte del 2026-11-23 (Opus), todo aprobado y sin importar: sala de paso del piso 3 → 4 (rama `feature/floor3-exit-art`) y arte final de la sala de la Arthropleura (rama `feature/arthropleura-canopy-art`, que sale de la anterior); importarlo y programarlo con Sonnet siguiendo cada README. **Siguiente arte (Opus):** marca roja de aviso del cono del Lepidodendron → iconos de las especiales del ámbar → área de giro de la Peonza (ver `primordia-art-session-2026-11-23.md`; preguntar antes de cada pieza). Regla nueva: ninguna transición entre temas puede ser una línea de corte (`primordia-theme-transitions.md`). La Megarachne sigue en M5 (tarea 19) en `feature/boss-megarachne`; Codex ACTIVO. En un ordenador nuevo: Git LFS, `git config user.name/email`, `git fetch`, `git checkout feature/arthropleura-canopy-art`, Python 3.12 (`winget install Python.Python.3.12 --scope user`, `pip install pillow numpy`) y leer `primordia-unity-batchmode.md`. Archivos sin subir a propósito: `Assets/TextMesh Pro*` y el HTML de `docs/multimedia-temp/documento-piso3/` (preguntar a la usuaria).
