---
name: primordia-unity-batchmode
description: "Cómo lanzar menús de Primordia y las pruebas PlayMode con Unity CERRADO (batchmode), y qué pruebas fallan de antes"
metadata:
  type: reference
---

**Con Unity cerrado** (si está abierto con el proyecto, batchmode falla con «another Unity instance»; mirar `Logs/Editor.log` por `error CS` en su lugar):
- **Menú** (sirve también para compilar): `"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/Proyectos/Primordia" -executeMethod Primordia.Editor.<Clase>.<Método> -logFile <ruta>` (valen métodos `internal`/`private static`). Ejemplos: `ArtSetup.ImportRoomSigns`, `ArtSetup.ImportThemeDecor`, `Floor3HeartStorageSetup.Import`, `Floor3AmberSpecialsSetup.CreateAll|CreateScene`, `Floor3ThemeShowcaseSetup.Create`, `Floor3EncounterThemesSetup.Create`. Buscar `error CS` y el `Debug.Log` final del menú en el log.
- **Pruebas**: `... -batchmode -nographics -projectPath ... -runTests -testPlatform PlayMode [-testFilter "Clase;OtraClase"] -testResults <xml> -logFile <log>` (sin `-quit`). Resultado en el xml (`<test-run ... passed= failed=>`).
- **Fallos que YA EXISTEN en batch** (con el código original también): `Floor3EnemiesTests` (Calamites ×2, Dragonfly ×2) y `Floor3LimbsTests` (JumpingLegs ×2). Estado al 2026-11-05: 687 de 693 en verde. Confirmarlos con Unity con ventana.
- **No** hay referencia a Input System en el ensamblado de pruebas (no vale `InputTestFixture`); una prueba que carga una escena con `LoadSceneAsyncInPlayMode` deja estado global y rompe otras (teleportes, mira, interfaz): no dejarlas en el repositorio.
- **Escenas**: `TryCreateSceneFromTemplate` no sobrescribe una existente y no avisa (borrarla antes con `AssetDatabase.DeleteAsset`); los assets cargados antes de crear la escena hay que recargarlos después.
- Con **varios `python -` heredoc seguidos** o `&&` mal puestos el heredoc se rompe; escribir los scripts de edición en un `.py` del scratchpad y llamarlos. Los `.cs` del proyecto son CRLF: editar con `newline=''` y reemplazar `\n` por `\r\n`.

**En un ordenador nuevo:** Git LFS instalado (`git lfs install`), `git config user.name/email` local, abrir el proyecto con Unity 6000.6.2f1; los assets generados (Resources/*Art.asset, escenas Test_Floor3*, Floor_3_Themes) van en el repositorio, no hace falta repetir menús salvo que cambie el arte (entonces el menú de importación que toque).
