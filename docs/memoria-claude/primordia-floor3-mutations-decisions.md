---
name: primordia-floor3-mutations-decisions
description: "Paso 4 del piso 3 (mutaciones y linajes): reglas concretas decididas con Opus el 2026-11-03 y subpasos para implementarlo con Sonnet"
metadata:
  type: project
---

Diseño base en el GDD («Mutaciones del piso 3» y «Clases y linajes del piso 3»). Todo el arte existe ya
(`docs/multimedia-temp/floor3-mutations/` y `floor3-lineages/`, con README); no hace falta arte nuevo.

**Subpasos (cada uno: prueba del usuario + commit en feature/floor-3), como el piso 2 ([[primordia-floor2-mutations]]):**
1 Extremidades (Patas de salto con `PlayerMudSink.DodgeResetsSink`, Patas hilanderas) · 2 Piel (anfibia, carbonizadas) ·
3 Órganos (Saco de metano, Feromonas + sistema de confusión) · 4a clases del piso 3 + Parásito (Chupar) + 8 LineageData y
evoluciones · 4b los 8 toques · 5 reserva solo del piso 3, tienda, sala de cambio, atajo F8, documento del piso, GDD/PLAN.

**Subpaso 1 (Extremidades) ESCRITO y compilado con dotnet, SIN probar en Unity ni commit (2026-11-03):**
`JumpingLegsMutation`+`JumpingLegs` (esquiva = salto: excluye la capa Enemy del Rigidbody2D, ignora los colliders de
`JumpableObstacle` —los obstáculos 1×1 de FloorBuilder— y los restaura al no pisarlos; `PlayerMudSink.DodgeResetsSink`; aterrizaje
esparce el charco de veneno cercano, II 3 de daño radio 1,2, III aturde 0,8 s con `EnemyAI.Knockback(zero)`; evento `Landed`).
`SpinnerLegsMutation`+`SilkThreads`+`SilkThread`+`SilkWeb` (hilo entre inicio y fin de la esquiva, 5 s, 2 de daño una vez por
enemigo, frena a 0,5; máx 1/2/2 hilos, el más viejo se rompe; III telaraña 4 s donde se cruzan, inmoviliza 1 s; eventos `Laid`,
`WebFormed`). Editor: `Floor3MutationsSetup` (menús Primordia > Mutations > Crear mutaciones del piso 3 (Extremidades) /
Equipar Patas de salto|hilanderas en la escena de prueba del piso 3). Pruebas: `Floor3LimbsTests` (9). Sin reserva del piso 3
aún (subpaso 5). Decisión mía sin preguntar: «obstáculos bajos» = los de 1×1; los de 2×2 y las paredes paran el salto.

**Subpaso 2 (Piel) REHECHO tras la primera prueba y compilado, SIN probar ni commit (2026-11-03):** la usuaria las quiso contrastadas.
**Piel anfibia = defensiva** (`AmphibianSkinMutation`+`AmphibianMucus`, ya no es IHitReflector): se carga 3 s/2 s de pie en `Wetness.IsWet` (lodo
profundo, `GroundPuddle.IsOver` agua/lodo, `PoisonPuddle.IsOver`) sin esquivar; cargado = inmune a fuego, gas y veneno de plantas (hereda
`FireImmune` con `IsImmune => Charged` e implementa `IPoisonImmune`) y apaga el quemado; un golpe rompe el moco CON daño (I y II, vía
`Health.Damaged`); II +20 % velocidad; III **Bloqueo**: `Health.AddDamageModifier` anula el primer daño de cualquier tipo, rompe el moco y da
0,4 s de invulnerabilidad (sin Reflejo). **Escamas carbonizadas = ofensiva** (`CharredScalesMutation`+`CharredScales`, hereda `FireImmune`): I
inmune a fuego/gas y, tras pisar `GroundFire` a <0,8 u, 4 s de ataques que dan `Burn` 1/s 3 s (`Weapon.HitTarget`); II + inmune al veneno de
plantas + **Brasa** (en `Health.Damaged`: radio 1,5, 4 de daño, Burn, `Flammables.IgniteNear`, recarga 5 s); III **Llamarada**: siempre cebadas
y cada 0,1 s prenden lo inflamable junto a tus proyectiles (`Projectile.Active`), el giro (radio 1,4) y el chorro (5 u), y donde impactan
(0,9 u). `FireImmune` pasó a tener `IsImmune` virtual; `PoisonImmune` es ahora interfaz `IPoisonImmune` + ayudante estático. Menú «Crear
mutaciones del piso 3 (Piel)». Pruebas: `Floor3SkinTests` (6). Decisión mía: la inmunidad al veneno incluye la ralentización del charco.

**Subpaso 3 (Órganos) ESCRITO y compilado, SIN probar ni commit (2026-11-03):** `MethaneSacMutation`+`MethaneCloud` (AbilityMutation, recarga 10 s): tras 0,35 s
(cuadro 5 del dibujo) sale una nube (II: dos en abanico de ±7°) que deriva a 0,6 u/s hacia `AimDirection`, 6 s, la para una pared; vive en la capa
Enemy con 1 de vida y `excludeLayers` Player|Enemy (la golpean tus ataques por capa, como el odre; no choca con nadie); explota al morir/`Ignite()`
(IFlammable: fuego y otras explosiones): 6 de daño radio 2 a todo menos al jugador, `Flammables.IgniteNear`, III Incendio `GroundFire` 3 s. Mientras
se lleva el saco el jugador tiene `ExplosionImmune` (nuevo; `Explosion.Detonate` lo respeta: decisión mía, vale para el odre también).
`PheromonesMutation`+`Confusion` (recarga 14 s): lanza al cursor hasta 5 u (lo corta una pared de la capa Default), radio 2, confunde 3 s/4 s;
jefes (`IBoss`) inmunes. `EnemyAI` tiene `IsConfused`+`OverrideTarget` (sin objetivo se queda quieto; los cerebros reciben ese objetivo); el daño lo
hace `Confusion`: mordisco cada 0,8 s al tocarlo con el daño de `ContactDamage` o de su ataque (mín. 1) y sus proyectiles
(`Projectile.Active`, `Source == weapon`) dañan al primer otro enemigo que tocan (no chocan por la matriz de capas); III Frenesí ×2. Menú «Crear
mutaciones del piso 3 (Órgano)». Pruebas: `Floor3OrganTests` (6). Los enemigos del piso 3 aún no existen: probar las Feromonas con los del piso 1/2.

**Ajustes del Saco de metano tras probarlo (2026-11-03, la usuaria):** nube 8 s a 0,9 u/s (campos renombrados `cloudSettings`, `fanDegrees`, `fanGap` para
que el asset viejo no pise los valores); las dos nubes del nivel II salen a 1,6 u una de otra y abiertas ±18°; el fuego del Incendio no quema al jugador
(`GroundFire.Spawn(..., harmsPlayer: false)`; los enemigos sí arden y las Escamas siguen cebándose). Feromonas sin probar a fondo: no hay enemigos del piso 3, la
usuaria avisará si ve problemas. **SUBPASOS 1-3 COMPLETOS Y PROBADOS; commit hecho. SIGUIENTE: tanda de arte pendiente con Opus (lista en [[primordia-provisional-art]]) y luego 4a/4b linajes.**

**Arte del salto integrado (2026-11-03, Sonnet), SIN probar ni commit:** menú «Crear Bocas del piso 3 (cuerpos y datos)» otra vez (reimporta los cuerpos con `Jump_*` y `JumpCharge_*`) y «Crear mutaciones del piso 3 (Extremidades)» (marcador). `PlayerController.DodgeProgress/DodgeLength/DodgeDuration`; `PlayerAnimator.JumpProgress` (cuadro por avance: fases 0/12/35/62/88 %, animator a velocidad 0) y `JumpCharging`; `JumpingLegs` sube el cuerpo en arco (0,375 × distancia, tope 2,2 u) con sombra; `JumpMarker` usa diana/esquinas (cambia a «Full» con la carga al máximo); `SilkNetArt` teje la red con el polígono (9 cuadros) y `SilkNet` la anima; ya no hay hilos de contorno ni celdas de SilkWeb en la red. Las capas de Piel se ocultan solas durante el salto (no tienen esos estados).

**Arreglos tras probar el salto (2026-11-03):** `PlayerController.MoveFrozen` (no anda pero apunta, ataca y esquiva) lo pone `JumpingLegs` mientras carga (desde que pasa 1 s); las Pieles tienen ahora hojas `Skin_<Piel>_<Cuerpo>_Jump|JumpCharge_<dir>` (40×40, `python generar_mutaciones_piso3.py jumpskins`, calculadas de las hojas de salto) para no desaparecer al saltar o cargar; hay que ejecutar otra vez «Crear mutaciones del piso 3 (Piel)».

**ORDEN NUEVO (2026-11-03, la usuaria):** primero subpasos 2 (Piel) y 3 (Órganos), probar las 3 ranuras, y ANTES de los linajes (4a/4b) hacer todo el arte pendiente con Opus (lista en [[primordia-provisional-art]]). Error de assets viejos: al cambiar el tipo de un campo serializado de una mutación hay que renombrarlo (landRadius→landRadii, damage→damages, maxThreads→threadLimits) y volver a ejecutar el menú «Crear…».

**Cambios tras la primera prueba (2026-11-03, la usuaria):** Patas de salto = salto ALTO de 4/5/6 u (nivel I/II/III; `controller.DodgeDistance/DodgeDuration` por delta, duración 0,25·√(d/3)); II aterrizaje 3 de daño radio 1,6; III **salto cargado**: `PlayerController.ManualDodge` + eventos `DodgePressed/Released` + `TryDodge(direction, distance, duration)`; mantener ≥1 s muestra `JumpMarker` (anillos por código, provisional) que va de 3 a 8 u en 1,5 s hacia el cursor (se para ante paredes); al soltar salta ahí: 6 de daño, radio 2,2, empuja y aturda 1,2 s; un toque corto = salto normal de 6 u. Patas hilanderas: esquiva 4/5/5 u, hilos 7/9 s/permanentes, daño 3/4/4, máx 1/2/3 hilos; III **Red** (`SilkNet`): con 3 hilos en la sala (se sueltan al cambiar de sala) los extremos forman el contorno convexo, los hilos se sueltan y la telaraña daña 4 una vez e inmoviliza 3 s a lo de dentro, y se empieza de cero. Valores míos de hilos (la usuaria pidió subir rango, duración y daño sin cifras). **ARTE PENDIENTE (Opus, preguntar antes):** la usuaria eligió arte nuevo para el salto alto (rana, Diplocaulus y vinagrillo: agacharse, subir, caer, sombra), marcador del salto cargado y telaraña/red; hoy se usan los efectos existentes y anillos de código.

**Atajos de prueba (2026-11-03, pedido de la usuaria; DebugCheats):** F1/F2/F3 rotan Extremidades/Piel/Órganos del piso (mismo nivel), F4 rota linajes (completa uno), F5 sube nivel (Boca, Extremidades, Piel, Órgano), F6 Boca, F7 invulnerable, F8 sala de cambio. Con Mayús: F2 daño ×5, F3 vaciar sala, F4 mutación al azar, F5 especial. Los menús «Equipar … en la escena de prueba» se quitaron (no funcionaban).

**Reglas decididas (2026-11-03):**
- Patas de salto: invulnerable en el aire; pasa sobre enemigos, proyectiles, lodo, charcos y obstáculos bajos (rocas,
  tocones); paredes y árboles altos lo paran.
- Patas hilanderas II = 2 hilos a la vez; III Telaraña donde se cruzan dos hilos.
- Piel anfibia: carga en charcos de agua y de lodo, lodo profundo y charcos de veneno; el moco dura hasta recibir un golpe.
- Escamas III Brasa: al recibir golpe, llamarada radio 1,5, 4 de daño, prende inflamables, recarga 5 s.
- Saco de metano: sale del jugador y deriva a 0,6 u/s hacia el cursor, 6 s; recarga 10 s (II = 2 cargas). Usa la reacción de
  metano existente: explota con tus ataques, fuego y explosiones; tu explosión no te daña.
- Feromonas: nube rosa lanzada **en el cursor** (hasta ~5 u), radio 2; jefes y minijefe inmunes. Los confundidos persiguen y
  atacan al enemigo más cercano con su ataque normal.
- Parásito (Chupar): **máximo 3 parásitos vivos en total** (entre todos los enemigos).
- Venenoso: el envenenado que muere deja un charco grande de veneno (si arde → nube tóxica).
- Lamprea: mientras giras con la Peonza, cada parásito que pegas pega otro al enemigo más cercano y el máximo sube a 6.
- Sanguijuela: un enemigo que pisa tus charcos de veneno recibe un parásito (cuenta para el máximo).
- Yesquero (Yesca): la bola va ardiendo; su rastro es de **fuego en vez de veneno** y al explotar deja fuego.
- Pulmonoscorpius (Aguijón de gas): el chorro envenena; si toca metano (tu nube, la niebla o un odre), en vez de explotar se
  vuelve nube tóxica; con otros ataques explota como siempre.

**Why:** el GDD dejaba estas reglas abiertas; el usuario las cerró para poder implementar con Sonnet.
**How to apply:** implementar subpaso a subpaso con Sonnet; preguntar antes de cada uno ([[primordia-token-saving]]).
