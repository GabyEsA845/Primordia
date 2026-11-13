# Tarea 08 — Lepidodendron ancestral, parte 4: ataques (L4)

- **Rama:** `codex/lepido-4-ataques` (desde `codex/lepido-3-conos`, **que aún no está fusionada en `feature/floor-3`**: la usuaria prueba L3+L4+L5 juntas al final)
- **Escrita por:** Claude (2026-11-13) · **Implementa:** Codex
- **Estado:** pendiente
- **Plan completo:** `docs/codex/lepidodendron-plan.md`. Haz **solo esta** tarea.

## Objetivo
Dar al jefe sus ataques propios: **latigazos de raíces**, **corro de púas bajo el jugador**, **lluvia de esporas con marca**, y **enemigos por las esquinas** en la fase final; y las **esporas en arco sobre las paredes** en la fase de paredes. Los ataques corren **a la vez** que el ciclo de conos de la L3 y se aceleran al morir conos. El arte está aprobado: solo importar y programar. Estructura, paredes, conos, escena `Test_BossLepidodendron` y teclas **ya existen**: amplíalas, no las rehagas.

## Contexto mínimo (leer solo esto)
- `docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`, sección **«Ataques de la fase final»** (hojas, celdas, pivotes, ms, **cuadros con daño**, punta por cuadro del latigazo) y la de **Capa de hojas** (`Canopy_Arena_Shake`).
- Código para imitar: `LepidodendronBoss.Cones.cs` (ciclo, aviso con sombra, sacudida de la copa, limitador, golpe con daño en un cuadro), `LepidodendronBoss.Walls.cs` (oleadas, `FinalPhaseReady`), `LepidodendronBoss.cs`, `Editor/ArtSetup.BossLepidodendron.cs` (añade las hojas de `Attacks/` que falten), `Editor/LepidodendronBossSetup.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Para la marca de la espora, el patrón de la piedra que cae del Euriptérido (`Fx_RockFallShadow`): busca cómo elige el cuadro según el tiempo que falta.
- Para enemigos por esquinas y topes por sala: cómo L2 invoca las oleadas (mismos prefabs, mismo tope de vivos).
- Lección de la tarea 04: lo creado por código (texturas, sprites) se destruye con el objeto.

## Qué hacer
1. **Importar** las hojas de `Attacks/` (9): `Lepido_Whip_Warn/Sweep/Furrow`, `Lepido_Spikes_Warn/Erupt/Scar`, `Proj_LepidoSpore`, `Fx_LepidoSporeMark`, `Fx_LepidoSporeSplash` (amplía `ArtSetup.BossLepidodendron.cs` y el catálogo `LepidodendronArt`). Si falta alguna, **para y avisa**.
2. **Latigazo** (`LepidodendronBoss.Attacks.cs`, partial nuevo). Franja horizontal que sale de un borde de la arena (izquierdo o derecho, **alternando**; Flip X para el derecho) a la altura (y) del jugador en el momento de empezar el aviso, recortada a la arena y a ≥ 1 u de las paredes. `Whip_Warn` (~0,8 s, en el suelo) → `Whip_Sweep` (23 × 55 ms). **Daño al jugador 2** en los cuadros **2-20**, **una vez por latigazo** (no por cuadro), hitbox = rectángulo de ~1,3 u de ancho entre x = 10 px y la punta del cuadro actual (tabla de puntas del README), alto de la franja. Va con el jugador por altura (base = pivote + 6 px). Copa sacudida en los cuadros 3-14. Al acabar queda `Whip_Furrow` y se desvanece en ~1 s. Vida: **no daña la armadura ni al cono** y no se le puede dañar.
3. **Corro de púas.** Centro = posición del jugador al empezar el aviso, recortada a la arena y ≥ 2 u de las paredes. `Spikes_Warn` (6 × 130 ms) → `Spikes_Erupt` (8 × 80 ms), **daño 2** en los cuadros **0-4**, radio ~1,3 u, **una vez por corro**; la púa central primero y las 4 después (el arte ya lo hace). Va con el jugador por altura. `Spikes_Scar` se desvanece en ~0,6 s. El corro **no se puede mover** durante el aviso (si el jugador se va, falla: así se esquiva).
4. **Lluvia de esporas.** Una oleada = **6 esporas** (fase final) que caen a puntos al azar dentro de la arena (≥ 2 u de las paredes; al menos 2 cerca del jugador, el resto repartidas), **cada una con su `Fx_LepidoSporeMark`** en el suelo. Cada espora sale con un retraso aleatorio 0-1,2 s, cae desde arriba con `Proj_LepidoSpore` (dibujada encima de todo salvo la copa) en ~1,0 s; el cuadro de la marca depende del tiempo que le falta (0 = recién sale, 5 = a punto de tocar). Al tocar el suelo, `Fx_LepidoSporeSplash`; **daño 1 solo en su cuadro 0**, radio ~0,9 u, una vez. Las esporas son visibles solo desde que salen (no hay que verlas colgando).
5. **Esporas en arco sobre las paredes (fase de paredes).** Mientras una pared está viva y activa, cada ~**10 s** lanza una lluvia de **3-4 esporas** (misma mecánica) que caen **en el pasillo, alrededor de esa pared** (a ±4 u de su x, en el ancho del pasillo, ≥ 1 u de la pared). No cae si la pared está retirada o ya en ceniza. No se solapa con la oleada de enemigos recién invocada (retrasa la lluvia 3 s si acaba de empezar una oleada).
6. **Enemigos por las esquinas (solo fase final).** Cada ~20 s, **2 enemigos** entran por las esquinas de la arena (mezcla de los de L2: arañas, escarabajos, libélulas, Calamites; elige 2 distintos al azar), con el mismo tope de vivos que L2 (6) y sin superarlo. Marcador previo corto (1 s) en la esquina con un brillo morado en código, sin hoja nueva (provisional). Los enemigos que quedan al morir el jefe se limpian.
7. **Cadencia (provisional, constantes al inicio):** latigazo cada **7 s**, corro cada **9 s**, lluvia de 6 esporas cada **12 s**, enemigos cada **20 s**. Cada cono que muere acorta todos los intervalos un **15 %** (acumulativo, mínimo 50 %). Arrancan **2 s después** de empezar la fase final (no en el mismo instante que baja el primer cono) y **nunca dos ataques de aviso a la vez**: cola de ataques con un hueco mínimo de **1,2 s** entre el inicio de uno y de otro. Si el jugador está muriendo/pausado, el temporizador se detiene (como los demás ataques).
8. **Limitador y muerte.** Cualquier ataque en curso se **cancela y limpia** si el jugador muere o si cambia de sala; al terminar la fase (`ConesDestroyed`) se cancelan los ataques pendientes y los activos acaban su animación sin hacer daño nuevo.
9. **Teclas de prueba** (en `DebugCheats`, libres, documentadas en `Resultado`): lanzar latigazo (izquierda/derecha según la alternancia), lanzar corro, lanzar lluvia de esporas, lanzar enemigos de esquina, y una tecla para **activar/desactivar** la cadencia automática de los ataques (para probar cada uno sin ruido).
10. **Pruebas** en `LepidodendronBossTests`: (a) el latigazo avisa ~0,8 s, daña 2 **una sola vez** y solo cuando el jugador está dentro del rectángulo hasta la punta del cuadro; (b) el corro daña 2 una sola vez y no daña si el jugador salió del radio durante el aviso; (c) cada espora hace 1 una sola vez y solo cerca de su punto; (d) la lluvia de la fase de paredes solo cae junto a paredes vivas; (e) los enemigos de esquina respetan el tope de 6; (f) la cadencia se acorta un 15 % por cono muerto con mínimo del 50 %; (g) nunca hay dos avisos a la vez (hueco ≥ 1,2 s); (h) al morir el jugador / destruir la sala / `ConesDestroyed`, los ataques paran y no queda nada vivo ni textura fugada; (i) los ataques no dañan armadura, conos ni paredes; (j) **no se rompe** L1/L2/L3 (sus pruebas siguen verdes).

## Números y reglas (cerrados con la usuaria 2026-11-13; provisionales = se ajustan probando)
- Daño al jugador: latigazo **2**, corro **2**, espora **1**, golpe de cono 2 (L3).
- Intervalos: 7 / 9 / 12 / 20 s (provisional); −15 % por cono muerto (provisional); espora de pared cada ~10 s, 3-4 por vez.
- Avisos: latigazo ~0,8 s, corro ~0,8 s, espora marca visible ~1 s.
- Tope de vivos 6 (igual que las oleadas de L2).

## Archivos que puede tocar
- `Scripts/Enemies/LepidodendronBoss*.cs` (crea `LepidodendronBoss.Attacks.cs` y, si hace falta, `LepidodendronWhip.cs`, `LepidodendronSpikes.cs`, `LepidodendronSpore.cs`), `Editor/LepidodendronBossSetup.cs`, `Editor/ArtSetup.BossLepidodendron.cs`, `Scripts/Core/DebugCheats.cs`, `Tests/PlayMode/LepidodendronBossTests.cs`.
- Solo lo imprescindible y justificado en `Rooms/`/`Core/` — avísalo en `Resultado`.

## Fuera de alcance (NO hacer)
- No dibujar ni tocar PNG ni `.py`. Nada de barra propia, presentación, desplome, tocón, recompensa (L5).
- No tocar la Meganeura, otros jefes, el `Floor3.asset` ni la puerta secreta. No cambiar el comportamiento de paredes (L2) ni conos (L3) salvo engancharse a sus eventos.

## Cómo probar
- Menú `Primordia > Boss > Crear Lepidodendron y escena de prueba`; después `LepidodendronBossTests`, `MeganeuraBossTests`, `BossHealthBarTests`.
- A ojo (usuaria): cada ataque con su tecla (¿se lee el aviso?, ¿da tiempo a esquivar?), la fase de paredes con esporas, la fase final con todo junto (¿es injusto?, ¿qué intervalo sobra?).

## Resultado (lo rellena Codex)
- **Hecho:** implementada únicamente L4 en `codex/lepido-4-ataques`, creada desde `codex/lepido-3-conos`. Sin commit, push ni merge.
- Importadas las nueve hojas aprobadas de `Attacks/` mediante el menú existente, con sus celdas, pivotes y cuadros; ampliados el catálogo y la validación de hojas obligatorias. No se han modificado PNG ni ejecutado generadores.
- Latigazo alterno izquierda/derecha con aviso, hitbox hasta la punta de cada cuadro, un golpe de 2, sacudida y surco; corro fijo durante el aviso, un golpe de 2 y cicatriz; lluvia de seis esporas con retrasos, marcas por tiempo restante, proyectiles ocultos antes de salir y golpe de 1 en el cuadro 0 de la salpicadura.
- Lluvia de 3–4 esporas en arco junto a paredes activadas y vivas, fuera de su cuerpo y dentro del pasillo; espera de 3 s tras anunciar una oleada. La retirada de la pared cancela sus esporas.
- Esquinas: dos especies distintas de la reserva de L2 (sin sapo), aviso morado provisional de 1 s y tope compartido de seis vivos, incluidos descendientes. El sprite y la textura del aviso se destruyen al terminar de usarse.
- Cola de avisos de L4 con separación mínima de 1,2 s; una lluvia reserva también todo su intervalo de avisos (hasta 2,2 s). L3 conserva su ciclo independiente. Arranque automático a partir de 2 s de la fase final, reloj escalado para pausa, ajuste también de las esperas en curso al romper conos. Muerte del jugador/cambio de sala/desactivación cancelan y limpian; `ConesDestroyed` vacía la cola, limpia enemigos y deja terminar los efectos activos sin daño nuevo.

**Archivos tocados** (rutas de código relativas a `Assets/_Primordia/`):
- Nuevo `Scripts/Enemies/LepidodendronBoss.Attacks.cs` y su `.meta`.
- `Scripts/Enemies/LepidodendronBoss.cs`, `.Scenery.cs` y `.Walls.cs`: actualización, ciclo de vida, hojas obligatorias y avisos de oleada/limpieza; sin cambiar las mecánicas de conos ni paredes.
- `Editor/ArtSetup.BossLepidodendron.cs`, `Scripts/Core/DebugCheats.cs` y `Tests/PlayMode/LepidodendronBossTests.cs`.
- Generados por el menú: `Data/Bosses/LepidodendronArt.asset` y los nueve `.png.meta` de `Art/Floor3/floor3-bosses/lepidodendron/Attacks/`.
- Este archivo de tarea. Todos los `.cs` conservan CRLF. No hay cambios adicionales en `Rooms/` ni `Core/`; en `Core/` solo se amplían las teclas solicitadas de `DebugCheats`.
- Se ejecutó `Primordia > Boss > Crear Lepidodendron y escena de prueba`. Se descartó su regeneración incidental de la escena y su `.meta` para conservar la escena existente y su GUID; no se necesitaba alterar su estructura ni el prefab.

**Teclas de prueba (solo Editor)**
- `Ctrl+F7`: encolar latigazo (alterna izquierda/derecha).
- `Ctrl+F8`: encolar corro de púas.
- `Ctrl+F9`: encolar lluvia de seis esporas.
- `Ctrl+F10`: encolar enemigos de esquina.
- `Ctrl+F11`: activar/desactivar cadencia automática L4, incluidas lluvias de pared; al alternar se vacía la cola, sin cortar las animaciones activas. Los ataques manuales respetan la cola y los huecos.
- Preparación: abrir `Test_BossLepidodendron`; `Mayús+F8` recorre paredes y arena; `Ctrl+F1` activa la fase final. Desactivar L4 con `Ctrl+F11` para probar sus ataques por separado. Los conos de L3 siguen funcionando con sus teclas existentes.

**Pruebas**
- Menú de importación/creación: terminado con código 0. El primer intento aislado no encontró la licencia; el relanzamiento con acceso a la licencia local funcionó.
- Primera pasada PlayMode: 120/121 aprobadas. Único fallo: la nueva prueba de cambio de sala construía `Room` sin collider; corregida añadiendo `BoxCollider2D` al objeto de prueba.
- Verificación final PlayMode: **122/122 aprobadas, 0 fallidas**. `LepidodendronBossTests`: **69/69** (47 anteriores de L1/L2/L3 y 22 casos nuevos de L4); `MeganeuraBossTests`: **40/40**; `BossHealthBarTests`: **13/13**. Compilación sin errores de C#.
- Logs y XML fuera del repositorio, en `C:\Users\claud\AppData\Local\Temp\`: `primordia-l4-setup.log`, `primordia-l4-tests.log/.xml` y `primordia-l4-tests-final.log/.xml`.
- Tras la aclaración sobre el tercer cono: se evita incluso reajustar las esperas después de `ConesDestroyed`; prueba de fin de fase reforzada con cadencia automática activa y avance de 30 s sin ataques nuevos. Pruebas dirigidas: **6/6 aprobadas** (`L4CadenciaRestaQuincePuntosConSuelo` y `L4FinDeConosAnulaDanoYDejaTerminarAnimacion`), con `primordia-l4-cadencia.log/.xml` en la misma carpeta TEMP.
- Comprobación estática: CRLF en todos los C# tocados; `git diff --check -- '*.cs'` sin incidencias. Unity genera espacios al final de ciertos campos vacíos de sus `.meta`; se conserva su serialización.

**Dudas y comprobaciones pendientes**
- Resuelto con la usuaria: «−15 % acumulativo» resta 15 puntos porcentuales por cono: 100 % → 85 % → 70 %. Al morir el tercer cono, `ConesDestroyed` termina la fase y cancela la cadencia: el 55 % no se aplica. Constantes: 0,15 por cono y suelo de 0,5 como salvaguarda, no alcanzada durante esta fase. La prueba de la fórmula con 3/4 conos solo comprueba el cálculo y el tope, no fases jugables.
- Sin dudas de diseño pendientes. Queda la revisión visual/jugable de la usuaria: lectura de avisos, orden por altura, trayectoria de las esporas de pared y dificultad de L3+L4 juntas. Batchmode no permite dar por aprobados esos aspectos visuales ni el equilibrio de los intervalos provisionales.
