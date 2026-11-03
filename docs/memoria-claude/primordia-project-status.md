---
name: primordia-project-status
description: "Estado de Primordia (roguelike Unity): qué está hecho, dónde lo dejamos y qué sigue"
metadata:
  node_type: memory
  type: project
  originSessionId: 12d27d6d-300c-44eb-aef2-cffa0cfb885e
  modified: 2026-11-02T23:00:00.000Z
---

Primordia es un roguelike top-down twin-stick en Unity 6 (6000.6.2f1, 2D URP), repo https://github.com/GabyEsA845/Primordia. Es un proyecto personal (empezó como GameJam 2026) que se publicará en itch.io como build web (menú Primordia > Build > Build WebGL; zip en Builds/Primordia-WebGL.zip).

**Hecho (todo en main):** piso 1 completo (arte, mapa aleatorio, mutaciones con niveles y evoluciones, linajes, biomasa y tienda, 2 jefes, audio). Piso 2 completo (estructura, Bocas, enemigos, sala de cambio, mutaciones y linajes con Carroñero, almejas, Anomalocaris, Euriptérido y audio; último merge 2a6268f). La memoria también está copiada en `docs/memoria-claude`.

**Dónde lo dejamos (2026-11-02, fin de sesión):** diseño y ARTE del piso 3 completos en `feature/floor3-art` (push ya hecho). **Documento interactivo del piso 3 HECHO y aprobado** (ver [[primordia-floor-docs]]; fuente `docs/multimedia-temp/documento-piso3/`, **sin commit aún**; también sin commit los cambios en las plantillas de los documentos de pisos 1 y 2). Los 3 documentos tienen ya el botón «Animaciones» (respeta «reducir movimiento» pero permite forzarlo), comprobado por el usuario.
**Hecho 2026-11-02:** (1) commit 10a6a31 del documento del piso 3 y los botones; (2) **GDD/PLAN del piso 3 ESCRITOS** (secciones «Mutaciones/Clases y linajes del piso 3», «Piso 3», «Corazones de sala…», tabla «Decidido el 28 de octubre (piso 3)» en el GDD; entradas floor3-art/room-hearts/teleports/floor-3 con 8 pasos en PLAN), commit 0ddc14f, push hecho.
**2026-11-02 (tarde):** `feature/room-hearts` (62039a9) y `feature/teleports` (9b2f771, c286014) HECHAS Y PROBADAS por el usuario, sin merge a main aún (preguntar cómo/cuándo).
**2026-11-02 (noche):** room-hearts + teleports + floor12-fixes MERGEADAS en main y subidas (a4d71ed; los 5 cambios de los pisos 1-2 y la pista dorada de linaje hechos y probados por el usuario; GDD al día).
**2026-11-02 (cierre de sesión):** `feature/ui-extras-art` MERGEADA en main y subida (8ea22a8): arte de corazón de sala, corazón verde entero, iconos de mapa (teletransporte, regreso, corazón sin recoger), aura/mota/onda del corazón morado; menú `Primordia > Art > Importar extras de interfaz` (Resources/UiExtrasArt). 506 pruebas PlayMode en verde. Todas las ramas previas al piso 3 están HECHAS.
**PISO 3 EN CURSO (rama `feature/floor-3`, push hecho el 2026-11-03, último commit d9244789):**
- PASOS 1 (estructura), 2 (reacciones), pulido y **3 (cuerpos y Bocas) HECHOS Y PROBADOS**, con toda la pasada de arte integrada, la sala de paso del
  piso 2 al 3 y la metamorfosis por pareja ([[primordia-floor3-structure-decisions]], [[primordia-floor3-reactions-decisions]], [[primordia-floor3-mouths-decisions]],
  [[primordia-floor3-step3-art-pending]], [[primordia-floor2-transition-room]], [[primordia-door-design-pending]]). Incluye la rama `feature/movement-feel`.
- Menús: Primordia > Floor 3 > Importar todo el arte / Crear tilesets / Crear reacciones / Crear piso 3 y escena de prueba / Crear Bocas del piso 3; Primordia > Floor transition > Crear sala de paso del piso 2; Primordia > Art > Importar coronas y marcos de las puertas / Importar recuadro del Órgano (HUD); Primordia > Audio > Importar audio. Atajos de prueba: F12 prende inflamables, Mayús+F12 fuego en el suelo, Av Pág/Re Pág saltan de tramo, F6 cambia de Boca.
**PASO 4 (mutaciones y linajes) EN CURSO (2026-11-03):** subpasos 1-3 (Extremidades, Piel, Órganos) HECHOS, probados y con commit; detalles en [[primordia-floor3-mutations-decisions]]. **SIGUIENTE: tanda de arte pendiente con OPUS ([[primordia-provisional-art]]), luego subpasos 4a/4b (clases con Parásito, 8 linajes, toques) y 5 (reserva, tienda, cambio, F4/F8, documento y GDD/PLAN), con Sonnet.** Después 5) enemigos y trampas, 6) salas (10 especiales, salas con tema, ámbar, almacén de corazones y cucaracha ladrona, sala secreta, sala de paso del piso 3, que lleva a la victoria hasta el piso 4), 7) jefes uno a uno, 8) audio. Antes: ejecutar las pruebas PlayMode (esperado 563/563) y preguntar cuándo hacer merge de floor-3 a main (no hecho). Preguntar antes de cada paso/subtanda ([[primordia-token-saving]]). La usuaria dijo que comentará las opciones C (estiramiento) y D (polvo) del movimiento.

`ProjectSettings/ProjectSettings.asset` aparece modificado por Unity (vació `preloadedAssets`). No es nuestro y se deja fuera de todos los commits.

**How to apply:** al empezar, leer solo las secciones de docs/PLAN.md o del GDD que haga falta, no los archivos enteros. Seguir [[primordia-workflow]], [[primordia-unity-gotchas]] y [[primordia-token-saving]].
