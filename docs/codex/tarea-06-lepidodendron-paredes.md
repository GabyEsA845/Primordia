# Tarea 06 — Lepidodendron ancestral, parte 2: paredes de raíces y oleadas (L2)

- **Rama:** `codex/lepido-2-paredes` (desde `feature/floor-3`, que ya incluye la L1)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** pendiente (SIN dar a Codex)
- **Plan completo:** `docs/codex/lepidodendron-plan.md`. Haz **solo esta** tarea.

## Objetivo
Que las 3 paredes de raíces del pasillo **se puedan romper**: reciben daño (dañar la pared = dañar al jefe), cambian de aspecto con el daño, tienen nudos débiles, **arden** (frente de fuego, carbonizado, más daño) y al llegar a cero se **retiran** (o se desmoronan en ceniza si están totalmente carbonizadas) dejando el paso libre. Cada pared lanza **oleadas** de enemigos y tiene **odres y helechos** al lado para que el fuego sea el camino rápido. Cuando cae la tercera pared el jefe queda en 150 de vida y la fase final (L3) queda lista para engancharse. El arte está aprobado e importado; solo hay que programarlo y montarlo. La sala, el árbol, las paredes quietas, la escena `Test_BossLepidodendron` y las teclas de prueba **ya existen** (L1): amplíalas, no las rehagas.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`: sección **«Paredes de raíces»** (hojas, celdas, pivotes, ms, qué hace cada una, cómo avanza el fuego) y el resumen «Combate». **Respeta sus tiempos y reglas tal cual.**
- Código ya hecho: `Scripts/Enemies/LepidodendronBoss.cs` y `LepidodendronBoss.Scenery.cs` (`BuildWalls`, `WallX/WallThickness`, `KnotCrops`, `Piece`), `Editor/LepidodendronBossSetup.cs`, `Editor/ArtSetup.BossLepidodendron.cs` (qué hojas están ya cortadas; añade las que falten de `Walls/` y `Decor`), `Tests/PlayMode/LepidodendronBossTests.cs`.
- Para imitar: `Scripts/Combat/BossDamageLimiter.cs` y su uso en `Editor/MeganeuraBossSetup.cs` (~l.197); `Scripts/Enemies/MeganeuraBoss.Phase3.cs` (`BossSigillaria`, cómo reacciona al fuego/rayo); `Scripts/Rooms/IFlammable.cs`, `Flammables.cs`, `GroundFire.cs`, `Fern.cs`, `MethaneSac.cs`; `Scripts/Core/Health.cs` (`DamageSource`, `DamageGate`); `Scripts/Core/DebugCheats.cs` (teclas de prueba de la L1); enemigos en `Data/Enemies/Floor3/` y `Prefabs/Enemies/` (Calamites, Toad, Spider, Beetle, Dragonfly).
- Lección de la tarea 04: lo que crees por código (texturas, sprites, mallas) **se destruye** al terminar o al destruirse el objeto.

## Qué hacer
1. **Vida.** Vida total del jefe **600** (`MaxHealth`). Cada pared = **150** (25 %). El tronco/jefe sigue invulnerable a golpes directos; **solo las paredes reciben daño** y cada punto de daño que absorbe una pared **se resta de la vida del jefe** (así la barra baja). Cada pared tiene su propia vida (un `Health` o equivalente en su collider, para que todas las armas del jugador la dañen sin código extra) y su daño pasa por un `BossDamageLimiter` del jefe (mismos topes que la Meganeura; añade el componente en `LepidodendronBossSetup`). Al terminar las 3, el jefe tiene 150 y queda en un estado `FinalPhaseReady` (evento/propiedad pública para la L3); no hagas nada más con él.
2. **Estados de daño.** Según la vida de la pared: D0 (>75 %), D1 (>50 %), D2 (>25 %), D3 (≤25 %), sustituyendo el sprite de `Lepido_Wall{n}_D0`. Cada golpe reproduce `Lepido_Wall{n}_Hurt_D{estado}` (2 cuadros, 60 ms) encima del estado actual.
3. **Nudos.** Cada nudo (los recortes de `KnotCrops`, ya colocados con su `Idle` en la L1) tiene una zona de golpe: un golpe que cae en su zona hace **×2 de daño a la pared** y reproduce `Knot{j}_Hit` (2 cuadros, 60 ms). Cada nudo aguanta **22 puntos de daño sin multiplicar** (≈15 % de la pared, provisional): al agotarlos **revienta**: `Fx_KnotBurst` (6 cuadros, 70 ms, 64×64 centrado en el nudo) y se queda el parche `Knot{j}_Wound` (o `_WoundCharred` si esa franja está carbonizada) en lugar del `Idle`. Un nudo reventado ya no da ×2.
4. **Fuego.** Cada pared es inflamable: como `IFlammable` mide 18 u de alto y `Flammables` busca por posición, reparte la pared en **segmentos inflamables** (p. ej. uno cada 3 u de alto) cuyo `Ignite()` empieza el fuego en esa altura. Cuando prende (fuego en el suelo, cono del Escarabajo, explosión de metano, ataque de fuego, helecho que arde al lado...):
   - El frente avanza **hacia el norte y el sur desde donde prendió**, ~3 s hasta cubrir la pared. Detrás del frente, la pared pasa a las versiones `Charred` por **franjas horizontales** (usa recortes de sprite o `SpriteMask`; algo simple, sin regenerar texturas cada cuadro). En el frente, `Fx_GroundFire` a lo ancho con **su base en la línea donde pasa a carbonizada**; detrás, `Fx_RootSmolder` (bucle) sobre lo carbonizado.
   - **Daño del fuego: 12 por segundo** a la pared mientras arda (provisional), **no sujeto al tope de quemadura** del `BossDamageLimiter` (el fuego es la vía rápida a propósito).
   - Lo **carbonizado recibe ×2** de los golpes (y se suma al ×2 de un nudo si coinciden).
   - Con lluvia o sin ella no cambia nada: en esta sala no llueve.
5. **Caída de la pared (vida 0).** El collider desaparece al empezar la animación (se puede pasar por el hueco):
   - Si **no está totalmente carbonizada**: sacudida ~0,3 s (±2 px) → `Lepido_Wall{n}_Retract` (8 cuadros, 90 ms) → queda `Lepido_Wall{n}_Scar` (se puede pisar).
   - Si está **totalmente carbonizada** (todas las franjas): `Lepido_Wall{n}_Crumble` (8 cuadros, 90 ms) → queda `Lepido_Wall{n}_AshScar`.
   - Quitar los nudos, llamas y brasas que queden.
6. **Oleadas.** Cada pared lanza sus oleadas **la primera vez que recibe daño**: 3 oleadas con **12 s** entre ellas, tope de **6 enemigos vivos** a la vez en la sala (no se lanza oleada si ya hay 6; espera). Aparecen en el tramo de pasillo **delante de la pared** (lado oeste, entre la pared anterior —o la entrada— y esa pared), a ≥ 5 u del jugador y sobre suelo libre, con un destello/aparición sencillos (reutiliza lo que ya tengan los enemigos del piso). Se detienen cuando la pared cae; los enemigos que queden siguen vivos. Tamaño y composición (**provisionales**, constantes al inicio):
   - Pared 1: 3 / 4 / 4 enemigos de **Calamites y Toad** (sapo). *Si el sapo solo funciona sobre lodo, no inventes: usa solo Calamites en esa pared y dilo en `Dudas`.*
   - Pared 2: 3 / 4 / 4 de **Spider y Beetle** (el escarabajo es el «bombardero»: su cono debe poder prender la pared; es lo esperado).
   - Pared 3: 4 / 4 / 4 mezcla de las anteriores **más Dragonfly**.
   Usa el sistema de aparición/`EnemyBrain` que ya usan las salas de combate del piso 3; no escribas IA nueva.
7. **Odres y helechos.** Junto a cada pared, en el lado oeste, a 2-3 u de ella y repartidos a lo ancho del pasillo: **2 `MethaneSac` y 3 `Fern`** por pared (posiciones provisionales; no tapes el paso ni el punto de aparición del jugador). Son decorado con función: si el jugador los prende, el fuego llega a la pared. Reutiliza los prefabs/`FernSpec` que ya existen.
8. **Barra de vida.** Para poder ver el progreso, el jefe registra su **barra estándar** (la lisa de los otros jefes) al empezar el combate: comienza cuando el jugador entra en la sala; la barra propia con nudos es de la L5. Nombre «Lepidodendron ancestral».
9. **Teclas de prueba** (en `DebugCheats`, solo editor, junto a las de la L1; comprueba que no choquen): **Mayús+F10** hace 25 de daño a la pared más cercana; **Mayús+F11** rompe de golpe la pared más cercana (cae según su estado de fuego); además, **F12** (que ya prende lo inflamable cercano) debe poder prender la pared y **Mayús+F12** (fuego en el suelo) también. Documéntalas en `Resultado`.
10. **Pruebas** en `LepidodendronBossTests`: (a) vida 600 y paredes de 150; daño a una pared resta lo mismo al jefe; (b) los 4 estados de daño según la vida; (c) golpe en un nudo hace ×2 y el nudo revienta a los 22 sin multiplicar, y deja de dar ×2; (d) al prender una pared el frente cubre toda la pared en ~3 s, el daño del fuego es 12/s y **no lo limita** el tope de `Burn`; (e) lo carbonizado recibe ×2; (f) a vida 0 el collider desaparece y se usa Retract+Scar, y con la pared totalmente carbonizada Crumble+AshScar; (g) la oleada se lanza al primer daño, respeta los 12 s y el tope de 6, y se detiene al caer la pared; (h) al caer las 3 paredes el jefe tiene 150 y `FinalPhaseReady`; (i) sin fugas: al destruir la sala no quedan texturas/sprites/mallas de la tarea; (j) **no se rompe** nada de la L1 (las 12 pruebas anteriores siguen verdes).

## Números y reglas (cerrados)
- Vida total **600**; pared **150**; fase final **150**. Ya no es provisional el 25 % por pared.
- Nudos: ×2 al pegar en la zona; aguantan 22 de daño sin multiplicar (provisional); revientan una vez.
- Fuego: frente ~3 s, **12/s** (provisional, lo ajustaremos probando), carbonizado **×2**, sin tope de `Burn`.
- Oleadas: 3 por pared, 12 s entre ellas, tope 6 vivos; tamaños y especies de arriba (provisionales).
- 2 odres + 3 helechos por pared (provisional).
- Constantes al inicio del archivo, con nombre, marcadas provisionales cuando lo sean.

## Archivos que puede tocar
- `Scripts/Enemies/LepidodendronBoss*.cs` (puedes crear `LepidodendronBoss.Walls.cs` y `LepidodendronWall.cs`), `Editor/LepidodendronBossSetup.cs`, `Editor/ArtSetup.BossLepidodendron.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Solo lo imprescindible y justificado en `Rooms/` (por ejemplo, un método para registrar un `IFlammable` o crear un enemigo en una posición) — avísalo en `Resultado`.
- Se regeneran con el menú: prefab, escena de prueba y datos del jefe.

## Fuera de alcance (NO hacer)
- No dibujar ni tocar PNG ni los `.py`. Si falta una hoja de `Walls/` o `Decor/`, **para y avisa**.
- Nada de conos, fase final, tronco recibiendo daño, latigazos, raíces bajo el jugador, **esporas en arco** (pasan a la L4), barra propia, presentación, desplome ni recompensa.
- No tocar la Meganeura, otros jefes, el `Floor3.asset` real ni la puerta secreta.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba` (rehace prefab y escena) y después `LepidodendronBossTests`, `MeganeuraBossTests` y `BossHealthBarTests`. Fallos que ya existían: ver `AGENTS.md`.
- A ojo (usuaria), `Test_BossLepidodendron`: en la pared 1 pega a un nudo y a la pared normal (¿cambia de estado, hay Hurt, revienta el nudo?); prende un odre o un helecho y mira cómo avanza el fuego y cuánto tarda en caer; compara a golpes (~20-30 s por pared, a ojo) y con fuego (~8-12 s); observa las oleadas (cuántos salen, cada cuánto, si el tope de 6 se nota) y que la barra del jefe baje; tras la tercera pared, el jefe en 150.

## Resultado (lo rellena Codex)
- Hecho:
- Archivos tocados:
- Pruebas (cuántas pasan):
- Teclas de prueba:
- Dudas / no comprobado:
