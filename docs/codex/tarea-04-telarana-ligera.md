# Tarea 04 — Telaraña de las Patas hilanderas (nivel III) más ligera

- **Rama:** `codex/telarana-ligera` (desde `feature/floor-3`, que ya incluye las tareas 01-03 una vez fusionadas; si la 03 aún no está fusionada, desde donde te diga Claude)
- **Escrita por:** Claude (2026-11-11) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
Cuando se forma la red (3 hilos), hoy el juego da un tirón y el dibujo queda muy cargado. Hay que: (A) quitar el tirón, (B) usar menos cuadros y (C) hacer el diseño más ligero. La mecánica (daño 4 una vez, inmoviliza 3 s a lo de dentro, contorno convexo de los 3 hilos) **no cambia**.

## Causa del tirón (ya diagnosticada)
`SilkNetArt.Build` (llamada desde `SilkNet.cs` l.~52) genera **los 9 cuadros de golpe** en el frame en que se forma la red: por cada cuadro crea un `Dictionary<Vector2Int, Color32>` con todos los píxeles (radios, anillos, rocío, contorno) y una `Texture2D` nueva. Además **las texturas nunca se destruyen** (fuga de memoria cada vez que se forma una red).

## Contexto mínimo (leer solo esto)
- `Assets/_Primordia/Scripts/Mutations/SilkNetArt.cs` (el dibujo por código) y `SilkNet.cs` (`PlayWeb`, l.~48-85).
- `Assets/_Primordia/Tests/PlayMode/Floor3LimbsTests.cs`: las pruebas que usan `SilkNetArt` / `SilkNet`.

## Qué hacer
1. **A — repartir el trabajo y no fugar memoria.** Que `Build` ya no cree todos los cuadros de golpe: construye cada cuadro **solo cuando hace falta** (al llegar la animación a él) y lo guarda, o genera uno por frame en una corrutina; a elegir, pero el primer cuadro debe estar listo en el mismo frame en que se forma la red y ninguno puede costar más que uno. Al terminar la animación de la red (o si el objeto se destruye) **destruye las texturas y sprites creados** (`Object.Destroy`). No cambies la forma de calcular el polígono ni el daño.
2. **B — menos cuadros.** `GrowFrames` 4 → **2** (crecimiento 0,5 y 1), `TwinkleFrames` 2 → **1**, `FrayFrames` 3 → **2** (0,45 y 0,8). Total 5 cuadros. Ajusta `PlayWeb` y las constantes públicas para que la duración total de la animación siga siendo la misma (reparte los tiempos de cada cuadro).
3. **C — diseño más ligero (versión «intermedia»).** En `Weave`:
   - `SpokeEvery` 14 → **22** (menos radios) y `RingSpacing` 7 → **11** (menos anillos).
   - Rocío: probabilidad 0,35 → **0,12**, y solo en los anillos pares.
   - Opacidades: radios 235 → **170**, anillos 190 → **130**. Contorno y punto central se quedan en 255.
   - Conserva la misma paleta (`Silk`, `Dew`) y el estilo pixel art (filtro `Point`, 16 px/u).
4. **Constantes** nombradas al inicio de `SilkNetArt.cs`, marcadas «provisional» (la usuaria las ajusta a ojo).
5. **Pruebas:** actualiza las de `Floor3LimbsTests` que dependan del número de cuadros, y añade: (a) tras formar la red solo existe el primer cuadro y el resto se crea con el tiempo; (b) al acabar la animación no quedan texturas de la red vivas (cuenta con `Resources.FindObjectsOfTypeAll<Texture2D>()` por nombre/hideFlags o con un contador interno de prueba); (c) el daño, la inmovilización y el contorno de la red siguen igual (las pruebas que ya existen deben seguir verdes).

## Fuera de alcance (NO hacer)
- No cambiar daño, inmovilización, máx. de hilos ni tiempos de la mutación. No tocar el arte de los hilos (`SilkThread`). No dibujar PNG. No tocar nada de la Meganeura.

## Cómo probar
- Pruebas `Floor3LimbsTests` y `Floor3SkinTests` (para no romper nada vecino). Recuerda los fallos que ya existían (`JumpingLegs` ×2 en `Floor3LimbsTests`, ver `AGENTS.md`).
- A ojo (usuaria): Patas hilanderas nivel III, soltar 3 hilos: la red debe aparecer **sin tirón**, más sobria y con menos rocío.

## Resultado
- Hecho: generación diferida de los cinco cuadros (2 de crecimiento, 1 de brillo y 2 de deshilachado), con el primero disponible al formar la red y caché por animación. Liberación mediante Object.Destroy de todos los sprites y texturas al acabar o destruirse antes. Crecimiento de 0,4 s y deshilachado de 0,6 s conservados; brillo limitado al tiempo restante de la red. Radios cada 22 px, anillos cada 11 px, rocío de probabilidad 0,12 solo en anillos pares y opacidades 170/130; paleta, contorno, centro, filtro Point y 16 px/u conservados. Constantes visuales provisionales al inicio. Sin cambios en daño, inmovilización ni cálculo del polígono.
- Archivos tocados: `Assets/_Primordia/Scripts/Mutations/SilkNetArt.cs`, `Assets/_Primordia/Scripts/Mutations/SilkNet.cs`, `Assets/_Primordia/Tests/PlayMode/Floor3LimbsTests.cs` y esta tarea. Scripts guardados con CRLF.
- Pruebas (cuántas pasan): Unity 6000.6.2f1, PlayMode en batchmode autorizado: 21/23 aprobadas. Floor3LimbsTests: 14/16; Floor3SkinTests: 7/7. Pasan las cinco pruebas de SilkNet/SilkNetArt, incluidas generación progresiva, destrucción anticipada, liberación de texturas al final, daño único de 4, inmovilización de 3 s y contorno. Fallan únicamente las dos JumpingLegs ya documentadas: JumpingLegs_JumpsFourUnits_OverLowObstacle_ButNotOverWall y JumpingLegs_LevelTwoDamagesOnLanding. Sin errores CS; git diff --check correcto. Log: `Logs/telarana-tests.log`; resultados conservados en `Logs/telarana-tests.xml`.
- Dudas / no comprobado: sin dudas de implementación. Pendientes la revisión visual de la usuaria y la medición del tirón en juego; batchmode verifica creación diferida y liberación, pero no la fluidez ni el aspecto percibido.
