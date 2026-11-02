---
name: primordia-floor3-art-batch-pending
description: "Tanda de arte (Opus) pendiente del piso 3 programado hasta ahora: lodo en el cuerpo, puertas, llamas, iconos de barras, efecto del agua, etc., con lo que hay que programar después"
metadata:
  node_type: memory
  type: project
  originSessionId: c2e7fcb9-3aa7-479c-9175-1715e21fb25a
  modified: 2026-11-02T18:52:54.054Z
---

Lista acumulada (2026-11-02) de arte provisional o pendiente del piso 3, para hacerla de golpe con Opus (skill primordia-sprites;
PREGUNTAR antes de dibujar; avisar del cambio de modelo, ver [[primordia-token-saving]]). Proponer hacerla al cambiar de paso del PLAN
(tras las Reacciones, antes de Cuerpos y Bocas) o al acabar el piso.

1. **Lodo en el cuerpo del jugador** en vez de la máscara recta (grumos/burbujas que se limpian al caminar): [[primordia-mud-body-polish]].
2. **Pulido de puertas:** el jugador se ve «pegado» sobre el hueco del arco al cruzar (todos los pisos): [[primordia-door-crossing-polish]].
3. **Llamas sobre el cuerpo** de los 3 cuerpos del jugador (rana, Diplocaulus, escorpión látigo) y de los enemigos que arden, y el
   **icono definitivo de la barra de quemado** (hoy llama dibujada por código en `Rooms/BurnIcon.cs`; marcador `Fx_Burning` centrado).
4. **Icono definitivo de la barra de veneno de la nube** (hoy gota por código, misma clase `BurnIcon`; la gota sigue visible durante
   la intoxicación como cuenta atrás). Si se confunde con la barra al llenarse, darles colores/forma más distintos.
5. **Efecto o animación del jugador al pisar una charca de agua** (petición explícita de la usuaria, 2026-11-02): salpicadura/ondas
   al entrar y andar por el agua de charca (`Rooms/GroundPuddle.cs`, hoy el agua es solo decoración).
6. Quizá: luz 2D cian de los odres de metano (el sprite ya trae brillo; luz aparcada), empuje de la fumarola (no hay mecánica de
   empuje al jugador).

**CÓDIGO pendiente ligado a esto:** (a) **el agua de charca debe apagar el quemado**, igual que el lodo profundo: llamar a
`PlayerBurn.Douse` (el lodo lo hace a 3 de barra por segundo en `PlayerMudSink`) y quitar `Burn` a enemigos que la pisen; hoy solo
el lodo apaga. (b) Enemigos muy inflamables dejan rastro de fuego mientras caminan en llamas (al programar los enemigos, paso 5).

**Why:** la usuaria prefiere agrupar el arte en una sola tanda con Opus y revisar el aspecto jugando antes.
**How to apply:** al abrir la tanda de arte, leer esta lista, preguntar estilo/animaciones y luego (Sonnet) implementar.
