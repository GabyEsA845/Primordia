---
name: primordia-token-saving
description: Ahorro de tokens con una sola cuenta Pro; avisar al usuario cuándo cambiar entre Opus y Sonnet
metadata:
  node_type: memory
  type: feedback
  originSessionId: 12d27d6d-300c-44eb-aef2-cffa0cfb885e
  modified: 2026-10-28T17:31:27.869Z
---

Desde 2026-10-28 se trabaja solo con una cuenta Claude Pro y hay que ahorrar todos los tokens posibles sin bajar la calidad.

- **Avisar siempre del cambio de modelo** (el usuario lo volvió a pedir el 2026-10-31: recordárselo en cada cambio de tarea, también al cerrar cada subtanda si lo siguiente cambia de tipo): el usuario se olvida, así que hay que recordárselo explícitamente al empezar una tarea (o al terminarla) cuando toque cambiar con `/model`. Opus para diseño (jefes, salas, mecánicas, decisiones abiertas). Sonnet para implementar módulos ya decididos, importar arte, corregir bugs y hacer tareas mecánicas.
- El usuario abre sesiones nuevas a menudo (equivale a `/clear`). La continuidad depende de la memoria, así que hay que dejar el "SIGUIENTE" al día.
- Los cambios de arte y audio por código se agrupan en una sola tanda, no se piden de uno en uno.
- MEMORY.md se mantiene corto: los detalles van en cada archivo y el índice no lleva hashes ni fechas.

**Why:** el límite semanal se agotaba 4 días antes del reinicio.
**How to apply:** al empezar cada tarea, decir qué modelo conviene si no es el actual; leer solo lo necesario (rangos de líneas, grep) y no releer archivos completos.

**Puertas:** el arte ya está hecho (2026-11-02); el recordatorio de «recuerda lo de las puertas» ya no hace falta. Falta integrarlas con Sonnet: [[primordia-door-design-pending]].
