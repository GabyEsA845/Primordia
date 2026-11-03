# Sala de paso del piso 2 al 3: la charca

Generado con `generar_charca.py` (`python generar_charca.py`, usa `../floor3-environment/base_piso3.py`) y la skill
`primordia-sprites`. Sustituye a la fosa azul (`../floor-transition/Transition_Pit*`) en la sala de paso del piso 2.
16 px por unidad, filtro Point, sin compresión.

| Archivo | Cuadros | Tamaño | ms | Uso |
|---|---|---|---|---|
| `Transition_Pit_Swamp.png` | 8 | 112×80 (7×5 tiles) | 167 (6 por segundo, en bucle) | Pozo en 3/4 con borde de tierra y musgo, raíces de Lepidodendron medio enterradas en el borde, pared interior de tierra húmeda con raíces que cuelgan y, abajo, agua turbia con luz verdosa que late, dos ondas que se abren y esporas que flotan. Mismo tamaño y pivote (centro) que la fosa del piso 1: el borde sólido y la E no cambian |
| `Transition_Bubble_Swamp.png` | 4 | 8×8 | — | Burbuja verdosa: cuadros 0-1 sube (en bucle mientras sube), 2 revienta en anillo, 3 chispitas |

## Qué necesita el código

- **La fosa elige su arte por piso:** la del piso 1 sigue con la fosa azul; la del piso 2 usa estas hojas.
- **La burbuja revienta:** hoy `TransitionPit.UpdateBubbles` repite todos los cuadros y la desvanece. Con esta hoja,
  los cuadros 0-1 en bucle mientras sube y, en los últimos 0,25 s de su vida, el 2 y el 3 (sin desvanecerla).
- El icono del mapa es el mismo (`../floor-transition/UI_Map_Transition.png`).
- Ambiente que sube junto a la charca: `SFX_Amb_Floor3_Loop` (audio, `PROMPT_piso3.md`).

`vista_previa_charca.png`, `vista_previa_comparacion.png` (con la fosa del piso 1) y `gif/` son solo para revisar.
