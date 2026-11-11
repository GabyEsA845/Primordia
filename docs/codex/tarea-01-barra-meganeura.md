# Tarea 01 — Barra de vida «alas» de la Meganeura (7e-1, piloto)

- **Rama:** `codex/7e-1-barra` (desde `feature/floor-3`)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** revisada (2026-11-11); corregido: los ojos se conservan al volar (inmune)

## Objetivo
La Meganeura usa su barra de vida propia (las alas forman el marco) en vez de la lisa: abajo y centrada en la pantalla, con un marco/relleno/ojos distintos por fase, muescas en el 65 % y el 30 %, relleno de nubes mientras es invulnerable, efecto al cruzar cada muesca y temblor con chispas en el último 10 %. El arte ya está dibujado y aprobado; solo hay que importarlo y programarlo.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/meganeura/README.md`, sección «6g: barra de vida» (tabla de hojas, medidas, qué va en cada fase).
- Código a imitar: `Assets/_Primordia/Editor/ArtSetup.BossEurypterid.cs` → `CreateEurypteridBarSkin()` y la línea que hace `AddComponent<BossBarAppearance>().Configure(barSkin)` (~l.111).
- `Assets/_Primordia/Scripts/UI/BossBarSkin.cs`, `BossBarAppearance.cs`, `BossHealthBar.cs` (clase interna `SkinView`), `Scripts/Enemies/IBoss.cs` (`IPhasedBoss`).
- `Assets/_Primordia/Scripts/Enemies/MeganeuraBoss.cs` (`BarStyle`) y `MeganeuraBoss.Phase2.cs` (ya tiene `PhaseIndex`).
- Arte (origen): `docs/multimedia-temp/floor3-bosses/meganeura/BossBar/UI_BossBar_Meganeura_*.png`.

## Qué hacer
1. **Importar y crear el skin** — nuevo `Assets/_Primordia/Editor/ArtSetup.BossMeganeuraBar.cs` (partial de `ArtSetup`, como el del Euriptérido) con `CreateMeganeuraBarSkin()`, que importa las hojas a `UIArtFolder/BossBar/Meganeura` y crea `Data/Bosses/BossBar_Meganeura.asset`. Medidas: marco 300×32; ventana `RectInt(43, 12, 241, 8)`; celda del relleno 241×8; muescas 8×32.
   - Fases (`PhaseLook`): F1 = `Frame` + `Fill_Teal` + `Eyes_Hunt`; F2 = `Frame_Torn1` + `Fill_Rage` + `Eyes_Rage`; F3 = `Frame_Torn2` + `Fill_Storm` + `Eyes_Storm`.
   - Inmune (`dark`): relleno `Fill_Cloud`; ojos de la fase (corregido tras probar); marco = el de la fase en curso (no hay marco en silueta: deja `frameDark` = `Frame`).
   - `Back`, `Fill_Trail`, `Notch`, `Notch_Broken` (sin `Fx_NotchBreak`: usa un array vacío).
2. **Menú** `Primordia > Boss > Crear barra de la Meganeura`: ejecuta lo anterior y pone `BossBarAppearance` (con el skin) en el prefab `Boss_Meganeura` (rehaz la asignación si ya existe, sin duplicar el componente). Añade la llamada también en `MeganeuraBossSetup` para que «Crear Meganeura y escena de prueba» la incluya.
3. **Fases en el jefe:** `MeganeuraBoss` implementa `IPhasedBoss`: `PhaseIndex` (ya existe, devuelve 0/1/2) y `PhaseThresholds = { 0.65f, 0.30f }`. Usa las constantes de umbral que ya existan en `MeganeuraBoss.Fight.cs`/`Phase2.cs`/`Phase3.cs` en vez de repetir los números.
4. **Posición abajo:** la barra ahora sale arriba (`SkinTop` en `BossHealthBar`). Añade a `BossBarSkin` un campo `[SerializeField] bool bottomAnchored` (propiedad `BottomAnchored`) y haz que `SkinView` ancle abajo-centro (`anchorMin/Max/pivot = (0.5, 0)`, `anchoredPosition = (0, +margen)`) cuando sea true; margen 24 px (constante). El Euriptérido, el Anomalocaris y la Ameba **no cambian**. Para la Meganeura, `bottomAnchored = true`.
5. **Efectos por muesca:** hoy `BossBarSkin` solo admite un efecto de cambio de fase (`phaseChange`). Hazlo por fase sin romper lo anterior: añade `Sprite[][]`/clase serializable equivalente (`phaseChangeByPhase`) y úsalo si hay entrada para esa fase; si no, el efecto único de siempre. Meganeura: al cruzar el 65 % → `Fx_EggBurst` (6 cuadros, 80 ms); al cruzar el 30 % → `Fx_BoltStrike` (5 cuadros, 70 ms). Ambos del tamaño del marco (300×32).
6. **Último 10 %:** mientras `Health` ≤ 10 % de la vida, `SkinView` reproduce `Fx_LowSparks` (4 cuadros, 90 ms, bucle, encima del marco) y hace temblar el marco ±1 px (reutiliza el mecanismo de `shake` de `BossHealthBar` si encaja). Añade al skin un campo para esas chispas (vacío = nada, así el resto de jefes no cambia) y una constante `LowHealthFraction = 0.10f`.
7. **Pruebas** en `Assets/_Primordia/Tests/PlayMode/BossHealthBarTests.cs` (o un archivo nuevo `MeganeuraBarTests.cs`): (a) la barra de la Meganeura usa el skin y el `PhaseIndex` cambia el marco/relleno; (b) en alto/invulnerable el relleno es el de nubes; (c) anclada abajo (posición y del marco en la mitad inferior); (d) a ≤10 % se activan las chispas; (e) un jefe sin esos campos (Euriptérido) se ve como antes (las pruebas que ya existen de `BossHealthBarTests` siguen en verde).

## Números y reglas (cerrados)
- Umbrales 0,65 y 0,30 de la vida; último 10 % = 0,10. Margen inferior 24 px (provisional). Todo lo demás está en la tabla del README 6g.
- La barra va en la capa de interfaz (por encima de la oscuridad de la tormenta, que es del mundo): no hace falta tocar órdenes de dibujo.

## Archivos que puede tocar
- `Editor/ArtSetup.BossMeganeuraBar.cs` (nuevo), `Editor/MeganeuraBossSetup.cs` (llamada).
- `Scripts/UI/BossBarSkin.cs`, `Scripts/UI/BossHealthBar.cs` (campos nuevos, retrocompatibles).
- `Scripts/Enemies/MeganeuraBoss*.cs` (solo `IPhasedBoss`).
- Tests: `Tests/PlayMode/BossHealthBarTests.cs` o archivo nuevo.
- Se generan: `Data/Bosses/BossBar_Meganeura.asset`, sprites importados, `Prefabs/.../Boss_Meganeura.prefab`.

## Fuera de alcance (NO hacer)
- No dibujar ni modificar PNG. No tocar los generadores `.py`.
- No tocar la lógica de combate de la Meganeura ni sus constantes (7e-2 en adelante: muerte, recompensa).
- No cambiar el aspecto de las barras de los otros jefes.

## Cómo probar
- Menú `Primordia > Boss > Crear barra de la Meganeura` y luego pruebas `BossHealthBarTests` + `MeganeuraBossTests` (hoy 24 en esta última; deben seguir verdes).
- A ojo (usuaria): escena `Test_BossMeganeura`; la barra debe verse abajo, cambiar de marco en las fases (bajando la vida al 65 % y al 30 %), mostrar nubes cuando vuela alta y temblar con chispas al final.

## Resultado
- Hecho: implementada la barra de alas con tres aspectos, nubes sin ojos durante la inmunidad conservando el marco de la fase, muescas y efectos independientes por fase. Se respetan los tiempos del README para rellenos, ojos y efectos. La barra se ancla al contenedor de pantalla, abajo-centro a 24 px (provisional); las chispas y el temblor de ±1 px empiezan con la vida real al 10 %, sin esperar a la barra fantasma. Se oculta y destruye correctamente el aspecto inferior al desaparecer el jefe o la interfaz.
- Menú: `Primordia > Boss > Crear barra de la Meganeura` creado y ejecutado; importa el arte y asigna un solo BossBarAppearance al prefab, conservando los GUID al repetir. Integrado también en la creación completa de la Meganeura. IPhasedBoss reutiliza torn1Threshold y torn2Threshold; sin cambios de combate.
- Archivos tocados (bajo Assets/_Primordia): Editor/ArtSetup.BossMeganeuraBar.cs (+ .meta), Editor/MeganeuraBossSetup.cs, Scripts/UI/BossBarSkin.cs, Scripts/UI/BossHealthBar.cs, Scripts/Enemies/MeganeuraBoss.cs y Tests/PlayMode/BossHealthBarTests.cs. Generados: Data/Bosses/BossBar_Meganeura.asset (+ .meta), Art/UI/BossBar/Meganeura (17 PNG y sus .meta, más el .meta de carpeta) y asignación en Prefabs/Enemies/Boss_Meganeura.prefab. Este informe se escribe en la tarea. Los seis .cs conservan CRLF.
- Pruebas: compilación con dotnet de los ensamblados de juego, editor y pruebas: 0 errores (advertencias existentes de obsolescencia). Ejecución final PlayMode (Unity exit 0, resultados-final.xml): 41/41 pasan; también pasaron en la primera ejecución. Desglose: 41/41 pasan, BossHealthBarTests 13/13 (6 nuevas) y MeganeuraBossTests 28/28. Comprobados los 17 PNG importados byte a byte contra el origen aprobado, sus .meta y un único BossBarAppearance en el prefab.
- Logs y resultados: pruebas-final.log y resultados-final.xml fuera del repositorio en %TEMP%/Primordia-Codex-7e-1/. La primera ejecución guardó también el XML en %USERPROFILE%/AppData/LocalLow/DefaultCompany/Primordia/TestResults.xml; su copia de Temp del proyecto fue limpiada por Unity en el arranque posterior. Sin commit, push ni merge.
- Dudas / no comprobado: pendiente revisión visual de la usuaria en Test_BossMeganeura. No se ejecutó el menú completo que reconstruye la escena, solo el menú de la barra. No se modificaron los cambios previos de docs/memoria-claude ni AGENTS.md. No se ejecutaron generadores ni se modificó el arte aprobado.
