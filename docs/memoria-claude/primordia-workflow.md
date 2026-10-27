---
name: primordia-workflow
description: "Cómo trabajamos en Primordia: idioma, ramas por módulo, menús de editor, pruebas y cuándo hacer commit/merge"
metadata:
  node_type: memory
  type: feedback
  originSessionId: 804bccaa-8bc0-4674-9463-b67798259689
  modified: 2026-10-11T22:35:32.984Z
---

- Todo en **español** (respuestas, comentarios de código, commits, documentos).
- Una rama `feature/<módulo>` por módulo, sale de `main` y vuelve con `git merge --no-ff`; luego `git push` de main y la rama. Etiquetas de hito (`v0.1-prototipo`, `v0.2-jugable`, `v0.5-jugable`, próximo `v1.0`).
- Por cada módulo escribo: código en `Assets/_Primordia/Scripts/<Módulo>`, un menú de editor `Primordia > <Módulo> > Crear…` en `Assets/_Primordia/Editor/<Módulo>Setup.cs` que crea assets/prefabs/escena `Test_<Módulo>` sin sobrescribir lo existente, y pruebas PlayMode en `Assets/_Primordia/Tests/PlayMode`.
- El usuario prueba en Unity (Console, Test Runner, escena de prueba) y me cuenta; **no hago commit ni merge hasta que confirma** que funciona. Cuando confirma, hago commit + merge + push en el mismo paso y marco la casilla en docs/PLAN.md.
- Para decisiones de diseño grandes (p. ej. mutaciones) el usuario quiere que pregunte antes de programar; para cambios pequeños acepta que siga.
- Ideas que el usuario deja para después van a la sección "Mejoras a futuro" de docs/PLAN.md.

**Why:** así trabajó el usuario toda la primera sesión y lo aprobó en cada paso.
**How to apply:** seguir este ciclo en cada módulo nuevo. Ver también [[primordia-unity-gotchas]].
- **Dos equipos (desde 2026-10-26):** el usuario trabaja también desde otro PC clonando el repo. Antes de que cambie de equipo: commit de lo hecho, copiar la memoria a `docs/memoria-claude/` (README con cómo restaurarla) y push de la rama en curso. Al empezar en un equipo, si la memoria de docs/memoria-claude es más nueva que la local, copiarla a la carpeta de memoria.
- **Sprites (recordado por el usuario el 2026-10-26):** siempre, sin excepción, cualquier sprite nuevo del juego (también UI, barras, iconos) se hace con la skill `primordia-sprites` (ver [[primordia-sprites-skill]]).
