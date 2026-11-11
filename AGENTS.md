# Primordia — instrucciones para Codex

Eres el **implementador** de Primordia (juego 2D en Unity 6000.6.2f1, C#). Claude dirige el proyecto, diseña y revisa; tú escribes el código de las tareas que él te deja en `docs/codex/`. Comentarios y mensajes de commit en **español**.

## Cómo trabajar
1. Lee **solo** la tarea que te indiquen (`docs/codex/tarea-XX-*.md`). Trae el objetivo, los archivos, los números y cómo probar. Si algo no está claro o te obliga a decidir diseño, **para y pregúntalo**: no inventes mecánicas ni números.
2. Trabaja en la **rama que diga la tarea** (p. ej. `codex/7e-1-barra`), creada desde `feature/floor-3`. Nunca edites `feature/floor-3` ni `main` directamente. No hagas commit hasta que la tarea lo diga; no hagas push ni merge.
3. Toca **solo los archivos que la tarea lista** (y sus pruebas). Nada de refactors ni limpiezas fuera de ellos.
4. Si la tarea pide un cambio de editor de Unity (prefabs, assets, escenas), hazlo con un **menú `Primordia`** (clase en `Assets/_Primordia/Editor/`), nunca a mano en el editor.
5. Al acabar, escribe al final del archivo de la tarea una sección `## Resultado` con: qué hiciste, archivos tocados, pruebas lanzadas (con cuántas pasaron), dudas y cosas que no pudiste comprobar.

## Reglas del proyecto (las que más duelen)
- Los `.cs` son **CRLF**. Conserva los finales de línea al editar.
- Los `static` hay que limpiarlos a mano (el proyecto entra en Play sin recargar el dominio): usa `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
- **El arte y el audio siempre llegan ya hechos** (los crean y aprueban Claude y la usuaria, en `docs/multimedia-temp/`, con sus README y generadores `.py`). Tu trabajo es solo **importarlo y programarlo**: no dibujes, generes ni retoques sprites o sonidos, ni ejecutes los generadores `.py`. Si la tarea necesita arte que no encuentras, **para y avisa**; no pongas un sustituto. (Las tareas indican dónde está el arte y qué menú `Primordia` lo importa.)
- No toques `ProjectSettings/ProjectSettings.asset` (Unity lo modifica solo) ni dejes fuera referencias perdidas en prefabs/assets.
- Las constantes numéricas de combate van **al inicio del archivo**, con nombre, y marcadas como provisionales si lo son.
- Ahorra: no releas archivos enormes enteros; busca lo que necesites.

## Unity (¡solo una instancia a la vez!)
Si Unity está abierto con el proyecto, el batchmode falla («another Unity instance»). **Pregunta antes de lanzar Unity**; mientras tanto compila mirando `Logs/Editor.log` por `error CS`.
- Menú / compilar: `Unity.exe -batchmode -nographics -quit -projectPath <ruta> -executeMethod Primordia.Editor.<Clase>.<Método> -logFile <ruta>`
- Pruebas: `Unity.exe -batchmode -nographics -projectPath <ruta> -runTests -testPlatform PlayMode -testFilter "<Clase>" -testResults <xml> -logFile <log>` (sin `-quit`).
- Fallos que ya existían (no son tuyos): `Floor3EnemiesTests` (Calamites ×2, Dragonfly ×2), `Floor3LimbsTests` (JumpingLegs ×2), `HeartStorageTests.ShopPod_…`, `SlimeTrailTests.Clear_RemovesEveryStampAtOnce`; `PoisonSpitTests.Ball_…` es intermitente.
- Detalle y ruta de Unity en tu equipo: `docs/memoria-claude/primordia-unity-batchmode.md` y `primordia-unity-gotchas.md`.

## Memoria del proyecto
`docs/memoria-claude/` (índice en `MEMORY.md`) tiene el diseño de cada piso y jefe. Lee solo los archivos que tu tarea enlace. **No los edites**: de eso se encarga Claude.
