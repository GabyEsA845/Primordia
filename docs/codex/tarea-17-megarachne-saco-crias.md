# Tarea 17 — Megarachne: saco de crías y crías (M3)

- **Rama:** `codex/mega-3-crias` (ya creada desde `feature/boss-megarachne`, que ya incluye M1, M2 y los cables v2)
- **Escrita por:** Claude (2026-11-20) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Añadir al combate el **saco de crías**: la Megarachne teje un saco colgante; si no se rompe a tiempo, revienta y salen 3-4 **crías** que persiguen y muerden al jugador. Sin fuego (M4), enfado (M5), entrada/barra/muerte/recompensa (M6).

## Contexto mínimo
`AGENTS.md`, `docs/codex/megarachne-plan.md` (fila M3), `docs/memoria-claude/primordia-boss-megarachne.md`, README de `docs/multimedia-temp/floor3-bosses/megarachne/` (secciones «Efectos» → Saco y Crías). Código: `MegarachneBoss.Attacks.cs` (ciclo de ataques, `EffectSheet`, `TransientFx`, `SilkBall`), `MegarachneBossWeb.cs` (ejemplo de componente propio con vida limitada), `MegarachneBossSetup.cs` e `ImportMegarachneAttacks` (importar hojas). Para el movimiento y la vida de las crías, mira cómo otros enemigos del piso 3 pequeños reciben daño del jugador (capa Enemy, `Health`) y cómo lo hace la araña de las frondas.

## Arte (ya hecho; solo importar, sin generar ni editar)
Los PNG ya están en `Assets/.../megarachne/Effects/` (y en `docs/.../Effects/`): `Megarachne_Sac_Idle` / `_Tremble` (4) / `_Burst` (5, salen las crías) / `_Break` (5, roto antes: sin crías), 24×32 con el hilo atado **arriba en el centro (12, 0)**; `Megarachne_Brood_Move` (4) / `_Bite` (4, el mordisco cuenta en el cuadro 2) / `_Hurt` (2) / `_Death` (5), 24×24, **de lado** (Flip X para la izquierda). Cortar con la celda indicada; pivote del saco = (12/24, 1) (arriba), el de las crías centro.

## Qué hacer
1. **Saco** (`MegarachneBoss.Sac.cs` + componente `MegarachneBossSac`): cada **4.º ataque del ciclo** (es decir, tras salto → ráfaga → red → saco, se repite) en vez de un ataque nuevo, y SOLO si no hay ya un saco: ella teje un saco desde el cable donde está, con `Weave`/hilo (reutiliza el `DrawThread`/animación `Weave` del arte existente) y lo deja colgando a **entre 3 y 8 u del jugador** (dentro de la sala, fuera de paredes y de los colliders del Nido). Colocar el saco no cuenta como ataque del ciclo de 4 s: sustituye al ataque de esa vuelta.
   - Máximo **1 saco** a la vez. `Idle` colgando **6 s**, luego `Tremble` **1 s**, luego `Burst` (cuadro 3 de 5 suelta las crías; las crías salen en abanico alrededor).
   - Tiene **vida 3** (3 golpes/disparos del jugador); si llega a 0 antes del `Burst` → `Break` y **no salen crías** (el saco desaparece al acabar la animación). Debe recibir daño del jugador igual que otros objetos/enemigos pequeños (balas y melé).
   - Número de crías: **4** por saco (3 si ya hay 6 o más crías vivas; ninguna si hay 8). Total de crías vivas ≤ 8 (constante `BroodLimit`).
2. **Crías** (`MegarachneBrood`): enemigos pequeños en el suelo. Vida **2**, velocidad **4 u/s** hacia el jugador (esquivando los colliders del Nido; usa la misma navegación sencilla que la Megarachne en el suelo o la de otros enemigos pequeños, sin pathfinding complejo), radio ~0,4 u. **Mordisco**: si está a < 0,8 u del jugador, se detiene, anima `Bite` y en el cuadro 2 hace **1 de daño**; recarga **1,2 s**. `Hurt` al recibir daño; `Death` al morir (se destruye al acabar). Las telarañas de suelo de la Megarachne las **frenan** igual que a todos. Mueren al instante con `KillAllBrood()` (método público de `MegarachneBoss`; lo usará M6 al morir ella).
3. **No** cuentan como progreso de sala ni dan biomasa (como el resto de esbirros del jefe: ver cómo lo hacen Arthropleura/Eurypterid si hay precedente; si no, sin recompensa).
4. **Importador**: ampliar `ImportMegarachneAttacks` (o nuevo `ImportMegarachneBrood` con su menú `Primordia > Boss > Actualizar saco y crías Megarachne (M3)`) para cortar las 8 hojas, y regenerar el prefab `Boss_Megarachne`. No cambia el arte original.
5. **Escena de prueba** `Test_BossMegarachne`: tecla Ctrl+Mayús+F7 = tejer saco ahora; F8 = reventarlo ya.
6. **Pruebas PlayMode** (en `MegarachneBossTests`): el 4.º ataque del ciclo teje un saco; solo hay uno a la vez; tras 6 s + 1 s revienta y salen 4 crías; si se destruye antes (3 daños) no salen crías; tope de 8 crías vivas; una cría persigue y muerde 1 de daño con recarga 1,2 s; `KillAllBrood()` las elimina; las 24 pruebas actuales siguen en verde.

## Números (PROVISIONALES, se ajustarán al probar)
Saco: cada 4.º ataque, vida 3, cuelga 6 s + tiembla 1 s, 4 crías (máx. 8 vivas), a 3-8 u del jugador. Cría: vida 2, 4 u/s, mordisco 1 de daño cada 1,2 s.

## Archivos que puede tocar
`MegarachneBoss*.cs` (nuevos `MegarachneBoss.Sac.cs`, `MegarachneBossSac.cs`, `MegarachneBrood.cs`), `Assets/_Primordia/Editor/*Megarachne*`, `Tests/PlayMode/MegarachneBossTests.cs`, prefab `Boss_Megarachne`, escena de prueba. Arte solo importar.

## Fuera de alcance
Fuego en cables (M4), enfado (M5), entrada, barra, muerte y recompensa (M6), sonido, enganche a Floor_3. **No ejecutar generadores `.py` ni editar PNG.** No cambiar el comportamiento de los ataques existentes.

## Cómo probar
`Test_BossMegarachne`: tras tres ataques ella teje un saco; si lo ignoras revienta y salen 4 crías que te muerden; si lo rompes a tiempo no salen. Lanzar `MegarachneBossTests`.

## Resultado

- El ciclo automático alterna salto, ráfaga, red y saco. El saco ocupa la cuarta vuelta de la cadencia de 4 s. Si ya hay uno, esa vuelta se consume sin tejer otro. El saco cuelga del cable y su cuerpo queda entre 3 y 8 u del jugador, fuera de los obstáculos sólidos del Nido y las paredes. Tiene 3 PV, cuelga 6 s, tiembla 1 s y libera las crías en el tercer cuadro de Burst. Si llega a 0 PV antes, reproduce Break.
- Las crías tienen 2 PV, persiguen al jugador a 4 u/s, rodean obstáculos y reciben el frenado de las telarañas. El mordisco aplica 1 de daño en el segundo cuadro y tiene una recarga de 1,2 s. El abanico se recorta según el espacio libre. El tope de 8 vivas limita la eclosión a 2 crías cuando ya hay 6 y a 1 cuando hay 7. Las crías quedan fuera del progreso de sala y de la biomasa, y `KillAllBrood()` las retira de inmediato.
- En `Test_BossMegarachne`, Ctrl+Mayús+F7 teje un saco desde el cable y F8 adelanta Burst. La escena usa el prefab actualizado y sus teclas de prueba ya habilitadas.
- Se ejecutó el menú **Primordia > Boss > Actualizar saco y crías Megarachne (M3)** en Unity 6000.6.2f1. Cortó las ocho hojas existentes y guardó `Boss_Megarachne`. La hoja Idle del saco contiene cuatro cuadros de 24×32 y se importaron los cuatro.
- Archivos tocados en `Assets/_Primordia`: `Scripts/Enemies/MegarachneBoss.cs`, `MegarachneBoss.Attacks.cs`, `MegarachneBoss.Cables.cs`, `MegarachneBoss.Sac.cs`, `MegarachneBossSac.cs`, `MegarachneBrood.cs`, `MegarachneBossCanopy.cs` y `MegarachneBossWeb.cs`, con los tres `.cs.meta` nuevos. También `Editor/ArtSetup.BossMegarachne.cs`, `Editor/MegarachneBossSetup.cs`, `Tests/PlayMode/MegarachneBossTests.cs`, `Prefabs/Enemies/Boss_Megarachne.prefab` y los ocho `.png.meta` de las hojas del saco y las crías.
- PlayMode, filtro `MegarachneBossTests`: **28/28 aprobadas**, incluidas las 24 previas y 4 nuevas. La prueba previa del ciclo se amplió para verificar la cuarta vuelta y el regreso al salto. Resultados en `Logs/MegarachneBossTests-M3.xml`, registro del menú en `Logs/Megarachne-M3-import.log` y registro de pruebas en `Logs/Megarachne-M3-tests.log`. Los archivos C# conservan CRLF.
- Pendiente de la prueba manual en `Test_BossMegarachne`: comprobar los atajos, el aspecto del hilo y del abanico, la navegación en la sala, el frenado y los impactos con balas y melé. La prueba automatizada de rotura aplica los tres golpes mediante `Health`. Cambios listos para la revisión de la usuaria, sin commit.
- **Estado: HECHA y probada por la usuaria (2026-11-20): «en general funciona bien».** `MegarachneBossTests` 28/28. Commit en `codex/mega-3-crias` y fusionada en `feature/boss-megarachne`.
