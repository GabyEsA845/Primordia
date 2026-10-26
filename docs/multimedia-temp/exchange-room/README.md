# Sala de cambio (el laboratorio de Darwin)

Generado con `generar_sala_cambio.py` (`python generar_sala_cambio.py`, necesita Pillow, numpy y scipy). Lee los cuerpos
de `../floor2-bodies`; la revelación con colores reutiliza `../metamorphosis/Reveal_<Cuerpo>.png`.

16 px por unidad, filtro Point, sin compresión. Hojas en fila, cuadro 0 a la izquierda.

| Hoja | Cuadros | Lienzo | Apoyo | ms | Uso |
|---|---|---|---|---|---|
| `Capsule_Idle` | 6 (bucle) | 40×48 | Abajo en el centro (20, 47) | 140 | Cápsula con líquido verde y burbujas; base y tapa de latón. Ocupa 2×2 tiles (la `K` del plano); el vidrio sube por encima |
| `Capsule_Glow` | 4 (bucle) | 40×48 | Igual, encima | 100 | Brillo del líquido en blanco: el juego lo tiñe del color de la familia de la Boca |
| `Capsule_Flash` | 5 | 40×48 | Igual, encima | 70 | Destello al salir la criatura nueva, en blanco (teñido) |
| `Capsule_Used` | 4 (bucle) | 40×48 | Igual | 260 | Ya usada en este piso: el líquido baja y se apaga |
| `Morph_<A>_<B>` | 12 | 32×32 | Centro | — | Silueta blanca que pasa del cuerpo A al B, dentro del vidrio (centro del lienzo en (20, 25) de la cápsula) |

`<A>`, `<B>`: `Jellyfish` (Cnidocitos), `Placoderm` (Placas mandibulares) y `Crinoid` (Corona de brazos); las 6 parejas.

**Metamorfosis en la cápsula (~2,4 s):** el líquido brilla del color de la familia nueva, la silueta del cuerpo viejo
aparece dentro y cambia (`Morph_<A>_<B>`, ~1,1 s), se revela con colores (`../metamorphosis/Reveal_<B>`, 0,4 s), destello
y la criatura sale delante de la cápsula.

`vista_previa_capsula.png`, `vista_previa_secuencia.png` y `vista_previa_transformaciones.png` son solo para revisar.
