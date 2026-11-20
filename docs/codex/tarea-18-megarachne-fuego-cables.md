# Tarea 18 — Megarachne: fuego en los cables (M4)

- **Rama:** `codex/mega-4-fuego` (ya creada desde `feature/boss-megarachne`, que incluye M1, M2, cables v2 y M3)
- **Escrita por:** Claude (2026-11-20) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Es la mecánica central del combate: ella es invulnerable en los cables, así que el jugador **quema el cable** donde está ella. El fuego corre por el cable como una mecha; si el frente la alcanza, **cae aturdida** y se la puede dañar. Sin enfado (M5), entrada/barra/muerte/recompensa (M6), sonido.

## Contexto mínimo
`AGENTS.md`, `docs/codex/megarachne-plan.md` (fila M4), `docs/memoria-claude/primordia-boss-megarachne.md` (párrafo «Cable que arde», decidido 2026-11-01), README de `docs/multimedia-temp/floor3-bosses/megarachne/` (secciones «Cables v2» y «Efectos → Cable»). Código: `MegarachneBoss.Cables.cs` (`SetCableRootVisible`, `Mode`/`Travel`), `MegarachneBossWeb.cs` (ejemplo de `IFlammable` registrado en `Flammables`), `Rooms/IFlammable.cs` y `Rooms/Flammables.cs`. **Modelo de fuentes de fuego que rebrotan:** `ArthropleuraBoss.Fire.cs` (odres `MethaneSac`, `SacRegrowSeconds`, `FuseSeconds`).

## Arte (ya hecho; solo importar, sin generar ni editar)
En `Assets/.../megarachne/Effects/` ya están: `Cable_Burning` (3, al rojo, bucle), `Cable_Charred` (3, negro y fino con ascuas, bucle), `Cable_Grow` (6, hilo fino → haz), `Cable_Stub` 16×40 (4, cabo chamuscado, ancla arriba en el centro), `Fx_CableFlame` 16×24 (6, una cada ~16 px del tramo que arde), `Fx_CableFront` 24×24 (4, en cada frente), `Fx_CableAsh` 16×32 (6, ceniza que cae), `Fx_CableSmoke` 24×40 (6, humo junto al Nido). Los `Cable_*` son texturas de LineRenderer en VERTICAL (mismo sistema que ya usa el cable normal: `mainTextureScale (0.25, 1/cuadros)` y `mainTextureOffset.y = (cuadros-1-i)/cuadros`); los `Fx_*` y `Cable_Stub` son hojas de sprites (cortar en celdas del README; `Fx_*` sin girar con el cable). Importar lo que falte en `ArtSetup.BossMegarachne.cs`.

## Qué hacer
1. **Cable inflamable**: cada cable `Cable_i` se divide en ~**6 nodos** `IFlammable` (`MegarachneCableNode`, registrados en `Flammables`) repartidos por su longitud, para que `Flammables.IgniteNear` prenda el cable allí donde cae el fuego (disparos de fuego, mutaciones, odres, telarañas ardiendo cerca). Un cable ya ardiendo, quemado o ausente no se puede prender.
2. **Mecha**: al prender en un nodo, dos frentes corren **a 6 u/s** en ambos sentidos (`Fx_CableFront` en cada frente, `Fx_CableFlame` repartidas por el tramo que arde, tramo con `Cable_Burning`). Detrás del frente el cable pasa a `Cable_Charred` (negro, fino; suelta `Fx_CableAsh`). Se renderiza con una versión del cable partida en tramos (varios `LineRenderer` o un tramo por estado), sin cambiar `starts[i]`/`ends[i]`.
   - **Extremo de la Sigillaria**: el fuego llega hasta el tocón y se acaba allí.
   - **Extremo del Nido**: se detiene **justo antes** de la copa y se apaga con humo (`Fx_CableSmoke`), porque el árbol está empapado. Constante `NestCanBurn = false` (en M5 el enfado la pondrá a true y la copa podrá arder; deja el gancho, no lo implementes).
3. **Fin del cable**: cuando el fuego ha recorrido todo el cable, este **cae**: se oculta el LineRenderer y el abanico de arranque (`SetCableRootVisible(i, false)`) y quedan **dos cabos** `Cable_Stub` colgando (uno en el árbol, uno en la Sigillaria) que se deshacen a los ~3 s. Mientras no hay cable, ni ella ni nada puede usarlo: `NearestCable`, el cruce y `PlaceOnCable` deben **saltarse los cables quemados** (si no queda ninguno, ella queda en el suelo vulnerable hasta que se teja uno).
4. **Ella cae al alcanzarla el frente**: si está sobre ese cable y un frente llega a su posición → cae al suelo (`Fall`, 4 cuadros), **aturdida 3 s** (`Stunned` + `Fx_Megarachne_Stunned`, vulnerable), luego `Recover` y trepa (`ForceClimb`) a un cable que no arda. Puede **huir**: mientras un frente está a < 4 u en su cable, corre hacia el extremo opuesto a una velocidad `CableSpeed`; si el frente va más rápido que ella y la alcanza, cae; si llega al final antes de que el fuego llegue, baja/cruza a otro cable. En el ciclo de ataques, un ataque en curso (salto/red/ráfaga) no se interrumpe por el fuego; cae al terminar si el frente ya la alcanzó.
5. **Ascuas**: del cable que arde caen ascuas al suelo cada ~1 s (aleatorias a lo largo del tramo ya quemado, `Fx_CableAsh`); cada una **prende** con `Flammables.IgniteNear(posición, ~1,2 u)` las telarañas del suelo (`MegarachneBossWeb`) y los helechos que haya, **sin dañar al jugador**.
6. **Cable nuevo**: tras caer un cable, si ella está viva y ya no quemándose, a los **10 s** se vuelve a tejer: `Cable_Grow` (6 cuadros, ~3 s, de hilo fino a haz) con el abanico reapareciendo al final; el cable vuelve a ser inflamable. (La versión «cruza por el suelo con hilo fino vulnerable» queda fuera de alcance.)
7. **Fuentes de fuego que rebrotan**: **3 odres** `MethaneSac` en el suelo del Nido (reutiliza la clase y el arte de la Arthropleura si se puede referenciar desde esta sala; si no, un componente equivalente mínimo), colocados a ≥ 4,5 u del tronco y de las Sigillarias y lejos de las paredes, **rebrotan a los 20 s** (`SacRegrowSeconds`). Cuando el jugador los revienta y hay cable cerca (radio de ignición 2,5 u), prenden ese cable.
8. **Teclas en la escena de prueba**: Ctrl+Mayús+F9 prende el cable donde está ella; Ctrl+Mayús+F10 prende el cable más cercano al jugador por el medio.
9. **Pruebas PlayMode** (`MegarachneBossTests`): `IgniteNear` sobre un nodo prende el cable; los frentes avanzan a ~6 u/s en ambos sentidos; el fuego se detiene antes de la copa y se apaga con humo; al terminar quedan 2 cabos y el cable desaparece; ella cae si el frente la alcanza y queda vulnerable ~3 s; si corre más rápido que el frente a tiempo, no cae; `NearestCable` salta los cables quemados; las ascuas prenden una telaraña cercana y no dañan al jugador; tras 10 s se teje de nuevo; los odres rebrotan a los 20 s; las 28 pruebas actuales siguen en verde.

## Números (PROVISIONALES, se ajustarán al probar)
Mecha 6 u/s; 6 nodos por cable; aturdida 3 s; cabos 3 s; cable nuevo a los 10 s (+3 s de tejido); 3 odres, rebrote 20 s; ascua cada ~1 s con radio 1,2 u; ignición del odre 2,5 u; huida si el frente está a < 4 u.

## Archivos que puede tocar
`MegarachneBoss*.cs` (nuevo `MegarachneBoss.Fire.cs`, `MegarachneCableNode.cs`), `Assets/_Primordia/Editor/*Megarachne*` (importar efectos; nuevo menú `Primordia > Boss > Actualizar fuego Megarachne (M4)`), pruebas, escena de prueba, prefab `Boss_Megarachne`. Arte solo importar.

## Fuera de alcance
Enfado (M5) y que el Nido arda, entrada, barra, muerte, recompensa, sonido, enganche a Floor_3, cable nuevo «por el suelo». **No ejecutar generadores `.py` ni editar PNG.** No cambiar el comportamiento de salto/ráfaga/red/saco, salvo para saltarse cables quemados.

## Cómo probar
`Test_BossMegarachne`: prende el cable con Ctrl+Mayús+F9 con ella encima: el frente corre, ella huye o cae aturdida y se le puede dañar; el cable cae y deja dos cabos; a los 10 s se vuelve a tejer. Revienta un odre junto a un cable para prenderlo. Lanzar `MegarachneBossTests`.

## Resultado
(pendiente)
