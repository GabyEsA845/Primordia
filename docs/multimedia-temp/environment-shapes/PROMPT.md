Necesito 6 tiles nuevos de pared para el escenario de mi juego en pixel art (Primordia, piso 1 bajo el agua), en dos variantes de color: `Soup` (salas normales) y `Boss` (sala del jefe). Son para poder hacer salas que no sean rectángulos (en forma de L, en cruz, con columnas en medio). Te adjunto mis tiles actuales de `Soup/tiles/` y `Boss/tiles/`; los nuevos tienen que salir de ellos para que encajen píxel a píxel.

Hazlo con un script de Python + Pillow (`generar_formas.py`) que lea mis tiles existentes y genere los nuevos a partir de ellos (copiando y modificando píxeles, no redibujando a mano), para que la textura y los colores sean exactamente los mismos.

## Cómo están hechos mis tiles (16×16, vista cenital en 3/4)

- `Wall_Cap`: relleno de pared visto desde arriba (textura oscura con círculos).
- Los bordes de pared son la misma textura de `Wall_Cap` con una franja de 3 píxeles en el lado que da al suelo. Desde el suelo hacia dentro: 1 px oscuro, 1 px claro, 1 px medio.
  - Soup: oscuro `#181425`, claro `#d8a0c8`, medio `#a46a98`.
  - Boss: oscuro `#181425`, claro `#e898a8`, medio `#a84858`.
- `Wall_EdgeLeft`: franja en las 3 columnas de la **derecha** (pared a la izquierda del suelo). `Wall_EdgeRight`: franja en las 3 columnas de la **izquierda**. `Wall_EdgeBottom`: franja en las 3 filas de **arriba**.
- `Wall_FaceUpper_0/1` y `Wall_FaceLower_0/1`: la cara frontal de la pared de arriba (2 filas de tiles), con grietas. `FaceUpper` empieza con 1 fila oscura y 1 clara arriba; `FaceLower` termina con 1 fila oscura abajo (con gotas).

## Tiles nuevos (para cada variante, Soup y Boss)

1. `Wall_FaceUpper_EndLeft`: copia de `Wall_FaceUpper_0` donde la cara **termina por la izquierda** porque ahí hay suelo (el extremo de un entrante o de una columna, visto de frente). Pon en las 3 columnas de la izquierda la franja de borde vertical (columna 0 oscura, columna 1 clara, columna 2 media) en toda la altura, respetando que las filas 0 y 1 ya son la línea oscura y clara de arriba: en la esquina superior izquierda, las dos franjas se unen como una esquina redondeada de 1 px.
2. `Wall_FaceUpper_EndRight`: lo mismo con la franja a la derecha (espejo horizontal del anterior).
3. `Wall_FaceLower_EndLeft`: copia de `Wall_FaceLower_0` con la misma franja vertical en las 3 columnas de la izquierda; la última fila (oscura, toca el suelo) se mantiene.
4. `Wall_FaceLower_EndRight`: espejo del anterior.
5. `Wall_OuterTopLeft`: copia de `Wall_Cap` con franja **arriba y a la izquierda a la vez** (como `Wall_EdgeBottom` y `Wall_EdgeRight` juntos). Es la esquina de arriba a la izquierda de una masa de pared con suelo encima y a la izquierda. En la esquina, las franjas forman anillos concéntricos: el borde oscuro exterior, luego el claro y luego el medio. El píxel de la esquina (0, 0) es oscuro.
6. `Wall_OuterTopRight`: espejo horizontal del anterior (franja arriba y a la derecha).

Todos de 16×16, opacos (sin transparencia), en PNG.

## Qué me tienes que entregar

```
environment-shapes/
  Soup/tiles/   (los 6 PNG con esos nombres exactos)
  Boss/tiles/   (los 6 PNG)
  vista_previa_formas.png
  generar_formas.py
```

La vista previa (ampliada ×3) debe mostrar, con los tiles viejos y los nuevos juntos, para Soup y para Boss:

- Una sala en L (texto de arriba abajo; `#` pared, `.` suelo):
```
##########
##########
##########
#...######
#...######
#...######
#........#
#........#
##########
```
- Una sala con una columna de 2×3 en medio:
```
########
########
########
#......#
#..##..#
#..##..#
#..##..#
#......#
#......#
########
```

Para montar la vista previa usa estas reglas (son las que usa el juego), mirando cada celda de pared y a sus vecinas:

- Suelo justo debajo → `FaceLower`; suelo dos celdas más abajo → `FaceUpper`. Si además hay suelo a su izquierda → `_EndLeft`; a su derecha → `_EndRight`.
- Si no es cara: suelo encima y a la izquierda → `Wall_OuterTopLeft`; encima y a la derecha → `Wall_OuterTopRight`; solo encima → `Wall_EdgeBottom`; solo a la izquierda → `Wall_EdgeRight`; solo a la derecha → `Wall_EdgeLeft`.
- Si la vecina de la izquierda es una cara → `Wall_EdgeRight`; si lo es la de la derecha → `Wall_EdgeLeft` (el borde lateral sube hasta lo alto de la cara).
- Si solo hay suelo en la diagonal de arriba a la derecha → `Wall_CornerBL`; en la de arriba a la izquierda → `Wall_CornerBR`.
- El resto → `Wall_Cap`. El suelo con pared encima → `Floor_ShadowTop_0`; el resto del suelo → `Floor_0`.

Comprueba en la vista previa que no haya cortes ni saltos de color entre un tile nuevo y sus vecinos (sobre todo donde la franja de borde lateral se une con la línea superior de la cara y con `Wall_EdgeBottom`).
