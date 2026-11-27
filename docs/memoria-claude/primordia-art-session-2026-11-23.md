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
**(3) Marca de aviso del cono: ARTE APROBADO (2026-11-23)** (`floor3-bosses/lepidodendron/generar_marca_cono.py` → `Cones/Fx_ConeMark_{Follow,Lock,Hold}`): anillo orgánico de polvo rojo; la usuaria pidió **elipse en picada (0,7)** por coherencia visual → el daño del golpe debe pasar a elipse 1,6 × 1,12 u (Sonnet; README «Marca de aviso del cono»).
**(4) Iconos de las especiales del ámbar: APROBADOS (2026-11-23)** (`floor3-amber-icons/`, README): contorno dorado `#f2c24e` + destello en estrella = marca de las especiales; Cápsula D con corazón, Gota = cristales del lomo + gota, Cercos en V. Sustituyen a los de `floor3-mutations/Icons` (Sonnet).
**(5) Anillo de la Peonza: APROBADO (2026-11-23)** (`floor3-spin-ring/`, README): tras 1 s de giro, anillo de 2,0 u (igual en I-III) que daña con el daño del giro (alcance contra el daño por contacto); el clon del III lo lleva desde que sale, en su paleta con destello. Falta programarlo (Sonnet).
**Tanda 2026-11-23 de la lista CERRADA.** **(6) Decoración de la sala de paso 3 → 4: APROBADA (2026-11-23)** (`floor3-exit/generar_decor_salida.py` → `decor/`, README «Decoración»): subtandas A restos enterrados, B rocas (peñascos con colisión), C plantas (Walchia, matas, Calamites; Sway/Rustle), D huellas de sinápsido E/NE + barro + sal, E ráfagas. La usuaria pidió ver la sala **más amplia** (muestra de 60×26): las salas de muestra pequeñas no dejan juzgar la decoración. Reglas de código (rastro, ráfagas que no atraviesan paredes/sumidero/rocas) en el README. **Todo lo de la sesión falta importarlo y programarlo (Sonnet).**

**Sala de paso del piso 3 (tarea 20) IMPLEMENTADA por Codex en `codex/piso3-salida` (cac47587), probada por la usuaria (2026-11-23): «en general me gusta».** Dos retoques: (a) el sumidero está demasiado arriba → tarea 21 (`PitOffset` solo del piso 3, ~-3,5 celdas); (b) **la sala está totalmente vacía: ARTE PENDIENTE para Opus = decoraciones/props de la sala de paso del piso 3** (rocas, restos, huesos/troncos carbonizados, matas secas, algo que cuente la salida hacia el Pérmico; preguntar antes de dibujar), a añadir a la tanda de arte pendiente. Se deja la plantilla de cueva de siempre.

**2026-11-26:** NO queda arte pendiente de Opus de esta lista ni de las anteriores: subtanda 3 del ámbar DESCARTADA, decorado de los temas implementado y todo lo aprobado ya importado. La usuaria trae una idea nueva.
