# Tarea 03 — Pulido de la tormenta de la Meganeura (7d, lo que quedó pendiente)

- **Rama:** `codex/7e-3-tormenta` (desde `feature/floor-3`, que ya incluye las tareas 01 y 02)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Tres detalles de la fase 3 (la tormenta) que el diseño pedía y no se programaron:
1. **La lluvia apaga fuegos:** los del suelo y el quemado del jugador.
2. **Silueta con los relámpagos:** cuando cae un rayo y la Meganeura vuela alta, se ve su silueta a contraluz un instante.
3. **Sombra de nubes:** una sombra grande que se desliza despacio por el suelo mientras dura la tormenta.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/meganeura/README.md`: tabla de la tormenta (`Meganeura_Silhouette_Fly`, `Fx_Storm_CloudShadow`) y las reglas de capas («lo que brilla va encima de la oscuridad y del destello»).
- `Scripts/Enemies/MeganeuraStorm.cs` (capas, `Raining`, `SetRain`, `Flash`, `Calm`) y `MeganeuraBoss.Phase3.cs` (dónde se llama a `storm.Flash(...)`: l.~132, 233, 288, 384; entrada de la fase 3).
- Fuego del suelo: `Scripts/Rooms/GroundFire.cs` (`All`, `IsBurning`, `Spawn`, `endTime`). Quemado del jugador: `Scripts/Rooms/PlayerBurn.cs` (`Douse(amount)`) y cómo lo usa `Scripts/Rooms/GroundPuddle.cs` (`BurnDouseSpeed`, `CheckEvery`).
- `MeganeuraBoss.cs`: `IsHigh`, `ShowBody`, el cuerpo y la sombra (`bodyRenderer`, `shadowRenderer`), y `DirectionalSprite`/`DirectionalArt` para las hojas con 9 direcciones.
- Arte (ya importado en `Art/Floor3/floor3-bosses/meganeura/`): `Sheets/Meganeura_Silhouette_Fly` (4 cuadros × 9 filas, celda 192, = `Fly`) y `Effects/Fx_Storm_CloudShadow` (1 cuadro, 256×256, para mosaico). Si no están cortados como hojas, usa el menú existente de importación o amplíalo.

## Qué hacer
1. **Lluvia que apaga** (`MeganeuraStorm`, mientras `Raining` sea true):
   - **Fuegos del suelo:** cada `GroundFire` que arda dentro de la sala se acorta: su tiempo restante baja a **×3 de velocidad** (cada segundo real descuenta 3). No hace falta tocar los fuegos de otras salas (la lluvia solo existe aquí). Añade a `GroundFire` un método público mínimo (p. ej. `Quench(float seconds)`), sin cambiar su comportamiento normal.
   - **Jugador:** si el jugador está quemándose (`PlayerBurn`), la lluvia le aplica `Douse` a **1,5 de barra por segundo** (provisional; el lodo profundo del Eryops es 3). Una comprobación cada 0,25 s basta.
   - No afecta a los `Burnt` permanentes de las alas de la Meganeura (ese estado no se apaga) ni a enemigos.
2. **Silueta con relámpagos:** cuando se llama a `storm.Flash(alpha)` con `alpha ≥ 0,5` **y la Meganeura está en el aire** (`IsHigh`), muestra `Meganeura_Silhouette_Fly` en su posición y dirección actuales durante **0,35 s** (los 4 cuadros a 90 ms), **por encima de la oscuridad y del destello** (orden ≥ 26) y sin colisión. Usa la variante de alas de la fase (`Torn2`) si existe.
   - Además, en la fase 3 mientras vuela alta, el **cuerpo normal queda oculto** (solo se ve su sombra en el suelo y las marcas), y los destellos son lo único que la revela. Constante `HideBodyWhenHighInStorm = true` al inicio del archivo (para poder desactivarlo de un cambio si la usuaria lo prefiere). Cuando baja a ras de suelo, vuelve a verse con normalidad.
3. **Sombra de nubes:** al empezar la tormenta (misma entrada que la oscuridad), crea una capa en mosaico con `Fx_Storm_CloudShadow` que se **desliza a 0,6 u/s** en diagonal (provisional) sobre el suelo de la sala, **entre el suelo y los personajes** (orden por debajo de la oscuridad, p. ej. 18; revisa los órdenes que ya existen). Opacidad la del propio PNG. Se retira en `Calm` desvaneciéndose en 3 s.
4. **Constantes** nombradas al inicio de `MeganeuraStorm.cs` y marcadas provisionales: velocidad de apagado del suelo, `Douse` de la lluvia, duración de la silueta, velocidad y orden de las nubes.
5. **Pruebas** (en `MeganeuraBossTests`): (a) con lluvia, un `GroundFire` acaba en menos de la mitad del tiempo normal y sin lluvia no cambia; (b) con lluvia, un `PlayerBurn` con barra baja se apaga; sin lluvia no; (c) un destello de ≥ 0,5 con la Meganeura en el aire crea la silueta, y con ella en el suelo no; (d) en la fase 3 alta el cuerpo normal está oculto y al bajar se muestra; (e) la sombra de nubes existe durante la tormenta, se mueve y desaparece tras `Calm`.

## Números y reglas (cerrados)
- Todo lo de arriba es provisional salvo las reglas de capas del README (lo que brilla va encima de la oscuridad y el destello).
- La lluvia solo cuenta en la fase 3.

## Archivos que puede tocar
- `Scripts/Enemies/MeganeuraStorm.cs`, `MeganeuraBoss.Phase3.cs`, `MeganeuraBoss.cs` (solo lo necesario para ocultar el cuerpo en alto y mostrar la silueta), `MeganeuraBoss.Death.cs` si la muerte debe limpiar lo nuevo.
- `Scripts/Rooms/GroundFire.cs` (solo `Quench`), `Scripts/Rooms/PlayerBurn.cs` solo si hace falta un acceso mínimo.
- `Editor/MeganeuraBossSetup.cs` / `ArtSetup.BossMeganeura.cs` solo si falta importar o cortar arte.
- `Tests/PlayMode/MeganeuraBossTests.cs`.

## Fuera de alcance (NO hacer)
- Huevos, ninfas o aleteo en la fase 3 (decisión de diseño pendiente). Equilibrio de Bocas o `BossDamageLimiter`. Sonido (paso 8). Dibujar o retocar arte.
- Cambiar el combate de la fase 3 (rayos, aturdimiento, invulnerabilidad).

## Cómo probar
- Menú que corresponda y `MeganeuraBossTests` (34 hoy, deben seguir verdes) y `BossHealthBarTests`.
- A ojo (usuaria), en `Test_BossMeganeura` bajando la vida al 30 %: prende un fuego en el suelo (Mayús+F12) y quémate; la lluvia debe apagarlos. Cuando ella vuele alta, un rayo debe revelar su silueta (y sin rayo no se ve el cuerpo). Las nubes deben pasar despacio por el suelo.

## Resultado (lo rellena Codex)
- Hecho: la lluvia fuerte acelera a ×3 el desgaste de los fuegos dentro de la sala (desgaste natural más 2 s adicionales/s) mediante el método mínimo `GroundFire.Quench`. Aplica `PlayerBurn.Douse` a 1,5 de barra/s, con comprobaciones cada 0,25 s y usando el tiempo transcurrido real entre ellas. No afecta a otras salas, enemigos ni al estado permanente `Burnt` de la Meganeura; al cesar la lluvia fuerte deja de apagar, también durante la llovizna final.
- Silueta: `MeganeuraStorm.Flash` con alfa ≥ 0,5 revela al jefe solo si `IsHigh`, usando posición y dirección actuales, 90 ms/cuadro y retirada a los 0,35 s. Objeto sin colisión, orden 26, bajo la tormenta independiente de la sala. Se selecciona la variante de alas si existe y se usa la hoja base cuando no existe (actualmente solo hay hoja base). `HideBodyWhenHighInStorm = true` al inicio de `MeganeuraBoss.Phase3.cs` controla la ocultación del cuerpo normal en alto durante F3; vuelve a verse al bajar. `Calm` limpia cualquier silueta pendiente.
- Nubes: mosaico del PNG aprobado de 256×256, desplazamiento diagonal normalizado a 0,6 u/s, opacidad del propio PNG y desvanecimiento/retirada en 3 s al llamar a `Calm`. Orden 2: por encima del suelo/charcas (1) y bajo personajes y troncos (desde 4; jugador 10). Se descartó el ejemplo de orden 18 porque quedaría encima de los personajes. Las piezas pertenecen a la tormenta y se descartan con la sala.
- Importación: añadido y ejecutado el menú `Primordia > Boss > Importar ambiente de tormenta de la Meganeura`; corta la sombra de nubes como una sola celda de 256×256 y la asigna al prefab sin reconstruir la escena. La creación completa del jefe también la configura. La silueta direccional ya estaba correctamente importada (4 cuadros × 9 filas) y se reutiliza.
- Archivos tocados (bajo `Assets/_Primordia/`): `Scripts/Enemies/MeganeuraStorm.cs`, `MeganeuraBoss.Phase3.cs`, `MeganeuraBoss.cs` (solo visibilidad en `LateUpdate`), `Scripts/Rooms/GroundFire.cs` (solo `Quench`), `Editor/MeganeuraBossSetup.cs`, `Tests/PlayMode/MeganeuraBossTests.cs`. Generados por el menú: `Art/Floor3/floor3-bosses/meganeura/Effects/Fx_Storm_CloudShadow.png.meta` y la referencia en `Prefabs/Enemies/Boss_Meganeura.prefab`. También se completa este informe. Los seis `.cs` conservan CRLF; el PNG permanece idéntico al origen aprobado. Comprobado que no hay referencias externas a los antiguos recortes de nubes.
- Pruebas (cuántas pasan): importación y compilación con Unity 6000.6.2f1 completadas sin errores. PlayMode: **52/52 pasan**, `MeganeuraBossTests` **39/39** (34 anteriores y 5 nuevas) y `BossHealthBarTests` **13/13**. Las nuevas pruebas cubren apagado acelerado de fuegos y comportamiento sin lluvia/fuera de sala, apagado del jugador sin alterar `Burnt`, umbral del destello, dirección/posición/capa/duración y ausencia de colisión de la silueta, cuerpo oculto en alto y visible al bajar, movimiento y retirada gradual de las nubes.
- Logs y resultados: fuera del repositorio en `%TEMP%/Primordia-Codex-7e-3/`: `importacion.log`, `pruebas.log`, `resultados.xml`. Se comprobó que no hubiera otra instancia Unity antes de cada lanzamiento. Sin commit, push ni merge.
- Dudas / no comprobado: pendiente revisión visual de la usuaria en `Test_BossMeganeura` con las comprobaciones indicadas arriba. No se reconstruyó la escena ni se ejecutó la suite completa, solo las dos clases solicitadas. Las cifras visuales siguen siendo provisionales. Sin cambios de combate, daño, aturdimiento, invulnerabilidad, recompensa, sonido o arte; no se ejecutaron generadores.
