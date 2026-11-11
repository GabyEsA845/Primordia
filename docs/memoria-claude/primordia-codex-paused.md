---
name: primordia-codex-paused
description: "Flujo Claude dirige + Codex implementa, RETOMADO el 2026-11-11 (antes archivado); reglas y archivos"
metadata:
  node_type: memory
  type: project
  originSessionId: 821d7cde-e5b0-4e49-b819-228de2065ef8
  modified: 2026-11-11T14:44:33.580Z
---

El 2026-10-31 se archivó trabajar con Codex; el 2026-11-11 la usuaria lo **retomó** (tiene ChatGPT Plus gratis 1 mes y Codex instalado en el proyecto).

**Flujo:** Claude dirige (diseño, preguntas, revisión de diffs, memoria) y escribe cada tarea en `docs/codex/tarea-XX-*.md` (plantilla `docs/codex/PLANTILLA.md`); Codex implementa en una rama propia `codex/<nombre>` desde `feature/floor-3`, sin commit/push/merge; la usuaria prueba en Unity y confirma antes del commit. Reglas para Codex en `AGENTS.md` (raíz). Piloto: `tarea-01-barra-meganeura.md` (7e-1, barra «alas»).

**Why:** ahorrar el límite de uso de Claude (revisar sale más barato que implementar).
**How to apply:** solo una instancia de Unity a la vez (batchmode choca con el editor abierto); no edito Assets mientras Codex trabaja en el mismo árbol; reviso su diff y sus pruebas antes de dar la tarea por buena; la memoria (`docs/memoria-claude/`) la actualizo yo, Codex no. Sigue valiendo [[primordia-token-saving]].
