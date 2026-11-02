---
name: primordia-door-crossing-polish
description: "Pendiente de pulido: el jugador no se ve bien al cruzar una puerta (arco de pared superior), desde el piso 1"
metadata:
  node_type: memory
  type: project
  originSessionId: c2e7fcb9-3aa7-479c-9175-1715e21fb25a
  modified: 2026-11-02T17:18:02.472Z
---

Detectado el 2026-11-02 al probar el piso 3 (Test_Floor3): al pasar por una puerta de arriba el jugador se dibuja
entero encima del hueco del arco y se ve «pegado» sobre el fondo oscuro (el arco, el hueco y las raíces/membrana no
lo tapan ni lo recortan). Pasa desde el piso 1, en todos los pisos.

**Why:** la usuaria quiere mejorar esa animación/solapamiento; lo dejó para después de la estructura del piso 3.

**How to apply:** cuando toque, revisar el orden de dibujado del jugador frente al arco (`Doorway_Top`, `Door` en
`RoomTheme`) y proponer opciones (recorte/máscara por el arco, que el jugador pase «por detrás» de la parte alta del
arco, ligera sombra/fundido al cruzar). Preguntar antes de tocar arte. Ver [[primordia-floor3-design]].
