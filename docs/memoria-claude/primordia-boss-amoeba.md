---
name: primordia-boss-amoeba
description: "Diseño aprobado 2026-10-22 del segundo jefe del piso 1 (ameba gigante), su sala circular, cinemática y doble de vida en el piso 1"
metadata:
  node_type: memory
  type: project
  originSessionId: ac95b4b8-2b32-433a-8b9b-b419650c1901
  modified: 2026-10-23T01:21:33.939Z
---

Aprobado por el usuario el 2026-10-22 (rama `feature/boss-amoeba`):

- **Vida:** ameba 180; virus 120 (antes 60); todos los enemigos del piso 1 al doble. Medio corazón por contacto en todas las fases (salvo agotada). Sin ataques en fases 1-2; descartado engullir proyectiles.
- **Movimiento fases 1-2 (cambiado 2026-10-22 tras jugar):** embiste en línea recta hasta la pared (se aplasta), para; aviso 0,6 s temblando y mirando hacia donde saldrá (hojas Aim_0..7 / AimCalm_0..7, la cara se desplaza), se encoge y sale a toda velocidad. Velocidades 1,2 calma, 6 enojada, 10 medianas. No choca con el jugador (rb.excludeLayers): daño por solapamiento en AmoebaBody y avisa a Thorns.HurtAttacker.
- **Fase 1 calma (100-80 %):** sale en dirección al azar. **Enojo (80-50 %):** pequeña animación y luego apunta al jugador.
- **Fase 2 (50-10 %):** se divide en 2 amebas al 60 % de tamaño, cada una por su cuenta hacia el jugador, rebotan entre sí, una sola barra de vida compartida.
- **Fase 3 (10 %):** cinemática: proyectiles fuera, jugador congelado e invulnerable, franjas de cine, HUD del jugador oculto (no la barra del jefe); cámara al centro 2 s, fusión 3 s, enojo 2 s. Luego bullet hell de 30 s (3 tramos de 10: anillos con hueco, espiral doble, anillos+espirales+ráfagas dirigidas), difícil pero no imposible y distinto al virus; sin contador (se nota por la animación). Inmune: barra gris con candado. La cámara se aleja y centra para mostrar toda la sala y vuelve al jugador al terminar.
- **Agotada:** pálida, quieta en el centro, barra roja que palpita/tiembla, muere de 1-2 golpes.
- **Sala:** circular escalonada con piezas existentes, más grande que la pantalla (la ameba es más grande que el virus), vacía, color que contraste con la ameba rosa pálido, distinto de las otras salas.
- **Selección:** 50/50 totalmente al azar en cada partida. Escena de prueba con la ameba siempre de jefe y 3 copias de cada mutación en la sala inicial.
- **Arte:** lo genero yo con Python y muestro vista previa (puede ir tras la programación).

**Why:** completar el piso 1 con dos jefes al azar (ver [[primordia-floors-design]]).
**How to apply:** seguir [[primordia-workflow]]; commit solo tras confirmación del usuario.

**En main (commit 66f80e3, merge 22eec23, 2026-10-23; confirmado por el usuario):** código hecho y compilado con dotnet (copias *.check.csproj temporales, ya borradas). Raíz quieta con vida compartida (`AmoebaBoss`, `AmoebaBarrage`, Weapon, Enemy) y cuerpos hijos `AmoebaBody` (Body, Half_A, Half_B) con rb cinemático y Cast contra la capa Default. Nuevo: `IBoss`/`ActiveBoss`/`BossBarStyle`, `GameState.Cinematic`, `UI/CinematicOverlay`, `Player/CameraZoom` (sacado de LineageEvolution), foco en `CameraLookAhead`, `Projectile.DespawnAll`, `IStatusAnchor` en StatusMarkers, `FloorConfig.BossEntry[] bosses` + `FloorLayout.BossChoice`, `RoomLayouts.BossCircle` (32×34). Menú **Primordia > Boss > Crear ameba gigante y escena de prueba** (`Editor/AmoebaBossSetup.cs`, `ArtSetup.BossAmoeba.cs`), escena Test_BossAmoeba. Arte generado: `docs/multimedia-temp/enemies/BossAmoeba/generar_ameba_gigante.py` y `environment/generar_tema_ameba.py` (tema Amoeba azul marino). Pruebas en `AmoebaBossTests`.

**Arreglos tras la 2.ª prueba (2026-10-22):** no se movía en calma porque startDelay (1,5) tapaba el aviso y salía con dirección cero; ahora el retraso es el propio aviso (startDelay 0,6) y hay protección. Se atascaba en esquinas porque cualquier roce contaba como choque: ahora resbala si el ángulo con la pared es < 35° (máx. 3 veces por embestida), `FreeDirectionNear` gira hasta 180° buscando hueco y `AmoebaBody.ResolveOverlap` la saca de la pared.

**3.ª prueba (2026-10-23):** la "mini embestida" al recibir golpes era visual (Hurt + Angry tapaban el aviso): ahora avisando o embistiendo solo hay destello rojo (`AmoebaBody.Flash`). Medianas: el usuario eligió separarse al nacer (salen disparadas a lados opuestos, 2.ª con 0,45 s más de parada) y papeles (Half_B `Predicts`: `AmoebaBoss.PredictTarget`, adelanto máx. 1,2 s). Descartó turnos y "no cruzarse". Test_BossAmoeba y Test_Lineages usaban Floor_1 por un asset descargado al abrir la escena: arreglado.

**Fallo grave encontrado 2026-10-23:** `AmoebaBody.Move` daba "bloqueado" sin obstáculo (comparaba `free < distance + Skin` y el redondeo lo hacía verdadero), resbalaba 3 veces con normal cero y se paraba: por eso las medianas se frenaban enseguida. Ahora bloqueado = `LastHit != null`. Queda `AmoebaBoss.logCharges` (inspector) para depurar choques.
