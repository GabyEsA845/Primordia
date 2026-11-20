# Tarea 16 — Megarachne: importar y programar los cables v2

- **Rama:** `codex/mega-2-ataques` (sigue en ella; sin rama nueva)
- **Escrita por:** Claude (2026-11-20) · **Implementa:** Codex (o Sonnet si Codex no tiene tokens)
- **Estado:** pendiente

## Objetivo
El arte de los cables v2 ya está hecho (Opus, 2026-11-18) en `docs/multimedia-temp/floor3-bosses/megarachne/`. Hay que importarlo y usarlo en la sala del Nido: textura de haz de hebras sin estirar, abanico de arranque en cada cable y un hilo colgante con material propio. Solo importar y programar: **no generar ni modificar arte, no ejecutar ningún `.py`**.

## Contexto mínimo
`AGENTS.md`; `docs/memoria-claude/primordia-megarachne-cable-art-pending.md`; README de `floor3-bosses/megarachne/`, sección «Cables v2 y arranques» (es la especificación del arte). Código: `MegarachneBoss.Cables.cs` (`SetArena`, `MakeLine`), `MegarachneBossCanopy.cs`, `Assets/_Primordia/Editor/ArtSetup.BossMegarachne.cs` (`ImportMegarachneSheets`), `MegarachneBossSetup.cs` (material y `ConfigureScenery`).

## Qué hacer
1. **Importador** (`ArtSetup.BossMegarachne.cs`):
   - Añadir a la lista de archivos `Nido/Nido_CableRoot.png` y `Effects/Cable_Grow.png`, `Cable_Burning.png`, `Cable_Charred.png` (los `Cable_Normal`/`Cable_Thin` ya están, pero la copia de `Assets` es la antigua: la lógica de copia por bytes ya la sustituye).
   - `Nido_CableRoot`: hoja de **5 cuadros de 256×224** (NO, N, NE, SO, SE, en el orden de `SetArena`), pivote **(0,5, 36/224)**. Hoy los `Nido/` se cortan en 160; este necesita su propia celda (256×224) y el pivote indicado. `Nido_Tree_Wet`, `Nido_Web` y `Nido_Eyes` quedan como están.
   - Los `Cable_*` **no se cortan en sprites de 32**: son texturas para LineRenderer. Dejar `wrapMode` Repeat, filtro Point, sin compresión, sin mipmaps; no hace falta `SliceFloor3Sheet` para ellos (déjalos como textura simple; si el importador actual los corta, evitar que lo haga para `Cable_*`).
2. **Material del cable** (`MegarachneBossSetup.cs`, `Data/Bosses/MegarachneCable.mat`): textura `Cable_Normal`, **`mainTextureScale = (0.25, 1)`**, offset 0 (un solo cuadro). Crear otro material `MegarachneCableThin.mat` con `Cable_Thin` (3 cuadros de 64×4 en vertical, `mainTextureScale = (0.25, 1/3)`) para el hilo colgante. Pasar ambos a `ConfigureScenery` (añadir el parámetro; el material fino se guarda en un campo serializado).
3. **Cable** (`MakeLine` / `SetArena`): quitar el afinado en la rama. La curva de ancho pasa a **1 → 1 (en 0,9) → 0,7** en el tocón (ver README). Ancho sigue en `CableWidth` = 1,8.
4. **Arranques:** en `SetArena`, crear 5 `SpriteRenderer` `Nido_CableRoot_i` hijos de `scenery`, en la posición del árbol (el pie del tronco, la misma que `Copa`/`Telarana`), **orden 22**, cada uno con el cuadro `i`. Guardarlos en un array (`cableRoots`) y exponer `SetCableRootVisible(int i, bool visible)` (lo usará M4: cable que arde = abanico oculto). Hoy el hilo colgante usa `ThreadOrder = 22`: pasar el hilo a 23 para que no quede tapado.
5. **Canopy:** `MegarachneBossCanopy.Configure` recibe también los 5 arranques y les aplica el mismo alfa que a la copa y la telaraña (0,45 cuando cubre a un actor del suelo).
6. **Hilo colgante:** usar el material `Cable_Thin`; animar el brillo con `mainTextureOffset.y = (3 - 1 - frame) / 3` a ~8 fps mientras `thread.enabled`. Sin la propiedad `Tile` rota: el LineRenderer repite la textura entera.
7. **Menú:** que `Primordia > Boss > Actualizar ataques Megarachne (M2)` (o el menú equivalente) regenere el prefab con todo lo anterior, sin tocar otro arte. Ejecutar el menú.
8. **Pruebas PlayMode** (añadir a `MegarachneBossTests`): hay 5 `Nido_CableRoot_*` con orden 22 y sprite no nulo; el material del cable tiene `mainTextureScale` (0,25, 1); el hilo usa un material distinto al del cable; el alfa de los arranques sigue al de la copa (`Canopy.Alpha`); `SetCableRootVisible(i,false)` apaga solo ese. Las 19 pruebas actuales deben seguir en verde.

## Fuera de alcance
Fuego en los cables y uso de `Cable_Grow/Burning/Charred` en el juego (M4; aquí solo se importan), saco y crías (M3), enfado, entrada, barra, muerte, sonido, enganche a Floor_3. No tocar `generar_*.py` ni los PNG originales de `docs/`.

## Cómo probar
`Test_BossMegarachne`: los cables se ven como haces de hebras (no como pasarela), cada arranque tiene su abanico sobre la copa y la unión con las hojas se ve limpia; el hilo colgante del salto es fino y continuo; al pasar el jugador bajo la copa, abanicos y copa se aclaran a la vez. Lanzar `MegarachneBossTests`.

## Resultado
- Importé los cinco tramos `Cable_*` como texturas simples (Repeat, Point, sin compresión ni mipmaps) y corté `Nido_CableRoot` en cinco sprites de 256×224 con el pivote al pie del tronco. Los seis PNG importados coinciden byte a byte con los originales aprobados.
- El cable conserva el ancho 1,8, se afina solo junto al tocón y repite la textura cada 4 unidades. Los cinco abanicos están en orden 22, comparten el alfa de la copa y se pueden ocultar por separado. Los coloqué en el origen local del Nido, que coincide con el pie del tronco y la telaraña. El hilo está en orden 23 y usa una instancia del material fino animada a 8 fps. Ambos materiales usan `Unlit/Transparent` para aplicar la escala y el desplazamiento UV.
- Ejecuté `Primordia > Boss > Actualizar cables Megarachne (v2)`, el menú equivalente para esta tarea. Unity compiló y regeneró `Boss_Megarachne.prefab` con las cinco referencias a sprites y ambos materiales. El primer arranque se quedó esperando al servicio de licencias. El reintento fuera del sandbox completó el menú.
- Archivos tocados. `ArtSetup.BossMegarachne.cs`, `MegarachneBossSetup.cs`, `MegarachneBoss.Cables.cs`, `MegarachneBossCanopy.cs`, `MegarachneBossTests.cs`, `Boss_Megarachne.prefab`, `MegarachneCable.mat`, `MegarachneCableThin.mat` y su `.meta`. También se actualizaron los cinco PNG `Effects/Cable_{Normal,Thin,Grow,Burning,Charred}.png` y sus `.meta`, y se añadió `Nido/Nido_CableRoot.png` con su `.meta` bajo el arte importado en `Assets`.
- PlayMode, `MegarachneBossTests`: **24 de 24 aprobadas** (19 existentes y 5 nuevas), 0 fallos y 0 omitidas. Resultado en `Logs/MegarachneBossTests-v2.xml`.
- Queda pendiente la revisión visual de la usuaria en `Test_BossMegarachne`. `Cable_Grow`, `Cable_Burning` y `Cable_Charred` se importaron para M4. No se ejecutó ningún generador, no se editaron los PNG de `docs/` y no se hizo commit.
- **Estado: HECHA y probada por la usuaria (2026-11-20).** Ajustes posteriores hechos por Claude tras su revisión visual:
  - La unión cable-copa se veía cortada (el cable nacía en el borde bajo de la copa y el abanico, pálido sobre la telaraña, no tapaba el corte). Ahora el DIBUJO del cable empieza `CableRootInset = 2` u más adentro de la copa (solo visual; el recorrido del jefe sigue en `starts[i]`).
  - Hitbox: el tronco central (2 u de ancho) ahora va de y = −0,45 a 1,25 (`TrunkBottom/TrunkTop`) y cada Sigillaria va del pie al punto de amarre del cable (80 % del sprite), en vez de 1,2 u y 0,8 u en la base. La copa sigue sin colisión (es aérea).
