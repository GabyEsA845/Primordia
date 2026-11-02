---
name: primordia-floor3-mouths-decisions
description: Paso 3 del piso 3 (Bocas Escupitajo venenoso, Peonza y Chorro de vinagre) HECHO: decisiones finales, números, archivos y cómo probar
metadata:
  type: project
---

Paso 3 «Cuerpos y Bocas» del piso 3 HECHO en `feature/floor-3` (commit 77877540, 2026-11-02, sin push; las pruebas PlayMode nuevas están escritas pero NO ejecutadas). Probado a mano por la usuaria con Unity: las 3 Bocas funcionan y gustan; falta el pulido gráfico (ver [[primordia-floor3-step3-art-pending]]).

**Cómo probar:** menú `Primordia > Floor 3 > Crear Bocas del piso 3 (cuerpos y datos)` (vuelve a crear cuerpos, ataques, prefabs y pone la rana en Test_Floor3) y en `Test_Floor3` F6 cambia de Boca. Código: `Editor/Floor3MouthsSetup.cs`, `Editor/ArtSetup.Floor3Bodies.cs`; datos en `Data/Mutations/Floor3`, `Data/Attacks`, `Prefabs/Combat`. Pruebas: `PoisonSpitTests`, `SpinAttackTests`, `JetAttackTests`, `MovementFeelTests`.

**Números finales (cambian el GDD/PLAN al actualizarlos):**
- **Escupitajo venenoso (rana, hit 0,56 s):** bola a 2 u/s, vive 4 s, cada 2,8 s (II 2,4); 1 de daño al atravesar a cada enemigo; charco pequeño cada 0,5 s (3 s; II/III 4 s; frena 30 % y envenena a enemigos, NO daña al jugador); estalla al acabar, al chocar o al disparar otra: 6 de daño en 1,5 u (II 8) y charco grande de 4 s; III Camada: 4 bolitas en cruz, 1 s de vuelo, 3 de daño en 0,8 u. Los charcos arden (PoisonPuddle). Código: `Combat/PoisonSpitBall.cs`, `PoisonSpitBurst.cs`, `Projectile` (eventos Launched/Ending con EndReason), `PoisonPuddle` (pequeño, harmsPlayer).
- **Peonza (Diplocaulus, inercia 1,5):** mantener gira: radio 1,3 u, 2 de daño cada 0,25 s, empuja hacia fuera, 35 % más lento, máx 3 s, **mareo 1 s sin poder moverte ni esquivar** (`MovementLocked`), espera 0,5 s tras soltar, sin invulnerabilidad, esquivar la corta. II: 3 de daño, radio 1,5, 4 s, devuelve proyectiles enemigos. III Bumerán: si giras ≥1 s, al soltar sale un **clon translúcido (provisional: Spin tintado) 9 u hacia el cursor**, va y vuelve a 14 u/s, 8 de daño y aturde 0,8 s a la ida y a la vuelta; no puedes atacar hasta que vuelva. Código: `Player/SpinAttack.cs`, `Combat/SpinBoomerang.cs`, `PlayerAnimator.Hold/Release/ClipLength`.
- **Chorro de vinagre (vinagrillo):** mantener; chorro desde la punta de la cola (offsets por orientación en JetAttack), sigue al cursor a 180°/s, 1 de daño cada 0,15 s, crece desde la cola (30 u/s) y se recoge al soltar (40 u/s), inicio/punta suavizados por código. Alcance **5 u (I), 6 u (II), 7 u (III)**; I se corta en el primer enemigo o pared, II/III atraviesan (paredes cortan); barra 3 s (II/III 4 s), se rellena en 2 s al soltar, si se vacía queda bloqueada hasta llenarse (3 s); III Salpicadura **radio 2 u**, 1 de daño cada 0,5 s al final del chorro. Barra de carga provisional bajo el jugador (1,05 u abajo; la del órgano está a 0,85). Código: `Player/JetAttack.cs`.
- **Movimiento (rama feature/movement-feel, ya fusionada en floor-3):** sombra bajo el jugador (`PlayerShadow`) y arranque 0,07 s / frenada 0,09 s multiplicados por `Inertia` por cuerpo (rana 0,8, Diplocaulus 1,5, vinagrillo 1). Falta la C (estiramiento/aplastamiento) y D (polvo): la usuaria dijo que lo comentaría.

**Pendiente de este paso:** pasada de arte con Opus; ejecutar las pruebas PlayMode (no se han ejecutado nunca las nuevas); documento del piso 3 + GDD/PLAN con estos números (la usuaria lo hará tras el arte nuevo); cuando toque, preguntar por push de floor-3.
Relacionado: [[primordia-floor3-design]], [[primordia-project-status]].
