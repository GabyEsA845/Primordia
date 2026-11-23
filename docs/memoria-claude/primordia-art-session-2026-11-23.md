---
name: primordia-art-session-2026-11-23
description: Tanda de arte de Opus del 2026-11-23 (sin los cambios del otro PC): orden, decisiones de la usuaria y lo que se deja de lado
metadata:
  type: project
---

**2026-11-23 (Opus):** la usuaria no subió los últimos cambios del otro ordenador, así que se trabaja arte independiente.

Decisiones sobre la lista de arte pendiente:
- **Veta de carbón y Bosque en humo: APROBADOS** (`floor3-special-rooms/`).
- Orden de la sesión: (1) **sala de paso del piso 3** (diseño + arte), (2) **arte final de la espesura de la Arthropleura** (hoy tiles provisionales), (3) **marca roja de aviso del cono del Lepidodendron** (hoy por código, `LepidodendronCone`), (4) **iconos de las especiales del ámbar** (el resto de su arte le gusta), (5) **área de giro de la Peonza** (nuevo: la Peonza se hacía daño sola al golpear normal; habrá un área pequeña a su alrededor que daña al empezar a girar).
- **De lado por ahora:** piezas propias de las salas con tema ([[primordia-floor3-room-themes]]), subtanda 3 del ámbar (contorno dorado y motas, [[primordia-floor3-specials-look]]) y audio del piso 3.
- Preguntar y recomendar antes de cada paso ([[primordia-boss-art-workflow]], [[primordia-sprites-skill]]).

**(1) Sala de paso del piso 3: ARTE APROBADO (2026-11-23)** en `docs/multimedia-temp/floor3-exit/` (README): sumidero de arena, temas `Permian` y `BurntSand`, mezcla entre franjas ([[primordia-theme-transitions]]). Falta importar y programar (Sonnet, rama del piso 3). Python 3.12 + Pillow instalados en este PC. **SIGUIENTE: (2) espesura de la Arthropleura.**

**(2) Espesura de la Arthropleura: ARTE APROBADO (2026-11-23)**, rama `feature/arthropleura-canopy-art` (sale de `feature/floor3-exit-art`). Subtandas: A espesura por autotile (`Thicket/`, tapa aprobada del tramo como fondo + coronas de helecho y copas de Lepidodendron; 47 formas + variantes de cortina), B brechas (`Breach/`: filo roto por máscara, restos densos/ligeros, surcos girados), C caída F2 (`Fallen/`: alfombra por máscaras solo donde había espesura, troncos y helechos tumbados sin colisión), D lodo (`MudDouse` 48×48, `MudWake` onda 128×80 sin girar; escena `escenas/Arthropleura_F2_lodo.gif`). Detalle y reglas para el código en el README de la carpeta (sección «Arte final de la sala»). Lecciones: el usuario quiere **más copas de árbol** (sensación de bosque) y **variantes** para que nada se repita; en escena se vio que los efectos deben medirse contra el cuerpo del jefe (~64 px de ancho). Falta importar y programar (Sonnet). La sala debe usar `BandThemes[1]` (humeante), no `[2]`.
**SIGUIENTE en esta tanda (Opus):** (3) marca roja de aviso del cono del Lepidodendron, (4) iconos de las especiales del ámbar, (5) área de giro de la Peonza. Preguntar antes de cada una.
