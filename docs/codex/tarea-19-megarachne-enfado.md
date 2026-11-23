# Tarea 19 — Megarachne: enfado al 50 % (M5)

- **Rama:** `codex/mega-5-enfado` (ya creada desde `feature/boss-megarachne`, que incluye M1-M4)
- **Escrita por:** Claude (2026-11-21) · **Implementa:** Codex
- **Estado:** implementada y probada en batchmode; pendiente prueba manual de la usuaria (sin commit)

## Objetivo
Al bajar a la mitad de vida (75 de 150) ella **se enfada**: vuelve al Nido, chilla, sale en **Rage** (más rápida, ciclo más corto) y el **Nido puede arder UNA vez** (solo en el enfado): eso tira todos los cables y la deja aturdida mucho tiempo (la gran ventana de daño); después teje cables nuevos desde las ramas, más rápidos y ya sin refugio. Sin entrada, barra, muerte, recompensa (M6) ni sonido.

## Contexto mínimo
`AGENTS.md`, `docs/codex/megarachne-plan.md` (fila M5), `docs/memoria-claude/primordia-boss-megarachne.md` (párrafos «Enfado», «6i decisiones» y «6i-2 piezas»), README de `docs/multimedia-temp/floor3-bosses/megarachne/` (secciones del Nido, Rage y efectos del enfado). Código: `MegarachneBoss.cs` (`MaxHealth`, `DebugHealthFraction`), `.Cables.cs` (cables, tejido, `Mode`), `.Fire.cs` (**`NestCanBurn`**, `nestLimit`, `smoked`, caída del cable, re-tejido), `.Attacks.cs` (ciclo de 4 s), `.Sac.cs`, `MegarachneBossCanopy.cs` (copa).

## Arte (ya hecho; solo importar, sin generar ni editar)
- **Ella en Rage:** `Sheets/Rage/Megarachne_<Anim>_Rage` (todas las de cuerpo + Threat + Hurt + Drop/Climb), `Screech` 6 y `IntroEmerge` (1 dir, pivote = centro del embudo), `Death`/`Dead` (para M6, no usar aún).
- **Árbol (carpeta `Nido/`):** `Nido_Tree_Wet`/`Dry`/`Charred`, `Nido_Web`, `Nido_Tree_Shake` 6, `Fx_Nido_Rain` 8 (capa), `Nido_Tree_Drying` 6 (manchas que crecen), `Nido_Burn` 8 (árbol+Nido ardiendo, el último = carbonizado), `Nido_Tree_Charred_Smolder` 4 (bucle humeante), `Nido_WebDecay` 6.
- **Efectos:** `Fx_Megarachne_Screech` 6 (192×128, pivote = embudo), más `Fall`/`Stunned`/`Recover` y los `Cable_*` ya importados en M4.
Importar lo que falte en `ArtSetup.BossMegarachne.cs`.

## Qué hacer
1. **Disparador**: cuando la vida baja a ≤ 50 % (`Health <= MaxHealth/2`, una sola vez) y está en suelo o cable: cancela el ataque en curso, el saco y las crías vivas **no** se eliminan, y empieza el enfado. Mientras dura el enfado es **invulnerable** y el jugador no sufre daño de ella (las crías siguen).
2. **Retirada al Nido**: corre por un cable hasta el Nido (si no hay cable, baja/cruza por el suelo a velocidad normal) y **entra en el embudo** (`IntroEmerge` al revés o simplemente se oculta al llegar).
3. **Chillido (~2,5 s)**: desde dentro: `Nido_Tree_Shake` (el árbol se sacude), `Fx_Nido_Rain` (cae agua en lluvia), `Fx_Megarachne_Screech` (onda roja), `Nido_Tree_Drying` (la copa amarillea) y la copa cambia a `Nido_Tree_Dry`. Cámara con un temblor corto (usa el que ya exista, p. ej. el de la Arthropleura).
4. **Sale en Rage**: `IntroEmerge` y baja por un cable. A partir de aquí **usa las hojas `_Rage`** en todas sus animaciones y: velocidad ×1,4 (`CableSpeed`, huida, trepar, carrera), **ciclo de ataques de 3 s** (era 4), el saco sale cada 3.ª vuelta (era 4.ª) con **5 crías** (era 4, tope 10 vivas, crías ×1,2 de velocidad). Daños sin cambios.
5. **2 cables nuevos** salen del Nido nada más empezar la fase Rage (los tejes con `Cable_Grow` como en M4, hacia 2 Sigillarias libres o hacia puntos de anclaje nuevos sobre troncos de la sala; elige sitios sin cable y a ≥ 6 u de los existentes). Quedan **inflamables como los demás**.
6. **El Nido arde (una sola vez)**: `NestCanBurn` pasa a **true solo mientras dura la fase Rage y antes de arder**. Si el fuego de una mecha llega al Nido, o un odre/disparo de fuego prende el Nido (nodo `IFlammable` propio en la copa, radio de ignición 2,5 u), arde: `Nido_Burn` (8 cuadros) y luego `Nido_Tree_Charred_Smolder` en bucle, la seda cae (`Nido_WebDecay`), **todos los cables arden/caen a la vez** (cabos como en M4) y ya no vuelve a haber Nido que arda ni embudo (refugio perdido).
7. **Gran ventana**: al arder el Nido ella cae **aturdida 6 s** (`Stunned`, vulnerable, el doble que en M4); luego `Recover`. El Nido ardido no vuelve a arder (`NestBurned = true`).
8. **Cables tras el incendio**: pasados 4 s del Recover teje **2 cables nuevos desde las ramas desnudas** (puntos de anclaje en el árbol ardido; `Cable_Grow` 2 s en vez de 3, "más rápidos") hacia 2 Sigillarias. Si la fase Rage termina sin que el Nido arda, simplemente conserva los cables del paso 5.
9. **Rage y fuego en cables**: el aturdimiento por frente de fuego sigue siendo de 3 s; la huida va ×1,4; el re-tejido de un cable caído tarda 7 s (era 10) en Rage.
10. **Teclas en la escena de prueba**: Ctrl+Mayús+F11 fuerza el enfado ahora; Ctrl+Mayús+F12 prende el Nido (si está en Rage). Y que `DebugHealthFraction` siga disparándolo al bajar de 50 %.
11. **Menú** `Primordia > Boss > Actualizar enfado Megarachne (M5)` (importa lo que falte, guarda el prefab y la escena).
12. **Pruebas PlayMode** (`MegarachneBossTests`): al bajar a ≤ 75 PV empieza el enfado una sola vez; es invulnerable durante él; tras el chillido usa Rage (`IsRage` o similar) con velocidad ×1,4 y ciclo de 3 s; aparecen 2 cables nuevos; con `NestCanBurn` solo en Rage: el fuego en el Nido lo quema, todos los cables caen, queda aturdida ~6 s y vulnerable; el Nido no arde dos veces; en M4 (antes del enfado) el fuego sigue apagándose junto a la copa; tras el incendio teje 2 cables nuevos; las 37 pruebas actuales siguen en verde.

## Números (PROVISIONALES, se ajustarán al probar)
Enfado a 75 PV (50 %); chillido 2,5 s; velocidad ×1,4; ciclo 3 s; saco cada 3.ª vuelta con 5 crías (tope 10); 2 cables nuevos al salir y otros 2 tras el incendio; aturdida 6 s tras arder; teje 4 s después del Recover (2 s de Grow); re-tejido 7 s; ignición del Nido 2,5 u.

## Archivos que puede tocar
`MegarachneBoss*.cs` (nuevo `MegarachneBoss.Rage.cs`, `MegarachneBossNest.cs` para el nodo inflamable), `MegarachneBossCanopy.cs`, `Assets/_Primordia/Editor/*Megarachne*` (importar arte Rage y árbol; el menú nuevo), pruebas, escena de prueba, prefab `Boss_Megarachne`. Arte solo importar.

## Fuera de alcance
Entrada del jefe (M6, aunque uses `IntroEmerge` para salir del embudo), barra de vida y muesca del 50 % (M6), muerte y recompensa, sonido, enganche a Floor_3, cable «por el suelo». **No ejecutar generadores `.py` ni editar PNG.** No cambiar el comportamiento de salto/ráfaga/red salvo velocidad y ciclo de Rage.

## Cómo probar
`Test_BossMegarachne`: baja su vida con F1-F3 hasta 75 o pulsa Ctrl+Mayús+F11: se mete al Nido, chilla, sale roja y más rápida con 2 cables nuevos. Prende el Nido (Ctrl+Mayús+F12 o con un odre): arde, caen los cables, ella cae aturdida 6 s y se le puede dañar; luego teje 2 cables nuevos. Lanzar `MegarachneBossTests`.

## Resultado
Implementado por Codex el 2026-11-21. Todo sin commit en `codex/mega-5-enfado`; pendiente la prueba visual de la usuaria en `Test_BossMegarachne`.

### Qué se hizo
- Disparador único a ≤75 PV y Ctrl+Mayús+F11; Ctrl+Mayús+F1 deja exactamente 75 PV mediante `DebugHealthFraction`. Cancela el ataque y los proyectiles, libera al jugador si estaba atrapado y conserva telarañas ya depositadas, saco y crías vivas.
- Retirada por cable o por el suelo a velocidad normal, acceso al embudo, chillido de 2,5 s con sacudida, lluvia, onda roja, secado y temblor corto de cámara. Invulnerable y sin ataques durante toda la transición; emerge y baja usando Rage.
- Hojas Rage, movimiento/huida/trepada ×1,4, reloj de ataque de 3 s, saco cada tercera vuelta manteniendo la rotación salto/seda/red, cinco crías por saco, tope diez y velocidad de las crías ×1,2. El salto conserva sus tiempos: si sigue recuperándose/trepando al vencer el reloj, el siguiente ataque espera a que termine (aprox. 3,34 s entre esos ataques en batch).
- Dos cables nuevos con `Cable_Grow`, hacia dos Sigillarias nuevas al sur (anclas exteriores a ≥6 u de las existentes). Inflamables al terminar el tejido, como en M4.
- Nodo inflamable del Nido con radio de 2,5 u, habilitado tras la transición Rage y hasta el primer incendio. Lo prenden el sistema compartido de fuego, la mecha de un cable o Ctrl+Mayús+F12.
- Incendio único, seda que cae, árbol carbonizado humeante y todos los cables caídos con cabos. Aturdimiento vulnerable de 6 s, Recover y, cuatro segundos después, dos cables tejidos en 2 s desde las ramas. Los otros cinco quedan retirados; el Nido y el embudo no reaparecen. Los frentes de cable mantienen su aturdimiento de 3 s y el retejido en Rage espera 7 s.
- Menú `Primordia > Boss > Actualizar enfado Megarachne (M5)` ejecutado correctamente: importó el arte aprobado, guardó prefab y escena. Copa y tronco se recortan por separado para mantener la transparencia de la copa. La escena se guardó sin diferencias de contenido: genera el Nido desde el prefab en ejecución.

### Archivos tocados
- `Assets/_Primordia/Scripts/Enemies/MegarachneBoss.cs`, `.Cables.cs`, `.Attacks.cs`, `.Sac.cs`, `.Fire.cs`; nuevos `MegarachneBoss.Rage.cs` y `MegarachneBossNest.cs` con sus `.meta`; `MegarachneBossCanopy.cs`.
- `Assets/_Primordia/Editor/ArtSetup.BossMegarachne.cs` y `MegarachneBossSetup.cs`.
- `Assets/_Primordia/Tests/PlayMode/MegarachneBossTests.cs`: ocho pruebas nuevas; la prueba de importación anterior mantiene la comprobación de las 24 hojas originales y comprueba también las 15 nuevas (39 en total).
- Ampliaciones **autorizadas expresamente por la usuaria**: `MegarachneBrood.cs`, únicamente para aplicar velocidad ×1,2 durante Rage; `Assets/_Primordia/Scripts/Rooms/IFlammable.cs` y `Flammables.cs`, mediante la interfaz opcional `IFlammableRadius`. Los implementadores existentes conservan radio cero y no requieren cambios.
- Generados exclusivamente por el menú: `Assets/_Primordia/Data/Bosses/MegarachneArt.asset`, `Assets/_Primordia/Prefabs/Enemies/Boss_Megarachne.prefab` y 24 `.png.meta` del arte de Rage, IntroEmerge, Screech, Nido y onda roja. Ningún PNG modificado; los 24 archivos importados son idénticos a sus fuentes aprobadas.
- Este archivo de tarea. `ProjectSettings/ProjectSettings.asset` permanece sin cambios; se respetaron los archivos sin seguimiento que ya había al comenzar.

### Pruebas y verificaciones
- Unity 6000.6.2f1: menú M5 completado con salida 0, sin errores CS (`Logs/MegarachneM5-menu.log`).
- **MegarachneBossTests: 45/45 pasaron, 0 fallos**, incluidas las 37 anteriores y ocho nuevas (`Logs/MegarachneM5-final.xml`, `Logs/MegarachneM5-final.log`). Comprueban umbral, invulnerabilidad/cancelación, Rage y arte, anclas nuevas, radio, incendio único, tiempos de aturdimiento/retejido, conservación y velocidad de crías, ciclo y retorno sin cables.
- Suites existentes que usan `Flammables`/`IFlammable`: **211/211 pasaron** en `Logs/MegarachneM5-tests.xml`: ArthropleuraBossTests 46, FireTests 2, Floor3LineageTests 49, Floor3SkinTests 7, LepidodendronBossTests 87, SlimeTrailTests 7, SpecialRoomsTests 13.
- La primera tanda conjunta dio 254/256: fallaban la expectativa antigua de 24 hojas y la tolerancia demasiado corta de la nueva prueba de cadencia tras el salto. Se actualizaron esas expectativas conservando las comprobaciones originales y los tiempos del salto; la repetición completa de Megarachne pasó 45/45.
- Los 14 archivos C# modificados/nuevos conservan CRLF. `git diff --check -- '*.cs'` pasa; el chequeo general solo señala espacios finales en `.meta` serializados por Unity (no se retocaron manualmente).

### Dudas y comprobaciones pendientes
- Sin decisiones de diseño pendientes. Falta la revisión visual y jugable de la usuaria (chillido, alineación de capas, retorno, incendio con odres/disparos reales y sensación del combate); batchmode no sustituye esa comprobación. No se ha abierto Unity con ventana para probar la escena.
- `git pull` se intentó antes de implementar: la rama local no tiene upstream; `git pull origin codex/mega-5-enfado` informa que no existe esa referencia remota. Se trabajó en la rama local existente, sin crear otra, commit, push ni merge.
- El primer arranque de Unity dentro del entorno restringido falló por licencia (198). Con acceso autorizado a la licencia local, menú y pruebas se completaron. Unity queda cerrado.
- No se ejecutaron generadores `.py`, no se editaron PNG ni se implementó contenido de M6, sonido o conexión a Floor_3.
