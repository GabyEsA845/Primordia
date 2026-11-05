---
name: primordia-floor3-art-batch-pending
description: "HECHA (2026-11-02): tanda de pulido del piso 3 (lodo en el cuerpo, puertas, llamas, iconos, agua, odres, fumarola); dibujada, importada y programada; qué queda ligado"
metadata:
  type: project
---

**Tanda de pulido del piso 3 TERMINADA** (commits en `feature/floor-3`: arte 819e35d, A 6ee66da, B 71d8a2a, C dbf8629, D 90b5013, E 6bbc035; sin push).
Arte y generador: `docs/multimedia-temp/floor3-polish/` (README con las hojas y lo que hace el código). Menú: `Primordia > Art > Importar
pulido del piso 3` (crea `Resources/PolishArt` y añade `BurningMarker` al prefab `Effect_Burning`; hay que repetirlo si se cambian hojas).

Qué hay y dónde (código en `Assets/_Primordia/Scripts/Rooms` salvo `PolishArt` en Core):
- **Agua de charca** (`GroundPuddle`): apaga el quemado (jugador y enemigos), salpicadura, ondas, gotas, vapor. FALTA la franja de agua
  que tapa las patas del cuerpo (se aprobó el arte, no se programó): usar el mismo mecanismo de pintura que el lodo (`MudBodyPaint`).
- **Arder** (`BurningMarker`): llamas S/M/L por tamaño, lenguas y velo naranja; iconos de barras nuevos en `BurnIcon` (llama y nube con calavera).
- **Lodo en el cuerpo** (`MudBodyPaint` + `MudBodyVisual`; `MudSinkVisual.SetLevel` lo elige; sin arte, máscara antigua): repinta los sprites;
  la esquiva deja el hundimiento EN PAUSA (`PlayerMudSink.DodgeResetsSink`; la mutación de SALTO del paso 4 debe ponerlo a true).
- **Puertas** (`DoorwayCrossing`): el jugador pasa por detrás del marco, pisos 1-3. Tests en `Tests/PlayMode` (WaterPuddle, BurningMarker, MudBodyPaint, DoorwayCrossing).
- **Fumarola** (`SteamVent`): empuje ~2 tiles a jugador (`PlayerController.Shove`) y enemigos; luz cian en odres (`MethaneSac.SetGlow`).

Ligado que sigue pendiente: los enemigos muy inflamables dejan rastro de fuego al arder (paso 5, enemigos).

**Revisado 2026-11-04 (Opus):** la franja de agua y el rastro de fuego **NO necesitan arte nuevo**: la franja ya está aprobada (falta programarla con `MudBodyPaint`) y el rastro reutiliza `Fx_GroundFire` + `Fx_TinderScorch_*` vía `GroundFire.Spawn` (como el Yesquero). Ambos son trabajo de Sonnet.

**Franja de agua y rastro de fuego PROGRAMADOS (2026-11-05, Sonnet; SIN probar en juego ni commit):**
- Franja de agua: `MudBodyVisual.SetWater(raiz, bool)` (modo agua dentro del pintado del lodo) + `MudBodyPaint.Settings.water`: las 4 filas de abajo del cuerpo al 55 % de `#2a5e54` y la fila de arriba con la línea `#9cc8a0`/`#4e8a72` que titila; no toca contorno ni ojos. `GroundPuddle.CheckWater` lo pone al entrar (jugador y enemigos) y lo quita al salir; funciona con la gran charca y con los charcos de agua. Sin el arte del pulido no hace nada. Pruebas `WaterStripTests`.
- Rastro de fuego: `Burn.LeavesFireTrail`: Calamites, brote, Medullosa y libélula (cuerpo seco o vegetal; DECISIÓN MÍA al no estar definido, cambiar si la usuaria prefiere otros) dejan, ardiendo y andando, `GroundFire` de 2 s a los pies cada 0,4 s (mín. 0,3 u de paso) con `TinderScorch`. El escarabajo ya dejaba el suyo; la araña y el sapo no.
