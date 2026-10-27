---
name: primordia-unity-gotchas
description: "Trampas técnicas del entorno de Primordia (Unity abierto, logs, recarga de dominio, CRLF, zips, referencias perdidas)"
metadata:
  node_type: memory
  type: reference
  originSessionId: 804bccaa-8bc0-4674-9463-b67798259689
  modified: 2026-10-25T19:30:24.096Z
---

- **Unity está abierto** con el proyecto: no se puede usar batchmode ni compilar desde fuera. Unity recompila al recibir foco. Errores de compilación y excepciones: `D:\Proyectos\Primordia\Logs\Editor.log` (buscar `error CS`). Resultados del Test Runner: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Primordia\TestResults.xml`.
- **Compilar sin Unity:** `dotnet` 10 está instalado. Copiar los 3 .csproj de Unity a `*.check.csproj` con todos los .cs actuales de cada carpeta (los de Unity no incluyen archivos nuevos hasta que los regenera) y las ProjectReference renombradas; `dotnet build Primordia.Tests.PlayMode.check.csproj` compila los tres (salida en Temp/, ignorada). Borrar las copias después. LangVersion 9.
- **Recarga de dominio desactivada** (Enter Play Mode Options): todo `static` necesita reset con `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
- Ensamblados: `Primordia` (Scripts), `Primordia.Editor`, `Primordia.Tests.PlayMode`; `InternalsVisibleTo` para tests y editor. `PixelPerfectCamera` está en `Unity.RenderPipelines.Universal.2D.Runtime`.
- **Orden de Awake/OnEnable entre componentes no está garantizado** al cargar escenas: cada componente debe obtener sus propias referencias (bug real con EnemyAI/Enemy). En tests, `AddComponent` ejecuta OnEnable antes de `Configure`.
- Al abrir una escena nueva (NewScene/TryCreateSceneFromTemplate) Unity descarga los assets sin usar: una referencia a un asset recién creado queda "null" y FloorSetup.BuildFloor caía en Floor_1 sin avisar (pasó en Test_Lineages y Test_BossAmoeba). Volver a cargarlo con AssetDatabase.LoadAssetAtPath después de abrir la escena.
- En menús de editor: guardar (`AssetDatabase.SaveAssets`) los assets recién creados **antes** de `EditorSceneManager.OpenScene`, o sus referencias quedan vacías; en instancias de prefab en escena, asignar campos con `SerializedObject`.
- Tras `git switch` los archivos quedan con **CRLF**: en scripts node normalizar `\r\n` antes de buscar texto.
- Python 3.12 + Pillow instalados (2026-10-13), numpy y scipy desde el 2026-10-23; el paquete `imageio-ffmpeg` trae un ffmpeg con libvorbis (`imageio_ffmpeg.get_ffmpeg_exe()`) en `%LOCALAPPDATA%/Programs/Python/Python312/python.exe` (el `python` del PATH sigue siendo el alias de Microsoft Store: usar la ruta completa). En PowerShell 5.1 no pasar código con comillas a `python -c`: escribir un .py en el scratchpad. Para editar texto, node sigue valiendo. Zips para itch.io con `tar.exe -a -c -f`, no `Compress-Archive` (PowerShell 5.1 pone barras invertidas).
- Scripts node con Markdown (comillas invertidas): escribirlos en un .js del scratchpad; dentro de node -e en bash las comillas invertidas se ejecutan como comandos y estropean el texto.
- WebGL para itch.io: Gzip + decompression fallback (Brotli falla), 960×540.

- (2026-10-25) Las escenas hechas con la plantilla Lit2D **ya traen una Global Light 2D**: no crear otra (error "More than one global light"). Al montar el piso en el Awake del generador, la luz de la escena aún no pasó por OnEnable: comprobar `enabled && gameObject.activeInHierarchy`, no `isActiveAndEnabled`.
- En el editor, `GetComponent<T>() ?? AddComponent<T>()` falla (null falso de Unity): usar if (x == null).
- En Play Mode (Test Runner) `Object.Destroy` es al final del cuadro: las pruebas que comprueban destrucción necesitan `yield return null`.
- Las luces 2D por encima de 1 necesitan m_HDREmulationScale > 1 en Renderer2D (WebGL sin texturas float).
- El Test Runner escribe TestResults.xml solo de lo que se ejecutó: mirar la fecha y el total antes de dar algo por verde.
- Herramienta Bash: los heredoc con código largo (comillas, $, comillas invertidas) a veces fallan con "unexpected EOF"; escribir el script con Write en el scratchpad (que es de cada sesión: rehacer el check.js de compilación si hace falta).

- (2026-10-25) Las escenas de prueba hechas con TryCreateSceneFromTemplate **no llevan HUD** salvo que se instancie GameUI (Test_Floor2 no lo tenía: nada de la interfaz se veía). Si la escena ya existe, TryCreateSceneFromTemplate solo la abre y no rehace nada.
- (2026-10-25) En la herramienta Bash, dentro de un heredoc las barras invertidas dobles se quedan en una: un JSON con "barra barra n" acaba siendo un salto de línea real en el C#. Para código con barras invertidas, usar Write/Edit o un .py escrito con Write.

Relacionado: [[primordia-workflow]].
- (2026-10-25) En pruebas PlayMode, los proyectiles disparados (ProjectilePool) sobreviven al TearDown y alcanzan al jugador de la prueba siguiente (orden alfabético): fallo intermitente de la Ottoia por las cargas de la medusa. En el TearDown, destruir los Projectile (FindObjectsByType con FindObjectsInactive.Include) y ProjectilePool.Clear().
- (2026-10-26) Las pruebas que miden distancia recorrida por algo con fin por tiempo (esquiva, dash) bailan un paso de física (0,02 s): el fin se comprueba en FixedUpdate y el inicio cae en Update. Tolerancia de al menos un paso de recorrido (fallo intermitente de Floor2MutationsTests.TimeWarp_PlayerDodgeCoversTheSameDistance: 3,36 con ±0,35).
