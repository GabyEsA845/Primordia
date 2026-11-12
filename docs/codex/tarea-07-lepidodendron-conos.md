# Tarea 07 — Lepidodendron ancestral, parte 3: fase final con los 3 conos (L3)

- **Rama:** `codex/lepido-3-conos` (desde `feature/floor-3`, que ya incluye L1 y L2)
- **Escrita por:** Claude (2026-11-12) · **Implementa:** Codex
- **Estado:** por implementar
- **Plan completo:** `docs/codex/lepidodendron-plan.md`. Haz **solo esta** tarea.

## Objetivo
Cuando cae la tercera pared (`FinalPhaseReady`, ya existe) empieza la **fase final**: el jefe tiene 150 de vida = 3 conos × 50. Los 3 conos, envueltos en una armadura de ramas, cuelgan fuera de la vista; cada cierto tiempo el árbol **baja uno para golpear al jugador** (aviso → baja → golpe → ventana en el suelo → sube). El jugador rompe la armadura y daña el cono durante las ventanas; **el daño se acumula entre bajadas** (nada se cura). Un cono roto se queda en el suelo y no vuelve a atacar. Al romper los 3, el jefe llega a 0 y la fase termina (el desplome y la recompensa son de la L5). El arte está aprobado e importado; solo hay que programarlo y montarlo. La sala, el árbol, las 3 paredes con vida/fuego/oleadas, la escena `Test_BossLepidodendron` y sus teclas **ya existen**: amplíalas, no las rehagas.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`: sección **«Conos con armadura»** (hojas, celdas, pivotes, ms, flujo) y, en las tablas del tronco/copa, `Lepido_Veins_Warn`, `Lepido_Veins_Hit`, `Canopy_Arena_Sway/Shake`, `Lepido_Shroom*_Warn`. **Respeta sus tiempos de animación y los «cuadros que cuentan».**
- Código ya hecho, para imitar: `Scripts/Enemies/LepidodendronWall.cs` (una vida con `DamageGate`, hitbox en capa Enemy, `LastImpactPoint`, `owner.LimitWallDamage`, `WallLostHealth`), `LepidodendronBoss.Walls.cs` (`FinalPhaseReady`, limitador compartido), `LepidodendronBossWallIgnition.cs` (`IFlammable`), `LepidodendronBoss.Scenery.cs` (tronco, vetas, capa de hojas, hongos), `Editor/LepidodendronBossSetup.cs`, `Editor/ArtSetup.BossLepidodendron.cs` (añade las hojas de `Cones/` que falten), `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Para el fuego y el metano: `Scripts/Rooms/IFlammable.cs`, `Flammables.cs`, `MethaneSac.cs`; para la sacudida y la translucidez, lo que ya haga L1 con la capa de hojas y el pie del tronco.
- Lección de la tarea 04: lo que crees por código (texturas, sprites, mallas) **se destruye** al terminar o al destruirse el objeto.

## Qué hacer
1. **Los 3 conos.** Tres objetos `LepidodendronCone` (uno por rama, en la copa; fuera de la vista mientras cuelgan, **no atacan ni reciben daño** colgados). Cada uno tiene: **armadura 30** de resistencia (estados 0 / 1 / 2 a 0-10 / 10-20 / 20-30 de daño acumulado; al llegar a 30 se rompe) y **vida 50** una vez expuesto. Guarda armadura (estado y daño), quemada sí/no y vida **entre bajadas**.
2. **Vida del jefe.** Al empezar la fase la vida del jefe es 150 (ya lo deja L2). **Solo el daño a la vida de un cono expuesto resta del jefe** (patrón `WallLostHealth`); el daño a la armadura no. Al romperse los 3 conos el jefe queda en 0: emite `ConesDestroyed` (evento/propiedad pública para la L5) y **no mates ni destruyas nada más** (si `Health` en 0 dispara `Died` y rompe la sala, mantén lo menos invasivo y dilo en `Dudas`). El daño de golpes y fuego pasa por el `BossDamageLimiter` del jefe, como en las paredes.
3. **Ciclo de un cono** (constantes al inicio, provisionales). Orden de bajada: rota entre los conos vivos.
   - **Aviso 1,5 s:** `Lepido_Veins_Warn` (8×70 ms) + hongos `Shroom*_Warn` + sacudida de la copa (`Canopy_Arena_Shake`, ~1 s); después `Fx_ConeShadow` crece 0,72 s en el punto de caída (8×90 ms). **El punto se fija al empezar la sombra** (hasta entonces sigue al jugador), recortado a la arena y a ≥ 2 u de las paredes; dentro de esos 0,72 s el jugador puede salir.
   - **Bajada 0,35 s:** el cono baja acelerando por la rama (`Lepido_ConeBranch_Tile` repetido en vertical hasta la copa) y **golpea**: `Fx_ConeSlam` (7×70 ms), `Fx_ConeCrater`, rebote de 2 px en código. **Daño al jugador: 2**, **solo en el cuadro 0 de Fx_ConeSlam**, radio ~1,6 u, **una vez**. La nube/onda posteriores no hacen daño.
   - **Ventana en el suelo 8 s:** el cono queda en el suelo (`Fx_ConeShadow` cuadro 4 de sombra de contacto); ahora recibe daño. Con armadura: cada golpe `Lepido_Cone_Armor{s}_Hurt` (2×60 ms) + `Fx_ArmorChip` en el punto del golpe; el estado sube 0→1→2; al romperse, `Lepido_Cone_Shatter` (8×90 ms, el cono ya recibe daño desde el cuadro 0) y queda `Exposed_Idle`. Expuesto: cada golpe `Exposed_Hurt` + `Lepido_Veins_Hit`; a vida 0, `Lepido_Cone_Burst` (10×80 ms, estalla en el cuadro 3, sin daño), queda `Lepido_Cone_Husk` y la rama sube sola (`ConeBranch_Torn`).
   - **Subida 1 s** si la ventana acaba sin que haya muerto: tiembla, `Fx_ConeLift` y sube. **Sube con el estado que lleva** (armadura rota a la mitad, expuesto, quemada…) para que se vea el progreso; **no uses las hojas `Regrow`** (el daño no se cura). Al bajar de nuevo lo hace con ese mismo estado.
   - **Pausa 4 s** entre bajadas (desde que el cono anterior termina de subir o muere). **Cuando muere el primer cono, la pausa baja a 3 s y puede haber hasta 2 conos bajados a la vez.** Un cono solo baja si no ha muerto.
4. **Daño a la armadura y al cono.** Hitbox del cono = una `Health` única con `DamageGate` (como la pared), en capa Enemy, **activa solo mientras está en el suelo**; usa `LastImpactPoint` para el chip. La **armadura quemada recibe ×2**: el ×2 se aplica a la propia resistencia (un golpe de 10 a una armadura quemada descuenta 20 de sus 30 puntos). La armadura no es la vida del jefe, así que no hay nada más que multiplicar. El cono expuesto recibe daño completo.
5. **Fuego.** Mientras el cono está en el suelo con armadura sin romper y no quemada, es inflamable (`IFlammable`, un punto en su base y otro a media altura). Al prender: `Lepido_Cone_Burn{s}` (8×150 ms), termina en `ArmorCharred{s}` (con `Fx_RootSmolder` si encaja). **Quemada no rebrota**: sube y baja siempre con `ArmorCharred{s}`. Solo arde la armadura (la rama no). El fuego **no hace daño propio** a la armadura ni al cono: solo la carboniza (el ×2 es la ventaja). Con la armadura rota (expuesto) ya no arde.
6. **Metano junto al tronco.** Coloca **3 `MethaneSac`** alrededor del pie del tronco, a 7-8 u de su eje, repartidos y fuera del paso inmediato (posiciones provisionales; no los repongas). Si uno explota o arde **a ≤ 5 u del eje del tronco**: sacudida de la copa (`Canopy_Arena_Shake`) y **bajan a la vez todos los conos vivos que no estén ya bajados**, con aviso de 1 s (no 1,5 s) y cada uno sobre el jugador separados ≥ 3 u entre sí. Cada cono baja con su ciclo normal desde ahí. Un odre solo sirve una vez; si sus conos ya están abajo, no hace nada.
7. **Tronco.** Durante la fase final el árbol sigue sin recibir daño propio salvo esto: un golpe al tronco hace **20 %** del daño (−80 %) y se aplica **como daño a la vida del cono vivo con más vida restante**, ignorando su armadura y sin importar si cuelga o está abajo (así la vida del jefe sigue siendo siempre la suma de la vida de los conos). Pasa por el limitador. Esta regla es provisional: si te parece rara al implementarla, hazla y dilo en `Dudas`.
8. **Cono roto y colisión.** Cono en el suelo y `Husk`: colisión **solo en la base**, ordenado por altura con el jugador, y **translúcido (~50 %) mientras el jugador está detrás** (reutiliza lo que ya hace L1 con el pie del tronco si sirve). Los conos rotos se quedan hasta el final.
9. **Barra.** Se sigue usando la barra estándar de L2 (la propia con nudos y conos es de la L5).
10. **Teclas de prueba** (añádelas a `DebugCheats`, elige teclas libres y documéntalas en `Resultado`): saltar a la fase final (tirar las paredes que queden), bajar ya un cono, dar 10 de daño a la armadura del cono bajado, prender el cono bajado, romper el cono bajado, explotar el metano junto al tronco.
11. **Pruebas** en `LepidodendronBossTests`: (a) al empezar la fase hay 3 conos con armadura 30/vida 50 y el jefe en 150; (b) el aviso dura 1,5 s y el punto de caída se fija al empezar la sombra; (c) el daño del golpe es 2, una sola vez, solo dentro del radio; (d) la armadura pasa 0→1→2→rotura por daño acumulado y **no se cura al subir ni al bajar otra vez**; (e) la ventana dura 8 s y el cono sube si no murió, con su estado; (f) la armadura quemada recibe ×2, no rebrota y el fuego no hace daño propio; (g) solo el daño a la vida del cono resta del jefe, y al morir los 3 el jefe queda en 0 y se emite `ConesDestroyed`; (h) cono muerto = `Husk`, no vuelve a bajar; pausa 4 s y, tras el primer cono muerto, 3 s y hasta 2 conos a la vez; (i) el metano a ≤ 5 u del tronco baja todos los conos vivos libres con aviso de 1 s, y a más distancia no hace nada; (j) el tronco al −80 % daña al cono con más vida y no a la armadura; (k) colgados no reciben daño ni atacan; (l) sin fugas al destruir la sala; (m) **no se rompe** nada de L1/L2 (siguen verdes sus pruebas).

## Números y reglas (cerrados con la usuaria 2026-11-12; los marcados «provisional» se ajustan probando)
- Vida: **3 conos × 50 = 150** (cierre del jefe). Armadura **30** (provisional); quemada ×2 sobre la resistencia (golpe de 10 = 20 descontados).
- Daño al jugador: **golpe de cono 2**. (Latigazo 2, corro 2 y esporas 1 son de la L4.)
- Ciclo: aviso **1,5 s**, bajada **0,35 s**, ventana **8 s**, subida **1 s**, pausa **4 s** (3 s y 2 a la vez tras el primer cono muerto). Todo provisional.
- El daño de armadura y cono **se acumula entre bajadas** (decidido); la armadura quemada no rebrota.
- Metano junto al tronco: baja todos los conos vivos libres, aviso 1 s.
- Constantes al inicio del archivo, con nombre y marcadas provisionales.

## Archivos que puede tocar
- `Scripts/Enemies/LepidodendronBoss*.cs` (crea `LepidodendronBoss.Cones.cs` y `LepidodendronCone.cs`), `Editor/LepidodendronBossSetup.cs`, `Editor/ArtSetup.BossLepidodendron.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Solo lo imprescindible y justificado en `Rooms/`/`Core/` — avísalo en `Resultado`.
- Se regeneran con el menú: prefab, escena de prueba y datos del jefe.

## Fuera de alcance (NO hacer)
- No dibujar ni tocar PNG ni los `.py`. Si falta una hoja de `Cones/`, **para y avisa**.
- Nada de latigazos, raíces bajo el jugador, esporas, enemigos por las esquinas (L4), barra propia, presentación, desplome ni recompensa (L5).
- No tocar la Meganeura, otros jefes, el `Floor3.asset` real ni la puerta secreta. No cambies el comportamiento de las paredes de L2.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba` y después `LepidodendronBossTests`, `MeganeuraBossTests` y `BossHealthBarTests`. Fallos que ya existían: ver `AGENTS.md`.
- A ojo (usuaria), `Test_BossLepidodendron`: salta a la fase final; mira el aviso (¿se ve el sitio de caída?, ¿da tiempo a salir?), el golpe, la ventana de 8 s (¿cuántas bajadas hacen falta por cono?), que la armadura no se cure al subir, el fuego (¿se nota el ×2?), el metano junto al tronco, la translucidez del cono con el jugador detrás y que al romper los 3 el jefe quede en 0 sin romper la sala.

## Resultado
(Codex rellena: qué hizo, archivos, teclas de prueba, pruebas lanzadas y resultado, `Dudas`.)
