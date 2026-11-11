# Tarea 02 — Muerte de la Meganeura y final de la tormenta (7e-2)

- **Rama:** `codex/7e-2-muerte` (desde `feature/floor-3`, que ya incluye la tarea 01)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Hoy, al morir, la Meganeura solo reproduce `DeathFall` donde está (provisional, `MeganeuraBoss.OnDied`). Debe tener una muerte con puesta en escena:
1. Se le **desprende el ala trasera izquierda** (el ala cae girando por su cuenta).
2. **Cae en espiral** hacia una charca.
3. **Se estrella en la charca** (salpicadura) y queda **flotando muerta** sobre el agua, con ondas.
4. **La tormenta amaina**: se va la oscuridad y la lluvia, cae una llovizna, sale vapor del suelo.
5. La **recompensa** sigue saliendo como hasta ahora (ver más abajo) y todo queda limpio (huevos, ninfas, marcas, rayos).

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/meganeura/README.md`: tabla «6e-3: momentos» (`DeathFall`, `WingDetached`, `Dead`, `Fx_Meganeura_Splash`, `Fx_Meganeura_DeadRipple`; **versiones `_Burnt` si ardió en la fase 1**) y la sección de la tormenta (`Fx_Storm_Drizzle`, `Fx_Storm_Steam`, `Fx_Storm_RainSplash/Ripple`).
- `Scripts/Enemies/MeganeuraBoss.cs` → `OnDied` (l.~323) y `PlayBody`/`ShowBody`; `MeganeuraBoss.Scenery.cs` (las charcas fijas, `EnsurePuddles`); `MeganeuraBoss.Phase3.cs` (las 3 charcas que crecen en la F3; cómo se guarda la lista de charcas); `MeganeuraStorm.cs` (`Calm`, `SetRain`, `SetAsh`, `Darken`, capas y órdenes de dibujo).
- Cómo se crean y registran las hojas sueltas/efectos: `Editor/MeganeuraBossSetup.cs` y `Editor/ArtSetup.BossMeganeura.cs` (los efectos de una sola fila «se cortan al usarlos»; imita cómo se usan `Fx_Meganeura_Crash`, `Fx_Meganeura_Screech` y los efectos de `effects`).
- `Scripts/Mutations/MutationDrop.cs` (la recompensa usa `IBoss.ArenaCenter` al morir el jefe; **no la cambies**, solo comprueba que sigue saliendo).

## Qué hacer
1. **Secuencia de muerte** en un archivo nuevo `MeganeuraBoss.Death.cs` (partial), llamada desde `OnDied` en lugar del `PlayBody("DeathFall")` provisional. Corrutina `DeathRoutine`:
   - Entra en `Mode.Dead` (ya lo hace `OnDied`): sin colisión, sin ataques, `StopFight`, `ClearSummons`, fuego apagado (todo eso ya está; consérvalo).
   - Si estaba **en el suelo** (posada, boca arriba…), primero **se alza** hasta la altura de vuelo normal en ~0,6 s (lerp de `Altitude`) para poder caer; si ya estaba alta, sigue desde ahí.
   - **Ala:** al empezar `DeathFall`, crea un objeto hijo/independiente con `Meganeura_WingDetached` (8 cuadros, 70 ms, celda 128×128, no en bucle: al acabar, se queda en el suelo o se desvanece en 1 s). Sale de la posición del cuerpo y cae con su propia animación; su sombra no hace falta.
   - **Espiral:** `DeathFall` (8 cuadros, 90 ms, una sola fila, gira ella sola). La posición baja de su altura a 0 en `DeathDuration` = **2,2 s** (constante provisional) con una espiral horizontal suave (radio inicial ~1,5 u que se cierra a 0) hacia el **punto de la charca elegida**. La sombra (`shadow`) sigue en el suelo y se acerca al punto de impacto.
   - **Charca elegida:** la charca de la sala **más cercana a donde estaba** (entre todas las que existan en ese momento, incluidas las de la fase 3). Si no hay ninguna (no debería), cae en el centro de la sala.
   - **Impacto:** al llegar, `Fx_Meganeura_Splash` (7 cuadros, 128×96, **el agua está en y = 70 de la celda**: alinéalo con la superficie de la charca) y luego queda el cuerpo `Meganeura_Dead` (1 cuadro × 9 filas de dirección; usa la fila de la dirección `Side` = la última de la espiral) apagado y flotando sobre el agua, con `Fx_Meganeura_DeadRipple` (4 cuadros, 200 ms, bucle, 160×80) **debajo**. Se queda así hasta que se descarta la sala. Si la charca estaba electrificada, no importa.
   - Usa siempre la **variante `_Burnt`** de las tres hojas si la Meganeura ardió en la fase 1 (hay una propiedad para ello; la usa el resto del código con `PlayBody`).
2. **La tormenta amaina** (en `MeganeuraStorm`, ampliando `Calm`; hoy solo apaga ceniza y lluvia y aclara en 2 s):
   - Aclarado en **3 s** (parámetro de `Calm`).
   - La lluvia fuerte se cambia por la **llovizna** `Fx_Storm_Drizzle` (mismo mosaico y capa de la lluvia, 4 cuadros 128×128, 60 ms) durante ~8 s y luego se apaga.
   - **Vapor** `Fx_Storm_Steam` (6 cuadros, 48×48, 120 ms, bucle): unas 10 volutas repartidas al azar por el suelo de la sala, durante ~6 s, en capa por encima del suelo y por debajo del jugador. Se retiran solas.
   - Si no había tormenta (murió en la fase 1 o 2) no se crea llovizna ni vapor, solo la secuencia de caída; `storm` puede ser null.
3. **Constantes** al inicio de `MeganeuraBoss.Death.cs` y `MeganeuraStorm.cs`: `DeathDuration` 2,2 s, `RiseBeforeFall` 0,6 s, `SpiralRadius` 1,5 u, llovizna 8 s, vapor 6 s / 10 volutas. Márcalas provisionales.
4. **Menú:** el arte nuevo (`WingDetached`, `Dead`, `DeadRipple`, `Splash`, `Drizzle`, `Steam`) ya está en el origen y las hojas con direcciones ya están importadas (`MeganeuraArt.asset`). Si algo de lo nuevo no está importado, amplía el menú existente `Primordia > Boss > Crear Meganeura y escena de prueba` (o `Importar hojas de la Meganeura`) para que lo incluya, sin romper lo ya hecho.
5. **Pruebas** (añadir a `MeganeuraBossTests`): (a) al morir en el aire se desprende el ala (existe el objeto `WingDetached`) y el cuerpo termina en `Dead` en la posición de la charca más cercana; (b) al morir en el suelo primero sube; (c) tras morir, los huevos, ninfas y marcas desaparecen y no queda corrutina de combate; (d) con tormenta, `Calm` deja la oscuridad a 0, sin lluvia fuerte y con llovizna y vapor activos; sin tormenta no falla; (e) la recompensa (`MutationDrop`) sigue apareciendo en `ArenaCenter`; (f) variante `_Burnt` si había ardido (probar con el estado `Burnt` forzado).

## Números y reglas (cerrados)
- Ala trasera **izquierda**, **solo una**. La caída dura **2,2 s**; no debe poder interrumpirse ni dañarse (ya está muerta).
- La recompensa **no se mueve ni se retrasa** en esta tarea (la usuaria decidirá después si debe salir al acabar la caída).
- Todo el resto de números son provisionales y se ajustan a ojo.

## Archivos que puede tocar
- `Scripts/Enemies/MeganeuraBoss.cs` (solo `OnDied`), `MeganeuraBoss.Death.cs` (nuevo), `MeganeuraStorm.cs`, y lo mínimo de `MeganeuraBoss.Scenery.cs`/`Phase3.cs` si hace falta exponer la lista de charcas.
- `Editor/MeganeuraBossSetup.cs`, `Editor/ArtSetup.BossMeganeura.cs` (solo si falta importar algo).
- `Tests/PlayMode/MeganeuraBossTests.cs`.

## Fuera de alcance (NO hacer)
- Sonido (paso 8). Dibujar o retocar arte. Tocar la barra de vida (tarea 01, ya hecha). Cambiar la lógica de combate, la recompensa o las fases.
- Cámara lenta, zoom o cinemática de muerte (si te parece necesario, **pregunta**).

## Cómo probar
- Menú que corresponda y luego `MeganeuraBossTests` (hoy 28, deben seguir verdes) y `BossHealthBarTests`.
- A ojo (usuaria), en `Test_BossMeganeura`: matarla en fase 1 (con y sin haberla quemado), en fase 2 (posada) y en fase 3 (con tormenta). Debe caer a una charca, flotar, y en la F3 la tormenta debe amainar.

## Resultado (lo rellena Codex)
- Hecho: secuencia independiente en `MeganeuraCorpse`, definido en `MeganeuraBoss.Death.cs`. La usuaria autorizó separar cuerpo y sombra bajo la sala para sobrevivir a la destrucción de `Enemy` a los 1,2 s. El objeto recibe los datos y ejecuta su propia `DeathRoutine`: subida previa de 0,6 s si está baja, un ala desprendida que queda en el suelo, caída en espiral de 2,2 s hacia la charca más cercana (incluidas las nuevas de F3; centro de sala si no hay charcas), salpicadura alineada al agua y restos `Dead` mirando a `Side`, con ondas debajo. Las tres hojas de muerte usan `_Burnt` si corresponde. Los restos se descartan con la sala.
- Tormenta: se confirma que cuelga de la sala o del padre del jefe, no del jefe. Al morir aclara en 3 s, sustituye la lluvia fuerte por llovizna durante 8 s y crea 10 volutas de vapor durante 6 s, bajo el jugador. Sin tormenta no se crea ambiente. Se detienen todas las corrutinas del jefe, además de conservar `StopFight` y `ClearSummons`, y se retiran marcas, rayos, anillos y efectos eléctricos pendientes. No se cambian `Enemy` ni `MutationDrop`: la recompensa sigue siendo inmediata y se coloca respecto a `ArenaCenter`.
- Menú: añadido y ejecutado `Primordia > Boss > Importar efectos de muerte de la Meganeura`, que corta los cuatro efectos sueltos y actualiza solo el prefab, sin reconstruir la escena. La creación completa del jefe también configura esos efectos. Las hojas direccionales de muerte ya estaban importadas y se reutilizan.
- Archivos tocados (bajo `Assets/_Primordia/`): `Scripts/Enemies/MeganeuraBoss.cs` (solo `OnDied`), nuevo `Scripts/Enemies/MeganeuraBoss.Death.cs` y su `.meta`, `Scripts/Enemies/MeganeuraStorm.cs`, `Editor/MeganeuraBossSetup.cs`, `Tests/PlayMode/MeganeuraBossTests.cs`. Generados mediante el menú: asignación en `Prefabs/Enemies/Boss_Meganeura.prefab` y metadatos de importación de `Fx_Meganeura_Splash.png`, `Fx_Meganeura_DeadRipple.png`, `Fx_Storm_Drizzle.png` y `Fx_Storm_Steam.png`. También se completa este informe. Los cinco `.cs` mantienen CRLF; ningún PNG se modificó y no se ejecutaron generadores.
- Pruebas (cuántas pasan): compilación e importación con Unity 6000.6.2f1 completadas (salida 0). PlayMode: **47/47 pasan**, `MeganeuraBossTests` **34/34** (28 anteriores y 6 nuevas), `BossHealthBarTests` **13/13**. Las nuevas pruebas comprueban persistencia tras destruir `Enemy`, elección de una charca nueva más cercana, las tres variantes `_Burnt`, subida previa desde el suelo, limpieza de invocaciones y ataques, retirada temporizada de oscuridad/llovizna/vapor y recompensa inmediata en `ArenaCenter`. Comprobados los cuatro PNG byte a byte contra el origen aprobado y sus referencias en el prefab. Una llamada incorrecta a `MeganeuraEgg.Spawn` en la prueba nueva se corrigió antes de la ejecución final; no quedan errores de compilación ni fallos de pruebas.
- Logs y resultados: fuera del repositorio, en `%TEMP%/Primordia-Codex-7e-2/`: `importacion-final.log`, `pruebas.log` y `resultados.xml`. Se comprobó que no hubiera otra instancia Unity antes de cada lanzamiento. Sin commit, push ni merge.
- Dudas / no comprobado: pendiente revisión visual de la usuaria en `Test_BossMeganeura`, matándola en F1 normal/quemada, F2 posada y F3 con tormenta. No se reconstruyó la escena ni se ejecutó la suite completa; se ejecutaron las dos clases solicitadas. Los tiempos y la trayectoria provisionales pueden necesitar ajuste visual. No se añadieron sonidos, efectos de cámara ni cambios de combate, recompensa, fases o barra.
