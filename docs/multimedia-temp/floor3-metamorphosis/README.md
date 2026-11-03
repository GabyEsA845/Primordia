# Metamorfosis del piso 3

Generado con `generar_metamorfosis_piso3.py` (`python generar_metamorfosis_piso3.py`, necesita Pillow, numpy y scipy).
Lee los cuerpos de `../floor2-bodies` y `../floor3-bodies`. Mismo método que `../metamorphosis` y `../exchange-room`.
16 px por unidad, filtro Point, sin compresión. Hojas en fila, cuadro 0 a la izquierda.

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Morph_<A>_<B>` (A del piso 2) | 12 | 32×32 | Paso del piso 2 al 3: silueta blanca del cuerpo que llevabas (24 px, centrado) al cuerpo nuevo del piso 3 (32 px). 9 hojas |
| `Morph_<A>_<B>` (A del piso 3) | 12 | 32×32 | Sala de cambio del piso 3 (dentro de la cápsula, como las del piso 2). 6 hojas |
| `Reveal_<B>` | 6 | 32×32 | De la silueta blanca a los colores (`Idle_side`, cuadro 0). Sirve para el paso y para la cápsula |
| `Surface_Background` | 1 | 480×544 | Fondo de la subida, dos pantallas de alto: abajo el abismo del piso 2 (mismos colores que `Abyss_Background`), arriba el agua turbia del pantano, la superficie en y = 58 y por encima el pantano visto desde el agua (troncos, frondas, raíces que bajan) |
| `Surface_Rays` | 1 | 128×272 | Rayos de luz verdosa, translúcidos. Van pegados a la superficie (su borde de arriba en y = 61 del fondo) y se mueven con el fondo |
| `Surface_Mote` | 2 | 4×4 | Trocitos de hoja en suspensión para la mitad de arriba (abajo siguen las burbujas `Abyss_Mote`) |

`<A>`, `<B>`: `Jellyfish`, `Placoderm`, `Crinoid` (piso 2) y `Frog` (Escupitajo, área, lima), `Diplocaulus` (Peonza,
cuerpo a cuerpo, naranja), `Whipscorpion` (Chorro, proyectil, rosa). El aura y el estallido son los de
`../metamorphosis` (`Metamorphosis_Aura`, `Metamorphosis_Burst`), teñidos del color de la familia.

## Qué necesita el código

- **La hoja depende de la pareja:** en el piso 1 la silueta salía siempre de la célula (`MutationData.MorphFrames`);
  aquí sale del cuerpo que llevas. `Metamorphosis.Play` debe buscar `Morph_<cuerpo actual>_<cuerpo nuevo>` (por
  ejemplo una tabla en el componente o en `MutationData`, con la célula como origen en el piso 1). La sala de cambio del
  piso 3 usa las 6 parejas entre cuerpos del piso 3, igual que la del piso 2.
- **La subida (paso del piso 2 al 3):** en lugar de caer por `Abyss_Background`, la criatura sube. El fondo empieza
  mostrando su parte de abajo (abismo) y se desplaza hacia abajo durante toda la cinemática, de modo que al terminar la
  revelación queda a la vista la superficie, justo encima de la criatura. Las motas suben como antes; desde la mitad del
  recorrido se cambian las burbujas por `Surface_Mote`. Al acabar, el estallido coincide con llegar a la superficie y
  después viene el fundido. Ver `gif/Metamorphosis_*.gif`.
- Fondo y velocidad por piso: conviene que la fosa (o su `FloorConfig`) diga qué fondo usa, así la del piso 1 sigue
  con el abismo.

`vista_previa_transformaciones.png`, `vista_previa_fondo.png` y `gif/` son solo para revisar (los GIF `Exchange_*`
son las 6 parejas de la sala de cambio).
