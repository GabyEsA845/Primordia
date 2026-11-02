---
name: primordia-mud-body-polish
description: "Pendiente de arte+código (tanda Opus con el pulido de puertas): el lodo pegado al cuerpo del jugador como barra de hundimiento, en vez de la máscara recta"
metadata:
  node_type: memory
  type: project
  originSessionId: c2e7fcb9-3aa7-479c-9175-1715e21fb25a
  modified: 2026-11-02T17:42:32.246Z
---

Pedido el 2026-11-02 tras probar la subtanda 3 (lodo profundo): la máscara de `MudSinkVisual` corta al jugador con una línea
recta que baja al salir del lodo. La usuaria propone: mientras se hunde, burbujas y grumos de lodo sobre el cuerpo; al
salir, la zona cubierta queda oscurecida con la textura del lodo y se «limpia» mientras camina. El lodo del cuerpo hace de
barra de hundimiento (nivel = `PlayerMudSink.Sink`).

**Why:** la línea recta se ve mal y no se lee como lodo; el lodo en el cuerpo es más expresivo.

**How to apply:** hacerlo junto con [[primordia-door-crossing-polish]] cuando acaben las subtandas de estructura del piso 3
(Opus para el arte: sprites de grumos y burbujas por cuerpo —rana, Diplocaulus, escorpión látigo—, textura de lodo tileable;
luego Sonnet para implementar). Preguntar antes de crear arte (skill primordia-sprites). Implementación probable: tinte con
textura de lodo dentro de la silueta de cada capa (material/shader pequeño u overlay por capa), nivel que baja al caminar;
enemigos con versión simplificada. Ver [[primordia-floor3-structure-decisions]].
