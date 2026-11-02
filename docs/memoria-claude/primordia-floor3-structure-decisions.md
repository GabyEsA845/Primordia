---
name: primordia-floor3-structure-decisions
description: "Decisiones de diseño y estado de la estructura del piso 3 (rama feature/floor-3): subtandas, tramos, generador, pruebas"
metadata:
  node_type: memory
  type: project
  originSessionId: c2e7fcb9-3aa7-479c-9175-1715e21fb25a
  modified: 2026-11-02T17:20:07.079Z
---

Rama `feature/floor-3` (sale de main, con `feature/floor3-art` fusionada). Menús: Primordia > Floor 3 > Importar todo el
arte / Crear tilesets / Crear piso 3 y escena de prueba (`Test_Floor3`). Arte en Assets/_Primordia/Art/Floor3.

Estructura en 4 subtandas (2026-11-02): 1) paredes + tilesets + FloorConfig (HECHA, probada: tapa con variantes, caras en
pares, bordes junto a la cara); 2) tramos y generador; 3) lodo profundo; 4) maleza quemable y puertas de raíces.

ESTRUCTURA COMPLETA (4 subtandas hechas y probadas, 2026-11-02; subtanda 4 = maleza quemable + atajos, IFlammable, F12). Siguiente: paso 2 Reacciones del PLAN.

Subtanda 3 (lodo profundo) HECHA y probada (2026-11-02): barra de hundimiento 1,6 s, enemigos que andan lo rodean y mueren si caen, tiles de lodo; falta el pulido visual del cuerpo (el lodo en el cuerpo). Siguiente: subtanda 4 (maleza quemable y puertas de raíces).

Decisiones subtanda 2: cuevas con forma impuesta (rectángulo largo, L, 2×2) reutilizando CaveGenerator; islas con celda
nueva `Mud` dibujada provisional hasta la subtanda 3; salas algo mayores que el piso 2 (normales ~34×22–44×26, alargadas
50×18 / 22×30, 2×2 ~52×34); frontera mixta = una sala por límite de tramo (2 por partida) con mancha de ruido en suelo/deco,
paredes del tema de su sala. Pruebas sin enemigos; atajo Av Pág / Re Pág salta de tramo (F9/F10 ya ocupados).

**Why:** el piso 3 es lo más largo del proyecto; se prueba cada subtanda antes de la siguiente y se pregunta antes de cada paso.

**How to apply:** seguir este orden; al acabar las subtandas, arreglar el pulido de puertas.
Ver [[primordia-floor3-design]].

**Lodo y esquiva (2026-11-02):** esquivar/embestir en lodo profundo deja la barra de hundimiento EN PAUSA (no la vacía), para no cruzar el lodo a base de esquivas. La mutación que cambia la esquiva por un SALTO (paso 4, mutaciones) debe poner `PlayerMudSink.DodgeResetsSink = true` (saltar sí vacía la barra).
