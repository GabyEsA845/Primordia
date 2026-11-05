# Primordia — instrucciones para Claude

Antes de hacer nada, lee estos archivos, en este orden:

1. `docs/memoria-claude/MEMORY.md` — índice de la memoria del proyecto (reglas de trabajo, estado, diseño de cada piso, decisiones). Abre después los archivos que enlaza y que vengan al caso, empezando por `primordia-project-status.md`.
2. `.claude/skills/` — skills del proyecto (`primordia-sprites`, `primordia-audio`). Úsalas siempre que haya arte o audio nuevo.

## Reglas básicas (resumen; el detalle está en la memoria)

- Responde siempre en español.
- Una rama por módulo. Haz commit solo después de que el usuario confirme que lo probó.
- Los cambios de editor de Unity se hacen con menús `Primordia` (no a mano). Cuidado con Unity abierto, statics, CRLF y referencias perdidas (ver `primordia-unity-gotchas.md`).
- Antes de crear cualquier sprite o animación, pregunta al usuario.
- Cuando termines algo, actualiza el documento del piso correspondiente y la copia de la memoria en `docs/memoria-claude/`.
- El usuario tiene un límite de uso estricto: sé económico con los tokens y avisa si una tarea de diseño/arte (modelo más potente) o de implementación (modelo más barato) conviene hacerla con otro modelo.

## Memoria

La carpeta `docs/memoria-claude/` es la fuente de verdad compartida entre cuentas. Si aprendes algo nuevo y no obvio, guárdalo ahí (un hecho por archivo, con su línea en `MEMORY.md`) y no solo en la memoria local de tu cuenta.

## Siguiente paso

Mira `primordia-project-status.md`: piso 3 en `feature/floor-3`, pasos 1-5 hechos; paso 6 (salas) en curso (bloque 1 programado, bloque 2 con el 2a hecho). Todo el arte de las sesiones del 2026-11-04 está dibujado y subido pero SIN importar: letrero y decorado de las salas con tema, sala del ámbar, cucaracha ladrona, Veta de carbón y Bosque en humo (estas dos, pendientes de aprobación) y el aspecto nuevo de las especiales (bloque y nivel III). SIGUIENTE (Sonnet): importar y programar ese arte, franja de agua y rastro de fuego, y 2b Cercos, 2c Cápsula, 2d Gota (`primordia-floor3-amber.md`, `primordia-floor3-specials-look.md`), luego bloques 3-6. Para Opus quedan: subtanda 3 de las especiales (contorno dorado y motas), sala de paso del piso 3 y el audio del piso 3.
