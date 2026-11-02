# Evolución de los linajes del piso 3 (pantano carbonífero)

Generado con `generar_evolucion_piso3.py` (`python generar_evolucion_piso3.py`, skill primordia-sprites; usa los cuerpos
de `../floor3-bodies/`). Mismo momento que en los pisos 1 y 2: el cuerpo brilla, se transforma en la silueta del animal
con el color de su clase y vuelve.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, celda **80×80** (piso 1: 48,
piso 2: 64), pivote *Center*. 12 cuadros con estas duraciones (ms), unos 2 s, igual que antes:
`100, 110, 120, 120, 130, 150, 260, 260, 260, 150, 120, 200`

El primer y el último cuadro son el cuerpo del jugador en reposo mirando abajo (`Player_<Cuerpo>_Idle_down`, cuadro 0),
centrado: el cuerpo que da la Boca del linaje.

| Hoja | Linaje | Clase | Cuerpo de partida | Silueta |
|---|---|---|---|---|
| `Evolution_Eryops` | Eryops | Carnívoro (rojo) | Diplocaulus | Anfibio enorme de cabeza ancha y chata, patas abiertas |
| `Evolution_Xenacanthus` | Xenacanthus | Carnívoro | Whipscorpion | Tiburón de agua dulce: cuerpo de anguila, aleta dorsal larga y espina en la nuca |
| `Evolution_Pulmonoscorpius` | Pulmonoscorpius | Venenoso (morado) | Whipscorpion | Escorpión gigante: pinzas grandes y cola segmentada con aguijón |
| `Evolution_Centipede` | Ciempiés | Venenoso | Frog | Desde arriba: cuerpo ondulado de 11 segmentos con patas y antenas |
| `Evolution_TinderFungus` | Yesquero | Hongo (ocre) | Frog | Tocón con tres repisas de hongo y una llamita de yesca |
| `Evolution_Cordyceps` | Cordyceps | Hongo | Whipscorpion | Insecto con tallos de hongo con maza que le brotan del lomo |
| `Evolution_Lamprey` | Lamprea | Parásito (granate) | Diplocaulus | Anguila con el disco de la boca y su anillo de dientes |
| `Evolution_Leech` | Sanguijuela | Parásito | Frog | Cuerpo anillado en arco con una ventosa en cada punta |

Color de la clase nueva, **Parásito**: `#3a0e1e #9a2044 #d04a62 #f4a0a4` (granate). El pof al perder el linaje es el del
piso 1 (`../lineage-evolution/Effects/LineageLost.png`, el juego lo tiñe).

`vista_previa_evolucion_piso3.png`, `vista_previa_siluetas.png` y `gif/` son solo para revisar.
