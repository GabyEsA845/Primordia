---
name: primordia-door-design-pending
description: "RECORDAR AL USUARIO al cambiar a Opus por cualquier tarea de diseño: dos fallos de diseño de las puertas (copas/techo sobre el arco y arcos laterales) para diseñar, dibujar e implementar entonces"
metadata:
  type: project
---

**ENCARGO DE LA USUARIA (2026-11-02): cuando volvamos a cambiar a Opus para cualquier tarea de diseño, RECUÉRDALE estos dos detalles y
aprovechen para implementarlos ahí** (no antes; no esperar a acabar todo el piso 3):
1. La parte superior de las puertas de arriba se CORTA de golpe contra el fondo: faltan copas de árboles o una especie de techo sobre el arco.
2. Las puertas LATERALES no tienen arco: entrar a una sala de lado se siente artificial.

**How to apply:** al avisar del cambio a Opus (ver [[primordia-token-saving]]), añadir la frase «recuerda lo de las puertas». Luego: preguntar
(skill primordia-sprites), diseñar copas/techo del arco superior y arcos laterales por tema y piso (1, 2 y 3), y con Sonnet integrarlo en
`FloorBuilder.BuildDoor`, `DoorwayCrossing` (hoy solo cruza las de arriba; las laterales irán por el lado) y `RoomTheme`.
