---
name: primordia-door-design-pending
description: "Puertas (corona del arco de arriba y marco lateral): ARTE HECHO y aprobado el 2026-11-02 en docs/multimedia-temp/doors-frames/; falta integrarlo en el código con Sonnet"
metadata:
  node_type: memory
  type: project
  originSessionId: 5ea70e7a-8e35-410c-86c4-93176709d125
  modified: 2026-11-03T00:34:58.121Z
---

**Arte HECHO y aprobado (2026-11-02)** en `docs/multimedia-temp/doors-frames/` (README con pivotes, capas y cambios de código).
Por tema (10): `Doorway_Top_Crown`, `Doorway_Side_Back/Front`, `Wall_Side_Deco_0..2`. Sin importar ni programar.

**Falta (Sonnet):** menú de importación, `RoomTheme` con los sprites, `FloorBuilder.BuildDoor` (corona arriba; Back/Front en
laterales), adornos en `WallRules`. `DoorwayCrossing` hoy solo cruza las de arriba.

**Lo que aprendimos (para no repetirlo):**
- Lo que funciona es pintar con la textura de los tiles del tema y usar vegetación o flecos (frondas, algas, cilios). Las masas con
  colores planos y contorno parecen pegadas (setas, platillos, pedruscos).
- Un arco paralelo a la pared lateral, en esta cámara, se ve de canto y queda como un poste: no proponerlo.
- La usuaria pidió escenas GIF (antes y después, con el jugador cruzando) para juzgar: hacerlas pronto en el arte de escenario.

Ver [[primordia-boss-art-workflow]] (nada que parezca pegado sobre el fondo).

**Integración (Sonnet, 2026-11-02): CÓDIGO ESCRITO y compila, sin probar en Unity.** RoomTheme (doorCrown, doorSideBack/Front, wallSideDeco), FloorBuilder.BuildDoorFrame (corona orden 41 en las de arriba; Back -7 y Front 40 en las laterales, voltea la derecha) y BuildWallDeco (semilla propia por sala), menú Primordia > Art > Importar coronas y marcos de las puertas (Editor/ArtSetup.DoorFrames.cs, pone los sprites en los 10 temas). Si los marcos laterales quedan desplazados, ajustar SideFrameOffsetX/Y y CrownLift en FloorBuilder. FloorBuilder.cs y algunos .cs son CRLF: los parches deben respetarlo.
