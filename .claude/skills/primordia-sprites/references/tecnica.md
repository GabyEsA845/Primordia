# Técnica de pixel art para Primordia

Reglas sacadas de las skills de pixel art públicas (Pixel Art Studio, Pixel Art Professional, los MCP de
Aseprite) y adaptadas a nuestro estilo. Lo que ya hacíamos bien sigue igual; lo nuevo está marcado.

## Color

- **Rampas de 4 tonos** (5 en cuerpos de 32 px o más): sombra profunda, base, luz, brillo. El tono base es el que
  más se ve y va en el índice 1 de 4 (2 de 5).
- **Nuevo: el tono gira** de 10° a 20° por paso: las sombras hacia azul-morado (frío), las luces hacia amarillo
  (cálido). `ramp(base)` lo hace solo. Una rampa que solo cambia de claridad se ve apagada y "de plástico".
  `ramp_info()` avisa si una rampa gira menos de 10°.
- La saturación es más alta en los medios tonos y baja en los extremos: el brillo no es blanco puro ni chillón.
- **Pocos colores por criatura:** 2 o 3 rampas (cuerpo, detalle, ojos) más el contorno. Si el linter cuenta más de
  24 colores opacos en un cuadro, la paleta se ha disparado.
- Colores entre cuadros: los mismos en toda la animación. Si aparece un color en un solo cuadro (el linter lo
  anota), suele ser un error, salvo destellos o efectos.
- Legibilidad sobre el fondo del piso: el enemigo tiene que separarse del suelo y de la pared de su franja (mirar la
  vista previa con el fondo del tema, no solo sobre gris).

## Sombra y volumen

- Luz arriba a la izquierda y algo de frente.
- **Nuevo: la sombra sale de la luz, no del borde.** Antes oscurecíamos todo el borde (`shade_legacy`), y eso es el
  "sombreado de almohada": el cuerpo parece un cojín inflado. Ahora el borde de arriba a la izquierda queda
  iluminado y solo se oscurece el de abajo a la derecha (`shade`).
- En cuerpos grandes (jefes, 32 px o más) se puede añadir **luz rebotada**: un tono más claro en el borde en sombra,
  justo dentro del contorno (`shade(..., reflect=True)`). Queda muy bien bajo el agua.
- El brillo es pequeño (2-4 px en 24 px) y va dentro de la forma, no pegado al contorno.
- Las piezas superpuestas se separan con valor (la de delante, más clara o con contorno), no solo con el color.

## Contorno y silueta

- Primero la silueta: tiene que entenderse en negro sobre blanco. Luego el color y el detalle.
- Contorno de 1 px con `OUTL`. Donde se juntan dos piezas con contorno salen bloques gruesos de 2×2: quitar el
  sobrante (`Canvas.clean()` y el aviso de "contorno grueso" del linter).
- Las partes finas (1-2 px) van sin contorno; si tampoco se distinguen así, se oscurecen un tono.
- Los rasgos del lado lejano (el otro ojo, la otra pata) no deben desaparecer: se marcan con un tono más oscuro o
  asomando por detrás.
- Curvas y diagonales con escalones regulares (1-1-1, 2-2-2, 2-1-2-1). Los escalones irregulares (1-3-1) se ven
  como dientes de sierra.

## Detalle y ruido

- Nada de **píxeles huérfanos**: un píxel suelto de un color que no repite ningún vecino es ruido, salvo que sea un
  brillo, una pupila o una chispa puestos a propósito.
- Las piezas sueltas de 1-3 px fuera del cuerpo solo si son chispas, burbujas, gotas o motas.
- **Dithering** solo para texturas (roca, arena, nieve marina) y fondos grandes, con patrón ordenado (Bayer 2×2 o 4×4).
  Nunca en criaturas de 24 px o menos.
- Sin antialiasing en sprites pequeños; en piezas de 64 px o más, solo en curvas largas y con un tono intermedio de
  la propia rampa (no semitransparente).

## Animación

- Cuadros de pocos en pocos: reposo 4, moverse 4, ataque 6 (2 de preparación, 2 de golpe, 2 de recuperación),
  daño 2, muerte 5-7. Los tiempos van en el README (reposo ~120-180 ms, golpes 60-80 ms).
- Anticipación antes del golpe y aplastamiento y estiramiento (el volumen se conserva: si se aplasta, se ensancha).
- Las poses clave primero; los intermedios después. Un cuadro = una función con parámetros (fase, aplastamiento,
  pose); así un cambio se aplica a toda la animación.
- El aviso de un ataque enemigo tiene que leerse a la primera (color, brillo, ojos rojos) y durar lo que dice el
  diseño.
- El cuadro que cuenta (disparo, mordisco, pulso) se marca en el README.

## Lista de comprobación (antes de enseñar)

1. El linter no da **avisos** (o están explicados) y lo de **revisar** se ha mirado en la imagen marcada.
2. La silueta se entiende a tamaño real (×1) y sobre el fondo de su franja.
3. La luz viene de arriba a la izquierda en todos los cuadros y piezas.
4. Las rampas giran de tono; no hay negro puro, y el blanco puro solo en chispas y en Hurt.
5. Contorno de 1 px, sin bloques gruesos; las partes finas, sin contorno.
6. Sin píxeles huérfanos; nada cortado por el borde de la celda.
7. Mismos colores en todos los cuadros; la animación tiene anticipación y el cuadro que cuenta está apuntado.
8. Tamaños, nombres y orientación como dice `references/pisos.md` y el resto de su familia.
