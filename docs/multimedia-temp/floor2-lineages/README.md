# Evolución de los linajes del piso 2 (océano cámbrico)

Generado con `generar_evolucion_piso2.py` (`python generar_evolucion_piso2.py`, necesita Pillow; usa `../floor2-bosses/base_piso2.py` y los cuerpos de `../floor2-bodies/`). Mismo momento que en el piso 1: el cuerpo brilla, se transforma en la silueta del animal con el color de su clase y vuelve.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, celda **64×64** (en el piso 1 eran 48×48), pivote *Center*. 12 cuadros con estas duraciones (ms), unos 2 s en total, igual que el piso 1:
`100, 110, 120, 120, 130, 150, 260, 260, 260, 150, 120, 200`

El primer y el último cuadro son el cuerpo del jugador en reposo mirando abajo (`Player_<Cuerpo>_Idle_down`, cuadro 0), centrado, el cuerpo que da la Boca del linaje.

| Hoja | Linaje | Clase | Cuerpo de partida | Silueta |
|---|---|---|---|---|
| `Evolution_Placoderm` | Placodermo | Carnívoro (rojo) | Placoderm | Pez acorazado con la boca entreabierta, de lado |
| `Evolution_Opabinia` | Opabinia | Carnívoro | Placoderm | Desde arriba: cinco ojos, trompa con garra, aletas y cola en V |
| `Evolution_Burgessomedusa` | Burgessomedusa | Venenoso (morado) | Jellyfish | Campana alta casi cuadrada con fleco de tentáculos |
| `Evolution_Anemone` | Anémona | Venenoso | Jellyfish | Columna con una corona de tentáculos abierta como una flor |
| `Evolution_Crinoid` | Lirio de mar | Herbívoro (verde) | Crinoid | Tallo, cáliz y brazos plumosos |
| `Evolution_Wiwaxia` | Wiwaxia | Herbívoro | Placoderm | Cúpula de escamas con dos filas de espinas |
| `Evolution_Trilobite` | Trilobite | Carroñero (marrón arena) | Placoderm | Desde arriba: céfalo, tres lóbulos, espinas y antenas |
| `Evolution_Hallucigenia` | Hallucigenia | Carroñero | Crinoid | Gusano con siete púas en el lomo y patitas |

Color de la clase nueva, **Carroñero**: `#4a3420 #8a6a40 #c8a46a #f2e0b4` (marrón arena). El pof al perder el linaje es el del piso 1 (`../lineage-evolution/Effects/LineageLost.png`, el juego lo tiñe).

`vista_previa_evolucion_piso2.png`, `vista_previa_siluetas.png` y `gif/` son solo para revisar.
