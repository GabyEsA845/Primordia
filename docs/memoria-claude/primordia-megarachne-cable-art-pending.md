---
name: primordia-megarachne-cable-art-pending
description: Cables v2 de la Megarachne (2026-11-18 arte, 2026-11-20 código) - HECHO, importado, programado y probado por la usuaria
metadata:
  type: project
---

**2026-11-20: HECHO (arte + código, probado por la usuaria).** Arte de Opus del 2026-11-18 (estilo B: haz de hebras
64×29 en vertical + abanicos `Nido/Nido_CableRoot` 5×256×224, pivote (0,5, 36/224)); importado y programado por Codex
(tarea 16, `docs/codex/tarea-16-megarachne-cables-v2.md`): material `MegarachneCable.mat` con `mainTextureScale (0.25, 1)`,
`MegarachneCableThin.mat` para el hilo colgante (animado a 8 fps), 5 `Nido_CableRoot_i` en orden 22 que siguen el alfa de
la copa, `SetCableRootVisible(i, bool)` listo para M4 (cable que arde = abanico oculto), hilo en orden 23. Menú
`Primordia > Boss > Actualizar cables Megarachne (v2)`. `MegarachneBossTests` 24 pruebas.
`Cable_Grow/Burning/Charred` ya están importados para M4.

**Ajustes de Claude tras la prueba visual:** el cable se veía cortado en la copa → el dibujo del cable empieza
`CableRootInset = 2` u más adentro (solo visual). Hitbox: tronco de −0,45 a 1,25 u y Sigillaria del pie al amarre del cable.
Si el abanico se pierde sobre la telaraña blanca sería arte (abanico más grande/oscuro; preguntar antes).

**No ejecutar** `generar_efectos_megarachne.py cables` (machacaría los tramos nuevos).

Relacionado: [[primordia-boss-megarachne]], [[primordia-provisional-art]].
