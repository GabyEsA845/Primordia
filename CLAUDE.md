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

Mira `primordia-project-status.md` (la última sección, 2026-11-18). Piso 3 en `feature/floor-3`: pasos 1-6 hechos (la sala de paso queda para Opus); Meganeura y Lepidodendron cerrados (falta sonido, puerta secreta y documento). **Arthropleura A-F completa y probada** (rama `feature/boss-arthropleura`, commit c65e24f1, aún sin fusionar en floor-3; falta sonido, enganche a Floor_3, `CanopyShake`). **Megarachne en curso** (rama `feature/boss-megarachne`, plan `docs/codex/megarachne-plan.md`): M1 base y M2 ataques hechos; M2 está en `codex/mega-2-ataques` (19/19 pruebas), probada por la usuaria, con ajustes visuales del cable. **Arte de los cables v2 HECHO por Opus el 2026-11-18** (haz de hebras + abanicos de arranque `Nido_CableRoot`; ver `primordia-megarachne-cable-art-pending.md` y la sección «Cables v2» del README de `docs/multimedia-temp/floor3-bosses/megarachne/`). **SIGUIENTE SESIÓN:** escribir la tarea para Codex de importar y programar los cables v2 (o hacerla con Sonnet) → que la usuaria la pruebe → fusionar `codex/mega-2-ataques` en `feature/boss-megarachne` → tarea de M3 (saco de crías y crías) → M4 fuego en cables, M5 enfado, M6 entrada/barra/muerte/recompensa → paso 8 (audio, enganches). Codex (Claude dirige y escribe `docs/codex/tarea-NN-*.md`; Codex implementa en `codex/*`; si se queda sin tokens, termina Claude) está ACTIVO. Recompensa de Megarachne decidida: corazón + biomasa + tarjetas para subir un nivel una mutación equipada que no sea nivel 3. Para Opus quedan: sala de paso del piso 3, subtanda 3 de las especiales del ámbar, aprobar Veta de carbón y Bosque en humo, audio del piso 3. Pruebas del piso 3: la suite completa no se ha lanzado tras la Arthropleura/Megarachne (antes 720 de 729; 9 conocidos, ver `primordia-unity-batchmode.md`). En un ordenador nuevo: Git LFS instalado, `git config user.name/email`, `git fetch` y `git checkout codex/mega-2-ataques`, y leer `primordia-unity-batchmode.md` (la ruta de Unity y del proyecto cambian; Python 3 puede no estar instalado: `winget install Python.Python.3.12 --scope user` y `pip install pillow numpy`). Archivos sin subir a propósito: `Assets/TextMesh Pro*` y el HTML de `docs/multimedia-temp/documento-piso3/` (preguntar a la usuaria).
