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

Mira `primordia-project-status.md` (la última sección): piso 3 en `feature/floor-3`, pasos 1-6 hechos (el bloque 6 del paso 6, la **sala de paso del piso 3**, queda para **Opus**: diseño y arte nuevos). **PASO 7 (JEFES, uno cada vez) EN CURSO con la Meganeura:** 7a (sala 40×24, hojas con direcciones, presentación), 7b (fase 1: picado, aleteo con viento, metano) y 7c (fase 2: huevos, ninfas, furia) HECHOS, probados y con commit. **SIGUIENTE (Sonnet): 7d = fase 3 (≤30 %), la tormenta** (oscuridad, lluvia que apaga fuegos, rayo grande que sigue al jugador y rayos sueltos, charcas electrificadas que matan ninfas, Sigillarias quemadas como pararrayos, el jugador lleva el rayo hasta ella → cae aturdida) y luego 7e (barra de alas abajo de la pantalla, muerte con caída en espiral, recompensa, documento del piso 3, pruebas). Después, los otros jefes: Arthropleura, Megarachne (minijefe del Nido), Lepidodendron ancestral (jefe secreto; sustituye a la cámara provisional del bloque 5); y paso 8 (audio y cierre). Antes de tocar la Meganeura, leer `primordia-boss-meganeura.md` (diseño, hojas, reglas, y la sección «CÓDIGO» con los pasos 7a-7c) y el README de `docs/multimedia-temp/floor3-bosses/meganeura/`. Para Opus quedan: sala de paso del piso 3, subtanda 3 de las especiales del ámbar (contorno dorado y motas), aprobar Veta de carbón y Bosque en humo, audio del piso 3. Pruebas: `MeganeuraBossTests` (16). Suite completa en batch: 720 de 729 (los 9 que fallan son conocidos; ver `primordia-unity-batchmode.md`). Truco: una prueba PlayMode lanzada SIN `-nographics` puede renderizar la cámara a PNG para ver bugs visuales. En un ordenador nuevo: Git LFS instalado, `git config user.name/email`, y leer `primordia-unity-batchmode.md` (menús y pruebas se pueden lanzar con Unity cerrado; menú `Primordia > Boss > Crear Meganeura y escena de prueba`, escena `Test_BossMeganeura`).
