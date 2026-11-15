# Tarea 13 — Megarachne: importar arte, sala del Nido y movimiento (M1)

- **Rama:** `codex/mega-1-base` (desde `feature/boss-megarachne`)
- **Escrita por:** Claude (2026-11-15) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Una escena `Test_BossMegarachne` donde, al entrar, hay una sala de 32×20 u con un árbol empapado en el centro (copa translúcida cuando el jugador/enemigos pasan debajo), 5 cables a tocones/Sigillarias, y la Megarachne (150 PV) que se mueve: camina por el suelo hacia el jugador y trepa/corre por los cables. Todavía SIN ataques, fuego, crías ni enfado.

## Contexto mínimo (léelo)
- `AGENTS.md`, `docs/codex/megarachne-plan.md`.
- `docs/multimedia-temp/floor3-bosses/megarachne/README.md` (hojas, filas, pivotes, anclas) y `docs/memoria-claude/primordia-boss-megarachne.md` (diseño).
- Como modelo de estructura: `Assets/_Primordia/Scripts/Enemies/LepidodendronBoss*.cs`, `Assets/_Primordia/Editor/` del Lepidodendron y de la Arthropleura (importador de hojas, setup de escena, menú `Primordia > Boss`), y `Assets/_Primordia/Tests/PlayMode/` de esos jefes.

## Qué hacer
1. Importador por menú `Primordia > Art > Importar Megarachne` (patrón de `ArtSetup.ImportArthropleuraSheets`): hojas de `Sheets/` y `Shadows/` (Idle, Walk, Weave, Hurt, Stunned, Recover, Drop, Climb, DropWarn, Land, Fall), `Nido/` (árbol Wet, Web, Eyes) y `Effects/` de cable (Cable_Normal, Cable_Thin). Respetar 9 filas con Flip X y 1 dirección para Drop/Climb.
2. `MegarachneBoss` (partial, `MegarachneBoss.cs` + `.Cables.cs`): `Enemy`/vida 150, `BossDamageLimiter` como la Meganeura, 16 direcciones, estados `Ground` y `OnCable`. Invulnerable en `OnCable`; vulnerable en `Ground`.
3. Sala (menú `Primordia > Boss > Crear sala Megarachne` y prefab del jefe): suelo del Bosque humeante, árbol central con colisión en el tronco, copa translúcida (alfa ~0,45) si hay jugador o enemigo bajo ella, 5 anclas (puntas de la copa) → 5 puntos de tocón/Sigillaria; cable = `LineRenderer` o sprites repetidos de `Cable_Normal` (~1 u de ancho), solo visual.
4. Movimiento: en `Ground` camina hacia el jugador a ~5 u/s; cada ~6 s sube a un cable (`Climb`), corre por él a ~7 u/s hasta otra ancla y baja (`Drop`). Sin ataques todavía; estas transiciones son provisionales.
5. Escena `Test_BossMegarachne` con teclas: Ctrl+Mayús+F1 vida al 51 %, F2 forzar subida al cable, F3 forzar bajada.
6. Pruebas PlayMode `MegarachneBossTests` (vida, invulnerable en cable/vulnerable en suelo, sube y baja, copa translúcida) y que no queden errores de consola.

## Números y reglas (cerrados)
Vida 150, celda 128, patas ~6 u, cámara ×1, sala 32×20 u. Velocidades de arriba son PROVISIONALES.

## Archivos que puede tocar
- `Assets/_Primordia/Scripts/Enemies/MegarachneBoss*.cs`, `Assets/_Primordia/Editor/*Megarachne*`, `Assets/_Primordia/Tests/PlayMode/MegarachneBossTests.cs`, prefabs/escena/arte importado de Megarachne.

## Fuera de alcance (NO hacer)
Ataques, red, saco, crías, fuego, enfado, entrada, barra, muerte, recompensa, sonido, enganche a Floor_3. No ejecutar los generadores `.py`. No crear arte.

## Cómo probar
Menú de importación, menú de sala, abrir `Test_BossMegarachne`, jugar: ella persigue en el suelo y se sube/baja de cables; en cable no recibe daño. Lanzar `MegarachneBossTests` (batchmode permitido, ver `primordia-unity-batchmode.md`).

## Resultado
- Hecho: implementado M1 en `codex/mega-1-base` (descendiente de `feature/boss-megarachne`), sin commit, push ni merge. Menús `Primordia > Art > Importar Megarachne` y `Primordia > Boss > Crear sala Megarachne`; 22 hojas direccionales de cuerpo/sombra (9 filas, salvo Drop/Climb de una fila), árbol Wet/Web/Eyes y cables Normal/Thin. No se modificaron PNG ni se ejecutaron generadores.
- Jefe: 150 PV, `Enemy` y `BossDamageLimiter`, sin daño por contacto ni ataques; persecución a 5 u/s, espera de 6 s en suelo, aproximación a un extremo accesible, Climb, recorrido a 7 u/s y Drop. Invulnerable durante el recorrido elevado; vulnerable en suelo. Sombra en suelo e hilo hasta las hileras. Velocidades/transiciones marcadas provisionales. Ctrl+Mayús+F1 deja 77 PV (redondeo superior del 51 % de 150), F2 solicita subida y F3 solicita bajada; solo en la escena de prueba.
- Sala: suelo 32×20 u con tema Bosque humeante y cámara base ×1; tronco sólido, copa y seda al 0,45 cuando un jugador/enemigo del suelo pasa debajo, pie del árbol opaco mediante recortes de la misma textura, cinco cables visuales desde las anclas del README hasta Sigillarias. Escena independiente, sin conexión a Floor_3, presentación, barra ni recompensa propia.
- Archivos tocados:
  - `Assets/_Primordia/Scripts/Enemies/MegarachneBoss.cs`, `MegarachneBoss.Cables.cs`, `MegarachneBossCanopy.cs` y sus `.meta`.
  - `Assets/_Primordia/Editor/ArtSetup.BossMegarachne.cs`, `MegarachneBossSetup.cs` y sus `.meta`.
  - `Assets/_Primordia/Tests/PlayMode/MegarachneBossTests.cs` y su `.meta`.
  - `Assets/_Primordia/Data/Bosses/MegarachneArt.asset`, `MegarachneCable.mat`; `Data/Enemies/Floor3/Enemy_MegarachneBoss.asset`; `Data/Rooms/Floor3/Room_Boss_Megarachne.asset`; `Data/Rooms/Floor_BossMegarachneTest.asset`, con sus `.meta`.
  - `Assets/_Primordia/Prefabs/Enemies/Boss_Megarachne.prefab`, `Assets/_Primordia/Scenes/Test_BossMegarachne.unity` y sus `.meta`.
  - 27 `.png.meta` del arte de Megarachne: las 22 hojas de cuerpo/sombra indicadas, Wet/Web/Eyes y Normal/Thin. Este archivo de tarea.
- Pruebas: menú de creación ejecutado en Unity 6000.6.2f1 batchmode, compilación sin errores CS. `MegarachneBossTests`: **7 de 7 pasan**, 0 fallos, en PlayMode con recarga de dominio desactivada. Cubren vida/limitador/ausencia de contacto, hojas y direcciones, persecución, subida y bajada natural sin teletransporte, invulnerabilidad/vulnerabilidad, ciclo automático y bajada forzada, transparencia con jugador y enemigo y restauración de opacidad. La primera ejecución dio 5/7 y permitió corregir la elección de un cable cuyo acceso estaba bloqueado por el tronco. Registros: `Logs/Megarachne-setup.log`, `Logs/Megarachne-tests.log`, `Logs/Megarachne-tests.xml`.
- Reimportación: menú ejecutado de nuevo correctamente (`Logs/Megarachne-reimport.log`); los identificadores de los sprites de copa/tronco y las referencias del prefab se conservan. C# nuevos verificados con CRLF. Los espacios finales que señala `git diff --check` están en campos vacíos de `.meta` serializados por Unity.
- Dudas / no comprobado: pendiente revisión visual jugando en el editor y pulsación manual de los atajos; no se ha lanzado la suite general. `DebugCheats` existente también interpreta Ctrl+Mayús+F1/F2/F3 como atajos de Arthropleura y puede mostrar «No hay Arthropleura activa», aunque los atajos de Megarachne se ejecutan; no se ha tocado ese archivo, fuera del alcance. El primer arranque aislado falló por licencia; se ejecutó correctamente fuera del aislamiento tras la autorización de batchmode. No se modificó `ProjectSettings/ProjectSettings.asset` ni los archivos ajenos sin seguimiento.
