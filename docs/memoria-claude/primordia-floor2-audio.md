---
name: primordia-floor2-audio
description: "Audio del piso 2 terminado y en main (merge 2a6268f, 2026-10-27): música por franja, laboratorio, jefes, 102 efectos por datos y por código, Sfx.Stop"
metadata:
  node_type: memory
  type: project
  originSessionId: dc1d32ca-4c5b-4c78-a785-2f47937ada22
  modified: 2026-10-27T16:01:39.540Z
---

**TERMINADO y en main (merge 2a6268f, 2026-10-27)**, rama `feature/floor2-audio` subida. PLAN marcado, GDD, MULTIMEDIA, pisos.md de la skill y documento del piso 2 (sección «Música y sonido», versión 10) al día. Lo de abajo queda como referencia de cómo se hizo.

**Hecho y en la rama (2026-10-27):**
- Skill [[primordia-audio-skill]] (ae4b1f5). Decisiones del usuario: todo en re (cada franja cambia el modo), música por franja con capas exploración/combate, `Music_Exchange` para la sala de cambio (común a todos los pisos), `Music_Shop` común a todas las tiendas, fundido cruzado de ~3 s entre franjas, losas de roca para las puertas.
- Audio generado y aprobado por el usuario en `docs/multimedia-temp/audio/`: `PROMPT_piso2.md` (241 entradas entre los dos encargos), `instrumentos_piso2.py`, `musica_piso2.py` (11 pistas; los jefes se rehicieron más intensos a petición del usuario: Anomalocaris 126 BPM, Euriptérido 120), `efectos_piso2.py` (102 efectos + 2 ambientes). Commits 230abe4…946f1d4.
- Integración paso 1 (f784ba6), probado por el usuario: MusicDirector por franja (`Room.DepthBand` puesto por FloorBuilder), `BandFade` 3 s, laboratorio, jefes del piso 2 por `ModeChanged`, ambientes Abismo/sala de jefe; lee `FloorGenerator.CurrentFloor` al cargar escena. Arreglo `Turning.Towards` (RotateTowards con objetivo justo detrás) y 3 pruebas del Euriptérido corregidas.

**Paso 2:** commit 03bc176 (2026-10-27), probado por el usuario. `ProjectSettings.asset` (Unity vació preloadedAssets) se deja fuera de los commits.

**Paso 3 (commit 4030e4b, probado por el usuario):** constantes en `Sounds.cs` y `Sfx.Play` en cada sitio. Nuevo `Sfx.Stop(id)`/`AudioManager.StopSound` (fundido de 40 ms) e `IsSoundPlaying`. Decisiones: abanico de Cnidocitos sustituye al disparo en la salva; un solo bucle de remolino mientras quede alguno; Anomalocaris y Euriptérido con `PlayModeSound(previo, siguiente)` en SetMode (corta Charge/Circle/Silhouette y PincerWarn al salir); ojos y rugido, derrumbe y jadeo en las escenas (se cortan si se salta); pasos del Euriptérido cada 1,4 u; FuryShot en cada anillo, espiral y abanico; RockFall 0,15 s antes de tocar suelo (ahí está el golpe); enemigo que cae a la fosa no hace sonido de muerte; Inestable y corazón morado en PlayerAudio (Health.ExtraDamageTaken/PurpleBlocked). 4 pruebas nuevas en AudioTests.

**How to apply:** usar la skill primordia-audio; compilar sin Unity con un check.py en el scratchpad ([[primordia-unity-gotchas]], rehacerlo si el scratchpad es nuevo); no hacer commit sin confirmación del usuario.

- Arreglo de paso: `FloorGenerator.CurrentFloor` tiene `internal set` para que las pruebas fijen el piso (los static no se limpian entre pruebas y el MusicDirector lo lee al empezar).
