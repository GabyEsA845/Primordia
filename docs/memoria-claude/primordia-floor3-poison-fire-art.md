---
name: primordia-floor3-poison-fire-art
description: Arte pendiente para Opus (decidido 2026-11-11) - nube/aspecto ligero cuando arde el veneno del Escupitajo con Escamas carbonizadas III; mas dos arreglos de codigo sugeridos
metadata:
  type: project
---

**ESTADO 2026-11-11: ARTE DIBUJADO (Opus) en `docs/multimedia-temp/floor3-poison-fire/` (Fx_PoisonHaze Form/Idle/Fade + Fx_PoisonSplat_Ignite); falta importar y programar (Sonnet).**

**Problema (la usuaria, 2026-11-11):** con el Escupitajo venenoso + **Escamas carbonizadas nivel III (Llamarada)** el ataque prende todo lo inflamable por donde pasa y donde impacta; el reventon deja un charco de veneno (`PoisonPuddle`, inflamable) que arde al instante y suelta una `ToxicCloud` que **reutiliza el dibujo del gas del metano**. Con la Camada (muchas bolitas) salen muchas nubes y la pantalla queda muy cargada. No tiene que ver con el linaje Ciempies (solo lo noto ahi).

**Decision de la usuaria:** probar **arte nuevo con Opus en otra sesion** (no resolver solo con codigo). Opus: preguntar antes de dibujar (skill primordia-sprites). Idea de arte: una nube/llama propia, mas pequena y translucida, para cuando arde un charco del jugador (distinta de la nube del metano).

**Arreglos de codigo SUGERIDOS (sin decidir, preguntar a la usuaria antes de hacerlos):** (1) con fuego en el ataque (Llamarada/Yesquero) el reventon no deja charco sino fuego en el suelo (el Yesquero ya lo hace: `PoisonSpitBurst.Look.Fire`); (2) tope de `ToxicCloud` simultaneas (p. ej. 3) y las nuevas mas translucidas. Archivos: `Combat/PoisonSpitBall.cs`, `Combat/PoisonSpitBurst.cs`, `Rooms/PoisonPuddle.cs` (Burn, CloudFrame), `Rooms/ToxicCloud.cs`, `Mutations/CharredScales.cs`.

**How to apply:** esta en la tanda de arte pendiente de Opus junto a [[primordia-floor3-specials-look]] (subtanda 3), la sala de paso del piso 3, aprobar Veta de carbon y Bosque en humo y el audio del piso 3. Ver [[primordia-project-status]].
