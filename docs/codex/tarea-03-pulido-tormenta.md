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
- Hecho:
- Archivos tocados:
- Pruebas (cuántas pasan):
- Dudas / no comprobado:
