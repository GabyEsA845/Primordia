---
name: primordia-project-status
description: "Estado de Primordia (roguelike Unity): qué está hecho, dónde lo dejamos y qué sigue"
metadata:
  node_type: memory
  type: project
  originSessionId: 12d27d6d-300c-44eb-aef2-cffa0cfb885e
  modified: 2026-10-28T17:31:54.068Z
---

Primordia es un roguelike top-down twin-stick en Unity 6 (6000.6.2f1, 2D URP), repo https://github.com/GabyEsA845/Primordia. Es un proyecto personal (empezó como GameJam 2026) que se publicará en itch.io como build web (menú Primordia > Build > Build WebGL; zip en Builds/Primordia-WebGL.zip).

**Hecho (todo en main):** piso 1 completo (arte, mapa aleatorio, mutaciones con niveles y evoluciones, linajes, biomasa y tienda, 2 jefes, audio). Piso 2 completo (estructura, Bocas, enemigos, sala de cambio, mutaciones y linajes con Carroñero, almejas, Anomalocaris, Euriptérido y audio; último merge 2a6268f). La memoria también está copiada en `docs/memoria-claude`.

**Dónde lo dejamos (2026-11-02):** el diseño del piso 3 (Pantano carbonífero) está CERRADO en [[primordia-floor3-design]] (aún no en GDD/PLAN) y **TODO SU ARTE ESTÁ TERMINADO** en la rama `feature/floor3-art` (último commit bf0e8bf, el jefe secreto Lepidodendron; **push pendiente**, preguntar). **SIGUIENTE:** documento interactivo del piso 3 ([[primordia-floor-docs]], aún no existe) → GDD/PLAN → ramas `feature/room-hearts` y `feature/teleports` → `feature/floor-3` (importar e implementar; con Sonnet). Avisar al usuario del cambio de modelo ([[primordia-token-saving]]).

`ProjectSettings/ProjectSettings.asset` aparece modificado por Unity (vació `preloadedAssets`). No es nuestro y se deja fuera de todos los commits.

**How to apply:** al empezar, leer solo las secciones de docs/PLAN.md o del GDD que haga falta, no los archivos enteros. Seguir [[primordia-workflow]], [[primordia-unity-gotchas]] y [[primordia-token-saving]].
