# Tarea 14 — Megarachne: arreglos de la prueba de M1

- **Rama:** `codex/mega-1-base` (continuar; la usuaria aun no ha confirmado M1)
- **Escrita por:** Claude (2026-11-15) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Corregir lo que la usuaria vio al probar `Test_BossMegarachne`.

## Qué hacer
1. **No persigue, se queda en el centro del árbol.** Diagnostica con la escena real (prueba PlayMode que mida el desplazamiento tras 3 s con el jugador a 8 u). Sospechas: `MoveGround` (`MegarachneBoss.cs`) frena todo si el `body.Cast` encuentra algo en la dirección (el collider del tronco, el suyo propio o el de otro objeto) con `hits[i].distance` ~0; `SetArena` la coloca en `ArenaCenter + down*CableHeight`, pegada al `BoxCollider2D` del tronco; o `target` es null. Arregla la causa: ignorar su propio collider y el trigger, y si choca con un obstáculo, deslizar/rodearlo (probar a mover solo en X o solo en Y) en vez de quedarse parada. Debe perseguir al jugador por toda la sala y rodear el tronco.
2. **Puntas de los cables demasiado altas en los árboles.** Los extremos `ends[i] = outer[i] + up*CableHeight` quedan por encima del tronco/tocón (el sprite `Sigillaria_i` está en `outer[i] + up*1.5`). El cable debe atarse a la parte alta del tronco del tocón, no flotar sobre él: calcula la altura real del sprite (`Obstacle_Sigillaria`) y ata el cable a ~75-85 % de su altura visible. Ajusta también la altura a la que corre ella por el cable (`SetHeight`) para que coincida con los extremos. Comprueba a ojo con captura en la escena.
3. **Árboles sin hitbox.** El jugador atraviesa el tronco del árbol central y los 5 Sigillaria (estos no tienen collider). Comprueba capas/matriz de colisión (usa la misma capa que las paredes/obstáculos de la sala: ver cómo la Arthropleura o el Lepidodendron crean sus árboles sólidos, p. ej. `ArthropleuraBoss.Trees.cs`) y añade: tronco central sólido (caja ancha ~2×1,2 u en la base) y cada Sigillaria sólido en su base (~1×0,8 u). La Megarachne también debe chocar con ellos.
4. Pruebas nuevas: persigue y avanza ≥ 6 u en 3 s; el jugador no atraviesa el tronco ni una Sigillaria (collider en capa que bloquea al jugador); extremos de cable dentro de la altura del sprite del tocón.

## Fuera de alcance
Ataques, entrada, fuego, etc. (la animación de aparición es del subpaso M6, no ahora). No tocar `DebugCheats`.

## Resultado (lo rellena Codex)
- Hecho:
- Archivos tocados:
- Pruebas:
- Dudas:
