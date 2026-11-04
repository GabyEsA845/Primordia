---
name: primordia-floor3-room-themes
description: "Paso 6, bloque 1 del piso 3: las 5 salas con tema (decisiones, archivos, menú, números) y qué piezas nuevas se piden a Opus"
metadata:
  type: project
---

**Paso 6, bloque 1 (2026-11-04, Sonnet): salas con tema, programadas, pendientes de probar en juego y de commit.**

Decisiones de la usuaria (2026-11-04): 2 a 4 por partida sin repetir tema; casi solo el protagonista + 1 apoyo; escenario con piezas existentes por ahora y piezas nuevas anotadas para Opus; cuevas con la misma generación del piso (cada tema con su `CaveSpec`).

**Cómo funciona:** `FloorConfig.EncounterTheme` (nombre, `ThemedEnemy[]` con peso y tope, multiplicador de enemigos, `minDepth`, cueva propia, escalas de helechos/odres/vainas/fumarolas/charcos). `FloorLayoutGenerator.PickEncounters` los reparte en salas de combate del camino sin especial, profundidad ≥ 3 y nunca la última antes del jefe (sin temas no gasta azar). `LayoutRoom.Encounter` guarda el tema; `EncounterPlanner.PickEnemies(..., theme)` sortea solo sus enemigos por peso (coste y reglas de lodo/planta/helecho siguen siendo los de `Enemies`); `FloorBuilder` aplica `Scaled(...)` al escenario. Menú **Primordia > Floor 3 > Crear salas con tema** (repone los números en Floor_3).

**Los 5 temas (ya en el menú):** Pradera de Calamites (Calamites ×4 hasta 3 + Medullosa; claro abierto 44×26), Nido de bombarderos (escarabajos hasta 5 + araña; odres ×2, helechos ×1,3, fumarolas ×1,5), Charca de sapos (sapos hasta 5 + Medullosa; 44×26 con lagos, charcos ×2), Telar de la araña (arañas hasta 5 + 1 Calamites; helechos ×2, odres ×1,5), Cielo de libélulas (libélulas hasta 4 + Medullosa; 50×18 despejada). Todos ×1,25 de enemigos.

**Pruebas:** `CaveGeneratorTests` (EncounterThemes_*). **Sin hacer:** letrero con el nombre al entrar (pedido abajo) y nombre en el mapa.

**Pendiente de diseño/arte (Opus, tanda de diseño pendiente):** piezas propias de cada tema que hoy usan las genéricas: nido de bombarderos (montículo/nido), telarañas colgantes o troncos con hilo para el Telar, matas de Calamites para la pradera, nenúfares o charca más viva para los sapos, y algo de cielo/luz para la sala de las libélulas. Decidir también si cada tema merece letrero o música propia (paso 8).

**Pedido de la usuaria (2026-11-04) para la tanda de Opus: nombre visible de la sala con tema.** Al entrar, aparece el nombre arriba y en el centro de la pantalla, sobre un adorno (p. ej. una silueta de tronco), con entrada y salida animadas. Hay que dibujar el adorno (Opus, con la skill primordia-sprites, preguntando antes) y luego programar el letrero en Sonnet (usar `EncounterTheme.name`; mirar el letrero con fuente pixel de los corazones de sala/teletransportes como base).
