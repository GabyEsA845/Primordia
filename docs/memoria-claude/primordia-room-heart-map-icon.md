---
name: primordia-room-heart-map-icon
description: Pedido del usuario (2026-11-02): al hacer el sprite del corazón de sala, hacer también un icono de mapa para la sala con medio corazón sin recoger
metadata:
  type: project
---

`feature/room-hearts` (commit 62039a9, probado por el usuario: a veces aparece el corazón al limpiar) usa un sprite PROVISIONAL dibujado por código en `RoomHearts.cs` (clase `RoomHeartPickup`). **Cuando se haga el arte definitivo (skill primordia-sprites, preguntar antes), hacer además un icono especial en el mapa/minimapa para la sala que tenga un medio corazón en el suelo sin recoger**, para que el jugador no se lo pierda. Falta también el código de ese marcador en `MapUI` (hay `Rewards`, `Shop`, `Transition` como ejemplo); puede ir junto con la marca de «regreso» del mapa de [[primordia-floor3-design]].

**Why:** el usuario quiere que sea imposible perderse el corazón. **How to apply:** al abrir la tanda 7 (interfaz) del piso 3 o antes si hay hueco; Opus para el arte.
