# Tarea 19 — Megarachne: enfado al 50 % (M5)

- **Rama:** `codex/mega-5-enfado` (ya creada desde `feature/boss-megarachne`, que incluye M1-M4)
- **Escrita por:** Claude (2026-11-21) · **Implementa:** Codex
- **Estado:** pendiente

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
(Pendiente: lo rellena quien la implemente.)
