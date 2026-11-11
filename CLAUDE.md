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

Mira `primordia-project-status.md` (la última sección, 2026-11-11). Piso 3 en `feature/floor-3` (con push): pasos 1-6 hechos (la **sala de paso del piso 3** queda para **Opus**). **Paso 7 (jefes): la MEGANEURA está casi cerrada** (7a-7e-3 hechos y probados; falta sonido, huevos/ninfas/aleteo en la F3 sin decidir, topes del `BossDamageLimiter` provisionales y el documento del piso 3). **Se trabaja con Codex** (Claude dirige, Codex implementa): lee `primordia-codex-paused.md`, `AGENTS.md` y `docs/codex/`. **Tarea 04 ya escrita y SIN dar a Codex** (`docs/codex/tarea-04-telarana-ligera.md`: telaraña de las Patas hilanderas III más ligera; rama `codex/telarana-ligera` desde feature/floor-3). Después: otros jefes (Arthropleura, Megarachne, Lepidodendron ancestral; cada uno necesita diseño antes de pasarlo a Codex) y paso 8 (audio y cierre). Para Opus quedan: sala de paso del piso 3, subtanda 3 de las especiales del ámbar, aprobar Veta de carbón y Bosque en humo, audio del piso 3 y el **arte del veneno con Escamas carbonizadas III** (`primordia-floor3-poison-fire-art.md`). Antes de tocar la Meganeura, leer `primordia-boss-meganeura.md`. Pruebas: `MeganeuraBossTests` (39), `BossHealthBarTests` (13); la suite completa no se ha lanzado tras 7e (antes 720 de 729; los 9 que fallan son conocidos, ver `primordia-unity-batchmode.md`). En un ordenador nuevo: Git LFS instalado, `git config user.name/email`, `git checkout feature/floor-3` y `git pull`, y leer `primordia-unity-batchmode.md` (ojo: la ruta de Unity y del proyecto cambian de un equipo a otro; menús y pruebas se pueden lanzar con Unity cerrado).
