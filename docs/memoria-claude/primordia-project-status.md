---
name: primordia-project-status
description: "Estado de Primordia (roguelike Unity): proyecto personal para itch.io, hitos hechos y qué sigue (sprites)"
metadata:
  node_type: memory
  type: project
  originSessionId: 804bccaa-8bc0-4674-9463-b67798259689
  modified: 2026-10-11T22:35:18.048Z
---

Primordia es un roguelike top-down twin-stick en Unity 6 (6000.6.2f1, 2D URP), repo https://github.com/GabyEsA845/Primordia. Empezó como GameJam 2026, pero el 2026-10-11 pasó a ser **proyecto personal** (la jam no permitía código previo). Se publicará en itch.io como build web (WebGL).

Estado al 2026-10-11 (todo en `main`, etiqueta `v0.5-jugable`): partida completa jugable de MainMenu → 6 salas → jefe (virus gigante, 2 fases) → victoria; 9 mutaciones del GDD con elección 1 de 3 en salas 2 y 4; HUD de corazones, pausa con lista de mutaciones, barra de recarga del órgano bajo el jugador, barra del jefe. Todo el arte es provisional (cuadrados de colores).

Build web: menú Primordia > Build > Build WebGL (itch.io) ya funcionó; zip en Builds/Primordia-WebGL.zip. Falta que el usuario confirme que carga bien en itch.io (subido como Draft): clic derecho, tecla P, pantalla completa.

**Estado al cerrar la sesión del 2026-10-11:** en `main` ya están el arte animado del jugador, enemigos, jefe, proyectiles, efectos y escenario (salas con tiles y formas), el mapa aleatorio con semilla, la cámara con adelanto y la IA que rodea obstáculos. Rama `chore/cleanup` (sin commit, esperando que el usuario confirme la Console limpia): quita avisos obsoletos de FindObjectsSortMode, borra Projectile_Mandibles y pone al día el GDD. **Siguiente (2026-10-12):** piezas de mutación y arte de objetos e interfaz ya en main; faltan la mira y las ranuras del HUD (arte ya entregado en `docs/multimedia-temp/ui/HUD/`) y la biomasa, que irá con la tienda. La build web nueva para itch.io se hará cuando estén todos los sprites (decisión del usuario). Ver [[primordia-sprites-next]].

**Estado al 2026-10-25 (fin de sesión):** piso 1 completo en main. La estructura del piso 2 está **terminada en la rama feature/floor-2-structure** (pasos 1–4: cuevas, franjas, ambiente, salas especiales, fosas, almejas, corazón morado, oleadas), probada por el usuario y sin merge a main (ver [[primordia-floor2-structure]]). Siguiente tema acordado: las 3 mutaciones especiales de las almejas ([[primordia-clam-mutations]]). Lo anterior de esta nota es historia de la primera semana.

**Why:** la hoja de ruta (docs/PLAN.md) marca la Fase 2 hacia `v1.0`: build, mapa aleatorio + salas con formas, arte, audio, linajes, equilibrio.
**How to apply:** leer docs/PLAN.md, docs/GDD Primordia.md y docs/MULTIMEDIA.md al empezar; seguir el flujo de [[primordia-workflow]] y los avisos técnicos de [[primordia-unity-gotchas]].

**Actualización 2026-10-25:** en main también feature/clam-room (merge bace4a2: sala de la almeja y 3 mutaciones especiales; minimapa oculto en pausa y arrastrable) y feature/floor2-mouths (merge 9420dbf: Cnidocitos, Placas mandibulares, Corona de brazos). En curso feature/floor2-enemies (paso 1 hecho, cb26155): ver [[primordia-floor2-enemies]]. Pendientes del PLAN para el piso 2 después: exchange-room, floor2-mutations, boss-anomalocaris, boss-eurypterid, floor2-audio.

**Actualización 2026-10-26:** en main feature/floor2-mutations (merge d5680b5): las 6 mutaciones de Extremidades, Piel y Órganos del piso 2, los 8 linajes con el Carroñero y sus toques, reserva del piso 2 solo con sus mutaciones (más mejoras de lo que se trae) y atajo F8. También en main la skill primordia-sprites y docs/multimedia-temp en git. Siguiente: [[primordia-boss-anomalocaris]].

**Actualización 2026-10-26 (2):** en main feature/boss-anomalocaris (merge 1086049): el Anomalocaris con su sala, sus fases, su barra de vida propia, la barra fantasma para todos los jefes y el Tab de tres estados. Siguiente entonces: `feature/boss-eurypterid`.

**Actualización 2026-10-27:** en main feature/boss-eurypterid (merge 38cd9f2): el Euriptérido con su cueva que se derrumba (piedras, fosas que se abren), el temblor de pantalla para todo el juego (opción en la pausa), la muda, la fase blanda, el último aliento con bullet hell y su barra propia; ver [[primordia-boss-eurypterid]]. Siguiente: `feature/floor2-audio`.
