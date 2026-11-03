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

**Paso 4a EN CURSO (2026-11-03, Sonnet):** HECHO y probado en tests (sin enemigos en el piso 3 no se pudo probar en juego): clase Parásito (`LineageClass.Parasite`, `Parasite`, `ParasiteBenefit`, `ArcFlight`, ajustes `parasite*` en `LineageBenefitSettings`), `Floor3LineageTests` (9), menú «Crear linajes del piso 3» (`Floor3LineagesSetup`: 8 LineageData sin toque, evoluciones, arte del Parásito en Floor3Mutations). Sin commit aún. También HECHO (compilado, 5 pruebas más en Floor3LineageTests, sin probar en Unity): beneficios de clase del piso 3 — Carnívoro (`FinisherBenefit.SetBurningCount`: los que tienen `Burn` cuentan como heridos), Venenoso (`ToxinBenefit.SetPuddle`: el envenenado que muere deja `PoisonPuddle.Spawn` de 6 s, ajuste `toxinPuddleSeconds`; si arde sale nube tóxica por sí solo), Hongo (`ColonyBenefit.SetSprouting`: muere un enemigo sobre una mata → otra mata junto a él hasta `matSproutLimit` = 6); textos en `MutationText.ClassBenefitFloor3`. Con eso el 4a queda completo salvo la prueba en juego; SIGUIENTE 4b (los 8 toques con los cambios de abajo).

**CAMBIOS DE DISEÑO DE LOS LINAJES (la usuaria, 2026-11-03; ACTUALIZAR el documento del piso 3 y el GDD al acabar los linajes; detalles por confirmar, ver preguntas):**
- Eryops (Carnívoro): ya no «Emboscada del fango». Cruza el lodo profundo SIN hundirse (solo ralentizado; el dibujo hundido es estético); en el lodo gana +30 % de velocidad, hasta 5 s después de salir (animación del jugador cubierto de lodo mientras dure). Xenacanthus: sin cambios (confundidos = heridos).
- Pulmonoscorpius: sin cambios (el chorro envenena); ARTE: chorro de vinagre distinto por ser venenoso. Ciempiés: ya no «Cien patas»; al aterrizar de un salto deja un charco de veneno y es inmune al veneno y al humo venenoso.
- Yesquero y Cordyceps: se quedan como estaban. «Colonizar la sala»: ARTE nuevo de las matas de hongo.
- Lamprea: igual, pero los parásitos pegados son MÁS GRANDES (arte). Sanguijuela: el charco del Escupitajo tiene otra textura (rojiza, con parásitos nadando); al explotar la bola deja un charco de la misma textura y las sanguijuelas saltan a los enemigos más cercanos de la sala; el Escupitajo ya no se quema con fuego (ARTE: charco, bola y explosión).
- Arte pendiente con Opus añadido: chorro venenoso, jugador cubierto de lodo, matas de hongo nuevas, parásito grande, charco/bola/explosión con sangre y parásitos.
- **Respuestas (misma fecha):** Eryops SUSTITUYE a Emboscada del fango; medio hundido en el lodo («cazador acechando»), +30 % en lodo normal y profundo, 5 s desde la última entrada (se renueva); necesita un MARCADOR visual de que el efecto está activo (arte Opus) y el jugador cubierto de lodo = capa sobre los 3 cuerpos reutilizando/retocando la animación de hundirse (solo con el linaje Eryops). Ciempiés SUSTITUYE a Cien patas: cada aterrizaje deja UN charco del tamaño del que dejan las vainas venenosas al romperse, 4 s; inmune a todo veneno y gas tóxico (también el propio). Lamprea: parásito grande SOLO con el linaje Lamprea. Sanguijuela: valen los charcos que pegan parásito Y la explosión (al explotar la bola saltan sanguijuelas a los enemigos más cercanos hasta llenar el máximo de 3, uno por enemigo: interpretación mía); el charco sangriento hace MÁS daño que el normal (interpretación mía: el de la Sanguijuela); Escupitajo no se quema SOLO con Sanguijuela activa. Chorro venenoso solo para Pulmonoscorpius. Matas de hongo nuevas solo del piso 3, tamaños mediano y grande (preguntar diseño al hacer la tanda). Más preguntas de arte al empezar la tanda con Opus.

**PLAN DEL 4b (acordado con la usuaria, 2026-11-03; el arte de los toques ya está hecho en `docs/multimedia-temp/floor3-lineage-art/`, README con hojas, ms y reglas de montaje; commit del 4a = fc5e5b5f). Cada tanda: prueba de la usuaria + commit; antes de la 1 añadir los 8 `LineageTouch` del piso 3 al enum, sus textos y el menú de importación del arte nuevo:**
1. Toques ligeros: Xenacanthus (confundidos = heridos), Ciempiés (charco de 4 s al aterrizar + inmune a todo veneno/gas tóxico), Lamprea (al girar pega otro parásito al más cercano, máximo 6, parásito grande `Parasite_Attached_Lamprey`/`Leap_Lamprey`).
2. Escupitajo y Chorro: Pulmonoscorpius (chorro envenena, metano → nube tóxica, prefijo `Fx_VinegarJet_Poison_`), Yesquero (bola ardiendo, rastro y explosión de fuego), Sanguijuela (charcos con parásitos y más daño, salto al explotar hasta 3, Escupitajo sin quemarse, arte rojizo con Idle de 8 cuadros, coágulos/gusanillos en la banda), Y las **matas de hongo del piso 3** (4 especies × M/L al azar, `FungusMat/`, Appear/Idle/Pulse/Snare).
3. Cordyceps (Zombi): propuesta aceptada — el confundido que muere se levanta con ~50 % de vida, ataca a los enemigos más cercanos con su ataque 4 s y cae de verdad; jefes no (por confirmar). **La usuaria quiere un detalle de diseño especial en los renacidos: ESPERAR a que haga una pequeña tanda de arte con Opus en otra sesión antes de empezar esta tanda.**
4. Eryops (lodo sin hundirse, +30 % 5 s, shader de barro, estela, terrones, `Eryops/`).
**TANDA 1 ESCRITA y compilada, SIN probar ni commit (2026-11-03):** enum `LineageTouch` con los 8 del piso 3 (MudStalker, CephalicSpine, GasSting, HundredLegs, Tinder, Zombie, SpinningSucker, LeechPool); `FinisherBenefit.SetConfusedCount`; `CentipedeTouch` (: `IFullPoisonImmune`, charco `PoisonPuddle.Spawn` 4 s en `JumpingLegs.Landed`; `Poison.Apply/ApplyContagious` devuelven null si el objetivo lleva `IFullPoisonImmune`; PlayerToxin ya usaba `IPoisonImmune`); `LampreyTouch` (girando = `SpinAttack.State == Spinning`: cada `Parasite.Attached` pega otro al `Parasite.FindNextHost`, máximo 6 solo mientras gira +1 s de margen, luego 3; `ParasiteBenefit.SetArt/AttachTo`; parásito grande `Parasite_Attached_Lamprey`/`Leap_Lamprey`). Menú «Crear linajes del piso 3» ampliado (toques en `Touches`, prefabs de la Lamprea). 7 pruebas más en Floor3LineageTests. Decisión mía: el máximo 6 de la Lamprea es solo mientras gira (lectura literal de la regla).
**TANDA 1 PROBADA Y CON COMMIT (4280ff2e; arte d3ee2f5c). TANDA 2 en tres partes (cada una prueba + commit): 2a Pulmonoscorpius, 2b Escupitajo (Yesquero + Sanguijuela), 2c matas de hongo.** **2a ESCRITA y compilada, SIN probar ni commit:** `IToxifiable` (MethaneCloud, MethaneSac, MethaneMist) + `Flammables.ToxifyAlong(from,to,radius)`; `JetAttack.Venom` (= `PlayerLineage.ActiveTouch == GasSting`): envenena 1/s 3 s al que daña (`jetPoisonDamage/jetPoisonSeconds`), el metano que toca el chorro (y la salpicadura del III) se vuelve `ToxicCloud` sin explotar y usa el arte `Fx_VinegarJet_Poison_*` (JetArt `JetArt_VinegarJet_Poison`, efectos `Effect_VinegarJet_Poison_*`, campos `poisonJet*` de `LineageBenefitSettings`; los crea «Crear linajes del piso 3»). 3 pruebas más.
**2a PROBADA y con commit (ee07d35b; incluye arreglo de hitbox de odres/vainas y del Toxify directo). 2b ESCRITA y compilada, SIN probar ni commit:** `LineageTouches.Of(weapon)`; **Yesquero** (`PoisonSpitBall.tinder`: sin baba, `GroundFire.Spawn` cada `SpitTrailEvery`, quema enemigos/inflamables que roza cada 0,1 s, bola teñida de naranja PROVISIONAL, `PoisonSpitBurst.Look.Fire` deja fuego en vez de charco y `Burn` a lo que alcanza; sin arte propio de fuego); **Sanguijuela** (arte en `ReactionKit` Leech* —`ConfigureLeechSpit/Ball/Slime`, lo carga «Crear linajes del piso 3» en `ReactionKit_Floor3`—; `SlimeTrail.Begin(..., leech)` con coágulos (20 %) y gusanillos (20 %) y sin `Flammables.Register`; `PoisonPuddle.SpawnSplat(..., leech)` no inflamable; `PoisonPuddle.PlayerBonus` +1 daño/s a charcos y baba del jugador y evento `EnemyAffected` solo con `harmsPlayer == false`; `PoisonSpitBurst.Burst` (no minis) → `LeechTouch` pega parásitos a los enemigos más cercanos hasta el máximo con salto en arco; charco al pisar pega parásito). Las hojas `Fx_PoisonLeechPuddleBig_*` NO se usan (el charco grande solo lo usan vainas y el charco del Venenoso). 9 pruebas más en Floor3LineageTests. Pendiente 2c: matas de hongo.
Al cerrar los linajes: documento interactivo del piso 3, GDD y PLAN con todos los cambios; luego subpaso 5 (reserva del piso 3, tienda, cambio, F4/F8). Las 5 pruebas de clases de piso 3 pasaron en Unity (14 en Floor3LineageTests).

**Why:** el GDD dejaba estas reglas abiertas; el usuario las cerró para poder implementar con Sonnet.
**How to apply:** implementar subpaso a subpaso con Sonnet; preguntar antes de cada uno ([[primordia-token-saving]]).
