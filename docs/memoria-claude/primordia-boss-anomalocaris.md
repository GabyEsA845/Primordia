---
name: primordia-boss-anomalocaris
description: "SIGUIENTE (desde 2026-10-26): rama feature/boss-anomalocaris, primer jefe real del piso 2; qué hay hecho, dónde está el diseño y qué falta"
metadata:
  node_type: memory
  type: project
  originSessionId: bc20fcc8-a95a-48cd-a40d-3f24382d122a
  modified: 2026-10-26T21:49:13.417Z
---

Próxima tarea acordada con el usuario el 2026-10-26, al cerrar feature/floor2-mutations (merge d5680b5 en main). Se empieza en una sesión nueva: crear `feature/boss-anomalocaris` desde `main`. Después vienen `feature/boss-eurypterid` y `feature/floor2-audio` (PLAN.md).

**Diseño (cerrado):** GDD "Anomalocaris" (~línea 358 de docs/GDD Primordia.md), línea de PLAN.md de la rama y [[primordia-floor2-design]]. Resumen: 300 de vida; cueva ovalada ~36×26 sin fosas, en el Abismo; al entrar baja la luz (penumbra, no ceguera) y las plantas luminosas (luces 2D de URP) dan casi todo el brillo; 4-6 columnas de algas tapan la vista sin bloquear. F1 acecho: zarpazo con aviso 0,7 s y 1 s vulnerable después, estela de burbujas. F2 cerco en espiral. Al 25 %, escena de los ojos (solo los ojos nadando en la oscuridad, inmune). F3 frenesí: se para, carga, marcador 0,8→0,5 s del área y la trayectoria de sus dos zarpazos (casi inesquivables) y pausa corta después.

**Ya hecho:**
- Arte del jefe APROBADO: docs/multimedia-temp/floor2-bosses/Anomalocaris/ (celda 112×112) y Effects/ (Fx_Anomalocaris_Bubbles, _Swipe 64×64 con origen (0,32), _ClawArea 64×64, _ClawPath 16×8 en mosaico). Tabla de hojas, cuadros, ms y cuadro de daño (ClawStrike cuadro 1) en floor2-bosses/README.md.
- Presentación del jefe (rótulo, retrato, pose) en main desde feature/boss-intro ([[primordia-floor2-rooms]]).
- Plantilla de sala `Data/Rooms/Floor2/Room_Boss_Anomalocaris` (de floor2-rooms/generar_salas_fijas.py, función anomalocaris()) con marcas A (columna de algas) y L (planta luminosa) que **aún no tienen sprite ni efecto**; ahora mismo en la sala hay un jefe provisional ([[primordia-floor2-structure]]).

**Falta (a proponer al usuario por pasos, arte primero):** sprites de las columnas de algas y de las plantas luminosas (con la skill primordia-sprites, ver [[primordia-sprites-skill]]); que el pintor de salas las ponga (A sin colisión que tapa, L con Light2D); bajada de luz al entrar en la sala; IA del jefe por fases (mirar cómo se hizo la ameba gigante, [[primordia-boss-amoeba]], y BossAI/EnemyBrain de [[primordia-floor2-enemies]]); escena de los ojos; marcadores del frenesí; menú de importación del arte; pruebas. Preguntar antes las decisiones de diseño que no estén cerradas.

**How to apply:** al empezar la sesión, leer esto y [[primordia-workflow]]; proponer el plan de pasos antes de programar.
