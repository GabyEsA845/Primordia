# Recuadro del Órgano en el HUD (abajo a la izquierda)

Generado con `generar_hud_organo.py` y la skill `primordia-sprites`. Sustituye a la barra de recarga bajo el jugador
(`UI/CooldownBar.cs`, `UI_Cooldown_*`): debajo del jugador va ahora la barra de carga del Chorro de vinagre.
Paleta del HUD: membrana `MEM` de los marcos, savia en el lima de la ranura del Órgano `#2e6a1e … #d4ffb0`.

## Hojas (`HUD/`)

| Hoja | Cuadros | Tamaño | ms | Uso |
|---|---|---|---|---|
| `UI_Organ_Panel` | 1 | 46×36 | — | Recuadro de 36×36 (icono grande del órgano, 32×32, en **(2, 2)**) y tubo vacío a la derecha |
| `UI_Organ_Panel_Empty` | 1 | 46×36 | — | Sin órgano: el recuadro sin tubo (encima, `UI_Organ_EmptyIcon` en (2, 2)) |
| `UI_Organ_EmptyIcon` | 1 | 32×32 | — | Contorno punteado de un órgano, apagado |
| `UI_Organ_SapFill` | 1 | 4×32 | — | Savia del tubo, en **(40, 2)**. `Image` *Filled*, *Vertical*, origen *Bottom*, `fillAmount` = progreso |
| `UI_Organ_SapBubbles` | 6 | 4×32 | 90 | Burbujas que suben, en bucle; mismo sitio y mismo recorte que el relleno |
| `UI_Organ_SapTop` | 4 | 4×2 | 100 | Superficie que tiembla; su fila de abajo va 1 px por encima de la altura del relleno. No se ve con el tubo lleno |
| `UI_Organ_Notch` | 1 | 4×1 | — | Muesca para los que se recargan matando: **n − 1** muescas en (40, 2 + 32 − 32·k/n), k = 1 … n − 1 |
| `UI_Organ_Ready` | 7 | 46×36 | 60 | Aviso de listo (0,42 s, una vez): destello del marco y del tubo, brillo diagonal por el icono y chispitas |
| `UI_Organ_Pulse` | 6 | 46×36 | 120 | Pulso suave en bucle mientras está listo (después del aviso) |

Posición: abajo a la izquierda, a **8 px** de los dos bordes de la pantalla (480×272). Pivote: arriba a la izquierda (las
coordenadas de arriba son desde esa esquina, en píxeles del sprite).

## Comportamiento (para el código, con Sonnet)

- **Recargando:** el icono va **apagado** y le vuelve el color **de abajo arriba** al ritmo del progreso. Dos `Image` del icono: debajo,
  el apagado (material que pasa la luminancia a los tonos `#28463e #36594f #4a7062 #628a7a`, conservando el contorno, alfa 235); encima,
  el de color con *Filled Vertical Bottom* y el mismo `fillAmount`. En el borde del corte, una línea de 1 px lima `#8ce659` solo sobre
  el icono (puede ser una `Image` de 32×1 que se mueve con el corte, enmascarada por el icono).
- **Por muertes** (`AbilityMutation.KillsToChargeAt`): progreso = muertos / necesarios, con las muescas.
- **Listo:** `UI_Organ_Ready` una vez + `SFX_UI_OrganReady`; después, `UI_Organ_Pulse` en bucle hasta que se use.
- **Sin órgano:** `UI_Organ_Panel_Empty` + `UI_Organ_EmptyIcon`, sin tubo.
- Si se intenta usar mientras recarga, sigue sonando `SFX_Player_AbilityNotReady`.

## Revisión

`vista_previa_hud_organo.png` (todos los estados) y `gif/escenas/escena_recarga.gif` / `escena_recarga_matando.gif` (la esquina de la
pantalla). Solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_hud_organo.py`. Sonido: `audio/generar_audio.py UI_OrganReady`.
