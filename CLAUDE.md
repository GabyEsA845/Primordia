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

Mira `primordia-project-status.md` (la última sección): piso 3 en `feature/floor-3`, pasos 1-5 hechos; paso 6 (salas) en curso. HECHOS y con commit o a punto: bloque 1 (5 salas con tema: letrero, decorado y gran charca de agua, escena `Test_Floor3Themes`), bloque 2 (sala del ámbar y las 3 especiales Cercos/Cápsula/Gota con escena `Test_Floor3Specials`; **los sprites de esas 3 los rehace la usuaria**), franja de agua en las patas, rastro de fuego y bloque 3 (almacén de corazones y cucaracha ladrona). SIGUIENTE (Sonnet): bloque 4 = las 10 salas especiales (la Veta de carbón y el Bosque en humo, dibujadas por Opus, están pendientes de que la usuaria las apruebe antes de programarlas), bloque 5 = sala secreta, bloque 6 = sala de paso del piso 3 (diseño pendiente, Opus). Después, paso 7 (jefes, uno cada vez) y 8 (audio y cierre). Para Opus quedan: subtanda 3 de las especiales (contorno dorado y motas), sala de paso del piso 3 y el audio del piso 3. NOTA 2026-11-06: la Cápsula de ámbar se está rediseñando; leer `primordia-floor3-capsule-redesign.md` y `docs/multimedia-temp/floor3-capsule/README.md` antes de tocar las especiales del ámbar (arte de la Cápsula CERRADO, reposo incluido; siguiente: rediseño de la Gota de ámbar con Opus, luego importar/programar la Cápsula con Sonnet, y los Cercos). En un ordenador nuevo: Git LFS instalado, `git config user.name/email`, y leer `primordia-unity-batchmode.md` (menús y pruebas se pueden lanzar con Unity cerrado).
