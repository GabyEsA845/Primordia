---
name: primordia-project-status
description: "Estado de Primordia (roguelike Unity): qué está hecho, dónde lo dejamos y qué sigue"
metadata:
  node_type: memory
  type: project
  originSessionId: 12d27d6d-300c-44eb-aef2-cffa0cfb885e
  modified: 2026-11-02T14:21:50.560Z
---

Primordia es un roguelike top-down twin-stick en Unity 6 (6000.6.2f1, 2D URP), repo https://github.com/GabyEsA845/Primordia. Es un proyecto personal (empezó como GameJam 2026) que se publicará en itch.io como build web (menú Primordia > Build > Build WebGL; zip en Builds/Primordia-WebGL.zip).

**Hecho (todo en main):** piso 1 completo (arte, mapa aleatorio, mutaciones con niveles y evoluciones, linajes, biomasa y tienda, 2 jefes, audio). Piso 2 completo (estructura, Bocas, enemigos, sala de cambio, mutaciones y linajes con Carroñero, almejas, Anomalocaris, Euriptérido y audio; último merge 2a6268f). La memoria también está copiada en `docs/memoria-claude`.

**Dónde lo dejamos (2026-11-02, fin de sesión):** diseño y ARTE del piso 3 completos en `feature/floor3-art` (push ya hecho). **Documento interactivo del piso 3 HECHO y aprobado** (ver [[primordia-floor-docs]]; fuente `docs/multimedia-temp/documento-piso3/`, **sin commit aún**; también sin commit los cambios en las plantillas de los documentos de pisos 1 y 2). Los 3 documentos tienen ya el botón «Animaciones» (respeta «reducir movimiento» pero permite forzarlo), comprobado por el usuario.
**Hecho 2026-11-02:** (1) commit 10a6a31 del documento del piso 3 y los botones; (2) **GDD/PLAN del piso 3 ESCRITOS** (secciones «Mutaciones/Clases y linajes del piso 3», «Piso 3», «Corazones de sala…», tabla «Decidido el 28 de octubre (piso 3)» en el GDD; entradas floor3-art/room-hearts/teleports/floor-3 con 8 pasos en PLAN), commit 0ddc14f, push hecho.
**2026-11-02 (tarde):** `feature/room-hearts` (62039a9) y `feature/teleports` (9b2f771, c286014) HECHAS Y PROBADAS por el usuario, sin merge a main aún (preguntar cómo/cuándo). Arte provisional anotado en [[primordia-art-pending-batch]].
**2026-11-02 (noche):** room-hearts + teleports + floor12-fixes MERGEADAS en main y subidas (a4d71ed; los 5 cambios de los pisos 1-2 y la pista dorada de linaje hechos y probados por el usuario; GDD al día).
**2026-11-02 (cierre de sesión):** `feature/ui-extras-art` MERGEADA en main y subida (8ea22a8): arte de corazón de sala, corazón verde entero, iconos de mapa (teletransporte, regreso, corazón sin recoger), aura/mota/onda del corazón morado; menú `Primordia > Art > Importar extras de interfaz` (Resources/UiExtrasArt). 506 pruebas PlayMode en verde. Todas las ramas previas al piso 3 están HECHAS.
**PISO 3 EN CURSO (2026-11-02, rama `feature/floor-3`, ~30 commits LOCALES, SIN PUSH; salió de main y trae fusionada `feature/floor3-art`):**
PASO 1 ESTRUCTURA COMPLETO y probado por la usuaria (paredes con tapa/caras en pares, 3 tilesets, FloorConfig `Floor_3`, escena `Test_Floor3`,
cuevas en L/alargadas/grandes con lagos de lodo e islas, salas de frontera mixtas, lodo profundo, maleza quemable y atajos). PASO 2 REACCIONES
COMPLETO y probado (subtandas A fuego/helechos, B metano, C veneno, D fumarolas y escenario vivo; detalles en [[primordia-floor3-structure-decisions]]
y [[primordia-floor3-reactions-decisions]]). Menús: Primordia > Floor 3 > Importar todo el arte / Crear tilesets / Crear reacciones (fuego) /
Crear piso 3 y escena de prueba (este último lo hace todo). Atajos de prueba: F12 prende inflamables cercanos, Mayús+F12 fuego en el suelo,
Av Pág / Re Pág saltan de tramo. Arte pendiente y código ligado: [[primordia-floor3-art-batch-pending]].
**SIGUIENTE (nueva sesión, con Sonnet):** 1) preguntar si se hace push de `feature/floor-3` y/o la tanda de arte pendiente (Opus) ahora o más tarde;
2) pequeño cambio de código: el agua de charca apaga el quemado como el lodo; 3) PASO 3 del PLAN «Cuerpos y Bocas» (rana, Diplocaulus, escorpión
látigo; Escupitajo venenoso, Peonza, Chorro de vinagre con barra de carga; el charco de veneno del Escupitajo y la nube/charcos ya existen en
`Rooms/PoisonPuddle` y `Toxins`), luego 4) mutaciones y linajes (clase Parásito), 5) enemigos y trampas (los enemigos inflamables dejan rastro de
fuego; el odre/bulbo no están en la navegación), 6) salas (10 especiales, ámbar, almacén de corazones, sala secreta, sala de paso; el Nido y
Lepidodendron), 7) jefes uno a uno, 8) audio. Preguntar al usuario antes de cada paso/subtanda y avisar del cambio de modelo ([[primordia-token-saving]]).
Teletransporte del piso 3 (`Teleporter_Floor3`) ya se asigna en `Floor3Setup`.

`ProjectSettings/ProjectSettings.asset` aparece modificado por Unity (vació `preloadedAssets`). No es nuestro y se deja fuera de todos los commits.

**How to apply:** al empezar, leer solo las secciones de docs/PLAN.md o del GDD que haga falta, no los archivos enteros. Seguir [[primordia-workflow]], [[primordia-unity-gotchas]] y [[primordia-token-saving]].
