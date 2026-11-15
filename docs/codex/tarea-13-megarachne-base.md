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

## Resultado (lo rellena Codex)
- Hecho:
- Archivos tocados:
- Pruebas (cuántas pasan):
- Dudas / no comprobado:
