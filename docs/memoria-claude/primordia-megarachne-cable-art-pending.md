---
name: primordia-megarachne-cable-art-pending
description: Cables v2 de la Megarachne (2026-11-18) - arte hecho por Opus (haz de hebras + abanicos de arranque sobre la copa); falta importar y programar
metadata:
  type: project
---

**2026-11-18 (Opus): ARTE HECHO, falta importar y programar.** La usuaria no veía bien la unión de los cables con la
copa del Nido en `Test_BossMegarachne` y la textura parecía una pasarela. Eligió el **estilo B** entre 3 propuestas:
- Textura: **haz de hebras de seda** casi paralelas, 64×29 (= 1,8 u, sin estirar). La antigua (32×14) se estiraba ×2 a
  lo ancho por el ancho del cable: por eso parecía una tabla.
- **Abanico de seda** en cada arranque (`Nido/Nido_CableRoot`, 5 cuadros de 256×224, pivote = pie del tronco),
  encima de la copa: las hebras se despegan del cable y se pegan a las hojas.
- Variantes Thin/Grow/Burning/Charred rehechas con el mismo estilo; **cuadros en VERTICAL** (el LineRenderer repite la
  textura entera; el cuadro se elige con `mainTextureOffset.y`).
- Generador: `docs/multimedia-temp/floor3-bosses/megarachne/generar_cables_megarachne.py piezas`. Detalle para el
  código en el README de esa carpeta (sección «Cables v2»). **No ejecutar `generar_efectos_megarachne.py cables`**
  (machacaría los tramos nuevos).

**Falta (Sonnet/Codex):** importador (`ArtSetup.BossMegarachne.cs`: añadir `Nido_CableRoot` con celda 256 y pivote
(0,5, 36/224); los `Cable_*` sin cortar en sprites de 32), material `mainTextureScale = (0.25, 1)`, curva de ancho
sin afinar en la rama, 5 `SpriteRenderer` de arranque en orden 22 (ocultar el del cable que arde; alfa con la copa),
material propio con `Cable_Thin` para el hilo colgante. Después, que la usuaria lo pruebe.

Estado previo del código (rama `codex/mega-2-ataques`, sin commit): `BodyScale = 0.75`, `CableWidth = 1.8`,
`CableSeat = 0`, `CableOrder = 19` detrás de la copa (20).

Relacionado: [[primordia-boss-megarachne]], [[primordia-provisional-art]].
