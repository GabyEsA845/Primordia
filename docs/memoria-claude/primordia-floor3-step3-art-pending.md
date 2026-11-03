---
name: primordia-floor3-step3-art-pending
description: "Pasada de arte e integración del paso 3 del piso 3 (Bocas): HECHA, probada y subida (commit d9244789, 2026-11-03)"
metadata:
  type: project
---

**TERMINADO y probado por la usuaria (commit d9244789 en feature/floor-3, push hecho, 2026-11-03).** Todo lo que estaba pendiente
de arte (Opus) y de integración (Sonnet) del paso 3 está integrado: Escupitajo (bola que rueda + banda de baba `Combat/SlimeTrail.cs`,
sale 0,35 u más abajo de lado: `SideMouthDrop`), Chorro (`Combat/JetArt.cs`, montaje por huecos de 1 u), clon de la Peonza
(`SpinBoomerang` con ecos/remolino), puertas de los pisos 1-3, recuadro del Órgano en el HUD (`UI/OrganHud.cs`, sustituye a la
barra bajo el jugador; sonido `UI_OrganReady`), metamorfosis por pareja y resúmenes de las cartas de Boca. Cada carpeta de arte
(`floor3-spit`, `floor3-jet`, `floor3-top-clone`, `doors-frames`, `organ-hud`, `floor3-metamorphosis`, `floor3-transition`)
tiene README con lo que usa el código. Ver [[primordia-floor2-transition-room]] y [[primordia-door-design-pending]].

**Por hacer de esa pasada:** nada. Solo comprobar que las pruebas PlayMode dan 563/563 tras el último arreglo (JetArt.Start →
StartFrame y RoomArtTests con EdgeRightFaceLower; antes 562/563).
