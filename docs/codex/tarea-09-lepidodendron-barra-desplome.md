# Tarea 09 — Lepidodendron ancestral, parte 5a: barra, presentación y desplome (L5a)

- **Rama:** `codex/lepido-5a-desplome` (desde `codex/lepido-4-ataques` una vez hecha la tarea 08; si 08 no está hecha, desde `codex/lepido-3-conos`)
- **Escrita por:** Claude (2026-11-13) · **Implementa:** Codex
- **Estado:** pendiente
- **Plan completo:** `docs/codex/lepidodendron-plan.md`. Haz **solo esta** tarea. La recompensa, el tocón interior y la reliquia son la tarea 10.

## Objetivo
Cerrar el combate: el jefe tiene su **barra de vida propia** (rama con savia, nudos y conos), una **presentación** («Corazón del bosque», sello «JEFE SECRETO») con la cámara recorriendo el pasillo, y al romper los 3 conos el árbol **se desploma** en una cinemática (~6,6 s) y el tronco tendido sustituye para siempre a la pared norte. Arte aprobado: solo importar y programar.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`: secciones **«Barra de vida»**, **«Presentación»** y **«Desplome»** (hojas, celdas, pivotes, ms, posiciones respecto al pie del tronco, colisión del tronco tendido, qué cuadro es el golpe).
- Barra: `docs/codex/tarea-01-barra-meganeura.md` (+ su `Resultado`) y `Editor/ArtSetup.BossMeganeuraBar.cs`, `Scripts/UI/BossBarSkin.cs`, `BossBarAppearance.cs`, `BossHealthBar.cs`. La barra del Lepidodendron es **un caso nuevo**: tiene **3 nudos (75/50/25 %)** que revientan, **cabeza** animada (calma/final/golpe) y **3 conos colgando** bajo el último tramo con estado propio. Amplía el sistema de skins sin romper Meganeura/Euriptérido/Anomalocaris (`BossHealthBarTests` en verde); si necesitas una clase nueva para los conos de la barra, hazla pequeña y propia del skin.
- Presentación: `Scripts/UI/BossIntroBanner.cs`, `CinematicOverlay.cs` y cómo lo usa la Meganeura / Euriptérido (retrato + título); los PNG están en `docs/multimedia-temp/boss-intros/` (`Portraits/Portrait_Lepidodendron.png`, `Titles/Title_Lepidodendron.png`; el sello morado va en el título, ver README). Si el sistema actual no admite un título de dos líneas o un sello distinto, amplíalo mínimamente.
- Desplome: `LepidodendronBoss.Cones.cs` (`ConesDestroyed`, el jefe queda en 0 **sin `Died`**: la L3 lo dejó así a propósito para esta tarea), `LepidodendronBoss.Scenery.cs`, `Editor/ArtSetup.BossLepidodendron.cs`, `docs/codex/tarea-02-muerte-meganeura.md` (+ `Resultado`) para el estilo de una muerte con puesta en escena, y lo que ya haga la cámara en las cinemáticas de otros jefes (`CinematicOverlay`).
- Lección de la tarea 04: lo creado por código se destruye con el objeto.

## Qué hacer
1. **Importar** `BossBar/UI_BossBar_Lepidodendron_*`, `Death/*` (18 hojas) y la presentación. Si falta algo, **para y avisa**.
2. **Barra.** Skin propio `BossBar_Lepidodendron.asset` usado por el jefe, anclada **abajo y centrada**, marco 320×44, ventana 260×8 en (46, 13) rellena de izquierda a derecha, **savia** `Fill_Sap` (paredes) → `Fill_SapGold` (fase final); marco `Frame` → `Frame_Final`; cabeza `Head_Calm` → `Head_Final`, con `Head_Hit` al recibir daño; `Fill_Trail` fantasma. **Nudos** `Knot` en el 75/50/25 % (x = 241/176/111 del marco, en (muesca − 6, 7)): destellan con `Knot_Hit` al golpear **un nudo de la pared en curso**, y al **caer cada pared** revientan con `Fx_KnotBurst` (muesca − 16, −3) y se quedan `Knot_Broken`. **Conos:** `Cone_Appear` al empezar la fase final (x = 62/80/98, y = 24) y luego `Cone_Armor` / `Cone_Charred` / `Cone_Exposed` / `Cone_Broken` según el estado real de cada cono (leer del `LepidodendronCone`). La vida mostrada es la del jefe (600 → 0).
3. **Presentación** al entrar en la sala (patrón de los jefes anteriores): la cámara **recorre el pasillo** desde la entrada hasta el árbol (~1,5 s, se ven las 3 paredes), allí `Lepido_Veins_Warn`, `Canopy_Arena_Shake` (la copa se sacude) y las paredes se estremecen un instante; sale el rótulo (retrato + «LEPIDODENDRON / ANCESTRAL», título «Corazón del bosque» y sello «JEFE SECRETO» morado) y la cámara vuelve al jugador. El combate **no empieza** hasta que acaba (el jugador no recibe daño ni se mueve). Solo se muestra **la primera vez** que se entra a la sala de esa partida (provisional: una vez por escena de prueba).
4. **Desplome** (`LepidodendronBoss.Death.cs`, partial nuevo), cuando se emite `ConesDestroyed`:
   - El jugador queda **quieto e invulnerable** durante la cinemática (~6,6 s); se limpia lo vivo (enemigos, oleadas, ataques de L4, odres, fuego); se paran las vetas del tronco (parpadean y se apagan).
   - **Cámara:** espera a que el tronco se incline ~48° y entonces lo **sigue hacia el oeste (izquierda)** y vuelve al jugador al acabar (README: cuadro y tiempos).
   - Secuencia según README: `Death_Veins` (parpadeo hasta apagar) → `Death_Crack` (sustituye a `Lepido_Trunk`; grieta de derecha a izquierda) → `Death_Fall` (15 cuadros 1056×416, pivote (944, 352); **el golpe es el cuadro 12**) con `Fx_FallShadow` (única señal: la sombra se alarga y se oscurece) → `Fx_TrunkSplinters`, `Fx_CrownCrash`, `Fx_TrunkDust` al golpear; la copa se rasga (`Canopy_Arena_Tear` → `Canopy_Arena_Torn_Sway`), `Fx_GoldColumn` (columna de luz) queda sobre el tocón; `WallRubble` donde cae.
   - **Estado final** (para siempre): `Death_FallenTrunk` tendido **sustituye a la pared norte** desde el tocón hasta x ≈ 98 con **colisión** (posiciones y colisión exactas en el README «Desplome»); `Stump_Break` → `Stump_Open`/`Stump_Idle` (tocón macizo con la **grieta de luz** en la cara sur; la interacción **E** es de la tarea 10, déjala solo como punto de anclaje o `Transform` vacío `StumpCrack`); setas apagadas `Shroom*_Off` salvo las del tocón; la arena más clara.
   - **El jefe muere de verdad al terminar la cinemática**: ahora sí dispara `Died` (la L3 lo evitó; usa `Health.Drain(allowZero)` o el mecanismo que ya dejó) **o**, si eso rompe la sala (cierra puertas, recompensa del `MutationDrop`, etc.), emite un evento propio `Collapsed` y mantén lo menos invasivo; describe qué hiciste en `Dudas`. **No debe aparecer la recompensa estándar de jefe** (`MutationDrop`): la recompensa del Lepidodendron es la de la tarea 10.
   - La nube de esporas moradas es solo ambiente.
5. **Teclas de prueba** (en `DebugCheats`, libres, documentadas en `Resultado`): forzar el desplome ya, repetir la presentación, bajar la vida a 25 % / 1 %.
6. **Pruebas** en `LepidodendronBossTests` (y `BossHealthBarTests` si amplías el skin): (a) la barra usa el skin, está abajo y muestra savia/cabeza de calma en paredes y dorada en la fase final; (b) cada pared que cae rompe su nudo y no antes; los 3 conos de la barra reflejan el estado real (armadura/quemada/expuesto/roto); (c) la presentación no empieza el combate hasta acabar y solo se muestra una vez; (d) el desplome empieza al emitirse `ConesDestroyed`, dura ~6,6 s, el jugador no recibe daño ni se mueve y la cámara vuelve; (e) tras el desplome existe el tronco tendido con colisión donde estaba la pared norte, la pared norte original ya no bloquea ni se solapa, y el tocón tiene el ancla `StumpCrack`; (f) no aparece `MutationDrop`; (g) sin fugas al destruir la sala en mitad de la cinemática; (h) **no se rompen** L1-L4 ni `MeganeuraBossTests`/`BossHealthBarTests`.

## Números y reglas (cerrados)
- Cinemática ~6,6 s (constante provisional); caída arranca casi suspendida y acelera (el arte ya lo trae).
- Vida total 600; barra 600 → 0; nudos en 75/50/25 %.
- El tronco tendido es permanente en esa partida; la mitad sur de arena y pasillo queda libre para volver.
- Presentación una vez por partida/escena de prueba (provisional).

## Archivos que puede tocar
- `Scripts/Enemies/LepidodendronBoss*.cs` (nuevo `.Death.cs`, y `.Intro.cs` si lo prefieres), `Scripts/UI/BossBarSkin.cs`, `BossBarAppearance.cs`, `BossHealthBar.cs`, `BossIntroBanner.cs` (solo ampliaciones compatibles), `Editor/ArtSetup.BossLepidodendron.cs`, nuevo `Editor/ArtSetup.BossLepidodendronBar.cs`, `Editor/LepidodendronBossSetup.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Solo lo imprescindible y justificado en `Rooms/`/`Core/` — avísalo en `Resultado`.

## Fuera de alcance (NO hacer)
- No dibujar ni tocar PNG ni `.py`. Nada de recompensa, corazón, Espora, reliquia, sala interior del tocón ni su fundido (tarea 10). No enganchar al piso (`Floor3.asset`, puerta secreta). No tocar otros jefes salvo ampliar el skin sin romperlos.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba`; `LepidodendronBossTests`, `MeganeuraBossTests`, `BossHealthBarTests`.
- A ojo (usuaria): la barra durante todo el combate, la presentación, la cinemática (¿se ve la caída?, ¿la cámara la sigue?), el tronco tendido como pared norte y que el tocón queda con la luz.

## Resultado (lo rellena Codex)
- Hecho:
