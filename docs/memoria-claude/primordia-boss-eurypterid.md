---
name: primordia-boss-eurypterid
description: "SIGUIENTE módulo (desde 2026-10-26): jefe Euriptérido en feature/boss-eurypterid; qué existe ya, el diseño, el plan por pasos y la decisión de la barra propia dibujada"
metadata:
  node_type: memory
  type: project
  originSessionId: a6c7aea5-90de-4478-8958-7597eb766b7c
  modified: 2026-10-27T02:06:32.896Z
---

**Estado (2026-10-26, fin de sesión):** no hay nada empezado. La rama `feature/boss-eurypterid` **todavía no existe**: crearla desde `main` (merge 1086049, con el Anomalocaris ya dentro). Después viene `feature/floor2-audio`.

## Qué existe ya
- **Arte del jefe aprobado** (2026-10-24) en `docs/multimedia-temp/floor2-bosses/Eurypterid/`, hecho con `generar_euripterido.py` + `base_piso2.py`. El lienzo es de 128 y el cuerpo mide unos 90 px. Hojas: Walk, PincerWarn, PincerStrike, SpineWarn, SpineShoot, Hurt, Molt (12 cuadros, 3 s; **al final está 1 u más a la derecha**: la Exuvia se crea en la posición original y el jefe se mueve 1 u hacia delante), Exuvia, Walk_Soft, TailWarn, TailSweep, DoublePincer (daño en los cuadros 1 y 3), Hurt_Soft, Death e Intro. Efectos en `Effects/`: Proj_EurypteridSpine (16×8) y Fx_Eurypterid_TailSweep (128). Los ritmos y los cuadros que cuentan están en el README de floor2-bosses.
- **Presentación:** el arte está en boss-intros. Falta añadirlo a `BossIntros` en ArtSetup.BossIntro.cs (como se hizo con el Anomalocaris en `ArtSetup.ImportBossIntro("Anomalocaris")`).
- **Sala:** `Room_Boss_Eurypterid` existe en Floor2Setup (`BossRooms`, con prefab `null`, así que ahora pelea en ella un jefe provisional del piso 1). El plano tiene rocas `R` que se derrumban, todavía sin efecto ([[primordia-floor2-structure]]).

## Diseño (GDD, sección "Euriptérido")
300 de vida. Cueva de unos 30×22 con 5–6 rocas de cobertura y grietas solo en los bordes. **Acorazado (100–50 %):** pinzas con aviso si estás cerca; abanico de 3 púas venenosas que salen del caparazón (no de la cola, la opción B que eligió el usuario); su coraza reduce a la mitad el daño de frente, como el Trilobite. **Muda (50 %, unos 3 s, inmune):** franjas de cine; la Exuvia queda como cobertura y bloquea proyectiles de los dos bandos; se derrumban 2 rocas. **Blando (50–0 %):** recibe un 25 % más de daño y va el doble de rápido; barrido de cola de 180° con estela venenosa y aviso (la punta se enciende en verde); doble pinza. Detalles en [[primordia-floor2-design]].

## Plan (el mismo esquema que el Anomalocaris, ver [[primordia-boss-anomalocaris]])
1. Sala: rocas de cobertura, grietas y derrumbe de las 2 rocas.
2. El jefe en la fase acorazada: pinzas, púas y frente que reduce el daño.
3. La escena de la muda (con la Exuvia como cobertura) y la fase blanda con la cola.
4. **Barra de vida propia dibujada.** El usuario lo decidió el 2026-10-26: sí, como la del Anomalocaris.
5. Presentación, menú `Primordia > Boss > Crear Euriptérido y escena de prueba` (Test_BossEurypterid), pruebas, GDD, PLAN, documento del piso 2 ([[primordia-floor-docs]]), merge y push.

## Barra propia: cómo hacerla
- Se dibuja con la skill [[primordia-sprites-skill]]. Un generador nuevo, `floor2-bosses/generar_barra_euripterido.py`, tomando como modelo `generar_barra_anomalocaris.py` (las hojas `UI_BossBar_Eurypterid_*` en `BossBar/`).
- El código ya sirve: hay que crear un `BossBarSkin` (Data/Bosses/BossBar_Eurypterid.asset) con un `ArtSetup.CreateEurypteridBarSkin` igual que `CreateAnomalocarisBarSkin`, añadir `BossBarAppearance` al prefab e implementar `IPhasedBoss` (umbral 0,5, una sola muesca; fase 0 acorazado, fase 1 desde la muda). Mientras está en `BossBarStyle.Immune` (la muda) usa los sprites `dark`/`FrameDark`.
- **Antes de dibujar, preguntar al usuario las decisiones visibles**: cómo es el marco (lo natural sería el propio escorpión visto desde arriba, como el Anomalocaris: pinzas a la izquierda, segmentos a lo largo, cola con púa a la derecha), qué se ve durante la muda (en vez de silueta, quizá el caparazón agrietándose o la piel rosada) y los colores por fase (fase 1 acorazada en tonos de su caparazón, fase 2 blanda en rosa). Al Anomalocaris le pidió una cabeza que "dé miedo" (nada redondo): conviene que este también sea amenazante desde el primer boceto.
- Enseñarle la vista previa y esperar su aprobación antes de programar ([[primordia-workflow]]).

**How to apply:** al empezar la sesión, crear la rama y proponer el paso 1 (o preguntar por dónde quiere empezar). La barra va en el paso 4, pero sus preguntas de diseño se pueden hacer antes si el usuario quiere.
